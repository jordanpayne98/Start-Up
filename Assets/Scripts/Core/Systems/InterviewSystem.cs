// InterviewSystem Version: Knowledge v2
using System;
using System.Collections.Generic;

public class InterviewSystem : ISystem
{
    public event Action<int, TeamId> OnInterviewStarted;
    // Fires (candidateId, thresholdLevel) where thresholdLevel = 20/40/60/80/100
    public event Action<int, int> OnInterviewThresholdReached;

    // Knowledge gain tuning
    private const float BaseGainPerTick = 2.0f;
    private const int MinDaysToComplete = 3;
    // TeamSize → gain factor: index = min(memberCount-1, 3)
    private static readonly float[] TeamSizeFactors = { 1.0f, 1.3f, 1.5f, 1.6f };
    // Thresholds fired as knowledge crosses them
    private static readonly int[] RevealThresholds = { 20, 40, 60, 80, 100 };

    private const int FollowUpIdleDays = 3;

    private InterviewState _state;
    private EmployeeState _employeeState;
    private FinanceSystem _financeSystem;
    private HRSystem _hrSystem;
    private GameEventBus _eventBus;
    private ILogger _logger;
    private IRng _rng;
    private RoleProfileTable _roleProfileTable;

    // Pre-allocated scratch buffers — no alloc in steady state
    private readonly List<int> _keyScratch = new List<int>();
    private readonly List<int> _completedScratch = new List<int>();

    // Event data buffers — cleared per tick, fired in PostTick
    private readonly List<(int candidateId, TeamId teamId, int tick)> _startedBuffer = new List<(int, TeamId, int)>();
    private readonly List<(int candidateId, int threshold)> _thresholdBuffer = new List<(int, int)>();
    private readonly List<(int candidateId, string name, int tick)> _followUpBuffer = new List<(int, string, int)>();

    public InterviewSystem(InterviewState state, EmployeeState employeeState,
        FinanceSystem financeSystem, GameEventBus eventBus, ILogger logger, IRng rng = null)
    {
        _state = state;
        _employeeState = employeeState;
        _financeSystem = financeSystem;
        _eventBus = eventBus;
        _logger = logger ?? new NullLogger();
        _rng = rng;
    }

    public void SetHRSystem(HRSystem hrSystem)
    {
        _hrSystem = hrSystem;
    }

    public void SetRoleProfileTable(RoleProfileTable roleProfileTable)
    {
        _roleProfileTable = roleProfileTable;
    }

    // ─── Public queries ──────────────────────────────────────────────────────

    public bool IsInterviewInProgress(int candidateId)
    {
        if (!_state.activeInterviews.TryGetValue(candidateId, out var interview)) return false;
        return interview.knowledgeLevel < 100f;
    }

    public bool IsComplete(int candidateId)
    {
        if (!_state.activeInterviews.TryGetValue(candidateId, out var interview)) return false;
        return interview.knowledgeLevel >= 100f;
    }

    // Legacy accessors — kept for backward compatibility
    public bool IsFirstReportReady(int candidateId)
    {
        if (!_state.activeInterviews.TryGetValue(candidateId, out var interview)) return false;
        return interview.knowledgeLevel >= 40f;
    }

    public bool IsFinalReportReady(int candidateId)
    {
        return IsComplete(candidateId);
    }

    public float GetKnowledgeLevel(int candidateId)
    {
        if (!_state.activeInterviews.TryGetValue(candidateId, out var interview)) return 0f;
        return interview.knowledgeLevel;
    }

    public float GetInterviewProgressPercent(int candidateId, int currentTick)
    {
        if (!_state.activeInterviews.TryGetValue(candidateId, out var interview)) return 0f;
        float pct = interview.knowledgeLevel / 100f;
        return pct < 0f ? 0f : pct > 1f ? 1f : pct;
    }

    public TeamId GetAssignedTeamId(int candidateId)
    {
        if (!_state.activeInterviews.TryGetValue(candidateId, out var interview)) return default;
        return interview.assignedTeamId;
    }

    public bool CanStartInterview(int candidateId)
    {
        return CanStartInterview(candidateId, HiringMode.HR);
    }

