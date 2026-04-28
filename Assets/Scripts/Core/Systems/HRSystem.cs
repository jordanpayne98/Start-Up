// HRSystem Version: Clean v1
using System;
using System.Collections.Generic;

public enum HRTeamStatus { Idle, Searching, Interviewing }

public class HRSystem : ISystem
{
    public event Action<CandidateData> OnSearchCompleted;
    public event Action<HRSearchId> OnSearchStarted;
    public event Action<HRSearchId> OnSearchRetrying;
    public event Action<HRCandidatesReadyForReviewEvent> OnCandidatesReadyForReview;
    public event Action<CandidateData> OnCandidateAccepted;
    public event Action<int, int, int> OnPoolFull;

    private HRState _hrState;
    private EmployeeState _employeeState;
    private FinanceSystem _financeSystem;
    private TeamSystem _teamSystem;
    private RecruitmentReputationSystem _recruitmentReputationSystem;
    private IRng _rng;
    private ILogger _logger;
    private List<Action> _pendingEvents;
    private AbilitySystem _abilitySystem;
    private List<TeamId> _teamIdScratch;
    private List<EmployeeId> _employeeIdScratch;
    private List<int> _assignmentKeyScratch;
    private List<CandidateData> _pendingSearchCompleted;
    private List<HRSearchId> _pendingSearchRetrying;
    private List<HRSearchId> _pendingSearchStarted;
    private List<HRCandidatesReadyForReviewEvent> _pendingCandidatesReady;
    private List<CandidateData> _pendingCandidateAccepted;
    private struct PoolFullData { public int PoolCount; public int PoolMax; public int RejectedCount; }
    private List<PoolFullData> _pendingPoolFull;

    public bool HadCompletionThisTick { get; private set; }

    public HRSystem(HRState hrState, EmployeeState employeeState, FinanceSystem financeSystem,
        TeamSystem teamSystem,
        RecruitmentReputationSystem recruitmentReputationSystem, IRng rng, ILogger logger)
    {
        _hrState = hrState;
        _employeeState = employeeState;
        _financeSystem = financeSystem;
        _teamSystem = teamSystem;
        _recruitmentReputationSystem = recruitmentReputationSystem;
        _rng = rng;
        _logger = logger ?? new NullLogger();
        _pendingEvents = new List<Action>();
        _teamIdScratch = new List<TeamId>();
        _employeeIdScratch = new List<EmployeeId>();
        _assignmentKeyScratch = new List<int>();
        _pendingSearchCompleted = new List<CandidateData>();
        _pendingSearchRetrying = new List<HRSearchId>();
        _pendingSearchStarted = new List<HRSearchId>();
        _pendingCandidatesReady = new List<HRCandidatesReadyForReviewEvent>();
        _pendingCandidateAccepted = new List<CandidateData>();
        _pendingPoolFull = new List<PoolFullData>();

        // Ensure assignment dict is initialised even on old saves
        if (_hrState.activeInterviewAssignments == null)
            _hrState.activeInterviewAssignments = new Dictionary<int, int>();
    }

    // ── Interview assignment tracking ─────────────────────────────────

    /// <summary>Returns the best available HR team ranked by avgHRSkill * memberCount.</summary>
    public TeamId? GetBestAvailableHRTeam()
    {
        if (_teamSystem == null) return null;

        var teamState = _teamSystem.GetTeamState();
        if (teamState == null || teamState.teams == null) return null;

        _teamIdScratch.Clear();
        foreach (var kvp in teamState.teams)
            _teamIdScratch.Add(kvp.Value.id);

        TeamId? best = null;
        float bestScore = -1f;

        int teamCount = _teamIdScratch.Count;
        for (int t = 0; t < teamCount; t++)
        {
            var teamId = _teamIdScratch[t];
            if (!teamState.teams.TryGetValue(teamId, out var team)) continue;
            if (!team.isActive) continue;
            if (team.teamType != TeamType.HR) continue;
            if (!IsTeamAvailable(teamId)) continue;

            float avgSkill = GetHRSkillAverage(teamId);
            int memberCount = GetHRMemberCount(teamId);
            float score = avgSkill * memberCount;

            if (best == null || score > bestScore)
            {
                bestScore = score;
                best = teamId;
            }
        }
        return best;
    }

