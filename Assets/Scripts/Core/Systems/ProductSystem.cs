// ProductSystem Version: Clean v1
using System;
using System.Collections.Generic;

public class ProductSystem : ISystem
{
    private const float WorkRatePerSkillPoint = 0.016f;
    private const float BugWorkMultiplier = 50f;
    private const int ProgressEventThrottleTicks = 480;
    public const int SaleEventCooldownTicks = 4800 * 30 * 3;
    public const int SaleEventDurationTicks = 4800 * 7;
    public const int SaleDiscountPercent = 50;
    public const float SaleUserSpikeMult = 3.0f;

    // ── Technical Debt constants ───────────────────────────────────────────────
    private const float DebtAccumulationRatePerTick = 0.001f;
    private const float LegacyDebtPerFeature = 30f;

    // ── Successor Migration constants ──────────────────────────────────────────
    private const float SuccessorMigrationBaseRate = 0.20f;       // default 20% of predecessor users
    private const float SuccessorMigrationGenerationSkipPenalty = 0.75f;
    private const int SuccessorMigrationDurationTicks = TimeState.TicksPerDay * 180;  // 6 months

    // ── Pivot constants ───────────────────────────────────────────────────────
    private const float DropPivotFeePercent = 0.20f;
    private const float AddPivotFeePercent = 0.30f;
    private const float SwapPivotFeePercent = 0.25f;
    private const float DropTimeRecoveryPercent = 0.65f;
    private const float AddTimeOverheadPercent = 0.25f;
    private const float FeatureRelevanceWeight = 0.3f;

    public event Action<ProductId> OnProductCreated;
    public event Action<ProductId, TeamId, ProductTeamRole> OnTeamAssignedToProduct;
    public event Action<ProductId, TeamId> OnTeamUnassignedFromProduct;
    public event Action<ProductId, ProductPhaseType> OnPhaseUnlocked;
    public event Action<ProductId, ProductPhaseType, float> OnPhaseCompleted;
    public event Action<ProductId, ProductPhaseType, int> OnPhaseIterationStarted;
    public event Action<ProductId, ProductPhaseType, float> OnPhaseIterationCompleted;
    public event Action<ProductId> OnProductProgressUpdated;
    public event Action<ProductId, float> OnProductShipped;
    public event Action<ProductId> OnProductAbandoned;
    public event Action<List<EmployeeId>> OnSkillsAwarded;
    public event Action<ProductId, PivotAction, string, string> OnFeaturePivoted;

    // Post-ship economy events
    public event Action<ProductId, int> OnProductLaunched;        // productId, launchRevenue
    public event Action<ProductId> OnProductDead;
    public event Action<ProductId, ProductLifecycleStage, ProductLifecycleStage> OnLifecycleChanged;
    public event Action<ProductId> OnProductSaleStarted;
    public event Action<ProductId> OnProductSaleEnded;

    // Identity events
    public event Action<ProductId, ProductIdentitySnapshot, ProductIdentitySnapshot> OnProductIdentityChanged;

    // Marketing / Hype events
    public event Action<ProductId> OnMarketingStarted;
    public event Action<ProductId> OnMarketingStopped;
    public event Action<ProductId, float, int> OnHypeChanged;          // productId, newHype, totalSpend
    public event Action<ProductId, float, int> OnHypeUnderdelivered;   // productId, qualityGap, extraRepLoss

    // Post-launch marketing events
    public event Action<ProductId, int> OnAdRunStarted;    // productId, spendAmount
    public event Action<ProductId> OnAdRunEnded;
    public event Action<ProductId> OnUpdateAnnounced;

    // Random hype events
    public event Action<ProductId, HypeEventType, bool> OnHypeEvent;  // productId, eventType, wasMitigated

    // Crisis and sell events
    public event Action<ProductId, CrisisEventType> OnProductCrisis;
    public event Action<ProductId, CompetitorId, long> OnProductSold;

    // Release date events
    public event Action<ProductId, int> OnReleaseDateAnnounced;  // productId, targetTick
    public event Action<ReleaseDateChangedEvent> OnReleaseDateChanged;
    public event Action<ProductId, string, int, int> OnShipWarning;  // productId, productName, incompletePhasesCount, daysRemaining

    private enum PendingEventType : byte
    {
        ProductCreated,
        TeamAssigned,
        TeamUnassigned,
        PhaseUnlocked,
        PhaseCompleted,
        PhaseIterationStarted,
        PhaseIterationCompleted,
        ProductProgressUpdated,
        ProductShipped,
        ProductAbandoned,
        SkillsAwarded,
        FeaturePivoted,
        ProductLaunched,
        ProductDead,
        LifecycleChanged,
        ProductSaleStarted,
        ProductSaleEnded,
        MarketingStarted,
        MarketingStopped,
        HypeChanged,
        HypeUnderdelivered,
        AdRunStarted,
        AdRunEnded,
        UpdateAnnounced,
        HypeEvent,
        ProductCrisis,
        ProductSold,
        LogUpdate,
        LogUpdateEnd,
        LogRemoveFromMarket,
        ReleaseDateAnnounced,
        ReleaseDateChanged,
        ShipWarning,
    }

    private struct PendingIdentityChange
    {
        public ProductId ProductId;
        public ProductIdentitySnapshot Previous;
        public ProductIdentitySnapshot Current;
    }

    private struct PendingEvent
    {
        public PendingEventType Type;
        public ProductId ProductId;
        public TeamId TeamId;
        public ProductTeamRole Role;
        public ProductPhaseType PhaseType;
        public float FloatA;
        public float FloatB;
        public int IntA;
        public int IntB;
        public ProductLifecycleStage StageA;
        public ProductLifecycleStage StageB;
        public PivotAction PivotAction;
        public string StringA;
        public string StringB;
        public HypeEventType HypeEventType;
        public bool BoolA;
        public CrisisEventType CrisisType;
        public CompetitorId CompetitorId;
        public long LongA;
        public List<EmployeeId> EmployeeIds;
        public ProductUpdateType UpdateType;
    }

    private ProductState _state;
    private CompetitorState _competitorState;
    private IRng _rng;
    private ILogger _logger;
    private FinanceSystem _financeSystem;
    private TeamSystem _teamSystem;
    private EmployeeSystem _employeeSystem;
    private ReputationSystem _reputationSystem;
    private ReviewSystem _reviewSystem;
    private InboxSystem _inboxSystem;
    private RoleProfileTable _roleProfileTable;
    private AbilitySystem _abilitySystem;
    private ContractState _contractState;
    private TimeSystem _timeSystem;
    private MarketSystem _marketSystem;
    private MoraleSystem _moraleSystem;
    private FatigueSystem _fatigueSystem;
    private TeamChemistrySystem _chemistrySystem;
    private PlatformSystem _platformSystem;
    private GenerationSystem _generationSystem;
    private HardwareGenerationConfig[] _hardwareGenerationConfigs;
    private CrossProductGateConfig _crossProductGateConfig;
    private int _currentTick;
    private Dictionary<string, ProductTemplateDefinition> _templateLookup;
    private List<PendingEvent> _pendingEvents;
    private List<PendingIdentityChange> _pendingIdentityChanges;
    private TuningConfig _tuning;
    private readonly HashSet<ProductId> _shipWarningSent = new HashSet<ProductId>();

    // Pre-allocated scratch lists — no per-tick allocations
    private readonly List<int> _phaseUnlockQueue;
    private readonly List<ProductPhaseType> _completedPhaseTypes;
    private readonly List<ProductId> _productIds;
    private readonly List<ProductId> _shippedProductIds;
    private readonly HashSet<TeamId> _uniqueTeamIds;

    public ProductSystem(ProductState state, IRng rng, ILogger logger)
    {
        _state = state;
        _rng = rng;
        _logger = logger ?? new NullLogger();
        _templateLookup = new Dictionary<string, ProductTemplateDefinition>();
        _pendingEvents = new List<PendingEvent>(32);
        _pendingIdentityChanges = new List<PendingIdentityChange>(4);
        _phaseUnlockQueue = new List<int>(8);
        _completedPhaseTypes = new List<ProductPhaseType>(8);
        _productIds = new List<ProductId>(16);
        _shippedProductIds = new List<ProductId>(16);
        _uniqueTeamIds = new HashSet<TeamId>();
    }

    public void SetFinanceSystem(FinanceSystem financeSystem)
    {
        _financeSystem = financeSystem;
    }

    public void SetCompetitorState(CompetitorState competitorState)
    {
        _competitorState = competitorState;
    }

    public void SetTuningConfig(TuningConfig tuning)
    {
        _tuning = tuning;
    }

    public void SetTeamSystem(TeamSystem teamSystem)
    {
        if (_teamSystem != null)
            _teamSystem.OnTeamDeleted -= OnTeamDeleted;
        _teamSystem = teamSystem;
        if (_teamSystem != null)
            _teamSystem.OnTeamDeleted += OnTeamDeleted;
    }

    public void SetEmployeeSystem(EmployeeSystem employeeSystem)
    {
        _employeeSystem = employeeSystem;
    }

    public void SetMoraleSystem(MoraleSystem moraleSystem)
    {
        _moraleSystem = moraleSystem;
    }

    public void SetFatigueSystem(FatigueSystem fatigueSystem)
    {
        _fatigueSystem = fatigueSystem;
    }

    public void SetChemistrySystem(TeamChemistrySystem chemistrySystem)
    {
        _chemistrySystem = chemistrySystem;
    }

    public void SetReputationSystem(ReputationSystem reputationSystem)
    {
        _reputationSystem = reputationSystem;
    }

    public void SetReviewSystem(ReviewSystem reviewSystem) {
        _reviewSystem = reviewSystem;
    }

    public void SetInboxSystem(InboxSystem inboxSystem)
    {
        _inboxSystem = inboxSystem;
    }

    public void SetSkillGrowthDependencies(RoleProfileTable roleProfileTable, AbilitySystem abilitySystem)
    {
        _roleProfileTable = roleProfileTable;
        _abilitySystem = abilitySystem;
    }

    public void SetContractState(ContractState contractState)
    {
        _contractState = contractState;
    }

    public void SetTimeSystem(TimeSystem timeSystem)
    {
        if (_timeSystem != null)
        {
            _timeSystem.OnDayChanged -= OnDayChanged;
            _timeSystem.OnMonthChanged -= OnMonthChanged;
        }
        _timeSystem = timeSystem;
        if (_timeSystem != null)
        {
            _timeSystem.OnDayChanged += OnDayChanged;
            _timeSystem.OnMonthChanged += OnMonthChanged;
        }
    }

    public void SetMarketSystem(MarketSystem marketSystem)
    {
        _marketSystem = marketSystem;
    }

    public void SetPlatformSystem(PlatformSystem platformSystem) {
        _platformSystem = platformSystem;
    }

    public void SetGenerationSystem(GenerationSystem generationSystem) {
        _generationSystem = generationSystem;
    }

    public void SetHardwareGenerationConfigs(HardwareGenerationConfig[] configs) {
        _hardwareGenerationConfigs = configs;
    }

    public void SetCrossProductGateConfig(CrossProductGateConfig config) {
        _crossProductGateConfig = config;
    }

    private float GetNicheDevTimeMultiplier(ProductNiche niche)
    {
        return _marketSystem?.GetNicheDevTimeMultiplier(niche) ?? 1f;
    }

    public ProductPhaseRuntime[] BuildPhasesForTemplate(string templateId, string[] selectedFeatureIds, float nicheDevTimeMult = 1f)
    {
        if (templateId == null || !_templateLookup.TryGetValue(templateId, out var template)) return new ProductPhaseRuntime[0];
        return BuildPhaseRuntimes(template, selectedFeatureIds, _tuning?.ProductBaseWorkMultiplier ?? 100f, nicheDevTimeMult, template.difficultyTier);
    }

    public void RegisterTemplates(ProductTemplateDefinition[] templates)
    {
        if (templates == null) return;
        _templateLookup.Clear();
        for (int i = 0; i < templates.Length; i++)
        {
            var t = templates[i];
            if (t != null && !string.IsNullOrEmpty(t.templateId))
                _templateLookup[t.templateId] = t;
        }
    }

    public bool IsTeamAssignedToProduct(TeamId teamId)
    {
        return _state.teamToProduct.ContainsKey(teamId);
    }

    public ProductId? GetProductForTeam(TeamId teamId)
    {
        if (_state.teamToProduct.TryGetValue(teamId, out var productId))
            return productId;
        return null;
    }

    public void PreTick(int tick) { }

    // ─── Day / Month Boundary Handlers ────────────────────────────────────────

    private void OnDayChanged(int day)
    {
        // Process products in PreLaunch stage — they shipped the previous day
        _shippedProductIds.Clear();
        foreach (var key in _state.shippedProducts.Keys)
            _shippedProductIds.Add(key);

        int count = _shippedProductIds.Count;
        for (int i = 0; i < count; i++)
        {
            if (!_state.shippedProducts.TryGetValue(_shippedProductIds[i], out var product)) continue;
            if (product.LifecycleStage == ProductLifecycleStage.PreLaunch)
                ProcessLaunch(product, _currentTick);
        }

        // Process passive hype building for marketing-active products
        _productIds.Clear();
        foreach (var key in _state.developmentProducts.Keys)
            _productIds.Add(key);

        int devCount = _productIds.Count;
        for (int i = 0; i < devCount; i++)
        {
            if (!_state.developmentProducts.TryGetValue(_productIds[i], out var devProduct)) continue;
            if (!devProduct.IsMarketingActive) continue;
            if (!HasValidMarketingTeam(devProduct))
            {
                devProduct.IsMarketingActive = false;
                continue;
            }
            ProcessDailyHype(devProduct);
        }

        // Process post-launch hype for shipped products with active marketing
        _shippedProductIds.Clear();
        foreach (var key in _state.shippedProducts.Keys)
            _shippedProductIds.Add(key);

        int shippedHypeCount = _shippedProductIds.Count;
        for (int i = 0; i < shippedHypeCount; i++)
        {
            if (!_state.shippedProducts.TryGetValue(_shippedProductIds[i], out var shipped)) continue;
            if (!shipped.IsMarketingActive) continue;
            if (!HasValidMarketingTeam(shipped))
            {
                shipped.IsMarketingActive = false;
                continue;
            }
            ProcessDailyHype(shipped);
        }

        // Decay hype for shipped products without active marketing
        float postLaunchDecay = _tuning?.PostLaunchHypeDecayPerDay ?? 0.5f;
        for (int i = 0; i < shippedHypeCount; i++)
        {
            if (!_state.shippedProducts.TryGetValue(_shippedProductIds[i], out var shipped)) continue;
            if (shipped.IsMarketingActive) continue;
            if (shipped.IsCompetitorProduct) continue;
            if (shipped.HypeScore <= 0f) continue;
            shipped.HypeScore = Math.Max(0f, shipped.HypeScore - postLaunchDecay);
        }

        // Process competitor daily hype for shipped products with marketing budget
        for (int i = 0; i < shippedHypeCount; i++)
        {
            if (!_state.shippedProducts.TryGetValue(_shippedProductIds[i], out var shipped)) continue;
            if (!shipped.IsCompetitorProduct) continue;
            if (shipped.MarketingBudgetMonthly <= 0) continue;
            ProcessCompetitorDailyHype(shipped);
        }

        // Decay hype for competitor shipped products without marketing budget
        for (int i = 0; i < shippedHypeCount; i++)
        {
            if (!_state.shippedProducts.TryGetValue(_shippedProductIds[i], out var shipped)) continue;
            if (!shipped.IsCompetitorProduct) continue;
            if (shipped.MarketingBudgetMonthly > 0) continue;
            if (shipped.HypeScore <= 0f) continue;
            shipped.HypeScore = Math.Max(0f, shipped.HypeScore - postLaunchDecay);
        }

        // Process ad daily popularity boost for shipped products
        _shippedProductIds.Clear();
        foreach (var key in _state.shippedProducts.Keys)
            _shippedProductIds.Add(key);

        int shippedDayCount = _shippedProductIds.Count;
        for (int i = 0; i < shippedDayCount; i++)
        {
            if (!_state.shippedProducts.TryGetValue(_shippedProductIds[i], out var shipped)) continue;
            if (shipped.IsRunningAds)
            {
                float adSkillMult = GetMarketingSkillMult(shipped);
                float adPopGain = (_tuning?.AdPopularityGainPerDay ?? 1.5f) * adSkillMult;
                shipped.PopularityScore = Math.Min(100f, shipped.PopularityScore + adPopGain);
            }
        }

        // Process update hype daily for shipped products with announced updates
        for (int i = 0; i < shippedDayCount; i++)
        {
            if (!_state.shippedProducts.TryGetValue(_shippedProductIds[i], out var shipped)) continue;
            if (!shipped.HasAnnouncedUpdate) continue;

            if (shipped.CurrentUpdate != null && shipped.CurrentUpdate.isUpdating && HasValidMarketingTeam(shipped))
            {
                float skillMult = GetMarketingSkillMult(shipped);
                float gain = (_tuning?.UpdateHypePassiveGainPerDay ?? 0.5f) * skillMult;
                shipped.UpdateHype = Math.Min(100f, shipped.UpdateHype + gain);
            }

            int daysSinceAnnounce = (_currentTick - shipped.UpdateAnnounceTick) / TimeState.TicksPerDay;
            int graceDays = _tuning?.UpdateHypeDecayGraceDays ?? 60;
            if (daysSinceAnnounce > graceDays)
            {
                shipped.UpdateHype = Math.Max(0f, shipped.UpdateHype - 0.5f);
                if (shipped.UpdateHype <= 0f && shipped.HasAnnouncedUpdate)
                {
                    float penalty = _tuning?.UpdateBrokenPromiseSentimentPenalty ?? 2f;
                    _reputationSystem?.AdjustSentimentDelta(-penalty);
                    shipped.HasAnnouncedUpdate = false;
                }
            }
        }

        // Auto-ship at deadline and apply delay hype decay
        for (int i = 0; i < devCount; i++)
        {
            if (!_state.developmentProducts.TryGetValue(_productIds[i], out var devProduct)) continue;
            if (!devProduct.HasAnnouncedReleaseDate) continue;

            if (_currentTick >= devProduct.TargetReleaseTick)
            {
                _shipWarningSent.Remove(devProduct.Id);
                AutoShipProduct(devProduct);
                continue;
            }

            int ticksRemaining = devProduct.TargetReleaseTick - _currentTick;
            int daysRemaining = ticksRemaining / TimeState.TicksPerDay;

            bool shouldWarn = (daysRemaining == 14 || daysRemaining == 7) && !_shipWarningSent.Contains(devProduct.Id);
            if (shouldWarn)
            {
                int incompleteCount = 0;
                int phaseCount = devProduct.Phases != null ? devProduct.Phases.Length : 0;
                for (int p = 0; p < phaseCount; p++)
                {
                    var phase = devProduct.Phases[p];
                    if (!phase.isComplete && phase.totalWorkRequired > 0f)
                        incompleteCount++;
                }
                if (incompleteCount > 0)
                {
                    _shipWarningSent.Add(devProduct.Id);
                    _pendingEvents.Add(new PendingEvent
                    {
                        Type = PendingEventType.ShipWarning,
                        ProductId = devProduct.Id,
                        StringA = devProduct.ProductName,
                        IntA = incompleteCount,
                        IntB = daysRemaining
                    });
                }
            }

            if (_currentTick > devProduct.OriginalReleaseTick)
            {
                float dailyDecay = (_tuning?.DelayHypeDecayPerDay ?? 0.1f) * (devProduct.HypeScore / 100f);
                devProduct.HypeScore = Math.Max(0f, devProduct.HypeScore - dailyDecay);
            }
        }

        // Roll for random hype events — development products
        for (int i = 0; i < devCount; i++)
        {
            if (!_state.developmentProducts.TryGetValue(_productIds[i], out var devProduct)) continue;
            ProcessHypeEventRoll(devProduct, true);
        }

        // Roll for random hype events — shipped products
        _shippedProductIds.Clear();
        foreach (var key in _state.shippedProducts.Keys)
            _shippedProductIds.Add(key);

        int eventShippedCount = _shippedProductIds.Count;
        for (int i = 0; i < eventShippedCount; i++)
        {
            if (!_state.shippedProducts.TryGetValue(_shippedProductIds[i], out var shipped)) continue;
            if (!shipped.IsOnMarket) continue;
            ProcessHypeEventRoll(shipped, false);
        }

        // Daily revenue processing for shipped products
        _shippedProductIds.Clear();
        foreach (var key in _state.shippedProducts.Keys)
            _shippedProductIds.Add(key);

        int revenueCount = _shippedProductIds.Count;
        for (int i = 0; i < revenueCount; i++)
        {
            if (!_state.shippedProducts.TryGetValue(_shippedProductIds[i], out var product)) continue;
            if (product.IsCompetitorProduct) continue;
            if (product.IsOnMarket)
                ProcessDailyRevenue(product, _currentTick);
        }
    }

    private void OnMonthChanged(int month)
    {
        _reputationSystem?.DriftSentiment();

        _shippedProductIds.Clear();
        foreach (var key in _state.shippedProducts.Keys)
            _shippedProductIds.Add(key);

        int count = _shippedProductIds.Count;

        // Snapshot accumulated monthly revenue at month boundary
        for (int i = 0; i < count; i++)
        {
            if (!_state.shippedProducts.TryGetValue(_shippedProductIds[i], out var product)) continue;
            if (!product.IsOnMarket) continue;

            int oldPreviousMonthActiveUsers = product.PreviousMonthActiveUsers;

            // Snapshot for ALL products (player + competitor) — used by trend display
            product.PreviousMonthActiveUsers = product.ActiveUserCount;
            product.PreviousMonthlyRevenue = product.MonthlyRevenue;

            // Monthly snapshot: users and trend (all products)
            product.SnapshotMonthlyUsers = product.ActiveUserCount;
            if (oldPreviousMonthActiveUsers <= 0)
                product.SnapshotMonthlyTrend = product.ActiveUserCount > 0 ? "New" : "--";
            else {
                float trendDelta = (float)(product.ActiveUserCount - oldPreviousMonthActiveUsers) / oldPreviousMonthActiveUsers;
                if (trendDelta > 0.05f) product.SnapshotMonthlyTrend = "Growth";
                else if (trendDelta < -0.05f) product.SnapshotMonthlyTrend = "Decline";
                else product.SnapshotMonthlyTrend = "Stable";
            }
            product.HasCompletedFirstMonth = true;

            if (product.IsCompetitorProduct) {
                product.SnapshotMonthlyRevenue = (long)product.MonthlyRevenue;

                if (product.IsSubscriptionBased) {
                    product.SnapshotMonthlySales = product.TotalSubscribers;
                } else {
                    float unitPrice = GetCompetitorUnitPrice(product);
                    if (unitPrice > 0f && product.MonthlyRevenue > 0) {
                        int derivedSales = (int)(product.MonthlyRevenue / unitPrice);
                        product.SnapshotMonthlySales = derivedSales;
                        product.TotalUnitsSold += derivedSales;
                    } else {
                        int userBasedFallback = Math.Max(1, (int)Math.Round(product.ActiveUserCount * 0.15f));
                        product.SnapshotMonthlySales = userBasedFallback;
                        product.TotalUnitsSold += userBasedFallback;
                    }
                }
                product.PreviousMonthUnitsSold = product.TotalUnitsSold;

                if (product.SnapshotMonthlySales > product.PeakMonthlySales)
                    product.PeakMonthlySales = product.SnapshotMonthlySales;

                continue;
            }

            // Player-only: finalize accumulated revenue
            product.MonthlyRevenue = product.AccumulatedMonthlyRevenue;
            product.AccumulatedMonthlyRevenue = 0;

            // Player snapshot: revenue
            product.SnapshotMonthlyRevenue = (long)product.MonthlyRevenue;

            // Player snapshot: sales
            if (product.IsSubscriptionBased) {
                product.SnapshotMonthlySales = product.TotalSubscribers;
            } else {
                product.SnapshotMonthlySales = product.TotalUnitsSold - product.PreviousMonthUnitsSold;
                product.PreviousMonthUnitsSold = product.TotalUnitsSold;
            }
        }

        // Derive DailyRevenue for competitor products from MonthlyRevenue set by MarketSystem
        for (int i = 0; i < count; i++)
        {
            if (!_state.shippedProducts.TryGetValue(_shippedProductIds[i], out var product)) continue;
            if (!product.IsOnMarket) continue;
            if (!product.IsCompetitorProduct) continue;
            product.DailyRevenue = product.MonthlyRevenue / 30;
        }

        // Award monthly marketing XP for active post-launch marketing actions
        for (int i = 0; i < count; i++)
        {
            if (!_state.shippedProducts.TryGetValue(_shippedProductIds[i], out var product)) continue;
            if (!HasValidMarketingTeam(product)) continue;
            if (!product.IsRunningAds && !product.HasAnnouncedUpdate) continue;
            if (!product.TeamAssignments.TryGetValue(ProductTeamRole.Marketing, out var mktTeamId)) continue;
            AwardMarketingXPForProduct(product.Id, mktTeamId, _tuning?.MarketingXPPerMonth ?? 0.3f);
        }

        if (_reputationSystem != null && _tuning != null) {
            bool hasActiveProduct = false;
            for (int i = 0; i < count; i++) {
                if (!_state.shippedProducts.TryGetValue(_shippedProductIds[i], out var p)) continue;
                if (p.LifecycleStage == ProductLifecycleStage.Launch ||
                    p.LifecycleStage == ProductLifecycleStage.Growth) {
                    hasActiveProduct = true;
                    break;
                }
            }
            if (!hasActiveProduct)
                _reputationSystem.DecayIdleFans(_tuning.FanIdleDecayRate);
        }

        AccumulateDevSalaryCosts();
        ProcessReputationDecay();
        ProcessMonthlyMaintenance();
        ProcessLicensingRevenue();
    }