    public bool CanStartInterview(int candidateId, HiringMode mode)
    {
        CandidateData candidate = FindCandidate(candidateId);
        if (candidate == null) return false;
        if (IsInterviewInProgress(candidateId)) return false;
        if (IsComplete(candidateId)) return false;

        if (_hrSystem == null) return false;
        return _hrSystem.GetBestAvailableHRTeam() != null;
    }

    // Legacy stage mapping: 0=none, 1=in-progress, 2=halfway(knowledge>=40), 3=complete
    public int GetInterviewStage(int candidateId)
    {
        if (!_state.activeInterviews.TryGetValue(candidateId, out var interview)) return 0;
        float k = interview.knowledgeLevel;
        if (k >= 100f) return 3;
        if (k >= 40f)  return 2;
        if (k > 0f)    return 1;
        return 0;
    }

    public bool IsHireable(int candidateId)
    {
        return IsComplete(candidateId) && !IsInterviewInProgress(candidateId);
    }

    public bool GetCandidateHasSentFollowUp(int candidateId)
    {
        CandidateData candidate = FindCandidate(candidateId);
        if (candidate == null) return false;
        return candidate.HasSentFollowUp;
    }

    public int GetCandidateWithdrawalDeadlineTick(int candidateId)
    {
        CandidateData candidate = FindCandidate(candidateId);
        if (candidate == null) return 0;
        return candidate.WithdrawalDeadlineTick;
    }

    // ─── New knowledge query methods ─────────────────────────────────────────

    /// <summary>Returns per-skill revealed values based on knowledge level and noise.
    /// Unrevealed skills return -1. Primary skills (tier==2) revealed at knowledge 40;
    /// all skills revealed at knowledge 40 but with range; final value at knowledge 100.</summary>
    public int[] GetRevealedSkills(int candidateId, int[] trueSkills, int[] tiers)
    {
        if (trueSkills == null) return null;
        int count = trueSkills.Length;
        int[] result = new int[count];
        if (!_state.activeInterviews.TryGetValue(candidateId, out var interview))
        {
            for (int i = 0; i < count; i++) result[i] = -1;
            return result;
        }

        float k = interview.knowledgeLevel;
        for (int i = 0; i < count; i++)
        {
            bool isPrimary = tiers != null && i < tiers.Length && tiers[i] == 2;

            if (k < 40f)
            {
                result[i] = -1;
            }
            else if (k >= 40f && k < 100f)
            {
                // Show range midpoint with wide noise (+/-5), narrow at 60 (+/-2), tight at 80 (+/-1)
                int rangeHalf = k >= 80f ? 1 : k >= 60f ? 2 : 5;
                int noise = interview.GetSkillNoise(i < 9 ? i : 0);
                int displayed = trueSkills[i] + noise;
                if (displayed < 0) displayed = 0;
                if (displayed > 20) displayed = 20;
                result[i] = displayed;
            }
            else // k >= 100
            {
                int noise = interview.GetSkillNoise(i < 9 ? i : 0);
                int displayed = trueSkills[i] + noise;
                if (displayed < 0) displayed = 0;
                if (displayed > 20) displayed = 20;
                result[i] = displayed;
            }
        }
        return result;
    }

    /// <summary>Returns estimated ability stars with noise offset. Returns -1 if knowledge < 60.</summary>
    public int GetAbilityStarEstimate(int candidateId, int trueCA, int[] tiers)
    {
        if (!_state.activeInterviews.TryGetValue(candidateId, out var interview)) return -1;
        if (interview.knowledgeLevel < 60f) return -1;
        int trueStars = AbilityCalculator.AbilityToStars(trueCA);
        int estimated = trueStars + interview.abilityStarNoise;
        if (estimated < 1) estimated = 1;
        if (estimated > 5) estimated = 5;
        return estimated;
    }

