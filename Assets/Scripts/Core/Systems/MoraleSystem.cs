// MoraleSystem Version: Clean v1
using System;
using System.Collections.Generic;

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
    private List<EmployeeId> _employeeKeys;

    // Event buffers — pre-allocated, no per-tick allocation
    private List<(EmployeeId id, float morale)> _moraleChangedBuffer;
    private List<EmployeeId> _mayQuitBuffer;
    private List<(int tick, TeamId teamId, string teamName)> _idleAlertBuffer;

    private const float DefaultStartingMorale = 60f;
    private const float QuitThreshold = 20f;
    private const float IdleAlertMoraleThreshold = 50f;

    private TuningConfig _tuning;

    // Single authoritative morale multiplier. Range [0.85, 1.10].
    // 0.25 spread means a 100-morale employee works 29% harder than a 0-morale one.
    public static float MoraleMultiplier(float morale)
    {
        return 0.85f + (morale / 100f) * 0.25f;
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

    // Called by GameController when a contract is completed.
    // base +3, quality bonus, upgrade bonus all applied per team member.
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
    // penalty is a positive integer representing the magnitude of loss.
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

    // Resets crunch tracking for the given members — called when crunch is auto-cancelled.
    public void ResetCrunchTracking(List<EmployeeId> members) {
        if (members == null) return;
        int count = members.Count;
        for (int i = 0; i < count; i++) {
            var empId = members[i];
            if (!_state.employeeMorale.TryGetValue(empId, out var data)) continue;
            data.crunchDaysActive = 0;
            _state.employeeMorale[empId] = data;
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

        float quitThreshold       = _tuning != null ? _tuning.QuitThreshold              : QuitThreshold;
        float idleAlertThreshold  = _tuning != null ? _tuning.IdleAlertMoraleThreshold   : IdleAlertMoraleThreshold;
        float quitChanceBase      = _tuning != null ? _tuning.QuitChanceBase             : 0.025f;
        float quitAmbitionScale   = _tuning != null ? _tuning.QuitChanceAmbitionScale    : 0.05f;
        float equilibriumTarget   = _tuning != null ? _tuning.MoraleEquilibriumTarget    : 60f;
        float equilibriumCoeff    = _tuning != null ? _tuning.MoraleEquilibriumCoefficient : 0.07f;
        float overloadSevere      = _tuning != null ? _tuning.MoraleOverloadSevere       : 2.0f;
        float overloadModerate    = _tuning != null ? _tuning.MoraleOverloadModerate     : 0.75f;
        float overloadMild        = _tuning != null ? _tuning.MoraleOverloadMild         : 0.25f;
        float wrongFuncSevere     = _tuning != null ? _tuning.MoraleWrongFuncSevere      : 1.5f;
        float wrongFuncModerate   = _tuning != null ? _tuning.MoraleWrongFuncModerate    : 0.75f;
        float wrongFuncMild       = _tuning != null ? _tuning.MoraleWrongFuncMild        : 0.25f;
        float satisfactionFull    = _tuning != null ? _tuning.MoraleSatisfactionFull     : 1.5f;
        float satisfactionPartial = _tuning != null ? _tuning.MoraleSatisfactionPartial  : 0.75f;
        float prodSatFull         = _tuning != null ? _tuning.MoraleProductSatisfactionFull    : 1.0f;
        float prodSatPartial      = _tuning != null ? _tuning.MoraleProductSatisfactionPartial : 0.5f;
        float prodOverloadSevere  = _tuning != null ? _tuning.MoraleProductOverloadSevere   : 1.0f;
        float prodOverloadModerate = _tuning != null ? _tuning.MoraleProductOverloadModerate : 0.5f;
        float prodOverloadMild    = _tuning != null ? _tuning.MoraleProductOverloadMild     : 0.1f;
        float workingBonus        = _tuning != null ? _tuning.MoraleWorkingBonus            : 0.3f;
        float penaltyFloor        = _tuning != null ? _tuning.MoraleDailyPenaltyFloor       : 3.0f;
        int keyCount = _employeeKeys.Count;        for (int i = 0; i < keyCount; i++) {
            var empId = _employeeKeys[i];
            var employee = _employeeSystem.GetEmployee(empId);
            if (employee == null || !employee.isActive) continue;
            if (!employee.ownerCompanyId.IsPlayer) continue;

            if (!_state.employeeMorale.TryGetValue(empId, out var data)) continue;

            float moraleChange = 0f;

            bool isOnTeam = _teamState.employeeToTeam.TryGetValue(empId, out var teamId);
            bool teamHasContract = isOnTeam && _contractState.teamAssignments.ContainsKey(teamId);
            bool teamHasProduct = isOnTeam && _productState != null && _productState.teamToProduct.ContainsKey(teamId);

            // Equilibrium pull — always applied, draws morale toward equilibriumTarget
            float gap = equilibriumTarget - data.currentMorale;
            float absGap = gap >= 0f ? gap : -gap;
            float pullStrength = equilibriumCoeff * (1f + absGap / 30f);
            moraleChange += gap * pullStrength;

            float lowThreshold = _tuning != null ? _tuning.MoraleLowRecoveryThreshold : 30f;
            float lowBonus = _tuning != null ? _tuning.MoraleLowRecoveryBonus : 0.5f;
            if (data.currentMorale < lowThreshold)
                moraleChange += lowBonus;

            if (isOnTeam) {
                if (!teamHasContract && !teamHasProduct) {
                    // Idle: team has no contract — idle is neutral (no decay)

                    data.consecutiveIdleDays++;

                    int idleRecoveryStart = _tuning != null ? _tuning.IdleRecoveryStartDay : 3;
                    int idleBoredomStart = _tuning != null ? _tuning.IdleBoredomStartDay : 8;
                    int idleDecayStart = _tuning != null ? _tuning.IdleDecayStartDay : 61;
                    float idleRecoveryBonus = _tuning != null ? _tuning.IdleRecoveryBonus : 15f;
                    float idleBoredomRate = _tuning != null ? _tuning.IdleBoredomDecayPerDay : 0.2f;
                    float idleDecayRate = _tuning != null ? _tuning.IdleDecayPerDay : 0.5f;

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

                    if (data.consecutiveIdleDays == idleRecoveryStart)
                    {
                        moraleChange += idleRecoveryBonus;
                        _logger.Log($"[MoraleSystem] Employee {empId.Value} idle {idleRecoveryStart} days — relief bonus +{idleRecoveryBonus}");
                    }
                    else if (data.consecutiveIdleDays >= idleDecayStart)
                    {
                        float weeksOverThreshold = (data.consecutiveIdleDays - idleDecayStart) / 7f;
                        float scaledDecay = idleDecayRate + weeksOverThreshold * 0.1f;
                        float maxDecay = _tuning != null ? _tuning.IdleDecayMax : 2.0f;
                        if (scaledDecay > maxDecay) scaledDecay = maxDecay;
                        moraleChange -= scaledDecay;
                    }
                    else if (data.consecutiveIdleDays >= idleBoredomStart)
                    {
                        moraleChange -= idleBoredomRate;
                    }
                } else {
                    // Team has work — reset idle alert and idle counter
                    if (data.idleAlertSent) {
                        data.idleAlertSent = false;
                    }
                    data.consecutiveIdleDays = 0;

                    moraleChange += workingBonus;

                    if (teamHasContract) {
                        // Get the assigned contract
                        ContractId contractId = _contractState.teamAssignments[teamId];
                        if (_contractState.activeContracts.TryGetValue(contractId, out var contract)) {
                            // Overload penalty
                            int optimal = contract.OptimalContributors > 0 ? contract.OptimalContributors : 1;
                            int memberCount = _teamState.teams.TryGetValue(teamId, out var contractTeam) ? contractTeam.MemberCount : 1;
                            if (memberCount < (int)(optimal * 0.5f))        moraleChange -= overloadSevere;
                            else if (memberCount < (int)(optimal * 0.75f))  moraleChange -= overloadModerate;
                            else if (memberCount < optimal)                  moraleChange -= overloadMild;
                            // else: optimal or above, no penalty

                            // Wrong-function penalty
                            bool isRoleFit = TeamWorkEngine.IsRoleFitForSkill(employee.role, contract.RequiredSkill);
                            if (!isRoleFit) {
                                int skillValue = employee.GetSkill(contract.RequiredSkill);
                                if (skillValue == 0)      moraleChange -= wrongFuncSevere;
                                else if (skillValue < 3)  moraleChange -= wrongFuncModerate;
                                else                      moraleChange -= wrongFuncMild;
                            }

                            // Job satisfaction bonus
                            if (isRoleFit) {
                                if (memberCount >= optimal)
                                    moraleChange += satisfactionFull;
                                else if (memberCount >= (int)(optimal * 0.75f))
                                    moraleChange += satisfactionPartial;
                            }
                        }
                    } else if (teamHasProduct && _productState.teamToProduct.TryGetValue(teamId, out var productId)) {
                        Product product = null;
                        if (!_productState.developmentProducts.TryGetValue(productId, out product)) {
                            _productState.shippedProducts.TryGetValue(productId, out product);
                        }

                        if (product != null && product.Phases != null) {
                            int phaseCount = product.Phases.Length;

                            // Count unlocked phases to determine optimal baseline
                            int unlockedPhaseCount = 0;
                            for (int p = 0; p < phaseCount; p++) {
                                if (product.Phases[p].isUnlocked)
                                    unlockedPhaseCount++;
                            }

                            // Overload: use unlocked phase count as organic optimal baseline (min 2)
                            int memberCount = _teamState.teams.TryGetValue(teamId, out var productTeam) ? productTeam.MemberCount : 1;
                            int optimal = Math.Max(2, unlockedPhaseCount);
                            if (memberCount < (int)(optimal * 0.5f))        moraleChange -= prodOverloadSevere;
                            else if (memberCount < (int)(optimal * 0.75f))  moraleChange -= prodOverloadModerate;
                            else if (memberCount < optimal)                  moraleChange -= prodOverloadMild;

                            // Phase-matching wrong-function penalty:
                            // Check if the employee's role fits ANY phase in the product, not just the active one.
                            bool employeeHasMatchingPhase = false;
                            for (int p = 0; p < phaseCount; p++) {
                                SkillType phaseSkill = TeamWorkEngine.MapPhaseToSkill(product.Phases[p].phaseType);
                                if (TeamWorkEngine.IsRoleFitForSkill(employee.role, phaseSkill)) {
                                    employeeHasMatchingPhase = true;
                                    break;
                                }
                            }

                            if (!employeeHasMatchingPhase) {
                                // Role has no relevance to any phase of this product — flat penalty
                                moraleChange -= 1.0f;
                            }
                            // else: role matches at least one phase — no wrong-function penalty

                            // Product satisfaction bonus
                            if (employeeHasMatchingPhase) {
                                if (memberCount >= optimal)
                                    moraleChange += prodSatFull;
                                else if (memberCount >= (int)(optimal * 0.75f))
                                    moraleChange += prodSatPartial;
                            }
                        }
                    }
                }
            }

            bool teamIsCrunching = false;
            if (isOnTeam && _teamState.teams.TryGetValue(teamId, out var crunchTeam))
                teamIsCrunching = crunchTeam.isCrunching;

            if (teamIsCrunching) {
                data.crunchDaysActive++;
                if (data.recentCrunchDays < 14) data.recentCrunchDays++;

                float crunchPenalty;
                if (data.crunchDaysActive <= 2)       crunchPenalty = 0.5f;
                else if (data.crunchDaysActive <= 5)  crunchPenalty = 1.5f;
                else if (data.crunchDaysActive <= 9)  crunchPenalty = 2.5f;
                else                                  crunchPenalty = 4.0f;

                moraleChange -= crunchPenalty;

                if (data.recentCrunchDays >= 8)
                    moraleChange -= 1.0f;
            } else {
                if (data.crunchDaysActive > 0) {
                    data.crunchDaysActive = 0;
                }
                if (data.recentCrunchDays > 0) {
                    data.recentCrunchDays--;
                    moraleChange += 1.5f;
                }
            }

            if (moraleChange < -penaltyFloor)
                moraleChange = -penaltyFloor;

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

    public void PreTick(int tick) {
    }

    public void Tick(int tick) {
    }

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

    public void ApplyCommand(ICommand command) {
    }

    public void Dispose() {
        _moraleChangedBuffer.Clear();
        _mayQuitBuffer.Clear();
        _idleAlertBuffer.Clear();
        OnMoraleChanged = null;
        OnEmployeeMayQuit = null;
    }
}
