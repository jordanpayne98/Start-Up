// NegotiationSystem Version: v3 — 3-way outcome with patience and counter-offers
using System.Collections.Generic;

public enum NegotiationResult
{
    Accepted,
    CounterOffered,
    Rejected
}

public class NegotiationSystem : ISystem
{
    public event System.Action<int, int, EmploymentOffer> OnOfferAccepted;
    public event System.Action<int> OnOfferRejected;

    private NegotiationState _state;
    private EmployeeState _employeeState;
    private InterviewSystem _interviewSystem;
    private GameEventBus _eventBus;
    private IRng _rng;
    private RoleTierTable _roleTierTable;
    private ILogger _logger;

    private enum PendingEventType
    {
        Accepted,
        Rejected,
        CounterOffered,
        CandidateLostPatience,
        CounterOfferExpired,
        EmployeeFrustrated,
        EmployeeCooldownExpired,
        PatienceLow
    }

    private struct PendingEvent
    {
        public PendingEventType type;
        public int candidateId;
        public int salary;
        public EmploymentOffer offer;
        public CounterOffer counter;
        public string candidateName;
        public int remainingPatience;
        public bool isEmployee;
        public int cooldownExpiryTick;
    }

    private List<PendingEvent> _pendingEvents;
    private List<int> _candidateKeyBuffer;
    private List<int> _employeeIdxBuffer;

    public NegotiationSystem(NegotiationState state, EmployeeState employeeState,
        InterviewSystem interviewSystem, GameEventBus eventBus, IRng rng, RoleTierTable roleTierTable, ILogger logger)
    {
        _state = state;
        _employeeState = employeeState;
        _interviewSystem = interviewSystem;
        _eventBus = eventBus;
        _rng = rng;
        _roleTierTable = roleTierTable;
        _logger = logger ?? new NullLogger();
        _pendingEvents = new List<PendingEvent>();
        _candidateKeyBuffer = new List<int>();
        _employeeIdxBuffer = new List<int>();

        if (_state.employeeNegotiations == null)
            _state.employeeNegotiations = new List<EmployeeNegotiation>();
    }

    // ─── Read-model accessors ──────────────────────────────────────────────────

    public bool HasActiveNegotiation(int candidateId)
    {
        if (!_state.activeNegotiations.TryGetValue(candidateId, out var neg)) return false;
        return neg.status == NegotiationStatus.Pending || neg.status == NegotiationStatus.CounterOffered;
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
        return SalaryDemandCalculator.ComputeDemand(candidate);
    }

    public bool IsSalaryRevealed(int candidateId)
    {
        CandidateData candidate = FindCandidate(candidateId);
        return SalaryDemandCalculator.IsSalaryRevealed(candidate);
    }

    public bool IsOfferOnCooldown(int candidateId, int currentTick)
    {
        CandidateData candidate = FindCandidate(candidateId);
        if (candidate == null || candidate.LastOfferTick == 0) return false;
        int cooldownDays = candidateId % 4 + 2;
        int cooldownTicks = cooldownDays * TimeState.TicksPerDay;
        return (currentTick - candidate.LastOfferTick) < cooldownTicks;
    }

    public bool IsEmployeeOnCooldown(EmployeeId id, int currentTick)
    {
        int idx = FindEmployeeNegotiationIndex(id);
        if (idx < 0) return false;
        var neg = _state.employeeNegotiations[idx];
        return neg.cooldownExpiryTick > 0 && currentTick < neg.cooldownExpiryTick;
    }

    public EmployeeNegotiation? GetEmployeeNegotiation(EmployeeId id)
    {
        int idx = FindEmployeeNegotiationIndex(id);
        if (idx < 0) return null;
        return _state.employeeNegotiations[idx];
    }

    // ─── Candidate offer flow ──────────────────────────────────────────────────