    /// <summary>Returns estimated potential stars with noise offset. Returns -1 if knowledge < 80.</summary>
    public int GetPotentialStarEstimate(int candidateId, int truePA)
    {
        if (!_state.activeInterviews.TryGetValue(candidateId, out var interview)) return -1;
        if (interview.knowledgeLevel < 80f) return -1;
        int trueStars = AbilityCalculator.PotentialToStars(truePA);
        int estimated = trueStars + interview.potentialStarNoise;
        if (estimated < 1) estimated = 1;
        if (estimated > 5) estimated = 5;
        return estimated;
    }

    /// <summary>Returns reliability label based on HR lead skill at time of interview start.</summary>
    public string GetReliabilityLabel(int candidateId)
    {
        if (!_state.activeInterviews.TryGetValue(candidateId, out var interview)) return "Unreliable";
        int skill = interview.hrLeadSkill;
        if (skill >= 16) return "High";
        if (skill >= 12) return "Moderate";
        if (skill >= 8)  return "Low";
        return "Unreliable";
    }

    /// <summary>Returns USS badge class based on HR lead skill.</summary>
    public string GetReliabilityClass(int candidateId)
    {
        if (!_state.activeInterviews.TryGetValue(candidateId, out var interview)) return "reliability--unreliable";
        int skill = interview.hrLeadSkill;
        if (skill >= 16) return "reliability--high";
        if (skill >= 12) return "reliability--moderate";
        if (skill >= 8)  return "reliability--low";
        return "reliability--unreliable";
    }

    // ─── Command: start interview ────────────────────────────────────────────

    public bool StartInterview(int candidateId, int currentTick)
    {
        return StartInterview(candidateId, currentTick, HiringMode.HR);
    }

    public bool StartInterview(int candidateId, int currentTick, HiringMode mode)
    {
        CandidateData candidate = FindCandidate(candidateId);
        if (candidate == null)
        {
            _logger.LogWarning($"[InterviewSystem] Cannot start interview: candidate {candidateId} not found");
            return false;
        }

        if (IsInterviewInProgress(candidateId))
        {
            _logger.LogWarning($"[InterviewSystem] Interview already in progress for candidate {candidateId}");
            return false;
        }

        if (IsComplete(candidateId))
        {
            _logger.LogWarning($"[InterviewSystem] Candidate {candidateId} already fully interviewed");
            return false;
        }

        TeamId? bestTeam = _hrSystem?.GetBestAvailableHRTeam();
        if (bestTeam == null)
        {
            _logger.LogWarning($"[InterviewSystem] No available HR team to conduct interview for candidate {candidateId}");
            return false;
        }

        TeamId assignedTeamId = bestTeam.Value;

        // Find HR lead skill (highest hrSkill member on team)
        int hrLeadSkill = 0;
        if (_hrSystem != null)
        {
            hrLeadSkill = _hrSystem.GetHighestHRSkill(assignedTeamId);
        }

        // Compute noise amplitudes from HR lead skill bracket
        int noiseAmplitude = GetSkillNoiseAmplitude(hrLeadSkill);
        int starNoiseAmplitude = GetStarNoiseAmplitude(hrLeadSkill);

        var interview = new ActiveInterview
        {
            candidateId        = candidateId,
            startTick          = currentTick,
            assignedTeamId     = assignedTeamId,
            mode               = mode,
            knowledgeLevel     = 0f,
            lastRevealThreshold = 0,
            hrLeadId           = default,
            hrLeadSkill        = hrLeadSkill,
            completedTick      = 0
        };

        // Compute deterministic skill noise offsets
        if (_rng != null)
        {
            for (int i = 0; i < 9; i++)
            {
                int noise = _rng.Range(-noiseAmplitude, noiseAmplitude + 1);
                interview.SetSkillNoise(i, noise);
            }
            interview.abilityStarNoise   = _rng.Range(-starNoiseAmplitude, starNoiseAmplitude + 1);
            interview.potentialStarNoise = _rng.Range(-starNoiseAmplitude, starNoiseAmplitude + 1);
        }

        _state.activeInterviews[candidateId] = interview;
        _hrSystem?.RegisterInterviewAssignment(assignedTeamId, candidateId);

        _startedBuffer.Add((candidateId, assignedTeamId, currentTick));

        _logger.Log($"[InterviewSystem] Started {mode} interview for candidate {candidateId}" +
            $" assigned to team {assignedTeamId.Value}" +
            $" (hrLead skill:{hrLeadSkill} noiseAmp:{noiseAmplitude})");
        return true;
    }

