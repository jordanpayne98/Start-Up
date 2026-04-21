// ContractSystem Version: Clean v1
using System;
using System.Collections.Generic;

public struct SkillContribution
{
    public float[] Contributions;
    public float TotalWorkThisTick;

    public SkillContribution(SkillType skill, float amount)
    {
        Contributions = new float[SkillTypeHelper.SkillTypeCount];
        Contributions[(int)skill] = amount;
        TotalWorkThisTick = amount;
    }

    public float ProgrammingContribution => Contributions[(int)SkillType.Programming];
    public float DesignContribution => Contributions[(int)SkillType.Design];
    public float QAContribution => Contributions[(int)SkillType.QA];
}

public class ContractSystem : ISystem
{
    private const float FallbackWorkRatePerSkillPoint = 0.016f;

    private TuningConfig _tuning;

    public event Action<ContractId> OnContractAccepted;
    public event Action<ContractId, TeamId> OnContractAssigned;
    public event Action<ContractId, TeamId> OnContractUnassigned;
    public event Action<ContractId> OnContractProgressUpdated;
    public event Action<ContractId, int, int, float> OnContractCompleted;
    public event Action<ContractId, string, int> OnContractFailed;
    public event Action<ContractId> OnContractExpired;
    // Fired after SkillGrowthSystem awards XP so listeners can invalidate CA caches
    public event Action<List<EmployeeId>> OnSkillsAwarded;
    public event Action OnPoolRerolled;

    public bool HadCompletionOrFailureThisTick { get; private set; }

    private enum PendingEventType : byte
    {
        ContractAccepted,
        ContractAssigned,
        ContractUnassigned,
        ContractProgressUpdated,
        ContractCompleted,
        ContractFailed,
        ContractExpired,
        SkillsAwarded,
        PoolRerolled,
    }

    private struct PendingEvent
    {
        public PendingEventType Type;
        public ContractId ContractId;
        public TeamId TeamId;
        public int IntA;
        public int IntB;
        public float FloatA;
        public string StringA;
        public List<EmployeeId> EmployeeList;
    }

    private ContractState _state;
    private ContractFactory _factory;
    private TeamSystem _teamSystem;
    private EmployeeSystem _employeeSystem;
    private FinanceSystem _financeSystem;
    private ReputationSystem _reputationSystem;
    private MoraleSystem _moraleSystem;
    private IRng _rng;
    private ILogger _logger;
    private readonly List<PendingEvent> _pendingEvents;
    private readonly List<ContractId> _scratchIds;
    private readonly List<ContractId> _completedOrFailedIds;
    private readonly int[] _poolSkillCountScratch;
    private IReadOnlyCollection<string> _unlockedUpgrades;
    private RoleTierTable _roleTierTable;
    private AbilitySystem _abilitySystem;
    private ProductSystem _productSystem;

    public int AvailableContractCount => _state.availableContracts.Count;
    public int ActiveContractCount => _state.activeContracts.Count;
    public int LastPoolRefreshTick => _state.lastPoolRefreshTick;
    public int PoolRefreshIntervalTicks => _state.poolRefreshIntervalTicks;
    public int RerollsUsedThisCycle => _state.rerollsUsedThisCycle;
    public bool CanReroll => _state.rerollsUsedThisCycle < 1;

    public ContractSystem(
        ContractState state,
        ContractFactory factory,
        TeamSystem teamSystem,
        EmployeeSystem employeeSystem,
        FinanceSystem financeSystem,
        IRng rng,
        ILogger logger,
        ReputationSystem reputationSystem = null)
    {
        _state = state ?? throw new ArgumentNullException(nameof(state));
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        _teamSystem = teamSystem ?? throw new ArgumentNullException(nameof(teamSystem));
        _employeeSystem = employeeSystem ?? throw new ArgumentNullException(nameof(employeeSystem));
        _financeSystem = financeSystem ?? throw new ArgumentNullException(nameof(financeSystem));
        _rng = rng ?? throw new ArgumentNullException(nameof(rng));
        _logger = logger ?? new NullLogger();
        _reputationSystem = reputationSystem;
        _pendingEvents = new List<PendingEvent>(16);
        _scratchIds = new List<ContractId>(32);
        _completedOrFailedIds = new List<ContractId>(8);
        _poolSkillCountScratch = new int[SkillTypeHelper.SkillTypeCount];

        _teamSystem.OnTeamDeleted += OnTeamDeleted;
    }