    public bool IsTeamAvailable(TeamId teamId)
    {
        if (GetActiveSearchForTeam(teamId) != null) return false;
        if (_hrState.activeInterviewAssignments.ContainsKey(teamId.Value)) return false;
        return true;
    }

    public void RegisterInterviewAssignment(TeamId teamId, int candidateId)
    {
        _hrState.activeInterviewAssignments[teamId.Value] = candidateId;
    }

    public void ReleaseInterviewAssignment(int candidateId)
    {
        // Find and remove the team that was interviewing this candidate
        _assignmentKeyScratch.Clear();
        foreach (var kvp in _hrState.activeInterviewAssignments)
            _assignmentKeyScratch.Add(kvp.Key);

        int keyCount = _assignmentKeyScratch.Count;
        for (int k = 0; k < keyCount; k++)
        {
            int key = _assignmentKeyScratch[k];
            if (_hrState.activeInterviewAssignments.TryGetValue(key, out int val) && val == candidateId)
            {
                _hrState.activeInterviewAssignments.Remove(key);
                break;
            }
        }
    }

    public HRTeamStatus GetTeamStatus(TeamId teamId)
    {
        if (_hrState.activeInterviewAssignments.ContainsKey(teamId.Value)) return HRTeamStatus.Interviewing;
        if (GetActiveSearchForTeam(teamId) != null) return HRTeamStatus.Searching;
        return HRTeamStatus.Idle;
    }

    public int? GetInterviewingCandidateId(TeamId teamId)
    {
        if (_hrState.activeInterviewAssignments.TryGetValue(teamId.Value, out int candidateId))
            return candidateId;
        return null;
    }

    // ── Validation ────────────────────────────────────────────────────

    public bool CanStartSearch(TeamId teamId)
    {
        var team = _teamSystem?.GetTeam(teamId);
        if (team == null || !team.isActive) return false;
        if (GetHRMemberCount(teamId) == 0) return false;
        if (GetActiveSearchForTeam(teamId) != null) return false;
        return true;
    }

    // ── Live computation ──────────────────────────────────────────────

    public float ComputeSuccessChance(TeamId teamId)
    {
        int avgSkill = GetHRSkillAverage(teamId);
        float chance = HRSearchConfig.BaseSuccessChance + avgSkill * HRSearchConfig.SkillSuccessScaleFactor;
        if (chance < HRSearchConfig.BaseSuccessChance) chance = HRSearchConfig.BaseSuccessChance;
        if (chance > HRSearchConfig.MaxSuccessChance)  chance = HRSearchConfig.MaxSuccessChance;
        return chance;
    }

    public int ComputeDurationTicks(TeamId teamId)
    {
        float effectiveCapacity = GetHREffectiveCapacity(teamId);
        if (effectiveCapacity > HRSearchConfig.MaxTeamSizeForSpeedBonus)
            effectiveCapacity = HRSearchConfig.MaxTeamSizeForSpeedBonus;
        float speedFactor = 1.0f - effectiveCapacity * HRSearchConfig.TeamSizeSpeedBonusPerMember;
        float days = HRSearchConfig.BaseDurationDays * speedFactor;
        if (days < HRSearchConfig.MinDurationDays) days = HRSearchConfig.MinDurationDays;
        if (days > HRSearchConfig.BaseDurationDays) days = HRSearchConfig.BaseDurationDays;
        return (int)days * TimeState.TicksPerDay;
    }

    public float GetHREffectiveCapacity(TeamId teamId)
    {
        var team = _teamSystem?.GetTeam(teamId);
        if (team == null) return 0f;
        float capacity = 0f;
        int memberCount = team.members.Count;
        for (int i = 0; i < memberCount; i++)
        {
            var empId = team.members[i];
            var emp = _employeeState.employees.TryGetValue(empId, out var e) ? e : null;
            if (emp == null || !emp.isActive) continue;
            if (emp.role != RoleId.HrSpecialist) continue;
            capacity += emp.EffectiveOutput;
        }
        return capacity;
    }

