// MoraleSystem Version: Clean v2 (Part 5 — Energy/Morale Refactor)
using System;
using System.Collections.Generic;
using UnityEngine;

public class MoraleSystem : ISystem
{
    public event Action<EmployeeId, float> OnMoraleChanged;
    public event Action<EmployeeId> OnEmployeeMayQuit;

    private MoraleState _state;
    private EmployeeSystem _employeeSystem;
    private TeamState _teamState;
    private ContractState _contractState;
    private ProductState _productState;
    private GameEventBus _eventBus;
    private ILogger _logger;
    private FatigueSystem _fatigueSystem;
    private List<EmployeeId> _employeeKeys;

    // Event buffers — pre-allocated, no per-tick allocation
    private List<(EmployeeId id, float morale)> _moraleChangedBuffer;
    private List<EmployeeId> _mayQuitBuffer;
    private List<(int tick, TeamId teamId, string teamName)> _idleAlertBuffer;

    private const float DefaultStartingMorale = 60f;
    private const float QuitThreshold = 20f;
    private const float IdleAlertMoraleThreshold = 50f;

    private TuningConfig _tuning;

    // Single authoritative morale multiplier. Range [0.95, 1.05].
    // Narrow 10% spread — energy carries the 25% spread via EnergyMultiplier.
    public static float MoraleMultiplier(float morale) {
        float raw = 1.0f + (morale - 60f) * 0.00125f;
        return Mathf.Clamp(raw, 0.95f, 1.05f);
    }

    public MoraleSystem(MoraleState state, EmployeeSystem employeeSystem,
        TeamState teamState, ContractState contractState, ProductState productState, GameEventBus eventBus, ILogger logger) {
        _state = state ?? throw new ArgumentNullException(nameof(state));
        _employeeSystem = employeeSystem ?? throw new ArgumentNullException(nameof(employeeSystem));
        _teamState = teamState ?? throw new ArgumentNullException(nameof(teamState));
        _contractState = contractState ?? throw new ArgumentNullException(nameof(contractState));
        _productState = productState;
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _logger = logger ?? new NullLogger();
        _employeeKeys = new List<EmployeeId>();
        _moraleChangedBuffer = new List<(EmployeeId, float)>();
        _mayQuitBuffer = new List<EmployeeId>();
        _idleAlertBuffer = new List<(int, TeamId, string)>();
    }

    public void SetTuningConfig(TuningConfig tuning) {
        _tuning = tuning;
    }

    public void SetFatigueSystem(FatigueSystem fatigueSystem) {
        _fatigueSystem = fatigueSystem;
    }

    public void InitializeEmployee(EmployeeId employeeId, float startingMorale = 60f) {
        if (!_state.employeeMorale.ContainsKey(employeeId)) {
            _state.employeeMorale[employeeId] = new MoraleData(startingMorale);
            var employee = _employeeSystem.GetEmployee(employeeId);
            if (employee != null) employee.morale = (int)startingMorale;
            _logger.Log($"[MoraleSystem] Initialized morale for employee {employeeId.Value} at {startingMorale}");
        }
    }

    public void RemoveEmployee(EmployeeId employeeId) {
        _state.employeeMorale.Remove(employeeId);
    }

    public float GetMorale(EmployeeId employeeId) {
        if (_state.employeeMorale.TryGetValue(employeeId, out var data)) {
            return data.currentMorale;
        }
        return 50f;
    }

    public float GetMoraleMultiplier(EmployeeId employeeId) {
        return MoraleMultiplier(GetMorale(employeeId));
    }

    // Instant morale delta — used by TeamChemistrySystem for conflict events (bypasses daily clamp).
    public void ApplyDirectMoraleDelta(EmployeeId employeeId, float delta) {
        if (!_state.employeeMorale.TryGetValue(employeeId, out var data)) return;
        var employee = _employeeSystem.GetEmployee(employeeId);
        if (employee == null || !employee.isActive) return;

        float newMorale = data.currentMorale + delta;
        if (newMorale < 0f) newMorale = 0f;
        if (newMorale > 100f) newMorale = 100f;

        data.currentMorale = newMorale;
        _state.employeeMorale[employeeId] = data;
        employee.morale = (int)newMorale;
        _moraleChangedBuffer.Add((employeeId, newMorale));
    }