    public void SetTuningConfig(TuningConfig tuning)
    {
        _tuning = tuning;
    }

    public void SetReputationSystem(ReputationSystem reputationSystem)
    {
        _reputationSystem = reputationSystem;
    }

    public void SetUnlockedUpgrades(IReadOnlyCollection<string> upgrades)
    {
        _unlockedUpgrades = upgrades;
    }

    public void SetSkillGrowthDependencies(RoleTierTable roleTierTable, AbilitySystem abilitySystem)
    {
        _roleTierTable = roleTierTable;
        _abilitySystem = abilitySystem;
    }

    public void SetProductSystem(ProductSystem productSystem)
    {
        _productSystem = productSystem;
    }

    public void SetMoraleSystem(MoraleSystem moraleSystem)
    {
        _moraleSystem = moraleSystem;
    }

    public void RefreshContractPool(int currentTick)
    {
        int maxSlots = _state.maxAvailableContracts;
        if (maxSlots < 1) maxSlots = 1;

        // Zero out scratch buffer
        for (int i = 0; i < _poolSkillCountScratch.Length; i++)
            _poolSkillCountScratch[i] = 0;

        // Count dominant skills from existing available contracts
        foreach (var kvp in _state.availableContracts)
        {
            int skillIdx = (int)kvp.Value.RequiredSkill;
            if (skillIdx >= 0 && skillIdx < _poolSkillCountScratch.Length)
                _poolSkillCountScratch[skillIdx]++;
        }

        while (_state.availableContracts.Count < maxSlots)
        {
            int difficultyCap = _reputationSystem != null
                ? _reputationSystem.GetEffectiveContractDifficultyCap()
                : 10;

            Contract contract = _factory.GenerateContract(
                currentTick,
                difficultyCap,
                _unlockedUpgrades ?? Array.Empty<string>(),
                _poolSkillCountScratch);

            if (contract == null)
            {
                _logger.LogWarning("[ContractSystem] Factory returned null — no available categories.");
                break;
            }

            int skillIdx = (int)contract.RequiredSkill;
            if (skillIdx >= 0 && skillIdx < _poolSkillCountScratch.Length)
                _poolSkillCountScratch[skillIdx]++;

            var contractId = new ContractId(_state.nextContractId++);
            contract.Id = contractId;
            _state.availableContracts[contractId] = contract;

            _logger.Log($"[Tick {currentTick}] Generated contract '{contract.Name}' (ID: {contractId.Value}, Category: {contract.CategoryId})");
        }
    }
    
    public bool AcceptContract(ContractId id, int currentTick)
    {
        if (!_state.availableContracts.TryGetValue(id, out var contract))
        {
            _logger.LogWarning($"Cannot accept contract {id.Value}: Not found in available pool");
            return false;
        }

        _state.availableContracts.Remove(id);
        _state.activeContracts[id] = contract;

        contract.Status = ContractStatus.Accepted;
        contract.AcceptedTick = currentTick;
        contract.DeadlineTick = currentTick + contract.DeadlineDurationTicks;

        var capturedId = id;
        _pendingEvents.Add(new PendingEvent { Type = PendingEventType.ContractAccepted, ContractId = capturedId });
        _logger.Log($"[Tick {currentTick}] Accepted contract '{contract.Name}' (ID: {id.Value})");
        RefreshContractPool(currentTick);
        TryAutoAssignTeam(id);
        return true;
    }
    
