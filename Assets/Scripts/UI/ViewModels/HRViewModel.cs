using System.Collections.Generic;

public struct CandidateDisplay
{
    public int CandidateId;
    public string Name;
    public string Role;
    public int Age;
    public string SalaryDisplay;
    public int AbilityStars;
    public int PotentialStars;
    public string ExpiryDisplay;
    public bool IsTargeted;
    public string Priority;
    // Interview pipeline state
    public bool InterviewInProgress;
    public bool FirstReportReady;
    public bool FinalReportReady;
    public bool IsHardRejected;
    public bool StatsRevealed;         // true only after FinalReportReady (or IsTargeted)
    public bool CanStartInterview;
    public float InterviewProgressPercent;
    public string InterviewTeamLabel;
    // Recommendation (post-final report)
    public string RecommendationLabel;
    public string SkillTierLabel;
    // Negotiation
    public int PatiencePips;           // 0-4 (kept for layout compatibility)
    public int SalaryDemandRaw;        // exact demand integer (0 = not revealed)
    public string SalaryDemandDisplay;
    public bool HasActiveOffer;
    public bool IsDeclined;
    public string DeclineConditionText;
    public int CounterSalary;
    public bool HasCounter;
    // Manual hiring
    public bool CanMakeOfferManually;  // true when not hard-rejected and no active interview in progress
    public bool IsHRInterviewDone;     // same as FinalReportReady
    public HiringMode SelectedMode;    // persisted per-candidate in ViewModel
    public string HiringModeTag;       // "Manual" / "HR" label for badge
    // Ability/Potential range display (computed from CandidatePotentialEstimate)
    public string CADisplay;           // e.g. "★★★", "★★–★★★★", or "?"
    public string PADisplay;           // e.g. "★★★", "★★–★★★★", or "?"
}

public struct HRTeamStatusDisplay
{
    public TeamId Id;
    public string TeamName;
    public HRTeamStatus Status;
    public string StatusLabel;
    public string DetailLabel;
    public int MemberCount;
    public List<EmployeeRowDisplay> Members;
    public bool CanSearch;            // true when idle and has at least one member
    public HRSearchId? ActiveSearchId; // set when status == Searching
}

public struct HRAvailableMemberDisplay
{
    public EmployeeId EmployeeId;
    public string Name;
    public string Role;
}

public struct HRSearchDisplay
{
    public HRSearchId SearchId;
    public string TargetRole;
    public string CompletionDisplay;
    public string CostDisplay;
    public float ProgressPercent;
}

public class HRViewModel : IViewModel
{
    private readonly List<CandidateDisplay> _candidates = new List<CandidateDisplay>();
    public List<CandidateDisplay> Candidates => _candidates;

    private readonly List<HRSearchDisplay> _activeSearches = new List<HRSearchDisplay>();
    public List<HRSearchDisplay> ActiveSearches => _activeSearches;

    private readonly List<HRTeamStatusDisplay> _hrTeamStatuses = new List<HRTeamStatusDisplay>();
    public List<HRTeamStatusDisplay> HRTeamStatuses => _hrTeamStatuses;

    // Unassigned HR Specialists available to join an HR team
    private readonly List<HRAvailableMemberDisplay> _availableHREmployees = new List<HRAvailableMemberDisplay>();
    public List<HRAvailableMemberDisplay> AvailableHREmployees => _availableHREmployees;

    public bool CanRerollCandidates { get; private set; }
    public string RerollCost { get; private set; }

    // Persists hiring mode selection across Refresh cycles — keyed by candidateId
    private readonly Dictionary<int, HiringMode> _candidateModes = new Dictionary<int, HiringMode>();

    public HRViewModel()
    {
        RerollCost = "$0";
    }

    /// <summary>Set the hiring mode for a candidate and trigger re-bind.</summary>
    public void SetHiringMode(int candidateId, HiringMode mode)
    {
        _candidateModes[candidateId] = mode;
    }

    /// <summary>Get the current hiring mode for a candidate.</summary>
    public HiringMode GetHiringMode(int candidateId)
    {
        if (_candidateModes.TryGetValue(candidateId, out var mode))
            return mode;
        return HiringMode.HR;
    }

    /// <summary>Returns the revealed salary demand for a candidate, or 0 if not yet revealed.</summary>
    public int GetSalaryDemand(int candidateId)
    {
        int count = _candidates.Count;
        for (int i = 0; i < count; i++)
        {
            if (_candidates[i].CandidateId == candidateId)
                return _candidates[i].SalaryDemandRaw;
        }
        return 0;
    }