    private void ProcessLicensingRevenue()
    {
        if (_financeSystem == null) return;

        _shippedProductIds.Clear();
        foreach (var key in _state.shippedProducts.Keys)
            _shippedProductIds.Add(key);

        int toolCount = _shippedProductIds.Count;
        for (int i = 0; i < toolCount; i++)
        {
            if (!_state.shippedProducts.TryGetValue(_shippedProductIds[i], out var tool)) continue;
            if (!tool.Category.IsTool() && !tool.Category.IsPlatform()) continue;
            if (tool.DistributionModel != ToolDistributionModel.Licensed) continue;
            if (tool.PlayerLicensingRate <= 0f) continue;

            long totalRoyalty = 0L;
            int licenseeCount = 0;

            var allShipped = _state.shippedProducts;
            foreach (var kvp in allShipped)
            {
                var licensee = kvp.Value;
                if (!licensee.IsOnMarket) continue;
                if (!licensee.IsCompetitorProduct) continue;

                bool usesTool = false;
                if (licensee.RequiredToolIds != null) {
                    for (int t = 0; t < licensee.RequiredToolIds.Length; t++) {
                        if (licensee.RequiredToolIds[t] == tool.Id) { usesTool = true; break; }
                    }
                }
                if (!usesTool && licensee.TargetPlatformIds != null) {
                    for (int t = 0; t < licensee.TargetPlatformIds.Length; t++) {
                        if (licensee.TargetPlatformIds[t] == tool.Id) { usesTool = true; break; }
                    }
                }
                if (!usesTool) continue;

                long royalty = (long)(licensee.MonthlyRevenue * tool.PlayerLicensingRate);
                if (royalty <= 0L) continue;
                totalRoyalty += royalty;
                licenseeCount++;
            }

            if (totalRoyalty > 0L)
            {
                tool.ActiveLicenseeCount = licenseeCount;
                tool.TotalLicensingRevenue += totalRoyalty;
                tool.TotalLifetimeRevenue += totalRoyalty;
                tool.MonthlyRevenue += (int)Math.Min(totalRoyalty, int.MaxValue);
                if (!tool.IsCompetitorProduct)
                    _financeSystem.AddMoney((int)Math.Min(totalRoyalty, int.MaxValue));
            }
        }
    }

    private void ProcessMonthlyMaintenance()
    {
        _shippedProductIds.Clear();
        foreach (var key in _state.shippedProducts.Keys)
            _shippedProductIds.Add(key);

        int count = _shippedProductIds.Count;
        for (int i = 0; i < count; i++)
        {
            if (!_state.shippedProducts.TryGetValue(_shippedProductIds[i], out var product)) continue;
            if (!product.IsOnMarket) continue;

            ProcessShowdownChurnExpiry(product, _currentTick);
            ProcessCrisisEscalation(product, _currentTick);
            if (product.IsCompetitorProduct)
                ProcessCompetitorMaintenance(product);
            else
                ProcessMaintenanceBudgetDrain(product);
            ProcessMarketingBudgetDrain(product);
        }

        _productIds.Clear();
        foreach (var key in _state.developmentProducts.Keys)
            _productIds.Add(key);

        int devCount2 = _productIds.Count;
        for (int i = 0; i < devCount2; i++)
        {
            if (!_state.developmentProducts.TryGetValue(_productIds[i], out var devProduct)) continue;
            if (!devProduct.IsMarketingActive) continue;
            ProcessMarketingBudgetDrain(devProduct);
        }
    }

    private void ProcessMaintenanceBudgetDrain(Product product)
    {
        if (_financeSystem == null) return;

        if (!product.TeamAssignments.TryGetValue(ProductTeamRole.QA, out var qaTeamId))
        {
            product.IsMaintained = false;
            product.MaintenanceQuality = 0f;
            return;
        }

        if (product.MaintenanceBudgetMonthly <= 0)
        {
            product.IsMaintained = false;
            product.MaintenanceQuality = 0f;
            return;
        }

        if (_financeSystem.Money < (int)Math.Min(product.MaintenanceBudgetMonthly, int.MaxValue))
        {
            product.IsMaintained = false;
            product.MaintenanceQuality = 0f;
            return;
        }

        _financeSystem.RecordTransaction(-(int)Math.Min(product.MaintenanceBudgetMonthly, int.MaxValue), FinanceCategory.ProductMaintenance, _currentTick, product.Id.Value.ToString());

        product.IsMaintained = true;

        var qaTeam = _teamSystem?.GetTeam(qaTeamId);
        int optimalSize = ComputeMaintenanceOptimalTeamSize(product);

        if (qaTeam != null && qaTeam.members.Count > 0)
        {
            float coverageMod = TeamWorkEngine.ComputeCoverageQualityMod(qaTeam.members.Count, optimalSize);

            var qaResult = TeamWorkEngine.AggregateTeam(
                qaTeam.members,
                _employeeSystem,
                _fatigueSystem,
                SkillId.QaTesting,
                _roleProfileTable,
                _tuning?.TeamOverheadPerMember ?? 0.04f,
                optimalTeamSize: optimalSize);

            float baseWork = _tuning?.ProductBaseWorkMultiplier ?? 100f;
            ChemistryBand maintChemBand = _chemistrySystem != null
                ? _chemistrySystem.GetTeamChemistry(qaTeam.id).Band
                : ChemistryBand.Neutral;
            float maintConflictQuality = _chemistrySystem != null
                ? 1f + _chemistrySystem.GetTeamQualityPenalty(qaTeam.id)
                : 1f;
            product.MaintenanceQuality = TeamWorkEngine.ComputeQuality(
                qaResult.AvgQualitySkill,
                2f,
                5f,
                10f,
                coverageMod,
                qaResult.AvgMorale,
                TeamWorkEngine.GetChemistryQualityMod(maintChemBand),
                maintConflictQuality);
        }
        else
        {
            product.MaintenanceQuality = 0f;
        }
    }

    private void ProcessCompetitorMaintenance(Product product)
    {
        product.IsMaintained = product.MaintenanceBudgetMonthly > 0;
        if (!product.IsMaintained)
        {
            product.MaintenanceQuality = 0f;
            return;
        }

        float budgetRef = _tuning?.HypeBudgetReferenceCost ?? 5000f;
        float budgetMult = (float)(Math.Log10(product.MaintenanceBudgetMonthly + 1) / Math.Log10(budgetRef + 1));
        if (budgetMult < 0.1f) budgetMult = 0.1f;
        if (budgetMult > 2.0f) budgetMult = 2.0f;

        product.MaintenanceQuality = Math.Clamp(budgetMult * 50f, 10f, 80f);

        int optimalSize = ComputeMaintenanceOptimalTeamSize(product);
        int syntheticTeamSize = Math.Max(1, (int)(budgetMult * optimalSize));
        float coverageRatio = Math.Clamp((float)syntheticTeamSize / Math.Max(1, optimalSize), 0.2f, 1.5f);

        float qualityFactor = product.MaintenanceQuality / 100f;
        float bugFixRate = (_tuning?.MaintenanceBugFixBaseRate ?? 0.4f) * qualityFactor * coverageRatio;
        product.BugsRemaining = Math.Max(0f, product.BugsRemaining - bugFixRate);

        if (product.Features != null)
        {
            float debtReduction = qualityFactor * coverageRatio * DebtAccumulationRatePerTick * 0.5f;
            int featureCount = product.Features.Length;
            for (int i = 0; i < featureCount; i++)
            {
                var fs = product.Features[i];
                if (fs == null) continue;
                fs.TechnicalDebt = Math.Max(0f, fs.TechnicalDebt - debtReduction);
            }
        }
    }

    private void ProcessCompetitorDailyHype(Product product)
    {
        float skillMult = 1.0f;
        float tierMult = GetHypeTierMult();
        float diminishing = 1f / (1f + product.HypeScore * (_tuning?.HypeDiminishingFactor ?? 0.02f));
        float baseGain = _tuning?.HypePassiveGainPerDay ?? 0.8f;
        float budgetRef = _tuning?.HypeBudgetReferenceCost ?? 5000f;
        float budgetMult = (float)(Math.Log10(product.MarketingBudgetMonthly + 1) / Math.Log10(budgetRef + 1));
        if (budgetMult < 0.1f) budgetMult = 0.1f;
        if (budgetMult > 2.0f) budgetMult = 2.0f;
        float dailyGain = baseGain * skillMult * tierMult * diminishing * budgetMult;

        int daysSincePeak = (_currentTick - product.PeakHypeTick) / TimeState.TicksPerDay;
        int graceDays = _tuning?.HypeDecayGracePeriodDays ?? 30;
        float dailyDecay = 0f;
        if (daysSincePeak > graceDays && product.PeakHype > 0f)
        {
            int excessDays = daysSincePeak - graceDays;
            float rampDays = _tuning?.HypeDecayRampDays ?? 30f;
            float maxDecay = _tuning?.HypeDecayMaxPerDay ?? 1.5f;
            float baseDecay = _tuning?.HypeDecayPerDay ?? 0.3f;
            dailyDecay = baseDecay * (excessDays / rampDays);
            if (dailyDecay > maxDecay) dailyDecay = maxDecay;
        }

        float netChange = dailyGain - dailyDecay;
        product.HypeScore += netChange;
        if (product.HypeScore < 0f) product.HypeScore = 0f;
        if (product.HypeScore > 100f) product.HypeScore = 100f;

        if (product.HypeScore > product.PeakHype)
        {
            product.PeakHype = product.HypeScore;
            product.PeakHypeTick = _currentTick;
        }
    }

    private void ProcessMarketingBudgetDrain(Product product)
    {
        if (product.IsCompetitorProduct) return;
        if (_financeSystem == null) return;

        if (!product.TeamAssignments.TryGetValue(ProductTeamRole.Marketing, out var mktTeamId))
        {
            product.IsMarketingActive = false;
            return;
        }

        if (product.MarketingBudgetMonthly <= 0)
        {
            product.IsMarketingActive = false;
            return;
        }

        if (_financeSystem.Money < (int)Math.Min(product.MarketingBudgetMonthly, int.MaxValue))
        {
            product.IsMarketingActive = false;
            return;
        }

        _financeSystem.RecordTransaction(-(int)Math.Min(product.MarketingBudgetMonthly, int.MaxValue), FinanceCategory.ProductMarketing, _currentTick, product.Id.Value.ToString());

        product.IsMarketingActive = true;
    }

    private int ComputeMaintenanceOptimalTeamSize(Product product)
    {
        int featureCount = product.SelectedFeatureIds != null ? product.SelectedFeatureIds.Length : 1;
        int userBased = System.Math.Max(1, product.ActiveUserCount / 10000);
        int featureBased = System.Math.Max(1, featureCount / 3);
        return System.Math.Max(1, System.Math.Max(userBased, featureBased));
    }

    private int ComputeMarketingOptimalTeamSize(Product product)
    {
        return System.Math.Max(1, product.ActiveUserCount / 15000);
    }

    private void ProcessShowdownChurnExpiry(Product product, int tick)
    {
        if (product.ShowdownChurnMultiplier <= 1f) return;
        if (tick >= product.ShowdownChurnExpiryTick)
        {
            product.ShowdownChurnMultiplier = 1f;
            product.ShowdownChurnExpiryTick = 0;
        }
    }

    private void ProcessCrisisEscalation(Product product, int tick)
    {
        bool hasQATeam = product.TeamAssignments != null && product.TeamAssignments.ContainsKey(ProductTeamRole.QA);

        if (hasQATeam)
        {
            product.UnmaintainedMonths = 0;
            product.CrisisLevel = 0;
            return;
        }

        product.UnmaintainedMonths++;

        int threshold1 = _tuning?.CrisisThresholdMonths1 ?? 3;
        int threshold2 = _tuning?.CrisisThresholdMonths2 ?? 6;
        int threshold3 = _tuning?.CrisisThresholdMonths3 ?? 9;
        float baseChance = _tuning?.CrisisBaseChancePerMonth ?? 0.05f;
        float escalation = _tuning?.CrisisChanceEscalationPerMonth ?? 0.04f;

        // Identity-based crisis chance modifiers
        if (product.IdentityAtShip.IsValid)
        {
            if (product.IdentityAtShip.ProductionDiscipline >= 40)
                baseChance *= 0.85f;
            else if (product.IdentityAtShip.ProductionDiscipline <= -40)
                baseChance *= 1.20f;
        }

        int nextLevel = product.CrisisLevel + 1;
        if (nextLevel > 3) return;

        int threshold = nextLevel == 1 ? threshold1 : nextLevel == 2 ? threshold2 : threshold3;

        bool triggered;
        if (product.UnmaintainedMonths >= threshold)
        {
            triggered = true;
        }
        else
        {
            float chance = baseChance + product.UnmaintainedMonths * escalation;
            triggered = _rng.NextFloat01() < chance;
        }

        if (!triggered) return;

        product.CrisisLevel = nextLevel;
        product.LastCrisisTick = tick;

        if (nextLevel == 3)
        {
            bool isLastOfKind = product.Category.IsCriticalCategory()
                && _state.IsLastOnMarketInCategory(product.Id);

            if (!product.IsCompetitorProduct)
            {
                long catastrophicCost = (long)(product.MonthlyRevenue * 0.5f);
                if (_financeSystem != null && catastrophicCost > 0)
                    _financeSystem.TrySubtractMoney((int)catastrophicCost, out _);
            }

            if (isLastOfKind)
            {
                product.CrisisLevel = 2;
                float churnMult = _tuning?.CrisisModerateChurnMultiplier ?? 1.25f;
                product.ActiveUserCount = (int)(product.ActiveUserCount / (churnMult * 2f));
                _pendingEvents.Add(new PendingEvent { Type = PendingEventType.ProductCrisis, ProductId = product.Id, CrisisType = CrisisEventType.ModerateBreach });
                _logger.Log($"[ProductSystem] Crisis downgraded for last {product.Category} product {product.Id.Value} — kept on market.");
            }
            else
            {
                product.IsOnMarket = false;

                if (product.TeamAssignments != null) {
                    foreach (var kvp in product.TeamAssignments)
                        _state.teamToProduct.Remove(kvp.Value);
                    product.TeamAssignments.Clear();
                }

                _state.shippedProducts.Remove(product.Id);
                _state.archivedProducts[product.Id] = product;

                if (product.IsCompetitorProduct && _competitorState != null)
                {
                    CompetitorId compId = product.OwnerCompanyId.ToCompetitorId();
                    if (_competitorState.competitors.TryGetValue(compId, out var comp) && comp.ActiveProductIds != null)
                        comp.ActiveProductIds.Remove(product.Id);
                }

                _pendingEvents.Add(new PendingEvent { Type = PendingEventType.ProductCrisis, ProductId = product.Id, CrisisType = CrisisEventType.Catastrophic });
                _logger.Log($"[ProductSystem] Catastrophic crisis archived product {product.Id.Value}.");
            }
        }
        else if (nextLevel == 2)
        {
            if (!product.IsCompetitorProduct)
            {
                long moderateCost = (long)(product.MonthlyRevenue * 0.25f);
                if (_financeSystem != null && moderateCost > 0)
                    _financeSystem.TrySubtractMoney((int)moderateCost, out _);
            }
            float churnMult = _tuning?.CrisisModerateChurnMultiplier ?? 1.25f;
            product.ActiveUserCount = (int)(product.ActiveUserCount / churnMult);
            _pendingEvents.Add(new PendingEvent { Type = PendingEventType.ProductCrisis, ProductId = product.Id, CrisisType = CrisisEventType.ModerateBreach });
        }
        else
        {
            if (!product.IsCompetitorProduct)
            {
                long minorCost = (long)(product.MonthlyRevenue * 0.05f);
                if (_financeSystem != null && minorCost > 0)
                    _financeSystem.TrySubtractMoney((int)minorCost, out _);
            }
            float minorChurnMult = _tuning?.CrisisMinorChurnMultiplier ?? 1.10f;
            product.ActiveUserCount = (int)(product.ActiveUserCount / minorChurnMult);
            _pendingEvents.Add(new PendingEvent { Type = PendingEventType.ProductCrisis, ProductId = product.Id, CrisisType = CrisisEventType.MinorBug });
        }
    }

    public void ProcessProductSale(ProductId id, CompetitorId buyerId, long salePrice)
    {
        if (!_state.shippedProducts.TryGetValue(id, out var product)) return;

        product.OwnerCompanyId = buyerId.ToCompanyId();

        if (_financeSystem != null && salePrice > 0)
            _financeSystem.AddMoney((int)Math.Min(salePrice, int.MaxValue));

        product.SaleValue = salePrice;

        _pendingEvents.Add(new PendingEvent { Type = PendingEventType.ProductSold, ProductId = id, CompetitorId = buyerId, LongA = salePrice });
    }

    public long CalculateProductFairValue(ProductId id)
    {
        if (!_state.shippedProducts.TryGetValue(id, out var product)) return 0L;

        float monthlyRevenue = product.MonthlyRevenue;
        int activeUsers = product.ActiveUserCount;
        int remainingMonths = 24;

        switch (product.LifecycleStage)
        {
            case ProductLifecycleStage.Launch:  remainingMonths = 36; break;
            case ProductLifecycleStage.Growth:  remainingMonths = 30; break;
            case ProductLifecycleStage.Plateau: remainingMonths = 18; break;
            case ProductLifecycleStage.Decline: remainingMonths = 6;  break;
        }

        long revenueValue = (long)(monthlyRevenue * remainingMonths * 0.6f);
        long userValue = (long)(activeUsers * 2L);
        return revenueValue + userValue;
    }

    private void AccumulateDevSalaryCosts()
    {
        if (_teamSystem == null || _employeeSystem == null) return;

        _productIds.Clear();
        foreach (var key in _state.developmentProducts.Keys)
            _productIds.Add(key);

        int count = _productIds.Count;
        for (int i = 0; i < count; i++)
        {
            if (!_state.developmentProducts.TryGetValue(_productIds[i], out var product)) continue;
            if (product.TeamAssignments == null) continue;

            _uniqueTeamIds.Clear();
            foreach (var teamId in product.TeamAssignments.Values)
                _uniqueTeamIds.Add(teamId);

            long monthlySalaryCost = 0;
            foreach (var teamId in _uniqueTeamIds)
            {
                var team = _teamSystem.GetTeam(teamId);
                if (team == null) continue;
                int memberCount = team.members.Count;
                for (int m = 0; m < memberCount; m++)
                {
                    var employee = _employeeSystem.GetEmployee(team.members[m]);
                    if (employee == null) continue;
                    monthlySalaryCost += employee.salary;
                }
            }

            product.AccumulatedSalaryCost += monthlySalaryCost;
        }
    }

    private void ProcessLaunch(Product product, int tick)
    {
        if (product.IsCompetitorProduct) { ProcessCompetitorLaunch(product); return; }
        if (product.TemplateId == null || !_templateLookup.TryGetValue(product.TemplateId, out var template)) return;
        var config = template.economyConfig;
        if (config == null) return;

        // Pull retention and volatility from the niche config
        float nicheVolatility = 0.2f;
        int nicheRetentionMonths = 12;
        if (template.nicheConfigs != null)
        {
            for (int n = 0; n < template.nicheConfigs.Length; n++)
            {
                if (template.nicheConfigs[n].niche == product.Niche)
                {
                    nicheRetentionMonths = template.nicheConfigs[n].retentionMonths > 0
                        ? template.nicheConfigs[n].retentionMonths
                        : 12;
                    nicheVolatility = template.nicheConfigs[n].volatility;
                    break;
                }
            }
        }

        float effectiveQuality = _reputationSystem?.GetEffectiveQuality(product.OverallQuality) ?? product.OverallQuality;
        float qualityMult = effectiveQuality / 100f * 1.5f;
        float reputationMult = GetReputationLaunchMultiplier(template.category);
        float genreMult = 0.8f;
        float marketDemandMult = _marketSystem?.GetCombinedDemandMultiplier(product) ?? 1.0f;
        float elasticityMult = 1f;
        float fanLaunchMult = _reputationSystem?.GetFanLaunchMultiplier() ?? 1.0f;

        // Hype launch multiplier
        float hypeLaunchMult = 1f;
        float[] catSensitivity = _tuning?.HypeCategorySensitivity;
        float categorySens = 1f;
        if (catSensitivity != null && (int)template.category < catSensitivity.Length)
            categorySens = catSensitivity[(int)template.category];
        if (product.HypeAtShip <= 0f && product.HypeScore > 0f)
            product.HypeAtShip = product.HypeScore;
        float effectiveHype = product.HypeAtShip * categorySens;
        if (effectiveHype > 0f)
        {
            float maxBonus = _tuning?.HypeMaxBonus ?? 2f;
            hypeLaunchMult = 1f + (effectiveHype / 100f) * maxBonus;
        }

        int baseSales = ProductLaunchEngine.ComputeLaunchSales(
            effectiveQuality, config.launchSalesBase, reputationMult,
            genreMult, marketDemandMult, elasticityMult, fanLaunchMult, hypeLaunchMult);

        // Apply niche volatility variance to launch sales
        float volatilityVariance = 1f + (_rng.NextFloat01() * 2f - 1f) * nicheVolatility;
        baseSales = (int)(baseSales * Math.Max(0.1f, volatilityVariance));

        ReputationTier ownerTier = _reputationSystem?.CurrentTier ?? ReputationTier.Unknown;
        ProductLaunchEngine.RollBreakout(
            product.OverallQuality, product.PopularityScore, ownerTier,
            config.breakoutBaseChance, config.breakoutMinMultiplier, config.breakoutMaxMultiplier,
            baseSales, _rng,
            out int finalSales, out bool isBreakout, out float breakoutMult, out int breakoutDays);
        product.IsBreakout = isBreakout;
        product.BreakoutMultiplier = breakoutMult;
        product.BreakoutDaysRemaining = breakoutDays;
        product.BreakoutMonthsRemaining = breakoutDays / 30;

        product.PopularityScore = ProductLaunchEngine.ComputePopularityScore(product.OverallQuality);

        product.FanAppealBonus = ProductLaunchEngine.ComputeFanAppealBonus(
            _reputationSystem?.CompanyFans ?? 0,
            _reputationSystem?.FanSentiment ?? 50f,
            _tuning?.FanLaunchBonusDivisor ?? 50000f);

        if (product.ReviewResult == null && _reviewSystem != null && _templateLookup.TryGetValue(product.TemplateId, out var reviewTemplate)) {
            float savedQuality = product.OverallQuality;
            product.OverallQuality = ComputeWeightedQuality(product, reviewTemplate);
            product.ReviewResult = _reviewSystem.GenerateReviews(product, reviewTemplate, product.FeatureRelevanceAtShip,
                product.IdentityAtShip.IsValid ? (ProductIdentitySnapshot?)product.IdentityAtShip : null);
            product.PublicReceptionScore = product.ReviewResult.AggregateScore;
            product.OverallQuality = savedQuality;
        } else if (product.ReviewResult != null) {
            product.PublicReceptionScore = product.ReviewResult.AggregateScore;
        } else if (_reputationSystem != null) {
            product.PublicReceptionScore = _reputationSystem.GenerateReceptionScore(
                effectiveQuality, marketDemandMult, 0f, _rng);
        }

        // Use player pricing model override
        bool isSubscription = product.IsSubscriptionBased;
        float pricePerUnit = product.PriceOverride > 0f ? product.PriceOverride : config.pricePerUnit;
        float subPrice = product.PriceOverride > 0f ? product.PriceOverride : config.monthlySubscriptionPrice;

        int launchRevenue;
        if (isSubscription)
        {
            product.TotalSubscribers = finalSales;
            product.ActiveUserCount = finalSales;
            launchRevenue = (int)(finalSales * subPrice);
        }
        else
        {
            product.TailDecayFactor = 1.0f;
            product.PeakMonthlySales = finalSales;
            product.TotalUnitsSold = finalSales;
            product.ActiveUserCount = finalSales;
            launchRevenue = (int)(finalSales * pricePerUnit);
        }

        product.LaunchRevenue = launchRevenue;
        product.TotalLifetimeRevenue += launchRevenue;
        product.LifecycleStage = ProductLifecycleStage.Launch;
        product.IsOnMarket = true;
        product.TicksSinceShip = 0;

        // Apply identity-based launch modifiers (section 12.2)
        if (product.IdentityAtShip.IsValid && product.ReviewResult != null)
        {
            var snap = product.IdentityAtShip;
            float aggScore = product.ReviewResult.AggregateScore;
            float stab = product.ReviewResult.GetDimensionScore(ReviewDimension.Stability);
            float val  = product.ReviewResult.GetDimensionScore(ReviewDimension.Value);

            if (snap.PricePositioning >= 40)
            {
                if (aggScore >= 75f && val >= 50f)
                    finalSales = (int)(finalSales * 1.05f);
                else if (aggScore < 70f)
                    finalSales = (int)(finalSales * 0.95f);
            }

            if (snap.InnovationRisk >= 40)
            {
                if (aggScore >= 70f)
                    hypeLaunchMult += 0.05f;
                if (product.BugsRemaining > 10f)
                    finalSales = (int)(finalSales * 0.90f);
            }

            if (snap.AudienceBreadth >= 40 && product.ExpectedSelectedRatioAtShip >= 0.70f)
                finalSales = (int)(finalSales * 1.08f);

            if (snap.AudienceBreadth <= -40 && aggScore >= 75f)
                finalSales = (int)(finalSales * 1.08f);

            if (snap.FeatureScope <= -40 && stab >= 70f)
                product.TailDecayFactor *= 1.05f;

            if (isSubscription)
            {
                product.TotalSubscribers = finalSales;
                product.ActiveUserCount = finalSales;
                launchRevenue = (int)(finalSales * subPrice);
            }
            else
            {
                product.PeakMonthlySales = finalSales;
                product.TotalUnitsSold = finalSales;
                product.ActiveUserCount = finalSales;
                launchRevenue = (int)(finalSales * pricePerUnit);
            }
            product.LaunchRevenue = launchRevenue;
        }

        // Scale lifecycle growth stage threshold by retention months relative to default 12 months
        float retentionScale = nicheRetentionMonths / 12f;
        if (retentionScale > 0f && config.ticksToGrowthStage > 0)
        {
            product.TailDecayFactor = 1f;
        }

        if (_financeSystem != null && launchRevenue > 0)
            _financeSystem.AddMoney(launchRevenue);

        if (_reputationSystem != null && _tuning != null) {
            int launchRep = ProductLaunchEngine.ComputeLaunchReputation(
                effectiveQuality, launchRevenue, _tuning.LaunchReputationBase);
            if (launchRep > 0) {
                _reputationSystem.AddReputation(launchRep, "global");
                _reputationSystem.AddReputation(launchRep, template.category.ToString());
            }
        }

        if (product.HypeAtShip > 0f && _reputationSystem != null && _tuning != null) {
            float expectScale = _tuning.HypeExpectationScale;
            float hypeExpectation = effectiveHype * expectScale;
            float qualityGap = hypeExpectation - effectiveQuality;

            if (qualityGap > 0f) {
                int extraRepLoss = (int)(qualityGap * _tuning.HypeRepPenaltyPerPoint);
                if (extraRepLoss > 0) {
                    _reputationSystem.RemoveReputation(extraRepLoss, "global");
                    _reputationSystem.RemoveReputation(extraRepLoss, template.category.ToString());
                }
                float sentimentMult = 1f + qualityGap * _tuning.HypeSentimentPenaltyScale;
                float baseSentimentLoss = (40f - effectiveQuality);
                if (baseSentimentLoss > 0f) {
                    float extraSentimentLoss = baseSentimentLoss * 1.5f * (sentimentMult - 1f);
                    _reputationSystem.AdjustSentimentDelta(-extraSentimentLoss);
                }
                product.PublicReceptionScore = Math.Max(0f, product.PublicReceptionScore - qualityGap * 0.5f);
                _pendingEvents.Add(new PendingEvent { Type = PendingEventType.HypeUnderdelivered, ProductId = product.Id, FloatA = qualityGap, IntA = extraRepLoss });
            } else {
                float sentimentBonus = _tuning.HypeMeetsExpectationsBonus;
                _reputationSystem.AdjustSentimentDelta(sentimentBonus);
            }
        }

        _pendingEvents.Add(new PendingEvent { Type = PendingEventType.ProductLaunched, ProductId = product.Id, IntA = launchRevenue });
        _logger.Log($"[ProductSystem] Product '{product.ProductName}' launched — {finalSales} {(isSubscription ? "subscribers" : "units")}, ${launchRevenue} launch revenue{(product.IsBreakout ? " [BREAKOUT!]" : "")}");
    }