    public bool AssignTeamToContract(ContractId contractId, TeamId teamId)
    {
        if (!_state.activeContracts.TryGetValue(contractId, out var contract))
        {
            _logger.LogWarning($"Cannot assign team to contract {contractId.Value}: Contract not found or not accepted");
            return false;
        }

        if (contract.Status != ContractStatus.Accepted && contract.Status != ContractStatus.InProgress)
        {
            _logger.LogWarning($"Cannot assign team to contract {contractId.Value}: Contract status is {contract.Status}");
            return false;
        }

        if (_teamSystem.GetTeam(teamId) == null)
        {
            _logger.LogWarning($"Cannot assign team {teamId.Value} to contract: Team not found");
            return false;
        }

        if (_teamSystem.GetTeamType(teamId) != TeamType.Contracts)
        {
            _logger.LogWarning($"Cannot assign team {teamId.Value} to contract: Only Contracts teams can be assigned to contracts");
            return false;
        }

        if (_productSystem != null && _productSystem.IsTeamAssignedToProduct(teamId))
        {
            _logger.LogWarning($"Cannot assign team {teamId.Value} to contract: team is already assigned to a product");
            return false;
        }

        if (_state.teamAssignments.TryGetValue(teamId, out var existingContractId))
        {
            if (existingContractId == contractId)
            {
                _logger.LogWarning($"Team {teamId.Value} is already assigned to contract {contractId.Value}");
                return false;
            }
            UnassignTeamFromContract(existingContractId);
        }

        contract.AssignedTeamId = teamId;
        contract.Status = ContractStatus.InProgress;
        _state.teamAssignments[teamId] = contractId;

        var capturedContractId = contractId;
        var capturedTeamId = teamId;
        _pendingEvents.Add(new PendingEvent { Type = PendingEventType.ContractAssigned, ContractId = capturedContractId, TeamId = capturedTeamId });
        _logger.Log($"Assigned team {teamId.Value} to contract '{contract.Name}' (ID: {contractId.Value})");
        return true;
    }

    public bool UnassignTeamFromContract(ContractId contractId)
    {
        if (!_state.activeContracts.TryGetValue(contractId, out var contract))
        {
            _logger.LogWarning($"Cannot unassign team from contract {contractId.Value}: Contract not found");
            return false;
        }

        if (contract.AssignedTeamId == null)
        {
            _logger.LogWarning($"Cannot unassign team from contract {contractId.Value}: No team assigned");
            return false;
        }

        TeamId teamId = contract.AssignedTeamId.Value;
        contract.AssignedTeamId = null;
        contract.Status = ContractStatus.Accepted;
        _state.teamAssignments.Remove(teamId);
        _teamSystem.NotifyTeamFreed(teamId);

        var capturedContractId = contractId;
        var capturedTeamId = teamId;
        _pendingEvents.Add(new PendingEvent { Type = PendingEventType.ContractUnassigned, ContractId = capturedContractId, TeamId = capturedTeamId });
        _logger.Log($"Unassigned team {teamId.Value} from contract '{contract.Name}' (ID: {contractId.Value})");
        return true;
    }

    private void TickActiveContracts(int tick)
    {
        _scratchIds.Clear();
        foreach (var kvp in _state.activeContracts)
            _scratchIds.Add(kvp.Key);

        _completedOrFailedIds.Clear();

        int count = _scratchIds.Count;
        for (int i = 0; i < count; i++)
        {
            ContractId id = _scratchIds[i];
            if (!_state.activeContracts.TryGetValue(id, out var contract)) continue;
            if (contract.Status != ContractStatus.InProgress) continue;
            if (contract.AssignedTeamId == null) continue;

            var team = _teamSystem.GetTeam(contract.AssignedTeamId.Value);
            if (team == null) continue;

            ProcessWork(contract, team, tick);

            var capturedId = id;
            _pendingEvents.Add(new PendingEvent { Type = PendingEventType.ContractProgressUpdated, ContractId = capturedId });

            if (contract.WorkCompleted >= contract.TotalWorkRequired)
                _completedOrFailedIds.Add(id);
        }

        int doneCount = _completedOrFailedIds.Count;
        for (int i = 0; i < doneCount; i++)
            CompleteContract(_completedOrFailedIds[i], tick);
    }