    // Called by GameController when a contract is completed.
    public void ApplyContractCompletedBonus(List<EmployeeId> teamMembers, float quality) {
        if (teamMembers == null) return;

        int upgradeBonus = 0;

        int qualityHigh = _tuning != null ? _tuning.MoraleCompletionQualityHigh : 8;
        int qualityMid  = _tuning != null ? _tuning.MoraleCompletionQualityMid  : 5;
        int qualityLow  = _tuning != null ? _tuning.MoraleCompletionQualityLow  : 2;

        int qualityBonus;
        if (quality >= 90f)      qualityBonus = qualityHigh;
        else if (quality >= 75f) qualityBonus = qualityMid;
        else if (quality >= 50f) qualityBonus = qualityLow;
        else                     qualityBonus = 0;

        int baseBonus = _tuning != null ? _tuning.ContractCompletionBaseMoraleBonus : 5;
        int totalBonus = baseBonus + qualityBonus + upgradeBonus;

        int count = teamMembers.Count;
        for (int i = 0; i < count; i++) {
            var empId = teamMembers[i];
            var employee = _employeeSystem.GetEmployee(empId);
            if (employee == null || !employee.isActive) continue;

            if (!_state.employeeMorale.TryGetValue(empId, out var data)) continue;

            float newMorale = data.currentMorale + totalBonus;
            if (newMorale > 100f) newMorale = 100f;
            if (newMorale < 0f) newMorale = 0f;

            data.currentMorale = newMorale;
            _state.employeeMorale[empId] = data;
            employee.morale = (int)newMorale;

            _moraleChangedBuffer.Add((empId, newMorale));
        }
    }

    // Called by GameController when a contract fails.
    public void ApplyContractFailedPenalty(List<EmployeeId> teamMembers, int penalty) {
        if (teamMembers == null) return;

        int count = teamMembers.Count;
        for (int i = 0; i < count; i++) {
            var empId = teamMembers[i];
            var employee = _employeeSystem.GetEmployee(empId);
            if (employee == null || !employee.isActive) continue;

            if (!_state.employeeMorale.TryGetValue(empId, out var data)) continue;

            float newMorale = data.currentMorale - penalty;
            if (newMorale < 0f) newMorale = 0f;
            if (newMorale > 100f) newMorale = 100f;

            data.currentMorale = newMorale;
            _state.employeeMorale[empId] = data;
            employee.morale = (int)newMorale;

            _moraleChangedBuffer.Add((empId, newMorale));
        }
    }

    // Called by GameController when a product phase is completed.
    public void ApplyPhaseCompletedBonus(ProductId productId, float phaseQuality) {
        Product product = null;
        if (!_productState.developmentProducts.TryGetValue(productId, out product)) {
            _productState.shippedProducts.TryGetValue(productId, out product);
        }
        if (product == null || product.TeamAssignments == null) return;

        int qualityBonus;
        if (phaseQuality >= 80f)      qualityBonus = 3;
        else if (phaseQuality >= 60f) qualityBonus = 1;
        else                          qualityBonus = 0;

        int baseBonus = _tuning != null ? _tuning.PhaseCompletionBaseMoraleBonus : 3;
        int totalBonus = baseBonus + qualityBonus;

        var teamIds = product.TeamAssignments.Values;
        foreach (var teamId in teamIds) {
            if (!_teamState.teams.TryGetValue(teamId, out var team)) continue;
            int memberCount = team.members.Count;
            for (int i = 0; i < memberCount; i++) {
                var empId = team.members[i];
                var employee = _employeeSystem.GetEmployee(empId);
                if (employee == null || !employee.isActive) continue;
                if (!_state.employeeMorale.TryGetValue(empId, out var data)) continue;

                float newMorale = data.currentMorale + totalBonus;
                if (newMorale > 100f) newMorale = 100f;
                if (newMorale < 0f) newMorale = 0f;

                data.currentMorale = newMorale;
                _state.employeeMorale[empId] = data;
                employee.morale = (int)newMorale;

                _moraleChangedBuffer.Add((empId, newMorale));
            }
        }
    }