    private void ProcessCompetitorLaunch(Product product)
    {
        if (product.TemplateId == null || !_templateLookup.TryGetValue(product.TemplateId, out var template)) {
            product.LifecycleStage = ProductLifecycleStage.Launch;
            product.IsOnMarket = true;
            product.TicksSinceShip = 0;
            _pendingEvents.Add(new PendingEvent { Type = PendingEventType.ProductLaunched, ProductId = product.Id, IntA = 0 });
            return;
        }
        var config = template.economyConfig;
        float marketDemandMult = _marketSystem?.GetCombinedDemandMultiplier(product) ?? 1.0f;
        int launchSalesBase = config?.launchSalesBase ?? 5000;
        float pricePerUnit = config?.pricePerUnit ?? 20f;
        int baseSales = (int)(launchSalesBase * (product.OverallQuality / 100f) * marketDemandMult);
        if (baseSales < 100) baseSales = 100;
        int launchRevenue = (int)(baseSales * pricePerUnit);
        product.ActiveUserCount = baseSales;
        product.TotalUnitsSold = baseSales;
        product.PeakMonthlySales = baseSales;
        product.LaunchRevenue = launchRevenue;
        product.MonthlyRevenue = launchRevenue;
        product.AccumulatedMonthlyRevenue = launchRevenue;
        product.DailyRevenue = launchRevenue / 30;
        product.TotalLifetimeRevenue += launchRevenue;
        product.PopularityScore = ProductLaunchEngine.ComputePopularityScore(product.OverallQuality);
        product.FeatureRelevanceAtShip = ComputeFeatureRelevanceAtShip(product);
        if (_reviewSystem != null && _templateLookup.TryGetValue(product.TemplateId, out var reviewTemplate))
        {
            float savedQuality = product.OverallQuality;
            product.OverallQuality = ComputeWeightedQuality(product, reviewTemplate);

            if (!product.IdentityAtShip.IsValid)
            {
                product.IdentityAtShip = ProductIdentityHelper.ComputeAtShip(
                    product, reviewTemplate, _generationSystem, _platformSystem, _state, _tuning);
                product.CurrentIdentity = product.IdentityAtShip;
                product.ExpectedSelectedRatioAtShip = ComputeExpectedSelectedRatio(product, reviewTemplate);
            }

            product.ReviewResult = _reviewSystem.GenerateReviews(product, reviewTemplate, product.FeatureRelevanceAtShip,
                product.IdentityAtShip.IsValid ? (ProductIdentitySnapshot?)product.IdentityAtShip : null);
            product.PublicReceptionScore = product.ReviewResult.AggregateScore;
            product.OverallQuality = savedQuality;
        }
        product.LifecycleStage = ProductLifecycleStage.Launch;
        product.IsOnMarket = true;
        product.TicksSinceShip = 0;
        if (_marketSystem != null && product.OverallQuality >= 75f)
        {
            float uplift = (product.OverallQuality - 75f) / 25f * MarketSystem.MaxDemandUpliftPerRelease * 0.5f;
            _marketSystem.ApplyNicheUplift(product.Niche, uplift);
        }
        _pendingEvents.Add(new PendingEvent { Type = PendingEventType.ProductLaunched, ProductId = product.Id, IntA = launchRevenue });
        _logger.Log($"[ProductSystem] Competitor product '{product.ProductName}' launched — {baseSales} units, ${launchRevenue} revenue.");
    }

    private float GetReputationLaunchMultiplier(ProductCategory category)
    {
        if (_reputationSystem == null) return 1.0f;
        return _reputationSystem.GetCategoryLaunchMultiplier(category);
    }

    private void ProcessDailyRevenue(Product product, int tick)
    {
        if (string.IsNullOrEmpty(product.TemplateId)) return;
        if (!_templateLookup.TryGetValue(product.TemplateId, out var template)) return;
        var config = template.economyConfig;
        if (config == null) return;

        float priceMult = product.IsOnSale ? (1f - SaleDiscountPercent / 100f) : 1.0f;

        float rawRevenue;

        if (config.isSubscriptionBased)
        {
            // --- Subscription Path (daily) ---
            // MarketShareResolver has already set ActiveUserCount for this tick
            product.TotalSubscribers = product.ActiveUserCount;

            float subPrice = product.PriceOverride > 0f ? product.PriceOverride : config.monthlySubscriptionPrice;
            rawRevenue = product.TotalSubscribers * subPrice / 30f * priceMult;

            // Apply breakout multiplier to revenue
            if (product.IsBreakout && product.BreakoutDaysRemaining > 0)
            {
                rawRevenue *= product.BreakoutMultiplier;
                product.BreakoutDaysRemaining--;
                if (product.BreakoutDaysRemaining <= 0)
                    product.IsBreakout = false;
            }
        }
        else
        {
            // --- One-Time Purchase Path (daily) ---
            // MarketShareResolver has already set ActiveUserCount for this tick.
            // PreviousDailyActiveUsers was captured at end of previous tick, before MarketSystem ran.
            int userDelta = product.ActiveUserCount - product.PreviousDailyActiveUsers;
            int newBuyers = Math.Max(0, userDelta);

            float unitPrice = product.PriceOverride > 0f ? product.PriceOverride : config.pricePerUnit;
            rawRevenue = newBuyers * unitPrice * priceMult;
            product.TotalUnitsSold += newBuyers;

            // Apply breakout multiplier to revenue
            if (product.IsBreakout && product.BreakoutDaysRemaining > 0)
            {
                rawRevenue *= product.BreakoutMultiplier;
                product.BreakoutDaysRemaining--;
                if (product.BreakoutDaysRemaining <= 0)
                    product.IsBreakout = false;
            }

            // Decay tail daily — maintenance slows it
            float decayThisDay = config.tailDecayRate / 30f;
            if (product.IsMaintained)
                decayThisDay = Math.Max(0f, decayThisDay - config.maintenancePopDecayReduction / 30f);

            // Feature-heavy identity modifier: +10% decay if no updates after 60 days
            if (product.IdentityAtShip.IsValid && product.IdentityAtShip.FeatureScope >= 40 &&
                product.UpdateCount == 0 && product.TicksSinceShip > TimeState.TicksPerDay * 60)
                decayThisDay *= 1.10f;

            product.TailDecayFactor = Math.Max(config.minTailFactor, product.TailDecayFactor * (1f - decayThisDay));
        }

        // Remainder accumulation to prevent small products earning $0/day
        rawRevenue *= _tuning != null ? _tuning.ProductRevenueMultiplier : 1f;
        int displayRevenue = (int)rawRevenue;
        product.DailyRevenueRemainder += rawRevenue - displayRevenue;
        if (product.DailyRevenueRemainder >= 1f)
        {
            int extra = (int)product.DailyRevenueRemainder;
            displayRevenue += extra;
            product.DailyRevenueRemainder -= extra;
        }

        // Manufacturing cost deduction for console products (razor-and-blades model)
        if (product.Category == ProductCategory.GameConsole && product.ManufactureCostPerUnit > 0 && !config.isSubscriptionBased)
        {
            int userDelta = product.ActiveUserCount - product.PreviousDailyActiveUsers;
            int newBuyers = Math.Max(0, userDelta);
            int dailyManufacturingCost = newBuyers * product.ManufactureCostPerUnit;
            product.TotalHardwareRevenue += displayRevenue;
            product.TotalManufacturingCost += dailyManufacturingCost;
            displayRevenue -= dailyManufacturingCost;
            if (_financeSystem != null && dailyManufacturingCost > 0)
                _financeSystem.TrySubtractMoney(dailyManufacturingCost, out _);
        }

        // Deduct royalties for using competitor Licensed tools
        if (product.RequiredToolIds != null) {
            for (int t = 0; t < product.RequiredToolIds.Length; t++) {
                if (!_state.shippedProducts.TryGetValue(product.RequiredToolIds[t], out var usedTool)) continue;
                if (usedTool.DistributionModel != ToolDistributionModel.Licensed) continue;
                if (usedTool.PlayerLicensingRate <= 0f) continue;
                if (!usedTool.IsCompetitorProduct) continue;
                int royaltyCost = (int)(displayRevenue * usedTool.PlayerLicensingRate);
                displayRevenue -= royaltyCost;
                if (_financeSystem != null && royaltyCost > 0)
                    _financeSystem.TrySubtractMoney(royaltyCost, out _);
            }
        }
        // Deduct royalties for using competitor Licensed platforms
        if (product.TargetPlatformIds != null) {
            for (int t = 0; t < product.TargetPlatformIds.Length; t++) {
                if (!_state.shippedProducts.TryGetValue(product.TargetPlatformIds[t], out var usedPlat)) continue;
                if (usedPlat.DistributionModel != ToolDistributionModel.Licensed) continue;
                if (usedPlat.PlayerLicensingRate <= 0f) continue;
                if (!usedPlat.IsCompetitorProduct) continue;
                int royaltyCost = (int)(displayRevenue * usedPlat.PlayerLicensingRate);
                displayRevenue -= royaltyCost;
                if (_financeSystem != null && royaltyCost > 0)
                    _financeSystem.TrySubtractMoney(royaltyCost, out _);
            }
        }

        product.DailyRevenue = displayRevenue;
        product.AccumulatedMonthlyRevenue += displayRevenue;
        product.TotalLifetimeRevenue += displayRevenue;

        if (_financeSystem != null && displayRevenue > 0)
            _financeSystem.AddMoney(displayRevenue);

        UpdateLifecycleStage(product, config, tick);
        UpdatePopularityScore(product);

        if (_reputationSystem != null && _tuning != null) {
            float effectiveQuality = _reputationSystem.GetEffectiveQuality(product.OverallQuality);
            float minQuality = _tuning.FanMinQualityThreshold;
            if (effectiveQuality >= minQuality &&
                (product.LifecycleStage == ProductLifecycleStage.Launch ||
                 product.LifecycleStage == ProductLifecycleStage.Growth)) {
                float qualityFactor = (effectiveQuality - minQuality) / (100f - minQuality);
                int newFans = (int)(product.ActiveUserCount * _tuning.FanConversionRate / 30f * qualityFactor);
                if (newFans > 0)
                    _reputationSystem.AddFans(newFans, product.Id);
            }
        }

        if (product.IsMaintained && product.MaintenanceQuality > 0f)
        {
            if (product.TeamAssignments.TryGetValue(ProductTeamRole.QA, out var qaTeamId))
            {
                var qaTeam = _teamSystem?.GetTeam(qaTeamId);
                int qaTeamSize = qaTeam?.members.Count ?? 0;
                int optimalSize = ComputeMaintenanceOptimalTeamSize(product);
                float coverageRatio = Math.Clamp((float)qaTeamSize / Math.Max(1, optimalSize), 0.2f, 1.5f);
                float qualityFactor = product.MaintenanceQuality / 100f;
                float bugFixRate = (_tuning?.MaintenanceBugFixBaseRate ?? 0.4f) * qualityFactor * coverageRatio;
                product.BugsRemaining = Math.Max(0f, product.BugsRemaining - bugFixRate);
            }
        }
        else if (!product.IsMaintained)
        {
            float ageMonths = product.TicksSinceShip / (float)(TimeState.TicksPerDay * 30);
            float agePenalty = Math.Min(0.3f, ageMonths * (_tuning?.UnmaintainedBugGrowthAgePenaltyRate ?? 0.01f));
            float userScale = _tuning?.UnmaintainedBugGrowthUserScale ?? 0.05f;
            float baseBugRate = config.unmaintainedBugGrowthBase / 30f + (product.ActiveUserCount / 50000f) * userScale;
            float organicBugs = (baseBugRate + agePenalty) * (1f + _rng.NextFloat01() * 0.5f);
            product.BugsRemaining = Math.Min(100f, product.BugsRemaining + organicBugs);
        }

        // Recompute projections every 7 days to avoid waste
        if (product.TicksSinceShip % (TimeState.TicksPerDay * 7) == 0)
            ComputeProjections(product, config);

        product.TicksSinceShip += TimeState.TicksPerDay;

        // Capture current resolver-set count so next tick's delta is accurate
        product.PreviousDailyActiveUsers = product.ActiveUserCount;
    }

    private void ComputeProjections(Product product, ProductEconomyConfig config)
    {
        ComputeMonthlyProjections(product, config);
    }

    private void ComputeMonthlyProjections(Product product, ProductEconomyConfig config)
    {
        int recentDelta = product.ActiveUserCount - product.PreviousDailyActiveUsers;
        product.ProjectedActiveUsers = Math.Max(0, product.ActiveUserCount + recentDelta * 30);
        product.ProjectedMonthlyRevenue = product.DailyRevenue * 30;
    }

    public float GetCompetitorUnitPrice(Product product) {
        if (product.PriceOverride > 0f) return product.PriceOverride;
        if (product.TemplateId != null && _templateLookup != null
            && _templateLookup.TryGetValue(product.TemplateId, out var tmpl)
            && tmpl.economyConfig != null) {
            return tmpl.economyConfig.pricePerUnit;
        }
        return 20f;
    }

    private void UpdateLifecycleStage(Product product, ProductEconomyConfig config, int tick)
    {
        var oldStage = product.LifecycleStage;
        int ticksInCurrentStage = tick - product.LastStageChangeTick;
        int minGrowthTicks = (_tuning != null ? _tuning.MinGrowthStageDays : 60) * TimeState.TicksPerDay;
        int minPlateauTicks = (_tuning != null ? _tuning.MinPlateauStageDays : 30) * TimeState.TicksPerDay;

        switch (product.LifecycleStage)
        {
            case ProductLifecycleStage.Launch:
                if (product.TicksSinceShip >= config.ticksToGrowthStage)
                    product.LifecycleStage = ProductLifecycleStage.Growth;
                break;
            case ProductLifecycleStage.Growth:
                if (ticksInCurrentStage >= minGrowthTicks && product.PopularityScore < 40f)
                    product.LifecycleStage = ProductLifecycleStage.Plateau;
                break;
            case ProductLifecycleStage.Plateau:
                if (ticksInCurrentStage >= minPlateauTicks && product.PopularityScore < 15f)
                    product.LifecycleStage = ProductLifecycleStage.Decline;
                break;
            case ProductLifecycleStage.Decline:
                if (product.ActiveUserCount <= 0)
                {
                    _pendingEvents.Add(new PendingEvent { Type = PendingEventType.ProductDead, ProductId = product.Id });
                }
                break;
        }

        if (product.LifecycleStage != oldStage)
        {
            product.LastStageChangeTick = tick;
            _pendingEvents.Add(new PendingEvent { Type = PendingEventType.LifecycleChanged, ProductId = product.Id, StageA = oldStage, StageB = product.LifecycleStage });
            _logger.Log($"[ProductSystem] '{product.ProductName}' lifecycle: {oldStage} → {product.LifecycleStage}");
        }
    }

    private void UpdatePopularityScore(Product product)
    {
        float qualityTarget = product.OverallQuality * 0.7f;
        float receptionBonus = (product.PublicReceptionScore / 100f) * 15f;
        float maintainedBonus = product.IsMaintained ? 10f : 0f;
        float unmaintainedPenalty = (!product.IsMaintained && product.LifecycleStage != ProductLifecycleStage.Launch) ? -8f : 0f;
        float bugPenalty = -Math.Min(15f, product.BugsRemaining * 0.15f);
        float repScore = _reputationSystem != null ? _reputationSystem.GlobalReputation : 0f;
        float reputationBonus = (repScore / 1500f) * 10f;
        float stageModifier = product.LifecycleStage switch {
            ProductLifecycleStage.Growth  => 5f,
            ProductLifecycleStage.Plateau => 0f,
            ProductLifecycleStage.Decline => -10f,
            _                             => 0f,
        };

        // --- Market-aware modifiers ---

        // 1. Market demand: products in hot niches sustain popularity, cold niches lose it
        float demand = GetProductDemand(product);
        float demandModifier = (demand - 50f) * 0.15f;  // range: -7.5 to +7.5

        // 2. Saturation: more competitors in the niche = more downward pressure
        int competitorCount = CountProductsInSameMarket(product) - 1; // exclude self
        float saturationPenalty = -Math.Min(8f, competitorCount * 0.6f);  // 0 to -8

        // 3. Relative quality: above-average products hold better, below-average fall faster
        float avgQuality = ComputeAverageMarketQuality(product);
        float qualityDelta = avgQuality > 0f ? (product.OverallQuality - avgQuality) / 100f : 0f;
        float relativeQualityModifier = qualityDelta * 10f;  // range: -5 to +5

        // 4. Age decay: gentle downward drift after 12 months regardless of other factors
        float ageMonths = product.TicksSinceShip / (float)(TimeState.TicksPerDay * 30);
        float ageDecay = ageMonths > 12f ? -Math.Min(5f, (ageMonths - 12f) * 0.25f) : 0f;

        float equilibrium = qualityTarget + receptionBonus + maintainedBonus + unmaintainedPenalty + bugPenalty + reputationBonus + stageModifier
            + demandModifier + saturationPenalty + relativeQualityModifier + ageDecay;
        if (equilibrium < 0f) equilibrium = 0f;
        if (equilibrium > 85f) equilibrium = 85f;

        float gap = equilibrium - product.PopularityScore;
        float convergenceRate = _tuning != null ? _tuning.PopularityConvergenceRate : 0.05f;
        float speed = Math.Max(0.02f, convergenceRate * (Math.Abs(gap) / 50f));
        float newPop = product.PopularityScore + gap * speed;
        if (newPop < 0f) newPop = 0f;
        if (newPop > 100f) newPop = 100f;
        product.PopularityScore = newPop;
    }

    private float GetProductDemand(Product product) {
        if (_marketSystem == null) return 50f;
        if (product.Niche != ProductNiche.None)
            return _marketSystem.GetNicheDemand(product.Niche);
        return _marketSystem.GetCategoryDemand(product.Category);
    }

    private int CountProductsInSameMarket(Product product) {
        int count = 0;
        foreach (var kvp in _state.shippedProducts) {
            var p = kvp.Value;
            if (!p.IsOnMarket) continue;
            if (product.Niche != ProductNiche.None) {
                if (p.Niche == product.Niche) count++;
            } else {
                if (p.Niche == ProductNiche.None && p.Category == product.Category) count++;
            }
        }
        return count;
    }

    private float ComputeAverageMarketQuality(Product product) {
        float sum = 0f;
        int count = 0;
        foreach (var kvp in _state.shippedProducts) {
            var p = kvp.Value;
            if (!p.IsOnMarket) continue;
            bool sameMarket = product.Niche != ProductNiche.None
                ? p.Niche == product.Niche
                : (p.Niche == ProductNiche.None && p.Category == product.Category);
            if (!sameMarket) continue;
            sum += p.OverallQuality;
            count++;
        }
        return count > 0 ? sum / count : 0f;
    }

    private void ProcessReputationDecay()
    {
        if (_reputationSystem == null) return;

        _shippedProductIds.Clear();
        foreach (var key in _state.shippedProducts.Keys)
            _shippedProductIds.Add(key);

        int count = _shippedProductIds.Count;
        for (int i = 0; i < count; i++)
        {
            if (!_state.shippedProducts.TryGetValue(_shippedProductIds[i], out var product)) continue;
            if (product.LifecycleStage != ProductLifecycleStage.Decline) continue;
            if (product.TicksSinceShip % (TimeState.TicksPerDay * 30) == 0)
            {
                int repDecay = (int)((100f - product.PopularityScore) * 0.02f);
                if (repDecay > 0)
                    _reputationSystem.RemoveReputation(repDecay);
            }
            float fanDecayRate = _tuning?.FanDecayRateOnProductDeath ?? 0.15f;
            _reputationSystem.DecayFansForProduct(product.Id, fanDecayRate);
        }
    }


    public void Tick(int tick)
    {
        _currentTick = tick;
        _productIds.Clear();
        foreach (var key in _state.developmentProducts.Keys)
            _productIds.Add(key);

        int productCount = _productIds.Count;
        for (int i = 0; i < productCount; i++)
        {
            if (!_state.developmentProducts.TryGetValue(_productIds[i], out var product)) continue;

            bool anyProgress = false;

            int phaseCount = product.Phases.Length;
            for (int p = 0; p < phaseCount; p++)
            {
                var phase = product.Phases[p];
                if (!phase.isUnlocked || (phase.isComplete && !phase.isIterating)) continue;

                if (phase.isIterating)
                    TickIteration(product, phase);
                else
                    TickPhaseWork(product, phase);

                anyProgress = true;
            }

            // Check for newly unlockable gated phases
            _phaseUnlockQueue.Clear();
            for (int p = 0; p < phaseCount; p++)
            {
                var phase = product.Phases[p];
                if (phase.isUnlocked) continue;
                if (AllPrerequisitesComplete(product, phase))
                    _phaseUnlockQueue.Add(p);
            }

            int unlockCount = _phaseUnlockQueue.Count;
            for (int u = 0; u < unlockCount; u++)
            {
                int idx = _phaseUnlockQueue[u];
                product.Phases[idx].isUnlocked = true;

                if (product.Phases[idx].phaseType == ProductPhaseType.QA)
                    RecalculateQABugWork(product, product.Phases[idx]);

                _pendingEvents.Add(new PendingEvent { Type = PendingEventType.PhaseUnlocked, ProductId = product.Id, PhaseType = product.Phases[idx].phaseType });
            }

            product.TotalDevelopmentTicks++;

            if (anyProgress && (tick % ProgressEventThrottleTicks == 0))
            {
                _pendingEvents.Add(new PendingEvent { Type = PendingEventType.ProductProgressUpdated, ProductId = product.Id });
            }
        }

        // Process updates on shipped products
        _shippedProductIds.Clear();
        foreach (var key in _state.shippedProducts.Keys)
            _shippedProductIds.Add(key);

        int shippedCount = _shippedProductIds.Count;
        for (int i = 0; i < shippedCount; i++)
        {
            if (!_state.shippedProducts.TryGetValue(_shippedProductIds[i], out var product)) continue;
            if (product.CurrentUpdate != null && product.CurrentUpdate.isUpdating)
                TickUpdate(product, tick);

            // Accumulate technical debt on all features every tick
            TickTechnicalDebt(product, tick);

            // Process gradual successor migration
            TickSuccessorMigration(product);

            // Process sale timers
            if (product.IsOnSale)
            {
                product.SaleTicksRemaining--;
                product.TicksSinceLastSale = 0;
                if (product.SaleTicksRemaining <= 0)
                {
                    product.IsOnSale = false;
                    _pendingEvents.Add(new PendingEvent { Type = PendingEventType.ProductSaleEnded, ProductId = product.Id });
                }
            }
            else
            {
                product.TicksSinceLastSale++;
            }

            // Process ad timers
            if (product.IsRunningAds)
            {
                product.AdTicksRemaining--;
                if (product.AdTicksRemaining <= 0)
                {
                    product.IsRunningAds = false;
                    product.TeamAssignments.TryGetValue(ProductTeamRole.Marketing, out var adMarketingTeamId);
                    _pendingEvents.Add(new PendingEvent { Type = PendingEventType.AdRunEnded, ProductId = product.Id, TeamId = adMarketingTeamId, FloatA = _tuning?.MarketingXPPerAdRun ?? 0.5f });
                }
            }
        }
    }

    public void PostTick(int tick)
    {
        int count = _pendingEvents.Count;
        for (int i = 0; i < count; i++)
        {
            var e = _pendingEvents[i];
            switch (e.Type)
            {
                case PendingEventType.ProductCreated:
                    OnProductCreated?.Invoke(e.ProductId);
                    break;
                case PendingEventType.TeamAssigned:
                    OnTeamAssignedToProduct?.Invoke(e.ProductId, e.TeamId, e.Role);
                    break;
                case PendingEventType.TeamUnassigned:
                    OnTeamUnassignedFromProduct?.Invoke(e.ProductId, e.TeamId);
                    break;
                case PendingEventType.PhaseUnlocked:
                    OnPhaseUnlocked?.Invoke(e.ProductId, e.PhaseType);
                    break;
                case PendingEventType.PhaseCompleted:
                    OnPhaseCompleted?.Invoke(e.ProductId, e.PhaseType, e.FloatA);
                    break;
                case PendingEventType.PhaseIterationStarted:
                    OnPhaseIterationStarted?.Invoke(e.ProductId, e.PhaseType, e.IntA);
                    break;
                case PendingEventType.PhaseIterationCompleted:
                    OnPhaseIterationCompleted?.Invoke(e.ProductId, e.PhaseType, e.FloatA);
                    break;
                case PendingEventType.ProductProgressUpdated:
                    OnProductProgressUpdated?.Invoke(e.ProductId);
                    break;
                case PendingEventType.ProductShipped:
                    OnProductShipped?.Invoke(e.ProductId, e.FloatA);
                    break;
                case PendingEventType.ProductAbandoned:
                    OnProductAbandoned?.Invoke(e.ProductId);
                    break;
                case PendingEventType.SkillsAwarded:
                    OnSkillsAwarded?.Invoke(e.EmployeeIds);
                    break;
                case PendingEventType.FeaturePivoted:
                    OnFeaturePivoted?.Invoke(e.ProductId, e.PivotAction, e.StringA, e.StringB);
                    break;
                case PendingEventType.ProductLaunched:
                    OnProductLaunched?.Invoke(e.ProductId, e.IntA);
                    break;
                case PendingEventType.ProductDead:
                    OnProductDead?.Invoke(e.ProductId);
                    break;
                case PendingEventType.LifecycleChanged:
                    OnLifecycleChanged?.Invoke(e.ProductId, e.StageA, e.StageB);
                    break;
                case PendingEventType.ProductSaleStarted:
                    OnProductSaleStarted?.Invoke(e.ProductId);
                    break;
                case PendingEventType.ProductSaleEnded:
                    OnProductSaleEnded?.Invoke(e.ProductId);
                    break;
                case PendingEventType.MarketingStarted:
                    OnMarketingStarted?.Invoke(e.ProductId);
                    break;
                case PendingEventType.MarketingStopped:
                    OnMarketingStopped?.Invoke(e.ProductId);
                    break;
                case PendingEventType.HypeChanged:
                    OnHypeChanged?.Invoke(e.ProductId, e.FloatA, e.IntA);
                    break;
                case PendingEventType.HypeUnderdelivered:
                    OnHypeUnderdelivered?.Invoke(e.ProductId, e.FloatA, e.IntA);
                    break;
                case PendingEventType.AdRunStarted:
                    OnAdRunStarted?.Invoke(e.ProductId, e.IntA);
                    break;
                case PendingEventType.AdRunEnded:
                    OnAdRunEnded?.Invoke(e.ProductId);
                    AwardMarketingXPForProduct(e.ProductId, e.TeamId, e.FloatA);
                    break;
                case PendingEventType.UpdateAnnounced:
                    OnUpdateAnnounced?.Invoke(e.ProductId);
                    break;
                case PendingEventType.HypeEvent:
                    OnHypeEvent?.Invoke(e.ProductId, e.HypeEventType, e.BoolA);
                    break;
                case PendingEventType.ProductCrisis:
                    OnProductCrisis?.Invoke(e.ProductId, e.CrisisType);
                    break;
                case PendingEventType.ProductSold:
                    OnProductSold?.Invoke(e.ProductId, e.CompetitorId, e.LongA);
                    break;
                case PendingEventType.LogUpdate:
                    _logger.Log($"[ProductSystem] Update '{e.UpdateType}' complete on product {e.ProductId.Value}");
                    break;
                case PendingEventType.LogUpdateEnd:
                    _logger.Log($"[ProductSystem] Update '{e.UpdateType}' started on product {e.ProductId.Value}, work required: {e.FloatA:F0}");
                    break;
                case PendingEventType.LogRemoveFromMarket:
                    _logger.Log($"[ProductSystem] Product {e.ProductId.Value} removed from market and archived.");
                    break;
                case PendingEventType.ReleaseDateAnnounced:
                    OnReleaseDateAnnounced?.Invoke(e.ProductId, e.IntA);
                    break;
                case PendingEventType.ReleaseDateChanged:
                    OnReleaseDateChanged?.Invoke(new ReleaseDateChangedEvent
                    {
                        ProductId = e.ProductId,
                        OldTick = e.IntA,
                        NewTick = e.IntB,
                        IsRush = e.BoolA,
                        ShiftCount = (int)e.FloatA
                    });
                    break;
                case PendingEventType.ShipWarning:
                    OnShipWarning?.Invoke(e.ProductId, e.StringA, e.IntA, e.IntB);
                    break;
            }
        }
        _pendingEvents.Clear();

        int identityChangeCount = _pendingIdentityChanges.Count;
        for (int i = 0; i < identityChangeCount; i++)
        {
            var ic = _pendingIdentityChanges[i];
            OnProductIdentityChanged?.Invoke(ic.ProductId, ic.Previous, ic.Current);
        }
        _pendingIdentityChanges.Clear();
    }