    private void ProcessWork(Contract contract, Team team, int tick)
    {
        int optimalTeamSize = System.Math.Max(2, (int)(contract.TotalWorkRequired / 500f));
        var teamResult = TeamWorkEngine.AggregateTeam(
            team.members,
            _employeeSystem,
            contract.RequiredSkill,
            _roleTierTable,
            _tuning?.TeamOverheadPerMember ?? 0.04f,
            optimalTeamSize: optimalTeamSize);

        int resolvedMin     = contract.MinContributors > 0 ? contract.MinContributors : 1;
        int resolvedOptimal = contract.OptimalContributors > 0 ? contract.OptimalContributors : resolvedMin + 1;

        int contributors = teamResult.Contributors;

        float speedCoverage;
        if (contributors <= 0)
            speedCoverage = 0f;
        else if (contributors < resolvedOptimal)
            speedCoverage = 0.60f + 0.40f * (contributors / (float)resolvedOptimal);
        else
            speedCoverage = 1.0f;

        if (contributors > resolvedOptimal) {
            float excessRatio = (float)(contributors - resolvedOptimal) / resolvedOptimal;
            float diminishingFactor = 1f / (1f + excessRatio * 0.6f);
            speedCoverage *= diminishingFactor;
        }

        float speedRangeMult = TeamWorkEngine.ComputeSpeedRangeMultiplier(
            teamResult.AvgQualitySkill,
            contract.MinSkillRequired,
            contract.TargetSkill,
            contract.ExcellenceSkill);

        float workRate = _tuning != null ? _tuning.WorkRatePerSkillPoint : FallbackWorkRatePerSkillPoint;
        float variance = 0.95f + _rng.NextFloat01() * 0.10f;

        float crunchMult = team.isCrunching ? 1.15f : 1f;

        float workThisTick = TeamWorkEngine.ComputeWorkPerTick(
            in teamResult,
            workRate,
            speedCoverage * speedRangeMult,
            variance,
            GetPhaseSkillUpgradeMultiplier(contract.RequiredSkill) * crunchMult);

        contract.WorkCompleted += workThisTick;
        if (contract.WorkCompleted > contract.TotalWorkRequired)
            contract.WorkCompleted = contract.TotalWorkRequired;

        float quality = TeamWorkEngine.ComputeQuality(
            teamResult.AvgQualitySkill,
            contract.MinSkillRequired,
            contract.TargetSkill,
            contract.ExcellenceSkill,
            teamResult.CoverageQualityMod,
            teamResult.AvgMorale);
        contract.QualityScore = quality;
#if UNITY_EDITOR
        UnityEngine.Debug.Log($"[ContractSystem] {contract.Name} (id={contract.Id.Value}) | AvgQualitySkill={teamResult.AvgQualitySkill:F3} Contributors={teamResult.Contributors}/{teamResult.ActiveCount} coverageMod={teamResult.CoverageQualityMod:F3} quality={quality:F2}%");
#endif
    }

