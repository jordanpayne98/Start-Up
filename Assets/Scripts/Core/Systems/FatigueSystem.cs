using System;
using System.Collections.Generic;

public class FatigueSystem : ISystem
{
    public event Action<EmployeeId, float> OnEnergyChanged;
    public event Action<EmployeeId> OnBurnoutStarted;

    private FatigueState _state;
    private EmployeeSystem _employeeSystem;
    private TeamState _teamState;
    private ILogger _logger;

    private List<EmployeeId> _employeeKeys;
    private List<(EmployeeId id, float energy)> _energyChangedBuffer;
    private List<EmployeeId> _burnoutStartedBuffer;

    private const float StartingEnergy = 100f;
    private const int LowEnergyStreakThreshold = 7;
    private const float LowEnergyThreshold = 25f;

    public FatigueSystem(FatigueState state, EmployeeSystem employeeSystem, TeamState teamState, ILogger logger) {
        _state = state ?? throw new ArgumentNullException(nameof(state));
        _employeeSystem = employeeSystem ?? throw new ArgumentNullException(nameof(employeeSystem));
        _teamState = teamState ?? throw new ArgumentNullException(nameof(teamState));
        _logger = logger ?? new NullLogger();
        _employeeKeys = new List<EmployeeId>();
        _energyChangedBuffer = new List<(EmployeeId, float)>();
        _burnoutStartedBuffer = new List<EmployeeId>();
    }

    public void InitializeEmployee(EmployeeId employeeId) {
        if (!_state.employeeFatigue.ContainsKey(employeeId)) {
            _state.employeeFatigue[employeeId] = new FatigueData(StartingEnergy);
        }
    }

    public void RemoveEmployee(EmployeeId employeeId) {
        _state.employeeFatigue.Remove(employeeId);
    }

    public float GetEnergy(EmployeeId employeeId) {
        if (_state.employeeFatigue.TryGetValue(employeeId, out var data))
            return data.Energy;
        return StartingEnergy;
    }

    public EnergyBand GetEnergyBand(EmployeeId employeeId) {
        return ToEnergyBand(GetEnergy(employeeId));
    }

    public float GetAverageTeamEnergy(TeamId teamId) {
        if (!_teamState.teams.TryGetValue(teamId, out var team)) return StartingEnergy;
        int count = team.members.Count;
        if (count == 0) return StartingEnergy;
        float weightedTotal = 0f;
        float totalEffective = 0f;
        for (int i = 0; i < count; i++) {
            var emp = _employeeSystem.GetEmployee(team.members[i]);
            float effectiveOutput = (emp != null && emp.isActive) ? emp.EffectiveOutput : 1.0f;
            weightedTotal += GetEnergy(team.members[i]) * effectiveOutput;
            totalEffective += effectiveOutput;
        }
        return totalEffective > 0f ? weightedTotal / totalEffective : StartingEnergy;
    }

    public bool IsBurnout(EmployeeId employeeId) {
        if (_state.employeeFatigue.TryGetValue(employeeId, out var data))
            return data.BurnoutPressure;
        return false;
    }

    public int GetCrunchDaysActive(EmployeeId employeeId) {
        if (_state.employeeFatigue.TryGetValue(employeeId, out var data))
            return data.CrunchDaysActive;
        return 0;
    }

    public void ResetCrunchTracking(List<EmployeeId> members) {
        if (members == null) return;
        int count = members.Count;
        for (int i = 0; i < count; i++) {
            var empId = members[i];
            if (!_state.employeeFatigue.TryGetValue(empId, out var data)) continue;
            data.CrunchDaysActive = 0;
            _state.employeeFatigue[empId] = data;
        }
    }

    public static float EnergyMultiplier(float energy) {
        return 0.75f + (energy / 100f) * 0.25f;
    }

