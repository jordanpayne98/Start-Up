// GameController Version: Clean v1
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class GameController : MonoBehaviour
{
    [Header("Simulation Settings")]
    [SerializeField] private bool startPaused = false;

    [Header("Advance Settings")]
    [SerializeField] private int ticksPerFrame = 480; // 480 = 0.1 sim-hours/frame @ 60fps
    
    private GameState _gameState;
    private List<ISystem> _systems;
    private ILogger _logger;
    private GameEventBus _eventBus;
    
    [Header("Contract Categories")]
    [SerializeField] private ContractCategoryDefinition[] contractCategories;

    [Header("Product Templates")]
    [SerializeField] private ProductTemplateDefinition[] productTemplates;

    [Header("Review System")]
    [SerializeField] private ReviewOutletDefinition[] reviewOutlets;

    [Header("Market Data")]
    [SerializeField] private MarketNicheData[] marketNiches;

    [Header("Competitor Data")]
    [SerializeField] private CompetitorArchetypeConfig[] competitorArchetypeConfigs;
    [SerializeField] private CompetitorStartConfig[] competitorStartConfigs;
    [SerializeField] private CompetitorNameData competitorNameData;

    [Header("Generation Data")]
    [SerializeField] private ArchitectureGenerationDefinition[] architectureGenerations;

    [Header("Hardware Data")]
    [SerializeField] private HardwareGenerationConfig[] hardwareGenerationConfigs;

    [Header("Cross-Product Gate Config")]
    [SerializeField] private CrossProductGateConfig crossProductGateConfig;
    
    private TimeSystem _timeSystem;
    private FinanceSystem _financeSystem;
    private EmployeeSystem _employeeSystem;
    private TeamSystem _teamSystem;
    private ContractSystem _contractSystem;
    private ContractFactory _contractFactory;
    private ReputationSystem _reputationSystem;
    private MoraleSystem _moraleSystem;
    private FatigueSystem _fatigueSystem;
    private LoanSystem _loanSystem;
    private InterviewSystem _interviewSystem;
    private NegotiationSystem _negotiationSystem;
    private HRSystem _hrSystem;
    private RecruitmentReputationSystem _recruitmentReputationSystem;
    private IRng _moraleRng;
    private InboxSystem _inboxSystem;
    private RoleProfileTable _roleProfileTable;
    private AbilitySystem _abilitySystem;
    private TuningConfig _tuning;
    private ProductSystem _productSystem;
    private MarketSystem _marketSystem;
    private AutoSaveSystem _autoSaveSystem;
    private CompetitorSystem _competitorSystem;
    private CompetitorContractBridge _competitorContractBridge;
    private StockSystem _stockSystem;
    
    private AIDecisionSystem _aiDecisionSystem;
    private TeamChemistrySystem _teamChemistrySystem;
    private IRng _chemistryRng;
    private DisruptionSystem _disruptionSystem;
    private TaxSystem _taxSystem;
    private PlatformSystem _platformSystem;
    private GenerationSystem _generationSystem;
    private bool _isAdvancing;
    private bool _stopAdvanceRequested;
    private bool _freshWorldGeneration;
    private Coroutine _advanceCoroutine;
    
    // Interrupt dirty flags — set by On* handlers after each SimTick batch
    private bool _interruptContractEvent;
    private bool _interruptCandidateRefresh;
    private bool _interruptFinanceNegative;
    private bool _interruptTierChange;
    private bool _interruptBankrupt;
    private bool _interruptHRSearchComplete;
    private bool _interruptAutoAction;
    private bool _interruptCandidateExpired;
    private bool _interruptInterviewComplete;
    private bool _interruptMarketDemand;
    private bool _interruptCompetitorBankrupt;
    private bool _interruptCompetitorProduct;
    private bool _interruptPlayerAcquired;
    
    private Queue<ICommand> _commandQueue;
    
    public int CurrentTick => _gameState?.currentTick ?? 0;
    public bool IsAdvancing => _isAdvancing;
    public TimeSystem TimeSystem => _timeSystem;
    public FinanceSystem FinanceSystem => _financeSystem;
    public EmployeeSystem EmployeeSystem => _employeeSystem;
    public TeamSystem TeamSystem => _teamSystem;
    public ContractSystem ContractSystem => _contractSystem;
    public ReputationSystem ReputationSystem => _reputationSystem;
    public ILoanReadModel LoanReadModel => _loanSystem;
    public InterviewSystem InterviewSystem => _interviewSystem;
    public NegotiationSystem NegotiationSystem => _negotiationSystem;
    public HRSystem HRSystem => _hrSystem;
    public RecruitmentReputationSystem RecruitmentReputationSystem => _recruitmentReputationSystem;
    public GameEventBus EventBus => _eventBus;
    public InboxSystem InboxSystem => _inboxSystem;
    public AbilitySystem AbilitySystem => _abilitySystem;
    public ProductSystem ProductSystem => _productSystem;
    public MarketSystem MarketSystem => _marketSystem;
    public CompetitorSystem CompetitorSystem => _competitorSystem;
    public StockSystem StockSystem => _stockSystem;
    public DisruptionSystem DisruptionSystem => _disruptionSystem;
    public TaxSystem TaxSystem => _taxSystem;
    public PlatformSystem PlatformSystem => _platformSystem;
    public GenerationSystem GenerationSystem => _generationSystem;
    public FatigueSystem FatigueSystem => _fatigueSystem;

    public TeamChemistrySystem TeamChemistrySystem => _teamChemistrySystem;
    public ProductTemplateDefinition[] ProductTemplates => productTemplates;
    public MarketNicheData[] MarketNiches => marketNiches;
    public HardwareGenerationConfig[] HardwareGenerationConfigs => hardwareGenerationConfigs;
    public CrossProductGateConfig CrossProductGateConfig => crossProductGateConfig;
    
    public GameState GetGameState() => _gameState;
    public TuningConfig Tuning => _tuning;
    
    private void Awake()
    {
        _commandQueue = new Queue<ICommand>();
        _systems = new List<ISystem>();
        _logger = new UnityLogger();
        _eventBus = new GameEventBus();
    }
    
    private void Start()
    {
        bool isNewGameWithFounders = NewGameData.IsNewGame;
        LoadOrCreateGameState();
        InitializeSystems();

        if (isNewGameWithFounders)
        {
            CreateFoundingEmployees();
            foreach (var kvp in _gameState.employeeState.employees)
            {
                if (kvp.Value != null && kvp.Value.isFounder)
                {
                    _abilitySystem.OnEmployeeHired(kvp.Key, kvp.Value);
                }
            }
        }

        if (_gameState.competitorState.competitors.Count == 0)
        {
            if (competitorStartConfigs != null && competitorStartConfigs.Length > 0)
                _competitorSystem.RegisterStartConfigs(competitorStartConfigs);
            IRng startRng = RngFactory.CreateStream(_gameState.masterSeed, "competitor-start");
            int competitorCount = startRng.Range(10, 16);
            _competitorSystem.GenerateStartingCompetitors(competitorCount, _gameState.masterSeed);
            _freshWorldGeneration = true;
        }

        _competitorSystem.MigrateGhostProducts();

        if (_gameState.stockState.listings.Count == 0) {
            _stockSystem.EnsureListingsForAll();
        }

        _marketSystem.ForceInitialShareResolution(_gameState.currentTick);
        _competitorSystem.SyncNicheMarketShares(_gameState.marketState);
        _competitorSystem.ForceInitialFinanceEval(_gameState.currentTick);

        // Seed previous-period snapshots so starting products don't show "New" trend
        foreach (var kvp in _gameState.productState.shippedProducts) {
            var p = kvp.Value;
            if (p.IsCompetitorProduct) {
                if (!_freshWorldGeneration) continue;
                p.PreviousDailyActiveUsers = p.ActiveUserCount;
                p.PreviousMonthActiveUsers = p.ActiveUserCount;
                p.PreviousMonthlyRevenue = p.MonthlyRevenue;
                if (p.HasCompletedFirstMonth) {
                    p.SnapshotMonthlyUsers = p.ActiveUserCount;
                    p.SnapshotMonthlyRevenue = (long)p.MonthlyRevenue;
                    int ageInMonths = p.TicksSinceShip / (TimeState.TicksPerDay * 30);
                    if (ageInMonths > 0) {
                        p.TotalLifetimeRevenue = EstimateShapedLifetimeTotal(p.MonthlyRevenue, p.TailDecayFactor, ageInMonths);
                        if (p.IsSubscriptionBased) {
                            p.SnapshotMonthlySales = p.TotalSubscribers;
                        } else {
                            int userBasedCap = Math.Max(1, (int)Math.Round(p.ActiveUserCount * 0.15f));
                            float unitPrice = _productSystem.GetCompetitorUnitPrice(p);
                            if (unitPrice > 0f) {
                                int currentMonthlySales = Math.Min((int)(p.MonthlyRevenue / unitPrice), userBasedCap);
                                p.SnapshotMonthlySales = currentMonthlySales;
                                p.PeakMonthlySales = Math.Max(p.PeakMonthlySales, currentMonthlySales);
                                long shapedUnits = EstimateShapedLifetimeTotal(currentMonthlySales, p.TailDecayFactor, ageInMonths);
                                p.TotalUnitsSold = (int)Math.Min(shapedUnits, int.MaxValue);
                                p.PreviousMonthUnitsSold = p.TotalUnitsSold;
                            } else {
                                int currentMonthlySales = userBasedCap;
                                p.SnapshotMonthlySales = currentMonthlySales;
                                p.PeakMonthlySales = Math.Max(p.PeakMonthlySales, currentMonthlySales);
                                long shapedUnits = EstimateShapedLifetimeTotal(currentMonthlySales, p.TailDecayFactor, ageInMonths);
                                p.TotalUnitsSold = (int)Math.Min(shapedUnits, int.MaxValue);
                                p.PreviousMonthUnitsSold = p.TotalUnitsSold;
                            }
                        }
                    }
                }
            } else {
                p.PreviousDailyActiveUsers = p.ActiveUserCount;
                p.PreviousMonthActiveUsers = p.ActiveUserCount;
                p.PreviousMonthlyRevenue = p.MonthlyRevenue;
                if (p.HasCompletedFirstMonth) {
                    p.SnapshotMonthlyUsers = p.ActiveUserCount;
                    p.SnapshotMonthlyRevenue = (long)p.MonthlyRevenue;
                    p.SnapshotMonthlySales = p.ActiveUserCount;
                }
            }
        }

        if (_freshWorldGeneration) {
            foreach (var compKvp in _gameState.competitorState.competitors) {
                var comp = compKvp.Value;
                if (comp.ActiveProductIds == null) continue;
                long companyHistoricalRevenue = 0L;
                long companyCurrentMonthlyRevenue = 0L;
                int pidCount = comp.ActiveProductIds.Count;
                for (int pi = 0; pi < pidCount; pi++) {
                    if (_gameState.productState.shippedProducts.TryGetValue(comp.ActiveProductIds[pi], out var prod)) {
                        companyHistoricalRevenue += prod.TotalLifetimeRevenue;
                        companyCurrentMonthlyRevenue += (long)prod.MonthlyRevenue;
                    }
                }
                long retainedCash = Math.Min((long)(companyHistoricalRevenue * 0.10), companyCurrentMonthlyRevenue * 12);
                comp.Finance.Cash += retainedCash;
            }
            _freshWorldGeneration = false;
        }

        if (!startPaused)
        {
            StartAdvance();
        }
    }
    
    private void Update()
    {
        if (_timeSystem == null) return;
        if (_commandQueue.Count > 0)
        {
            ProcessCommands(_gameState.currentTick);
            FlushSystemPostTick(_gameState.currentTick);
            FlushPendingEvents();
        }
        // No tick loop here — all tick advancement is in AdvanceCoroutine
    }

    // Flushes pending events from all systems after out-of-tick command processing.
    // Mirrors what SimTick does with PostTick so events raised by commands in Update()
    // (e.g. OnEmployeeHired) are not swallowed before the next sim tick.
    private void FlushSystemPostTick(int tick)
    {
        for (int i = 0; i < _systems.Count; i++)
        {
            _systems[i].PostTick(tick);
        }
        _competitorSystem.SyncNicheMarketShares(_gameState.marketState);
    }
    
    private void SimTick(int tick)
    {
        ProcessCommands(tick);
        
        for (int i = 0; i < _systems.Count; i++)
        {
            _systems[i].PreTick(tick);
        }
        
        for (int i = 0; i < _systems.Count; i++)
        {
            _systems[i].Tick(tick);
        }
        
        for (int i = 0; i < _systems.Count; i++)
        {
            _systems[i].PostTick(tick);
        }
    }
    
    public void StartAdvance()
    {
        if (_isAdvancing) return;
        _stopAdvanceRequested = false;
        _advanceCoroutine = StartCoroutine(AdvanceCoroutine());
    }
    
    public void StopAdvance()
    {
        _stopAdvanceRequested = true;
    }
    
    private IEnumerator AdvanceCoroutine()
    {
        _isAdvancing = true;
        _eventBus.Raise(new AdvanceStartedEvent(_gameState.currentTick));
        
        while (true)
        {
            if (_stopAdvanceRequested)
                break;
            
            int batchEnd = _gameState.currentTick + ticksPerFrame;
            while (_gameState.currentTick < batchEnd)
            {
                SimTick(_gameState.currentTick);
                _gameState.currentTick++;
                
                if (CheckInterrupts())
                    goto AdvanceDone;
            }
            
            _eventBus.Raise(new AdvanceTickedEvent(_gameState.currentTick));
            _competitorSystem.SyncNicheMarketShares(_gameState.marketState);
            yield return null;
        }
        
        AdvanceDone:
        _isAdvancing = false;
        ClearInterruptFlags();
        _advanceCoroutine = null;
        _eventBus.Raise(new AdvancePausedEvent(_gameState.currentTick));
    }
    
    private bool CheckInterrupts()
    {
        return _interruptBankrupt
            || _interruptContractEvent
            || _interruptCandidateRefresh
            || _interruptFinanceNegative
            || _interruptTierChange
            || _interruptHRSearchComplete
            || _interruptAutoAction
            || _interruptCandidateExpired
            || _interruptInterviewComplete
            || _interruptMarketDemand
            || _interruptCompetitorBankrupt
            || _interruptCompetitorProduct
            || _interruptPlayerAcquired;
    }
    
    private void ClearInterruptFlags()
    {
        _interruptBankrupt = false;
        _interruptContractEvent = false;
        _interruptCandidateRefresh = false;
        _interruptFinanceNegative = false;
        _interruptTierChange = false;
        _interruptHRSearchComplete = false;
        _interruptAutoAction = false;
        _interruptCandidateExpired = false;
        _interruptInterviewComplete = false;
        _interruptMarketDemand = false;
        _interruptCompetitorBankrupt = false;
        _interruptCompetitorProduct = false;
        _interruptPlayerAcquired = false;
    }
    
    private void ProcessCommands(int tick)
    {
        while (_commandQueue.Count > 0 && _commandQueue.Peek().Tick <= tick)
        {
            var cmd = _commandQueue.Dequeue();
            
            if (cmd is AdvanceTimeCommand)
            {
                StartAdvance();
                continue;
            }

            if (cmd is DismissMailCommand dismissMail)
            {
                if (_inboxSystem != null)
                {
                    if (dismissMail.MailId.HasValue)
                        _inboxSystem.DismissItem(dismissMail.MailId.Value);
                    else
                        _inboxSystem.DismissAll();
                }
                continue;
            }

            if (cmd is MarkMailReadCommand markRead)
            {
                if (_inboxSystem != null)
                {
                    if (markRead.MailId.HasValue)
                        _inboxSystem.MarkRead(markRead.MailId.Value);
                    else
                        _inboxSystem.MarkAllRead();
                }
                _eventBus.Raise(new InboxChangedEvent(cmd.Tick));
                continue;
            }
            
            for (int i = 0; i < _systems.Count; i++)
            {
                _systems[i].ApplyCommand(cmd);
            }
            
            _logger.Log($"[Tick {cmd.Tick}] Command executed: {cmd.GetType().Name}");

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            CommandHistoryInspector.RecordCommand(cmd);
#endif
        }
    }
    
    private void FlushPendingEvents()
    {
        int tick = _gameState.currentTick;
        for (int i = 0; i < _systems.Count; i++)
        {
            _systems[i].PostTick(tick);
        }
    }
    
    private static long EstimateShapedLifetimeTotal(double currentMonthly, float tailDecayFactor, int ageInMonths) {
        if (tailDecayFactor <= 0f || ageInMonths <= 0) return (long)(currentMonthly * Math.Max(ageInMonths, 0));
        float effectiveTail;
        if (ageInMonths >= 60) effectiveTail = Math.Min(tailDecayFactor, 0.94f);
        else if (ageInMonths >= 36) effectiveTail = Math.Min(tailDecayFactor, 0.96f);
        else if (ageInMonths >= 12) effectiveTail = Math.Min(tailDecayFactor, 0.98f);
        else effectiveTail = Math.Min(tailDecayFactor, 0.99f);
        double monthDecay = Math.Pow(effectiveTail, 1.0 / ageInMonths);
        double peak = currentMonthly / effectiveTail;
        double total = peak * (1.0 - Math.Pow(monthDecay, ageInMonths)) / (1.0 - monthDecay);
        return (long)Math.Max(total, 0);
    }

    private static void MigrateInterviewState(InterviewState state, int currentTick)
    {
        if (state?.activeInterviews == null) return;
        var keys = new System.Collections.Generic.List<int>(state.activeInterviews.Keys);
        int count = keys.Count;
        for (int i = 0; i < count; i++)
        {
            int key = keys[i];
            var intv = state.activeInterviews[key];
            if (intv.startTick > 0 && intv.knowledgeLevel <= 0f)
            {
                // Legacy interview with no knowledge data — mark as complete
                intv.knowledgeLevel = 100f;
                intv.lastRevealThreshold = 100;
                intv.completedTick = currentTick;
                state.activeInterviews[key] = intv;
            }
        }
    }

    private void LoadOrCreateGameState()
    {
        if (NewGameData.IsNewGame)
        {
            int seed = NewGameData.Seed != 0 ? NewGameData.Seed : System.Environment.TickCount;
            _gameState = GameState.CreateNew(seed, NewGameData.CompanyName);
            _gameState.difficultySettings = NewGameData.Difficulty;
            _gameState.financeState = FinanceState.CreateNew(NewGameData.Difficulty.StartingCash);
            _gameState.taxState = TaxState.CreateNew();
            _gameState.taxState.taxRate = NewGameData.Difficulty.TaxRate;
            _logger.Log($"[GameController] New game: seed={seed}, company={NewGameData.CompanyName}");
        }
        else if (!string.IsNullOrEmpty(NewGameData.LoadSlotName))
        {
            var saveData = SaveManager.LoadGame(NewGameData.LoadSlotName);
            if (saveData != null)
            {
                _gameState = saveData.State;
                _logger.Log($"[GameController] Loaded save: {NewGameData.LoadSlotName}");
            }
            else
            {
                Debug.LogError($"[GameController] Failed to load save: {NewGameData.LoadSlotName}. Starting debug session.");
                _gameState = GameState.CreateNew(seed: 12345, companyName: "Recovery Corp");
            }
            NewGameData.Clear();
        }
        else
        {
            _gameState = GameState.CreateNew(seed: 12345, companyName: "Debug Corp");
            _logger.Log($"[GameController] Debug session: seed=12345");
        }
    }

    private void ApplyDifficultyToTuning(DifficultySettings diff)
    {
        _tuning.ContractRewardMultiplier = diff.ContractRewardMultiplier;

        if (!diff.SalariesEnabled)
            _tuning.SalaryGlobalMultiplier = 0f;
        else
            _tuning.SalaryGlobalMultiplier = diff.SalaryMultiplier;

        if (!diff.QuittingEnabled)
        {
            _tuning.QuitChanceBase = 0f;
            _tuning.QuitChanceAmbitionScale = 0f;
        }

        _tuning.MaxXPPerContract = Math.Max(1, (int)(_tuning.MaxXPPerContract * diff.SkillGrowthMultiplier));
        _tuning.ProductPhaseXPPerDay *= diff.SkillGrowthMultiplier;
        _tuning.SkillSpilloverRateBase *= diff.SkillGrowthMultiplier;
        _tuning.ContractNativeXPRate *= diff.SkillGrowthMultiplier;
        _tuning.ContractMisfitXPRate *= diff.SkillGrowthMultiplier;

        _tuning.MoraleDailyPenaltyFloor *= diff.MoraleDecayMultiplier;
        _tuning.MoraleOverloadSevere *= diff.MoraleDecayMultiplier;
        _tuning.MoraleOverloadModerate *= diff.MoraleDecayMultiplier;
        _tuning.MoraleOverloadMild *= diff.MoraleDecayMultiplier;
        _tuning.IdleBoredomDecayPerDay *= diff.MoraleDecayMultiplier;
        _tuning.IdleDecayPerDay *= diff.MoraleDecayMultiplier;

        _tuning.CompetitorAggressionMultiplier = diff.CompetitorAggressionMultiplier;

        _tuning.MarketDifficultyMultiplier = diff.MarketDifficultyMultiplier;
        _tuning.CrisisBaseChancePerMonth *= diff.MarketDifficultyMultiplier;
        _tuning.CrisisChanceEscalationPerMonth *= diff.MarketDifficultyMultiplier;

        if (!diff.BankruptcyEnabled)
        {
            _tuning.FinanceBankruptDaysThreshold = int.MaxValue;
            _tuning.FinanceBankruptMissedThreshold = int.MaxValue;
        }

        _tuning.LoanBaseInterestRate *= diff.LoanInterestMultiplier;

        _tuning.ProductBaseWorkMultiplier *= diff.ProductWorkRateMultiplier;
        _tuning.BugRateMultiplier = diff.BugRateMultiplier;
        _tuning.ReviewHarshnessMultiplier = diff.ReviewHarshnessMultiplier;
        _tuning.ProductRevenueMultiplier = diff.ProductRevenueMultiplier;
    }

    private void CreateFoundingEmployees()
    {
        if (NewGameData.Founders == null || NewGameData.Founders.Count == 0)
        {
            NewGameData.Clear();
            return;
        }

        IRng hiddenRng = RngFactory.CreateStream(_gameState.masterSeed, "founder-hidden");

        var founders = NewGameData.Founders;
        int count = founders.Count;
        for (int i = 0; i < count; i++)
        {
            var data = founders[i];
            var empId = new EmployeeId(_gameState.employeeState.nextEmployeeId);
            _gameState.employeeState.nextEmployeeId++;

            int cardSeed = _gameState.masterSeed ^ (i * 6271) ^ (data.Tier * 7919) ^ ((int)data.Role * 4999);
            var founderRng = new RngStream(cardSeed);
            int[] rawSkills = GenerateFounderSkills(data.Tier, data.Role, founderRng);

            var stats = EmployeeStatBlock.Create();
            for (int s = 0; s < SkillIdHelper.SkillCount && s < rawSkills.Length; s++)
                stats.SetSkill((SkillId)s, rawSkills[s]);
            stats.PotentialAbility = 200;

            // Generate hidden attributes
            stats.SetHiddenAttribute(HiddenAttributeId.LearningRate,   hiddenRng.Range(15, 21));
            stats.SetHiddenAttribute(HiddenAttributeId.Ambition,        hiddenRng.Range(15, 21));
            // Visible attributes
            stats.SetVisibleAttribute(VisibleAttributeId.WorkEthic,     hiddenRng.Range(15, 21));
            stats.SetVisibleAttribute(VisibleAttributeId.Adaptability,  hiddenRng.Range(15, 21));
            stats.SetVisibleAttribute(VisibleAttributeId.Creativity,    hiddenRng.Range(15, 21));

            var emp = new Employee(
                empId,
                data.Name,
                data.Gender,
                data.Age,
                stats,
                salary: 0,
                hireDate: _gameState.currentTick,
                data.Role
            );

            emp.isFounder = true;
            emp.contractExpiryTick = int.MaxValue;
            emp.morale = 100;
            emp.isActive = true;
            emp.salary = 0;
            emp.personality = PersonalitySystem.GeneratePersonality(founderRng);

            _gameState.employeeState.employees[empId] = emp;
        }

        NewGameData.Clear();
    }

    private int[] GenerateFounderSkills(int tier, RoleId role, IRng rng)
    {
        var profile = _roleProfileTable?.Get(role);
        int[] roleTiers = profile != null ? RoleSuitabilityCalculator.BuildTierArray(profile) : null;
        int skillCount = SkillIdHelper.SkillCount;
        var skills = new int[skillCount];
        for (int i = 0; i < skillCount; i++)
        {
            int weight = (roleTiers != null && i < roleTiers.Length) ? roleTiers[i] : 3;
            int min, max;
            switch (tier)
            {
                case 1:
                    if (weight == 2) { min = 3; max = 5; }
                    else if (weight == 3) { min = 1; max = 3; }
                    else { min = 1; max = 1; }
                    break;
                case 2:
                    if (weight == 2) { min = 5; max = 8; }
                    else if (weight == 3) { min = 2; max = 5; }
                    else { min = 1; max = 2; }
                    break;
                case 3:
                    if (weight == 2) { min = 8; max = 12; }
                    else if (weight == 3) { min = 4; max = 7; }
                    else { min = 1; max = 3; }
                    break;
                case 4:
                    if (weight == 2) { min = 12; max = 15; }
                    else if (weight == 3) { min = 6; max = 10; }
                    else { min = 2; max = 5; }
                    break;
                default:
                    if (weight == 2) { min = 16; max = 20; }
                    else if (weight == 3) { min = 9; max = 13; }
                    else { min = 4; max = 7; }
                    break;
            }
            skills[i] = rng.Range(min, max + 1);
        }

        int identityIdx = -1;
        for (int i = 0; i < skillCount; i++)
        {
            if (roleTiers != null && i < roleTiers.Length && roleTiers[i] == 2) { identityIdx = i; break; }
        }
        if (identityIdx >= 0)
        {
            int maxOther = 0;
            for (int i = 0; i < skillCount; i++)
            {
                if (i != identityIdx && skills[i] > maxOther) maxOther = skills[i];
            }
            if (skills[identityIdx] <= maxOther)
                skills[identityIdx] = maxOther + 1 > 20 ? 20 : maxOther + 1;
        }

        return skills;
    }
    
    private void InitializeSystems()
    {
        _tuning = new TuningConfig();
        ApplyDifficultyToTuning(_gameState.difficultySettings);

        IRng employeeRng = RngFactory.CreateStream(_gameState.masterSeed, "employees");
        IRng contractRng = RngFactory.CreateStream(_gameState.masterSeed, "contracts");

        _timeSystem = new TimeSystem(_gameState.timeState);
        _financeSystem = new FinanceSystem(_gameState.financeState, _logger);
        _employeeSystem = new EmployeeSystem(_gameState.employeeState, employeeRng, _logger);
        _teamSystem = new TeamSystem(_gameState.teamState, _logger);

        if (_gameState.productState == null) _gameState.productState = ProductState.CreateNew();
        IRng productRng = RngFactory.CreateStream(_gameState.masterSeed, "products");
        _productSystem = new ProductSystem(_gameState.productState, productRng, _logger);

        var safeCategories = (contractCategories != null && contractCategories.Length > 0)
            ? contractCategories
            : new ContractCategoryDefinition[0];
        _contractFactory = new ContractFactory(safeCategories, contractRng, _logger);

        _contractSystem = new ContractSystem(
            _gameState.contractState,
            _contractFactory,
            _teamSystem,
            _employeeSystem,
            _financeSystem,
            contractRng,
            _logger);

        _reputationSystem = new ReputationSystem(_gameState.reputationState, _logger);
        
        _contractSystem.SetReputationSystem(_reputationSystem);
        _employeeSystem.SetReputationSystem(_reputationSystem);
        _employeeSystem.SetTeamState(_gameState.teamState);
        _teamSystem.SetEmployeeSystem(_employeeSystem);

        if (_gameState.moraleState == null) _gameState.moraleState = MoraleState.CreateNew();
        if (_gameState.fatigueState == null) _gameState.fatigueState = FatigueState.CreateNew();
        if (_gameState.loanState == null) _gameState.loanState = LoanState.CreateNew();

        _moraleRng = RngFactory.CreateStream(_gameState.masterSeed, "morale");

        _moraleSystem = new MoraleSystem(_gameState.moraleState, _employeeSystem, _gameState.teamState, _gameState.contractState, _gameState.productState, _eventBus, _logger);
        _fatigueSystem = new FatigueSystem(_gameState.fatigueState, _employeeSystem, _gameState.teamState, _logger);
        _moraleSystem.SetFatigueSystem(_fatigueSystem);
        _loanSystem = new LoanSystem(_gameState.loanState, _reputationSystem, _financeSystem, _logger);

        if (_gameState.chemistryState == null) _gameState.chemistryState = ChemistryState.CreateNew();
        _chemistryRng = RngFactory.CreateStream(_gameState.masterSeed, "chemistry");
        _teamChemistrySystem = new TeamChemistrySystem(
            _gameState.chemistryState,
            _teamSystem,
            _employeeSystem,
            _moraleSystem,
            _eventBus,
            _chemistryRng);

        // Hiring pipeline systems
        if (_gameState.interviewState == null) _gameState.interviewState = InterviewState.CreateNew();
        if (_gameState.negotiationState == null) _gameState.negotiationState = NegotiationState.CreateNew();
        if (_gameState.hrState == null) _gameState.hrState = HRState.CreateNew();
        if (_gameState.recruitmentReputationState == null) _gameState.recruitmentReputationState = RecruitmentReputationState.CreateNew();

        IRng interviewRng = RngFactory.CreateStream(_gameState.masterSeed, "interviews");
        IRng headhuntingRng = RngFactory.CreateStream(_gameState.masterSeed, "headhunting");
        IRng negotiationRng = RngFactory.CreateStream(_gameState.masterSeed, "negotiation");
        _interviewSystem = new InterviewSystem(_gameState.interviewState, _gameState.employeeState, _financeSystem, _eventBus, _logger, interviewRng);
        _negotiationSystem = new NegotiationSystem(_gameState.negotiationState, _gameState.employeeState, _interviewSystem, _eventBus, negotiationRng, _logger);
        _hrSystem = new HRSystem(
            _gameState.hrState,
            _gameState.employeeState,
            _financeSystem,
            _teamSystem,
            _recruitmentReputationSystem,
            headhuntingRng,
            _logger
        );
        _recruitmentReputationSystem = new RecruitmentReputationSystem(_gameState.recruitmentReputationState, _employeeSystem, _logger);

        _employeeSystem.SetRecruitmentReputationSystem(_recruitmentReputationSystem);
        _employeeSystem.SetInterviewSystem(_interviewSystem);
        _employeeSystem.SetNegotiationSystem(_negotiationSystem);
        _employeeSystem.SetHRSystem(_hrSystem);
        _interviewSystem.SetHRSystem(_hrSystem);

        // Save migration: convert any interviews that loaded with legacy fields (all zeros)
        // to knowledge 100 so they appear complete. Old saves had completionTick set;
        // since the field no longer exists, knowledgeLevel stays 0. Treat any interview
        // with startTick > 0 but knowledgeLevel == 0 as complete to avoid infinite interviews.
        MigrateInterviewState(_gameState.interviewState, _gameState.currentTick);

        // AbilitySystem — CA/PA system
        _roleProfileTable = new RoleProfileTable();
        var roleProfiles = Resources.LoadAll<RoleProfileDefinition>("RoleProfiles");
        for (int p = 0; p < roleProfiles.Length; p++)
            _roleProfileTable.Register(roleProfiles[p]);

        IRng abilityRng = RngFactory.CreateStream(_gameState.masterSeed, "ability");
        _abilitySystem = new AbilitySystem(_gameState.employeeState, _roleProfileTable, abilityRng, _logger);
        _employeeSystem.SetAbilitySystem(_abilitySystem);
        _employeeSystem.SetEventBus(_eventBus);
        _hrSystem.SetAbilitySystem(_abilitySystem);
        _negotiationSystem.SetRoleProfileTable(_roleProfileTable);
        _contractSystem.SetSkillGrowthDependencies(_roleProfileTable, _abilitySystem);
        _productSystem.SetFinanceSystem(_financeSystem);
        _productSystem.SetTeamSystem(_teamSystem);
        _productSystem.SetEmployeeSystem(_employeeSystem);
        _productSystem.SetReputationSystem(_reputationSystem);
        _productSystem.SetSkillGrowthDependencies(_roleProfileTable, _abilitySystem);
        _productSystem.SetContractState(_gameState.contractState);
        _productSystem.SetTimeSystem(_timeSystem);

        var safeOutlets = (reviewOutlets != null && reviewOutlets.Length > 0)
            ? reviewOutlets
            : new ReviewOutletDefinition[0];
        IRng reviewRng = RngFactory.CreateStream(_gameState.masterSeed, "reviews");
        var reviewSystem = new ReviewSystem(safeOutlets, reviewRng, _tuning);
        _productSystem.SetReviewSystem(reviewSystem);
        _contractSystem.SetProductSystem(_productSystem);
        _contractSystem.SetMoraleSystem(_moraleSystem);
        _productSystem.SetMoraleSystem(_moraleSystem);
        _contractSystem.SetFatigueSystem(_fatigueSystem);
        _productSystem.SetFatigueSystem(_fatigueSystem);
        _contractSystem.SetChemistrySystem(_teamChemistrySystem);
        _productSystem.SetChemistrySystem(_teamChemistrySystem);
        _teamChemistrySystem.SetFatigueSystem(_fatigueSystem);
        var safeProductTemplates = (productTemplates != null && productTemplates.Length > 0)
            ? productTemplates
            : new ProductTemplateDefinition[0];
        _productSystem.RegisterTemplates(safeProductTemplates);
        _contractSystem.OnSkillsAwarded += empIds =>
        {
            for (int i = 0; i < empIds.Count; i++)
                _abilitySystem.InvalidateCA(empIds[i]);
        };
        _productSystem.OnSkillsAwarded += empIds =>
        {
            for (int i = 0; i < empIds.Count; i++)
                _abilitySystem.InvalidateCA(empIds[i]);
        };
        _productSystem.OnProductShipped += (productId, quality) =>
        {
            if (_gameState.productState.shippedProducts.TryGetValue(productId, out var shipped) && shipped.IsCompetitorProduct) return;
            _interruptContractEvent = true;
            _eventBus.Raise(new ProductShippedEvent(_gameState.currentTick, productId, quality));
            _moraleSystem.ApplyProductShippedBonus(productId, quality);
        };
        _productSystem.OnPhaseCompleted += (productId, phaseType, quality) =>
        {
            _moraleSystem.ApplyPhaseCompletedBonus(productId, quality);
        };
        _productSystem.OnProductLaunched += OnProductLaunched;
        _productSystem.OnLifecycleChanged += OnProductLifecycleChanged;
        _productSystem.OnProductDead += OnProductDead;
        _productSystem.OnProductSaleStarted += OnProductSaleStarted;
        _productSystem.OnProductSaleEnded += OnProductSaleEnded;
        _productSystem.OnTeamAssignedToProduct += (productId, teamId, role) =>
        {
            _eventBus.Raise(new TeamAssignedToProductEvent(_gameState.currentTick, productId, teamId, role));
        };
        _productSystem.OnTeamUnassignedFromProduct += (productId, teamId) =>
        {
            _eventBus.Raise(new TeamAssignedToProductEvent(_gameState.currentTick, productId, teamId, default));
        };
        _productSystem.OnMarketingStarted += productId =>
        {
            _eventBus.Raise(new MarketingCampaignChangedEvent(_gameState.currentTick, productId, true));
        };
        _productSystem.OnMarketingStopped += productId =>
        {
            _eventBus.Raise(new MarketingCampaignChangedEvent(_gameState.currentTick, productId, false));
        };
        _productSystem.OnProductCreated += productId =>
        {
            _eventBus.Raise(new ProductCreatedEvent(_gameState.currentTick, productId));
        };
        _productSystem.OnProductProgressUpdated += productId =>
        {
            _eventBus.Raise(new ProductProgressUpdatedEvent(_gameState.currentTick, productId));
        };
        _productSystem.OnPhaseIterationStarted += (productId, phaseType, iterationCount) =>
        {
            _eventBus.Raise(new ProductPhaseIterationStartedEvent(_gameState.currentTick, productId, phaseType, iterationCount));
        };
        _productSystem.OnPhaseIterationCompleted += (productId, phaseType, newQuality) =>
        {
            _eventBus.Raise(new ProductPhaseIterationCompletedEvent(_gameState.currentTick, productId, phaseType, newQuality));
        };
        _productSystem.OnShipWarning += OnProductShipWarning;
        _productSystem.OnProductIdentityChanged += (productId, prev, curr) =>
        {
            _eventBus.Raise(new ProductIdentityChangedEvent(_gameState.currentTick, productId, prev, curr));
        };

        // MarketSystem
        IRng marketRng = RngFactory.CreateStream(_gameState.masterSeed, "market");
        var safeNiches = (marketNiches != null && marketNiches.Length > 0)
            ? marketNiches
            : new MarketNicheData[0];
        if (_gameState.marketState == null)
            _gameState.marketState = MarketState.CreateNew(safeNiches, marketRng, safeProductTemplates);
        _marketSystem = new MarketSystem(_gameState.marketState, _gameState.productState, marketRng, _logger);
        _marketSystem.RegisterNicheConfigs(safeNiches);

        // Collect all feature definitions from product templates and register with MarketSystem
        var allFeatures = CollectAllFeatureDefinitions(safeProductTemplates);
        _marketSystem.RegisterFeatureConfigs(allFeatures);
        _marketSystem.RegisterTemplates(safeProductTemplates);

        _productSystem.SetMarketSystem(_marketSystem);

        // CompetitorSystem
        if (_gameState.competitorState == null) _gameState.competitorState = CompetitorState.CreateNew();
        _productSystem.SetCompetitorState(_gameState.competitorState);
        IRng competitorRng = RngFactory.CreateStream(_gameState.masterSeed, "competitors");
        _competitorSystem = new CompetitorSystem(
            _gameState.competitorState,
            _gameState.productState,
            _gameState.marketState,
            competitorRng,
            _logger);
        _competitorSystem.SetTimeSystem(_timeSystem);
        _competitorSystem.SetMarketSystem(_marketSystem);
        _competitorSystem.SetReviewSystem(reviewSystem);
        _competitorSystem.SetEmployeeSystem(_employeeSystem);
        _competitorSystem.SetTeamSystem(_teamSystem);
        _competitorSystem.SetTuning(_tuning);

        var safeArchetypeConfigs = (competitorArchetypeConfigs != null && competitorArchetypeConfigs.Length > 0)
            ? competitorArchetypeConfigs
            : new CompetitorArchetypeConfig[0];
        _competitorSystem.SetArchetypeConfigs(safeArchetypeConfigs);
        _competitorSystem.SetNameData(competitorNameData);
        _competitorSystem.RegisterTemplates(safeProductTemplates);

        _competitorSystem.OnCompetitorBankrupt += OnCompetitorBankrupt;
        _competitorSystem.OnCompetitorProductLaunched += OnCompetitorProductLaunched;
        _competitorSystem.OnCompetitorSpawned += OnCompetitorSpawned;
        _competitorSystem.OnCompetitorDevStarted += OnCompetitorDevStarted;

        // StockSystem
        if (_gameState.stockState == null) _gameState.stockState = StockState.CreateNew();
        IRng stockRng = RngFactory.CreateStream(_gameState.masterSeed, "stock");
        _stockSystem = new StockSystem(_gameState.stockState, _gameState.competitorState, _financeSystem, stockRng, _logger);
        _stockSystem.SetTimeSystem(_timeSystem);
        _stockSystem.SetCompetitorSystem(_competitorSystem);
        _stockSystem.OnCompanyAcquired += OnCompanyAcquired;
        _stockSystem.OnPlayerAcquired  += OnStockPlayerAcquired;
        _stockSystem.OnDividendPaid    += OnDividendPaid;

        // DisruptionSystem
        if (_gameState.disruptionState == null) _gameState.disruptionState = DisruptionState.CreateNew();
        IRng disruptionRng = RngFactory.CreateStream(_gameState.masterSeed, "disruptions");
        _disruptionSystem = new DisruptionSystem(_gameState.disruptionState, _gameState.marketState, _gameState.competitorState, disruptionRng, _logger);
        _disruptionSystem.SetTimeSystem(_timeSystem);

        // Wire MarketSystem with competitor and disruption context
        _marketSystem.SetCompetitorState(_gameState.competitorState);
        _marketSystem.SetDisruptionSystem(_disruptionSystem);

        _disruptionSystem.OnMinorDisruptionStarted += OnMinorDisruptionStarted;
        _disruptionSystem.OnMajorDisruptionStarted += OnMajorDisruptionStarted;
        _disruptionSystem.OnDisruptionEnded += OnDisruptionEnded;

        // Initialize rolling demand projections (after SetDisruptionSystem so active disruptions can be stamped)
        _marketSystem.SetMasterSeed(_gameState.masterSeed);
        _marketSystem.InitializeProjections(_gameState.timeState.currentDay);

        // Wire disruption events to projection stamping
        _disruptionSystem.OnMinorDisruptionStarted += OnMinorDisruptionStampProjection;
        _disruptionSystem.OnMajorDisruptionStarted += OnMajorDisruptionStampProjection;

        // TaxSystem
        if (_gameState.taxState == null) _gameState.taxState = TaxState.CreateNew();
        _taxSystem = new TaxSystem(_gameState.taxState, _financeSystem, _timeSystem, _eventBus, _logger);
        _eventBus.Subscribe<TaxBankruptcyEvent>(OnTaxBankruptcyEvent);
        _competitorSystem.SetTaxSystem(_taxSystem);
        _competitorSystem.SetMoraleSystem(_moraleSystem);

        // GenerationSystem
        if (_gameState.generationState == null) {
            IRng genSeedRng = RngFactory.CreateStream(_gameState.masterSeed, "gen-seed");
            var safeGenDefs = (architectureGenerations != null && architectureGenerations.Length > 0)
                ? architectureGenerations
                : new ArchitectureGenerationDefinition[0];
            _gameState.generationState = GenerationState.CreateNew(genSeedRng, safeGenDefs);
        }
        IRng generationRng = RngFactory.CreateStream(_gameState.masterSeed, "generations");
        var safeGenerationDefs = (architectureGenerations != null && architectureGenerations.Length > 0)
            ? architectureGenerations
            : new ArchitectureGenerationDefinition[0];
        _generationSystem = new GenerationSystem();
        _generationSystem.Initialize(_gameState.generationState, safeGenerationDefs, generationRng, _inboxSystem);

        // PlatformSystem
        if (_gameState.platformState == null)
            _gameState.platformState = PlatformState.CreateNew();
        IRng platformRng = RngFactory.CreateStream(_gameState.masterSeed, "platforms");
        _platformSystem = new PlatformSystem(_gameState.platformState, _gameState.productState, _gameState.competitorState, platformRng);

        _productSystem.SetPlatformSystem(_platformSystem);
        _productSystem.SetGenerationSystem(_generationSystem);
        _marketSystem.SetPlatformSystem(_platformSystem);
        _marketSystem.SetGenerationSystem(_generationSystem);

        var safeHardwareConfigs = (hardwareGenerationConfigs != null && hardwareGenerationConfigs.Length > 0)
            ? hardwareGenerationConfigs
            : new HardwareGenerationConfig[0];
        _productSystem.SetHardwareGenerationConfigs(safeHardwareConfigs);
        _productSystem.SetCrossProductGateConfig(crossProductGateConfig);
        _competitorSystem.SetCrossProductGateConfig(crossProductGateConfig);
        _competitorSystem.SetProductSystem(_productSystem);

        // AIDecisionSystem
        IRng aiDecisionRng = RngFactory.CreateStream(_gameState.masterSeed, "aiDecisions");
        _aiDecisionSystem = new AIDecisionSystem(
            _gameState.competitorState,
            _employeeSystem,
            _teamSystem,
            _gameState.productState,
            _gameState.marketState,
            _eventBus,
            aiDecisionRng,
            _timeSystem,
            _gameState.employeeState,
            _logger);
        _aiDecisionSystem.SetArchetypeConfigs(safeArchetypeConfigs);
        _aiDecisionSystem.SetMarketSystem(_marketSystem);
        _aiDecisionSystem.SetGenerationSystem(_generationSystem);
        _aiDecisionSystem.SetReviewSystem(reviewSystem);
        _aiDecisionSystem.SetPlatformState(_gameState.platformState);
        _aiDecisionSystem.SetMoraleSystem(_moraleSystem);
        _aiDecisionSystem.SetCrossProductGateConfig(crossProductGateConfig);
        _aiDecisionSystem.SetNameData(competitorNameData);
        _aiDecisionSystem.RegisterTemplates(safeProductTemplates);
        _aiDecisionSystem.SetProductSystem(_productSystem);
        _aiDecisionSystem.SetCompetitorSystem(_competitorSystem);
        _aiDecisionSystem.SetTaxSystem(_taxSystem);

        _productSystem.OnProductCrisis += OnProductCrisis;
        _productSystem.OnProductSold += OnProductSold;

        _productSystem.OnReleaseDateAnnounced += (productId, targetTick) =>
        {
            _interruptContractEvent = true;
        };
        _productSystem.OnReleaseDateChanged += releaseDateChangedEvent =>
        {
            _interruptContractEvent = true;
        };

        _marketSystem.OnShowdownResolved += OnShowdownResolved;

        // Back-fill PA and hidden attributes on any existing employees/candidates without them
        MigrateAbilityDataIfNeeded();

        // Back-fill EmployeeStatBlock on all existing employees/candidates
        MigrateToV3StatModel(_gameState);

        _moraleSystem.OnEmployeeMayQuit += OnMoraleEmployeeMayQuit;

        _systems.Add(_timeSystem);
        _systems.Add(_financeSystem);
        _systems.Add(_employeeSystem);
        _systems.Add(_teamSystem);
        _systems.Add(_generationSystem);
        _systems.Add(_marketSystem);
        _systems.Add(_productSystem);
        _systems.Add(_platformSystem);
        _systems.Add(_contractSystem);
        _systems.Add(_reputationSystem);
        _systems.Add(_fatigueSystem);
        _systems.Add(_moraleSystem);
        _systems.Add(_teamChemistrySystem);
        _systems.Add(_loanSystem);
        _systems.Add(_interviewSystem);
        _systems.Add(_negotiationSystem);
        _systems.Add(_hrSystem);
        _systems.Add(_recruitmentReputationSystem);
        _systems.Add(_abilitySystem);

        _competitorContractBridge = new CompetitorContractBridge();
        _competitorContractBridge.Initialize(_competitorSystem, _contractSystem, _gameState.productState);
        if (_gameState.difficultySettings.CompetitorsEnabled)
        {
            _systems.Add(_competitorSystem);
            _systems.Add(_aiDecisionSystem);
        }
        _systems.Add(_stockSystem);
        _systems.Add(_disruptionSystem);
        if (_gameState.difficultySettings.TaxEnabled)
            _systems.Add(_taxSystem);

        _timeSystem.OnDayChanged += OnDayChanged;
        _timeSystem.OnMonthChanged += OnMonthChanged;
        _timeSystem.OnYearChanged += OnYearChanged;
        _financeSystem.OnBankrupt += OnBankrupt;
        _financeSystem.OnBankruptcyWarning += OnBankruptcyWarning;
        _employeeSystem.OnEmployeeHired += OnEmployeeHired;
        _employeeSystem.OnEmployeeFired += OnEmployeeFired;
        _employeeSystem.OnEmployeeQuit += OnEmployeeQuit;
        _employeeSystem.OnCandidatesGenerated += OnCandidatesGenerated;
        _employeeSystem.OnContractRenewed += OnContractRenewed;
        _employeeSystem.OnContractRenewalRequested += OnContractRenewalRequested;
        _teamSystem.OnTeamCreated += OnTeamCreated;
        _teamSystem.OnTeamDeleted += OnTeamDeleted;
        _teamSystem.OnEmployeeAssignedToTeam += OnEmployeeAssignedToTeam;
        _teamSystem.OnEmployeeRemovedFromTeam += OnEmployeeRemovedFromTeam;
        _teamSystem.OnCrunchModeChanged += OnTeamCrunchModeChanged;
        _contractSystem.OnContractAccepted += OnContractAccepted;
        _contractSystem.OnContractAssigned += OnContractAssigned;
        _contractSystem.OnContractProgressUpdated += OnContractProgressUpdated;
        _contractSystem.OnContractCompleted += OnContractCompleted;
        _contractSystem.OnContractFailed += OnContractFailed;
        _contractSystem.OnContractExpired += OnContractExpired;
        _contractSystem.OnPoolRerolled += OnPoolRerolled;
        _reputationSystem.OnTierChanged += OnReputationTierChanged;

        // Hiring pipeline event subscriptions
        _employeeSystem.OnCandidatesGenerated += OnCandidatesGeneratedClearPipeline;
        _employeeSystem.OnEmployeeHired += _recruitmentReputationSystem.OnEmployeeHiredHandler;
        _employeeSystem.OnEmployeeFired += _recruitmentReputationSystem.OnEmployeeFiredHandler;
        _employeeSystem.OnEmployeeQuit += _recruitmentReputationSystem.OnEmployeeQuitHandler;
        _negotiationSystem.OnOfferRejected += _recruitmentReputationSystem.OnOfferRejectedHandler;

        _negotiationSystem.OnOfferAccepted += OnNegotiationAccepted;
        _negotiationSystem.OnOfferRejected += OnNegotiationRejected;
        _interviewSystem.OnInterviewThresholdReached += OnInterviewThresholdReached;

        // New subscriptions for interrupt conditions
        _hrSystem.OnSearchCompleted += OnHRSearchCompleted;
        _hrSystem.OnCandidatesReadyForReview += OnHRCandidatesReadyForReview;
        _hrSystem.OnCandidateAccepted += OnHRCandidateAccepted;
        _hrSystem.OnPoolFull += OnHRPoolFull;
        _eventBus.Subscribe<CandidateWithdrewEvent>(OnCandidateWithdrew);
        _eventBus.Subscribe<CompetitorHiredCandidateEvent>(OnCompetitorHiredCandidate);
        _eventBus.Subscribe<CandidateLostPatienceEvent>(OnCandidateLostPatience);
        _eventBus.Subscribe<EmployeeFrustratedEvent>(OnEmployeeFrustrated);

        _contractSystem.RefreshContractPool(_gameState.currentTick);
        _gameState.contractState.lastPoolRefreshTick = _gameState.currentTick;
        
        if (_gameState.contractState.poolRefreshIntervalTicks <= 0)
        {
            _gameState.contractState.poolRefreshIntervalTicks = 7 * TimeState.TicksPerDay;
        }

        // InboxSystem
        if (_gameState.inboxState == null)
            _gameState.inboxState = InboxState.CreateNew();
        _inboxSystem = new InboxSystem(_gameState.inboxState, _eventBus, _logger);
        _inboxSystem.Initialize();

        // Wire TuningConfig into all systems
        _contractSystem.SetTuningConfig(_tuning);
        _moraleSystem.SetTuningConfig(_tuning);
        _loanSystem.SetTuningConfig(_tuning);
        _financeSystem.SetTuningConfig(_tuning);
        _abilitySystem.SetTuningConfig(_tuning);
        _reputationSystem.SetTuningConfig(_tuning);
        _recruitmentReputationSystem.SetTuningConfig(_tuning);
        _employeeSystem.SetTuningConfig(_tuning);
        _contractFactory.SetTuningConfig(_tuning);
        _productSystem.SetTuningConfig(_tuning);
        HRSearchConfig.SetTuningConfig(_tuning);

        _logger.Log("Systems initialized");

        _autoSaveSystem = new AutoSaveSystem(this, _eventBus);
    }

    private static ProductFeatureDefinition[] CollectAllFeatureDefinitions(ProductTemplateDefinition[] templates)
    {
        var seen = new System.Collections.Generic.HashSet<string>();
        var result = new System.Collections.Generic.List<ProductFeatureDefinition>();
        if (templates == null) return result.ToArray();
        for (int t = 0; t < templates.Length; t++)
        {
            var tpl = templates[t];
            if (tpl?.availableFeatures == null) continue;
            for (int f = 0; f < tpl.availableFeatures.Length; f++)
            {
                var feat = tpl.availableFeatures[f];
                if (feat == null || string.IsNullOrEmpty(feat.featureId)) continue;
                if (seen.Add(feat.featureId)) result.Add(feat);
            }
        }
        return result.ToArray();
    }

    private void MigrateAbilityDataIfNeeded()
    {
        if (_gameState.employeeState == null) return;

        // Version 1 → 2: legacy skill rescale (old fields removed; migration no-op)
        if (_gameState.version < 2)
        {
            _gameState.version = 2;
            _logger.Log("[GameController] Save migration v1→v2: legacy field migration skipped (EmployeeStatBlock migration).");
        }

        // Version 2 → 3: legacy Art2D/Art3D skill merge (old fields removed; migration no-op)
        if (_gameState.version < 3)
        {
            _gameState.version = 3;
            _logger.Log("[GameController] Save migration v2→v3: legacy field migration skipped (EmployeeStatBlock migration).");
        }

        // Version 3 → 4: MailCategory enum values changed.
        // Old: HR=0, Contract=1, Finance=2, Research=3
        // New: Alert=0, Contract=1, Recruitment=2, HR=3, Finance=4, Research=5, Operations=6
        if (_gameState.version < 4 && _gameState.inboxState != null && _gameState.inboxState.Items != null)
        {
            var items = _gameState.inboxState.Items;
            int itemCount = items.Count;
            for (int i = 0; i < itemCount; i++)
            {
                var mail = items[i];
                int oldVal = (int)mail.Category;
                switch (oldVal)
                {
                    case 0: mail.Category = MailCategory.HR;       break; // old HR(0) → new HR(3)
                    case 1: mail.Category = MailCategory.Contract;  break; // unchanged
                    case 2: mail.Category = MailCategory.Finance;   break; // old Finance(2) → new Finance(4)
                    case 3: mail.Category = MailCategory.Technology;  break; // old Research(3) → new Technology(5)
                }
                // Priority defaults to Info(0) for all existing items — no remapping needed
                items[i] = mail;
            }
            _gameState.version = 4;
            _logger.Log("[GameController] Save migration v3→v4 complete: MailCategory enum remapped.");
        }
        if (_gameState.version < 5) {
            // v4→v5: legacy Marketing skill slot added (old fields removed; migration no-op)
            _gameState.version = 5;
            _logger.Log("[GameController] Save migration v4->v5 complete: legacy migration skipped (EmployeeStatBlock migration).");
        }
        if (_gameState.version < 6) {
            // SyntheticAdaptability and RoleFitRatio removed — migration no-op
            _gameState.version = 6;
            _logger.Log("[GameController] Save migration v5->v6 complete: SyntheticAdaptability and RoleFitRatio defaulted for existing competitors.");
        }
        if (_gameState.version < 7) {
            if (_gameState.moraleState?.employeeMorale != null) {
                var keys = new System.Collections.Generic.List<EmployeeId>(_gameState.moraleState.employeeMorale.Keys);
                int migCount = keys.Count;
                for (int mi = 0; mi < migCount; mi++) {
                    var key = keys[mi];
                    var mdata = _gameState.moraleState.employeeMorale[key];
                    mdata.currentMorale = 60f;
                    _gameState.moraleState.employeeMorale[key] = mdata;
                }
            }
            _gameState.version = 7;
            _logger.Log("[GameController] Save migration v6->v7 complete: MoraleData new fields defaulted, morale reset to 60.");
        }
        if (_gameState.version < 8) {
            if (_gameState.teamState?.teams != null) {
                foreach (var kvp in _gameState.teamState.teams) {
                    kvp.Value.isCrunching = false;
                }
            }
            _gameState.version = 8;
            _logger.Log("[GameController] Save migration v7->v8 complete: isCrunching defaulted to false on all teams.");
        }
        if (_gameState.version < 9) {
            // MoraleEquilibrium and SyntheticMorale removed — migration no-op
            _gameState.version = 9;
            _logger.Log("[GameController] Save migration v8->v9 complete: MoraleEquilibrium initialised from SyntheticMorale for all competitors.");
        }
        if (_gameState.version < 10) {
            MigrateCompetitorUnification();
        }

        if (_gameState.version < 11) {
            if (_gameState.financeState?.recurringCosts != null) {
                var costs = _gameState.financeState.recurringCosts;
                for (int i = costs.Count - 1; i >= 0; i--) {
                    var entry = costs[i];
                    if (entry.category != FinanceCategory.Salary) continue;
                    if (string.IsNullOrEmpty(entry.sourceId)) continue;
                    if (!entry.sourceId.StartsWith("employee-")) continue;
                    if (!int.TryParse(entry.sourceId.Substring("employee-".Length), out int empIdValue)) continue;
                    var empId = new EmployeeId(empIdValue);
                    var emp = _employeeSystem.GetEmployee(empId);
                    if (emp == null || emp.ownerCompanyId != CompanyId.Player)
                        costs.RemoveAt(i);
                }
            }
            _gameState.version = 11;
            _logger.Log("[GameController] Save migration v10->v11 complete: Stale competitor salary recurring costs purged.");
        }

        if (_gameState.version < 12)
        {
            // Team type enum was redesigned: Contracts/Programming/SFX/VFX → Development,
            // Accounting → Development (deletion not possible post-deserialization; converter handled remapping).
            // ProductTeamRole: Programming/SFX/VFX → Development.
            // Both remappings are handled transparently by LegacyTeamTypeConverter and
            // LegacyProductTeamRoleConverter during deserialization.
            // Purge duplicate Development role assignments in product TeamAssignments:
            // (old saves may have had Programming+SFX+VFX all remapped to Development in the same product)
            if (_gameState.productState != null)
            {
                var devProducts = _gameState.productState.developmentProducts;
                if (devProducts != null)
                {
                    foreach (var kvp in devProducts)
                    {
                        var p = kvp.Value;
                        if (p?.TeamAssignments == null) continue;
                    }
                }
                var shippedProducts = _gameState.productState.shippedProducts;
                if (shippedProducts != null)
                {
                    foreach (var kvp in shippedProducts)
                    {
                        var p = kvp.Value;
                        if (p?.TeamAssignments == null) continue;
                    }
                }
            }
            _gameState.version = 12;
            _logger.Log("[GameController] Save migration v11->v12 complete: TeamType and ProductTeamRole enums remapped to 5-lane model.");
        }

        if (_gameState.version < 13)
        {
            // Assign personality to all existing employees using deterministic per-entity RNG
            if (_gameState.employeeState?.employees != null)
            {
                foreach (var kvp in _gameState.employeeState.employees)
                {
                    var emp = kvp.Value;
                    if (emp == null) continue;
                    var personalityRng = new RngStream(unchecked(emp.id.Value ^ (int)0xBEEF1234));
                    emp.personality = PersonalitySystem.GeneratePersonality(personalityRng);
                }
            }

            // Assign personality to all existing candidates
            if (_gameState.employeeState?.availableCandidates != null)
            {
                var cands = _gameState.employeeState.availableCandidates;
                int cCount = cands.Count;
                for (int i = 0; i < cCount; i++)
                {
                    var c = cands[i];
                    if (c == null) continue;
                    var personalityRng = new RngStream(unchecked(c.CandidateId ^ (int)0xCAFEDEAD));
                    c.personality = PersonalitySystem.GeneratePersonality(personalityRng);
                }
            }

            // Initialize empty chemistry state
            if (_gameState.chemistryState == null)
                _gameState.chemistryState = ChemistryState.CreateNew();

            _gameState.version = 13;
            _logger.Log("[GameController] Save migration v12->v13 complete: Personality assigned to all employees and candidates; ChemistryState initialized.");
        }

        if (_gameState.version < 14)
        {
            // Initialize FatigueState for all existing employees.
            // Transfer crunchDaysActive and recentCrunchDays from MoraleData if present.
            if (_gameState.fatigueState == null)
                _gameState.fatigueState = FatigueState.CreateNew();

            if (_gameState.employeeState?.employees != null)
            {
                foreach (var kvp in _gameState.employeeState.employees)
                {
                    var emp = kvp.Value;
                    if (emp == null) continue;
                    if (!_gameState.fatigueState.employeeFatigue.ContainsKey(kvp.Key))
                    {
                        _gameState.fatigueState.employeeFatigue[kvp.Key] = new FatigueData(100f);
                    }
                }
            }

            // Reset morale to 60 to account for the narrowed multiplier range
            if (_gameState.moraleState?.employeeMorale != null)
            {
                var moraleKeys = new System.Collections.Generic.List<EmployeeId>(_gameState.moraleState.employeeMorale.Keys);
                int mCount = moraleKeys.Count;
                for (int mi = 0; mi < mCount; mi++)
                {
                    var key = moraleKeys[mi];
                    var mdata = _gameState.moraleState.employeeMorale[key];
                    mdata.currentMorale = 60f;
                    _gameState.moraleState.employeeMorale[key] = mdata;
                }
            }

            _gameState.version = 14;
            _logger.Log("[GameController] Save migration v13->v14 complete: FatigueState initialized for all employees; morale reset to 60.");
        }

        if (_gameState.version < 15)
        {
            foreach (var kvp in _gameState.employeeState.employees)
            {
                var emp = kvp.Value;
                if (emp == null) continue;
                if (emp.preferredRole == default)
                    emp.preferredRole = emp.role;
            }
            _gameState.version = 15;
            _logger.Log("[GameController] Save migration v14->v15 complete: preferredRole initialized for all employees.");
        }

        // Always run: convert BreakoutMonthsRemaining -> BreakoutDaysRemaining for products that pre-date daily revenue
        if (_gameState.productState != null)
        {
            foreach (var kvp in _gameState.productState.shippedProducts)
            {
                var p = kvp.Value;
                if (p == null) continue;
                if (p.BreakoutDaysRemaining == 0 && p.BreakoutMonthsRemaining > 0)
                    p.BreakoutDaysRemaining = p.BreakoutMonthsRemaining * 30;
            }
        }
        _logger.Log("[GameController] Ability data migration complete (legacy fields removed).");
    }

    private void MigrateToV3StatModel(GameState state)
    {
        if (state.employeeState == null) return;
        int migratedEmployees = 0;
        int migratedCandidates = 0;

        foreach (var kvp in state.employeeState.employees)
        {
            var emp = kvp.Value;
            if (emp == null) continue;
            if (emp.Stats.Skills != null) continue;
            emp.Stats = EmployeeStatBlock.Create();
            migratedEmployees++;
        }

        var candidates = state.employeeState.availableCandidates;
        if (candidates != null)
        {
            int cCount = candidates.Count;
            for (int i = 0; i < cCount; i++)
            {
                var c = candidates[i];
                if (c == null) continue;
                if (c.Stats.Skills != null) continue;
                c.Stats = EmployeeStatBlock.Create();
                if (c.SkillConfidence == null)
                {
                    c.SkillConfidence = new ConfidenceLevel[SkillIdHelper.SkillCount];
                    for (int j = 0; j < c.SkillConfidence.Length; j++)
                        c.SkillConfidence[j] = ConfidenceLevel.Unknown;
                }
                if (c.VisibleAttributeConfidence == null)
                {
                    c.VisibleAttributeConfidence = new ConfidenceLevel[VisibleAttributeHelper.AttributeCount];
                    for (int j = 0; j < c.VisibleAttributeConfidence.Length; j++)
                        c.VisibleAttributeConfidence[j] = ConfidenceLevel.Unknown;
                }
                if (c.HiddenAttributeConfidence == null)
                {
                    c.HiddenAttributeConfidence = new ConfidenceLevel[HiddenAttributeHelper.AttributeCount];
                    for (int j = 0; j < c.HiddenAttributeConfidence.Length; j++)
                        c.HiddenAttributeConfidence[j] = ConfidenceLevel.Unknown;
                }
                migratedCandidates++;
            }
        }

        _logger.Log($"[GameController] V3 stat model migration: {migratedEmployees} employees, {migratedCandidates} candidates populated.");
    }

    // v9 -> v10: Competitor Unification migration.
    // Sets ownerCompanyId on player employees/teams, migrates product ownership from
    // competitor ID, strips phantom competitor employee IDs, re-generates real competitor
    // employees and teams, initializes morale.
    private void MigrateCompetitorUnification()
    {
        // Step 1: Set ownerCompanyId = Player on all existing employees
        if (_gameState.employeeState?.employees != null) {
            foreach (var kvp in _gameState.employeeState.employees) {
                if (kvp.Value != null && kvp.Value.ownerCompanyId == default(CompanyId))
                    kvp.Value.ownerCompanyId = CompanyId.Player;
            }
        }

        // Step 2: Set ownerCompanyId = Player on all existing teams
        if (_gameState.teamState?.teams != null) {
            foreach (var kvp in _gameState.teamState.teams) {
                if (kvp.Value != null && kvp.Value.ownerCompanyId == default(CompanyId))
                    kvp.Value.ownerCompanyId = CompanyId.Player;
            }
        }

        // Step 3: Migrate product OwnerCompanyId from competitor ActiveProductIds / InDevelopmentProductIds
        if (_gameState.competitorState?.competitors != null && _gameState.productState != null) {
            foreach (var compKvp in _gameState.competitorState.competitors) {
                var comp = compKvp.Value;
                if (comp == null) continue;
                CompanyId companyId = comp.Id.ToCompanyId();

                if (comp.ActiveProductIds != null) {
                    for (int i = 0; i < comp.ActiveProductIds.Count; i++) {
                        var pid = comp.ActiveProductIds[i];
                        if (_gameState.productState.shippedProducts.TryGetValue(pid, out var sp))
                            sp.OwnerCompanyId = companyId;
                        if (_gameState.productState.developmentProducts.TryGetValue(pid, out var dp))
                            dp.OwnerCompanyId = companyId;
                        if (_gameState.productState.archivedProducts.TryGetValue(pid, out var ap))
                            ap.OwnerCompanyId = companyId;
                    }
                }
                if (comp.InDevelopmentProductIds != null) {
                    for (int i = 0; i < comp.InDevelopmentProductIds.Count; i++) {
                        var pid = comp.InDevelopmentProductIds[i];
                        if (_gameState.productState.shippedProducts.TryGetValue(pid, out var sp))
                            sp.OwnerCompanyId = companyId;
                        if (_gameState.productState.developmentProducts.TryGetValue(pid, out var dp))
                            dp.OwnerCompanyId = companyId;
                        if (_gameState.productState.archivedProducts.TryGetValue(pid, out var ap))
                            ap.OwnerCompanyId = companyId;
                    }
                }
            }
        }

        // Steps 4-7: Per competitor — strip phantom employees, re-hire real ones, create teams, init morale
        if (_gameState.competitorState?.competitors != null) {
            foreach (var compKvp in _gameState.competitorState.competitors) {
                var comp = compKvp.Value;
                if (comp == null || comp.IsBankrupt || comp.IsAbsorbed) continue;

                // Step 4: Remove any phantom employee IDs (don't exist in EmployeeState)
                if (comp.EmployeeIds == null)
                    comp.EmployeeIds = new System.Collections.Generic.List<EmployeeId>();
                else {
                    for (int i = comp.EmployeeIds.Count - 1; i >= 0; i--) {
                        if (!_gameState.employeeState.employees.ContainsKey(comp.EmployeeIds[i]))
                            comp.EmployeeIds.RemoveAt(i);
                    }
                }

                // Steps 5-7: Only re-generate if the competitor has no real employees yet
                if (comp.EmployeeIds.Count > 0) continue;

                // Deterministic RNG seeded per competitor per save
                IRng compRng = new RngStream(_gameState.masterSeed ^ comp.Id.Value ^ 0xCAFE);

                int minCount, maxCount;
                switch (comp.Archetype) {
                    case CompetitorArchetype.PlatformGiant: minCount = 15; maxCount = 25; break;
                    case CompetitorArchetype.FullStack:     minCount = 12; maxCount = 20; break;
                    case CompetitorArchetype.ToolMaker:     minCount = 8;  maxCount = 15; break;
                    default:                                minCount = 5;  maxCount = 12; break;
                }
                int employeeCount = compRng.Range(minCount, maxCount + 1);

                float ftRatio = 0.65f;
                float salaryMod = 1.0f;
                if (competitorArchetypeConfigs != null) {
                    for (int ci = 0; ci < competitorArchetypeConfigs.Length; ci++) {
                        var cfg = competitorArchetypeConfigs[ci];
                        if (cfg != null && cfg.archetype == comp.Archetype) {
                            ftRatio = cfg.fullTimeRatio;
                            salaryMod = cfg.salaryTierModifier;
                            break;
                        }
                    }
                }

                var hiredIds = _employeeSystem.BulkHireForCompany(comp.Id.ToCompanyId(), comp.Archetype, employeeCount, compRng, _gameState.currentTick, ftRatio, salaryMod);
                for (int i = 0; i < hiredIds.Count; i++)
                    comp.EmployeeIds.Add(hiredIds[i]);

                // Step 6: Create one team per product (active + in-dev), distribute employees
                int productCount = (comp.ActiveProductIds != null ? comp.ActiveProductIds.Count : 0)
                                 + (comp.InDevelopmentProductIds != null ? comp.InDevelopmentProductIds.Count : 0);
                int teamCount = productCount > 0 ? productCount : 1;
                CompanyId companyId = comp.Id.ToCompanyId();
                var teamIds = new System.Collections.Generic.List<TeamId>(teamCount);
                for (int t = 0; t < teamCount; t++) {
                    TeamId teamId = _teamSystem.CreateTeam(TeamType.Development, _gameState.currentTick, companyId);
                    teamIds.Add(teamId);
                }
                int empCount = comp.EmployeeIds.Count;
                for (int i = 0; i < empCount; i++) {
                    _teamSystem.AssignEmployeeToTeam(comp.EmployeeIds[i], teamIds[i % teamIds.Count]);
                }

                // Step 7: Initialize morale for new employees
                for (int i = 0; i < comp.EmployeeIds.Count; i++)
                    _moraleSystem.InitializeEmployee(comp.EmployeeIds[i]);
            }
        }

        _gameState.version = 10;
        _logger.Log("[GameController] Save migration v9->v10 complete: Competitor Unification — real employees generated, product ownership migrated.");
    }
    
    private int ComputeTotalRevenue()
    {
        if (_gameState.financeState?.transactions == null) return 0;
        int total = 0;
        var txns = _gameState.financeState.transactions;
        int count = txns.Count;
        for (int i = 0; i < count; i++)
        {
            if (txns[i].amount > 0) total += txns[i].amount;
        }
        return total;
    }

    private void OnDayChanged(int day)
    {
        int dayOfMonth = _timeSystem.DayOfMonth;
        int month = _timeSystem.CurrentMonth;
        int year = _timeSystem.CurrentYear;
        
        _logger.Log($"[Day {dayOfMonth}, Month {month}, Year {year}] Day changed");
        
        // Day-boundary processing: daily recurring costs -> energy -> morale -> bankruptcy check
        _financeSystem.ProcessDaily(_gameState.currentTick);
        
        // Energy processing (must run before morale)
        _fatigueSystem.ProcessDailyEnergy(day);

        // Morale processing
        IRng moraleRng = _moraleRng;
        _moraleSystem.ProcessDailyMorale(day, moraleRng);

        // Chemistry processing
        _teamChemistrySystem?.ProcessDailyChemistry(_gameState.currentTick);
        
        // Reputation penalty for extended debt
        if (_financeSystem.ConsecutiveDaysNegativeCash >= 3) {
            _reputationSystem.RemoveReputation(5);
            _logger.Log($"[Day {dayOfMonth}] Reputation penalty for debt: -5");
        }
        
        _eventBus.Raise(new DayChangedEvent(_gameState.currentTick, day, month, year));
        _eventBus.Raise(new FinanceChangedEvent(
            _gameState.currentTick, 
            _gameState.financeState.money, 
            _financeSystem.MonthlyExpenses, 
            _financeSystem.RecentTransactions != null ? ComputeTotalRevenue() : 0
        ));
    }
    
    private void OnMonthChanged(int month)
    {
        int year = _timeSystem.CurrentYear;
        _logger.Log($"[Month {month}, Year {year}] Month changed — processing payroll");
        
        // Monthly payroll: deduct all employee salaries via recurring monthly costs
        _financeSystem.ProcessMonthly(_gameState.currentTick);
        _loanSystem.ProcessMonthlyRepayment();
        
        // Update salary benchmarks from competitor data
        _employeeSystem.UpdateIndustryBenchmarks(_gameState.competitorState);
        
        int totalSalaries = _financeSystem.MonthlyObligations;
        if (totalSalaries > 0)
            _eventBus.Raise(new SalaryPaidEvent(_gameState.currentTick, totalSalaries, _gameState.financeState.money));
        
        _eventBus.Raise(new FinanceChangedEvent(
            _gameState.currentTick,
            _gameState.financeState.money,
            _financeSystem.MonthlyExpenses,
            ComputeTotalRevenue()
        ));

        // Monthly news report
        if (_gameState.productState != null && _disruptionSystem != null)
        {
            var report = NewsReportGenerator.Generate(
                _gameState.marketState,
                _gameState.competitorState,
                _gameState.productState,
                _disruptionSystem,
                _gameState.currentTick
            );
            if (report != null)
            {
                _inboxSystem?.AddMonthlyNewsReport(_gameState.currentTick, report);
                _eventBus.Raise(new MonthlyNewsReportEvent(_gameState.currentTick, report));
            }
        }

        // 60-day auto-clear of read/dismissed inbox messages
        _inboxSystem?.PurgeExpiredMessages(_gameState.currentTick, 60 * TimeState.TicksPerDay);
    }
    
    private void OnYearChanged(int year)
    {
        _logger.Log($"[Year {year}] Year changed");
    }
    
    private void OnBankruptcyWarning()
    {
        _interruptFinanceNegative = true;
        _logger.LogWarning("BANKRUPTCY WARNING: Negative balance!");
    }
    
    private void OnBankrupt()
    {
        _interruptBankrupt = true;
        StopAdvance();
        _logger.LogError("GAME OVER: Company bankrupt!");
    }

    private void OnTaxBankruptcyEvent(TaxBankruptcyEvent evt)
    {
        OnBankrupt();
    }
    
    private void OnEmployeeHired(EmployeeId employeeId)
    {
        var employee = _employeeSystem.GetEmployee(employeeId);
        if (employee != null && employee.ownerCompanyId == CompanyId.Player)
        {
            _financeSystem.AddRecurringCost(
                $"salary-{employeeId.Value}",
                FinanceCategory.Salary,
                employee.salary,
                RecurringInterval.Monthly,
                $"employee-{employeeId.Value}"
            );
            _moraleSystem.InitializeEmployee(employeeId, 100f);
            _fatigueSystem.InitializeEmployee(employeeId);

            _eventBus.Raise(new EmployeeHiredEvent(
                _gameState.currentTick,
                employeeId,
                employee.name,
                employee.Contract.Type,
                GetContractLengthOption(employee.Contract),
                employee.salary
            ));
        }

        int totalEmployees = _employeeSystem.EmployeeCountForCompany(CompanyId.Player);
        _eventBus.Raise(new EmployeeCountChangedEvent(_gameState.currentTick, totalEmployees, employeeId, true));
        _eventBus.Raise(new FinanceChangedEvent(
            _gameState.currentTick,
            _gameState.financeState.money,
            _financeSystem.MonthlyExpenses,
            ComputeTotalRevenue()
        ));
    }

    private static ContractLengthOption GetContractLengthOption(ContractTerms contract)
    {
        return contract.Length;
    }
    
    private void OnEmployeeFired(EmployeeId employeeId)
    {
        var employee = _employeeSystem.GetEmployee(employeeId);
        if (employee != null && employee.ownerCompanyId == CompanyId.Player)
        {
            _financeSystem.RemoveRecurringCost($"salary-{employeeId.Value}");
        }

        _teamSystem.RemoveEmployeeFromTeam(employeeId);
        _fatigueSystem.RemoveEmployee(employeeId);

        int totalEmployees = _employeeSystem.EmployeeCountForCompany(CompanyId.Player);
        _eventBus.Raise(new EmployeeCountChangedEvent(_gameState.currentTick, totalEmployees, employeeId, false));
        _eventBus.Raise(new FinanceChangedEvent(
            _gameState.currentTick,
            _gameState.financeState.money,
            _financeSystem.MonthlyExpenses,
            ComputeTotalRevenue()
        ));
    }

    private void OnEmployeeQuit(EmployeeId employeeId)
    {
        var employee = _employeeSystem.GetEmployee(employeeId);
        if (employee != null && employee.ownerCompanyId == CompanyId.Player)
        {
            _financeSystem.RemoveRecurringCost($"salary-{employeeId.Value}");
        }

        _teamSystem.RemoveEmployeeFromTeam(employeeId);
        _fatigueSystem.RemoveEmployee(employeeId);

        int totalEmployees = _employeeSystem.EmployeeCountForCompany(CompanyId.Player);
        _eventBus.Raise(new EmployeeCountChangedEvent(_gameState.currentTick, totalEmployees, employeeId, false));
        _eventBus.Raise(new FinanceChangedEvent(
            _gameState.currentTick,
            _gameState.financeState.money,
            _financeSystem.MonthlyExpenses,
            ComputeTotalRevenue()
        ));
    }

    private void OnContractRenewed(EmployeeId employeeId, int newSalary, int oldSalary)
    {
        var employee = _employeeSystem.GetEmployee(employeeId);
        if (employee != null && employee.ownerCompanyId == CompanyId.Player)
        {
            _financeSystem.AddRecurringCost(
                $"salary-{employeeId.Value}",
                FinanceCategory.Salary,
                newSalary,
                RecurringInterval.Monthly,
                $"employee-{employeeId.Value}"
            );
        }
        _eventBus.Raise(new ContractRenewedEvent(
            _gameState.currentTick, employeeId, newSalary, oldSalary));
        _eventBus.Raise(new FinanceChangedEvent(
            _gameState.currentTick,
            _gameState.financeState.money,
            _financeSystem.MonthlyExpenses,
            ComputeTotalRevenue()
        ));
    }

    private void OnContractRenewalRequested(EmployeeId employeeId)
    {
        var employee = _employeeSystem.GetEmployee(employeeId);
        if (employee == null) return;
        _eventBus.Raise(new ContractRenewalRequestedEvent(
            _gameState.currentTick,
            employeeId,
            employee.name,
            employee.renewalDemand,
            employee.contractExpiryTick
        ));
    }
    
    private void OnCandidatesGenerated(int tick, int count)
    {
        _interruptCandidateRefresh = true;
        _eventBus.Raise(new CandidatesGeneratedEvent(_gameState.currentTick, count));
    }
    
    private void OnTeamCreated(TeamId teamId)
    {
        var team = _teamSystem.GetTeam(teamId);
        if (team != null)
        {
            _eventBus.Raise(new TeamCreatedEvent(_gameState.currentTick, teamId, team.name));
        }
    }

    private void OnTeamDeleted(TeamId teamId)
    {
        _eventBus.Raise(new TeamDeletedEvent(_gameState.currentTick, teamId));
    }
    
    private void OnEmployeeAssignedToTeam(EmployeeId employeeId, TeamId teamId)
    {
        _eventBus.Raise(new EmployeeAssignedToTeamEvent(_gameState.currentTick, employeeId, teamId));
    }
    
    private void OnEmployeeRemovedFromTeam(EmployeeId employeeId, TeamId teamId)
    {
        _eventBus.Raise(new EmployeeRemovedFromTeamEvent(_gameState.currentTick, employeeId, teamId));
    }

    private void OnTeamCrunchModeChanged(TeamId teamId, bool isCrunching)
    {
        _eventBus.Raise(new CrunchModeChangedEvent(_gameState.currentTick, teamId, isCrunching));
    }
    
    private void OnContractAccepted(ContractId contractId)
    {
        _eventBus.Raise(new ContractAcceptedEvent(_gameState.currentTick, contractId));
    }
    
    private void OnContractAssigned(ContractId contractId, TeamId teamId)
    {
        _eventBus.Raise(new ContractAssignedEvent(_gameState.currentTick, contractId, teamId));
    }
    
    private void OnContractProgressUpdated(ContractId contractId)
    {
        var contract = _contractSystem.GetContract(contractId);
        if (contract != null)
        {
            _eventBus.Raise(new ContractProgressUpdatedEvent(_gameState.currentTick, contractId, contract.ProgressPercent, 0f));
        }
    }
    
    private void OnContractCompleted(ContractId contractId, int reward, int reputationReward, float quality)
    {
        _interruptContractEvent = true;
        // Note: ContractSystem already calls _financeSystem.AddMoney internally — do not add again.
        _reputationSystem.AddReputation(reputationReward);
        _reputationSystem.IncrementContractCount();

        // Apply morale bonus to team members
        var contract = _contractSystem.GetContract(contractId);
        if (contract != null && contract.AssignedTeamId.HasValue) {
            var team = _teamSystem.GetTeam(contract.AssignedTeamId.Value);
            if (team != null) {
                _moraleSystem.ApplyContractCompletedBonus(team.members, quality);
            }
        }

        ReputationTier currentTier = _reputationSystem.CurrentTier;
        int currentRep = _reputationSystem.GlobalReputation;

        _eventBus.Raise(new ContractCompletedEvent(_gameState.currentTick, contractId, reward));
        _eventBus.Raise(new FinanceChangedEvent(
            _gameState.currentTick,
            _gameState.financeState.money,
            _financeSystem.MonthlyExpenses,
            ComputeTotalRevenue()
        ));
        _eventBus.Raise(new ReputationChangedEvent(
            _gameState.currentTick,
            currentRep,
            currentTier,
            currentTier,
            reputationReward,
            _reputationSystem.CompanyFans,
            _reputationSystem.FanSentiment
        ));
    }
    
    private void OnContractFailed(ContractId contractId, string contractName, int reputationPenalty)
    {
        _interruptContractEvent = true;
        if (reputationPenalty > 0)
        {
            ReputationTier oldTier = _reputationSystem.CurrentTier;
            _reputationSystem.RemoveReputation(reputationPenalty);
            ReputationTier newTier = _reputationSystem.CurrentTier;
            int currentRep = _reputationSystem.GlobalReputation;
            
            _eventBus.Raise(new ReputationChangedEvent(
                _gameState.currentTick,
                currentRep,
                newTier,
                oldTier,
                -reputationPenalty,
                _reputationSystem.CompanyFans,
                _reputationSystem.FanSentiment
            ));
        }

        // Apply morale penalty to team members — scale by difficulty, clamp [5, 15]
        var contract = _contractSystem.GetContract(contractId);
        if (contract != null && contract.AssignedTeamId.HasValue) {
            var team = _teamSystem.GetTeam(contract.AssignedTeamId.Value);
            if (team != null) {
                int penalty = 5 + contract.Difficulty * 2;
                if (penalty < 5)  penalty = 5;
                if (penalty > 15) penalty = 15;
                _moraleSystem.ApplyContractFailedPenalty(team.members, penalty);
            }
        }
        
        _eventBus.Raise(new ContractFailedEvent(_gameState.currentTick, contractId, contractName, "Contract failed"));
    }
    
    private void OnContractExpired(ContractId contractId)
    {
        _eventBus.Raise(new ContractExpiredEvent(_gameState.currentTick, contractId));
    }
    
    private void OnPoolRerolled()
    {
        _eventBus.Raise(new PoolRerolledEvent(_gameState.currentTick));
    }

    private void OnCompetitorSpawned(CompetitorId competitorId)
    {
        _eventBus.Raise(new CompetitorSpawnedEvent(_gameState.currentTick, competitorId));
    }

    private void OnCompetitorBankrupt(CompetitorId competitorId)
    {
        _interruptCompetitorBankrupt = true;
        string companyName = "A competitor";
        if (_gameState.competitorState?.competitors != null
            && _gameState.competitorState.competitors.TryGetValue(competitorId, out var bankrupt))
            companyName = bankrupt.CompanyName;
        _inboxSystem?.AddMail(new MailItem
        {
            Tick     = _gameState.currentTick,
            Category = MailCategory.Alert,
            Priority = MailPriority.Warning,
            Title    = "Competitor Bankrupt",
            Body     = $"{companyName} has gone bankrupt.",
            IsRead   = false,
            Actions  = new[] { new MailAction { Label = "View Competitors", Type = MailActionType.Navigate, NavTargetInt = (int)ScreenId.CompetitorsList } }
        });
        _eventBus.Raise(new CompetitorBankruptEvent(_gameState.currentTick, competitorId));
        _logger.Log($"[GameController] Competitor {competitorId.Value} went bankrupt.");
    }

    private void OnCompetitorProductLaunched(CompetitorId competitorId, ProductId productId)
    {
        _interruptCompetitorProduct = true;
        string companyName = "A competitor";
        if (_gameState.competitorState?.competitors != null
            && _gameState.competitorState.competitors.TryGetValue(competitorId, out var comp))
            companyName = comp.CompanyName;
        _inboxSystem?.AddMail(new MailItem
        {
            Tick     = _gameState.currentTick,
            Category = MailCategory.Operations,
            Priority = MailPriority.Info,
            Title    = "Competitor Product Launch",
            Body     = $"{companyName} released a new product.",
            IsRead   = false,
            Actions  = new[] { new MailAction { Label = "View Competitors", Type = MailActionType.Navigate, NavTargetInt = (int)ScreenId.CompetitorsList } }
        });
        _eventBus.Raise(new CompetitorProductLaunchedEvent(_gameState.currentTick, competitorId, productId, default));
        _logger.Log($"[GameController] Competitor {competitorId.Value} launched product {productId.Value}.");
    }

    private void OnCompetitorDevStarted(CompetitorId competitorId, ProductId productId)
    {
        Product devProduct = null;
        _gameState.productState?.developmentProducts?.TryGetValue(productId, out devProduct);
        ProductNiche niche = devProduct?.Niche ?? default;
        _eventBus.Raise(new CompetitorDevStartedEvent(_gameState.currentTick, competitorId, productId, niche));
        _logger.Log($"[GameController] Competitor {competitorId.Value} started developing product {productId.Value}.");
    }

    private void OnCompanyAcquired(CompetitorId acquirerId, CompetitorId targetId)
    {
        string acquirerName = "A competitor";
        string targetName = "a company";
        var competitors = _gameState.competitorState?.competitors;
        if (competitors != null)
        {
            if (competitors.TryGetValue(acquirerId, out var acquirer)) acquirerName = acquirer.CompanyName;
            if (competitors.TryGetValue(targetId, out var target)) targetName = target.CompanyName;
        }
        _eventBus.Raise(new CompanyAcquiredEvent(_gameState.currentTick, acquirerId, targetId));
        if (_inboxSystem != null)
        {
            _inboxSystem.AddMail(new MailItem
            {
                Tick        = _gameState.currentTick,
                Category    = MailCategory.Alert,
                Priority    = MailPriority.Critical,
                Title       = "Corporate Acquisition",
                Body        = $"{acquirerName} acquired {targetName}.",
                IsRead      = false,
                IsDismissed = false,
                Actions     = new[] { new MailAction { Label = "View Competitors", Type = MailActionType.Navigate, NavTargetInt = (int)ScreenId.CompetitorsList } }
            });
        }
        _logger.Log($"[GameController] Acquisition: {acquirerName} acquired {targetName}.");
    }

    private void OnStockPlayerAcquired(CompetitorId acquirerId)
    {
        _interruptPlayerAcquired = true;
        StopAdvance();
        _eventBus.Raise(new PlayerAcquiredEvent(_gameState.currentTick, acquirerId));
        _logger.LogError($"[GameController] GAME OVER: Player company acquired by competitor {acquirerId.Value}!");
    }

    private void OnDividendPaid(CompetitorId ownerId, long amount)
    {
        if (ownerId.Value == 0)
        {
            _financeSystem.AddMoney((int)amount);
            _eventBus.Raise(new DividendPaidEvent(_gameState.currentTick, ownerId, amount));
            _inboxSystem?.AddMail(new MailItem
            {
                Tick     = _gameState.currentTick,
                Category = MailCategory.Finance,
                Priority = MailPriority.Info,
                Title    = "Dividend Received",
                Body     = $"You received a dividend payment of {MoneyFormatter.FormatShort((int)amount)}.",
                IsRead   = false,
                Actions  = new[] { new MailAction { Label = "View Finance", Type = MailActionType.Navigate, NavTargetInt = (int)ScreenId.FinanceOverview } }
            });
            _logger.Log($"[GameController] Player received dividend: ${amount:N0}.");
        }
    }
    
    private void OnReputationTierChanged(ReputationTier oldTier, ReputationTier newTier)
    {
        _interruptTierChange = true;
        _logger.Log($"Reputation tier changed from {oldTier} to {newTier}");
    }
    
    private void OnHRSearchCompleted(CandidateData candidate)
    {
        _interruptHRSearchComplete = true;
        _logger.Log($"[HRSystem] Search complete: {candidate.Name} ({candidate.Role}) is now available");
    }

    private void OnHRCandidatesReadyForReview(HRCandidatesReadyForReviewEvent evt)
    {
        _eventBus.Raise(evt);
        _logger.Log($"[HRSystem] Candidates ready for review: {evt.CandidateCount} by {evt.TeamName}");
    }

    private void OnHRCandidateAccepted(CandidateData candidate)
    {
        QueueCommand(new HireEmployeeCommand
        {
            Tick = _gameState.currentTick,
            CandidateId = candidate.CandidateId,
            Name = candidate.Name,
            Gender = candidate.Gender,
            Age = candidate.Age,
            Stats = candidate.Stats,
            HRSkill = candidate.HRSkill,
            Salary = candidate.Salary,
            Role = candidate.Role,
            PreferredRole = candidate.Role,
            PotentialAbility = candidate.Stats.PotentialAbility,
            Mode = HiringMode.HR,
            Personality = candidate.personality
        });
        _logger.Log($"[HRSystem] Accepted HR candidate {candidate.Name} — queued HireEmployeeCommand");
    }

    private void OnHRPoolFull(int poolCount, int poolMax, int rejectedCount)
    {
        _eventBus.Raise(new CandidatePoolFullEvent(_gameState.currentTick, poolCount, poolMax, rejectedCount));
    }
    
    private void OnProductLaunched(ProductId productId, int launchRevenue)
    {
        if (_gameState.productState.shippedProducts.TryGetValue(productId, out var launched) && launched.IsCompetitorProduct) return;
        _interruptContractEvent = true;
        _eventBus.Raise(new FinanceChangedEvent(
            _gameState.currentTick,
            _gameState.financeState.money,
            _financeSystem.MonthlyExpenses,
            ComputeTotalRevenue()
        ));
        _logger.Log($"[GameController] Product {productId.Value} launched — ${launchRevenue} launch revenue");
    }

    private void OnProductLifecycleChanged(ProductId productId, ProductLifecycleStage oldStage, ProductLifecycleStage newStage)
    {
        if (newStage == ProductLifecycleStage.Decline)
            _interruptContractEvent = true;
        _logger.Log($"[GameController] Product {productId.Value} lifecycle: {oldStage} → {newStage}");
    }

    private void OnProductDead(ProductId productId)
    {
        _interruptContractEvent = true;
        _logger.Log($"[GameController] Product {productId.Value} is dead (no users remaining)");
    }

    private void OnProductSaleStarted(ProductId productId)
    {
        _logger.Log($"[GameController] Product {productId.Value} sale started.");
    }

    private void OnProductSaleEnded(ProductId productId)
    {
        _logger.Log($"[GameController] Product {productId.Value} sale ended.");
    }

    private void OnMoraleEmployeeMayQuit(EmployeeId employeeId)
    {
        var employees = _gameState.employeeState.employees;
        if (employees.TryGetValue(employeeId, out var emp) && emp.isFounder) return;
        _employeeSystem.QuitEmployee(employeeId);
    }

    private void OnCandidateWithdrew(CandidateWithdrewEvent evt)
    {
        _interruptCandidateExpired = true;
    }

    private void OnCandidateLostPatience(CandidateLostPatienceEvent evt)
    {
        int candidateCount = _gameState.employeeState.availableCandidates.Count;
        for (int i = candidateCount - 1; i >= 0; i--)
        {
            if (_gameState.employeeState.availableCandidates[i].CandidateId == evt.CandidateId)
            {
                _gameState.employeeState.availableCandidates.RemoveAt(i);
                break;
            }
        }
        _interruptCandidateExpired = true;
    }

    private void OnEmployeeFrustrated(EmployeeFrustratedEvent evt)
    {
        if (_moraleSystem != null)
            _moraleSystem.ApplyDirectMoraleDelta(evt.EmployeeId, -15f);
    }

    private void OnCompetitorHiredCandidate(CompetitorHiredCandidateEvent evt)
    {
        if (_inboxSystem == null) return;
        _inboxSystem.AddMail(new MailItem {
            Tick     = evt.Tick,
            Category = MailCategory.Recruitment,
            Priority = MailPriority.Info,
            Title    = $"{evt.CompanyName} hired a {evt.Role}",
            Body     = $"{evt.CompanyName} hired {evt.CandidateName} as a {evt.Role} from the talent pool.",
            Actions  = new MailAction[0]
        });
    }

    private void OnCandidatesGeneratedClearPipeline(int tick, int count)
    {
        _interviewSystem.ClearAll();
        _negotiationSystem.ClearAll();
        CandidateExpiryHelper.AssignExpiryTicks(_gameState.employeeState, _moraleRng, tick, _tuning);
    }

    private void OnInterviewThresholdReached(int candidateId, int threshold)
    {
        if (threshold == 100)
        {
            _interruptInterviewComplete = true;
            StopAdvance();
            _logger.Log($"[GameController] Interview complete for candidate id:{candidateId} — simulation paused");
        }
    }

    private void OnNegotiationAccepted(int candidateId, int agreedSalary, EmploymentOffer offer)
    {
        // Find the candidate and auto-trigger the hire command
        int candidateCount = _gameState.employeeState.availableCandidates.Count;
        for (int i = 0; i < candidateCount; i++)
        {
            var candidate = _gameState.employeeState.availableCandidates[i];
            if (candidate.CandidateId == candidateId)
            {
                var hireCmd = new HireEmployeeCommand
                {
                    Tick = _gameState.currentTick,
                    CandidateId = candidateId,
                    Name = candidate.Name,
                    Gender = candidate.Gender,
                    Age = candidate.Age,
                    Stats = candidate.Stats,
                    HRSkill = candidate.HRSkill,
                    Salary = agreedSalary,
                    Role = offer.Role != default ? offer.Role : candidate.Role,
                    PotentialAbility = candidate.Stats.PotentialAbility,
                    Personality = candidate.personality,
                    EmploymentType = offer.Type,
                    ContractLength = offer.Length,
                    PreferredRole = candidate.Role
                };
                QueueCommand(hireCmd);
                break;
            }
        }
    }

    private void OnNegotiationRejected(int candidateId)
    {
        string candidateName = "A candidate";
        int declineExpiryTick = _gameState.currentTick + 7 * TimeState.TicksPerDay;
        int candidateCount = _gameState.employeeState.availableCandidates.Count;
        for (int i = 0; i < candidateCount; i++)
        {
            if (_gameState.employeeState.availableCandidates[i].CandidateId == candidateId)
            {
                candidateName = _gameState.employeeState.availableCandidates[i].Name;
                break;
            }
        }
        _eventBus.Raise(new CandidateDeclinedEvent(
            _gameState.currentTick, candidateId, candidateName,
            string.Empty, declineExpiryTick, DeclineReason.SalaryTooLow));
    }
    
    private void OnProductShipWarning(ProductId productId, string productName, int incompletePhasesCount, int daysRemaining)
    {
        string phaseText = incompletePhasesCount == 1 ? "1 phase" : incompletePhasesCount + " phases";
        string body = productName + " auto-ships in " + daysRemaining + " day" + (daysRemaining == 1 ? "" : "s") + " with " + phaseText + " still incomplete.";
        _inboxSystem?.AddMail(new MailItem
        {
            Tick     = _gameState.currentTick,
            Category = MailCategory.Alert,
            Priority = MailPriority.Warning,
            Title    = "Upcoming Auto-Ship Warning",
            Body     = body,
            IsRead   = false,
            Actions  = new[] { new MailAction { Label = "View Products", Type = MailActionType.Navigate, NavTargetInt = (int)ScreenId.ProductionProductsInDev } }
        });
        _eventBus.Raise(new ProductShipWarningEvent(_gameState.currentTick, productId, productName, incompletePhasesCount, daysRemaining));
    }

    private void OnMinorDisruptionStampProjection(ActiveDisruption disruption)
    {
        _marketSystem.StampDisruptionIntoProjection(disruption, _gameState.timeState.currentDay);
    }

    private void OnMajorDisruptionStampProjection(ActiveDisruption disruption)
    {
        _marketSystem.StampDisruptionIntoProjection(disruption, _gameState.timeState.currentDay);
    }

    private void OnMinorDisruptionStarted(ActiveDisruption disruption)
    {
        _logger.Log($"[GameController] Minor disruption: {disruption.EventType} — {disruption.Description}");
        _inboxSystem?.AddMail(new MailItem
        {
            Tick = _gameState.currentTick,
            Title = "Market Disruption",
            Body = disruption.Description,
            Category = MailCategory.Operations,
            Priority = MailPriority.Info,
            IsRead = false
        });
        _eventBus.Raise(new MinorDisruptionStartedEvent(_gameState.currentTick, disruption));
    }

    private void OnMajorDisruptionStarted(ActiveDisruption disruption)
    {
        _interruptMarketDemand = true;
        StopAdvance();
        _logger.LogWarning($"[GameController] Major disruption: {disruption.EventType} — {disruption.Description}");
        _inboxSystem?.AddMail(new MailItem
        {
            Tick = _gameState.currentTick,
            Title = "Major Market Disruption",
            Body = disruption.Description,
            Category = MailCategory.Alert,
            Priority = MailPriority.Critical,
            IsRead = false
        });
        _eventBus.Raise(new MajorDisruptionStartedEvent(_gameState.currentTick, disruption));
    }

    private void OnDisruptionEnded(ActiveDisruption disruption)
    {
        _logger.Log($"[GameController] Disruption ended: {disruption.EventType}");
    }

    private void OnProductCrisis(ProductId productId, CrisisEventType crisisType)
    {
        if (_gameState.productState.shippedProducts.TryGetValue(productId, out var product))
        {
            if (product.IsCompetitorProduct)
            {
                _eventBus.Raise(new ProductCrisisEvent(_gameState.currentTick, productId, crisisType, product.ProductName));
                return;
            }
            _interruptContractEvent = true;
            StopAdvance();
            _logger.LogWarning($"[GameController] Product crisis: {product.ProductName} — {crisisType}");
            _eventBus.Raise(new ProductCrisisEvent(_gameState.currentTick, productId, crisisType, product.ProductName));
        }
    }

    private void OnProductSold(ProductId productId, CompetitorId buyerId, long salePrice)
    {
        _logger.Log($"[GameController] Product sold: id={productId.Value}, buyer={buyerId.Value}, price={salePrice}");
        _eventBus.Raise(new ProductSoldEvent(_gameState.currentTick, productId, buyerId, salePrice));
    }

    private void OnShowdownResolved(ProductNiche niche, ShowdownResult showdown)
    {
        _logger.Log($"[GameController] Showdown in {niche}: {showdown.WinnerName} beats {showdown.LoserName}");
        _inboxSystem?.AddMail(new MailItem
        {
            Tick = _gameState.currentTick,
            Title = $"Market Showdown — {niche}",
            Body = $"{showdown.WinnerName} launched to dominate while {showdown.LoserName} suffers user loss.",
            Category = MailCategory.Operations,
            Priority = MailPriority.Info,
            IsRead = false
        });
        bool playerInvolved = false;
        if (_gameState.productState.shippedProducts.TryGetValue(showdown.LoserId, out var loserProduct))
            playerInvolved = !loserProduct.IsCompetitorProduct;
        if (!playerInvolved && _gameState.productState.shippedProducts.TryGetValue(showdown.WinnerId, out var winnerProduct))
            playerInvolved = !winnerProduct.IsCompetitorProduct;
        if (playerInvolved)
        {
            _interruptMarketDemand = true;
            StopAdvance();
        }
        _eventBus.Raise(new ShowdownResolvedEvent(_gameState.currentTick, niche, showdown));
    }

    public void QueueCommand(ICommand cmd)
    {
        _commandQueue.Enqueue(cmd);
    }

    public void SaveCurrentGame(string slotName, string displayName, bool isAutoSave = false)
    {
        var rngCounts = RngStateTracker.GetInvocationCounts();
        SaveManager.SaveGame(_gameState, rngCounts, slotName, displayName, isAutoSave);
    }
    
    public void PauseSimulation()
    {
        StopAdvance();
    }
    
    public void ResumeSimulation()
    {
        StartAdvance();
    }
    
    public void TogglePause()
    {
        if (_isAdvancing)
            StopAdvance();
        else
            StartAdvance();
    }
    
    public void SkipTicks(int ticksToSkip)
    {
        Debug.Assert(!_isAdvancing, "[GameController] SkipTicks called while advancing — save guard");
        for (int i = 0; i < ticksToSkip; i++)
        {
            SimTick(_gameState.currentTick);
            _gameState.currentTick++;
        }
    }
    
    public int GetDeterministicSeed(string context)
    {
        if (_gameState == null) return 0;
        
        int contextHash = context.GetHashCode();
        int tickComponent = _gameState.currentTick;
        
        return _gameState.masterSeed ^ contextHash ^ tickComponent;
    }
    
    private void OnDestroy()
    {
        _autoSaveSystem?.Dispose();
        _autoSaveSystem = null;

        if (_advanceCoroutine != null)
        {
            StopCoroutine(_advanceCoroutine);
            _advanceCoroutine = null;
        }
        
        if (_timeSystem != null)
        {
            _timeSystem.OnDayChanged -= OnDayChanged;
            _timeSystem.OnMonthChanged -= OnMonthChanged;
            _timeSystem.OnYearChanged -= OnYearChanged;
        }
        
        if (_financeSystem != null)
        {
            _financeSystem.OnBankrupt -= OnBankrupt;
            _financeSystem.OnBankruptcyWarning -= OnBankruptcyWarning;
        }

        _eventBus?.Unsubscribe<TaxBankruptcyEvent>(OnTaxBankruptcyEvent);
        
        if (_employeeSystem != null)
        {
            _employeeSystem.OnEmployeeHired -= OnEmployeeHired;
            _employeeSystem.OnEmployeeFired -= OnEmployeeFired;
            _employeeSystem.OnEmployeeQuit -= OnEmployeeQuit;
            _employeeSystem.OnCandidatesGenerated -= OnCandidatesGenerated;
            _employeeSystem.OnCandidatesGenerated -= OnCandidatesGeneratedClearPipeline;
            _employeeSystem.OnContractRenewed -= OnContractRenewed;
            _employeeSystem.OnContractRenewalRequested -= OnContractRenewalRequested;
            if (_recruitmentReputationSystem != null)
            {
                _employeeSystem.OnEmployeeHired -= _recruitmentReputationSystem.OnEmployeeHiredHandler;
                _employeeSystem.OnEmployeeFired -= _recruitmentReputationSystem.OnEmployeeFiredHandler;
                _employeeSystem.OnEmployeeQuit -= _recruitmentReputationSystem.OnEmployeeQuitHandler;
            }
        }
        
        if (_hrSystem != null)
        {
            _hrSystem.OnSearchCompleted -= OnHRSearchCompleted;
            _hrSystem.OnCandidatesReadyForReview -= OnHRCandidatesReadyForReview;
            _hrSystem.OnCandidateAccepted -= OnHRCandidateAccepted;
            _hrSystem.OnPoolFull -= OnHRPoolFull;
        }

        if (_negotiationSystem != null)
        {
            if (_recruitmentReputationSystem != null)
            {
                _negotiationSystem.OnOfferRejected -= _recruitmentReputationSystem.OnOfferRejectedHandler;
            }
            _negotiationSystem.OnOfferAccepted -= OnNegotiationAccepted;
            _negotiationSystem.OnOfferRejected -= OnNegotiationRejected;
        }

        if (_interviewSystem != null)
        {
            _interviewSystem.OnInterviewThresholdReached -= OnInterviewThresholdReached;
        }
        
        if (_teamSystem != null)
        {
            _teamSystem.OnTeamCreated -= OnTeamCreated;
            _teamSystem.OnTeamDeleted -= OnTeamDeleted;
            _teamSystem.OnEmployeeAssignedToTeam -= OnEmployeeAssignedToTeam;
            _teamSystem.OnEmployeeRemovedFromTeam -= OnEmployeeRemovedFromTeam;
            _teamSystem.OnCrunchModeChanged -= OnTeamCrunchModeChanged;
        }
        
        if (_contractSystem != null)
        {
            _contractSystem.OnContractAccepted -= OnContractAccepted;
            _contractSystem.OnContractAssigned -= OnContractAssigned;
            _contractSystem.OnContractProgressUpdated -= OnContractProgressUpdated;
            _contractSystem.OnContractCompleted -= OnContractCompleted;
            _contractSystem.OnContractFailed -= OnContractFailed;
            _contractSystem.OnContractExpired -= OnContractExpired;
            _contractSystem.OnPoolRerolled -= OnPoolRerolled;
        }

        if (_moraleSystem != null)
        {
            _moraleSystem.OnEmployeeMayQuit -= OnMoraleEmployeeMayQuit;
        }
        
        if (_reputationSystem != null)
        {
            _reputationSystem.OnTierChanged -= OnReputationTierChanged;
        }
        
        if (_competitorSystem != null)
        {
            _competitorSystem.OnCompetitorBankrupt -= OnCompetitorBankrupt;
            _competitorSystem.OnCompetitorProductLaunched -= OnCompetitorProductLaunched;
            _competitorSystem.OnCompetitorSpawned -= OnCompetitorSpawned;
            _competitorSystem.OnCompetitorDevStarted -= OnCompetitorDevStarted;
        }

        if (_stockSystem != null)
        {
            _stockSystem.OnCompanyAcquired -= OnCompanyAcquired;
            _stockSystem.OnPlayerAcquired  -= OnStockPlayerAcquired;
            _stockSystem.OnDividendPaid    -= OnDividendPaid;
        }

        if (_disruptionSystem != null)
        {
            _disruptionSystem.OnMinorDisruptionStarted -= OnMinorDisruptionStarted;
            _disruptionSystem.OnMinorDisruptionStarted -= OnMinorDisruptionStampProjection;
            _disruptionSystem.OnMajorDisruptionStarted -= OnMajorDisruptionStarted;
            _disruptionSystem.OnMajorDisruptionStarted -= OnMajorDisruptionStampProjection;
            _disruptionSystem.OnDisruptionEnded -= OnDisruptionEnded;
        }

        if (_productSystem != null)
        {
            _productSystem.OnProductCrisis -= OnProductCrisis;
            _productSystem.OnProductSold -= OnProductSold;
        }

        if (_marketSystem != null)
        {
            _marketSystem.OnShowdownResolved -= OnShowdownResolved;
        }

        _competitorContractBridge?.Dispose();
        _competitorContractBridge = null;

        if (_systems != null)
        {
            for (int i = 0; i < _systems.Count; i++)
            {
                _systems[i]?.Dispose();
            }
            _systems.Clear();
        }

        _inboxSystem?.Dispose();
    }
}