    private bool CompleteContract(ContractId id, int currentTick)
    {
        if (!_state.activeContracts.TryGetValue(id, out var contract))
            return false;

        float overallQuality = contract.QualityScore;
        int qualityThreshold = contract.QualityThreshold;

        // Quality gate: below threshold = reduced payout + treated as failure
        bool qualityPassed = overallQuality >= qualityThreshold;

        float qualityMultiplier;
        if (overallQuality <= 40f)
            qualityMultiplier = Lerp(0.40f, 0.70f, overallQuality / 40f);
        else if (overallQuality <= 70f)
            qualityMultiplier = Lerp(0.70f, 1.00f, (overallQuality - 40f) / 30f);
        else if (overallQuality <= 90f)
            qualityMultiplier = Lerp(1.00f, 1.25f, (overallQuality - 70f) / 20f);
        else
            qualityMultiplier = Lerp(1.25f, 1.50f, (overallQuality - 90f) / 10f);

        float upgradeRewardMult = 1f;

        int grossReward = (int)(contract.RewardMoney * qualityMultiplier * upgradeRewardMult);
        int finalRepReward = (int)(contract.ReputationReward * qualityMultiplier);

        if (qualityPassed)
        {
            // Stretch goal bonus: quality must exceed threshold + 20 (capped at 100)
            if (contract.HasStretchGoal && contract.StretchGoalActivated)
            {
                int stretchThreshold = Math.Min(qualityThreshold + 20, 100);
                if (overallQuality >= stretchThreshold)
                {
                    grossReward = (int)(grossReward * 1.4f);
                    finalRepReward = (int)(finalRepReward * 1.3f);
                    _logger.Log($"[Tick {currentTick}] Stretch goal achieved for '{contract.Name}' (quality {overallQuality:F1}% >= {stretchThreshold}%)");
                }
            }

            if (grossReward > 0)
                _financeSystem.AddMoney(grossReward);

            contract.RewardMoney = grossReward;
            contract.Status = ContractStatus.Completed;

            // Award skill XP to team members before unassigning
            if (contract.AssignedTeamId != null)
            {
                var assignedTeam = _teamSystem.GetTeam(contract.AssignedTeamId.Value);
                if (assignedTeam != null)
                {
                    SkillGrowthSystem.AwardSkillXP(contract, assignedTeam, _employeeSystem, _rng, _roleTierTable, _abilitySystem, _tuning);

                    // Invalidate CA cache for all team members so star ratings update
                    var membersForEvent = new List<EmployeeId>(assignedTeam.members);
                    _pendingEvents.Add(new PendingEvent { Type = PendingEventType.SkillsAwarded, EmployeeList = membersForEvent });

                    if (assignedTeam.isCrunching) {
                        assignedTeam.isCrunching = false;
                        _moraleSystem?.ResetCrunchTracking(assignedTeam.members);
                    }
                }

                _state.teamAssignments.Remove(contract.AssignedTeamId.Value);
                _teamSystem.NotifyTeamFreed(contract.AssignedTeamId.Value);
                contract.AssignedTeamId = null;
            }

            _state.activeContracts.Remove(id);
            HadCompletionOrFailureThisTick = true;

            var capturedId = id;
            int capturedReward = grossReward;
            int capturedRepReward2 = finalRepReward;
            float capturedQuality = overallQuality;
            _pendingEvents.Add(new PendingEvent { Type = PendingEventType.ContractCompleted, ContractId = capturedId, IntA = capturedReward, IntB = capturedRepReward2, FloatA = capturedQuality });
            _logger.Log($"[Tick {currentTick}] Completed '{contract.Name}' — Quality: {overallQuality:F1}% (threshold: {qualityThreshold}%), Reward: ${grossReward}, Rep: {finalRepReward}");
        }
        else
        {
            // Quality below threshold: proportional reduced payout
            float qualityRatio = qualityThreshold > 0 ? overallQuality / qualityThreshold : 0f;
            int reducedReward = (int)(grossReward * qualityRatio * 0.5f);
            int reducedRepReward = (int)(finalRepReward * qualityRatio * 0.5f);

            if (reducedReward > 0)
                _financeSystem.AddMoney(reducedReward);

            contract.RewardMoney = reducedReward;
            contract.Status = ContractStatus.Failed;

            if (contract.AssignedTeamId != null)
            {
                _state.teamAssignments.Remove(contract.AssignedTeamId.Value);
                _teamSystem.NotifyTeamFreed(contract.AssignedTeamId.Value);
                contract.AssignedTeamId = null;
            }

            _state.activeContracts.Remove(id);
            HadCompletionOrFailureThisTick = true;

            int reputationPenalty2 = contract.Difficulty * 5;
            var capturedId2 = id;
            string capturedName2 = contract.Name;
            int capturedPenalty2 = reputationPenalty2;
            _pendingEvents.Add(new PendingEvent { Type = PendingEventType.ContractFailed, ContractId = capturedId2, StringA = capturedName2, IntA = capturedPenalty2 });
            _logger.Log($"[Tick {currentTick}] Failed '{contract.Name}' — Quality: {overallQuality:F1}% below threshold {qualityThreshold}%. Partial payout: ${reducedReward}");
        }

        return true;
    }

    private void CheckDeadlines(int currentTick)
    {
        if (currentTick >= _state.lastPoolRefreshTick + _state.poolRefreshIntervalTicks)
            CycleContractPool(currentTick);

        _scratchIds.Clear();
        foreach (var kvp in _state.activeContracts)
            _scratchIds.Add(kvp.Key);

        int count = _scratchIds.Count;
        for (int i = 0; i < count; i++)
        {
            ContractId id = _scratchIds[i];
            if (!_state.activeContracts.TryGetValue(id, out var contract)) continue;
            if (contract.Status == ContractStatus.Completed) continue;
            if (contract.DeadlineTick < 0) continue;
            if (currentTick >= contract.DeadlineTick)
                FailContract(id, currentTick, "Missed deadline");
        }
    }