    public int ComputeSearchCost(int minCA, int minPAStars, int desiredSkillCount = 0, int searchCount = 1)
    {
        const int CostPerCAPoint  = 25;
        const int CostPerPAStar   = 500;
        int baseCost = HRSearchConfig.BaseSearchCost;
        baseCost += minCA * CostPerCAPoint;
        baseCost += minPAStars * CostPerPAStar;
        baseCost *= searchCount;
        return baseCost;
    }

    // ── Queries ───────────────────────────────────────────────────────

    public IReadOnlyList<ActiveHRSearch> GetActiveSearches() => _hrState.activeSearches;

    public ActiveHRSearch GetActiveSearchForTeam(TeamId teamId)
    {
        int count = _hrState.activeSearches.Count;
        for (int i = 0; i < count; i++)
        {
            if (_hrState.activeSearches[i].assignedTeamId == teamId)
                return _hrState.activeSearches[i];
        }
        return null;
    }

    public int GetHRSkillAverage(TeamId teamId)
    {
        var team = _teamSystem?.GetTeam(teamId);
        if (team == null) return 0;
        int total = 0;
        int count = 0;
        int memberCount = team.members.Count;
        for (int i = 0; i < memberCount; i++)
        {
            var empId = team.members[i];
            var emp = _employeeState.employees.TryGetValue(empId, out var e) ? e : null;
            if (emp == null || !emp.isActive) continue;
            if (emp.role != RoleId.HrSpecialist) continue;
            total += emp.Stats.GetSkill(SkillId.HrRecruitment);
            count++;
        }
        return count > 0 ? total / count : 0;
    }

    public int GetNegotiationSkillAverage(TeamId teamId)
    {
        var team = _teamSystem?.GetTeam(teamId);
        if (team == null) return 0;
        int total = 0;
        int count = 0;
        int memberCount = team.members.Count;
        for (int i = 0; i < memberCount; i++)
        {
            var empId = team.members[i];
            var emp = _employeeState.employees.TryGetValue(empId, out var e) ? e : null;
            if (emp == null || !emp.isActive) continue;
            if (emp.role != RoleId.HrSpecialist) continue;
            total += emp.Stats.GetSkill(SkillId.Negotiation);
            count++;
        }
        return count > 0 ? total / count : 0;
    }

    public int GetHRMemberCount(TeamId teamId)
    {
        var team = _teamSystem?.GetTeam(teamId);
        if (team == null) return 0;
        int count = 0;
        int memberCount = team.members.Count;
        for (int i = 0; i < memberCount; i++)
        {
            var empId = team.members[i];
            var emp = _employeeState.employees.TryGetValue(empId, out var e) ? e : null;
            if (emp == null || !emp.isActive) continue;
            if (emp.role == RoleId.HrSpecialist) count++;
        }
        return count;
    }

    /// <summary>Returns the highest HrRecruitment skill value among active HR employees on the given team. Returns 0 if none.</summary>
    public int GetHighestHRSkill(TeamId teamId)
    {
        var team = _teamSystem?.GetTeam(teamId);
        if (team == null) return 0;
        int highest = 0;
        int memberCount = team.members.Count;
        for (int i = 0; i < memberCount; i++)
        {
            var empId = team.members[i];
            var emp = _employeeState.employees.TryGetValue(empId, out var e) ? e : null;
            if (emp == null || !emp.isActive) continue;
            if (emp.role != RoleId.HrSpecialist) continue;
            int hrSkill = emp.Stats.GetSkill(SkillId.HrRecruitment);
            if (hrSkill > highest) highest = hrSkill;
        }
        return highest;
    }

    /// <summary>Returns average HrRecruitment skill across all active HR employees company-wide.
    /// Returns -1 if no HR employees exist (sentinel for ShowAsUnknown).</summary>
    public int GetAllHREmployeesSkillAverage()
    {
        int total = 0;
        int count = 0;
        _employeeIdScratch.Clear();
        foreach (var kvp in _employeeState.employees)
            _employeeIdScratch.Add(kvp.Key);

        int empCount = _employeeIdScratch.Count;
        for (int e = 0; e < empCount; e++)
        {
            if (!_employeeState.employees.TryGetValue(_employeeIdScratch[e], out var emp)) continue;
            if (!emp.isActive) continue;
            if (emp.role != RoleId.HrSpecialist) continue;
            total += emp.Stats.GetSkill(SkillId.HrRecruitment);
            count++;
        }
        return count > 0 ? total / count : -1;
    }