    // ─── Update Tick Helpers ───────────────────────────────────────────────────

    private void TickUpdate(Product product, int tick)
    {
        var update = product.CurrentUpdate;

        switch (update.updateType)
        {
            case ProductUpdateType.BugFix:
                if (product.TeamAssignments.TryGetValue(ProductTeamRole.QA, out var qaTeamId))
                {
                    var qaTeam = _teamSystem.GetTeam(qaTeamId);
                    if (qaTeam != null)
                    {
                        int optimalQASize = ComputeMaintenanceOptimalTeamSize(product);
                        var result = TeamWorkEngine.AggregateTeam(
                            qaTeam.members, _employeeSystem, _fatigueSystem, SkillId.QaTesting,
                            _roleProfileTable, _tuning?.TeamOverheadPerMember ?? 0.04f,
                            optimalTeamSize: optimalQASize);
                        float variance = 0.95f + _rng.NextFloat01() * 0.10f;
                        ChemistryBand bugFixChemBand = _chemistrySystem != null
                            ? _chemistrySystem.GetTeamChemistry(qaTeam.id).Band
                            : ChemistryBand.Neutral;
                        float bugFixConflictSpeed = _chemistrySystem != null
                            ? 1f + _chemistrySystem.GetTeamSpeedPenalty(qaTeam.id)
                            : 1f;
                        float work = TeamWorkEngine.ComputeWorkPerTick(in result, WorkRatePerSkillPoint, result.CoverageSpeedMod, variance,
                            1f, TeamWorkEngine.GetChemistrySpeedMod(bugFixChemBand), bugFixConflictSpeed);
                        update.updateWorkCompleted += work;
                    }
                }
                break;

            case ProductUpdateType.AddFeatures:
            case ProductUpdateType.UpgradeFeature:
                float totalWork = 0f;
                int updateOptimalSize = GetPhaseOptimalTeamSize(ProductPhaseType.Programming, product);
                foreach (var kvp in product.TeamAssignments)
                {
                    ProductPhaseType phaseType = MapRoleToPhase(kvp.Key);
                    SkillId skillType = TeamWorkEngine.MapPhaseToSkill(phaseType);
                    var assignedTeam = _teamSystem.GetTeam(kvp.Value);
                    if (assignedTeam != null)
                    {
                        var result = TeamWorkEngine.AggregateTeam(
                            assignedTeam.members, _employeeSystem, _fatigueSystem, skillType,
                            _roleProfileTable, _tuning?.TeamOverheadPerMember ?? 0.04f,
                            optimalTeamSize: updateOptimalSize);
                        float variance = 0.95f + _rng.NextFloat01() * 0.10f;
                        ChemistryBand updChemBand = _chemistrySystem != null
                            ? _chemistrySystem.GetTeamChemistry(assignedTeam.id).Band
                            : ChemistryBand.Neutral;
                        float updConflictSpeed = _chemistrySystem != null
                            ? 1f + _chemistrySystem.GetTeamSpeedPenalty(assignedTeam.id)
                            : 1f;
                        totalWork += TeamWorkEngine.ComputeWorkPerTick(in result, WorkRatePerSkillPoint, result.CoverageSpeedMod, variance,
                            0.25f, TeamWorkEngine.GetChemistrySpeedMod(updChemBand), updConflictSpeed);
                    }
                }
                update.updateWorkCompleted += totalWork;
                break;
        }

        if (update.updateWorkRequired > 0f && update.updateWorkCompleted >= update.updateWorkRequired)
            CompleteUpdate(product, tick);
    }

    private void CompleteUpdate(Product product, int tick)
    {
        var update = product.CurrentUpdate;

        switch (update.updateType)
        {
            case ProductUpdateType.BugFix:
                float fixPercent = 0.6f + _rng.NextFloat01() * 0.3f;
                product.BugsRemaining = Math.Max(0f, product.BugsRemaining * (1f - fixPercent));
                product.PopularityScore = Math.Min(100f, product.PopularityScore + 5f);
                break;

            case ProductUpdateType.AddFeatures:
                if (update.targetFeatureIds != null)
                {
                    var featureList = new List<string>(product.SelectedFeatureIds ?? new string[0]);
                    for (int i = 0; i < update.targetFeatureIds.Length; i++)
                    {
                        string fId = update.targetFeatureIds[i];
                        if (!featureList.Contains(fId))
                            featureList.Add(fId);
                    }
                    product.SelectedFeatureIds = featureList.ToArray();
                    product.PopularityScore = Math.Min(100f, product.PopularityScore + 3f * update.targetFeatureIds.Length);
                    for (int i = 0; i < update.targetFeatureIds.Length; i++)
                        product.BugsRemaining += 2f + _rng.NextFloat01() * 6f;
                    product.BugsRemaining = Math.Min(100f, product.BugsRemaining);

                    if (product.Features != null)
                    {
                        for (int i = 0; i < update.targetFeatureIds.Length; i++)
                        {
                            string newFid = update.targetFeatureIds[i];
                            bool alreadyTracked = false;
                            for (int fi = 0; fi < product.Features.Length; fi++)
                            {
                                if (product.Features[fi]?.FeatureId == newFid) { alreadyTracked = true; break; }
                            }
                            if (!alreadyTracked)
                            {
                                var newState = new ProductFeatureState
                                {
                                    FeatureId = newFid,
                                    Quality = product.OverallQuality * 0.5f,
                                    TechnicalDebt = 0f,
                                    LastUpgradeTick = tick,
                                    IsNew = true
                                };
                                var expanded = new ProductFeatureState[product.Features.Length + 1];
                                for (int fi = 0; fi < product.Features.Length; fi++)
                                    expanded[fi] = product.Features[fi];
                                expanded[product.Features.Length] = newState;
                                product.Features = expanded;
                            }
                        }
                    }
                }
                break;

            case ProductUpdateType.UpgradeFeature:
                if (update.targetFeatureIds != null && product.Features != null)
                {
                    float qualityGain = 10f + _rng.NextFloat01() * 15f;
                    for (int i = 0; i < update.targetFeatureIds.Length; i++)
                    {
                        string upgId = update.targetFeatureIds[i];
                        for (int fi = 0; fi < product.Features.Length; fi++)
                        {
                            if (product.Features[fi]?.FeatureId == upgId)
                            {
                                product.Features[fi].Quality = Math.Min(100f, product.Features[fi].Quality + qualityGain);
                                product.Features[fi].TechnicalDebt = 0f;
                                product.Features[fi].LastUpgradeTick = tick;
                                product.Features[fi].IsNew = false;
                                break;
                            }
                        }
                    }
                    ApplyFeatureCeilings(product);
                    if (_templateLookup.TryGetValue(product.TemplateId, out var upgradeTemplate))
                        product.OverallQuality = RecalculateOverallQuality(product, upgradeTemplate);
                    product.PopularityScore = Math.Min(100f, product.PopularityScore + 4f * update.targetFeatureIds.Length);
                    product.IsLegacy = false;
                }
                break;
        }

        update.isUpdating = false;
        product.UpdateCount++;
        product.ProductVersion++;
        product.TicksSinceLastUpdate = 0;

        // Auto-cancel crunch on update completion
        foreach (var kvp in product.TeamAssignments) {
            var updateTeam = _teamSystem?.GetTeam(kvp.Value);
            if (updateTeam != null && updateTeam.isCrunching) {
                updateTeam.isCrunching = false;
                _fatigueSystem?.ResetCrunchTracking(updateTeam.members);
            }
        }

        // Apply update hype bonus if announced
        if (product.HasAnnouncedUpdate && product.UpdateHype > 0f)
        {
            float updateHypeBonus = _tuning?.UpdateHypeMaxBonus ?? 1.5f;
            product.PopularityScore = Math.Min(100f, product.PopularityScore + product.UpdateHype * 0.3f);
            product.HasAnnouncedUpdate = false;
            product.UpdateHype = 0f;
        }

        _pendingEvents.Add(new PendingEvent { Type = PendingEventType.LogUpdate, ProductId = product.Id, UpdateType = update.updateType });

        RecomputeCurrentIdentity(product, _currentTick);
    }

    private void RecomputeCurrentIdentity(Product product, int tick)
    {
        if (!product.IsShipped) return;
        if (!_templateLookup.TryGetValue(product.TemplateId, out var template)) return;

        var previous = product.CurrentIdentity;
        var next = ProductIdentityHelper.ComputeCurrent(product, template, _generationSystem, _platformSystem, _state, _tuning);
        product.CurrentIdentity = next;

        if (!previous.IsValid) return;
        bool significantChange =
            Math.Abs((int)next.PricePositioning - (int)previous.PricePositioning) >= 20 ||
            Math.Abs((int)next.InnovationRisk - (int)previous.InnovationRisk) >= 20 ||
            Math.Abs((int)next.AudienceBreadth - (int)previous.AudienceBreadth) >= 20 ||
            Math.Abs((int)next.FeatureScope - (int)previous.FeatureScope) >= 20 ||
            Math.Abs((int)next.ProductionDiscipline - (int)previous.ProductionDiscipline) >= 20;

        if (significantChange)
            _pendingIdentityChanges.Add(new PendingIdentityChange { ProductId = product.Id, Previous = previous, Current = next });
    }

    private static ProductPhaseType MapRoleToPhase(ProductTeamRole role)
    {
        switch (role)
        {
            case ProductTeamRole.Development: return ProductPhaseType.Programming;
            case ProductTeamRole.Design:      return ProductPhaseType.Design;
            case ProductTeamRole.QA:          return ProductPhaseType.QA;
            default:                          return ProductPhaseType.Programming;
        }
    }

    public void ApplyCommand(ICommand command)
    {
        switch (command)
        {
            case CreateProductCommand createProduct:
                HandleCreateProduct(createProduct);
                break;
            case AssignTeamToProductCommand assignTeam:
                HandleAssignTeam(assignTeam);
                break;
            case UnassignTeamFromProductCommand unassignTeam:
                HandleUnassignTeam(unassignTeam);
                break;
            case IteratePhaseCommand iteratePhase:
                HandleIteratePhase(iteratePhase);
                break;
            case ShipProductCommand shipProduct:
                HandleShipProduct(shipProduct);
                break;
            case AbandonProductCommand abandonProduct:
                HandleAbandonProduct(abandonProduct);
                break;
            case AnnounceReleaseDateCommand announceDate:
                HandleAnnounceReleaseDate(announceDate);
                break;
            case ChangeReleaseDateCommand changeDate:
                HandleChangeReleaseDate(changeDate);
                break;
            case TriggerProductUpdateCommand triggerUpdate:
                HandleTriggerProductUpdate(triggerUpdate);
                break;
            case RemoveProductFromMarketCommand removeFromMarket:
                HandleRemoveProductFromMarket(removeFromMarket);
                break;
            case SetProductBudgetCommand setBudget:
                HandleSetProductBudget(setBudget);
                break;
            case TriggerSaleEventCommand triggerSale:
                HandleTriggerSaleEvent(triggerSale);
                break;
            case PivotFeatureCommand pivotFeature:
                HandlePivotFeature(pivotFeature);
                break;
            case SpawnShippedProductCheatCommand spawnShipped:
                HandleSpawnShippedCheat(spawnShipped);
                break;
            case BuildHypeCommand buildHype:
                HandleBuildHype(buildHype);
                break;
            case RunAdsCommand runAds:
                HandleRunAds(runAds);
                break;
            case AnnounceUpdateCommand announceUpdate:
                HandleAnnounceUpdate(announceUpdate);
                break;
            case SetToolDistributionCommand setDist:
                HandleSetToolDistribution(setDist);
                break;
        }
    }

    private void HandleSetToolDistribution(SetToolDistributionCommand cmd)
    {
        if (!_state.shippedProducts.TryGetValue(cmd.ProductId, out var product))
        {
            _logger.LogWarning($"[ProductSystem] SetToolDistribution failed: product {cmd.ProductId.Value} not found in shipped products.");
            return;
        }
        if (!product.Category.IsTool() && !product.Category.IsPlatform())
        {
            _logger.LogWarning($"[ProductSystem] SetToolDistribution failed: product {cmd.ProductId.Value} is not a Tool or Platform.");
            return;
        }
        if (product.DistributionModel == ToolDistributionModel.OpenSource)
        {
            _logger.LogWarning($"[ProductSystem] SetToolDistribution rejected: Open Source is permanent for product {cmd.ProductId.Value}.");
            return;
        }
        product.DistributionModel = cmd.Model;
        if (cmd.Model == ToolDistributionModel.Licensed)
        {
            product.PlayerLicensingRate = cmd.LicensingRate;
            product.IsSubscriptionBased = true;
            if (cmd.MonthlySubscriptionPrice > 0f)
                product.PriceOverride = cmd.MonthlySubscriptionPrice;
            product.ActiveSubscriberCount = 0;
        }
        else
        {
            product.PlayerLicensingRate = 0f;
            product.IsSubscriptionBased = false;
            product.PriceOverride = 0f;
            product.ActiveSubscriberCount = 0;
            product.MonthlySubscriptionPrice = 0f;
        }
    }

    public void Dispose()
    {
        if (_teamSystem != null)
            _teamSystem.OnTeamDeleted -= OnTeamDeleted;
        if (_timeSystem != null)
        {
            _timeSystem.OnDayChanged -= OnDayChanged;
            _timeSystem.OnMonthChanged -= OnMonthChanged;
        }
        _pendingEvents.Clear();
        OnProductCreated = null;
        OnTeamAssignedToProduct = null;
        OnTeamUnassignedFromProduct = null;
        OnPhaseUnlocked = null;
        OnPhaseCompleted = null;
        OnPhaseIterationStarted = null;
        OnPhaseIterationCompleted = null;
        OnProductProgressUpdated = null;
        OnProductShipped = null;
        OnProductAbandoned = null;
        OnSkillsAwarded = null;
        OnFeaturePivoted = null;
        OnProductLaunched = null;
        OnProductDead = null;
        OnLifecycleChanged = null;
        OnProductSaleStarted = null;
        OnProductSaleEnded = null;
        OnMarketingStarted = null;
        OnMarketingStopped = null;
        OnHypeChanged = null;
        OnHypeUnderdelivered = null;
        OnAdRunStarted = null;
        OnAdRunEnded = null;
        OnUpdateAnnounced = null;
        OnHypeEvent = null;
        OnProductCrisis = null;
        OnProductSold = null;
    }

    // ─── Command Handlers ──────────────────────────────────────────────────────

    private void HandleCreateProduct(CreateProductCommand cmd)
    {
        if (!_templateLookup.TryGetValue(cmd.TemplateId, out var template))
        {
            _logger.LogWarning($"[ProductSystem] CreateProduct failed: template '{cmd.TemplateId}' not found.");
            return;
        }

        // Base cost
        int totalCost = template.baseUpfrontCost;

        // Hardware dev cost add (console only, calculated before finance check)
        int hardwareDevCostAdd = 0;
        int manufactureCostPerUnit = 0;
        if (cmd.HasHardwareConfig && template.category == ProductCategory.GameConsole)
        {
            HardwareGenerationConfig genConfig = GetHardwareGenerationConfig(_generationSystem?.GetCurrentGeneration() ?? 1);
            if (genConfig != null)
            {
                manufactureCostPerUnit = genConfig.CalculateManufactureCost(cmd.HardwareConfig);
                hardwareDevCostAdd = genConfig.CalculateDevCostAdd(cmd.HardwareConfig);
            }
        }

        int totalUpfrontCost = totalCost + hardwareDevCostAdd;

        if (_financeSystem != null && _financeSystem.Money < totalUpfrontCost)
        {
            _logger.LogWarning($"[ProductSystem] CreateProduct failed: cannot afford ${totalUpfrontCost} (have ${_financeSystem.Money}).");
            return;
        }

        if (_financeSystem != null && totalUpfrontCost > 0)
        {
            if (!_financeSystem.TrySubtractMoney(totalUpfrontCost, out string error))
            {
                _logger.LogWarning($"[ProductSystem] CreateProduct failed to deduct cost: {error}");
                return;
            }
        }

        ProductNiche productNiche = DeriveNiche(template, cmd);
        float nicheDevTimeMult = GetNicheDevTimeMultiplier(productNiche);
        var phases = BuildPhaseRuntimes(template, cmd.SelectedFeatureIds, _tuning?.ProductBaseWorkMultiplier ?? 100f, nicheDevTimeMult, template.difficultyTier);

        // Subscription override: use player choice if set, otherwise template default
        bool isSubscription = cmd.IsSubscriptionBased;
        float priceOverride = cmd.Price;

        var product = new Product
        {
            Id = new ProductId(_state.nextProductId++),
            TemplateId = cmd.TemplateId,
            ProductName = cmd.ProductName,
            Category = template.category,
            Niche = productNiche,
            SelectedFeatureIds = cmd.SelectedFeatureIds,
            TargetPlatformIds = template.category.IsTool() ? null : cmd.TargetPlatformIds,
            RequiredToolIds = cmd.RequiredToolIds,
            Stance = cmd.Stance,
            PredecessorProductId = cmd.PredecessorProductId,
            IsSubscriptionBased = isSubscription,
            PriceOverride = priceOverride,
            Phases = phases,
            IsInDevelopment = true,
            IsShipped = false,
            IsOnMarket = false,
            UpfrontCostPaid = totalUpfrontCost,
            TeamAssignments = new Dictionary<ProductTeamRole, TeamId>(),
            PivotsUsed = 0,
            MaxPivots = 1,
            DroppedFeatureIds = new List<string>(),
            FeatureRelevanceAtShip = 1f,
            CreationTick = _currentTick,
            HasHardwareConfig = cmd.HasHardwareConfig,
            HardwareConfig = cmd.HardwareConfig,
            ManufactureCostPerUnit = manufactureCostPerUnit
        };

        _state.developmentProducts[product.Id] = product;

        product.DistributionModel = cmd.DistributionModel;
        product.PlayerLicensingRate = cmd.LicensingRate;
        if (template.category.IsTool() && cmd.DistributionModel == ToolDistributionModel.Licensed && cmd.MonthlySubscriptionPrice > 0f)
            product.MonthlySubscriptionPrice = cmd.MonthlySubscriptionPrice;

        if (cmd.TargetDay > 0)
        {
            int targetTick = cmd.TargetDay * TimeState.TicksPerDay;
            product.TargetReleaseTick = targetTick;
            product.OriginalReleaseTick = targetTick;
            product.HasAnnouncedReleaseDate = true;
        }

        // Handle sequel linkage
        if (cmd.SequelOfId.HasValue)
        {
            product.SequelOfId = cmd.SequelOfId;
            Product original = null;
            if (_state.shippedProducts.TryGetValue(cmd.SequelOfId.Value, out original) ||
                _state.archivedProducts.TryGetValue(cmd.SequelOfId.Value, out original))
            {
                if (original.SequelIds == null)
                    original.SequelIds = new List<ProductId>();
                original.SequelIds.Add(product.Id);
                product.SequelNumber = original.SequelNumber + original.SequelIds.Count;
            }
        }

        // Process initial team assignments (reuse HandleAssignTeam logic)
        if (cmd.InitialTeamAssignments != null)
        {
            for (int i = 0; i < cmd.InitialTeamAssignments.Length; i++)
            {
                var assignment = cmd.InitialTeamAssignments[i];
                if (_teamSystem != null && _teamSystem.GetTeam(assignment.TeamId) == null) continue;
                if (_contractState != null && _contractState.teamAssignments.ContainsKey(assignment.TeamId)) continue;
                if (_state.teamToProduct.TryGetValue(assignment.TeamId, out var existingProductId))
                {
                    if (existingProductId != product.Id) continue;
                }
                if (product.TeamAssignments.TryGetValue(assignment.Role, out var prevTeamId))
                {
                    _state.teamToProduct.Remove(prevTeamId);
                    product.TeamAssignments.Remove(assignment.Role);
                }
                product.TeamAssignments[assignment.Role] = assignment.TeamId;
                _state.teamToProduct[assignment.TeamId] = product.Id;
            }
        }

        _pendingEvents.Add(new PendingEvent { Type = PendingEventType.ProductCreated, ProductId = product.Id });
        _logger.Log($"[ProductSystem] Created product '{product.ProductName}' (ID: {product.Id.Value}) — cost ${totalCost}");
        TryAutoAssignTeams(product);
    }

    private void TryAutoAssignTeams(Product product)
    {
        if (_teamSystem == null) return;

        for (int pi = 0; pi < product.Phases.Length; pi++)
        {
            ProductPhaseType phaseType = product.Phases[pi].phaseType;
            ProductTeamRole role;
            switch (phaseType)
            {
                case ProductPhaseType.Programming: role = ProductTeamRole.Development; break;
                case ProductPhaseType.Design:      role = ProductTeamRole.Design;      break;
                case ProductPhaseType.QA:          role = ProductTeamRole.QA;          break;
                case ProductPhaseType.SFX:         role = ProductTeamRole.Development; break;
                case ProductPhaseType.VFX:         role = ProductTeamRole.Development; break;
                default: continue;
            }

            if (product.TeamAssignments.ContainsKey(role)) continue;

            TeamType neededType = TeamTypeMapping.ToTeamType(role);
            var candidates = _teamSystem.GetFreeTeamsByType(neededType);
            int count = candidates.Count;
            for (int i = 0; i < count; i++)
            {
                TeamId candidateId = candidates[i];
                if (_state.teamToProduct.ContainsKey(candidateId)) continue;
                if (_contractState != null && _contractState.teamAssignments.ContainsKey(candidateId)) continue;
                var team = _teamSystem.GetTeam(candidateId);
                if (team == null || team.members.Count == 0) continue;

                product.TeamAssignments[role] = candidateId;
                _state.teamToProduct[candidateId] = product.Id;

                _pendingEvents.Add(new PendingEvent { Type = PendingEventType.TeamAssigned, ProductId = product.Id, TeamId = candidateId, Role = role });
                _logger.Log($"[ProductSystem] Auto-assigned team {candidateId.Value} to product {product.Id.Value} as {role}");
                break;
            }

            if (!product.TeamAssignments.ContainsKey(role))
                _logger.Log($"[ProductSystem] No free {neededType} team available for auto-assign to product {product.Id.Value} role {role}");
        }
    }

    private void HandleAssignTeam(AssignTeamToProductCommand cmd)
    {
        Product product = null;
        bool isShipped = _state.shippedProducts.TryGetValue(cmd.ProductId, out product);
        bool isDev = !isShipped && _state.developmentProducts.TryGetValue(cmd.ProductId, out product);

        if (!isShipped && !isDev)
        {
            _logger.LogWarning($"[ProductSystem] AssignTeam failed: product {cmd.ProductId.Value} not found.");
            return;
        }

        if (_teamSystem != null && _teamSystem.GetTeam(cmd.TeamId) == null)
        {
            _logger.LogWarning($"[ProductSystem] AssignTeam failed: team {cmd.TeamId.Value} not found.");
            return;
        }

        if (_teamSystem != null) {
            var teamType = _teamSystem.GetTeamType(cmd.TeamId);
            if (teamType == TeamType.HR) {
                _logger.LogWarning($"[ProductSystem] AssignTeam failed: {teamType} teams cannot work on products.");
                return;
            }
        }

        if (_contractState != null && _contractState.teamAssignments.ContainsKey(cmd.TeamId))
        {
            _logger.LogWarning($"[ProductSystem] AssignTeam failed: team {cmd.TeamId.Value} is already assigned to a contract.");
            return;
        }

        if (_state.teamToProduct.TryGetValue(cmd.TeamId, out var existingProductId))
        {
            if (existingProductId != cmd.ProductId)
            {
                Product oldProduct = null;
                if (!_state.developmentProducts.TryGetValue(existingProductId, out oldProduct))
                    _state.shippedProducts.TryGetValue(existingProductId, out oldProduct);

                if (oldProduct != null)
                {
                    ProductTeamRole oldRole = default;
                    bool oldFound = false;
                    foreach (var kvp in oldProduct.TeamAssignments)
                    {
                        if (kvp.Value == cmd.TeamId)
                        {
                            oldRole = kvp.Key;
                            oldFound = true;
                            break;
                        }
                    }
                    if (oldFound)
                        oldProduct.TeamAssignments.Remove(oldRole);
                }
                _state.teamToProduct.Remove(cmd.TeamId);
            }
        }

        // If role slot is already occupied, evict the previous team first
        if (product.TeamAssignments.TryGetValue(cmd.RoleSlot, out var previousTeamId))
        {
            _state.teamToProduct.Remove(previousTeamId);
            product.TeamAssignments.Remove(cmd.RoleSlot);
        }

        product.TeamAssignments[cmd.RoleSlot] = cmd.TeamId;
        _state.teamToProduct[cmd.TeamId] = cmd.ProductId;

        _pendingEvents.Add(new PendingEvent { Type = PendingEventType.TeamAssigned, ProductId = cmd.ProductId, TeamId = cmd.TeamId, Role = cmd.RoleSlot });
        _logger.Log($"[ProductSystem] Team {cmd.TeamId.Value} assigned to product {cmd.ProductId.Value} as {cmd.RoleSlot}");
    }