    private void CycleContractPool(int currentTick)
    {
        _scratchIds.Clear();
        foreach (var kvp in _state.availableContracts)
            _scratchIds.Add(kvp.Key);

        if (_scratchIds.Count > 0)
        {
            int countToRotate = (_scratchIds.Count + 1) / 2;
            if (countToRotate < 1) countToRotate = 1;

            for (int i = _scratchIds.Count - 1; i > 0; i--)
            {
                int j = _rng.Range(0, i + 1);
                ContractId temp = _scratchIds[i];
                _scratchIds[i] = _scratchIds[j];
                _scratchIds[j] = temp;
            }

            for (int i = 0; i < countToRotate && i < _scratchIds.Count; i++)
            {
                ContractId cid = _scratchIds[i];
                _state.availableContracts.Remove(cid);
                _logger.Log($"[Tick {currentTick}] Pool cycle: removed contract (ID: {cid.Value})");
            }
        }

        RefreshContractPool(currentTick);
        _state.lastPoolRefreshTick = currentTick;
        _state.rerollsUsedThisCycle = 0;
        _logger.Log($"[Tick {currentTick}] Contract pool cycled.");
    }

    public bool RerollContractPool(int currentTick)
    {
        if (_state.rerollsUsedThisCycle >= 1)
        {
            _logger.Log($"[Tick {currentTick}] Reroll denied: already used this cycle.");
            return false;
        }
        CycleContractPool(currentTick);
        _state.rerollsUsedThisCycle++;
        _pendingEvents.Add(new PendingEvent { Type = PendingEventType.PoolRerolled });
        _logger.Log($"[Tick {currentTick}] Contract pool rerolled.");
        return true;
    }

    private void FailContract(ContractId id, int currentTick, string reason)
    {
        if (!_state.activeContracts.TryGetValue(id, out var contract)) return;

        string contractName = contract.Name;
        int reputationPenalty = contract.Difficulty * 10;
        contract.Status = ContractStatus.Failed;

        if (contract.AssignedTeamId != null)
        {
            _state.teamAssignments.Remove(contract.AssignedTeamId.Value);
            _teamSystem.NotifyTeamFreed(contract.AssignedTeamId.Value);
            contract.AssignedTeamId = null;
        }

        _state.activeContracts.Remove(id);
        HadCompletionOrFailureThisTick = true;

        var capturedId = id;
        string capturedName = contractName;
        int capturedPenalty = reputationPenalty;
        _pendingEvents.Add(new PendingEvent { Type = PendingEventType.ContractFailed, ContractId = capturedId, StringA = capturedName, IntA = capturedPenalty });
        _logger.Log($"[Tick {currentTick}] Failed contract '{contractName}' (ID: {id.Value}) — Reason: {reason}");
    }

    private void OnTeamDeleted(TeamId teamId)
    {
        if (_state.teamAssignments.TryGetValue(teamId, out var contractId))
        {
            if (_state.activeContracts.ContainsKey(contractId))
                FailContract(contractId, 0, "Team deleted");
        }
    }

    public Contract GetContract(ContractId id)
    {
        if (_state.availableContracts.TryGetValue(id, out var contract)) return contract;
        if (_state.activeContracts.TryGetValue(id, out contract)) return contract;
        return null;
    }

    public TeamFitResult GetTeamFitPrediction(ContractId contractId, TeamId teamId)
    {
        var contract = GetContract(contractId);
        if (contract == null) return default;
        var team = _teamSystem.GetTeam(teamId);
        if (team == null) return default;
        var employees = _employeeSystem.GetAllActiveEmployees();
        return ContractPredictionHelper.Predict(contract, team, employees);
    }

    public IEnumerable<Contract> GetAvailableContracts() => _state.availableContracts.Values;
    public IEnumerable<Contract> GetActiveContracts() => _state.activeContracts.Values;

    public Contract GetContractForTeam(TeamId teamId)
    {
        if (_state.teamAssignments.TryGetValue(teamId, out var contractId))
            return GetContract(contractId);
        return null;
    }

    public bool CanAcceptMoreContracts() => true;

    public void PreTick(int tick)
    {
        HadCompletionOrFailureThisTick = false;
    }

    public void Tick(int tick)
    {
        TickActiveContracts(tick);
        CheckDeadlines(tick);
    }