    // ── Commands ──────────────────────────────────────────────────────

    public bool CancelSearch(HRSearchId searchId, FinanceSystem financeSystem)
    {
        for (int i = _hrState.activeSearches.Count - 1; i >= 0; i--)
        {
            var s = _hrState.activeSearches[i];
            if (s.searchId == searchId)
            {
                int refund = s.cost / 2;
                if (refund > 0) financeSystem.AddMoney(refund);
                _hrState.activeSearches.RemoveAt(i);
                _logger.Log($"[HRSystem] Cancelled search {searchId.Value}, refunded ${refund}");
                return true;
            }
        }
        _logger.LogWarning($"[HRSystem] CancelSearch: search {searchId.Value} not found");
        return false;
    }

    public void PreTick(int tick) { }

    public void Tick(int tick)
    {
        HadCompletionThisTick = false;

        int count = _hrState.activeSearches.Count;
        for (int i = count - 1; i >= 0; i--)
        {
            var search = _hrState.activeSearches[i];
            if (tick < search.completionTick) continue;

            float chance = ComputeSuccessChance(search.assignedTeamId);
            int roll = _rng.Range(0, 100);
            bool success = roll < (int)(chance * 100f);

            if (success)
            {
                int deliverCount = search.searchCount > 0 ? search.searchCount : 1;
                var deliveredIds = new int[deliverCount];

                int poolMax = EmployeeSystem.CandidatePoolSize;
                int poolCountBefore = _employeeState.availableCandidates.Count;
                int rejected = 0;

                for (int d = 0; d < deliverCount; d++)
                {
                    if (_employeeState.availableCandidates.Count >= poolMax)
                    {
                        rejected++;
                        continue;
                    }
                    CandidateData candidate = GenerateTargetedCandidate(search);
                    candidate.IsTargeted = true;
                    candidate.IsPendingReview = true;
                    candidate.SourcingTeamId = search.assignedTeamId;
                    candidate.SourceTier = 1; // HR-sourced
                    candidate.CandidateId = _employeeState.nextCandidateId++;
                    _employeeState.availableCandidates.Add(candidate);
                    deliveredIds[d] = candidate.CandidateId;
                    _pendingSearchCompleted.Add(candidate);
                    _logger.Log($"[HRSystem] Search succeeded: {candidate.Name} ({candidate.Role}) CA roll:{roll} needed<{(int)(chance * 100f)}");
                }

                if (rejected > 0)
                    _pendingPoolFull.Add(new PoolFullData { PoolCount = _employeeState.availableCandidates.Count, PoolMax = poolMax, RejectedCount = rejected });

                // Build criteria label for inbox notification
                string roleStr = search.targetRole.ToString();
                string caStr = search.minCA > 0 ? $"CA {search.minCA}+" : "Any CA";
                string paStr = search.minPAStars > 1 ? $"★{search.minPAStars}+" : "Any PA";
                string criteriaLabel = $"{roleStr} | {caStr} | {paStr}";

                // Resolve team name
                string teamName = search.assignedTeamId.Value.ToString();
                var team = _teamSystem?.GetTeam(search.assignedTeamId);
                if (team != null) teamName = team.name;

                _pendingCandidatesReady.Add(new HRCandidatesReadyForReviewEvent(tick, search.assignedTeamId, teamName, deliveredIds, criteriaLabel));

                _hrState.activeSearches.RemoveAt(i);
                _hrState.totalSearchesCompleted++;
                HadCompletionThisTick = true;
            }
            else
            {
                // Retry — re-queue with fresh completionTick, do not remove
                search.retryCount++;
                search.completionTick = tick + ComputeDurationTicks(search.assignedTeamId);
                _pendingSearchRetrying.Add(search.searchId);
                _hrState.totalSearchesFailed++;
                _logger.Log($"[HRSystem] Search failed (retry #{search.retryCount}, rolled {roll}, needed < {(int)(chance * 100f)}) — re-queuing");
            }
        }
    }

