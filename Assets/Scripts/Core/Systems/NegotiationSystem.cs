// NegotiationSystem Version: Clean v1
using System.Collections.Generic;

public enum NegotiationResult
{
    Accepted,
    Rejected
}

public class NegotiationSystem : ISystem
{
    public event System.Action<int, int> OnOfferAccepted;
    public event System.Action<int> OnOfferRejected;

    private NegotiationState _state;
    private EmployeeState _employeeState;
    private InterviewSystem _interviewSystem;
    private HRSystem _hrSystem;
    private ILogger _logger;

    private struct PendingEvent
    {
        public bool accepted;
        public int candidateId;
        public int salary;
    }

    private List<PendingEvent> _pendingEvents;

    public NegotiationSystem(NegotiationState state, EmployeeState employeeState,
        InterviewSystem interviewSystem, ILogger logger)
    {
        _state = state;
        _employeeState = employeeState;
        _interviewSystem = interviewSystem;
        _logger = logger ?? new NullLogger();
        _pendingEvents = new List<PendingEvent>();
    }

    public void SetHRSystem(HRSystem hrSystem)
    {
        _hrSystem = hrSystem;
    }

    // ─── Read-model accessors ─────────────────────────────────────────────────

    public bool HasActiveNegotiation(int candidateId)
    {
        if (!_state.activeNegotiations.TryGetValue(candidateId, out var neg)) return false;
        return neg.status == NegotiationStatus.Pending;
    }

    public ActiveNegotiation? GetNegotiation(int candidateId)
    {
        if (_state.activeNegotiations.TryGetValue(candidateId, out var neg))
            return neg;
        return null;
    }

    public int GetEffectiveSalaryDemand(int candidateId)
    {
        CandidateData candidate = FindCandidate(candidateId);
        if (candidate == null) return 0;
        float avgNegSkill = GetAvgNegotiationSkill(candidateId);
        return SalaryDemandCalculator.GetEffectiveDemand(candidate, avgNegSkill);
    }

    public bool IsSalaryRevealed(int candidateId)
    {
        CandidateData candidate = FindCandidate(candidateId);
        return SalaryDemandCalculator.IsSalaryRevealed(candidate);
    }

    // ─── Commands ────────────────────────────────────────────────────────────

    public NegotiationResult MakeOffer(int candidateId, int offeredSalary, int currentTick = 0, HiringMode mode = HiringMode.HR)
    {
        CandidateData candidate = FindCandidate(candidateId);
        if (candidate == null)
        {
            _logger.LogWarning($"[NegotiationSystem] Candidate {candidateId} not found");
            return NegotiationResult.Rejected;
        }

        float avgNegSkill = GetAvgNegotiationSkill(candidateId);
        int resolvedStage = _interviewSystem != null ? _interviewSystem.GetInterviewStage(candidateId) : candidate.InterviewStage;
        int demand = SalaryDemandCalculator.GetEffectiveDemand(candidate, avgNegSkill, resolvedStage);

        if (offeredSalary >= demand)
        {
            var negotiation = CreateNegotiation(candidateId, offeredSalary, mode);
            negotiation.status = NegotiationStatus.Accepted;
            _state.activeNegotiations[candidateId] = negotiation;

            _pendingEvents.Add(new PendingEvent { accepted = true, candidateId = candidateId, salary = offeredSalary });
            _logger.Log($"[NegotiationSystem] Offer accepted for candidate {candidateId}: offered ${offeredSalary} >= demand ${demand}");
            return NegotiationResult.Accepted;
        }
        else
        {
            var negotiation = CreateNegotiation(candidateId, offeredSalary, mode);
            negotiation.status = NegotiationStatus.Rejected;
            _state.activeNegotiations[candidateId] = negotiation;

            _pendingEvents.Add(new PendingEvent { accepted = false, candidateId = candidateId, salary = 0 });
            _logger.Log($"[NegotiationSystem] Offer rejected for candidate {candidateId}: offered ${offeredSalary} < demand ${demand}");
            return NegotiationResult.Rejected;
        }
    }

    public void ClearAll()
    {
        _state.activeNegotiations.Clear();
        _logger.Log("[NegotiationSystem] Cleared all active negotiations (candidate pool refreshed)");
    }

    public void PreTick(int tick) { }

    public void Tick(int tick) { }

    public void PostTick(int tick)
    {
        int count = _pendingEvents.Count;
        for (int i = 0; i < count; i++)
        {
            PendingEvent ev = _pendingEvents[i];
            if (ev.accepted)
                OnOfferAccepted?.Invoke(ev.candidateId, ev.salary);
            else
                OnOfferRejected?.Invoke(ev.candidateId);
        }
        _pendingEvents.Clear();
    }

    public void ApplyCommand(ICommand command)
    {
        if (command is MakeOfferCommand makeOffer)
        {
            MakeOffer(makeOffer.CandidateId, makeOffer.OfferedSalary, command.Tick, makeOffer.Mode);
        }
    }

    // ─── Private helpers ─────────────────────────────────────────────────────

    private ActiveNegotiation CreateNegotiation(int candidateId, int offeredSalary, HiringMode mode = HiringMode.HR)
    {
        return new ActiveNegotiation
        {
            candidateId   = candidateId,
            offeredSalary = offeredSalary,
            status        = NegotiationStatus.Pending,
            mode          = mode
        };
    }

    private float GetAvgNegotiationSkill(int candidateId)
    {
        if (_hrSystem == null || _interviewSystem == null) return 0f;
        var teamId = _interviewSystem.GetAssignedTeamId(candidateId);
        return (float)_hrSystem.GetNegotiationSkillAverage(teamId);
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

    public void Dispose()
    {
        _pendingEvents.Clear();
        OnOfferAccepted = null;
        OnOfferRejected = null;
    }
}