    public NegotiationResult MakeOffer(OfferPackage offer, int currentTick)
    {
        int candidateId = offer.CandidateId;
        CandidateData candidate = FindCandidate(candidateId);
        if (candidate == null)
        {
            _logger.LogWarning($"[NegotiationSystem] Candidate {candidateId} not found");
            return NegotiationResult.Rejected;
        }

        candidate.IsTargeted = true;

        int baseDemand = SalaryDemandCalculator.ComputeDemand(candidate);
        RoleSuitability suitability = RoleSuitabilityCalculator.GetSuitabilityForRole(candidate.Skills, _roleTierTable, offer.OfferedRole);
        int demand = SalaryModifierCalculator.ComputeOfferSalary(baseDemand, offer.EmploymentType, offer.Length,
            candidate.Preferences, candidate.Role, offer.OfferedRole, suitability);

        _state.activeNegotiations.TryGetValue(candidateId, out var existing);
        bool isFirstOffer = existing.roundCount == 0;

        if (!isFirstOffer)
        {
            if (IsSameOffer(existing.lastOffer, offer))
            {
                if (!OfferEvaluator.EvaluateSameTermsGamble(candidate.HiddenAttributes.Adaptability, _rng))
                {
                    existing.currentPatience = 0;
                    existing.status = NegotiationStatus.PatienceExhausted;
                    _state.activeNegotiations[candidateId] = existing;
                    _pendingEvents.Add(new PendingEvent
                    {
                        type = PendingEventType.CandidateLostPatience,
                        candidateId = candidateId,
                        candidateName = candidate.Name
                    });
                    _logger.Log($"[NegotiationSystem] Same-terms gamble failed for {candidateId} — patience exhausted");
                    return NegotiationResult.Rejected;
                }
            }
        }

        float satisfaction = OfferEvaluator.ComputeSatisfaction(offer, demand, candidate.Role,
            candidate.Preferences, suitability);
        OfferOutcome outcome = OfferEvaluator.Evaluate(satisfaction);

        int maxPatience = isFirstOffer
            ? OfferEvaluator.ComputeMaxPatience(candidate.HiddenAttributes)
            : existing.maxPatience;
        int currentPatience = isFirstOffer ? maxPatience : existing.currentPatience;
        int roundCount = existing.roundCount + 1;

        candidate.LastOfferTick = currentTick;

        if (outcome == OfferOutcome.Accept)
        {
            var negotiation = new ActiveNegotiation
            {
                candidateId    = candidateId,
                offeredSalary  = offer.OfferedSalary,
                status         = NegotiationStatus.Accepted,
                mode           = offer.Mode,
                lastOffer      = offer,
                maxPatience    = maxPatience,
                currentPatience = currentPatience,
                roundCount     = roundCount
            };
            _state.activeNegotiations[candidateId] = negotiation;

            var employmentOffer = new EmploymentOffer
            {
                Type          = offer.EmploymentType,
                Length        = offer.Length,
                MonthlySalary = offer.OfferedSalary,
                Role          = offer.OfferedRole
            };
            _pendingEvents.Add(new PendingEvent
            {
                type = PendingEventType.Accepted,
                candidateId = candidateId,
                salary = offer.OfferedSalary,
                offer = employmentOffer
            });
            _logger.Log($"[NegotiationSystem] Offer accepted for {candidateId}: satisfaction={satisfaction:F1}");
            return NegotiationResult.Accepted;
        }

        if (outcome == OfferOutcome.Counter)
        {
            int patienceDrain = satisfaction >= 40f ? 1 : 2;
            currentPatience -= patienceDrain;

            if (currentPatience <= 0)
            {
                var exhausted = new ActiveNegotiation
                {
                    candidateId    = candidateId,
                    offeredSalary  = offer.OfferedSalary,
                    status         = NegotiationStatus.PatienceExhausted,
                    mode           = offer.Mode,
                    lastOffer      = offer,
                    maxPatience    = maxPatience,
                    currentPatience = 0,
                    roundCount     = roundCount
                };
                _state.activeNegotiations[candidateId] = exhausted;
                _pendingEvents.Add(new PendingEvent
                {
                    type = PendingEventType.CandidateLostPatience,
                    candidateId = candidateId,
                    candidateName = candidate.Name
                });
                _logger.Log($"[NegotiationSystem] Patience exhausted for {candidateId} during counter");
                return NegotiationResult.Rejected;
            }

            CounterOffer counter = OfferEvaluator.GenerateCounter(offer, candidate, demand, currentTick);
            var negotiation = new ActiveNegotiation
            {
                candidateId    = candidateId,
                offeredSalary  = offer.OfferedSalary,
                status         = NegotiationStatus.CounterOffered,
                mode           = offer.Mode,
                lastOffer      = offer,
                counterOffer   = counter,
                hasCounterOffer = true,
                maxPatience    = maxPatience,
                currentPatience = currentPatience,
                roundCount     = roundCount
            };
            _state.activeNegotiations[candidateId] = negotiation;
            _pendingEvents.Add(new PendingEvent
            {
                type = PendingEventType.CounterOffered,
                candidateId = candidateId,
                counter = counter,
                candidateName = candidate.Name,
                remainingPatience = currentPatience
            });
            if (currentPatience == 1)
            {
                _pendingEvents.Add(new PendingEvent
                {
                    type = PendingEventType.PatienceLow,
                    candidateId = candidateId,
                    candidateName = candidate.Name,
                    remainingPatience = currentPatience,
                    isEmployee = false
                });
            }
            _logger.Log($"[NegotiationSystem] Counter-offer for {candidateId}: satisfaction={satisfaction:F1}, patience={currentPatience}/{maxPatience}");
            return NegotiationResult.CounterOffered;
        }

        // Reject
        {
            currentPatience -= 2;

            if (currentPatience <= 0)
            {
                var exhausted = new ActiveNegotiation
                {
                    candidateId    = candidateId,
                    offeredSalary  = offer.OfferedSalary,
                    status         = NegotiationStatus.PatienceExhausted,
                    mode           = offer.Mode,
                    lastOffer      = offer,
                    maxPatience    = maxPatience,
                    currentPatience = 0,
                    roundCount     = roundCount
                };
                _state.activeNegotiations[candidateId] = exhausted;
                _pendingEvents.Add(new PendingEvent
                {
                    type = PendingEventType.CandidateLostPatience,
                    candidateId = candidateId,
                    candidateName = candidate.Name
                });
                _logger.Log($"[NegotiationSystem] Patience exhausted on reject for {candidateId}");
                return NegotiationResult.Rejected;
            }

            var negotiation = new ActiveNegotiation
            {
                candidateId    = candidateId,
                offeredSalary  = offer.OfferedSalary,
                status         = NegotiationStatus.Rejected,
                mode           = offer.Mode,
                lastOffer      = offer,
                maxPatience    = maxPatience,
                currentPatience = currentPatience,
                roundCount     = roundCount
            };
            _state.activeNegotiations[candidateId] = negotiation;
            _pendingEvents.Add(new PendingEvent
            {
                type = PendingEventType.Rejected,
                candidateId = candidateId,
                candidateName = candidate.Name
            });
            _logger.Log($"[NegotiationSystem] Offer rejected for {candidateId}: satisfaction={satisfaction:F1}, patience={currentPatience}/{maxPatience}");
            return NegotiationResult.Rejected;
        }
    }