    // Called by GameController when a product is shipped.
    public void ApplyProductShippedBonus(ProductId productId, float overallQuality) {
        Product product = null;
        if (!_productState.developmentProducts.TryGetValue(productId, out product)) {
            _productState.shippedProducts.TryGetValue(productId, out product);
        }
        if (product == null || product.TeamAssignments == null) return;

        int qualityBonus;
        if (overallQuality >= 85f)      qualityBonus = 8;
        else if (overallQuality >= 70f) qualityBonus = 5;
        else if (overallQuality >= 50f) qualityBonus = 2;
        else                            qualityBonus = 0;

        int baseBonus = _tuning != null ? _tuning.ProductShipBaseMoraleBonus : 8;
        int totalBonus = baseBonus + qualityBonus;

        var teamIds = product.TeamAssignments.Values;
        foreach (var teamId in teamIds) {
            if (!_teamState.teams.TryGetValue(teamId, out var team)) continue;
            int memberCount = team.members.Count;
            for (int i = 0; i < memberCount; i++) {
                var empId = team.members[i];
                var employee = _employeeSystem.GetEmployee(empId);
                if (employee == null || !employee.isActive) continue;
                if (!_state.employeeMorale.TryGetValue(empId, out var data)) continue;

                float newMorale = data.currentMorale + totalBonus;
                if (newMorale > 100f) newMorale = 100f;
                if (newMorale < 0f) newMorale = 0f;

                data.currentMorale = newMorale;
                _state.employeeMorale[empId] = data;
                employee.morale = (int)newMorale;

                _moraleChangedBuffer.Add((empId, newMorale));
            }
        }
    }