    public void PostTick(int tick)
    {
        int sc = _pendingSearchCompleted.Count;
        for (int i = 0; i < sc; i++)
            OnSearchCompleted?.Invoke(_pendingSearchCompleted[i]);
        _pendingSearchCompleted.Clear();

        int cr = _pendingCandidatesReady.Count;
        for (int i = 0; i < cr; i++)
            OnCandidatesReadyForReview?.Invoke(_pendingCandidatesReady[i]);
        _pendingCandidatesReady.Clear();

        int sr = _pendingSearchRetrying.Count;
        for (int i = 0; i < sr; i++)
            OnSearchRetrying?.Invoke(_pendingSearchRetrying[i]);
        _pendingSearchRetrying.Clear();

        int ss = _pendingSearchStarted.Count;
        for (int i = 0; i < ss; i++)
            OnSearchStarted?.Invoke(_pendingSearchStarted[i]);
        _pendingSearchStarted.Clear();

        int ca = _pendingCandidateAccepted.Count;
        for (int i = 0; i < ca; i++)
            OnCandidateAccepted?.Invoke(_pendingCandidateAccepted[i]);
        _pendingCandidateAccepted.Clear();

        int pf = _pendingPoolFull.Count;
        for (int i = 0; i < pf; i++)
        {
            var d = _pendingPoolFull[i];
            OnPoolFull?.Invoke(d.PoolCount, d.PoolMax, d.RejectedCount);
        }
        _pendingPoolFull.Clear();

        int pe = _pendingEvents.Count;
        for (int i = 0; i < pe; i++)
            _pendingEvents[i]?.Invoke();
        _pendingEvents.Clear();
    }

    public void ApplyCommand(ICommand command)
    {
        if (command is StartHRSearchCommand start)
        {
            StartSearch(start);
        }
        else if (command is CancelHRSearchCommand cancel)
        {
            CancelSearch(cancel.SearchId, _financeSystem);
        }
        else if (command is AcceptHRCandidateCommand accept)
        {
            AcceptPendingCandidate(accept.CandidateId);
        }
        else if (command is DeclineHRCandidateCommand decline)
        {
            DeclinePendingCandidate(decline.CandidateId);
        }
    }

    private void AcceptPendingCandidate(int candidateId)
    {
        int count = _employeeState.availableCandidates.Count;
        for (int i = 0; i < count; i++)
        {
            var c = _employeeState.availableCandidates[i];
            if (c.CandidateId == candidateId && c.IsPendingReview)
            {
                c.IsPendingReview = false;
                _employeeState.availableCandidates.RemoveAt(i);
                _logger.Log($"[HRSystem] Accepted pending candidate {candidateId} — hiring directly");
                _pendingCandidateAccepted.Add(c);
                return;
            }
        }
        _logger.LogWarning($"[HRSystem] AcceptPendingCandidate: candidate {candidateId} not found or not pending");
    }

    private void DeclinePendingCandidate(int candidateId)
    {
        int count = _employeeState.availableCandidates.Count;
        for (int i = count - 1; i >= 0; i--)
        {
            var c = _employeeState.availableCandidates[i];
            if (c.CandidateId == candidateId && c.IsPendingReview)
            {
                _employeeState.availableCandidates.RemoveAt(i);
                _logger.Log($"[HRSystem] Declined pending candidate {candidateId} — removed");
                return;
            }
        }
        _logger.LogWarning($"[HRSystem] DeclinePendingCandidate: candidate {candidateId} not found or not pending");
    }

    private void StartSearch(StartHRSearchCommand cmd)
    {
        if (!CanStartSearch(cmd.TeamId))
        {
            _logger.LogWarning($"[HRSystem] Cannot start search for team {cmd.TeamId.Value}: validation failed");
            return;
        }

        int cost = ComputeSearchCost(cmd.MinCA, cmd.MinPAStars);
        if (!_financeSystem.TrySubtractMoney(cost, out string error))
        {
            _logger.LogWarning($"[HRSystem] Insufficient funds for search: {error}");
            return;
        }

        int durationTicks = ComputeDurationTicks(cmd.TeamId);
        int searchCount = cmd.SearchCount > 0 ? cmd.SearchCount : 1;

        var search = new ActiveHRSearch
        {
            searchId      = new HRSearchId(_hrState.nextSearchId++),
            targetRole    = cmd.TargetRole,
            startTick     = cmd.Tick,
            completionTick = cmd.Tick + durationTicks,
            cost          = cost,
            assignedTeamId = cmd.TeamId,
            searchCount   = searchCount,
            retryCount    = 0,
            minCA         = cmd.MinCA,
            maxCA         = cmd.MaxCA,
            minPAStars    = cmd.MinPAStars,
            maxPAStars    = cmd.MaxPAStars,
            desiredSkills = cmd.DesiredSkills
        };

        _hrState.activeSearches.Add(search);

        _pendingSearchStarted.Add(search.searchId);

        float chance = ComputeSuccessChance(cmd.TeamId);
        _logger.Log($"[HRSystem] Started search for {cmd.TargetRole} — cost:${cost}, success:{chance:P0}, duration:{durationTicks / TimeState.TicksPerDay}d, count:{searchCount}, CA:{cmd.MinCA}–{cmd.MaxCA}, PA:★{cmd.MinPAStars}–★{cmd.MaxPAStars}");
    }