    // ─── Counter-offer response commands ──────────────────────────────────────

    private void AcceptCounterOffer(int candidateId, int currentTick)
    {
        if (!_state.activeNegotiations.TryGetValue(candidateId, out var neg) || !neg.hasCounterOffer)
        {
            _logger.LogWarning($"[NegotiationSystem] AcceptCounterOffer: no counter for {candidateId}");
            return;
        }

        neg.status = NegotiationStatus.Accepted;
        _state.activeNegotiations[candidateId] = neg;

        var employmentOffer = new EmploymentOffer
        {
            Type          = neg.counterOffer.CounterType,
            Length        = neg.counterOffer.CounterLength,
            MonthlySalary = neg.counterOffer.CounterSalary,
            Role          = neg.counterOffer.CounterRole
        };
        _pendingEvents.Add(new PendingEvent
        {
            type = PendingEventType.Accepted,
            candidateId = candidateId,
            salary = neg.counterOffer.CounterSalary,
            offer = employmentOffer
        });
        _logger.Log($"[NegotiationSystem] Player accepted counter for {candidateId} at ${neg.counterOffer.CounterSalary}");
    }

    private void RejectCounterOffer(int candidateId, int currentTick)
    {
        if (!_state.activeNegotiations.TryGetValue(candidateId, out var neg))
        {
            _logger.LogWarning($"[NegotiationSystem] RejectCounterOffer: no negotiation for {candidateId}");
            return;
        }

        CandidateData candidate = FindCandidate(candidateId);
        string name = candidate != null ? candidate.Name : string.Empty;

        neg.currentPatience -= 1;
        neg.hasCounterOffer = false;

        if (neg.currentPatience <= 0)
        {
            neg.status = NegotiationStatus.PatienceExhausted;
            neg.currentPatience = 0;
            _state.activeNegotiations[candidateId] = neg;
            _pendingEvents.Add(new PendingEvent
            {
                type = PendingEventType.CandidateLostPatience,
                candidateId = candidateId,
                candidateName = name
            });
            _logger.Log($"[NegotiationSystem] Patience exhausted after player rejected counter for {candidateId}");
            return;
        }

        if (candidate != null)
            candidate.LastOfferTick = currentTick;

        neg.status = NegotiationStatus.Pending;
        _state.activeNegotiations[candidateId] = neg;
        _logger.Log($"[NegotiationSystem] Player rejected counter for {candidateId}, patience={neg.currentPatience}/{neg.maxPatience}");
    }