    public void ClearAll()
    {
        _state.activeInterviews.Clear();
        _logger.Log("[InterviewSystem] Cleared all active interviews (candidate pool refreshed)");
    }

    // ─── ISystem ─────────────────────────────────────────────────────────────

    public void PreTick(int tick) { }

    public void Tick(int tick)
    {
        _completedScratch.Clear();
        _thresholdBuffer.Clear();
        _followUpBuffer.Clear();

        if (_state.activeInterviews.Count > 0)
        {
            _keyScratch.Clear();
            foreach (var kvp in _state.activeInterviews)
                _keyScratch.Add(kvp.Key);

            float maxGainPerTick = ComputeMaxGainPerTick();

            int keyCount = _keyScratch.Count;
            for (int k = 0; k < keyCount; k++)
            {
                int key = _keyScratch[k];
                var interview = _state.activeInterviews[key];
                if (interview.knowledgeLevel >= 100f) continue;

                int memberCount = _hrSystem != null ? _hrSystem.GetHRMemberCount(interview.assignedTeamId) : 1;
                int avgSkill    = _hrSystem != null ? _hrSystem.GetHRSkillAverage(interview.assignedTeamId) : 0;

                int sizeIdx = memberCount - 1;
                if (sizeIdx < 0) sizeIdx = 0;
                if (sizeIdx >= TeamSizeFactors.Length) sizeIdx = TeamSizeFactors.Length - 1;
                float sizeFactor = TeamSizeFactors[sizeIdx];
                float skillFactor = 0.5f + (avgSkill / 20f) * 0.5f;

                float gain = BaseGainPerTick * sizeFactor * skillFactor;
                if (gain > maxGainPerTick) gain = maxGainPerTick;

                interview.knowledgeLevel += gain;
                if (interview.knowledgeLevel > 100f) interview.knowledgeLevel = 100f;

                // Check threshold crossings: 20, 40, 60, 80, 100
                int threshCount = RevealThresholds.Length;
                for (int t = 0; t < threshCount; t++)
                {
                    int threshold = RevealThresholds[t];
                    if (threshold > interview.lastRevealThreshold && interview.knowledgeLevel >= threshold)
                    {
                        _thresholdBuffer.Add((key, threshold));
                        interview.lastRevealThreshold = threshold;

                        // Update candidate confidence and regenerate report at key milestones
                        if (threshold == 40 || threshold == 100)
                        {
                            UpdateCandidateConfidenceAndReport(key, (int)interview.knowledgeLevel);
                        }
                    }
                }

                if (interview.knowledgeLevel >= 100f)
                {
                    interview.completedTick = tick;
                    _completedScratch.Add(key);
                }

                _state.activeInterviews[key] = interview;
            }

            int completedCount = _completedScratch.Count;
            for (int i = 0; i < completedCount; i++)
            {
                int candidateId = _completedScratch[i];
                _state.totalInterviewsCompleted++;
                _hrSystem?.ReleaseInterviewAssignment(candidateId);
                _logger.Log($"[InterviewSystem] Interview complete (knowledge 100) for candidate {candidateId}");
            }
        }

        // Follow-up notification fires after N idle days post-completion
        int followUpIdleDays = FollowUpIdleDays;
        int candidateCount = _employeeState.availableCandidates.Count;
        for (int i = 0; i < candidateCount; i++)
        {
            var candidate = _employeeState.availableCandidates[i];
            if (candidate.HasSentFollowUp) continue;
            if (!IsComplete(candidate.CandidateId)) continue;
            if (IsInterviewInProgress(candidate.CandidateId)) continue;

            if (_state.activeInterviews.TryGetValue(candidate.CandidateId, out var completedIntv) && completedIntv.knowledgeLevel >= 100f)
            {
                if ((tick - completedIntv.completedTick) >= followUpIdleDays * TimeState.TicksPerDay)
                {
                    candidate.HasSentFollowUp = true;
                    candidate.FollowUpSentTick = tick;
                    candidate.WithdrawalDeadlineTick = 0;

                    _followUpBuffer.Add((candidate.CandidateId, candidate.Name, tick));
                    _logger.Log($"[InterviewSystem] Follow-up notification sent for {candidate.Name}");
                }
            }
        }
    }

