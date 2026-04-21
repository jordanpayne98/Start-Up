// InterviewSystem Version: Clean v1
using System;
using System.Collections.Generic;

public class InterviewSystem : ISystem
{
    // Two reveal events replace the old OnInterviewStageCompleted
    public event Action<int> OnInterviewFirstReportReady;
    public event Action<int> OnInterviewFinalReportReady;
    public event Action<int, TeamId> OnInterviewStarted;

    // Duration constants
    public static readonly int MinDurationTicks = TimeState.TicksPerDay * 1;
    public static readonly int MaxDurationTicks = TimeState.TicksPerDay * 3;

    // Follow-up notification fires after 3 idle days post-final-report
    private const int FollowUpIdleDays = 3;

    private InterviewState _state;
    private EmployeeState _employeeState;
    private FinanceSystem _financeSystem;
    private HRSystem _hrSystem;
    private GameEventBus _eventBus;
    private ILogger _logger;
    private IRng _rng;

    // Pre-allocated scratch buffers — cleared each tick, no alloc in steady state
    private readonly List<int> _completedScratch    = new List<int>();
    private readonly List<int> _halfwayFiredScratch = new List<int>();
    private readonly List<int> _keyScratch          = new List<int>();

    // Pre-allocated event data buffers — reused per tick, no lambda closures
    private readonly List<(int candidateId, TeamId teamId, int tick)> _startedBuffer    = new List<(int, TeamId, int)>();
    private readonly List<int>                                         _halfwayBuffer    = new List<int>();
    private readonly List<int>                                         _finalBuffer      = new List<int>();
    private readonly List<(int candidateId, string name, int tick)>    _followUpBuffer   = new List<(int, string, int)>();

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

    /// <summary>Call after HRSystem is constructed to enable auto-assign.</summary>
    public void SetHRSystem(HRSystem hrSystem)
    {
        _hrSystem = hrSystem;
    }

    // ─── Public queries ──────────────────────────────────────────────────────

    public bool IsInterviewInProgress(int candidateId)
    {
        if (!_state.activeInterviews.TryGetValue(candidateId, out var interview)) return false;
        return !interview.isComplete;
    }

    public bool IsFirstReportReady(int candidateId)
    {
        if (!_state.activeInterviews.TryGetValue(candidateId, out var interview)) return false;
        return interview.halfwayFired;
    }

    public bool IsFinalReportReady(int candidateId)
    {
        if (!_state.activeInterviews.TryGetValue(candidateId, out var interview)) return false;
        return interview.isComplete;
    }

    public float GetInterviewProgressPercent(int candidateId, int currentTick)
    {
        if (!_state.activeInterviews.TryGetValue(candidateId, out var interview)) return 0f;
        int total = interview.completionTick - interview.startTick;
        if (total <= 0) return 1f;
        int elapsed = currentTick - interview.startTick;
        float p = (float)elapsed / total;
        return p < 0f ? 0f : p > 1f ? 1f : p;
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
        if (IsFinalReportReady(candidateId)) return false;

        if (mode == HiringMode.Manual)
            return true;

        if (_hrSystem == null) return false;
        return _hrSystem.GetBestAvailableHRTeam() != null;
    }

    // Legacy accessor retained for snapshot compatibility
    public int GetInterviewStage(int candidateId)
    {
        if (IsInterviewInProgress(candidateId)) return 1;
        if (IsFinalReportReady(candidateId)) return 3;
        if (IsFirstReportReady(candidateId)) return 2;
        return 0;
    }