    // ─── Employee renewal negotiation ─────────────────────────────────────────

    public NegotiationResult MakeRenewalOffer(OfferPackage offer, EmployeeId employeeId, int currentTick)
    {
        Employee emp = FindEmployee(employeeId);
        if (emp == null)
        {
            _logger.LogWarning($"[NegotiationSystem] Employee {employeeId.Value} not found for renewal");
            return NegotiationResult.Rejected;
        }
        if (emp.isFounder) return NegotiationResult.Accepted;

        int idx = FindEmployeeNegotiationIndex(employeeId);
        EmployeeNegotiation existing = idx >= 0 ? _state.employeeNegotiations[idx] : default;
        bool isFirstOffer = existing.roundCount == 0;

        int baseDemand = emp.renewalDemand > 0 ? emp.renewalDemand : emp.salary;
        RoleSuitability suitability = RoleSuitabilityCalculator.GetSuitabilityForRole(emp.skills, _roleTierTable, offer.OfferedRole);
        int demand = SalaryModifierCalculator.ComputeRenewalDemand(emp, baseDemand, offer.EmploymentType,
            offer.Length, emp.StrikeCount, emp.preferredRole, offer.OfferedRole, suitability);

        if (!isFirstOffer && IsSameOffer(existing.lastOffer, offer))
        {
            if (!OfferEvaluator.EvaluateSameTermsGamble(emp.hiddenAttributes.Adaptability, _rng))
            {
                existing.currentPatience = 0;
                existing.status = NegotiationStatus.PatienceExhausted;
                existing.cooldownExpiryTick = currentTick + 30 * TimeState.TicksPerDay;
                UpdateEmployeeNegotiation(idx, existing);
                _logger.Log($"[NegotiationSystem] Renewal same-terms gamble failed for employee {employeeId.Value}");
                return NegotiationResult.Rejected;
            }
        }

        CandidatePreferences prefs = emp.OriginalPreferences;
        float satisfaction = OfferEvaluator.ComputeSatisfaction(offer, demand, emp.preferredRole, prefs, suitability);
        OfferOutcome outcome = OfferEvaluator.Evaluate(satisfaction);

        int maxPatience = isFirstOffer
            ? OfferEvaluator.ComputeMaxPatience(emp.hiddenAttributes)
            : existing.maxPatience;
        int currentPatience = isFirstOffer ? maxPatience : existing.currentPatience;
        int roundCount = existing.roundCount + 1;

        if (outcome == OfferOutcome.Accept)
        {
            var neg = new EmployeeNegotiation
            {
                employeeId     = employeeId,
                lastOffer      = offer,
                status         = NegotiationStatus.Accepted,
                maxPatience    = maxPatience,
                currentPatience = currentPatience,
                roundCount     = roundCount
            };
            UpdateEmployeeNegotiation(idx, neg);

            var employmentOffer = new EmploymentOffer
            {
                Type          = offer.EmploymentType,
                Length        = offer.Length,
                MonthlySalary = offer.OfferedSalary,
                Role          = offer.OfferedRole
            };
            _pendingEvents.Add(new PendingEvent { type = PendingEventType.Accepted, candidateId = employeeId.Value, salary = offer.OfferedSalary, offer = employmentOffer });
            return NegotiationResult.Accepted;
        }

        if (outcome == OfferOutcome.Counter)
        {
            int patienceDrain = satisfaction >= 40f ? 1 : 2;
            currentPatience -= patienceDrain;

            if (currentPatience <= 0)
            {
                var neg = new EmployeeNegotiation
                {
                    employeeId      = employeeId,
                    lastOffer       = offer,
                    status          = NegotiationStatus.PatienceExhausted,
                    maxPatience     = maxPatience,
                    currentPatience = 0,
                    roundCount      = roundCount,
                    cooldownExpiryTick = currentTick + 30 * TimeState.TicksPerDay
                };
                UpdateEmployeeNegotiation(idx, neg);
                return NegotiationResult.Rejected;
            }

            CandidatePreferences candidatePrefs = emp.OriginalPreferences;
            var counterCandidate = new CandidateData
            {
                CandidateId  = employeeId.Value,
                Role         = emp.preferredRole,
                Preferences  = candidatePrefs
            };
            CounterOffer counter = OfferEvaluator.GenerateCounter(offer, counterCandidate, demand, currentTick);
            var negState = new EmployeeNegotiation
            {
                employeeId      = employeeId,
                lastOffer       = offer,
                counterOffer    = counter,
                hasCounterOffer = true,
                status          = NegotiationStatus.CounterOffered,
                maxPatience     = maxPatience,
                currentPatience = currentPatience,
                roundCount      = roundCount
            };
            UpdateEmployeeNegotiation(idx, negState);
            return NegotiationResult.CounterOffered;
        }

        // Reject
        currentPatience -= 2;
        if (currentPatience <= 0)
        {
            var neg = new EmployeeNegotiation
            {
                employeeId      = employeeId,
                lastOffer       = offer,
                status          = NegotiationStatus.PatienceExhausted,
                maxPatience     = maxPatience,
                currentPatience = 0,
                roundCount      = roundCount,
                cooldownExpiryTick = currentTick + 30 * TimeState.TicksPerDay
            };
            UpdateEmployeeNegotiation(idx, neg);
            return NegotiationResult.Rejected;
        }

        var rejNeg = new EmployeeNegotiation
        {
            employeeId      = employeeId,
            lastOffer       = offer,
            status          = NegotiationStatus.Rejected,
            maxPatience     = maxPatience,
            currentPatience = currentPatience,
            roundCount      = roundCount
        };
        UpdateEmployeeNegotiation(idx, rejNeg);
        return NegotiationResult.Rejected;
    }