    private void HandleUnassignTeam(UnassignTeamFromProductCommand cmd)
    {
        Product product = null;
        bool isShipped = _state.shippedProducts.TryGetValue(cmd.ProductId, out product);
        bool isDev = !isShipped && _state.developmentProducts.TryGetValue(cmd.ProductId, out product);

        if (!isShipped && !isDev)
        {
            _logger.LogWarning($"[ProductSystem] UnassignTeam failed: product {cmd.ProductId.Value} not found.");
            return;
        }

        ProductTeamRole foundRole = default;
        bool found = false;
        foreach (var kvp in product.TeamAssignments)
        {
            if (kvp.Value == cmd.TeamId)
            {
                foundRole = kvp.Key;
                found = true;
                break;
            }
        }

        if (!found)
        {
            _logger.LogWarning($"[ProductSystem] UnassignTeam failed: team {cmd.TeamId.Value} not assigned to product {cmd.ProductId.Value}.");
            return;
        }

        product.TeamAssignments.Remove(foundRole);
        _state.teamToProduct.Remove(cmd.TeamId);

        _pendingEvents.Add(new PendingEvent { Type = PendingEventType.TeamUnassigned, ProductId = cmd.ProductId, TeamId = cmd.TeamId });
        _logger.Log($"[ProductSystem] Team {cmd.TeamId.Value} unassigned from product {cmd.ProductId.Value}");
    }

    private void HandleIteratePhase(IteratePhaseCommand cmd)
    {
        if (!_state.developmentProducts.TryGetValue(cmd.ProductId, out var product))
        {
            _logger.LogWarning($"[ProductSystem] IteratePhase failed: product {cmd.ProductId.Value} not found.");
            return;
        }

        var phase = FindPhase(product, cmd.PhaseType);
        if (phase == null)
        {
            _logger.LogWarning($"[ProductSystem] IteratePhase failed: phase {cmd.PhaseType} not found on product {cmd.ProductId.Value}.");
            return;
        }

        if (!phase.isComplete)
        {
            _logger.LogWarning($"[ProductSystem] IteratePhase failed: phase {cmd.PhaseType} is not yet complete.");
            return;
        }

        if (phase.isIterating)
        {
            _logger.LogWarning($"[ProductSystem] IteratePhase failed: phase {cmd.PhaseType} is already iterating.");
            return;
        }

        if (!product.TeamAssignments.ContainsKey(phase.primaryRole))
        {
            _logger.LogWarning($"[ProductSystem] IteratePhase failed: no team assigned to role {phase.primaryRole} for phase {cmd.PhaseType}.");
            return;
        }

        phase.isIterating = true;
        phase.iterationCount++;

        float workMult;
        switch (phase.iterationCount)
        {
            case 1:  workMult = 0.50f; break;
            case 2:  workMult = 0.35f; break;
            default: workMult = 0.35f; break;
        }

        phase.bonusWorkTarget = phase.totalWorkRequired * workMult;
        phase.bonusWorkCompleted = 0f;

        _pendingEvents.Add(new PendingEvent { Type = PendingEventType.PhaseIterationStarted, ProductId = cmd.ProductId, PhaseType = cmd.PhaseType, IntA = phase.iterationCount });
        _logger.Log($"[ProductSystem] Phase {cmd.PhaseType} iteration #{phase.iterationCount} started on product {cmd.ProductId.Value}");
    }

    private void HandleShipProduct(ShipProductCommand cmd)
    {
        if (!_state.developmentProducts.TryGetValue(cmd.ProductId, out var product))
        {
            _logger.LogWarning($"[ProductSystem] ShipProduct failed: product {cmd.ProductId.Value} not found.");
            return;
        }

        if (!product.HasAnnouncedReleaseDate)
        {
            _logger.LogWarning("[ProductSystem] ShipProduct failed: no release date announced.");
            return;
        }

        int phaseCount = product.Phases.Length;
        for (int p = 0; p < phaseCount; p++)
        {
            if (!product.Phases[p].isComplete)
            {
                _logger.LogWarning($"[ProductSystem] ShipProduct failed: phase {product.Phases[p].phaseType} is not complete.");
                return;
            }
            if (product.Phases[p].isIterating)
            {
                _logger.LogWarning($"[ProductSystem] ShipProduct failed: phase {product.Phases[p].phaseType} is still iterating.");
                return;
            }
        }

        // Weighted average quality
        float totalWork = 0f;
        for (int p = 0; p < phaseCount; p++)
            totalWork += product.Phases[p].totalWorkRequired;

        float overallQuality = 0f;
        if (totalWork > 0f)
        {
            for (int p = 0; p < phaseCount; p++)
            {
                float weight = product.Phases[p].totalWorkRequired / totalWork;
                overallQuality += product.Phases[p].phaseQuality * weight;
            }
        }

        // Feature synergy/conflict quality modifier still applies

        // Apply feature synergy/conflict quality modifier
        if (_templateLookup.TryGetValue(product.TemplateId, out var shipTemplate) &&
            shipTemplate.availableFeatures != null &&
            product.SelectedFeatureIds != null && product.SelectedFeatureIds.Length > 1)
        {
            float qualityMod = 0f;
            int featureCount = product.SelectedFeatureIds.Length;
            for (int fi = 0; fi < featureCount; fi++)
            {
                var featDef = FindFeatureDef(shipTemplate, product.SelectedFeatureIds[fi]);
                if (featDef == null) continue;

                if (featDef.synergyFeatureIds != null)
                {
                    for (int si = 0; si < featDef.synergyFeatureIds.Length; si++)
                    {
                        string synId = featDef.synergyFeatureIds[si];
                        for (int fj = fi + 1; fj < featureCount; fj++)
                        {
                            if (product.SelectedFeatureIds[fj] == synId)
                            {
                                qualityMod += featDef.synergyBonusPercent;
                                break;
                            }
                        }
                    }
                }

                if (featDef.conflictFeatureIds != null)
                {
                    for (int ci = 0; ci < featDef.conflictFeatureIds.Length; ci++)
                    {
                        string confId = featDef.conflictFeatureIds[ci];
                        for (int fj = fi + 1; fj < featureCount; fj++)
                        {
                            if (product.SelectedFeatureIds[fj] == confId)
                            {
                                qualityMod -= featDef.conflictPenaltyPercent;
                                break;
                            }
                        }
                    }
                }
            }
            if (qualityMod != 0f)
                overallQuality = Math.Max(0f, Math.Min(100f, overallQuality * (1f + qualityMod)));
        }

        product.FeatureRelevanceAtShip = ComputeFeatureRelevanceAtShip(product);

        // Initialize per-feature quality states from phase quality if not already set
        InitializeFeatureStates(product, template: _templateLookup.TryGetValue(product.TemplateId, out var initTemplate) ? initTemplate : null, overallQuality);

        // Recalculate overall quality from per-feature model (with ceilings applied)
        ApplyFeatureCeilings(product);
        if (product.Features != null && product.Features.Length > 0 && _templateLookup.TryGetValue(product.TemplateId, out var recalcTemplate))
        {
            float featureDerivedQuality = RecalculateOverallQuality(product, recalcTemplate);
            if (featureDerivedQuality > 0f)
                overallQuality = featureDerivedQuality;
        }

        // Collect employee IDs from current team assignments before freeing them
        var employeeIds = new List<EmployeeId>();
        foreach (var kvp in product.TeamAssignments)
        {
            var team = _teamSystem?.GetTeam(kvp.Value);
            if (team == null) continue;
            int memberCount = team.members.Count;
            for (int m = 0; m < memberCount; m++)
                employeeIds.Add(team.members[m]);
        }        // Free all team assignments except Marketing (retained for post-ship campaigns and event mitigation)
        TeamId retainedMarketingTeamId = default;
        bool hasMarketing = product.TeamAssignments.TryGetValue(ProductTeamRole.Marketing, out retainedMarketingTeamId);

        foreach (var kvp in product.TeamAssignments)
        {
            if (kvp.Key == ProductTeamRole.Marketing) continue;
            var shippedTeam = _teamSystem?.GetTeam(kvp.Value);
            if (shippedTeam != null && shippedTeam.isCrunching) {
                shippedTeam.isCrunching = false;
                _fatigueSystem?.ResetCrunchTracking(shippedTeam.members);
            }
            _state.teamToProduct.Remove(kvp.Value);
            _teamSystem?.NotifyTeamFreed(kvp.Value);
        }
        product.TeamAssignments.Clear();

        if (hasMarketing)
            product.TeamAssignments[ProductTeamRole.Marketing] = retainedMarketingTeamId;

        // Move to shipped
        _state.developmentProducts.Remove(cmd.ProductId);
        product.IsShipped = true;
        product.ShipTick = _currentTick;
        product.LastStageChangeTick = _currentTick;
        product.IsInDevelopment = false;
        product.LifecycleStage = ProductLifecycleStage.PreLaunch;
        product.OverallQuality = overallQuality;
        product.ProductVersion++;
        product.HypeAtShip = product.HypeScore;
        if (!hasMarketing) product.IsMarketingActive = false;

        if (product.Category.IsTool() || product.Category.IsPlatform())
        {
            product.DistributionModel = cmd.DistributionModel;
            product.PlayerLicensingRate = cmd.LicensingRate;
            product.ActiveLicenseeCount = 0;
            product.TotalLicensingRevenue = 0L;
        }
        if (product.Category.IsTool() && cmd.DistributionModel == ToolDistributionModel.Licensed)
        {
            float subPrice = product.MonthlySubscriptionPrice;
            if (subPrice > 0f)
            {
                product.IsSubscriptionBased = true;
                product.PriceOverride = subPrice;
            }
            product.ActiveSubscriberCount = 0;
        }
        else if (product.Category.IsTool())
        {
            product.ActiveSubscriberCount = 0;
            product.IsSubscriptionBased = false;
        }

        float totalBugs = 0f;
        for (int p = 0; p < phaseCount; p++)
        {
            if (product.Phases[p].phaseType != ProductPhaseType.QA)
                totalBugs += product.Phases[p].bugAccumulation;
        }
        product.BugsRemaining = Math.Min(100f, totalBugs);

        _state.shippedProducts[cmd.ProductId] = product;

        // Compute product identity at ship (must happen before reviews to enable profile adjustment)
        if (_templateLookup.TryGetValue(product.TemplateId, out var identityTemplate)) {
            product.IdentityAtShip = ProductIdentityHelper.ComputeAtShip(
                product, identityTemplate, _generationSystem, _platformSystem, _state, _tuning);
            product.CurrentIdentity = product.IdentityAtShip;
            product.ExpectedSelectedRatioAtShip = ComputeExpectedSelectedRatio(product, identityTemplate);
        }

        // Generate review result at ship time so the modal can display it immediately
        if (_reviewSystem != null && _templateLookup.TryGetValue(product.TemplateId, out var shipReviewTemplate)) {
            float savedQuality = product.OverallQuality;
            product.OverallQuality = ComputeWeightedQuality(product, shipReviewTemplate);
            product.ReviewResult = _reviewSystem.GenerateReviews(product, shipReviewTemplate, product.FeatureRelevanceAtShip,
                product.IdentityAtShip.IsValid ? (ProductIdentitySnapshot?)product.IdentityAtShip : null);
            product.PublicReceptionScore = product.ReviewResult.AggregateScore;
            product.OverallQuality = savedQuality;
        }

        if (product.BugsRemaining > 0f && _templateLookup.TryGetValue(product.TemplateId, out var shipEconTemplate))
        {
            var shipConfig = shipEconTemplate.economyConfig;
            float bugSeverity = Math.Min(1f, product.BugsRemaining / 100f);
            float popularityPenalty = bugSeverity * shipConfig.shipBugPopularityPenalty;
            product.PopularityScore = Math.Max(0f, product.PopularityScore - popularityPenalty);
        }

        // Sequel popularity modifier
        if (product.SequelOfId.HasValue)
        {
            Product original = null;
            if (_state.shippedProducts.TryGetValue(product.SequelOfId.Value, out original) ||
                _state.archivedProducts.TryGetValue(product.SequelOfId.Value, out original))
            {
                if (_templateLookup.TryGetValue(product.TemplateId, out var sequelTemplate) &&
                    sequelTemplate.economyConfig != null)
                {
                    float originalPop = original.PopularityScore;
                    int sequelCount = original.SequelIds != null ? original.SequelIds.Count : 1;
                    float fatigue = 1f / sequelCount;

                    float sentiment = (originalPop - 40f) / 60f * sequelTemplate.economyConfig.sequelPopularityBoostMax * fatigue;
                    float variance = (_rng.NextFloat01() * 2f - 1f) * 15f;
                    float totalModifier = sentiment + variance;

                    product.PopularityScore = Math.Max(0f, Math.Min(100f, product.PopularityScore + totalModifier));
                }
            }
        }

        // Award reputation
        if (_reputationSystem != null)
        {
            float effectiveQuality = _reputationSystem.GetEffectiveQuality(overallQuality);
            int repGain = (int)(effectiveQuality * 0.3f);
            if (repGain > 0) {
                _reputationSystem.AddReputation(repGain, "global");
                if (_templateLookup.TryGetValue(product.TemplateId, out var repTemplate))
                    _reputationSystem.AddReputation(repGain, repTemplate.category.ToString());
            }

            if (overallQuality < 40f) {
                int repPenalty = (int)((40f - overallQuality) * 2f);
                _reputationSystem.RemoveReputation(repPenalty, "global");
                if (_templateLookup.TryGetValue(product.TemplateId, out var penaltyTemplate))
                    _reputationSystem.RemoveReputation(repPenalty, penaltyTemplate.category.ToString());
            }

            _reputationSystem.AdjustSentimentOnShip(overallQuality);
            _reputationSystem.IncrementProductCount();
        }

        // Set up successor lineage migration from predecessor
        SetupSuccessorMigration(product);

        // Award skill XP to all contributing employees
        AwardShipXP(product, employeeIds);

        // Append work history to all contributing employees
        AppendProductWorkHistory(product, overallQuality, WorkOutcome.Completed);

        // Quality release uplift: high-quality releases boost market demand via niche
        if (_marketSystem != null && overallQuality >= 75f)
        {
            float uplift = (overallQuality - 75f) / 25f * MarketSystem.MaxDemandUpliftPerRelease;
            _marketSystem.ApplyNicheUplift(product.Niche, uplift * 0.5f);
        }

        _pendingEvents.Add(new PendingEvent { Type = PendingEventType.ProductShipped, ProductId = cmd.ProductId, FloatA = overallQuality });
        if (employeeIds.Count > 0)
            _pendingEvents.Add(new PendingEvent { Type = PendingEventType.SkillsAwarded, EmployeeIds = employeeIds });

        _logger.Log($"[ProductSystem] Shipped product '{product.ProductName}' (ID: {cmd.ProductId.Value}) — Quality: {overallQuality:F1}%");
    }

    public void InitializeFeatureStates(Product product, ProductTemplateDefinition template, float overallQuality)
    {
        if (product.SelectedFeatureIds == null || product.SelectedFeatureIds.Length == 0) return;

        int featureCount = product.SelectedFeatureIds.Length;

        if (product.Features != null && product.Features.Length == featureCount)
            return;

        var states = new ProductFeatureState[featureCount];
        for (int i = 0; i < featureCount; i++)
        {
            string fid = product.SelectedFeatureIds[i];
            float existingQuality = overallQuality;

            if (template?.availableFeatures != null)
            {
                for (int j = 0; j < template.availableFeatures.Length; j++)
                {
                    if (template.availableFeatures[j]?.featureId == fid)
                    {
                        float devCostMult = template.availableFeatures[j].devCostMultiplier > 0f
                            ? template.availableFeatures[j].devCostMultiplier
                            : 1f;
                        existingQuality = Math.Min(100f, overallQuality / devCostMult);
                        break;
                    }
                }
            }

            states[i] = new ProductFeatureState
            {
                FeatureId = fid,
                Quality = existingQuality,
                TechnicalDebt = 0f,
                LastUpgradeTick = _currentTick,
                IsNew = true
            };
        }

        product.Features = states;
    }

    private void SetupSuccessorMigration(Product successor)
    {
        if (!successor.PredecessorProductId.HasValue) return;

        var predecessorId = successor.PredecessorProductId.Value;
        Product predecessor = null;
        _state.shippedProducts.TryGetValue(predecessorId, out predecessor);
        if (predecessor == null)
            _state.archivedProducts.TryGetValue(predecessorId, out predecessor);
        if (predecessor == null) return;

        int predecessorUsers = predecessor.ActiveUserCount;
        if (predecessorUsers <= 0) return;

        float migrationRate = SuccessorMigrationBaseRate;

        float marketShare = predecessor.PopularityScore / 100f;
        migrationRate = 0.15f + marketShare * 0.25f;

        int generationDiff = successor.ArchitectureGeneration - predecessor.ArchitectureGeneration;
        if (generationDiff > 1)
            migrationRate *= SuccessorMigrationGenerationSkipPenalty;

        int totalUsersToMigrate = (int)(predecessorUsers * migrationRate);
        if (totalUsersToMigrate <= 0) return;

        successor.SuccessorMigrationTicksTotal = SuccessorMigrationDurationTicks;
        successor.SuccessorMigrationTicksElapsed = 0;
        successor.SuccessorMigrationUsersPerTick = Math.Max(1, totalUsersToMigrate / SuccessorMigrationDurationTicks);

        _logger.Log($"[ProductSystem] Successor migration setup: '{successor.ProductName}' will receive ~{totalUsersToMigrate} users from '{predecessor.ProductName}' over {SuccessorMigrationDurationTicks / (TimeState.TicksPerDay * 30)} months. Rate: {migrationRate:P0}");
    }

    private void HandleSpawnShippedCheat(SpawnShippedProductCheatCommand cmd)
    {
        if (!_templateLookup.TryGetValue(cmd.TemplateId, out var template))
        {
            _logger.LogWarning($"[ProductSystem] SpawnShippedCheat failed: template '{cmd.TemplateId}' not found.");
            return;
        }

        var cheatNiche = template.nicheConfigs != null && template.nicheConfigs.Length > 0
            ? template.nicheConfigs[0].niche
            : ProductNiche.None;
        float cheatNicheDevTimeMult = GetNicheDevTimeMultiplier(cheatNiche);
        var phases = BuildPhaseRuntimes(template, cmd.SelectedFeatureIds, _tuning?.ProductBaseWorkMultiplier ?? 100f, cheatNicheDevTimeMult, template.difficultyTier);
        int phaseCount = phases.Length;
        for (int i = 0; i < phaseCount; i++)
        {
            phases[i].isComplete = true;
            phases[i].isUnlocked = true;
            phases[i].workCompleted = phases[i].totalWorkRequired;
            phases[i].phaseQuality = cmd.OverallQuality;
        }

        var product = new Product
        {
            Id = new ProductId(_state.nextProductId++),
            TemplateId = cmd.TemplateId,
            ProductName = cmd.ProductName,
            Category = template.category,
            SelectedFeatureIds = cmd.SelectedFeatureIds,
            IsSubscriptionBased = cmd.IsSubscriptionBased,
            PriceOverride = cmd.Price,
            Phases = phases,
            IsInDevelopment = false,
            IsShipped = true,
            IsOnMarket = false,
            LifecycleStage = ProductLifecycleStage.PreLaunch,
            OverallQuality = cmd.OverallQuality,
            UpfrontCostPaid = 0,
            TeamAssignments = new Dictionary<ProductTeamRole, TeamId>(),
            PivotsUsed = 0,
            MaxPivots = 1,
            DroppedFeatureIds = new List<string>(),
            FeatureRelevanceAtShip = 1f,
            CreationTick = _currentTick,
            ShipTick = _currentTick
        };

        _state.shippedProducts[product.Id] = product;

        _pendingEvents.Add(new PendingEvent { Type = PendingEventType.ProductShipped, ProductId = product.Id, FloatA = cmd.OverallQuality });
        _pendingEvents.Add(new PendingEvent { Type = PendingEventType.ProductCreated, ProductId = product.Id });

        _logger.Log($"[ProductSystem] Cheat: spawned shipped product '{product.ProductName}' (ID: {product.Id.Value}) — Quality: {cmd.OverallQuality:F1}%");
    }

    private void HandleBuildHype(BuildHypeCommand cmd)
    {
        if (!_state.developmentProducts.TryGetValue(cmd.ProductId, out var product) &&
            !_state.shippedProducts.TryGetValue(cmd.ProductId, out product))
        {
            _logger.LogWarning($"[ProductSystem] BuildHype failed: product {cmd.ProductId.Value} not found.");
            return;
        }
        if (!product.IsMarketingActive)
        {
            _logger.LogWarning($"[ProductSystem] BuildHype failed: marketing not active on product {cmd.ProductId.Value}.");
            return;
        }
        if (!HasValidMarketingTeam(product))
        {
            _logger.LogWarning($"[ProductSystem] BuildHype failed: no valid marketing team on product {cmd.ProductId.Value}.");
            return;
        }
        if (cmd.SpendAmount <= 0)
        {
            _logger.LogWarning($"[ProductSystem] BuildHype failed: SpendAmount must be > 0.");
            return;
        }

        int cooldown = _tuning?.HypeCampaignCooldownTicks ?? 33600;
        if ((cmd.Tick - product.LastPaidBoostTick) < cooldown && product.LastPaidBoostTick > 0)
        {
            _logger.LogWarning($"[ProductSystem] BuildHype failed: campaign on cooldown for product {cmd.ProductId.Value}.");
            return;
        }

        if (_financeSystem == null || _financeSystem.Money < cmd.SpendAmount)
        {
            _logger.LogWarning($"[ProductSystem] BuildHype failed: cannot afford ${cmd.SpendAmount}.");
            return;
        }

        if (!_financeSystem.TrySubtractMoney(cmd.SpendAmount, out string error))
        {
            _logger.LogWarning($"[ProductSystem] BuildHype failed to deduct money: {error}");
            return;
        }

        int baseCost = _tuning?.HypeCampaignBaseCost ?? 500;
        float spendMult = (float)cmd.SpendAmount / Math.Max(1, baseCost);
        float skillMult = GetMarketingSkillMult(product);
        float tierMult = GetHypeTierMult();
        float diminishing = 1f / (1f + product.HypeScore * (_tuning?.HypeDiminishingFactor ?? 0.02f));
        float baseGain = _tuning?.HypeCampaignBaseGain ?? 5.0f;
        float gain = baseGain * spendMult * skillMult * tierMult * diminishing;
        product.HypeScore = Math.Min(100f, product.HypeScore + gain);

        product.TotalMarketingSpend += cmd.SpendAmount;
        product.PaidBoostCount++;
        product.LastPaidBoostTick = cmd.Tick;

        if (product.HypeScore > product.PeakHype)
        {
            product.PeakHype = product.HypeScore;
            product.PeakHypeTick = cmd.Tick;
        }

        _pendingEvents.Add(new PendingEvent { Type = PendingEventType.HypeChanged, ProductId = cmd.ProductId, FloatA = product.HypeScore, IntA = product.TotalMarketingSpend });
        _logger.Log($"[ProductSystem] Paid hype boost on product {cmd.ProductId.Value}: +{gain:F1} hype (now {product.HypeScore:F1}), spent ${cmd.SpendAmount}");
    }

    private void ProcessDailyHype(Product product)
    {
        float skillMult = GetMarketingSkillMult(product);
        float tierMult = GetHypeTierMult();
        float diminishing = 1f / (1f + product.HypeScore * (_tuning?.HypeDiminishingFactor ?? 0.02f));
        float baseGain = _tuning?.HypePassiveGainPerDay ?? 0.8f;
        float budgetRef = _tuning?.HypeBudgetReferenceCost ?? 5000f;
        float budgetMult;
        if (product.MarketingBudgetMonthly > 0)
        {
            budgetMult = (float)(Math.Log10(product.MarketingBudgetMonthly + 1) / Math.Log10(budgetRef + 1));
            if (budgetMult < 0.1f) budgetMult = 0.1f;
        }
        else
        {
            budgetMult = 0f;
        }
        float dailyGain = baseGain * skillMult * tierMult * diminishing * budgetMult;

        int daysSincePeak = (_currentTick - product.PeakHypeTick) / TimeState.TicksPerDay;
        int graceDays = _tuning?.HypeDecayGracePeriodDays ?? 30;
        float dailyDecay = 0f;
        if (daysSincePeak > graceDays && product.PeakHype > 0f)
        {
            int excessDays = daysSincePeak - graceDays;
            float rampDays = _tuning?.HypeDecayRampDays ?? 30f;
            float maxDecay = _tuning?.HypeDecayMaxPerDay ?? 1.5f;
            float baseDecay = _tuning?.HypeDecayPerDay ?? 0.3f;
            dailyDecay = baseDecay * (excessDays / rampDays);
            if (dailyDecay > maxDecay) dailyDecay = maxDecay;
        }

        float netChange = dailyGain - dailyDecay;
        product.HypeScore += netChange;
        if (product.HypeScore < 0f) product.HypeScore = 0f;
        if (product.HypeScore > 100f) product.HypeScore = 100f;

        if (product.HypeScore > product.PeakHype)
        {
            product.PeakHype = product.HypeScore;
            product.PeakHypeTick = _currentTick;
        }
    }

    private bool HasValidMarketingTeam(Product product)
    {
        if (!product.TeamAssignments.TryGetValue(ProductTeamRole.Marketing, out var teamId)) return false;
        if (_teamSystem == null) return false;
        return _teamSystem.GetTeamType(teamId) == TeamType.Marketing;
    }

