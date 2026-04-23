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
    private LoanSystem _loanSystem;
    private InterviewSystem _interviewSystem;
    private NegotiationSystem _negotiationSystem;
    private HRSystem _hrSystem;
    private RecruitmentReputationSystem _recruitmentReputationSystem;
    private IRng _moraleRng;
    private InboxSystem _inboxSystem;
    private RoleTierTable _roleTierTable;
    private AbilitySystem _abilitySystem;
    private TuningConfig _tuning;
    private ProductSystem _productSystem;
    private MarketSystem _marketSystem;
    private AutoSaveSystem _autoSaveSystem;
    private CompetitorSystem _competitorSystem;
    private CompetitorContractBridge _competitorContractBridge;
    private StockSystem _stockSystem;
    
    private AIDecisionSystem _aiDecisionSystem;
    private DisruptionSystem _disruptionSystem;
    private TaxSystem _taxSystem;
    private PlatformSystem _platformSystem;
    private GenerationSystem _generationSystem;
    private bool _isAdvancing;
    private bool _stopAdvanceRequested;
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
            p.PreviousDailyActiveUsers = p.ActiveUserCount;
            p.PreviousMonthActiveUsers = p.ActiveUserCount;
            p.PreviousMonthlyRevenue = p.MonthlyRevenue;
            if (p.HasCompletedFirstMonth) {
                p.SnapshotMonthlyUsers = p.ActiveUserCount;
                p.SnapshotMonthlyRevenue = (long)p.MonthlyRevenue;
                p.SnapshotMonthlySales = p.ActiveUserCount;
                if (p.IsCompetitorProduct && p.TicksSinceShip > 0) {
                    int ageInMonths = p.TicksSinceShip / (TimeState.TicksPerDay * 30);
                    if (ageInMonths > 0) {
                        p.TotalLifetimeRevenue = (long)p.MonthlyRevenue * ageInMonths;
                        if (!p.IsSubscriptionBased) {
                            float unitPrice = _productSystem.GetCompetitorUnitPrice(p);
                            if (unitPrice > 0f) {
                                int currentMonthlySales = (int)(p.MonthlyRevenue / unitPrice);
                                p.TotalUnitsSold = currentMonthlySales * ageInMonths;
                                p.PreviousMonthUnitsSold = p.TotalUnitsSold;
                                p.PeakMonthlySales = Math.Max(p.PeakMonthlySales, currentMonthlySales);
                            }
                        }
                    }
                }
            }
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
            int[] skills = GenerateFounderSkills(data.Tier, data.Role, founderRng);

            var emp = new Employee(
                empId,
                data.Name,
                data.Gender,
                data.Age,
                skills,
                salary: 0,
                hireDate: _gameState.currentTick,
                data.Role
            );

            emp.isFounder = true;
            emp.potentialAbility = 200;
            emp.hiddenAttributes = new HiddenAttributes {
                LearningRate = hiddenRng.Range(15, 21),
                Creative = hiddenRng.Range(15, 21),
                WorkEthic = hiddenRng.Range(15, 21),
                Adaptability = hiddenRng.Range(15, 21),
                Ambition = hiddenRng.Range(15, 21)
            };
            emp.contractExpiryTick = int.MaxValue;
            emp.morale = 100;
            emp.isActive = true;
            emp.salary = 0;

            _gameState.employeeState.employees[empId] = emp;
        }

        NewGameData.Clear();
    }

    private static readonly int[] FallbackTiersDeveloper    = { 2, 3, 3, 3, 4, 4, 4, 4, 4 };
    private static readonly int[] FallbackTiersDesigner     = { 3, 2, 4, 3, 3, 4, 4, 4, 4 };
    private static readonly int[] FallbackTiersQAEngineer   = { 3, 3, 2, 4, 4, 4, 3, 4, 4 };
    private static readonly int[] FallbackTiersHR           = { 4, 4, 4, 4, 4, 2, 3, 3, 3 };
    private static readonly int[] FallbackTiersSoundEngineer = { 3, 3, 4, 3, 2, 4, 4, 4, 4 };
    private static readonly int[] FallbackTiersVFXArtist    = { 3, 3, 4, 2, 3, 4, 4, 4, 4 };
    private static readonly int[] FallbackTiersAccountant   = { 3, 4, 4, 4, 4, 4, 3, 2, 3 };
    private static readonly int[] FallbackTiersMarketer     = { 4, 3, 4, 4, 4, 3, 3, 4, 2 };

    private static int[] GetFallbackTiers(EmployeeRole role)
    {
        switch (role)
        {
            case EmployeeRole.Developer:    return FallbackTiersDeveloper;
            case EmployeeRole.Designer:     return FallbackTiersDesigner;
            case EmployeeRole.QAEngineer:   return FallbackTiersQAEngineer;
            case EmployeeRole.HR:           return FallbackTiersHR;
            case EmployeeRole.SoundEngineer: return FallbackTiersSoundEngineer;
            case EmployeeRole.VFXArtist:    return FallbackTiersVFXArtist;
            case EmployeeRole.Accountant:   return FallbackTiersAccountant;
            case EmployeeRole.Marketer:     return FallbackTiersMarketer;
            default:                        return FallbackTiersDeveloper;
        }
    }

    private int[] GenerateFounderSkills(int tier, EmployeeRole role, IRng rng)
    {
        int[] roleTiers = _roleTierTable != null
            ? _roleTierTable.GetTiers(role)
            : GetFallbackTiers(role);

        const int skillCount = 9;
        var skills = new int[skillCount];
        for (int i = 0; i < skillCount; i++)
        {
            int min, max;
            int weight = roleTiers[i]; // 2=Primary, 3=Secondary, 4=Tertiary
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
                default: // tier 5
                    if (weight == 2) { min = 16; max = 20; }
                    else if (weight == 3) { min = 9; max = 13; }
                    else { min = 4; max = 7; }
                    break;
            }
            skills[i] = rng.Range(min, max + 1);
        }

        // Identity clamp: the Primary skill (tier value 2) must exceed all others by at least 1
        int identityIdx = -1;
        for (int i = 0; i < skillCount; i++)
        {
            if (roleTiers[i] == 2) { identityIdx = i; break; }
        }
        if (identityIdx >= 0)
        {
            int maxOther = 0;
            for (int i = 0; i < skillCount; i++)
            {
                if (i != identityIdx && skills[i] > maxOther) maxOther = skills[i];
            }
            if (skills[identityIdx] <= maxOther)
            {
                skills[identityIdx] = maxOther + 1 > 20 ? 20 : maxOther + 1;
            }
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
        if (_gameState.loanState == null) _gameState.loanState = LoanState.CreateNew();

        _moraleRng = RngFactory.CreateStream(_gameState.masterSeed, "morale");

        _moraleSystem = new MoraleSystem(_gameState.moraleState, _employeeSystem, _gameState.teamState, _gameState.contractState, _gameState.productState, _eventBus, _logger);
        _loanSystem = new LoanSystem(_gameState.loanState, _reputationSystem, _financeSystem, _logger);

        // Hiring pipeline systems
        if (_gameState.interviewState == null) _gameState.interviewState = InterviewState.CreateNew();
        if (_gameState.negotiationState == null) _gameState.negotiationState = NegotiationState.CreateNew();
        if (_gameState.hrState == null) _gameState.hrState = HRState.CreateNew();
        if (_gameState.recruitmentReputationState == null) _gameState.recruitmentReputationState = RecruitmentReputationState.CreateNew();

        IRng interviewRng = RngFactory.CreateStream(_gameState.masterSeed, "interviews");
        IRng headhuntingRng = RngFactory.CreateStream(_gameState.masterSeed, "headhunting");
        _interviewSystem = new InterviewSystem(_gameState.interviewState, _gameState.employeeState, _financeSystem, _eventBus, _logger, interviewRng);
        _negotiationSystem = new NegotiationSystem(_gameState.negotiationState, _gameState.employeeState, _interviewSystem, _logger);
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
        _negotiationSystem.SetHRSystem(_hrSystem);
        _interviewSystem.SetHRSystem(_hrSystem);

        // AbilitySystem — CA/PA system
        _roleTierTable = new RoleTierTable();
        var roleTierProfiles = Resources.LoadAll<RoleTierProfile>("RoleTiers");
        for (int p = 0; p < roleTierProfiles.Length; p++)
            _roleTierTable.Register(roleTierProfiles[p]);

        IRng abilityRng = RngFactory.CreateStream(_gameState.masterSeed, "ability");
        _abilitySystem = new AbilitySystem(_gameState.employeeState, _roleTierTable, abilityRng, _logger);
        _employeeSystem.SetAbilitySystem(_abilitySystem);
        _employeeSystem.SetRoleTierTable(_roleTierTable);
        _employeeSystem.SetEventBus(_eventBus);
        _hrSystem.SetAbilitySystem(_abilitySystem);
        _contractSystem.SetSkillGrowthDependencies(_roleTierTable, _abilitySystem);
        _productSystem.SetFinanceSystem(_financeSystem);
        _productSystem.SetTeamSystem(_teamSystem);
        _productSystem.SetEmployeeSystem(_employeeSystem);
        _productSystem.SetReputationSystem(_reputationSystem);
        _productSystem.SetSkillGrowthDependencies(_roleTierTable, _abilitySystem);
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
        _systems.Add(_moraleSystem);
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
        _interviewSystem.OnInterviewFirstReportReady += OnInterviewFirstReportReady;
        _interviewSystem.OnInterviewFinalReportReady += OnInterviewFinalReportReady;

        // New subscriptions for interrupt conditions
        _hrSystem.OnSearchCompleted += OnHRSearchCompleted;
        _hrSystem.OnCandidatesReadyForReview += OnHRCandidatesReadyForReview;
        _hrSystem.OnCandidateAccepted += OnHRCandidateAccepted;
        _eventBus.Subscribe<CandidateWithdrewEvent>(OnCandidateWithdrew);
        _eventBus.Subscribe<CompetitorHiredCandidateEvent>(OnCompetitorHiredCandidate);

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

    // Back-fill potentialAbility and hiddenAttributes for employees and candidates that
    // pre-date this system (PA == 0 means not yet generated). Uses a deterministic per-entity
    // RNG seeded by entity ID so the same save always back-fills identically.
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

        // Version 1 → 2: divide all employee skill values by 5 (0-100 → 0-20 scale),
        // and divide HiddenAttributes by 5 (0-100 → 0-20).
        if (_gameState.version < 2)
        {
            foreach (var kvp in _gameState.employeeState.employees)
            {
                var emp = kvp.Value;
                if (emp == null) continue;
                int skillCount = emp.skills != null ? emp.skills.Length : 0;
                for (int i = 0; i < skillCount; i++)
                {
                    emp.skills[i] = UnityEngine.Mathf.Clamp(emp.skills[i] / 5, 0, 20);
                }
                emp.hrSkill = UnityEngine.Mathf.Clamp(emp.hrSkill / 5, 0, 20);

                // Migrate HiddenAttributes from 0-100 to 0-20
                // Only migrate if values look like the old scale (> 20 suggests 0-100)
                if (emp.hiddenAttributes.LearningRate > 20 ||
                    emp.hiddenAttributes.Creative     > 20 ||
                    emp.hiddenAttributes.WorkEthic    > 20 ||
                    emp.hiddenAttributes.Adaptability > 20 ||
                    emp.hiddenAttributes.Ambition     > 20)
                {
                    emp.hiddenAttributes = new HiddenAttributes
                    {
                        LearningRate = UnityEngine.Mathf.Clamp(emp.hiddenAttributes.LearningRate / 5, 1, 20),
                        Creative     = UnityEngine.Mathf.Clamp(emp.hiddenAttributes.Creative     / 5, 1, 20),
                        WorkEthic    = UnityEngine.Mathf.Clamp(emp.hiddenAttributes.WorkEthic    / 5, 1, 20),
                        Adaptability = UnityEngine.Mathf.Clamp(emp.hiddenAttributes.Adaptability / 5, 1, 20),
                        Ambition     = UnityEngine.Mathf.Clamp(emp.hiddenAttributes.Ambition     / 5, 1, 20)
                    };
                }
            }
            _gameState.version = 2;
            _logger.Log("[GameController] Save migration v1→v2 complete: skills and hidden attributes divided by 5.");
        }

        // Version 2 → 3: skills array shrinks from 10 to 8 (Art2D index 3 and Art3D index 4 removed).
        // Design (index 1) absorbs the higher of Art2D/Art3D. Remaining skills shift left.
        // Candidates are regenerated fresh in-game, so only employees need migration.
        if (_gameState.version < 3)
        {
            foreach (var kvp in _gameState.employeeState.employees)
            {
                var emp = kvp.Value;
                if (emp == null) continue;
                if (emp.skills == null || emp.skills.Length != 10) continue;

                // Merge Art2D (old idx 3) and Art3D (old idx 4) into Design (idx 1)
                int art2d = emp.skills[3];
                int art3d = emp.skills[4];
                int designMerged = UnityEngine.Mathf.Max(emp.skills[1], art2d, art3d);

                emp.skills = new int[8]
                {
                    emp.skills[0],  // Programming   (was 0)
                    designMerged,   // Design         (absorbs 1, 3, 4)
                    emp.skills[2],  // QA             (was 2)
                    emp.skills[5],  // VFX            (was 5 → now 3)
                    emp.skills[6],  // SFX            (was 6 → now 4)
                    emp.skills[7],  // HR             (was 7 → now 5)
                    emp.skills[8],  // Negotiation    (was 8 → now 6)
                    emp.skills[9],  // Accountancy    (was 9 → now 7)
                };

                // Resize XP and delta arrays to match
                if (emp.skillXp != null && emp.skillXp.Length == 10)
                {
                    float xpDesign = UnityEngine.Mathf.Max(emp.skillXp[1], emp.skillXp[3], emp.skillXp[4]);
                    emp.skillXp = new float[8]
                    {
                        emp.skillXp[0], xpDesign, emp.skillXp[2],
                        emp.skillXp[5], emp.skillXp[6], emp.skillXp[7], emp.skillXp[8], emp.skillXp[9]
                    };
                }
                if (emp.skillDeltaDirection != null && emp.skillDeltaDirection.Length == 10)
                {
                    emp.skillDeltaDirection = new sbyte[8]
                    {
                        emp.skillDeltaDirection[0], emp.skillDeltaDirection[1], emp.skillDeltaDirection[2],
                        emp.skillDeltaDirection[5], emp.skillDeltaDirection[6], emp.skillDeltaDirection[7],
                        emp.skillDeltaDirection[8], emp.skillDeltaDirection[9]
                    };
                }
            }
            _gameState.version = 3;
            _logger.Log("[GameController] Save migration v2→v3 complete: Art2D/Art3D merged into Design; skills array resized 10→8.");
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
            foreach (var kvp in _gameState.employeeState.employees) {
                var emp = kvp.Value;
                if (emp == null) continue;
                if (emp.skills != null && emp.skills.Length < SkillTypeHelper.SkillTypeCount) {
                    var old = emp.skills;
                    emp.skills = new int[SkillTypeHelper.SkillTypeCount];
                    for (int i = 0; i < old.Length; i++) emp.skills[i] = old[i];
                }
                if (emp.skillXp != null && emp.skillXp.Length < SkillTypeHelper.SkillTypeCount) {
                    var old = emp.skillXp;
                    emp.skillXp = new float[SkillTypeHelper.SkillTypeCount];
                    for (int i = 0; i < old.Length; i++) emp.skillXp[i] = old[i];
                }
                if (emp.skillDeltaDirection != null && emp.skillDeltaDirection.Length < SkillTypeHelper.SkillTypeCount) {
                    var old = emp.skillDeltaDirection;
                    emp.skillDeltaDirection = new sbyte[SkillTypeHelper.SkillTypeCount];
                    for (int i = 0; i < old.Length; i++) emp.skillDeltaDirection[i] = old[i];
                }
            }
            var v5candidates = _gameState.employeeState.availableCandidates;
            if (v5candidates != null) {
                for (int i = 0; i < v5candidates.Count; i++) {
                    var c = v5candidates[i];
                    if (c == null) continue;
                    if (c.Skills != null && c.Skills.Length < SkillTypeHelper.SkillTypeCount) {
                        var old = c.Skills;
                        c.Skills = new int[SkillTypeHelper.SkillTypeCount];
                        for (int j = 0; j < old.Length; j++) c.Skills[j] = old[j];
                    }
                }
            }
            _gameState.version = 5;
            _logger.Log("[GameController] Save migration v4->v5 complete: Marketing skill slot added.");
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
                    mdata.crunchDaysActive = 0;
                    mdata.recentCrunchDays = 0;
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
        foreach (var kvp in _gameState.employeeState.employees)
        {
            var emp = kvp.Value;
            if (emp == null) continue;

            if (emp.skillXp == null)
                emp.skillXp = new float[SkillTypeHelper.SkillTypeCount];
            if (emp.skillDeltaDirection == null)
                emp.skillDeltaDirection = new sbyte[SkillTypeHelper.SkillTypeCount];

            // Always recompute PA — use progressive Ability formula with tier multipliers.
            var migRng = new RngStream(emp.id.Value);
            int[] tiers = _roleTierTable.GetTiers(emp.role);
            int ability = AbilityCalculator.ComputeAbility(emp.skills, tiers);
            _logger.Log($"[GameController] Migration: {emp.name} ({emp.role}) Ability={ability} (progressive formula)");

            int uplift = migRng.Range(8, 82);
            int pa = ability + uplift;
            if (pa < 10)  pa = 10;
            if (pa > 200) pa = 200;
            emp.potentialAbility = pa;
            emp.hiddenAttributes = GenerateMigrationHiddenAttributes(migRng, pa);
        }

        var candidates = _gameState.employeeState.availableCandidates;
        if (candidates != null)
        {
            for (int i = 0; i < candidates.Count; i++)
            {
                var c = candidates[i];
                if (c == null) continue;

                // Always recompute for the same reason as employees above.
                var migRng = new RngStream(c.CandidateId + 500000);
                int[] tiers = _roleTierTable.GetTiers(c.Role);
                int ability = AbilityCalculator.ComputeAbility(c.Skills, tiers);

                int uplift = migRng.Range(8, 82);
                int pa = ability + uplift;
                if (pa < 10)  pa = 10;
                if (pa > 200) pa = 200;
                c.PotentialAbility = pa;
                c.HiddenAttributes = GenerateMigrationHiddenAttributes(migRng, pa);
            }
        }
        _logger.Log("[GameController] Ability data migration complete.");
    }

    // Deterministic PA-linked HiddenAttributes for migration back-fill.
    // Mirrors AbilitySystem.GenerateHiddenAttributesForPA logic without a class dependency.
    private static HiddenAttributes GenerateMigrationHiddenAttributes(IRng rng, int pa)
    {
        int floor = pa / 20;
        if (floor < 1) floor = 1;
        if (floor > 10) floor = 10;
        int spread = (20 - floor) / 2 + 1;

        int Attr()
        {
            int v = rng.Range(floor, floor + spread + 1) + rng.Range(-1, 2);
            return v < 1 ? 1 : v > 20 ? 20 : v;
        }

        return new HiddenAttributes
        {
            LearningRate = Attr(),
            Creative     = Attr(),
            WorkEthic    = Attr(),
            Adaptability = Attr(),
            Ambition     = Attr()
        };
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

                var hiredIds = _employeeSystem.BulkHireForCompany(comp.Id.ToCompanyId(), comp.Archetype, employeeCount, compRng, _gameState.currentTick);
                for (int i = 0; i < hiredIds.Count; i++)
                    comp.EmployeeIds.Add(hiredIds[i]);

                // Step 6: Create one team per product (active + in-dev), distribute employees
                int productCount = (comp.ActiveProductIds != null ? comp.ActiveProductIds.Count : 0)
                                 + (comp.InDevelopmentProductIds != null ? comp.InDevelopmentProductIds.Count : 0);
                int teamCount = productCount > 0 ? productCount : 1;
                CompanyId companyId = comp.Id.ToCompanyId();
                var teamIds = new System.Collections.Generic.List<TeamId>(teamCount);
                for (int t = 0; t < teamCount; t++) {
                    TeamId teamId = _teamSystem.CreateTeam(TeamType.Programming, _gameState.currentTick, companyId);
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
        
        // Day-boundary processing: daily recurring costs -> morale -> bankruptcy check
        _financeSystem.ProcessDaily(_gameState.currentTick);
        
        // Morale processing
        IRng moraleRng = _moraleRng;
        _moraleSystem.ProcessDailyMorale(day, moraleRng);
        
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
    
    private void OnEmployeeFired(EmployeeId employeeId)
    {
        var employee = _employeeSystem.GetEmployee(employeeId);
        if (employee != null && employee.ownerCompanyId == CompanyId.Player)
        {
            _financeSystem.RemoveRecurringCost($"salary-{employeeId.Value}");
        }

        _teamSystem.RemoveEmployeeFromTeam(employeeId);

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
            Skills = candidate.Skills,
            HRSkill = candidate.HRSkill,
            Salary = candidate.Salary,
            Role = candidate.Role,
            PotentialAbility = candidate.PotentialAbility,
            Mode = HiringMode.HR
        });
        _logger.Log($"[HRSystem] Accepted HR candidate {candidate.Name} — queued HireEmployeeCommand");
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

    private void OnInterviewFirstReportReady(int candidateId)
    {
        _interruptInterviewComplete = true;
        StopAdvance();
        _logger.Log($"[GameController] Interview first report ready for candidate {candidateId} — simulation paused");
    }

    private void OnInterviewFinalReportReady(int candidateId)
    {
        _interruptInterviewComplete = true;
        StopAdvance();

        // Inbox notification with candidate name
        string candidateName = "A candidate";
        var candidates = _gameState.employeeState.availableCandidates;
        int count = candidates.Count;
        for (int i = 0; i < count; i++)
        {
            if (candidates[i].CandidateId == candidateId)
            {
                candidateName = candidates[i].Name;
                break;
            }
        }
        _eventBus.Raise(new InterviewFinalReportEvent(_gameState.currentTick, candidateId, candidateName));

        _logger.Log($"[GameController] Interview final report ready for {candidateName} (id:{candidateId}) — simulation paused");
    }

    private void OnNegotiationAccepted(int candidateId, int agreedSalary)
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
                    Skills = candidate.Skills,
                    HRSkill = candidate.HRSkill,
                    Salary = agreedSalary,
                    Role = candidate.Role,
                    PotentialAbility = candidate.PotentialAbility
                };
                QueueCommand(hireCmd);
                break;
            }
        }
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
        }

        if (_negotiationSystem != null)
        {
            if (_recruitmentReputationSystem != null)
            {
                _negotiationSystem.OnOfferRejected -= _recruitmentReputationSystem.OnOfferRejectedHandler;
            }
            _negotiationSystem.OnOfferAccepted -= OnNegotiationAccepted;
        }

        if (_interviewSystem != null)
        {
            _interviewSystem.OnInterviewFirstReportReady -= OnInterviewFirstReportReady;
            _interviewSystem.OnInterviewFinalReportReady -= OnInterviewFinalReportReady;
        }
        
        if (_teamSystem != null)
        {
            _teamSystem.OnTeamCreated -= OnTeamCreated;
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