    public void AcceptRenewalCounter(EmployeeId id)
    {
        int idx = FindEmployeeNegotiationIndex(id);
        if (idx < 0) return;
        var neg = _state.employeeNegotiations[idx];
        if (!neg.hasCounterOffer) return;

        var employmentOffer = new EmploymentOffer
        {
            Type          = neg.counterOffer.CounterType,
            Length        = neg.counterOffer.CounterLength,
            MonthlySalary = neg.counterOffer.CounterSalary,
            Role          = neg.counterOffer.CounterRole
        };
        neg.status = NegotiationStatus.Accepted;
        neg.hasCounterOffer = false;
        _state.employeeNegotiations[idx] = neg;
        _pendingEvents.Add(new PendingEvent { type = PendingEventType.Accepted, candidateId = id.Value, salary = neg.counterOffer.CounterSalary, offer = employmentOffer });
    }

    public void RejectRenewalCounter(EmployeeId id, int currentTick)
    {
        int idx = FindEmployeeNegotiationIndex(id);
        if (idx < 0) return;
        var neg = _state.employeeNegotiations[idx];
        neg.currentPatience -= 1;
        neg.hasCounterOffer = false;

        if (neg.currentPatience <= 0)
        {
            neg.status = NegotiationStatus.PatienceExhausted;
            neg.currentPatience = 0;
            neg.cooldownExpiryTick = currentTick + 30 * TimeState.TicksPerDay;
        }
        else
        {
            neg.status = NegotiationStatus.Pending;
        }
        _state.employeeNegotiations[idx] = neg;
    }