    public void Dispose()
    {
        _pendingEvents.Clear();
        _pendingSearchCompleted.Clear();
        _pendingSearchRetrying.Clear();
        _pendingSearchStarted.Clear();
        _pendingCandidatesReady.Clear();
        _pendingCandidateAccepted.Clear();
        OnSearchCompleted = null;
        OnSearchStarted = null;
        OnSearchRetrying = null;
        OnCandidatesReadyForReview = null;
        OnCandidateAccepted = null;
    }

    public void SetAbilitySystem(AbilitySystem abilitySystem)
    {
        _abilitySystem = abilitySystem;
    }

    // ── Candidate generation ──────────────────────────────────────────

    private CandidateData GenerateTargetedCandidate(ActiveHRSearch search)
    {
        float qualityMultiplier = 1.0f;
        qualityMultiplier *= 1.4f; // HR-sourced candidates are higher quality
        if (qualityMultiplier > 2.0f) qualityMultiplier = 2.0f;

        bool hasCAFilter = search.minCA > 0 || search.maxCA > 0;
        bool hasPAFilter = search.minPAStars > 0 || search.maxPAStars > 0;

        int effectiveMaxCA = (hasCAFilter && search.maxCA > 0) ? search.maxCA : 200;
        int effectiveMinCA = hasCAFilter ? search.minCA : 0;

        int paStarMin = (hasPAFilter && search.minPAStars > 0) ? search.minPAStars : 1;
        int paStarMax = (hasPAFilter && search.maxPAStars > 0) ? search.maxPAStars : 5;

        // Fix C: maxAttempts = 1 when fully unconstrained
        bool hasAnyFilter = hasCAFilter || hasPAFilter;
        int maxAttempts = hasAnyFilter ? 20 : 1;

        CandidateData bestCandidate = null;
        int bestScore = -1;

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            var candidate = CandidateData.GenerateCandidate(_rng, qualityMultiplier, search.targetRole);

            int candidateCA      = _abilitySystem != null ? _abilitySystem.ComputeCandidateCA(candidate) : 0;
            int candidatePAStars = AbilityCalculator.PotentialToStars(candidate.Stats.PotentialAbility);

            bool passesCA = !hasCAFilter || (candidateCA >= effectiveMinCA && candidateCA <= effectiveMaxCA);
            bool passesPA = !hasPAFilter || (candidatePAStars >= paStarMin && candidatePAStars <= paStarMax);

            int score = 0;
            if (passesCA) score += 100;
            if (passesPA) score += 100;
            score += candidate.AverageSkill;

            if (bestCandidate == null || score > bestScore)
            {
                bestCandidate = candidate;
                bestScore = score;
            }

            if (passesCA && passesPA)
                break;
        }

        _abilitySystem?.GenerateCandidateAbility(bestCandidate);

        int finalCA     = _abilitySystem != null ? _abilitySystem.ComputeCandidateCA(bestCandidate) : 0;
        int finalPAStars = AbilityCalculator.PotentialToStars(bestCandidate.Stats.PotentialAbility);
        _logger.Log($"[HRSystem] Delivered {bestCandidate.Name} — Ability:{finalCA} Potential:★{finalPAStars} (filter Ability:{effectiveMinCA}–{effectiveMaxCA} Potential:★{paStarMin}–★{paStarMax})");

        return bestCandidate;
    }
}
