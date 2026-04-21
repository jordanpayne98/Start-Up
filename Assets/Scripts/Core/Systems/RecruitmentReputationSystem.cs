// RecruitmentReputationSystem Version: Clean v1
using System;
using System.Collections.Generic;

public class RecruitmentReputationSystem : ISystem
{
    public event Action<int, int> OnRecruitmentScoreChanged;

    private const int DecayIntervalDays = 30;
    private const int NeutralScore = 50;

    private RecruitmentReputationState _state;
    private EmployeeSystem _employeeSystem;
    private ILogger _logger;
    private TuningConfig _tuning;
    private int _lastDayProcessed = -1;

    private readonly List<(int old, int next)> _pendingScoreEvents;

    public int Score => _state.score;

    public RecruitmentReputationSystem(RecruitmentReputationState state, EmployeeSystem employeeSystem, ILogger logger)
    {
        _state = state;
        _employeeSystem = employeeSystem;
        _logger = logger ?? new NullLogger();
        _pendingScoreEvents = new List<(int, int)>();
    }

    public void SetTuningConfig(TuningConfig tuning)
    {
        _tuning = tuning;
    }

    public void AddScore(int amount)
    {
        if (amount <= 0) return;
        int oldScore = _state.score;
        _state.score += amount;
        if (_state.score > 100) _state.score = 100;

        if (oldScore != _state.score)
        {
            _pendingScoreEvents.Add((oldScore, _state.score));
            _logger.Log($"[RecruitmentReputation] Score +{amount}: {oldScore} -> {_state.score}");
        }
    }

    public void RemoveScore(int amount)
    {
        if (amount <= 0) return;
        int oldScore = _state.score;
        _state.score -= amount;
        if (_state.score < 0) _state.score = 0;

        if (oldScore != _state.score)
        {
            _pendingScoreEvents.Add((oldScore, _state.score));
            _logger.Log($"[RecruitmentReputation] Score -{amount}: {oldScore} -> {_state.score}");
        }
    }

    public void OnEmployeeHiredHandler(EmployeeId id)
    {
        AddScore(_tuning != null ? _tuning.RecruitRepHireBonus : 2);
    }

    public void OnEmployeeFiredHandler(EmployeeId id)
    {
        RemoveScore(_tuning != null ? _tuning.RecruitRepFirePenalty : 5);
    }

    public void OnEmployeeQuitHandler(EmployeeId id)
    {
        RemoveScore(_tuning != null ? _tuning.RecruitRepQuitPenalty : 3);
    }

    public void OnOfferRejectedHandler(int candidateId)
    {
        RemoveScore(_tuning != null ? _tuning.RecruitRepRejectPenalty : 1);
    }

    public void PreTick(int tick) { }

    public void Tick(int tick)
    {
        int currentDay = tick / TimeState.TicksPerDay;
        bool isDayBoundary = tick % TimeState.TicksPerDay == 0 && currentDay != _lastDayProcessed;
        if (!isDayBoundary) return;

        _lastDayProcessed = currentDay;

        int decayIntervalDays = _tuning != null ? _tuning.RecruitRepDecayIntervalDays : DecayIntervalDays;
        int neutralScore      = _tuning != null ? _tuning.RecruitRepNeutralScore      : NeutralScore;
        if (currentDay - _state.lastDecayDay >= decayIntervalDays)
        {
            _state.lastDecayDay = currentDay;
            if (_state.score > neutralScore)
            {
                int oldScore = _state.score;
                _state.score--;
                _pendingScoreEvents.Add((oldScore, _state.score));
            }
            else if (_state.score < neutralScore)
            {
                int oldScore = _state.score;
                _state.score++;
                _pendingScoreEvents.Add((oldScore, _state.score));
            }
        }

        int loyaltyDays = _tuning != null ? _tuning.RecruitRepLoyaltyDays : 180;
        int loyaltyThresholdTicks = TimeState.TicksPerDay * loyaltyDays;
        int loyaltyBonus = _tuning != null ? _tuning.RecruitRepLoyaltyBonus : 3;
        var activeEmployees = _employeeSystem.GetAllActiveEmployees();
        int empCount = activeEmployees.Count;
        for (int i = 0; i < empCount; i++)
        {
            var emp = activeEmployees[i];
            if (_state.loyaltyBonusAwarded.ContainsKey(emp.id)) continue;

            int ticksEmployed = tick - emp.hireDate;
            if (ticksEmployed >= loyaltyThresholdTicks)
            {
                _state.loyaltyBonusAwarded[emp.id] = true;
                AddScore(loyaltyBonus);
                _logger.Log($"[RecruitmentReputation] Loyalty bonus for {emp.name} (6+ months)");
            }
        }
    }

    public void PostTick(int tick)
    {
        int count = _pendingScoreEvents.Count;
        for (int i = 0; i < count; i++)
        {
            (int old, int next) = _pendingScoreEvents[i];
            OnRecruitmentScoreChanged?.Invoke(old, next);
        }
        _pendingScoreEvents.Clear();
    }

    public void ApplyCommand(ICommand command) { }

    public float GetQualityMultiplier()
    {
        int score = _state.score;
        if (score <= 20) return 0.7f;
        if (score <= 40) return 0.85f;
        if (score <= 60) return 1.0f;
        if (score <= 80) return 1.15f;
        return 1.3f;
    }

    public void Dispose()
    {
        _pendingScoreEvents.Clear();
        OnRecruitmentScoreChanged = null;
    }
}