    // ─── Utility ──────────────────────────────────────────────────────────────

    public void ClearAll()
    {
        _state.activeNegotiations.Clear();
        _logger.Log("[NegotiationSystem] Cleared all active negotiations (candidate pool refreshed)");
    }

    // ─── ISystem lifecycle ────────────────────────────────────────────────────

    public void PreTick(int tick) { }

    public void Tick(int tick)
    {
        ProcessCounterOfferExpiry(tick);
        ProcessEmployeeNegotiations(tick);
    }

    public void PostTick(int tick)
    {
        int count = _pendingEvents.Count;
        for (int i = 0; i < count; i++)
        {
            PendingEvent ev = _pendingEvents[i];
            switch (ev.type)
            {
                case PendingEventType.Accepted:
                    OnOfferAccepted?.Invoke(ev.candidateId, ev.salary, ev.offer);
                    break;
                case PendingEventType.Rejected:
                    OnOfferRejected?.Invoke(ev.candidateId);
                    break;
                case PendingEventType.CounterOffered:
                    _eventBus?.Raise(new CounterOfferReceivedEvent(tick, ev.candidateId, ev.candidateName, ev.counter, ev.remainingPatience));
                    break;
                case PendingEventType.CandidateLostPatience:
                    _eventBus?.Raise(new CandidateLostPatienceEvent(tick, ev.candidateId, ev.candidateName));
                    break;
                case PendingEventType.CounterOfferExpired:
                    _eventBus?.Raise(new CounterOfferExpiredEvent(tick, ev.candidateId, ev.candidateName));
                    break;
                case PendingEventType.EmployeeFrustrated:
                    _eventBus?.Raise(new EmployeeFrustratedEvent(tick, new EmployeeId(ev.candidateId), ev.candidateName, ev.cooldownExpiryTick));
                    break;
                case PendingEventType.EmployeeCooldownExpired:
                    _eventBus?.Raise(new EmployeeCooldownExpiredEvent(tick, new EmployeeId(ev.candidateId), ev.candidateName));
                    break;
                case PendingEventType.PatienceLow:
                    _eventBus?.Raise(new PatienceLowEvent(tick, ev.candidateId, ev.candidateName, ev.remainingPatience, ev.isEmployee));
                    break;
            }
        }
        _pendingEvents.Clear();
    }

    public void ApplyCommand(ICommand command)
    {
        if (command is MakeOfferCommand makeOffer)
        {
            var pkg = new OfferPackage
            {
                CandidateId    = makeOffer.CandidateId,
                OfferedRole    = makeOffer.OfferedRole,
                EmploymentType = makeOffer.EmploymentType,
                Length         = makeOffer.Length,
                OfferedSalary  = makeOffer.OfferedSalary,
                Mode           = makeOffer.Mode
            };
            MakeOffer(pkg, command.Tick);
            return;
        }
        if (command is AcceptCounterOfferCommand acceptCounter)
        {
            AcceptCounterOffer(acceptCounter.CandidateId, command.Tick);
            return;
        }
        if (command is RejectCounterOfferCommand rejectCounter)
        {
            RejectCounterOffer(rejectCounter.CandidateId, command.Tick);
        }
    }

    public void Dispose()
    {
        _pendingEvents.Clear();
        _candidateKeyBuffer.Clear();
        _employeeIdxBuffer.Clear();
        OnOfferAccepted = null;
        OnOfferRejected = null;
    }

    // ─── Tick helpers ─────────────────────────────────────────────────────────