    public void ProcessDailyMorale(int currentDay, IRng rng) {
        _employeeKeys.Clear();
        var keys = _state.employeeMorale.Keys;
        foreach (var key in keys) {
            _employeeKeys.Add(key);
        }

        float quitThreshold      = _tuning != null ? _tuning.QuitThreshold              : QuitThreshold;
        float idleAlertThreshold = _tuning != null ? _tuning.IdleAlertMoraleThreshold   : IdleAlertMoraleThreshold;
        float quitChanceBase     = _tuning != null ? _tuning.QuitChanceBase             : 0.025f;
        float quitAmbitionScale  = _tuning != null ? _tuning.QuitChanceAmbitionScale    : 0.05f;
        float equilibriumTarget  = 60f;
        float equilibriumCoeff   = 0.03f;
        float lowRecovFloor      = 0.20f;
        float workingBonus       = 0.15f;

        int keyCount = _employeeKeys.Count;
        for (int i = 0; i < keyCount; i++) {
            var empId = _employeeKeys[i];
            var employee = _employeeSystem.GetEmployee(empId);
            if (employee == null || !employee.isActive) continue;
            if (!employee.ownerCompanyId.IsPlayer) continue;

            if (!_state.employeeMorale.TryGetValue(empId, out var data)) continue;

            float moraleChange = 0f;

            bool isOnTeam = _teamState.employeeToTeam.TryGetValue(empId, out var teamId);
            bool teamHasContract = isOnTeam && _contractState.teamAssignments.ContainsKey(teamId);
            bool teamHasProduct = isOnTeam && _productState != null && _productState.teamToProduct.ContainsKey(teamId);

            // 1. Equilibrium pull: gap * 0.03, clamped [-1.2, +1.2]
            float gap = equilibriumTarget - data.currentMorale;
            float delta = gap * equilibriumCoeff;
            if (delta > 1.2f) delta = 1.2f;
            if (delta < -1.2f) delta = -1.2f;
            moraleChange += delta;

            // 2. Low-morale recovery floor
            if (data.currentMorale < 30f)
                moraleChange += lowRecovFloor;

            if (isOnTeam) {
                if (!teamHasContract && !teamHasProduct) {
                    // Idle path
                    data.consecutiveIdleDays++;

                    int idleRecoveryStart = _tuning != null ? _tuning.IdleRecoveryStartDay : 3;
                    int idleBoredomStart  = _tuning != null ? _tuning.IdleBoredomStartDay  : 8;
                    int idleDecayStart    = _tuning != null ? _tuning.IdleDecayStartDay    : 61;
                    float idleRecoveryBonus  = _tuning != null ? _tuning.IdleRecoveryBonus      : 15f;
                    float idleBoredomRate    = _tuning != null ? _tuning.IdleBoredomDecayPerDay  : 0.25f;
                    float idleDecayRate      = _tuning != null ? _tuning.IdleDecayPerDay          : 0.50f;

                    // Idle alert: fire once per team when boredom phase begins
                    if (data.consecutiveIdleDays == idleBoredomStart && !data.idleAlertSent) {
                        if (_teamState.teams.TryGetValue(teamId, out var alertTeam)) {
                            int memberCount = alertTeam.members.Count;
                            for (int m = 0; m < memberCount; m++) {
                                var memberId = alertTeam.members[m];
                                if (_state.employeeMorale.TryGetValue(memberId, out var memberData)) {
                                    memberData.idleAlertSent = true;
                                    _state.employeeMorale[memberId] = memberData;
                                }
                            }
                            data.idleAlertSent = true;
                            _idleAlertBuffer.Add((currentDay * TimeState.TicksPerDay, teamId, alertTeam.name));
                            _logger.Log($"[MoraleSystem] Team {alertTeam.name} idle morale alert fired");
                        }
                    }

                    if (data.consecutiveIdleDays == idleRecoveryStart) {
                        moraleChange += idleRecoveryBonus;
                    } else if (data.consecutiveIdleDays >= idleDecayStart) {
                        float weeksOver = (data.consecutiveIdleDays - idleDecayStart) / 7f;
                        float scaledDecay = idleDecayRate + weeksOver * 0.1f;
                        float maxDecay = _tuning != null ? _tuning.IdleDecayMax : 2.0f;
                        if (scaledDecay > maxDecay) scaledDecay = maxDecay;
                        moraleChange -= scaledDecay;
                    } else if (data.consecutiveIdleDays >= idleBoredomStart) {
                        moraleChange -= idleBoredomRate;
                    }
                    // days 1-7 idle: 0 delta (spec)

                } else {
                    // Working path
                    if (data.idleAlertSent) data.idleAlertSent = false;
                    data.consecutiveIdleDays = 0;

                    moraleChange += workingBonus;

                    if (teamHasContract) {
                        ContractId contractId = _contractState.teamAssignments[teamId];
                        if (_contractState.activeContracts.TryGetValue(contractId, out var contract)) {
                            int optimal = contract.OptimalContributors > 0 ? contract.OptimalContributors : 1;
                            float effectiveCapacity = 1f;
                            if (_teamState.teams.TryGetValue(teamId, out var contractTeam)) {
                                effectiveCapacity = TeamWorkEngine.ComputeEffectiveCapacity(contractTeam.members, _employeeSystem);
                                if (effectiveCapacity <= 0f) effectiveCapacity = contractTeam.MemberCount;
                            }

                            // 4. Contract overload penalties: severe -1.00, mid -0.40, light -0.15
                            if (effectiveCapacity < optimal * 0.5f)        moraleChange -= 1.00f;
                            else if (effectiveCapacity < optimal * 0.75f)  moraleChange -= 0.40f;
                            else if (effectiveCapacity < optimal)           moraleChange -= 0.15f;

                            // 5. Contract wrong-role penalties
                            bool isRoleFit = TeamWorkEngine.IsRoleFitForSkill(employee.role, contract.RequiredSkill);
                            if (!isRoleFit) {
                                int skillValue = employee.GetSkill(contract.RequiredSkill);
                                if (skillValue == 0)      moraleChange -= 0.80f;
                                else if (skillValue < 3)  moraleChange -= 0.40f;
                                else                      moraleChange -= 0.15f;
                            }

                            // 6. Contract job satisfaction bonus
                            if (isRoleFit) {
                                if (effectiveCapacity >= optimal)
                                    moraleChange += 0.75f;
                                else if (effectiveCapacity >= optimal * 0.75f)
                                    moraleChange += 0.35f;
                            }
                        }
                    } else if (teamHasProduct && _productState.teamToProduct.TryGetValue(teamId, out var productId)) {
                        Product product = null;
                        if (!_productState.developmentProducts.TryGetValue(productId, out product)) {
                            _productState.shippedProducts.TryGetValue(productId, out product);
                        }

                        if (product != null && product.Phases != null) {
                            int phaseCount = product.Phases.Length;

                            int unlockedPhaseCount = 0;
                            for (int p = 0; p < phaseCount; p++) {
                                if (product.Phases[p].isUnlocked) unlockedPhaseCount++;
                            }

                            float effectiveCapacity = 1f;
                            if (_teamState.teams.TryGetValue(teamId, out var productTeam)) {
                                effectiveCapacity = TeamWorkEngine.ComputeEffectiveCapacity(productTeam.members, _employeeSystem);
                                if (effectiveCapacity <= 0f) effectiveCapacity = productTeam.MemberCount;
                            }
                            int optimal = Math.Max(2, unlockedPhaseCount);

                            // 7. Product overload penalties: severe -0.75, mid -0.30, light -0.10
                            if (effectiveCapacity < optimal * 0.5f)        moraleChange -= 0.75f;
                            else if (effectiveCapacity < optimal * 0.75f)  moraleChange -= 0.30f;
                            else if (effectiveCapacity < optimal)           moraleChange -= 0.10f;

                            // 8. Product wrong-function penalty
                            bool employeeHasMatchingPhase = false;
                            for (int p = 0; p < phaseCount; p++) {
                                SkillType phaseSkill = TeamWorkEngine.MapPhaseToSkill(product.Phases[p].phaseType);
                                if (TeamWorkEngine.IsRoleFitForSkill(employee.role, phaseSkill)) {
                                    employeeHasMatchingPhase = true;
                                    break;
                                }
                            }

                            if (!employeeHasMatchingPhase) {
                                moraleChange -= 0.50f;
                            }

                            // 9. Product fit bonus: strong +0.50, weaker +0.25
                            if (employeeHasMatchingPhase) {
                                if (effectiveCapacity >= optimal)
                                    moraleChange += 0.50f;
                                else if (effectiveCapacity >= optimal * 0.75f)
                                    moraleChange += 0.25f;
                            }
                        }
                    }
                }
            }

            // 11. Energy pressure from FatigueSystem
            if (_fatigueSystem != null) {
                float energy = _fatigueSystem.GetEnergy(empId);
                bool inBurnout = _fatigueSystem.IsBurnout(empId);

                if (energy < 40f) moraleChange -= 0.35f;
                if (energy < 25f) moraleChange -= 0.60f;
                if (energy < 10f) moraleChange -= 0.90f;
                if (inBurnout && energy < 25f) moraleChange -= 0.50f;
            }

            // 12. Salary competitiveness pressure (skip founders with salary 0)
            if (!employee.isFounder || employee.salary > 0) {
                int marketRate = SalaryBand.GetBase(employee.role);
                float effectiveOutput = employee.EffectiveOutput > 0f ? employee.EffectiveOutput : 1.0f;
                float employeeCostPerOutput = employee.salary / effectiveOutput;
                float marketCostPerOutput = marketRate;
                float salaryDelta = (employeeCostPerOutput - marketCostPerOutput) / marketCostPerOutput;

                float salaryWellAbove    = _tuning != null ? _tuning.SalaryPressureWellAbove    : 0.15f;
                float salaryAboveMarket  = _tuning != null ? _tuning.SalaryPressureAboveMarket  : 0.05f;
                float salaryBelowMarket  = _tuning != null ? _tuning.SalaryPressureBelowMarket  : -0.10f;
                float salaryFarBelow     = _tuning != null ? _tuning.SalaryPressureFarBelow     : -0.25f;

                if (salaryDelta >= 0.20f)       moraleChange += salaryWellAbove;
                else if (salaryDelta >= 0.10f)  moraleChange += salaryAboveMarket;
                else if (salaryDelta >= -0.10f) { /* at market — neutral */ }
                else if (salaryDelta >= -0.20f) moraleChange += salaryBelowMarket;
                else                            moraleChange += salaryFarBelow;
            }

            // 13. Preference satisfaction pressure
            {
                float prefMatchBoth    = _tuning != null ? _tuning.PrefMatchBothBonus        : 0.10f;
                float prefMatchOne     = _tuning != null ? _tuning.PrefMatchOneBonus          : 0.05f;
                float prefMismatchOne  = _tuning != null ? _tuning.PrefMismatchOnePenalty     : -0.08f;
                float prefMismatchBoth = _tuning != null ? _tuning.PrefMismatchBothPenalty    : -0.20f;
                float strikeScale      = _tuning != null ? _tuning.StrikeEscalationMultiplier : 0.5f;

                bool hasKnownPrefs = employee.Contract.HiredTick > 0;
                if (hasKnownPrefs) {
                    PreferenceMatchState matchState = SalaryModifierCalculator.ComputePreferenceMatch(
                        employee.OriginalPreferences,
                        employee.Contract.Type,
                        employee.Contract.Length);

                    float prefDelta = 0f;
                    switch (matchState) {
                        case PreferenceMatchState.BothMatched:          prefDelta = prefMatchBoth;    break;
                        case PreferenceMatchState.OneMatchedOneNeutral: prefDelta = prefMatchOne;     break;
                        case PreferenceMatchState.BothNeutral:          prefDelta = 0f;               break;
                        case PreferenceMatchState.OneMismatched:        prefDelta = prefMismatchOne;  break;
                        case PreferenceMatchState.BothMismatched:       prefDelta = prefMismatchBoth; break;
                    }

                    // Strike escalation: applies only to dissatisfaction
                    if (prefDelta < 0f && employee.StrikeCount >= 1) {
                        float escalation = 1.0f + employee.StrikeCount * strikeScale;
                        prefDelta *= escalation;
                    }

                    moraleChange += prefDelta;
                }
            }

            // 14. Clamp daily change: [-2.5, +1.5]
            if (moraleChange > 1.5f)  moraleChange = 1.5f;
            if (moraleChange < -2.5f) moraleChange = -2.5f;

            // 15. Clamp morale [0, 100]
            float newMorale = data.currentMorale + moraleChange;
            if (newMorale < 0f) newMorale = 0f;
            if (newMorale > 100f) newMorale = 100f;

            data.currentMorale = newMorale;
            _state.employeeMorale[empId] = data;
            employee.morale = (int)newMorale;

            if (data.currentMorale < quitThreshold) {
                float quitChance = quitChanceBase + (employee.hiddenAttributes.Ambition / 20f) * quitAmbitionScale;
                if (rng.Chance(quitChance)) {
                    _mayQuitBuffer.Add(empId);
                    _logger.Log($"[MoraleSystem] Employee {empId.Value} morale critically low ({data.currentMorale:F1}), may quit");
                }
            }

            _moraleChangedBuffer.Add((empId, newMorale));
        }
    }

    public void PreTick(int tick) { }
    public void Tick(int tick) { }

    public void PostTick(int tick) {
        int moraleCount = _moraleChangedBuffer.Count;
        for (int i = 0; i < moraleCount; i++) {
            var entry = _moraleChangedBuffer[i];
            OnMoraleChanged?.Invoke(entry.id, entry.morale);
        }
        _moraleChangedBuffer.Clear();

        int quitCount = _mayQuitBuffer.Count;
        for (int i = 0; i < quitCount; i++) {
            OnEmployeeMayQuit?.Invoke(_mayQuitBuffer[i]);
        }
        _mayQuitBuffer.Clear();

        int alertCount = _idleAlertBuffer.Count;
        for (int i = 0; i < alertCount; i++) {
            var entry = _idleAlertBuffer[i];
            _eventBus.Raise(new TeamIdleMoraleAlertEvent(entry.tick, entry.teamId, entry.teamName));
        }
        _idleAlertBuffer.Clear();
    }

    public void ApplyCommand(ICommand command) { }

    public void Dispose() {
        _moraleChangedBuffer.Clear();
        _mayQuitBuffer.Clear();
        _idleAlertBuffer.Clear();
        OnMoraleChanged = null;
        OnEmployeeMayQuit = null;
    }
}