    private void HandleRunAds(RunAdsCommand cmd)
    {
        if (!_state.shippedProducts.TryGetValue(cmd.ProductId, out var product))
        {
            _logger.LogWarning($"[ProductSystem] RunAds failed: product {cmd.ProductId.Value} not found in shipped products.");
            return;
        }
        if (!product.IsOnMarket)
        {
            _logger.LogWarning($"[ProductSystem] RunAds failed: product {cmd.ProductId.Value} is not on market.");
            return;
        }
        if (!HasValidMarketingTeam(product))
        {
            _logger.LogWarning($"[ProductSystem] RunAds failed: no marketing team assigned to product {cmd.ProductId.Value}.");
            return;
        }
        if (product.IsRunningAds)
        {
            _logger.LogWarning($"[ProductSystem] RunAds failed: product {cmd.ProductId.Value} already running ads.");
            return;
        }
        int adCooldown = _tuning?.AdCooldownTicks ?? 144000;
        if ((cmd.Tick - product.LastAdTick) < adCooldown && product.LastAdTick > 0)
        {
            _logger.LogWarning($"[ProductSystem] RunAds failed: product {cmd.ProductId.Value} ad campaign still on cooldown.");
            return;
        }
        if (cmd.SpendAmount <= 0)
        {
            _logger.LogWarning($"[ProductSystem] RunAds failed: SpendAmount must be > 0.");
            return;
        }
        if (_financeSystem == null || _financeSystem.Money < cmd.SpendAmount)
        {
            _logger.LogWarning($"[ProductSystem] RunAds failed: cannot afford ${cmd.SpendAmount}.");
            return;
        }

        if (!_financeSystem.TrySubtractMoney(cmd.SpendAmount, out string error))
        {
            _logger.LogWarning($"[ProductSystem] RunAds failed to deduct money: {error}");
            return;
        }

        product.IsRunningAds = true;
        product.AdTicksRemaining = _tuning?.AdDurationTicks ?? 67200;
        product.LastAdTick = cmd.Tick;
        product.TotalAdSpend += cmd.SpendAmount;

        _pendingEvents.Add(new PendingEvent { Type = PendingEventType.AdRunStarted, ProductId = product.Id, IntA = cmd.SpendAmount });
        _logger.Log($"[ProductSystem] Ad campaign started on product {cmd.ProductId.Value}, spend: ${cmd.SpendAmount}");
    }

    private void HandleAnnounceUpdate(AnnounceUpdateCommand cmd)
    {
        if (!_state.shippedProducts.TryGetValue(cmd.ProductId, out var product))
        {
            _logger.LogWarning($"[ProductSystem] AnnounceUpdate failed: product {cmd.ProductId.Value} not found in shipped products.");
            return;
        }
        if (!product.IsOnMarket)
        {
            _logger.LogWarning($"[ProductSystem] AnnounceUpdate failed: product {cmd.ProductId.Value} is not on market.");
            return;
        }
        if (!HasValidMarketingTeam(product))
        {
            _logger.LogWarning($"[ProductSystem] AnnounceUpdate failed: no marketing team assigned to product {cmd.ProductId.Value}.");
            return;
        }
        if (product.CurrentUpdate == null || !product.CurrentUpdate.isUpdating)
        {
            _logger.LogWarning($"[ProductSystem] AnnounceUpdate failed: product {cmd.ProductId.Value} has no update in progress.");
            return;
        }
        if (product.HasAnnouncedUpdate)
        {
            _logger.LogWarning($"[ProductSystem] AnnounceUpdate failed: product {cmd.ProductId.Value} update already announced.");
            return;
        }

        product.HasAnnouncedUpdate = true;
        product.UpdateAnnounceTick = cmd.Tick;
        product.UpdateHype = 0f;

        _pendingEvents.Add(new PendingEvent { Type = PendingEventType.UpdateAnnounced, ProductId = product.Id });
        _logger.Log($"[ProductSystem] Update announced for product {cmd.ProductId.Value}");
    }

    private void AwardMarketingXPForProduct(ProductId productId, TeamId marketingTeamId, float xpAmount)
    {
        if (_teamSystem == null || _employeeSystem == null || _roleProfileTable == null) return;
        var team = _teamSystem.GetTeam(marketingTeamId);
        if (team == null) return;
        SkillGrowthSystem.AwardMarketingXP(team, _employeeSystem, xpAmount, _rng, _roleProfileTable, _abilitySystem, _tuning);
    }

    private void ProcessHypeEventRoll(Product product, bool isPreLaunch)
    {
        bool hasTeam = HasValidMarketingTeam(product);
        float skillMult = hasTeam ? GetMarketingSkillMult(product) : 0f;

        var result = MarketingEventProcessor.TryRollEvent(
            product, product.Category, hasTeam, skillMult,
            _currentTick, _rng, _tuning);

        if (!result.EventOccurred) return;

        product.LastHypeEventTick = _currentTick;

        if (isPreLaunch)
        {
            float newHype = Math.Max(0f, Math.Min(100f, product.HypeScore + result.HypeChange));
            product.HypeScore = newHype;
            if (product.HypeScore > product.PeakHype)
            {
                product.PeakHype = product.HypeScore;
                product.PeakHypeTick = _currentTick;
            }
        }
        else
        {
            product.PopularityScore = Math.Max(0f, Math.Min(100f, product.PopularityScore + result.PopularityChange));
            product.ActiveUserCount = Math.Max(0, product.ActiveUserCount + result.UserChange);
        }

        if (result.SentimentChange != 0f)
            _reputationSystem?.AdjustSentimentDelta(result.SentimentChange);

        if (result.ReputationChange > 0f)
            _reputationSystem?.AddReputation((int)result.ReputationChange, "global");
        else if (result.ReputationChange < 0f)
            _reputationSystem?.RemoveReputation((int)Math.Abs(result.ReputationChange), "global");

        _inboxSystem?.AddNewsArticle(
            result.EventType, product.Id, product.ProductName,
            result.Headline, result.Body, _currentTick, result.WasMitigated);

        _pendingEvents.Add(new PendingEvent { Type = PendingEventType.HypeEvent, ProductId = product.Id, HypeEventType = result.EventType, BoolA = result.WasMitigated });        if (result.WasMitigated && hasTeam && product.TeamAssignments.TryGetValue(ProductTeamRole.Marketing, out var mktTeamId))
            AwardMarketingXPForProduct(product.Id, mktTeamId, _tuning?.MarketingXPPerEventMitigation ?? 0.3f);
    }

    private float GetMarketingSkillMult(Product product)
    {
        if (!product.TeamAssignments.TryGetValue(ProductTeamRole.Marketing, out var teamId)) return 0f;
        if (_teamSystem == null) return 1f;
        var team = _teamSystem.GetTeam(teamId);
        if (team == null || team.members == null || team.members.Count == 0) return 0.5f;

        int assignedProductCount = CountMarketingAssignments(teamId);

        int totalMarketingSkill = 0;
        int memberCount = team.members.Count;
        for (int i = 0; i < memberCount; i++)
        {
            var emp = _employeeSystem?.GetEmployee(team.members[i]);
            if (emp == null) continue;
            totalMarketingSkill += emp.GetSkill(SkillId.Marketing);
        }

        float effectivePower = (float)totalMarketingSkill / Math.Max(1, assignedProductCount);
        float avgSkill = effectivePower / memberCount;
        return avgSkill / 10f;
    }

    private int CountMarketingAssignments(TeamId marketingTeamId)
    {
        int count = 0;
        foreach (var kvp in _state.developmentProducts)
        {
            if (kvp.Value.TeamAssignments.TryGetValue(ProductTeamRole.Marketing, out var tid) && tid == marketingTeamId)
                count++;
        }
        foreach (var kvp in _state.shippedProducts)
        {
            if (kvp.Value.TeamAssignments.TryGetValue(ProductTeamRole.Marketing, out var tid) && tid == marketingTeamId)
                count++;
        }
        return Math.Max(1, count);
    }

    private float GetHypeTierMult()
    {
        if (_reputationSystem == null || _tuning == null) return 1f;
        int tierIdx = (int)_reputationSystem.CurrentTier;
        var mults = _tuning.HypeTierEfficiencyMults;
        if (mults == null || tierIdx >= mults.Length) return 1f;
        return mults[tierIdx];
    }

    public void OnMarketingTeamRemoved(TeamId teamId)
    {
        foreach (var kvp in _state.developmentProducts)
        {
            if (kvp.Value.TeamAssignments.TryGetValue(ProductTeamRole.Marketing, out var tid) && tid == teamId)
            {
                kvp.Value.IsMarketingActive = false;
                kvp.Value.TeamAssignments.Remove(ProductTeamRole.Marketing);
            }
        }
        foreach (var kvp in _state.shippedProducts)
        {
            if (kvp.Value.TeamAssignments.TryGetValue(ProductTeamRole.Marketing, out var tid) && tid == teamId)
            {
                kvp.Value.IsRunningAds = false;
                kvp.Value.AdTicksRemaining = 0;
                kvp.Value.HasAnnouncedUpdate = false;
                kvp.Value.TeamAssignments.Remove(ProductTeamRole.Marketing);
            }
        }
    }

    private void OnTeamDeleted(TeamId teamId)
    {
        if (!_state.teamToProduct.TryGetValue(teamId, out var productId))
            return;

        if (_state.developmentProducts.TryGetValue(productId, out var product))
        {
            ProductTeamRole foundRole = default;
            bool found = false;
            foreach (var kvp in product.TeamAssignments)
            {
                if (kvp.Value == teamId)
                {
                    foundRole = kvp.Key;
                    found = true;
                    break;
                }
            }
            if (found)
                product.TeamAssignments.Remove(foundRole);
        }

        _state.teamToProduct.Remove(teamId);
        _logger.LogWarning($"[ProductSystem] Team {teamId.Value} was deleted while assigned to product {productId.Value} — slot cleared.");
    }

    private void HandlePivotFeature(PivotFeatureCommand cmd)
    {
        if (!_state.developmentProducts.TryGetValue(cmd.ProductId, out var product))
        {
            _logger.LogWarning($"[ProductSystem] PivotFeature failed: product {cmd.ProductId.Value} not found.");
            return;
        }

        if (product.PivotsUsed >= product.MaxPivots)
        {
            _logger.LogWarning($"[ProductSystem] PivotFeature failed: product {cmd.ProductId.Value} has used all {product.MaxPivots} pivot(s).");
            return;
        }

        // Block pivoting in the final phase
        if (product.Phases != null && product.Phases.Length > 0)
        {
            var finalPhase = product.Phases[product.Phases.Length - 1];
            if (finalPhase.isUnlocked && !finalPhase.isComplete)
            {
                _logger.LogWarning($"[ProductSystem] PivotFeature failed: cannot pivot during the final phase.");
                return;
            }
        }

        if (!_templateLookup.TryGetValue(product.TemplateId, out var template))
        {
            _logger.LogWarning($"[ProductSystem] PivotFeature failed: template '{product.TemplateId}' not found.");
            return;
        }

        string removedId = null;
        string addedId = null;

        if (cmd.Action == PivotAction.Drop || cmd.Action == PivotAction.Swap)
        {
            if (string.IsNullOrEmpty(cmd.RemoveFeatureId))
            {
                _logger.LogWarning($"[ProductSystem] PivotFeature Drop/Swap failed: no RemoveFeatureId specified.");
                return;
            }
            ProductFeatureDefinition removeFeat = FindFeatureDef(template, cmd.RemoveFeatureId);
            if (removeFeat == null)
            {
                _logger.LogWarning($"[ProductSystem] PivotFeature failed: feature '{cmd.RemoveFeatureId}' not found in template.");
                return;
            }

            // Remove from selected list
            RemoveFeatureId(product, cmd.RemoveFeatureId);
            product.DroppedFeatureIds.Add(cmd.RemoveFeatureId);
            removedId = cmd.RemoveFeatureId;
        }

        if (cmd.Action == PivotAction.Add || cmd.Action == PivotAction.Swap)
        {
            if (string.IsNullOrEmpty(cmd.AddFeatureId))
            {
                _logger.LogWarning($"[ProductSystem] PivotFeature Add/Swap failed: no AddFeatureId specified.");
                return;
            }
            ProductFeatureDefinition addFeat = FindFeatureDef(template, cmd.AddFeatureId);
            if (addFeat == null)
            {
                _logger.LogWarning($"[ProductSystem] PivotFeature failed: feature '{cmd.AddFeatureId}' not found in template.");
                return;
            }

            // Add to selected list
            var newFeatureIds = new string[product.SelectedFeatureIds.Length + 1];
            for (int i = 0; i < product.SelectedFeatureIds.Length; i++)
                newFeatureIds[i] = product.SelectedFeatureIds[i];
            newFeatureIds[product.SelectedFeatureIds.Length] = cmd.AddFeatureId;
            product.SelectedFeatureIds = newFeatureIds;
            addedId = cmd.AddFeatureId;
        }

        product.PivotsUsed++;

        _pendingEvents.Add(new PendingEvent { Type = PendingEventType.FeaturePivoted, ProductId = cmd.ProductId, PivotAction = cmd.Action, StringA = removedId, StringB = addedId });
        _logger.Log($"[ProductSystem] Pivot on product {cmd.ProductId.Value}: {cmd.Action}, removed='{removedId}', added='{addedId}'");
    }

    private static ProductFeatureDefinition FindFeatureDef(ProductTemplateDefinition template, string featureId)
    {
        if (template.availableFeatures == null) return null;
        for (int i = 0; i < template.availableFeatures.Length; i++)
        {
            if (template.availableFeatures[i]?.featureId == featureId)
                return template.availableFeatures[i];
        }
        return null;
    }

    private static void RemoveFeatureId(Product product, string featureId)
    {
        int idx = -1;
        for (int i = 0; i < product.SelectedFeatureIds.Length; i++)
        {
            if (product.SelectedFeatureIds[i] == featureId) { idx = i; break; }
        }
        if (idx < 0) return;
        var updated = new string[product.SelectedFeatureIds.Length - 1];
        for (int i = 0, j = 0; i < product.SelectedFeatureIds.Length; i++)
        {
            if (i == idx) continue;
            updated[j++] = product.SelectedFeatureIds[i];
        }
        product.SelectedFeatureIds = updated;
    }

    private void HandleAbandonProduct(AbandonProductCommand cmd)
    {
        if (!_state.developmentProducts.TryGetValue(cmd.ProductId, out var product))
        {
            _logger.LogWarning($"[ProductSystem] AbandonProduct failed: product {cmd.ProductId.Value} not found.");
            return;
        }

        foreach (var kvp in product.TeamAssignments)
        {
            _state.teamToProduct.Remove(kvp.Value);
            _teamSystem?.NotifyTeamFreed(kvp.Value);
        }
        product.TeamAssignments.Clear();

        _state.developmentProducts.Remove(cmd.ProductId);

        _pendingEvents.Add(new PendingEvent { Type = PendingEventType.ProductAbandoned, ProductId = cmd.ProductId });
        _logger.Log($"[ProductSystem] Abandoned product '{product.ProductName}' (ID: {cmd.ProductId.Value})");
    }

    private void HandleAnnounceReleaseDate(AnnounceReleaseDateCommand cmd)
    {
        if (!_state.developmentProducts.TryGetValue(cmd.ProductId, out var product))
        {
            _logger.LogWarning($"[ProductSystem] AnnounceReleaseDate failed: product {cmd.ProductId.Value} not found.");
            return;
        }

        if (product.HasAnnouncedReleaseDate)
        {
            _logger.LogWarning($"[ProductSystem] AnnounceReleaseDate ignored: product {cmd.ProductId.Value} already has a release date. Use ChangeReleaseDateCommand to shift it.");
            return;
        }

        int targetTick = cmd.TargetDay * TimeState.TicksPerDay;
        product.TargetReleaseTick = targetTick;
        product.OriginalReleaseTick = targetTick;
        product.HasAnnouncedReleaseDate = true;

        _pendingEvents.Add(new PendingEvent { Type = PendingEventType.ReleaseDateAnnounced, ProductId = cmd.ProductId, IntA = targetTick });
        _logger.Log($"[ProductSystem] Release date announced for product {cmd.ProductId.Value}: day {cmd.TargetDay} (tick {targetTick})");
    }

    private void HandleChangeReleaseDate(ChangeReleaseDateCommand cmd)
    {
        if (!_state.developmentProducts.TryGetValue(cmd.ProductId, out var product))
        {
            _logger.LogWarning($"[ProductSystem] ChangeReleaseDate failed: product {cmd.ProductId.Value} not found.");
            return;
        }

        if (!product.HasAnnouncedReleaseDate)
        {
            _logger.LogWarning($"[ProductSystem] ChangeReleaseDate failed: product {cmd.ProductId.Value} has no announced release date. Use AnnounceReleaseDateCommand first.");
            return;
        }

        int oldTick = product.TargetReleaseTick;
        int newTick = cmd.NewTargetDay * TimeState.TicksPerDay;
        product.TargetReleaseTick = newTick;
        product.DateShiftCount++;
        bool isRush = newTick < oldTick;

        if (isRush)
        {
            float originalSpan = product.OriginalReleaseTick - product.CreationTick;
            if (originalSpan > 0f && _financeSystem != null)
            {
                float rushFraction = (float)(oldTick - newTick) / originalSpan;
                long rushCost = (long)(product.TotalProductionCost * rushFraction * (_tuning?.RushCostScale ?? 1.0f));
                if (rushCost > 0)
                    _financeSystem.RecordTransaction(-(int)Math.Min(rushCost, int.MaxValue), FinanceCategory.MiscExpense, _currentTick, "rush-penalty");
            }

            float rushSentimentLoss = (_tuning?.RushFanSentimentPenaltyBase ?? 5.0f) * (product.HypeScore / 100f);
            if (rushSentimentLoss > 0f)
                _reputationSystem?.AdjustSentimentDelta(-rushSentimentLoss);
        }
        else
        {
            int repLoss = (int)((_tuning?.DelayRepLossBase ?? 3.0f) * Math.Pow(_tuning?.DelayRepLossCompounding ?? 1.5f, product.DateShiftCount - 1));
            if (repLoss > 0)
                _reputationSystem?.RemoveReputation(repLoss, "global");

            float sentimentLoss = (_tuning?.DelayFanTrustErosionScale ?? 0.5f) * (product.HypeScore / 100f) * product.DateShiftCount;
            if (sentimentLoss > 0f)
                _reputationSystem?.AdjustSentimentDelta(-sentimentLoss);
        }

        _pendingEvents.Add(new PendingEvent
        {
            Type = PendingEventType.ReleaseDateChanged,
            ProductId = cmd.ProductId,
            IntA = oldTick,
            IntB = newTick,
            BoolA = isRush,
            FloatA = product.DateShiftCount
        });
        _logger.Log($"[ProductSystem] Release date changed for product {cmd.ProductId.Value}: day {cmd.NewTargetDay} (tick {newTick}), rush={isRush}, shifts={product.DateShiftCount}");
    }

    private void AutoShipProduct(Product product)
    {
        if (!_state.developmentProducts.ContainsKey(product.Id)) return;

        int phaseCount = product.Phases.Length;
        float totalWork = 0f;
        for (int p = 0; p < phaseCount; p++)
            totalWork += product.Phases[p].totalWorkRequired;

        float overallQuality = 0f;
        if (totalWork > 0f)
        {
            for (int p = 0; p < phaseCount; p++)
            {
                float weight = product.Phases[p].totalWorkRequired / totalWork;
                float phaseQuality = product.Phases[p].isComplete
                    ? product.Phases[p].phaseQuality
                    : product.Phases[p].phaseQuality * (product.Phases[p].totalWorkRequired > 0f
                        ? Math.Min(1f, product.Phases[p].workCompleted / product.Phases[p].totalWorkRequired)
                        : 0f);
                overallQuality += phaseQuality * weight;
            }
        }

        overallQuality = Math.Max(0f, Math.Min(100f, overallQuality));

        if (_templateLookup.TryGetValue(product.TemplateId, out var shipTemplate) &&
            shipTemplate.availableFeatures != null &&
            product.SelectedFeatureIds != null && product.SelectedFeatureIds.Length > 1)
        {
            float qualityMod = 0f;
            int featureCount = product.SelectedFeatureIds.Length;
            for (int fi = 0; fi < featureCount; fi++)
            {
                var featDef = FindFeatureDef(shipTemplate, product.SelectedFeatureIds[fi]);
                if (featDef == null) continue;

                if (featDef.synergyFeatureIds != null)
                {
                    for (int si = 0; si < featDef.synergyFeatureIds.Length; si++)
                    {
                        string synId = featDef.synergyFeatureIds[si];
                        for (int fj = fi + 1; fj < featureCount; fj++)
                        {
                            if (product.SelectedFeatureIds[fj] == synId)
                            {
                                qualityMod += featDef.synergyBonusPercent;
                                break;
                            }
                        }
                    }
                }

                if (featDef.conflictFeatureIds != null)
                {
                    for (int ci = 0; ci < featDef.conflictFeatureIds.Length; ci++)
                    {
                        string confId = featDef.conflictFeatureIds[ci];
                        for (int fj = fi + 1; fj < featureCount; fj++)
                        {
                            if (product.SelectedFeatureIds[fj] == confId)
                            {
                                qualityMod -= featDef.conflictPenaltyPercent;
                                break;
                            }
                        }
                    }
                }
            }
            if (qualityMod != 0f)
                overallQuality = Math.Max(0f, Math.Min(100f, overallQuality * (1f + qualityMod)));
        }

        product.FeatureRelevanceAtShip = ComputeFeatureRelevanceAtShip(product);

        InitializeFeatureStates(product, _templateLookup.TryGetValue(product.TemplateId, out var initTemplate) ? initTemplate : null, overallQuality);
        ApplyFeatureCeilings(product);
        if (product.Features != null && product.Features.Length > 0 && _templateLookup.TryGetValue(product.TemplateId, out var recalcTemplate))
        {
            float featureDerivedQuality = RecalculateOverallQuality(product, recalcTemplate);
            if (featureDerivedQuality > 0f)
                overallQuality = featureDerivedQuality;
        }

        var employeeIds = new List<EmployeeId>();
        foreach (var kvp in product.TeamAssignments)
        {
            var team = _teamSystem?.GetTeam(kvp.Value);
            if (team == null) continue;
            int memberCount = team.members.Count;
            for (int m = 0; m < memberCount; m++)
                employeeIds.Add(team.members[m]);
        }

        TeamId retainedMarketingTeamId = default;
        bool hasMarketing = product.TeamAssignments.TryGetValue(ProductTeamRole.Marketing, out retainedMarketingTeamId);

        foreach (var kvp in product.TeamAssignments)
        {
            if (kvp.Key == ProductTeamRole.Marketing) continue;
            var shippedTeam = _teamSystem?.GetTeam(kvp.Value);
            if (shippedTeam != null && shippedTeam.isCrunching)
            {
                shippedTeam.isCrunching = false;
                _fatigueSystem?.ResetCrunchTracking(shippedTeam.members);
            }
            _state.teamToProduct.Remove(kvp.Value);
            _teamSystem?.NotifyTeamFreed(kvp.Value);
        }
        product.TeamAssignments.Clear();

        if (hasMarketing)
            product.TeamAssignments[ProductTeamRole.Marketing] = retainedMarketingTeamId;

        _state.developmentProducts.Remove(product.Id);
        product.IsShipped = true;
        product.ShipTick = _currentTick;
        product.LastStageChangeTick = _currentTick;
        product.IsInDevelopment = false;
        product.LifecycleStage = ProductLifecycleStage.PreLaunch;
        product.OverallQuality = overallQuality;
        product.ProductVersion++;
        product.HypeAtShip = product.HypeScore;
        if (!hasMarketing) product.IsMarketingActive = false;

        float totalBugs = 0f;
        for (int p = 0; p < phaseCount; p++)
        {
            if (product.Phases[p].phaseType != ProductPhaseType.QA)
                totalBugs += product.Phases[p].bugAccumulation;
        }
        product.BugsRemaining = Math.Min(100f, totalBugs);

        _state.shippedProducts[product.Id] = product;

        if (product.BugsRemaining > 0f && _templateLookup.TryGetValue(product.TemplateId, out var shipEconTemplate))
        {
            var shipConfig = shipEconTemplate.economyConfig;
            float bugSeverity = Math.Min(1f, product.BugsRemaining / 100f);
            float popularityPenalty = bugSeverity * shipConfig.shipBugPopularityPenalty;
            product.PopularityScore = Math.Max(0f, product.PopularityScore - popularityPenalty);
        }

        if (product.SequelOfId.HasValue)
        {
            Product original = null;
            if (_state.shippedProducts.TryGetValue(product.SequelOfId.Value, out original) ||
                _state.archivedProducts.TryGetValue(product.SequelOfId.Value, out original))
            {
                if (_templateLookup.TryGetValue(product.TemplateId, out var sequelTemplate) &&
                    sequelTemplate.economyConfig != null)
                {
                    float originalPop = original.PopularityScore;
                    int sequelCount = original.SequelIds != null ? original.SequelIds.Count : 1;
                    float fatigue = 1f / sequelCount;
                    float sentiment = (originalPop - 40f) / 60f * sequelTemplate.economyConfig.sequelPopularityBoostMax * fatigue;
                    float variance = (_rng.NextFloat01() * 2f - 1f) * 15f;
                    product.PopularityScore = Math.Max(0f, Math.Min(100f, product.PopularityScore + sentiment + variance));
                }
            }
        }

        if (!product.IsCompetitorProduct && _reputationSystem != null)
        {
            float effectiveQuality = _reputationSystem.GetEffectiveQuality(overallQuality);
            int repGain = (int)(effectiveQuality * 0.3f);
            if (repGain > 0)
            {
                _reputationSystem.AddReputation(repGain, "global");
                if (_templateLookup.TryGetValue(product.TemplateId, out var repTemplate))
                    _reputationSystem.AddReputation(repGain, repTemplate.category.ToString());
            }

            if (overallQuality < 40f)
            {
                int repPenalty = (int)((40f - overallQuality) * 2f);
                _reputationSystem.RemoveReputation(repPenalty, "global");
                if (_templateLookup.TryGetValue(product.TemplateId, out var penaltyTemplate))
                    _reputationSystem.RemoveReputation(repPenalty, penaltyTemplate.category.ToString());
            }

            _reputationSystem.AdjustSentimentOnShip(overallQuality);
            _reputationSystem.IncrementProductCount();
        }

        if (!product.IsCompetitorProduct) SetupSuccessorMigration(product);
        if (!product.IsCompetitorProduct) AwardShipXP(product, employeeIds);
        if (!product.IsCompetitorProduct) AppendProductWorkHistory(product, overallQuality, WorkOutcome.Completed);

        if (_marketSystem != null && overallQuality >= 75f)
        {
            float uplift = (overallQuality - 75f) / 25f * MarketSystem.MaxDemandUpliftPerRelease;
            _marketSystem.ApplyNicheUplift(product.Niche, uplift * 0.5f);
        }

        _pendingEvents.Add(new PendingEvent { Type = PendingEventType.ProductShipped, ProductId = product.Id, FloatA = overallQuality });
        if (!product.IsCompetitorProduct && employeeIds.Count > 0)
            _pendingEvents.Add(new PendingEvent { Type = PendingEventType.SkillsAwarded, EmployeeIds = employeeIds });

        _logger.Log($"[ProductSystem] Auto-shipped product '{product.ProductName}' (ID: {product.Id.Value}) at deadline — Quality: {overallQuality:F1}%");
    }