    public static EnergyBand ToEnergyBand(float energy) {
        if (energy >= 85f) return EnergyBand.Fresh;
        if (energy >= 65f) return EnergyBand.Fit;
        if (energy >= 45f) return EnergyBand.Tiring;
        if (energy >= 25f) return EnergyBand.Drained;
        return EnergyBand.Exhausted;
    }

    public void ProcessDailyEnergy(int tick) {
        _employeeKeys.Clear();
        var keys = _state.employeeFatigue.Keys;
        foreach (var key in keys) {
            _employeeKeys.Add(key);
        }

        int keyCount = _employeeKeys.Count;
        for (int i = 0; i < keyCount; i++) {
            var empId = _employeeKeys[i];
            var employee = _employeeSystem.GetEmployee(empId);
            if (employee == null || !employee.isActive) continue;
            if (!employee.ownerCompanyId.IsPlayer) continue;

            if (!_state.employeeFatigue.TryGetValue(empId, out var data)) continue;

            bool wasInBurnout = data.BurnoutPressure;

            bool isOnTeam = _teamState.employeeToTeam.TryGetValue(empId, out var teamId);
            bool teamIsCrunching = false;
            if (isOnTeam && _teamState.teams.TryGetValue(teamId, out var crunchTeam))
                teamIsCrunching = crunchTeam.isCrunching;

            // Crunch tracking
            if (teamIsCrunching) {
                data.CrunchDaysActive++;
                if (data.RecentCrunchDays < 14) data.RecentCrunchDays++;

                float drain;
                if (data.CrunchDaysActive <= 2)       drain = 4f;
                else if (data.CrunchDaysActive <= 5)  drain = 8f;
                else if (data.CrunchDaysActive <= 9)  drain = 12f;
                else                                   drain = 18f;

                if (data.RecentCrunchDays >= 8) drain += 5f;

                data.Energy -= drain;
                if (data.Energy < 0f) data.Energy = 0f;
            } else {
                if (data.CrunchDaysActive > 0) data.CrunchDaysActive = 0;
                if (data.RecentCrunchDays > 0) {
                    data.RecentCrunchDays--;
                    data.Energy += 6f;
                    if (data.Energy > 100f) data.Energy = 100f;
                } else {
                    // Natural daily recovery when not in crunch
                    data.Energy += 3f;
                    if (data.Energy > 100f) data.Energy = 100f;
                }
            }

            // Low-energy streak tracking
            if (data.Energy < LowEnergyThreshold) {
                data.ConsecutiveLowEnergyDays++;
            } else {
                data.ConsecutiveLowEnergyDays = 0;
            }

            bool nowInBurnout = data.ConsecutiveLowEnergyDays >= LowEnergyStreakThreshold;
            data.BurnoutPressure = nowInBurnout;

            _state.employeeFatigue[empId] = data;

            _energyChangedBuffer.Add((empId, data.Energy));

            if (!wasInBurnout && nowInBurnout) {
                _burnoutStartedBuffer.Add(empId);
                _logger.Log($"[FatigueSystem] Employee {empId.Value} entered burnout pressure at energy {data.Energy:F1}");
            }
        }
    }

    public void PreTick(int tick) { }
    public void Tick(int tick) { }

    public void PostTick(int tick) {
        int energyCount = _energyChangedBuffer.Count;
        for (int i = 0; i < energyCount; i++) {
            var entry = _energyChangedBuffer[i];
            OnEnergyChanged?.Invoke(entry.id, entry.energy);
        }
        _energyChangedBuffer.Clear();

        int burnoutCount = _burnoutStartedBuffer.Count;
        for (int i = 0; i < burnoutCount; i++) {
            OnBurnoutStarted?.Invoke(_burnoutStartedBuffer[i]);
        }
        _burnoutStartedBuffer.Clear();
    }

    public void ApplyCommand(ICommand command) { }

    public void Dispose() {
        _energyChangedBuffer.Clear();
        _burnoutStartedBuffer.Clear();
        OnEnergyChanged = null;
        OnBurnoutStarted = null;
    }
}