    public void PostTick(int tick)
    {
        int count = _pendingEvents.Count;
        for (int i = 0; i < count; i++)
        {
            PendingEvent e = _pendingEvents[i];
            switch (e.Type)
            {
                case PendingEventType.ContractAccepted:        OnContractAccepted?.Invoke(e.ContractId); break;
                case PendingEventType.ContractAssigned:        OnContractAssigned?.Invoke(e.ContractId, e.TeamId); break;
                case PendingEventType.ContractUnassigned:      OnContractUnassigned?.Invoke(e.ContractId, e.TeamId); break;
                case PendingEventType.ContractProgressUpdated: OnContractProgressUpdated?.Invoke(e.ContractId); break;
                case PendingEventType.ContractCompleted:       OnContractCompleted?.Invoke(e.ContractId, e.IntA, e.IntB, e.FloatA); break;
                case PendingEventType.ContractFailed:          OnContractFailed?.Invoke(e.ContractId, e.StringA, e.IntA); break;
                case PendingEventType.ContractExpired:         OnContractExpired?.Invoke(e.ContractId); break;
                case PendingEventType.SkillsAwarded:           OnSkillsAwarded?.Invoke(e.EmployeeList); break;
                case PendingEventType.PoolRerolled:            OnPoolRerolled?.Invoke(); break;
            }
        }
        _pendingEvents.Clear();
    }

    public void ApplyCommand(ICommand command)
    {
        if (command is AcceptContractCommand acceptContract)
            AcceptContract(acceptContract.ContractId, command.Tick);
        else if (command is AssignTeamToContractCommand assignTeam)
            AssignTeamToContract(assignTeam.ContractId, assignTeam.TeamId);
        else if (command is UnassignTeamFromContractCommand unassignTeam)
            UnassignTeamFromContract(unassignTeam.ContractId);
        else if (command is ActivateStretchGoalCommand stretch)
            ActivateStretchGoal(stretch.ContractId);
        else if (command is CompleteContractCommand completeContract)
            DebugCompleteContract(completeContract.ContractId, command.Tick);
        else if (command is RerollContractPoolCommand)
            RerollContractPool(command.Tick);
    }

    private void TryAutoAssignTeam(ContractId contractId)
    {
        var candidates = _teamSystem.GetFreeTeamsByType(TeamType.Contracts);
        int count = candidates.Count;
        for (int i = 0; i < count; i++)
        {
            TeamId candidateId = candidates[i];
            if (_state.teamAssignments.ContainsKey(candidateId)) continue;
            if (_productSystem != null && _productSystem.IsTeamAssignedToProduct(candidateId)) continue;
            var team = _teamSystem.GetTeam(candidateId);
            if (team == null || team.members.Count == 0) continue;
            AssignTeamToContract(contractId, candidateId);
            _logger.Log($"[ContractSystem] Auto-assigned team {candidateId.Value} to contract {contractId.Value}");
            return;
        }
        _logger.Log($"[ContractSystem] No free Contracts team available for auto-assign to contract {contractId.Value}");
    }

    public void Dispose()
    {
        if (_teamSystem != null)
            _teamSystem.OnTeamDeleted -= OnTeamDeleted;
    }

    private void ActivateStretchGoal(ContractId contractId)
    {
        if (!_state.activeContracts.TryGetValue(contractId, out var contract)) return;
        if (!contract.HasStretchGoal || contract.StretchGoalActivated) return;
        contract.StretchGoalActivated = true;
        _logger.Log($"Stretch goal activated for contract '{contract.Name}' (ID: {contractId.Value})");
    }

    private void DebugCompleteContract(ContractId contractId, int tick)
    {
        if (!_state.activeContracts.TryGetValue(contractId, out var contract)) return;
        contract.WorkCompleted = contract.TotalWorkRequired;
        contract.QualityScore = 80f;
        _logger.Log($"[DEBUG] Force-completing contract {contractId.Value}");
        CompleteContract(contractId, tick);
    }

    private float GetPhaseSkillUpgradeMultiplier(SkillType skill)
    {
        return 1f;
    }

    private static float Lerp(float a, float b, float t)
    {
        if (t < 0f) t = 0f;
        if (t > 1f) t = 1f;
        return a + (b - a) * t;
    }

}