    private void HandleTriggerProductUpdate(TriggerProductUpdateCommand cmd)
    {
        if (!_state.shippedProducts.TryGetValue(cmd.ProductId, out var product))
        {
            _logger.LogWarning($"[ProductSystem] TriggerProductUpdate failed: product {cmd.ProductId.Value} not found in shipped products.");
            return;
        }
        if (!product.IsOnMarket)
        {
            _logger.LogWarning($"[ProductSystem] TriggerProductUpdate failed: product {cmd.ProductId.Value} is not on market.");
            return;
        }

        if (product.CurrentUpdate == null)
            product.CurrentUpdate = new ProductUpdateRuntime();

        if (product.CurrentUpdate.isUpdating)
        {
            _logger.LogWarning($"[ProductSystem] TriggerProductUpdate failed: product {cmd.ProductId.Value} already has an update in progress.");
            return;
        }

        if (cmd.UpdateType == ProductUpdateType.RemoveFeature)
        {
            ProcessRemoveFeature(product, cmd.FeatureIds);
            return;
        }

        float updateWorkRequired = 0f;
        switch (cmd.UpdateType)
        {
            case ProductUpdateType.BugFix:
                updateWorkRequired = Math.Max(250f, product.BugsRemaining * 500f);
                break;

            case ProductUpdateType.AddFeatures:
                float baseAddWork = 500f;
                int existingFeatureCount = product.SelectedFeatureIds != null ? product.SelectedFeatureIds.Length : 0;
                float featureCountTax = 1f + existingFeatureCount * 0.05f;
                updateWorkRequired = baseAddWork * featureCountTax;
                break;

            case ProductUpdateType.UpgradeFeature:
                if (cmd.FeatureIds != null && cmd.FeatureIds.Length > 0 && product.Features != null)
                {
                    float totalUpgradeWork = 0f;
                    for (int fIdx = 0; fIdx < cmd.FeatureIds.Length; fIdx++)
                    {
                        string upgradeId = cmd.FeatureIds[fIdx];
                        for (int fsi = 0; fsi < product.Features.Length; fsi++)
                        {
                            if (product.Features[fsi]?.FeatureId == upgradeId)
                            {
                                float currentQuality = product.Features[fsi].Quality;
                                float qualityCurve = (currentQuality / 100f) * (currentQuality / 100f);
                                totalUpgradeWork += 500f * (0.2f + qualityCurve * 0.8f);
                                break;
                            }
                        }
                    }
                    updateWorkRequired = totalUpgradeWork > 0f ? totalUpgradeWork : 500f;
                }
                else
                {
                    updateWorkRequired = 500f;
                }
                break;
        }

        if (cmd.TeamAssignments != null)
        {
            for (int i = 0; i < cmd.TeamAssignments.Length; i++)
            {
                var ta = cmd.TeamAssignments[i];
                product.TeamAssignments[ta.Role] = ta.TeamId;
                _state.teamToProduct[ta.TeamId] = cmd.ProductId;
            }
        }

        product.CurrentUpdate.isUpdating = true;
        product.CurrentUpdate.updateType = cmd.UpdateType;
        product.CurrentUpdate.targetFeatureIds = cmd.FeatureIds;
        product.CurrentUpdate.updateWorkRequired = updateWorkRequired;
        product.CurrentUpdate.updateWorkCompleted = 0f;

        _pendingEvents.Add(new PendingEvent { Type = PendingEventType.LogUpdateEnd, ProductId = cmd.ProductId, UpdateType = cmd.UpdateType, FloatA = updateWorkRequired });
    }

    private void ProcessRemoveFeature(Product product, string[] featureIds)
    {
        if (featureIds == null || featureIds.Length == 0) return;
        if (product.SelectedFeatureIds == null) return;

        var featureList = new List<string>(product.SelectedFeatureIds);
        for (int i = 0; i < featureIds.Length; i++)
            featureList.Remove(featureIds[i]);
        product.SelectedFeatureIds = featureList.ToArray();

        product.PopularityScore = Math.Min(100f, product.PopularityScore + 1f);
        product.UpdateCount++;

        _logger.Log($"[ProductSystem] Removed {featureIds.Length} feature(s) from product {product.Id.Value}");
    }

    private void HandleRemoveProductFromMarket(RemoveProductFromMarketCommand cmd)
    {
        if (!_state.shippedProducts.TryGetValue(cmd.ProductId, out var product))
        {
            _logger.LogWarning($"[ProductSystem] RemoveProductFromMarket failed: product {cmd.ProductId.Value} not found.");
            return;
        }

        if (product.Category.IsCriticalCategory() && _state.IsLastOnMarketInCategory(cmd.ProductId))
        {
            _logger.LogWarning($"[ProductSystem] Cannot remove last {product.Category} product '{product.ProductName}' from market.");
            return;
        }

        product.IsOnMarket = false;

        // Free all team assignments
        foreach (var kvp in product.TeamAssignments)
            _state.teamToProduct.Remove(kvp.Value);
        product.TeamAssignments.Clear();

        _state.shippedProducts.Remove(cmd.ProductId);
        _state.archivedProducts[cmd.ProductId] = product;

        _pendingEvents.Add(new PendingEvent { Type = PendingEventType.LogRemoveFromMarket, ProductId = cmd.ProductId });
        _logger.Log($"[ProductSystem] Product '{product.ProductName}' (ID: {cmd.ProductId.Value}) removed from market.");
    }

    private void HandleSetProductBudget(SetProductBudgetCommand cmd)
    {
        if (cmd.MonthlyAllocation < 0) return;

        Product product = null;
        bool isShipped = _state.shippedProducts.TryGetValue(cmd.ProductId, out product);
        bool isDev = !isShipped && _state.developmentProducts.TryGetValue(cmd.ProductId, out product);

        if (!isShipped && !isDev)
        {
            _logger.LogWarning($"[ProductSystem] SetProductBudget failed: product {cmd.ProductId.Value} not found.");
            return;
        }

        if (cmd.BudgetType == ProductBudgetType.Maintenance)
        {
            product.MaintenanceBudgetMonthly = cmd.MonthlyAllocation;
            bool hasQATeam = product.TeamAssignments != null && product.TeamAssignments.ContainsKey(ProductTeamRole.QA);
            product.IsMaintained = cmd.MonthlyAllocation > 0 && (product.IsCompetitorProduct || hasQATeam);
        }
        else
        {
            product.MarketingBudgetMonthly = cmd.MonthlyAllocation;
            bool hasMarketingTeam = product.TeamAssignments != null && product.TeamAssignments.ContainsKey(ProductTeamRole.Marketing);
            product.IsMarketingActive = cmd.MonthlyAllocation > 0 && hasMarketingTeam;
            if (product.IsMarketingActive)
                product.MarketingStartedTick = cmd.Tick;
        }

        _logger.Log($"[ProductSystem] Product {cmd.ProductId.Value} {cmd.BudgetType} budget set to ${cmd.MonthlyAllocation}/month");
    }

    private void HandleTriggerSaleEvent(TriggerSaleEventCommand cmd)
    {
        if (!_state.shippedProducts.TryGetValue(cmd.ProductId, out var product))
        {
            _logger.LogWarning($"[ProductSystem] TriggerSaleEvent failed: product {cmd.ProductId.Value} not found.");
            return;
        }

        if (!product.IsOnMarket)
        {
            _logger.LogWarning($"[ProductSystem] TriggerSaleEvent failed: product {cmd.ProductId.Value} not on market.");
            return;
        }

        if (!HasValidMarketingTeam(product))
        {
            _logger.LogWarning($"[ProductSystem] TriggerSaleEvent failed: no marketing team assigned to product {cmd.ProductId.Value}.");
            return;
        }

        if (product.IsOnSale)
        {
            _logger.LogWarning($"[ProductSystem] TriggerSaleEvent failed: product {cmd.ProductId.Value} already on sale.");
            return;
        }

        bool hasMarketing = HasValidMarketingTeam(product);
        int cooldown = hasMarketing ? SaleEventCooldownTicks / 2 : SaleEventCooldownTicks;
        int duration = hasMarketing ? SaleEventDurationTicks * 10 / 7 : SaleEventDurationTicks;

        bool firstSale = product.TotalSalesTriggered == 0;
        if (!firstSale && product.TicksSinceLastSale < cooldown)
        {
            _logger.LogWarning($"[ProductSystem] TriggerSaleEvent failed: product {cmd.ProductId.Value} still on cooldown.");
            return;
        }

        product.IsOnSale = true;
        product.SaleTicksRemaining = duration;
        product.TotalSalesTriggered++;
        product.PopularityScore = Math.Min(100f, product.PopularityScore + 8f);

        _pendingEvents.Add(new PendingEvent { Type = PendingEventType.ProductSaleStarted, ProductId = product.Id });
        _logger.Log($"[ProductSystem] Sale started on product '{product.ProductName}' (ID: {cmd.ProductId.Value})");
    }

    // ─── Feature Quality Helpers ───────────────────────────────────────────────

    private static float RecalculateOverallQuality(Product product, ProductTemplateDefinition template)
    {
        if (product.Features == null || product.Features.Length == 0)
            return product.OverallQuality;

        float weightedSum = 0f;
        float totalWeight = 0f;

        for (int i = 0; i < product.Features.Length; i++)
        {
            var fs = product.Features[i];
            if (fs == null) continue;

            float weight = 1f;
            if (template?.availableFeatures != null)
            {
                for (int j = 0; j < template.availableFeatures.Length; j++)
                {
                    if (template.availableFeatures[j]?.featureId == fs.FeatureId)
                    {
                        weight = template.availableFeatures[j].qualityWeight > 0f
                            ? template.availableFeatures[j].qualityWeight
                            : 1f;
                        break;
                    }
                }
            }

            weightedSum += fs.EffectiveQuality * weight;
            totalWeight += weight;
        }

        return totalWeight > 0f ? weightedSum / totalWeight : product.OverallQuality;
    }

    private float GetEffectiveFeatureCeiling(Product product, string featureId)
    {
        float platformCeiling = float.MaxValue;
        float toolCeiling = float.MaxValue;
        float hardwareCeiling = float.MaxValue;

        if (_platformSystem != null && product.TargetPlatformIds != null && product.TargetPlatformIds.Length > 0)
        {
            bool isOwned = !product.IsCompetitorProduct;
            platformCeiling = _platformSystem.GetCeiling(product.TargetPlatformIds[0], isOwned);
        }

        if (product.RequiredToolIds != null && product.RequiredToolIds.Length > 0)
        {
            float qualitySum = 0f;
            int count = 0;
            for (int i = 0; i < product.RequiredToolIds.Length; i++)
            {
                if (!_state.shippedProducts.TryGetValue(product.RequiredToolIds[i], out var toolProduct)) continue;
                float baseQuality = toolProduct.OverallQuality;
                float adjusted;
                if (toolProduct.IsCompetitorProduct)
                    adjusted = baseQuality * 0.95f;
                else if (toolProduct.DistributionModel == ToolDistributionModel.OpenSource)
                    adjusted = baseQuality * 1.05f;
                else
                    adjusted = baseQuality * 1.1f;

                int toolGenGap = product.ArchitectureGeneration - toolProduct.ArchitectureGeneration;
                if (toolGenGap > 0)
                {
                    float genPenalty = Math.Max(0.50f, 1f - toolGenGap * 0.10f);
                    adjusted *= genPenalty;
                }

                qualitySum += adjusted;
                count++;
            }
            if (count > 0)
                toolCeiling = qualitySum / count;
        }

        if (product.HasHardwareConfig && !string.IsNullOrEmpty(featureId))
        {
            ProductFeatureDefinition featureDef = FindFeatureDefinition(featureId);
            if (featureDef != null && featureDef.constrainedByHardware >= 0)
            {
                var component = (HardwareComponent)featureDef.constrainedByHardware;
                HardwareGenerationConfig genConfig = GetHardwareGenerationConfig(product.ArchitectureGeneration);
                if (genConfig != null)
                {
                    HardwareTier tier = GetTierForComponent(product.HardwareConfig, component);
                    hardwareCeiling = genConfig.GetHardwareCeiling(component, tier, product.HardwareConfig.formFactor);
                }
            }
        }

        float ceiling = platformCeiling;
        if (toolCeiling < ceiling) ceiling = toolCeiling;
        if (hardwareCeiling < ceiling) ceiling = hardwareCeiling;

        float crossProductCeiling = float.MaxValue;
        if (_crossProductGateConfig != null && !string.IsNullOrEmpty(featureId))
            crossProductCeiling = GetCrossProductFeatureCeiling(product, featureId);
        if (crossProductCeiling < ceiling) ceiling = crossProductCeiling;

        return ceiling == float.MaxValue ? 100f : ceiling;
    }

    private ProductFeatureDefinition FindFeatureDefinition(string featureId)
    {
        foreach (var kvp in _templateLookup)
        {
            var tmpl = kvp.Value;
            if (tmpl.availableFeatures == null) continue;
            for (int i = 0; i < tmpl.availableFeatures.Length; i++)
            {
                var f = tmpl.availableFeatures[i];
                if (f != null && f.featureId == featureId) return f;
            }
        }
        return null;
    }

    private float GetFeatureQualityOnProduct(ProductId productId, string featureId)
    {
        if (!_state.shippedProducts.TryGetValue(productId, out var product)) return 0f;
        if (product.Features == null) return 0f;
        int count = product.Features.Length;
        for (int i = 0; i < count; i++)
        {
            var fs = product.Features[i];
            if (fs != null && fs.FeatureId == featureId) return fs.EffectiveQuality;
        }
        return 0f;
    }

    public float GetUpstreamFeatureQuality(Product product, string upstreamFeatureId, bool isPlatformFeature)
    {
        ProductId[] ids = isPlatformFeature ? product.TargetPlatformIds : product.RequiredToolIds;
        if (ids == null) return 0f;
        float best = 0f;
        int count = ids.Length;
        for (int i = 0; i < count; i++)
        {
            float q = GetFeatureQualityOnProduct(ids[i], upstreamFeatureId);
            if (q > best) best = q;
        }
        return best;
    }

    public float GetCrossProductFeatureCeiling(Product product, string featureId)
    {
        ProductFeatureDefinition featureDef = FindFeatureDefinition(featureId);
        if (featureDef == null) return float.MaxValue;

        float platformFeatureCeiling = float.MaxValue;
        if (!string.IsNullOrEmpty(featureDef.requiresPlatformFeature))
        {
            float upstreamQuality = GetUpstreamFeatureQuality(product, featureDef.requiresPlatformFeature, true);
            platformFeatureCeiling = _crossProductGateConfig.GetTierCeiling(upstreamQuality);
        }

        float toolFeatureCeiling = float.MaxValue;
        if (!string.IsNullOrEmpty(featureDef.requiresToolFeature))
        {
            float upstreamQuality = GetUpstreamFeatureQuality(product, featureDef.requiresToolFeature, false);
            toolFeatureCeiling = _crossProductGateConfig.GetTierCeiling(upstreamQuality);
        }

        float result = platformFeatureCeiling;
        if (toolFeatureCeiling < result) result = toolFeatureCeiling;
        return result;
    }

    public bool IsFeatureAvailableForProduct(ProductFeatureDefinition feature, Product product)
    {
        if (feature == null) return true;

        if (!string.IsNullOrEmpty(feature.requiresPlatformFeature))
        {
            float q = GetUpstreamFeatureQuality(product, feature.requiresPlatformFeature, true);
            if (q <= 0f) return false;
        }

        if (!string.IsNullOrEmpty(feature.requiresToolFeature))
        {
            float q = GetUpstreamFeatureQuality(product, feature.requiresToolFeature, false);
            if (q <= 0f) return false;
        }

        return true;
    }

    private float ComputeWeightedQuality(Product product, ProductTemplateDefinition template)
    {
        if (product.Features == null || product.Features.Length == 0)
            return product.OverallQuality;

        float weightedSum = 0f;
        float totalWeight = 0f;
        int count = product.Features.Length;
        for (int i = 0; i < count; i++)
        {
            var fs = product.Features[i];
            if (fs == null) continue;

            float weight = 1f;
            if (template?.availableFeatures != null)
            {
                int aCount = template.availableFeatures.Length;
                for (int j = 0; j < aCount; j++)
                {
                    if (template.availableFeatures[j]?.featureId == fs.FeatureId)
                    {
                        weight = template.availableFeatures[j].qualityWeight > 0f
                            ? template.availableFeatures[j].qualityWeight
                            : 1f;
                        break;
                    }
                }
            }

            float baseQuality = fs.EffectiveQuality;
            float ceiling = GetEffectiveFeatureCeiling(product, fs.FeatureId);
            float adjusted = System.Math.Min(baseQuality, ceiling);

            weightedSum += adjusted * weight;
            totalWeight += weight;
        }

        return totalWeight > 0f ? weightedSum / totalWeight : product.OverallQuality;
    }

    public HardwareGenerationConfig GetHardwareGenerationConfig(int generation)
    {
        if (_hardwareGenerationConfigs == null || _hardwareGenerationConfigs.Length == 0) return null;
        HardwareGenerationConfig best = null;
        for (int i = 0; i < _hardwareGenerationConfigs.Length; i++)
        {
            var cfg = _hardwareGenerationConfigs[i];
            if (cfg == null) continue;
            if (cfg.generation == generation) return cfg;
            if (best == null || (cfg.generation < generation && cfg.generation > (best?.generation ?? -1)))
                best = cfg;
        }
        if (best != null) return best;
        // Fallback: highest generation
        HardwareGenerationConfig highest = null;
        for (int i = 0; i < _hardwareGenerationConfigs.Length; i++)
        {
            var cfg = _hardwareGenerationConfigs[i];
            if (cfg == null) continue;
            if (highest == null || cfg.generation > highest.generation) highest = cfg;
        }
        return highest;
    }

    public bool IsFeatureAvailableForHardware(ProductFeatureDefinition feature, HardwareConfiguration config)
    {
        if (feature == null) return true;
        if (feature.minimumHardwareTier >= 0 && feature.constrainedByHardware >= 0)
        {
            var component = (HardwareComponent)feature.constrainedByHardware;
            var minTier = (HardwareTier)feature.minimumHardwareTier;
            HardwareTier selectedTier = GetTierForComponent(config, component);
            if (selectedTier < minTier) return false;
        }
        if (feature.formFactorRequired >= 0)
        {
            var required = (ConsoleFormFactor)feature.formFactorRequired;
            if (required == ConsoleFormFactor.Portable && config.formFactor == ConsoleFormFactor.Hybrid) return true;
            if (config.formFactor != required) return false;
        }
        return true;
    }

    private static HardwareTier GetTierForComponent(HardwareConfiguration config, HardwareComponent component)
    {
        switch (component)
        {
            case HardwareComponent.Processing: return config.processingTier;
            case HardwareComponent.Graphics:   return config.graphicsTier;
            case HardwareComponent.Memory:     return config.memoryTier;
            case HardwareComponent.Storage:    return config.storageTier;
            default:                           return config.processingTier;
        }
    }

    private void ApplyFeatureCeilings(Product product)
    {
        if (product.Features == null) return;
        for (int i = 0; i < product.Features.Length; i++)
        {
            var fs = product.Features[i];
            if (fs == null) continue;
            float ceiling = GetEffectiveFeatureCeiling(product, fs.FeatureId);
            if (fs.Quality > ceiling)
                fs.Quality = ceiling;
        }
    }

    private void TickTechnicalDebt(Product product, int tick)
    {
        if (product.Features == null || product.Features.Length == 0) return;
        if (!product.IsOnMarket) return;

        int featureCount = product.Features.Length;
        float totalDebt = 0f;
        for (int i = 0; i < featureCount; i++)
        {
            var fs = product.Features[i];
            if (fs == null) continue;
            fs.TechnicalDebt += DebtAccumulationRatePerTick;
            if (fs.TechnicalDebt > fs.Quality)
                fs.TechnicalDebt = fs.Quality;
            totalDebt += fs.TechnicalDebt;
        }

        float legacyThreshold = featureCount * LegacyDebtPerFeature;
        if (!product.IsLegacy && totalDebt > legacyThreshold)
        {
            product.IsLegacy = true;
            _logger.Log($"[ProductSystem] Product '{product.ProductName}' (ID: {product.Id.Value}) flagged as Legacy — total debt {totalDebt:F1} exceeded threshold {legacyThreshold:F1}.");
        }

        if (product.Features.Length > 0 && _templateLookup.TryGetValue(product.TemplateId, out var template))
        {
            product.OverallQuality = RecalculateOverallQuality(product, template);
        }
    }

    private void TickSuccessorMigration(Product product)
    {
        if (product.SuccessorMigrationTicksTotal <= 0) return;
        if (product.SuccessorMigrationTicksElapsed >= product.SuccessorMigrationTicksTotal) return;
        if (product.SuccessorMigrationUsersPerTick <= 0) return;
        if (!product.IsOnMarket) return;

        int transfer = product.SuccessorMigrationUsersPerTick;
        product.ActiveUserCount = Math.Max(0, product.ActiveUserCount - transfer);
        product.SuccessorMigrationTicksElapsed++;
    }

    // ─── Tick Helpers ──────────────────────────────────────────────────────────

    private void TickPhaseWork(Product product, ProductPhaseRuntime phase)
    {
        if (!product.TeamAssignments.TryGetValue(phase.primaryRole, out TeamId primaryTeamId))
            return;

        var primaryTeam = _teamSystem.GetTeam(primaryTeamId);
        if (primaryTeam == null) return;

        SkillId skill = TeamWorkEngine.MapPhaseToSkill(phase.phaseType);
        int optimalSize = GetPhaseOptimalTeamSize(phase.phaseType, product);
        var primaryResult = TeamWorkEngine.AggregateTeam(
            primaryTeam.members,
            _employeeSystem,
            _fatigueSystem,
            skill,
            _roleProfileTable,
            _tuning?.TeamOverheadPerMember ?? 0.04f,
            optimalTeamSize: optimalSize);

        float variance = 0.95f + _rng.NextFloat01() * 0.10f;
        float genreSkillMult = 1f;
        float crunchMult = primaryTeam.isCrunching ? 1.10f : 1f;

        ChemistryBand phaseChemBand = _chemistrySystem != null
            ? _chemistrySystem.GetTeamChemistry(primaryTeam.id).Band
            : ChemistryBand.Neutral;
        float phaseConflictSpeed = _chemistrySystem != null
            ? 1f + _chemistrySystem.GetTeamSpeedPenalty(primaryTeam.id)
            : 1f;

        float primaryWork = TeamWorkEngine.ComputeWorkPerTick(
            in primaryResult,
            WorkRatePerSkillPoint,
            primaryResult.CoverageSpeedMod,
            variance,
            genreSkillMult * crunchMult,
            TeamWorkEngine.GetChemistrySpeedMod(phaseChemBand),
            phaseConflictSpeed);

        phase.workCompleted += primaryWork;

        if (_currentTick % TimeState.TicksPerDay == 0)
        {
            SkillGrowthSystem.AwardProductPhaseXP(
                primaryTeam, skill, _employeeSystem,
                _abilitySystem, _moraleSystem, _roleProfileTable, _tuning);
        }

        // Initial-phase bug accumulation during active development
        if (_currentTick % TimeState.TicksPerDay == 0 && phase.workCompleted < phase.totalWorkRequired && phase.phaseType != ProductPhaseType.QA) {
            float baseBugChance = 0.08f;
            if (primaryTeam.isCrunching) baseBugChance += 0.05f;

            int memberCount = primaryTeam.members.Count;
            for (int m = 0; m < memberCount; m++) {
                var member = _employeeSystem?.GetEmployee(primaryTeam.members[m]);
                if (member == null) continue;
                bool roleFit = TeamWorkEngine.IsRoleFitForSkill(member.role, skill);
                if (!roleFit) baseBugChance += 0.03f;
                if (member.GetSkill(skill) >= 12) baseBugChance -= 0.03f;
            }
            if (baseBugChance < 0.02f) baseBugChance = 0.02f;
            baseBugChance *= _tuning != null ? _tuning.BugRateMultiplier : 1f;

            float optimalFeaturesBug = GetOptimalFeatureCount(product);
            int featureCountBug = product.SelectedFeatureIds != null ? product.SelectedFeatureIds.Length : 0;
            float scopeBugRate = _tuning?.ScopeBugRatePerFeature ?? 0.08f;
            float scopeBugMult = 1f + Math.Max(0, featureCountBug - optimalFeaturesBug) * scopeBugRate;
            baseBugChance *= scopeBugMult;

            if (_rng.NextFloat01() < baseBugChance) {
                phase.bugAccumulation += 1.5f + _rng.NextFloat01() * 3.0f;
                if (phase.bugAccumulation > 50f) phase.bugAccumulation = 50f;
            }
        }

        if (phase.workCompleted >= phase.totalWorkRequired)
        {
            phase.workCompleted = phase.totalWorkRequired;
            phase.isComplete = true;
            phase.phaseQuality = CalculatePhaseQuality(product, phase);

            // Accumulate per-feature quality when a phase completes
            AccumulateFeatureQualityFromPhase(product, phase);

            if (phase.phaseType == ProductPhaseType.QA)
            {
                float qaCatchRate = 0.5f + (phase.phaseQuality / 100f) * 0.5f;
                int phaseCount = product.Phases.Length;
                for (int p = 0; p < phaseCount; p++)
                {
                    if (product.Phases[p].phaseType != ProductPhaseType.QA)
                        product.Phases[p].bugAccumulation *= (1f - qaCatchRate);
                }
            }

            _pendingEvents.Add(new PendingEvent { Type = PendingEventType.PhaseCompleted, ProductId = product.Id, PhaseType = phase.phaseType, FloatA = phase.phaseQuality });
            _logger.Log($"[ProductSystem] Phase {phase.phaseType} completed on product {product.Id.Value} — quality {phase.phaseQuality:F1}%");
        }
    }

    private void AccumulateFeatureQualityFromPhase(Product product, ProductPhaseRuntime phase)
    {
        if (product.Features == null || product.Features.Length == 0) return;
        if (!_templateLookup.TryGetValue(product.TemplateId, out var template)) return;

        float phaseContribution = phase.phaseQuality;

        int featureCount = product.Features.Length;
        for (int i = 0; i < featureCount; i++)
        {
            var fs = product.Features[i];
            if (fs == null) continue;

            float devCostMult = 1f;
            if (template.availableFeatures != null)
            {
                for (int j = 0; j < template.availableFeatures.Length; j++)
                {
                    if (template.availableFeatures[j]?.featureId == fs.FeatureId)
                    {
                        devCostMult = template.availableFeatures[j].devCostMultiplier > 0f
                            ? template.availableFeatures[j].devCostMultiplier
                            : 1f;
                        break;
                    }
                }
            }

            float contribution = phaseContribution / devCostMult;
            fs.Quality = Math.Min(100f, fs.Quality + contribution * 0.2f);
        }

        ApplyFeatureCeilings(product);

        float newOverall = RecalculateOverallQuality(product, template);
        if (newOverall > 0f)
            product.OverallQuality = newOverall;
    }