    public void PostTick(int tick)
    {
        int startedCount = _startedBuffer.Count;
        for (int i = 0; i < startedCount; i++)
        {
            var (candidateId, teamId, t) = _startedBuffer[i];
            OnInterviewStarted?.Invoke(candidateId, teamId);
            _eventBus?.Raise(new InterviewStartedEvent(t, candidateId));
        }
        _startedBuffer.Clear();

        int threshCount = _thresholdBuffer.Count;
        for (int i = 0; i < threshCount; i++)
        {
            var (candidateId, threshold) = _thresholdBuffer[i];
            OnInterviewThresholdReached?.Invoke(candidateId, threshold);

            string candidateName = FindCandidateName(candidateId);
            _eventBus?.Raise(new InterviewThresholdEvent(tick, candidateId, candidateName, threshold));
        }

        int followUpCount = _followUpBuffer.Count;
        for (int i = 0; i < followUpCount; i++)
        {
            var (candidateId, name, t) = _followUpBuffer[i];
            _eventBus?.Raise(new CandidateFollowUpEvent(t, candidateId, name, 0));
        }
    }

    public void ApplyCommand(ICommand command)
    {
        if (command is StartInterviewCommand startInterview)
        {
            StartInterview(startInterview.CandidateId, command.Tick, startInterview.Mode);
        }
    }

    public void Dispose()
    {
        _startedBuffer.Clear();
        _thresholdBuffer.Clear();
        _followUpBuffer.Clear();
        OnInterviewStarted = null;
        OnInterviewThresholdReached = null;
    }

    // ─── Private helpers ─────────────────────────────────────────────────────

    private void UpdateCandidateConfidenceAndReport(int candidateId, int knowledgeLevel)
    {
        int count = _employeeState.availableCandidates.Count;
        for (int i = 0; i < count; i++)
        {
            var candidate = _employeeState.availableCandidates[i];
            if (candidate.CandidateId != candidateId) continue;

            if (candidate.Confidence == null)
                candidate.Confidence = CandidateConfidenceData.FromSource(candidate.Source);

            candidate.Confidence.ApplyInterview(knowledgeLevel);
            candidate.Report = CandidateReportGenerator.Generate(candidate, _roleProfileTable);
            return;
        }
    }

    private float ComputeMaxGainPerTick()
    {
        // Enforce 3-day minimum: max gain = 100 / (3 * TicksPerDay)
        int minTicks = MinDaysToComplete * TimeState.TicksPerDay;
        return minTicks > 0 ? 100f / minTicks : BaseGainPerTick;
    }

    private static int GetSkillNoiseAmplitude(int hrLeadSkill)
    {
        if (hrLeadSkill >= 16) return 0;
        if (hrLeadSkill >= 12) return 1;
        if (hrLeadSkill >= 8)  return 2;
        if (hrLeadSkill >= 4)  return 3;
        return 4;
    }

    private static int GetStarNoiseAmplitude(int hrLeadSkill)
    {
        if (hrLeadSkill >= 14) return 0;
        if (hrLeadSkill >= 7)  return 1;
        return 2;
    }

    private CandidateData FindCandidate(int candidateId)
    {
        int count = _employeeState.availableCandidates.Count;
        for (int i = 0; i < count; i++)
        {
            if (_employeeState.availableCandidates[i].CandidateId == candidateId)
                return _employeeState.availableCandidates[i];
        }
        return null;
    }

    private string FindCandidateName(int candidateId)
    {
        int count = _employeeState.availableCandidates.Count;
        for (int i = 0; i < count; i++)
        {
            if (_employeeState.availableCandidates[i].CandidateId == candidateId)
                return _employeeState.availableCandidates[i].Name;
        }
        return "A candidate";
    }
}