    public void Refresh(IReadOnlyGameState state)
    {
        UnityEngine.Debug.Log("[DBG][HRViewModel] Refresh START");
        if (state == null) return;

        CanRerollCandidates = state.CanRerollCandidates;
        RerollCost = UIFormatting.FormatMoney(state.CandidateRerollCost);

        bool atHardCap = false;

        // Candidates — include declined so player can see them (greyed out)
        _candidates.Clear();
        var candidates = state.AvailableCandidates;
        int candCount = candidates.Count;
        for (int i = 0; i < candCount; i++) {
            var cand = candidates[i];
            int id = cand.CandidateId;

            bool firstReport = state.IsFirstReportReady(id);
            bool finalReport = state.IsFinalReportReady(id);
            bool inProgress  = state.IsInterviewInProgress(id);
            bool hardReject  = state.IsCandidateHardRejected(id);

            var neg = state.GetNegotiation(id);
            bool hasCounter = false;

            // Salary demand display — use GetInterviewStage() not raw CandidateData.InterviewStage
            int interviewStage = state.GetInterviewStage(id);
            TeamId assignedTeamId = state.GetInterviewingTeamId(id);
            int salaryDemandRaw;
            string salaryDemandStr;
            if (cand.IsTargeted)
            {
                float avgNegSkill = (float)state.GetNegotiationSkillAverage(assignedTeamId);
                salaryDemandRaw = SalaryDemandCalculator.ComputeHRDemand(cand.Salary, avgNegSkill);
                salaryDemandStr = UIFormatting.FormatMoney(salaryDemandRaw) + "/mo";
            }
            else if (interviewStage >= 3)
            {
                // Interviewed path — lower tier demand
                salaryDemandRaw = SalaryDemandCalculator.ComputeInterviewedDemand(cand.Salary);
                salaryDemandStr = UIFormatting.FormatMoney(salaryDemandRaw) + "/mo";
            }
            else
            {
                // Direct hire — demand is shown immediately (highest tier, no interview needed)
                salaryDemandRaw = SalaryDemandCalculator.ComputeDirectDemand(cand.Salary);
                salaryDemandStr = UIFormatting.FormatMoney(salaryDemandRaw) + "/mo";
            }

            // Determine hiring mode for this candidate
            // Default to Manual — player can upgrade to HR if a team is available
            if (!_candidateModes.TryGetValue(id, out HiringMode selectedMode))
            {
                selectedMode = HiringMode.Manual;
                _candidateModes[id] = selectedMode;
            }

            bool canMakeOfferManually = !hardReject && !inProgress && !atHardCap;

            // CA/PA range display
            UnityEngine.Debug.Log($"[DBG][HRViewModel] Candidate {id}: calling GetCandidatePotentialEstimate mode={selectedMode}");
            var estimate = state.GetCandidatePotentialEstimate(id, selectedMode);
            UnityEngine.Debug.Log($"[DBG][HRViewModel] Candidate {id}: estimate ShowAsUnknown={estimate.ShowAsUnknown} AbilityMin={estimate.AbilityMin} AbilityMax={estimate.AbilityMax} PotentialMin={estimate.PotentialStarsMin} PotentialMax={estimate.PotentialStarsMax}");
            string caDisplay = BuildCADisplay(estimate);
            string paDisplay = BuildPADisplay(estimate);
            UnityEngine.Debug.Log($"[DBG][HRViewModel] Candidate {id}: caDisplay='{caDisplay}' paDisplay='{paDisplay}'");

            string hiringModeTag = selectedMode == HiringMode.Manual && !finalReport && !inProgress && !hardReject
                ? "Manual"
                : selectedMode == HiringMode.HR ? "HR"
                : "";

            _candidates.Add(new CandidateDisplay {
                CandidateId             = id,
                Name                    = cand.Name,
                Role                    = UIFormatting.FormatRole(cand.Role),
                Age                     = cand.Age,
                SalaryDisplay           = UIFormatting.FormatMoney(cand.Salary),
                AbilityStars            = AbilityCalculator.AbilityToStars(cand.CurrentAbility),
                PotentialStars          = AbilityCalculator.PotentialToStars(cand.PotentialAbility),
                ExpiryDisplay           = UIFormatting.FormatDaysRemaining(cand.ExpiryTick, state.CurrentTick),
                IsTargeted              = cand.IsTargeted,
                Priority                = "",
                InterviewInProgress     = inProgress,
                FirstReportReady        = firstReport,
                FinalReportReady        = finalReport,
                IsHardRejected          = hardReject,
                StatsRevealed           = finalReport || cand.IsTargeted,
                CanStartInterview       = state.CanStartInterview(id, selectedMode),
                InterviewProgressPercent = state.GetInterviewProgressPercent(id),
                InterviewTeamLabel      = FormatTeamLabel(state, id),
                RecommendationLabel     = state.GetRecommendationLabel(id),
                SkillTierLabel          = cand.SuggestedSkillTier.ToString(),
                PatiencePips            = 0,
                SalaryDemandRaw         = salaryDemandRaw,
                SalaryDemandDisplay     = salaryDemandStr,
                HasActiveOffer          = state.HasActiveNegotiation(id),
                IsDeclined              = false,
                DeclineConditionText    = "",
                CounterSalary           = 0,
                HasCounter              = hasCounter,
                CanMakeOfferManually    = canMakeOfferManually,
                IsHRInterviewDone       = finalReport,
                SelectedMode            = selectedMode,
                HiringModeTag           = hiringModeTag,
                CADisplay               = caDisplay,
                PADisplay               = paDisplay
            });
        }

        // HR team statuses — include per-team member list
        _hrTeamStatuses.Clear();
        var teams = state.ActiveTeams;
        int teamCount = teams.Count;
        for (int i = 0; i < teamCount; i++) {
            var team = teams[i];
            if (state.GetTeamType(team.id) != TeamType.HR) continue;
            var status = state.GetHRTeamStatus(team.id);
            var memberRoles = state.GetTeamMemberRoles(team.id);
            var memberList = new List<EmployeeRowDisplay>(memberRoles.Count);
            int mc = memberRoles.Count;
            for (int m = 0; m < mc; m++) {
                var mr = memberRoles[m];
                memberList.Add(new EmployeeRowDisplay {
                    Id   = mr.EmployeeId,
                    Name = mr.Name,
                    Role = UIFormatting.FormatRole(mr.EmployeeRole),
                    SalaryDisplay = "",
                    Morale = 0,
                    TeamName = team.name
                });
            }

            // Determine CanSearch and ActiveSearchId
            bool hasActiveSearch = state.HasActiveHRSearch(team.id);
            HRSearchId? activeSearchId = null;
            var teamSearches = state.ActiveHRSearches;
            int sc = teamSearches.Count;
            for (int s = 0; s < sc; s++)
            {
                if (teamSearches[s].assignedTeamId.Equals(team.id))
                {
                    activeSearchId = teamSearches[s].searchId;
                    break;
                }
            }

            _hrTeamStatuses.Add(new HRTeamStatusDisplay {
                Id            = team.id,
                TeamName      = team.name,
                Status        = status,
                StatusLabel   = FormatTeamStatus(status),
                DetailLabel   = "",
                MemberCount   = mc,
                Members       = memberList,
                CanSearch     = !hasActiveSearch && mc > 0,
                ActiveSearchId = activeSearchId
            });
        }

        // Employees with HR role not currently in any team
        _availableHREmployees.Clear();
        var employees = state.ActiveEmployees;
        int empCount = employees.Count;
        for (int e = 0; e < empCount; e++) {
            var emp = employees[e];
            if (emp.role != EmployeeRole.HR) continue;
            if (state.GetEmployeeTeam(emp.id).HasValue) continue;
            _availableHREmployees.Add(new HRAvailableMemberDisplay {
                EmployeeId = emp.id,
                Name       = emp.name,
                Role       = UIFormatting.FormatRole(emp.role)
            });
        }

        // Active HR searches
        _activeSearches.Clear();
        var searches = state.ActiveHRSearches;
        int searchCount = searches.Count;
        for (int s = 0; s < searchCount; s++) {
            var search = searches[s];
            int elapsed = state.CurrentTick - search.startTick;
            int total = search.completionTick - search.startTick;
            float progress = total > 0 ? (float)elapsed / total : 0f;

            _activeSearches.Add(new HRSearchDisplay {
                SearchId          = search.searchId,
                TargetRole        = UIFormatting.FormatRole(search.targetRole),
                CompletionDisplay = UIFormatting.FormatDaysRemaining(search.completionTick, state.CurrentTick),
                CostDisplay       = UIFormatting.FormatMoney(search.cost),
                ProgressPercent   = progress
            });
        }
        UnityEngine.Debug.Log($"[DBG][HRViewModel] Refresh END — candidates:{_candidates.Count} teams:{_hrTeamStatuses.Count}");
    }