    private void TickIteration(Product product, ProductPhaseRuntime phase)
    {
        float work = CalculatePhaseWorkAmount(product, phase);
        phase.bonusWorkCompleted += work;

        if (phase.bonusWorkCompleted >= phase.bonusWorkTarget)
        {
            float qualityGain;
            float bugChance;

            if (phase.iterationCount == 1)
            {
                // Polish pass — large quality gains, moderate bug risk
                qualityGain = 12f + _rng.NextFloat01() * 8f;
                bugChance = 0.15f;

                if (product.TeamAssignments.TryGetValue(phase.primaryRole, out TeamId iterCrunchTeamId)) {
                    var iterCrunchTeam = _teamSystem?.GetTeam(iterCrunchTeamId);
                    if (iterCrunchTeam != null && iterCrunchTeam.isCrunching)
                        bugChance += 0.15f;
                }

                bugChance *= _tuning != null ? _tuning.BugRateMultiplier : 1f;
                phase.phaseQuality = Math.Min(100f, phase.phaseQuality + qualityGain);

                if (_rng.NextFloat01() < bugChance)
                {
                    float bugAmount = 3f + _rng.NextFloat01() * 7f;
                    phase.bugAccumulation += bugAmount;
                }
            }
            else if (phase.iterationCount == 2)
            {
                // Refinement pass — medium gains, slightly higher bug risk
                qualityGain = 5f + _rng.NextFloat01() * 5f;
                bugChance = 0.20f;

                if (product.TeamAssignments.TryGetValue(phase.primaryRole, out TeamId iterCrunchTeamId)) {
                    var iterCrunchTeam = _teamSystem?.GetTeam(iterCrunchTeamId);
                    if (iterCrunchTeam != null && iterCrunchTeam.isCrunching)
                        bugChance += 0.15f;
                }

                bugChance *= _tuning != null ? _tuning.BugRateMultiplier : 1f;
                phase.phaseQuality = Math.Min(100f, phase.phaseQuality + qualityGain);

                if (_rng.NextFloat01() < bugChance)
                {
                    float bugAmount = 3f + _rng.NextFloat01() * 7f;
                    phase.bugAccumulation += bugAmount;
                }
            }
            else
            {
                // Hardening pass — minimal quality gain, removes 40-70% of bugs
                qualityGain = _rng.NextFloat01() * 2f;
                bugChance = 0.08f;

                if (product.TeamAssignments.TryGetValue(phase.primaryRole, out TeamId iterCrunchTeamId)) {
                    var iterCrunchTeam = _teamSystem?.GetTeam(iterCrunchTeamId);
                    if (iterCrunchTeam != null && iterCrunchTeam.isCrunching)
                        bugChance += 0.15f;
                }

                bugChance *= _tuning != null ? _tuning.BugRateMultiplier : 1f;
                phase.phaseQuality = Math.Min(100f, phase.phaseQuality + qualityGain);

                float bugFixPercent = 0.40f + _rng.NextFloat01() * 0.30f;
                phase.bugAccumulation = Math.Max(0f, phase.bugAccumulation * (1f - bugFixPercent));

                if (_rng.NextFloat01() < bugChance)
                {
                    float bugAmount = 3f + _rng.NextFloat01() * 7f;
                    phase.bugAccumulation += bugAmount;
                }
            }

            phase.bugAccumulation = Math.Min(50f, phase.bugAccumulation);

            phase.isIterating = false;

            _pendingEvents.Add(new PendingEvent { Type = PendingEventType.PhaseIterationCompleted, ProductId = product.Id, PhaseType = phase.phaseType, FloatA = phase.phaseQuality });

            // Re-open QA if bugs accumulated (skip for hardening passes — they remove bugs)
            if (phase.bugAccumulation > 0f && phase.iterationCount < 3)
            {
                var qaPhase = FindPhase(product, ProductPhaseType.QA);
                if (qaPhase != null && qaPhase.isComplete && qaPhase.phaseType != phase.phaseType)
                {
                    qaPhase.isComplete = false;
                    RecalculateQABugWork(product, qaPhase);

                    _pendingEvents.Add(new PendingEvent { Type = PendingEventType.PhaseUnlocked, ProductId = product.Id, PhaseType = ProductPhaseType.QA });
                    _logger.Log($"[ProductSystem] QA re-opened on product {product.Id.Value} due to bugs from {phase.phaseType} iteration.");
                }
            }
        }
    }

    private float CalculatePhaseWorkAmount(Product product, ProductPhaseRuntime phase)
    {
        if (!product.TeamAssignments.TryGetValue(phase.primaryRole, out TeamId primaryTeamId))
            return 0f;

        var primaryTeam = _teamSystem.GetTeam(primaryTeamId);
        if (primaryTeam == null) return 0f;

        SkillId skill = TeamWorkEngine.MapPhaseToSkill(phase.phaseType);
        int optimalSize = GetPhaseOptimalTeamSize(phase.phaseType, product);
        var primaryResult = TeamWorkEngine.AggregateTeam(
            primaryTeam.members,
            _employeeSystem,
            _fatigueSystem,
            skill,
            _roleProfileTable,
            _tuning?.TeamOverheadPerMember ?? 0.04f,
            optimalTeamSize: optimalSize);

        float variance = 0.95f + _rng.NextFloat01() * 0.10f;
        float genreSkillMult = 1f;
        ChemistryBand calcChemBand = _chemistrySystem != null
            ? _chemistrySystem.GetTeamChemistry(primaryTeam.id).Band
            : ChemistryBand.Neutral;
        float calcConflictSpeed = _chemistrySystem != null
            ? 1f + _chemistrySystem.GetTeamSpeedPenalty(primaryTeam.id)
            : 1f;
        float primaryWork = TeamWorkEngine.ComputeWorkPerTick(
            in primaryResult,
            WorkRatePerSkillPoint,
            primaryResult.CoverageSpeedMod,
            variance,
            genreSkillMult,
            TeamWorkEngine.GetChemistrySpeedMod(calcChemBand),
            calcConflictSpeed);

        return primaryWork;
    }

    private float CalculatePhaseQuality(Product product, ProductPhaseRuntime phase)
    {
        float efficiency = System.Math.Min(1.0f, phase.totalWorkRequired > 0f ? phase.workCompleted / phase.totalWorkRequired : 0f);
        if (efficiency < 1.0f) return 0f;

        if (!product.TeamAssignments.TryGetValue(phase.primaryRole, out TeamId primaryTeamId))
            return 40f;

        var primaryTeam = _teamSystem.GetTeam(primaryTeamId);
        if (primaryTeam == null) return 40f;

        SkillId skill = TeamWorkEngine.MapPhaseToSkill(phase.phaseType);
        int optimalSize = GetPhaseOptimalTeamSize(phase.phaseType, product);
        var teamResult = TeamWorkEngine.AggregateTeam(
            primaryTeam.members,
            _employeeSystem,
            _fatigueSystem,
            skill,
            _roleProfileTable,
            _tuning?.TeamOverheadPerMember ?? 0.04f,
            optimalTeamSize: optimalSize);

        float minThresh = phase.minSkillThreshold;
        float targetThresh = phase.targetSkillThreshold;
        float excelThresh = phase.excellenceSkillThreshold;

        if (minThresh <= 0f && phase.qualitySoftCap > 0f)
        {
            minThresh = phase.qualitySoftCap * 0.08f;
            targetThresh = phase.qualitySoftCap * 0.14f;
            excelThresh = phase.qualitySoftCap * 0.20f;
        }

        int featureCount = product.SelectedFeatureIds != null ? product.SelectedFeatureIds.Length : 0;
        float optimalFeatures = GetOptimalFeatureCount(product);
        float scopeRatio = featureCount / optimalFeatures;
        if (scopeRatio > 1f) {
            float scopeExponent = _tuning?.ScopeComplexityExponent ?? 0.6f;
            float scopeMult = (float)Math.Pow(scopeRatio, scopeExponent);
            minThresh *= scopeMult;
            targetThresh *= scopeMult;
            excelThresh *= scopeMult;
        }

        ChemistryBand qualChemBand = _chemistrySystem != null
            ? _chemistrySystem.GetTeamChemistry(primaryTeam.id).Band
            : ChemistryBand.Neutral;
        float qualConflictQuality = _chemistrySystem != null
            ? 1f + _chemistrySystem.GetTeamQualityPenalty(primaryTeam.id)
            : 1f;

        float baseQuality = TeamWorkEngine.ComputeQuality(
            teamResult.AvgQualitySkill,
            minThresh,
            targetThresh,
            excelThresh,
            teamResult.CoverageQualityMod,
            teamResult.AvgMorale,
            TeamWorkEngine.GetChemistryQualityMod(qualChemBand),
            qualConflictQuality);
        float toolLift = GetToolQualityLift(product);
        float quality = baseQuality * (1f + toolLift);
        return Math.Clamp(quality, 0f, 100f);
    }

    private void RecalculateQABugWork(Product product, ProductPhaseRuntime qaPhase)
    {
        float totalBugWork = 0f;
        int phaseCount = product.Phases.Length;
        for (int p = 0; p < phaseCount; p++)
        {
            if (product.Phases[p].phaseType != ProductPhaseType.QA)
                totalBugWork += product.Phases[p].bugAccumulation;
        }
        qaPhase.totalWorkRequired += totalBugWork * BugWorkMultiplier;
    }

    private float GetOptimalFeatureCount(Product product)
    {
        if (_templateLookup.TryGetValue(product.TemplateId, out var template) && template.availableFeatures != null && template.availableFeatures.Length > 0)
            return Math.Max(3, template.availableFeatures.Length / 3);
        int featureCount = product.SelectedFeatureIds != null ? product.SelectedFeatureIds.Length : 0;
        return Math.Max(3, featureCount);
    }

    private int GetPhaseOptimalTeamSize(ProductPhaseType phaseType, Product product)
    {
        int baseOptimal = 4;
        if (_templateLookup.TryGetValue(product.TemplateId, out var template))
            baseOptimal = template.optimalTeamSizePerPhase;

        int generation = product.ArchitectureGeneration;
        float genScale = 1f + (generation - 1) * 0.15f;

        int featureCount = product.Features != null ? product.Features.Length : 0;
        float featureScale = 1f + featureCount * 0.03f;

        return Math.Max(2, (int)(baseOptimal * genScale * featureScale));
    }

    private float GetToolQualityLift(Product product)
    {
        if (!_templateLookup.TryGetValue(product.TemplateId, out var template)) return 0f;

        float lift = 0f;

        if (product.RequiredToolIds != null)
        {
            int toolCount = product.RequiredToolIds.Length;
            for (int i = 0; i < toolCount; i++)
            {
                if (_state.shippedProducts.TryGetValue(product.RequiredToolIds[i], out Product toolProduct))
                {
                    float toolQualityFactor = toolProduct.OverallQuality / 100f;
                    bool isOwn = !toolProduct.IsCompetitorProduct;
                    float bonus = isOwn ? template.ownToolQualityBonus : template.licensedToolQualityBonus;
                    lift += bonus * toolQualityFactor;
                }
            }
        }

        int generation = product.ArchitectureGeneration;
        float genBonus = (generation - 1) * 0.02f;
        lift += genBonus;

        return lift;
    }

    private bool AllPrerequisitesComplete(Product product, ProductPhaseRuntime phase)
    {
        if (phase.prerequisites == null || phase.prerequisites.Length == 0) return true;
        int reqCount = phase.prerequisites.Length;
        for (int r = 0; r < reqCount; r++)
        {
            var prereqPhase = FindPhase(product, phase.prerequisites[r]);
            if (prereqPhase == null || !prereqPhase.isComplete) return false;
        }
        return true;
    }

    private static ProductPhaseRuntime FindPhase(Product product, ProductPhaseType phaseType)
    {
        int phaseCount = product.Phases.Length;
        for (int p = 0; p < phaseCount; p++)
        {
            if (product.Phases[p].phaseType == phaseType)
                return product.Phases[p];
        }
        return null;
    }

    private static ProductNiche DeriveNiche(ProductTemplateDefinition template, CreateProductCommand cmd)
    {
        if (cmd.SelectedNiche != ProductNiche.None)
            return cmd.SelectedNiche;
        if (template.nicheConfigs != null && template.nicheConfigs.Length > 0)
            return template.nicheConfigs[0].niche;
        return ProductNiche.None;
    }

    private static ProductPhaseRuntime[] BuildPhaseRuntimes(ProductTemplateDefinition template, string[] selectedFeatureIds, float workMultiplier = 1f, float nicheDevTimeMult = 1f, int difficultyTier = 1)
    {
        if (template.phases == null || template.phases.Length == 0)
            return new ProductPhaseRuntime[0];

        int featureCount = selectedFeatureIds != null ? selectedFeatureIds.Length : 0;
        float difficultyScale = 1.0f + (difficultyTier - 1) * 0.75f;
        float featureScale = 1.0f + featureCount * 0.15f + (float)Math.Pow(featureCount, 1.8) * 0.015f;

        int phaseCount = template.phases.Length;
        var runtimes = new ProductPhaseRuntime[phaseCount];

        for (int i = 0; i < phaseCount; i++)
        {
            var def = template.phases[i];
            float totalWork = def.baseWorkUnits * workMultiplier * nicheDevTimeMult * difficultyScale * featureScale;

            runtimes[i] = new ProductPhaseRuntime
            {
                phaseType = def.phaseType,
                primaryRole = def.primaryRole,
                prerequisites = def.prerequisites ?? new ProductPhaseType[0],
                totalWorkRequired = totalWork,
                workCompleted = 0f,
                phaseQuality = 0f,
                qualitySoftCap = def.qualitySoftCapBase,
                minSkillThreshold = template.difficultyTier * 1.5f + 2f,
                targetSkillThreshold = template.difficultyTier * 3f + 2f,
                excellenceSkillThreshold = template.difficultyTier * 4f + 4f,
                iterationCount = 0,
                isComplete = false,
                isIterating = false,
                bonusWorkTarget = 0f,
                bonusWorkCompleted = 0f,
                bugAccumulation = 0f,
                isUnlocked = (def.prerequisites == null || def.prerequisites.Length == 0)
            };
        }

        return runtimes;
    }

    private void AwardShipXP(Product product, List<EmployeeId> employeeIds)
    {
        if (_employeeSystem == null || _roleProfileTable == null || employeeIds.Count == 0) return;

        float qualityFactor = product.OverallQuality / 100f;

        float complexityFactor = 1f;
        if (_templateLookup.TryGetValue(product.TemplateId, out var tmpl) && tmpl.availableFeatures != null && tmpl.availableFeatures.Length > 0)
        {
            int selected = product.SelectedFeatureIds != null ? product.SelectedFeatureIds.Length : 0;
            complexityFactor = (float)selected / tmpl.availableFeatures.Length;
            if (complexityFactor < 0.3f) complexityFactor = 0.3f;
            if (complexityFactor > 2f) complexityFactor = 2f;
        }

        int durationCapDays = _tuning?.ProductXPDurationCapDays ?? 180;
        int devTicks = _currentTick - product.CreationTick;
        float devDays = devTicks / (float)TimeState.TicksPerDay;
        float durationFactor = devDays / durationCapDays;
        if (durationFactor < 0.2f) durationFactor = 0.2f;
        if (durationFactor > 2f) durationFactor = 2f;

        int maxXP = _tuning?.MaxXPPerProductShip ?? 3;
        float baseXP = maxXP * qualityFactor * complexityFactor * durationFactor;

        int empCount = employeeIds.Count;
        for (int i = 0; i < empCount; i++)
        {
            var employee = _employeeSystem.GetEmployee(employeeIds[i]);
            if (employee == null || !employee.isActive) continue;

            var profile = _roleProfileTable.Get(employee.role);
            var skillBands = profile?.SkillBands;
            int currentCA = skillBands != null
                ? AbilityCalculator.ComputeRoleCA(employee.Stats.Skills, skillBands)
                : 0;
            if (currentCA >= employee.Stats.PotentialAbility) continue;

            float learningRate = employee.Stats.GetHiddenAttribute(HiddenAttributeId.LearningRate);
            float learningMult = 0.7f + (learningRate / 20f) * 0.6f;
            float xpAmount = baseXP * learningMult * (0.8f + _rng.NextFloat01() * 0.4f);
            if (employee.isFounder) xpAmount *= 1.5f;
            if (xpAmount <= 0f) continue;

            SkillId primarySkill = GetHighestSkillForEmployee(employee);
            int skillIdx = (int)primarySkill;

            int oldSkillLevel = employee.Stats.Skills[skillIdx];
            employee.Stats.SkillXp[skillIdx] += xpAmount;
            while (employee.Stats.SkillXp[skillIdx] >= 1.0f && employee.Stats.Skills[skillIdx] < 20)
            {
                employee.Stats.Skills[skillIdx]++;
                int newCA = skillBands != null ? AbilityCalculator.ComputeRoleCA(employee.Stats.Skills, skillBands) : 0;
                if (newCA > employee.Stats.PotentialAbility)
                {
                    employee.Stats.Skills[skillIdx]--;
                    employee.Stats.SkillXp[skillIdx] = 0f;
                    break;
                }
                employee.Stats.SkillXp[skillIdx] -= 1.0f;
            }
            if (employee.Stats.Skills[skillIdx] >= 20)
                employee.Stats.SkillXp[skillIdx] = 0f;
            employee.Stats.SkillDeltaDirection[skillIdx] = (sbyte)(employee.Stats.Skills[skillIdx] > oldSkillLevel ? 1 : 0);

            _abilitySystem?.InvalidateCA(employeeIds[i]);
        }
    }

    private static SkillId GetHighestSkillForEmployee(Employee employee)
    {
        int bestIdx = 0;
        int bestVal = employee.Stats.Skills.Length > 0 ? employee.Stats.Skills[0] : 0;
        for (int i = 1; i < SkillIdHelper.SkillCount; i++)
        {
            if (i < employee.Stats.Skills.Length && employee.Stats.Skills[i] > bestVal)
            {
                bestVal = employee.Stats.Skills[i];
                bestIdx = i;
            }
        }
        return (SkillId)bestIdx;
    }

    private void AppendProductWorkHistory(Product product, float overallQuality, WorkOutcome outcome)
    {
        if (_employeeSystem == null || _teamSystem == null) return;

        foreach (var kvp in product.TeamAssignments)
        {
            var team = _teamSystem.GetTeam(kvp.Value);
            if (team == null) continue;

            int memberCount = team.members.Count;
            if (memberCount == 0) continue;

            // Compute team average skill for contribution label
            float teamSkillSum = 0f;
            for (int m = 0; m < memberCount; m++)
            {
                var emp = _employeeSystem.GetEmployee(team.members[m]);
                if (emp == null) continue;
                teamSkillSum += (int)GetHighestSkillForEmployee(emp);
            }
            float teamAvgSkill = memberCount > 0 ? teamSkillSum / memberCount : 0f;

            for (int m = 0; m < memberCount; m++)
            {
                var emp = _employeeSystem.GetEmployee(team.members[m]);
                if (emp == null || !emp.isActive) continue;

                float memberSkill = (int)GetHighestSkillForEmployee(emp);
                string contribution;
                if (teamAvgSkill <= 0f)                          contribution = "Medium";
                else if (memberSkill >= teamAvgSkill * 1.2f)     contribution = "High";
                else if (memberSkill <= teamAvgSkill * 0.8f)     contribution = "Low";
                else                                              contribution = "Medium";

                emp.AppendWorkHistory(new WorkHistoryEntry
                {
                    CompletedTick     = _currentTick,
                    EntryType         = WorkEntryType.Product,
                    WorkName          = product.ProductName,
                    TeamName          = team.name,
                    Role              = emp.role,
                    ContributionLabel = contribution,
                    QualityScore      = (int)overallQuality,
                    XpSummary         = "",
                    Outcome           = outcome
                });
            }
        }
    }

    private float ComputeFeatureRelevanceAtShip(Product product)
    {
        if (_marketSystem == null || product.SelectedFeatureIds == null || product.SelectedFeatureIds.Length == 0)
            return 1f;

        if (!_templateLookup.TryGetValue(product.TemplateId, out var template) || template.availableFeatures == null)
            return 1f;

        int currentGen = _generationSystem != null ? _generationSystem.GetCurrentGeneration() : 1;
        int selectedCount = product.SelectedFeatureIds.Length;
        float innovationSum = 0f;
        float missingPenaltySum = 0f;

        FeatureCategory cat0 = (FeatureCategory)(-1);
        FeatureCategory cat1 = (FeatureCategory)(-1);
        FeatureCategory cat2 = (FeatureCategory)(-1);
        FeatureCategory cat3 = (FeatureCategory)(-1);
        FeatureCategory cat4 = (FeatureCategory)(-1);
        int distinctCats = 0;

        for (int i = 0; i < selectedCount; i++)
        {
            string featureId = product.SelectedFeatureIds[i];
            ProductFeatureDefinition featDef = null;
            int len = template.availableFeatures.Length;
            for (int f = 0; f < len; f++)
            {
                if (template.availableFeatures[f] != null && template.availableFeatures[f].featureId == featureId)
                {
                    featDef = template.availableFeatures[f];
                    break;
                }
            }

            float adoptionRate = _marketSystem.GetFeatureAdoptionRate(featureId, product.Niche, product.TemplateId);
            FeatureDemandStage stage;
            if (featDef != null)
                stage = FeatureDemandHelper.GetDemandStage(currentGen, featDef.demandIntroductionGen, featDef.demandMaturitySpeed, featDef.isFoundational, adoptionRate);
            else
                stage = FeatureDemandStage.Standard;

            innovationSum += FeatureDemandHelper.GetInnovationValue(stage);

            FeatureCategory category = _marketSystem.GetFeatureCategory(featureId);
            if      (category == cat0) { }
            else if (category == cat1) { }
            else if (category == cat2) { }
            else if (category == cat3) { }
            else if (category == cat4) { }
            else {
                switch (distinctCats) {
                    case 0: cat0 = category; break;
                    case 1: cat1 = category; break;
                    case 2: cat2 = category; break;
                    case 3: cat3 = category; break;
                    default: cat4 = category; break;
                }
                distinctCats++;
            }
        }

        int poolLen = template.availableFeatures.Length;
        for (int f = 0; f < poolLen; f++)
        {
            var featDef = template.availableFeatures[f];
            if (featDef == null) continue;

            bool isSelected = false;
            for (int s = 0; s < selectedCount; s++)
            {
                if (product.SelectedFeatureIds[s] == featDef.featureId) { isSelected = true; break; }
            }
            if (isSelected) continue;

            float coverageRatio = _marketSystem.GetFeatureAdoptionRate(featDef.featureId, product.Niche, product.TemplateId);
            FeatureDemandStage poolStage = FeatureDemandHelper.GetDemandStage(currentGen, featDef.demandIntroductionGen, featDef.demandMaturitySpeed, featDef.isFoundational, coverageRatio);
            missingPenaltySum += FeatureDemandHelper.GetMissingPenalty(poolStage, coverageRatio);
        }

        float avgInnovation = innovationSum / selectedCount;

        int categoriesRepresented = distinctCats;
        int availableCategories = CountTemplateCategories(product.TemplateId);
        float diversityMod;
        if (availableCategories <= 2)
        {
            diversityMod = 1.0f;
        }
        else
        {
            switch (categoriesRepresented)
            {
                case 1:  diversityMod = 0.8f; break;
                case 2:  diversityMod = 1.0f; break;
                case 3:  diversityMod = 1.1f; break;
                default: diversityMod = 1.2f; break;
            }
        }

        float raw = (avgInnovation - missingPenaltySum * 0.1f) / 50f;
        return Math.Max(0.2f, Math.Min(0.95f, raw * diversityMod));
    }

    private float ComputeExpectedSelectedRatio(Product product, ProductTemplateDefinition template)
    {
        if (template.availableFeatures == null || template.availableFeatures.Length == 0) return 1f;
        int currentGen = _generationSystem != null ? _generationSystem.GetCurrentGeneration() : 1;
        int expectedCount = 0;
        int selectedExpectedCount = 0;
        int poolLen = template.availableFeatures.Length;
        for (int f = 0; f < poolLen; f++)
        {
            var featDef = template.availableFeatures[f];
            if (featDef == null) continue;
            var stage = FeatureDemandHelper.GetDemandStage(currentGen, featDef.demandIntroductionGen, featDef.demandMaturitySpeed, featDef.isFoundational, 0f);
            bool isExpected = featDef.isFoundational || stage == FeatureDemandStage.Standard;
            if (!isExpected) continue;
            expectedCount++;
            if (product.SelectedFeatureIds != null)
            {
                int selLen = product.SelectedFeatureIds.Length;
                for (int s = 0; s < selLen; s++)
                {
                    if (product.SelectedFeatureIds[s] == featDef.featureId) { selectedExpectedCount++; break; }
                }
            }
        }
        return expectedCount > 0 ? (float)selectedExpectedCount / expectedCount : 1f;
    }

    private int GetTemplateFeaturePoolSize(string templateId)
    {
        if (_templateLookup.TryGetValue(templateId, out var t) && t.availableFeatures != null)
            return t.availableFeatures.Length;
        return 1;
    }

    private int GetTemplateCategoryCount(string templateId, FeatureCategory category)
    {
        if (!_templateLookup.TryGetValue(templateId, out var t) || t.availableFeatures == null)
            return 0;
        int count = 0;
        int len = t.availableFeatures.Length;
        for (int i = 0; i < len; i++)
        {
            if (t.availableFeatures[i] != null && t.availableFeatures[i].featureCategory == category)
                count++;
        }
        return count;
    }

    private int CountTemplateCategories(string templateId)
    {
        if (!_templateLookup.TryGetValue(templateId, out var t) || t.availableFeatures == null)
            return 1;
        FeatureCategory c0 = (FeatureCategory)(-1), c1 = (FeatureCategory)(-1), c2 = (FeatureCategory)(-1),
                        c3 = (FeatureCategory)(-1), c4 = (FeatureCategory)(-1);
        int distinct = 0;
        int len = t.availableFeatures.Length;
        for (int i = 0; i < len; i++)
        {
            if (t.availableFeatures[i] == null) continue;
            FeatureCategory fc = t.availableFeatures[i].featureCategory;
            if (fc == c0 || fc == c1 || fc == c2 || fc == c3 || fc == c4) continue;
            switch (distinct) {
                case 0: c0 = fc; break;
                case 1: c1 = fc; break;
                case 2: c2 = fc; break;
                case 3: c3 = fc; break;
                default: c4 = fc; break;
            }
            distinct++;
            if (distinct >= 5) break;
        }
        return distinct;
    }
}