    public bool IsHireable(int candidateId)
    {
        return IsFinalReportReady(candidateId) && !IsInterviewInProgress(candidateId);
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

        if (IsFinalReportReady(candidateId))
        {
            _logger.LogWarning($"[InterviewSystem] Candidate {candidateId} already fully interviewed");
            return false;
        }

        TeamId assignedTeamId = default;
        int durationTicks;

        if (mode == HiringMode.Manual)
        {
            // Manual mode: no HR team required, random 1–3 day duration
            durationTicks = ComputeDuration();
        }
        else
        {
            TeamId? bestTeam = _hrSystem?.GetBestAvailableHRTeam();
            if (bestTeam == null)
            {
                _logger.LogWarning($"[InterviewSystem] No available HR team to conduct interview for candidate {candidateId}");
                return false;
            }
            assignedTeamId = bestTeam.Value;
            durationTicks = ComputeDuration();
            _hrSystem?.RegisterInterviewAssignment(assignedTeamId, candidateId);
        }

        int halfwayTicks = durationTicks / 2;

        var interview = new ActiveInterview
        {
            candidateId    = candidateId,
            startTick      = currentTick,
            completionTick = currentTick + durationTicks,
            halfwayTick    = currentTick + halfwayTicks,
            halfwayFired   = false,
            isComplete     = false,
            assignedTeamId = assignedTeamId,
            mode           = mode
        };

        _state.activeInterviews[candidateId] = interview;

        _startedBuffer.Add((candidateId, assignedTeamId, currentTick));

        _logger.Log($"[InterviewSystem] Started {mode} interview for candidate {candidateId}" +
            (mode == HiringMode.HR ? $" assigned to team {assignedTeamId.Value}" : "") +
            $" (duration: {durationTicks} ticks)");
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
        _halfwayFiredScratch.Clear();
        _halfwayBuffer.Clear();
        _finalBuffer.Clear();
        _followUpBuffer.Clear();

        if (_state.activeInterviews.Count > 0)
        {
            _keyScratch.Clear();
            foreach (var kvp in _state.activeInterviews)
                _keyScratch.Add(kvp.Key);

            int keyCount = _keyScratch.Count;
            for (int k = 0; k < keyCount; k++)
            {
                int key = _keyScratch[k];
                var interview = _state.activeInterviews[key];
                if (interview.isComplete) continue;

                if (!interview.halfwayFired && tick >= interview.halfwayTick)
                    _halfwayFiredScratch.Add(key);

                if (tick >= interview.completionTick)
                    _completedScratch.Add(key);
            }

            // Apply halfway updates
            int halfwayCount = _halfwayFiredScratch.Count;
            for (int i = 0; i < halfwayCount; i++)
            {
                int candidateId = _halfwayFiredScratch[i];
                var interview = _state.activeInterviews[candidateId];
                interview.halfwayFired = true;
                _state.activeInterviews[candidateId] = interview;

                _halfwayBuffer.Add(candidateId);
                _logger.Log($"[InterviewSystem] First report ready for candidate {candidateId}");
            }

            int completedCount = _completedScratch.Count;
            for (int i = 0; i < completedCount; i++)
            {
                int candidateId = _completedScratch[i];
                var interview = _state.activeInterviews[candidateId];
                interview.isComplete = true;
                interview.halfwayFired = true;
                _state.activeInterviews[candidateId] = interview;

                _state.totalInterviewsCompleted++;
                _hrSystem?.ReleaseInterviewAssignment(candidateId);

                _finalBuffer.Add(candidateId);
                _logger.Log($"[InterviewSystem] Final report ready for candidate {candidateId}");
            }
        }

        // Follow-up notification fires after N idle days post-final-report
        int followUpIdleDays = FollowUpIdleDays;
        int candidateCount = _employeeState.availableCandidates.Count;
        for (int i = 0; i < candidateCount; i++)
        {
            var candidate = _employeeState.availableCandidates[i];
            if (candidate.HasSentFollowUp) continue;
            if (!IsFinalReportReady(candidate.CandidateId)) continue;
            if (IsInterviewInProgress(candidate.CandidateId)) continue;

            if (_state.activeInterviews.TryGetValue(candidate.CandidateId, out var completedIntv) && completedIntv.isComplete)
            {
                if ((tick - completedIntv.completionTick) >= followUpIdleDays * TimeState.TicksPerDay)
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

        int halfwayCount = _halfwayBuffer.Count;
        for (int i = 0; i < halfwayCount; i++)
            OnInterviewFirstReportReady?.Invoke(_halfwayBuffer[i]);

        int finalCount = _finalBuffer.Count;
        for (int i = 0; i < finalCount; i++)
            OnInterviewFinalReportReady?.Invoke(_finalBuffer[i]);

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
        _halfwayBuffer.Clear();
        _finalBuffer.Clear();
        _followUpBuffer.Clear();
        OnInterviewFirstReportReady = null;
        OnInterviewFinalReportReady = null;
        OnInterviewStarted = null;
    }

    // ─── Private helpers ─────────────────────────────────────────────────────

    private int ComputeDuration()
    {
        if (_rng != null) return _rng.Range(MinDurationTicks, MaxDurationTicks + 1);
        return MinDurationTicks + (MaxDurationTicks - MinDurationTicks) / 2;
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
}