    private static string FormatTeamLabel(IReadOnlyGameState state, int candidateId)
    {
        var teamId = state.GetInterviewingTeamId(candidateId);
        if (teamId.Value == 0) return "—";
        var teams = state.ActiveTeams;
        int count = teams.Count;
        for (int i = 0; i < count; i++)
        {
            if (teams[i].id.Equals(teamId)) return teams[i].name;
        }
        return "HR Team";
    }

    private static string FormatTeamStatus(HRTeamStatus status)
    {
        switch (status) {
            case HRTeamStatus.Searching:   return "Searching";
            case HRTeamStatus.Interviewing: return "Interviewing";
            default:                       return "Idle";
        }
    }

    private static string BuildCADisplay(CandidatePotentialEstimate est)
    {
        if (est.ShowAsUnknown) return "?";
        int minStars = AbilityCalculator.AbilityToStars(est.AbilityMin);
        int maxStars = AbilityCalculator.AbilityToStars(est.AbilityMax);
        if (minStars == maxStars) return AbilityCalculator.StarsLabel(minStars);
        return AbilityCalculator.StarsLabel(minStars) + "–" + AbilityCalculator.StarsLabel(maxStars);
    }

    private static string BuildPADisplay(CandidatePotentialEstimate est)
    {
        if (est.ShowAsUnknown) return "?";
        if (est.PotentialStarsMin == est.PotentialStarsMax) return AbilityCalculator.StarsLabel(est.PotentialStarsMin);
        return AbilityCalculator.StarsLabel(est.PotentialStarsMin) + "–" + AbilityCalculator.StarsLabel(est.PotentialStarsMax);
    }
}