    private void ProcessCounterOfferExpiry(int tick)
    {
        _candidateKeyBuffer.Clear();
        foreach (var key in _state.activeNegotiations.Keys)
            _candidateKeyBuffer.Add(key);

        int count = _candidateKeyBuffer.Count;
        for (int i = 0; i < count; i++)
        {
            int candidateId = _candidateKeyBuffer[i];
            if (!_state.activeNegotiations.TryGetValue(candidateId, out var neg)) continue;
            if (neg.status != NegotiationStatus.CounterOffered || !neg.hasCounterOffer) continue;
            if (tick < neg.counterOffer.ExpiryTick) continue;

            CandidateData candidate = FindCandidate(candidateId);
            string name = candidate != null ? candidate.Name : string.Empty;

            neg.currentPatience -= 1;
            neg.hasCounterOffer = false;

            if (neg.currentPatience <= 0)
            {
                neg.currentPatience = 0;
                neg.status = NegotiationStatus.PatienceExhausted;
                _state.activeNegotiations[candidateId] = neg;
                _pendingEvents.Add(new PendingEvent
                {
                    type = PendingEventType.CandidateLostPatience,
                    candidateId = candidateId,
                    candidateName = name
                });
            }
            else
            {
                neg.status = NegotiationStatus.Pending;
                _state.activeNegotiations[candidateId] = neg;
                _pendingEvents.Add(new PendingEvent
                {
                    type = PendingEventType.CounterOfferExpired,
                    candidateId = candidateId,
                    candidateName = name
                });
            }
        }
    }

    private void ProcessEmployeeNegotiations(int tick)
    {
        int count = _state.employeeNegotiations.Count;
        for (int i = 0; i < count; i++)
        {
            var neg = _state.employeeNegotiations[i];

            if (neg.cooldownExpiryTick > 0 && tick >= neg.cooldownExpiryTick)
            {
                Employee emp = FindEmployee(neg.employeeId);
                string empName = emp != null ? emp.name : string.Empty;
                neg.currentPatience = neg.maxPatience;
                neg.roundCount = 0;
                neg.cooldownExpiryTick = 0;
                neg.status = NegotiationStatus.None;
                _state.employeeNegotiations[i] = neg;
                _pendingEvents.Add(new PendingEvent
                {
                    type = PendingEventType.EmployeeCooldownExpired,
                    candidateId = neg.employeeId.Value,
                    candidateName = empName
                });
                continue;
            }

            if (neg.status != NegotiationStatus.CounterOffered || !neg.hasCounterOffer) continue;
            if (tick < neg.counterOffer.ExpiryTick) continue;

            Employee employee = FindEmployee(neg.employeeId);
            string name = employee != null ? employee.name : string.Empty;

            neg.currentPatience -= 1;
            neg.hasCounterOffer = false;

            if (neg.currentPatience <= 0)
            {
                neg.currentPatience = 0;
                neg.status = NegotiationStatus.PatienceExhausted;
                neg.cooldownExpiryTick = tick + 30 * TimeState.TicksPerDay;
                _state.employeeNegotiations[i] = neg;
                _pendingEvents.Add(new PendingEvent
                {
                    type = PendingEventType.EmployeeFrustrated,
                    candidateId = neg.employeeId.Value,
                    candidateName = name,
                    cooldownExpiryTick = neg.cooldownExpiryTick
                });
            }
            else
            {
                neg.status = NegotiationStatus.Pending;
                _state.employeeNegotiations[i] = neg;
                if (neg.currentPatience == 1)
                {
                    _pendingEvents.Add(new PendingEvent
                    {
                        type = PendingEventType.PatienceLow,
                        candidateId = neg.employeeId.Value,
                        candidateName = name,
                        remainingPatience = neg.currentPatience,
                        isEmployee = true
                    });
                }
            }
        }
    }

    // ─── Private helpers ──────────────────────────────────────────────────────

    private bool IsSameOffer(OfferPackage a, OfferPackage b)
    {
        return a.OfferedSalary == b.OfferedSalary
            && a.OfferedRole == b.OfferedRole
            && a.EmploymentType == b.EmploymentType
            && a.Length == b.Length;
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

    private Employee FindEmployee(EmployeeId id)
    {
        _employeeState.employees.TryGetValue(id, out var emp);
        return emp;
    }

    private int FindEmployeeNegotiationIndex(EmployeeId id)
    {
        int count = _state.employeeNegotiations.Count;
        for (int i = 0; i < count; i++)
        {
            if (_state.employeeNegotiations[i].employeeId == id) return i;
        }
        return -1;
    }

    private void UpdateEmployeeNegotiation(int idx, EmployeeNegotiation neg)
    {
        if (idx < 0)
            _state.employeeNegotiations.Add(neg);
        else
            _state.employeeNegotiations[idx] = neg;
    }
}
