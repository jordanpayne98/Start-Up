using System;
using System.Collections.Generic;

public class CompetitorSystem : ISystem
{
    private enum PendingEventType : byte
    {
        Spawned,
        Bankrupt,
        ProductLaunched,
        ProductSunset,
        FinancesChanged,
        DevStarted
    }

    private struct PendingCompetitorEvent
    {
        public PendingEventType Type;
        public CompetitorId CompId;
        public ProductId ProductId;
    }

    private const int MonthlyEvalIntervalTicks = TimeState.TicksPerDay * 30;
    private const int BankruptcyMonthsThreshold = 12;
    private const long MinCashFloor = 0L;
    private const long MaxCashCeiling = 1_000_000_000L;

    public event Action<CompetitorId> OnCompetitorSpawned;
    public event Action<CompetitorId> OnCompetitorBankrupt;
    public event Action<CompetitorId, CompetitorId> OnCompetitorAbsorbed;
    public event Action<CompetitorId, ProductId> OnCompetitorProductLaunched;
    public event Action<CompetitorId, ProductId> OnCompetitorProductSunset;
    public event Action<CompetitorId, ProductId> OnCompetitorProductUpdated;
    public event Action<CompetitorId> OnCompetitorFinancesChanged;
    public event Action<CompetitorId, ProductId> OnCompetitorDevStarted;

    private readonly CompetitorState _state;
    private readonly ProductState _productState;
    private readonly MarketState _marketState;
    private readonly IRng _rng;
    private readonly ILogger _logger;

    private TimeSystem _timeSystem;
    private MarketSystem _marketSystem;
    private ReviewSystem _reviewSystem;
    private TaxSystem _taxSystem;
    private GenerationSystem _generationSystem;
    private PlatformState _platformState;
    private ProductSystem _productSystem;
    private EmployeeSystem _employeeSystem;
    private MoraleSystem _moraleSystem;
    private TeamSystem _teamSystem;
    private TuningConfig _tuning;
    private CompetitorArchetypeConfig[] _archetypeConfigs;
    private CompetitorStartConfig[] _startConfigs;
    private CompetitorNameData _nameData;
    private Dictionary<string, ProductTemplateDefinition> _templateLookup;
    private CrossProductGateConfig _crossProductGateConfig;

    private static readonly float[] BiasNeutral = { 1.0f, 1.0f, 1.0f, 1.0f, 1.0f };

    private readonly List<PendingCompetitorEvent> _pendingEvents;
    private readonly List<CompetitorId> _scratchIds;
    private readonly List<ProductId> _scratchProductIds;
    private readonly List<CompetitorId> _bankruptPendingIds;

    private int _lastMonthlyEvalTick = -1;
    private int _lastKnownTick = 0;

    private Action<ProductId, float> _onProductShippedHandler;

    public int ActiveCompetitorCount
    {
        get
        {
            int count = 0;
            foreach (var kvp in _state.competitors)
            {
                if (!kvp.Value.IsBankrupt && !kvp.Value.IsAbsorbed)
                    count++;
            }
            return count;
        }
    }

    public CompetitorSystem(
        CompetitorState state,
        ProductState productState,
        MarketState marketState,
        IRng rng,
        ILogger logger)
    {
        _state = state ?? throw new ArgumentNullException(nameof(state));
        _productState = productState ?? throw new ArgumentNullException(nameof(productState));
        _marketState = marketState ?? throw new ArgumentNullException(nameof(marketState));
        _rng = rng ?? throw new ArgumentNullException(nameof(rng));
        _logger = logger ?? new NullLogger();
        _pendingEvents = new List<PendingCompetitorEvent>();
        _scratchIds = new List<CompetitorId>(16);
        _scratchProductIds = new List<ProductId>(16);
        _bankruptPendingIds = new List<CompetitorId>(4);
        _templateLookup = new Dictionary<string, ProductTemplateDefinition>();
    }

    public void SetTimeSystem(TimeSystem ts) { _timeSystem = ts; }
    public void SetMarketSystem(MarketSystem ms) { _marketSystem = ms; }
    public void SetReviewSystem(ReviewSystem rs) { _reviewSystem = rs; }
    public void SetTaxSystem(TaxSystem ts) { _taxSystem = ts; }
    public void SetGenerationSystem(GenerationSystem gs) { _generationSystem = gs; }
    public void SetPlatformState(PlatformState ps) { _platformState = ps; }
    public void SetEmployeeSystem(EmployeeSystem es) { _employeeSystem = es; }
    public void SetMoraleSystem(MoraleSystem ms) { _moraleSystem = ms; }
    public void SetTeamSystem(TeamSystem ts) { _teamSystem = ts; }
    public void SetTuning(TuningConfig tuning) { _tuning = tuning; }
    public void SetCrossProductGateConfig(CrossProductGateConfig cfg) { _crossProductGateConfig = cfg; }
    public void SetProductSystem(ProductSystem ps)
    {
        if (_productSystem != null)
        {
            if (_onProductShippedHandler != null)
                _productSystem.OnProductShipped -= _onProductShippedHandler;
        }

        _productSystem = ps;

        if (_productSystem != null)
        {
            _onProductShippedHandler = OnCompetitorProductShipped;
            _productSystem.OnProductShipped += _onProductShippedHandler;
        }
    }

    public void RegisterTemplates(ProductTemplateDefinition[] templates)
    {
        _templateLookup.Clear();
        if (templates == null) return;
        for (int i = 0; i < templates.Length; i++)
        {
            var t = templates[i];
            if (t != null && !string.IsNullOrEmpty(t.templateId))
                _templateLookup[t.templateId] = t;
        }
    }

    public void SetArchetypeConfigs(CompetitorArchetypeConfig[] configs)
    {
        _archetypeConfigs = configs;
    }

    public void SetNameData(CompetitorNameData nameData)
    {
        _nameData = nameData;
    }

    public void RegisterStartConfigs(CompetitorStartConfig[] configs)
    {
        _startConfigs = configs;
    }

    public void GenerateStartingCompetitors(int count, int masterSeed)
    {
        if (_startConfigs != null && _startConfigs.Length > 0)
        {
            InitializeFromStartConfigs();
            int remaining = count - _startConfigs.Length;
            if (remaining > 0)
                GenerateRandomCompetitors(remaining, masterSeed);
        }
        else
        {
            GenerateRandomCompetitors(count, masterSeed);
        }

        _logger.Log($"[CompetitorSystem] Generated {_state.competitors.Count} starting competitors.");
    }

    private void InitializeFromStartConfigs()
    {
        if (_startConfigs == null) return;
        IRng initRng = new RngStream(_state.nextCompetitorId ^ "startconfig-init".GetHashCode());

        for (int s = 0; s < _startConfigs.Length; s++)
        {
            var cfg = _startConfigs[s];
            if (cfg == null) continue;

            var id = new CompetitorId(_state.nextCompetitorId++);
            var comp = new Competitor
            {
                Id = id,
                CompanyName = cfg.companyName,
                FounderName = cfg.founderName,
                Archetype = cfg.archetype,
                Personality = cfg.personality,
                Specializations = cfg.specializations,
                IsFounderNamed = false,
                Finance = default,
                IsBankrupt = false,
                IsAbsorbed = false,
                FoundedTick = 0,
                NicheMarketShare = new Dictionary<ProductNiche, float>(),
                ActiveProductIds = new List<ProductId>(),
                InDevelopmentProductIds = new List<ProductId>(),
                ScheduledUpdates = new List<ScheduledCompetitorUpdate>(),
                EmployeeIds = new List<EmployeeId>(),
                TeamAssignments = new Dictionary<TeamId, ProductId>(),
                LastProductEvalTick = 0,
                LastFinanceEvalTick = 0,
                ReputationPoints = GetStartConfigReputation(cfg.archetype, initRng),
                LastHireTick = -1,
                LastProductStartedTick = -1,
                LastPricingReviewTick = -1,
                Memory = CompetitorMemory.CreateNew(),
            };

            comp.CompanyFans = comp.ReputationPoints * initRng.Range(10, 31);
            comp.FanSentiment = 55f + initRng.NextFloat01() * 30f;

            ApplyRealEmployees(comp, initRng, 0);
            SetInitialCash(comp, initRng);
            InitializeMoraleForEmployees(comp);

            _state.competitors[id] = comp;

            ProductId[] startingProductIds = null;
            if (cfg.startingProducts != null && cfg.startingProducts.Length > 0)
            {
                startingProductIds = new ProductId[cfg.startingProducts.Length];
                for (int p = 0; p < cfg.startingProducts.Length; p++)
                {
                    var sp = cfg.startingProducts[p];
                    int productIdBefore = _productState.nextProductId;
                    CreateStartingProduct(comp, sp, initRng);
                    startingProductIds[p] = new ProductId(productIdBefore);
                }
            }

            if (cfg.startingDevProducts != null)
            {
                for (int p = 0; p < cfg.startingDevProducts.Length; p++)
                {
                    var sdp = cfg.startingDevProducts[p];
                    CreateStartingDevProduct(comp, sdp);
                }
            }

            if (cfg.scheduledUpdates != null && startingProductIds != null)
            {
                for (int p = 0; p < cfg.scheduledUpdates.Length; p++)
                {
                    var su = cfg.scheduledUpdates[p];
                    if (su.productIndex < 0 || su.productIndex >= startingProductIds.Length) continue;
                    int scheduledTick = su.monthsUntilUpdate * 30 * TimeState.TicksPerDay;
                    comp.ScheduledUpdates.Add(new ScheduledCompetitorUpdate
                    {
                        ProductId = startingProductIds[su.productIndex],
                        ScheduledTick = scheduledTick
                    });
                }
            }

            CreateInitialTeams(comp, 0);

            var capturedId = id;
            _pendingEvents.Add(new PendingCompetitorEvent { Type = PendingEventType.Spawned, CompId = capturedId });
            _logger.Log($"[CompetitorSystem] Initialized start competitor '{comp.CompanyName}' ({comp.Archetype}).");
        }

        BackfillToolAndPlatformDependencies();

        for (int s = 0; s < _startConfigs.Length; s++)
        {
            var cfg = _startConfigs[s];
            if (cfg == null) continue;
            if (!TryGetCompetitorByName(cfg.companyName, out var comp)) continue;
            AutoSeedPlatformApplications(comp, initRng);
        }

        BackfillToolAndPlatformDependencies();
    }

    private bool TryGetCompetitorByName(string companyName, out Competitor comp)
    {
        foreach (var kvp in _state.competitors)
        {
            if (kvp.Value.CompanyName == companyName)
            {
                comp = kvp.Value;
                return true;
            }
        }
        comp = default;
        return false;
    }

    private int GetStartConfigReputation(CompetitorArchetype archetype, IRng rng)
    {
        switch (archetype)
        {
            case CompetitorArchetype.PlatformGiant: return rng.Range(8000, 18001);
            case CompetitorArchetype.FullStack:     return rng.Range(5000, 12001);
            case CompetitorArchetype.ToolMaker:     return rng.Range(3000, 8001);
            case CompetitorArchetype.GameStudio:    return rng.Range(1000, 4001);
            default:                                return rng.Range(500, 2001);
        }
    }

    private void ApplyRealEmployees(Competitor comp, IRng rng, int tick)
    {
        if (_employeeSystem == null) return;
        int minCount, maxCount;
        switch (comp.Archetype)
        {
            case CompetitorArchetype.PlatformGiant:
                minCount = 15; maxCount = 25; break;
            case CompetitorArchetype.FullStack:
                minCount = 12; maxCount = 20; break;
            case CompetitorArchetype.ToolMaker:
                minCount = 8; maxCount = 15; break;
            default:
                minCount = 5; maxCount = 12; break;
        }
        int employeeCount = rng.Range(minCount, maxCount + 1);
        var hiredIds = _employeeSystem.BulkHireForCompany(comp.Id.ToCompanyId(), comp.Archetype, employeeCount, rng, tick);
        for (int i = 0; i < hiredIds.Count; i++)
            comp.EmployeeIds.Add(hiredIds[i]);
    }

    private void InitializeMoraleForEmployees(Competitor comp)
    {
        if (_moraleSystem == null) return;
        int count = comp.EmployeeIds.Count;
        for (int i = 0; i < count; i++)
            _moraleSystem.InitializeEmployee(comp.EmployeeIds[i]);
    }

    private void CreateInitialTeams(Competitor comp, int tick)
    {
        if (_teamSystem == null || comp.EmployeeIds.Count == 0) return;

        int productCount = (comp.ActiveProductIds != null ? comp.ActiveProductIds.Count : 0)
                         + (comp.InDevelopmentProductIds != null ? comp.InDevelopmentProductIds.Count : 0);
        int devTeamCount = productCount > 0 ? productCount : 1;

        CompanyId companyId = comp.Id.ToCompanyId();
        var teamIds = new List<TeamId>(devTeamCount);
        for (int t = 0; t < devTeamCount; t++)
        {
            TeamId teamId = _teamSystem.CreateTeam(TeamType.Programming, tick, companyId);
            teamIds.Add(teamId);
        }

        int empCount = comp.EmployeeIds.Count;
        for (int i = 0; i < empCount; i++)
        {
            int teamIndex = i % teamIds.Count;
            _teamSystem.AssignEmployeeToTeam(comp.EmployeeIds[i], teamIds[teamIndex]);
        }
    }

    private void SetInitialCash(Competitor comp, IRng rng)
    {
        long monthlySalary = _employeeSystem != null
            ? (long)_employeeSystem.TotalMonthlySalariesForCompany(comp.Id.ToCompanyId())
            : (long)(comp.EmployeeIds.Count * SalaryBand.GetBase(EmployeeRole.Developer));
        int runwayMonths = rng.Range(12, 37);
        comp.Finance.Cash = monthlySalary * runwayMonths;
    }

    private void CreateStartingProduct(Competitor comp, StartingProduct sp, IRng rng)
    {
        var productId = new ProductId(_productState.nextProductId++);

        float phaseQuality = Clamp(sp.quality, 5f, 100f);

        CompetitorArchetypeConfig archetypeConfig = GetArchetypeConfig(comp.Archetype);
        int devMonthsMin = archetypeConfig != null ? (int)archetypeConfig.releaseIntervalMonthsMin : 3;
        int devMonthsMax = archetypeConfig != null ? (int)archetypeConfig.releaseIntervalMonthsMax : 8;
        int devMonths = rng.Range(devMonthsMin, devMonthsMax + 1);
        if (devMonths < 2) devMonths = 2;
        int devTicks = devMonths * TimeState.TicksPerDay * 30;

        var phase = new ProductPhaseRuntime
        {
            phaseType = ProductPhaseType.Programming,
            phaseQuality = phaseQuality,
            totalWorkRequired = 1000f,
            workCompleted = 1000f,
            isComplete = true
        };

        ProductNiche resolvedNiche = IsTemplateForCategoryTier2(sp.category) ? ProductNiche.None : sp.niche;
        string templateId = resolvedNiche != ProductNiche.None
            ? FindTemplateForNiche(resolvedNiche)
            : FindTemplateForCategory(sp.category);

        string[] featureIds = sp.featureIds != null && sp.featureIds.Length > 0
            ? sp.featureIds
            : SelectStartingFeatures(comp, resolvedNiche, sp.category, rng);

        var product = new Product
        {
            Id = productId,
            ProductName = sp.productName,
            OwnerCompanyId = comp.Id.ToCompanyId(),
            IsInDevelopment = false,
            IsShipped = true,
            IsOnMarket = true,
            CreationTick = 0,
            TargetReleaseTick = 0,
            Phases = new ProductPhaseRuntime[] { phase },
            TeamAssignments = new Dictionary<ProductTeamRole, TeamId>(),
            ActiveUserCount = 0,
            OverallQuality = phaseQuality,
            PopularityScore = ProductLaunchEngine.ComputePopularityScore(phaseQuality),
            MonthlyRevenue = 0,
            LifecycleStage = sp.ageInMonths >= 18 ? ProductLifecycleStage.Decline
                           : sp.ageInMonths >= 10 ? ProductLifecycleStage.Plateau
                           : sp.ageInMonths >= 4  ? ProductLifecycleStage.Growth
                                                  : ProductLifecycleStage.Launch,
            Niche = resolvedNiche,
            Category = sp.category,
            TemplateId = templateId,
            SelectedFeatureIds = featureIds,
            TotalDevelopmentTicks = 0,
            DroppedFeatureIds = new List<string>(),
            SequelIds = new List<ProductId>(),
            ShipTick = 0,
            IsSubscriptionBased = (templateId != null && _templateLookup != null
                && _templateLookup.TryGetValue(templateId, out var subTemplate)
                && subTemplate.economyConfig != null && subTemplate.economyConfig.isSubscriptionBased)
        };

        int ageInTicks = sp.ageInMonths * TimeState.TicksPerDay * 30;
        product.ShipTick = -ageInTicks;
        product.CreationTick = -ageInTicks - devTicks;
        product.TicksSinceShip = ageInTicks;
        int graceTicks = 6 * TimeState.TicksPerDay * 30;
        product.WorldStartSunsetGraceUntilTick = graceTicks;

        product.MaintenanceBudgetMonthly = sp.maintenanceBudgetMonthly;
        product.IsMaintained = sp.maintenanceBudgetMonthly > 0;
        if (product.IsMaintained) {
            const float budgetRef = 5000f;
            float budgetMult = (float)(Math.Log10(sp.maintenanceBudgetMonthly + 1) / Math.Log10(budgetRef + 1));
            if (budgetMult < 0.1f) budgetMult = 0.1f;
            if (budgetMult > 2.0f) budgetMult = 2.0f;
            product.MaintenanceQuality = Math.Clamp(budgetMult * 50f, 10f, 80f);
        }

        if (templateId != null && _templateLookup != null && _templateLookup.TryGetValue(templateId, out var tailTemplate)
            && tailTemplate.economyConfig != null) {
            var cfg = tailTemplate.economyConfig;
            float dailyDecay = cfg.tailDecayRate / 30f;
            if (product.IsMaintained)
                dailyDecay = Math.Max(0f, dailyDecay - cfg.maintenancePopDecayReduction / 30f);
            product.TailDecayFactor = Math.Max(cfg.minTailFactor, (float)Math.Pow(1f - dailyDecay, sp.ageInMonths * 30));
        } else {
            product.TailDecayFactor = Math.Max(0.15f, (float)Math.Pow(0.997, sp.ageInMonths * 30));
        }
        product.TotalDevelopmentTicks = EstimateDevTicks(templateId, phaseQuality);
        product.FeatureRelevanceAtShip = ComputeCompetitorFeatureRelevance(product, phaseQuality);

        float bugBase = (1f - phaseQuality / 100f) * 12f;
        float bugNoise = rng.NextFloat01() * 3f;
        product.BugsRemaining = bugBase + bugNoise;

        if (_reviewSystem != null && templateId != null && _templateLookup != null
            && _templateLookup.TryGetValue(templateId, out var reviewTemplate)) {
            product.ReviewResult = _reviewSystem.GenerateReviews(product, reviewTemplate, product.FeatureRelevanceAtShip);
            product.PublicReceptionScore = product.ReviewResult.AggregateScore;
        }

        // Pre-populate monthly snapshots so competitor products don't show "New" at game start
        product.HasCompletedFirstMonth = true;
        product.SnapshotMonthlyTrend = "Stable";

        _productState.shippedProducts[productId] = product;
        comp.ActiveProductIds.Add(productId);

        if (sp.category.IsTool())
        {
            product.DistributionModel = sp.licensingRate > 0f
                ? ToolDistributionModel.Licensed
                : ToolDistributionModel.OpenSource;
            product.PlayerLicensingRate = sp.licensingRate;
            _productState.shippedProducts[productId] = product;
        }

        if (sp.marketSharePercent > 0f && _platformState != null && sp.category.IsPlatform())
        {
            var entry = new PlatformMarketEntry
            {
                PlatformId = productId,
                OwnerId = comp.Id,
                MarketSharePercent = sp.marketSharePercent,
                InstallBase = (int)(sp.marketSharePercent * 10000f),
                EcosystemProductCount = 0,
                LicensingRate = sp.licensingRate,
                QualityCeiling = phaseQuality
            };
            _platformState.platformShares[productId] = entry;
        }

        if (comp.NicheMarketShare == null)
            comp.NicheMarketShare = new Dictionary<ProductNiche, float>();
        if (sp.marketSharePercent > 0f)
            comp.NicheMarketShare[sp.niche] = sp.marketSharePercent;
    }

    private void CreateStartingDevProduct(Competitor comp, StartingDevProduct sdp)
    {
        var productId = new ProductId(_productState.nextProductId++);

        int devTicks = sdp.devMonthsRemaining * 30 * TimeState.TicksPerDay;
        int targetReleaseTick = devTicks;

        ProductNiche resolvedNiche = IsTemplateForCategoryTier2(sdp.category) ? ProductNiche.None : sdp.niche;
        string templateId = resolvedNiche != ProductNiche.None
            ? FindTemplateForNiche(resolvedNiche)
            : FindTemplateForCategory(sdp.category);

        string[] featureIds = sdp.featureIds != null && sdp.featureIds.Length > 0
            ? sdp.featureIds
            : null;

        ProductPhaseRuntime[] phases;
        if (_productSystem != null && templateId != null)
        {
            phases = _productSystem.BuildPhasesForTemplate(templateId, featureIds, 1f);
        }
        else
        {
            phases = new ProductPhaseRuntime[]
            {
                new ProductPhaseRuntime
                {
                    phaseType = ProductPhaseType.Programming,
                    phaseQuality = 0f,
                    totalWorkRequired = 1000f,
                    workCompleted = 0f,
                    isComplete = false,
                    isUnlocked = true
                }
            };
        }

        var product = new Product
        {
            Id = productId,
            ProductName = sdp.productName,
            OwnerCompanyId = comp.Id.ToCompanyId(),
            IsInDevelopment = true,
            IsShipped = false,
            IsOnMarket = false,
            CreationTick = -devTicks,
            TargetReleaseTick = targetReleaseTick,
            OriginalReleaseTick = targetReleaseTick,
            HasAnnouncedReleaseDate = true,
            TemplateId = templateId,
            Phases = phases,
            TeamAssignments = new Dictionary<ProductTeamRole, TeamId>(),
            ActiveUserCount = 0,
            OverallQuality = 0f,
            PopularityScore = 0f,
            MonthlyRevenue = 0,
            LifecycleStage = ProductLifecycleStage.PreLaunch,
            Niche = resolvedNiche,
            Category = sdp.category,
            SelectedFeatureIds = featureIds,
            TotalDevelopmentTicks = 0,
            DroppedFeatureIds = new List<string>(),
            SequelIds = new List<ProductId>()
        };

        _productState.developmentProducts[productId] = product;
        comp.InDevelopmentProductIds.Add(productId);

        _logger.Log($"[CompetitorSystem] Created in-dev product '{sdp.productName}' for '{comp.CompanyName}' (releases tick {targetReleaseTick}).");
    }

    private void AutoSeedPlatformApplications(Competitor comp, IRng rng)
    {
        if (_productState == null || _platformState == null) return;

        int activeCount = comp.ActiveProductIds.Count;
        for (int pi = 0; pi < activeCount; pi++)
        {
            ProductId ownedId = comp.ActiveProductIds[pi];
            if (!_productState.shippedProducts.TryGetValue(ownedId, out var ownedProduct)) continue;
            if (!ownedProduct.Category.IsPlatform()) continue;

            bool hasTargetingProduct = false;
            foreach (var kvp in _productState.shippedProducts)
            {
                var candidate = kvp.Value;
                if (!candidate.Category.IsApplication()) continue;
                if (candidate.TargetPlatformIds == null) continue;
                for (int ti = 0; ti < candidate.TargetPlatformIds.Length; ti++)
                {
                    if (candidate.TargetPlatformIds[ti].Value == ownedId.Value)
                    {
                        hasTargetingProduct = true;
                        break;
                    }
                }
                if (hasTargetingProduct) break;
            }

            if (hasTargetingProduct) continue;

            float sharePercent = 0f;
            if (_platformState.platformShares.TryGetValue(ownedId, out var shareEntry))
                sharePercent = shareEntry.MarketSharePercent;

            int seedCount = Math.Max(2, (int)(sharePercent / 100f * 8f));

            for (int si = 0; si < seedCount; si++)
            {
                ProductCategory seedCategory = PickSeedCategory(ownedProduct.Category, rng);
                ProductNiche seedNiche = PickSeedNiche(seedCategory, rng);

                var sp = new StartingProduct
                {
                    category = seedCategory,
                    niche = seedNiche,
                    productName = GenerateProductName(comp, seedNiche, seedCategory),
                    quality = rng.Range(40, 76),
                    marketSharePercent = 0f,
                    ageInMonths = rng.Range(3, 25),
                    licensingRate = 0f,
                    featureIds = null,
                    featureQualities = null,
                    maintenanceBudgetMonthly = (long)(rng.Range(40, 76) * 30)
                };

                CreateStartingProduct(comp, sp, rng);

                ProductId newId = comp.ActiveProductIds[comp.ActiveProductIds.Count - 1];
                if (_productState.shippedProducts.TryGetValue(newId, out var newProduct))
                {
                    newProduct.TargetPlatformIds = new ProductId[] { ownedId };
                    _productState.shippedProducts[newId] = newProduct;
                }
            }
        }
    }

    private void BackfillToolAndPlatformDependencies()
    {
        if (_productState == null) return;

        var productIds = new List<ProductId>(_productState.shippedProducts.Count);
        foreach (var kvp in _productState.shippedProducts)
            productIds.Add(kvp.Key);

        productIds.Sort((a, b) => a.Value.CompareTo(b.Value));

        int count = productIds.Count;
        for (int i = 0; i < count; i++)
        {
            if (!_productState.shippedProducts.TryGetValue(productIds[i], out var product)) continue;
            if (!product.IsCompetitorProduct) continue;
            if (!product.Category.IsApplication()) continue;
            if (product.OwnerCompanyId.IsPlayer) continue;
            if (!_state.competitors.TryGetValue(product.OwnerCompanyId.ToCompetitorId(), out var comp)) continue;

            bool changed = false;
            if (product.RequiredToolIds == null)
            {
                product.RequiredToolIds = SelectToolsForCompetitor(comp, product.Category, product.Niche, 0);
                changed = true;
            }
            if (product.TargetPlatformIds == null)
            {
                product.TargetPlatformIds = SelectPlatformsForCompetitor(comp, product.Category, product.Niche, 0);
                changed = true;
            }
            if (changed)
                _productState.shippedProducts[productIds[i]] = product;
        }
    }

    public void MigrateGhostProducts()
    {
        if (_productState == null) return;

        var ghostIds = new List<ProductId>();
        foreach (var kvp in _productState.shippedProducts)
        {
            var product = kvp.Value;
            if (!product.IsOnMarket)
                ghostIds.Add(kvp.Key);
        }

        int count = ghostIds.Count;
        for (int i = 0; i < count; i++)
        {
            ProductId id = ghostIds[i];
            if (!_productState.shippedProducts.TryGetValue(id, out var product)) continue;
            _productState.shippedProducts.Remove(id);
            _productState.archivedProducts[id] = product;
            _logger.Log($"[CompetitorSystem] Migrated ghost product {id.Value} to archivedProducts.");
        }
    }

    private ProductCategory PickSeedCategory(ProductCategory platformCategory, IRng rng)
    {
        if (platformCategory == ProductCategory.GameConsole)
            return ProductCategory.VideoGame;

        int roll = rng.Range(0, 100);
        if (roll < 70) return ProductCategory.VideoGame;
        return ProductCategory.GameEngine;
    }

    private ProductNiche PickSeedNiche(ProductCategory category, IRng rng)
    {
        switch (category)
        {
            case ProductCategory.VideoGame:
            {
                var videoGameNiches = new ProductNiche[]
                {
                    ProductNiche.RPG, ProductNiche.FPS, ProductNiche.Strategy,
                    ProductNiche.Puzzle, ProductNiche.Platformer, ProductNiche.Simulation,
                    ProductNiche.Adventure, ProductNiche.Sports
                };
                return videoGameNiches[rng.Range(0, videoGameNiches.Length)];
            }
            default:
                return ProductNiche.None;
        }
    }

    private void GenerateRandomCompetitors(int count, int masterSeed)
    {
        IRng genRng = new RngStream(masterSeed ^ "competitor-gen".GetHashCode());

        var nicheKeys = new List<ProductNiche>();
        if (_marketState != null && _marketState.nicheDemand != null)
        {
            foreach (var kvp in _marketState.nicheDemand)
                nicheKeys.Add(kvp.Key);
            int keyCount = nicheKeys.Count;
            for (int i = keyCount - 1; i > 0; i--)
            {
                int j = genRng.Range(0, i + 1);
                ProductNiche tmp = nicheKeys[i];
                nicheKeys[i] = nicheKeys[j];
                nicheKeys[j] = tmp;
            }
        }

        for (int i = 0; i < count; i++)
        {
            ProductNiche? forced = (nicheKeys.Count > 0 && i < nicheKeys.Count)
                ? (ProductNiche?)nicheKeys[i]
                : null;
            GenerateCompetitor(genRng, 0, forced);
        }
    }

    public Competitor GetCompetitor(CompetitorId id)
    {
        _state.competitors.TryGetValue(id, out var comp);
        return comp;
    }

    public IReadOnlyDictionary<CompetitorId, Competitor> GetAllCompetitors()
    {
        return _state.competitors;
    }

    public List<CompetitorId> GetActiveCompetitorIds()
    {
        var result = new List<CompetitorId>(_state.competitors.Count);
        foreach (var kvp in _state.competitors)
        {
            if (!kvp.Value.IsBankrupt && !kvp.Value.IsAbsorbed)
                result.Add(kvp.Key);
        }
        return result;
    }

    public void PreTick(int tick) { }

    public void Tick(int tick)
    {
        _lastKnownTick = tick;
        bool isMonthlyBoundary = IsMonthlyBoundary(tick);
        if (!isMonthlyBoundary) return;

        _scratchIds.Clear();
        foreach (var kvp in _state.competitors)
        {
            if (!kvp.Value.IsBankrupt && !kvp.Value.IsAbsorbed)
                _scratchIds.Add(kvp.Key);
        }

        int count = _scratchIds.Count;
        for (int i = 0; i < count; i++)
        {
            CompetitorId id = _scratchIds[i];
            if (!_state.competitors.TryGetValue(id, out var comp)) continue;
            if (comp.IsBankrupt || comp.IsAbsorbed) continue;

            if (comp.ActiveProductIds != null)
            {
                for (int j = comp.ActiveProductIds.Count - 1; j >= 0; j--)
                {
                    Product p = GetShippedProduct(comp.ActiveProductIds[j]);
                    if (p == null || !p.IsOnMarket)
                        comp.ActiveProductIds.RemoveAt(j);
                }
            }

            EvaluateMonthlyFinances(comp, tick);
            EvaluateCompetitorTax(comp, tick);
            EvaluatePlatformMaintenance(comp, tick);
            ProcessScheduledUpdates(comp, tick);
        }

        _bankruptPendingIds.Clear();
        for (int i = 0; i < count; i++)
        {
            CompetitorId id = _scratchIds[i];
            if (!_state.competitors.TryGetValue(id, out var comp)) continue;
            if (comp.IsBankrupt || comp.IsAbsorbed) continue;

            if (comp.Finance.ConsecutiveNegativeCashMonths >= BankruptcyMonthsThreshold)
                _bankruptPendingIds.Add(id);
        }

        int bankruptCount = _bankruptPendingIds.Count;
        for (int i = 0; i < bankruptCount; i++)
            ProcessBankruptcy(_bankruptPendingIds[i], tick);

        CheckSpawnConditions(tick);
        _lastMonthlyEvalTick = tick;
    }

    public void PostTick(int tick)
    {
        int count = _pendingEvents.Count;
        for (int i = 0; i < count; i++)
        {
            PendingCompetitorEvent e = _pendingEvents[i];
            switch (e.Type)
            {
                case PendingEventType.Spawned:          OnCompetitorSpawned?.Invoke(e.CompId); break;
                case PendingEventType.Bankrupt:         OnCompetitorBankrupt?.Invoke(e.CompId); break;
                case PendingEventType.ProductLaunched:  OnCompetitorProductLaunched?.Invoke(e.CompId, e.ProductId); break;
                case PendingEventType.ProductSunset:    OnCompetitorProductSunset?.Invoke(e.CompId, e.ProductId); break;
                case PendingEventType.FinancesChanged:  OnCompetitorFinancesChanged?.Invoke(e.CompId); break;
                case PendingEventType.DevStarted:       OnCompetitorDevStarted?.Invoke(e.CompId, e.ProductId); break;
            }
        }
        _pendingEvents.Clear();
    }

    public void ApplyCommand(ICommand command) { }

    public void Dispose()
    {
        if (_productSystem != null)
        {
            if (_onProductShippedHandler != null)
                _productSystem.OnProductShipped -= _onProductShippedHandler;
        }
        _onProductShippedHandler = null;

        _pendingEvents.Clear();
        OnCompetitorSpawned = null;
        OnCompetitorBankrupt = null;
        OnCompetitorAbsorbed = null;
        OnCompetitorProductLaunched = null;
        OnCompetitorProductSunset = null;
        OnCompetitorProductUpdated = null;
        OnCompetitorFinancesChanged = null;
        OnCompetitorDevStarted = null;
    }

    private void OnCompetitorProductShipped(ProductId productId, float quality)
    {
        if (!_productState.shippedProducts.TryGetValue(productId, out var product)) return;
        if (!product.IsCompetitorProduct) return;

        CompetitorId compId = product.OwnerCompanyId.ToCompetitorId();
        if (!_state.competitors.TryGetValue(compId, out var comp)) return;

        comp.InDevelopmentProductIds?.Remove(productId);
        if (comp.ActiveProductIds == null) comp.ActiveProductIds = new List<ProductId>();
        if (!comp.ActiveProductIds.Contains(productId))
            comp.ActiveProductIds.Add(productId);

        int repGain = ProductLaunchEngine.ComputeLaunchReputation(quality, (int)product.LaunchRevenue, 5f);
        comp.ReputationPoints += repGain;

        int fanGain = (int)(quality * 0.5f * comp.ReputationPoints / 1000f);
        comp.CompanyFans += fanGain;

        if (quality >= 70f) {
            comp.FanSentiment = Clamp(comp.FanSentiment + (quality - 70f) * 0.5f, 0f, 100f);
        } else if (quality < 40f) {
            comp.FanSentiment = Clamp(comp.FanSentiment - (40f - quality) * 1.5f, 0f, 100f);
        }

        _pendingEvents.Add(new PendingCompetitorEvent { Type = PendingEventType.ProductLaunched, CompId = compId, ProductId = productId });
        _logger.Log($"[CompetitorSystem] {comp.CompanyName} product '{product.ProductName}' shipped via ProductSystem (quality: {quality:F1}).");
    }

    private bool IsMonthlyBoundary(int tick)
    {
        if (_timeSystem == null) return false;
        int dayOfTick = tick / TimeState.TicksPerDay;
        if (dayOfTick <= 0) return false;
        int prevDayOfTick = (tick - 1) / TimeState.TicksPerDay;
        int currentMonth = TimeState.GetMonth(dayOfTick);
        int prevMonth = TimeState.GetMonth(prevDayOfTick);
        return currentMonth != prevMonth;
    }

    private void EvaluateCompetitorTax(Competitor comp, int tick)
    {
        if (_taxSystem == null) return;

        if (comp.Finance.MonthlyProfit > 0)
            comp.TaxRecord.profitSinceLastCycle += comp.Finance.MonthlyProfit;

        int nextDueTick = _taxSystem.NextDueTick;

        if (tick >= nextDueTick && comp.TaxRecord.hasPendingTax)
        {
            long unpaid = comp.TaxRecord.pendingTaxAmount + comp.TaxRecord.pendingLateFees;
            long payment = Math.Min(unpaid, Math.Max(0L, comp.Finance.Cash));
            comp.Finance.Cash = Clamp(comp.Finance.Cash - payment, MinCashFloor, MaxCashCeiling);
            if (payment < unpaid)
                _logger.Log($"[CompetitorSystem] {comp.CompanyName} partial tax payment: paid {payment} of {unpaid}. Cash now {comp.Finance.Cash}.");
            else
                _logger.Log($"[CompetitorSystem] {comp.CompanyName} overdue tax collected: {payment}.");
            comp.TaxRecord.hasPendingTax = false;
            comp.TaxRecord.pendingTaxAmount = 0;
            comp.TaxRecord.pendingLateFees = 0;
            comp.TaxRecord.overdueMonthsApplied = 0;
            comp.TaxRecord.plannedPaymentTick = 0;
        }

        if (tick >= nextDueTick && !comp.TaxRecord.hasPendingTax)
        {
            long taxOwed = (long)(comp.TaxRecord.profitSinceLastCycle * _taxSystem.TaxRate);
            comp.TaxRecord.pendingTaxAmount = taxOwed;
            comp.TaxRecord.hasPendingTax = true;
            comp.TaxRecord.pendingTaxDueTick = tick;
            comp.TaxRecord.overdueMonthsApplied = 0;
            comp.TaxRecord.pendingLateFees = 0;
            comp.TaxRecord.profitSinceLastCycle = 0;

            int roll = _rng.Range(0, 100);
            int paymentDelay;
            if (roll < 60)
                paymentDelay = _rng.Range(0, 30);
            else if (roll < 85)
                paymentDelay = _rng.Range(30, 60);
            else if (roll < 95)
                paymentDelay = _rng.Range(60, 90);
            else
                paymentDelay = _rng.Range(90, 120);

            comp.TaxRecord.plannedPaymentTick = tick + paymentDelay * TimeState.TicksPerDay;
            _logger.Log($"[CompetitorSystem] {comp.CompanyName} tax due. Amount: {taxOwed}. Planned payment tick: {comp.TaxRecord.plannedPaymentTick}");
        }

        if (comp.TaxRecord.hasPendingTax && comp.TaxRecord.pendingTaxAmount > 0)
        {
            int monthsOverdue = (tick - comp.TaxRecord.pendingTaxDueTick) / (30 * TimeState.TicksPerDay);
            if (monthsOverdue > comp.TaxRecord.overdueMonthsApplied && comp.TaxRecord.overdueMonthsApplied < 3)
            {
                comp.TaxRecord.overdueMonthsApplied++;
                long fee = 0L;
                if (comp.TaxRecord.overdueMonthsApplied == 1)
                    fee = (long)(comp.TaxRecord.pendingTaxAmount * 0.02f);
                else if (comp.TaxRecord.overdueMonthsApplied == 2)
                    fee = (long)(comp.TaxRecord.pendingTaxAmount * 0.05f);
                else if (comp.TaxRecord.overdueMonthsApplied == 3)
                    fee = (long)(comp.TaxRecord.pendingTaxAmount * 0.10f);
                comp.TaxRecord.pendingLateFees += fee;
            }
        }

        if (comp.TaxRecord.hasPendingTax && tick >= comp.TaxRecord.plannedPaymentTick && comp.TaxRecord.plannedPaymentTick > 0)
        {
            long totalPayment = comp.TaxRecord.pendingTaxAmount + comp.TaxRecord.pendingLateFees;
            comp.Finance.Cash = Clamp(comp.Finance.Cash - totalPayment, MinCashFloor, MaxCashCeiling);
            _logger.Log($"[CompetitorSystem] {comp.CompanyName} paid tax. Total: {totalPayment}");
            comp.TaxRecord.hasPendingTax = false;
            comp.TaxRecord.pendingTaxAmount = 0;
            comp.TaxRecord.pendingLateFees = 0;
            comp.TaxRecord.overdueMonthsApplied = 0;
            comp.TaxRecord.plannedPaymentTick = 0;
        }
    }

    private void EvaluateMonthlyFinances(Competitor comp, int tick)
    {
        long revenue = EstimateMonthlyRevenue(comp);

        long totalSalaries = _employeeSystem != null
            ? (long)_employeeSystem.TotalMonthlySalariesForCompany(comp.Id.ToCompanyId())
            : (long)(comp.EmployeeIds != null ? comp.EmployeeIds.Count * SalaryBand.GetBase(EmployeeRole.Developer) : 0);

        long budgetCoveredSalaries = 0L;
        long productBudgets = 0L;
        long infraCost = 0L;

        int prodCount = comp.ActiveProductIds != null ? comp.ActiveProductIds.Count : 0;
        for (int i = 0; i < prodCount; i++)
        {
            Product product = GetShippedProduct(comp.ActiveProductIds[i]);
            if (product == null || !product.IsOnMarket) continue;
            productBudgets += product.MaintenanceBudgetMonthly + product.MarketingBudgetMonthly;
            infraCost += product.ActiveUserCount / 1000;
        }

        long unassignedSalaries = System.Math.Max(0L, totalSalaries - budgetCoveredSalaries);
        long expenses = unassignedSalaries + productBudgets + infraCost;
        long profit = revenue - expenses;

        comp.Finance.MonthlyRevenue = revenue;
        comp.Finance.MonthlyExpenses = expenses;
        comp.Finance.MonthlyProfit = profit;
        comp.Finance.Cash = Clamp(comp.Finance.Cash + profit, MinCashFloor, MaxCashCeiling);

        comp.CompanyFans -= (int)(comp.CompanyFans * 0.01f);
        if (comp.CompanyFans < 0) comp.CompanyFans = 0;

        if (comp.Finance.Cash <= 0)
        {
            comp.Finance.ConsecutiveNegativeCashMonths++;
        }
        else if (profit < 0)
        {
            long absProfit = -profit;
            long monthsOfRunway = absProfit > 0 ? comp.Finance.Cash / absProfit : 999;
            if (monthsOfRunway < 3)
                comp.Finance.ConsecutiveNegativeCashMonths++;
            else
                comp.Finance.ConsecutiveNegativeCashMonths = comp.Finance.ConsecutiveNegativeCashMonths > 0
                    ? comp.Finance.ConsecutiveNegativeCashMonths - 1 : 0;
        }
        else
        {
            comp.Finance.ConsecutiveNegativeCashMonths = 0;
        }

        comp.LastFinanceEvalTick = tick;

        _pendingEvents.Add(new PendingCompetitorEvent { Type = PendingEventType.FinancesChanged, CompId = comp.Id });

        if (revenue > 0)
        {
            int revenueScale = 0;
            if (revenue > 1000000L) revenueScale = 3;
            else if (revenue > 100000L) revenueScale = 2;
            else if (revenue > 10000L) revenueScale = 1;
            comp.ReputationPoints += revenueScale;
        }
        else if (comp.ActiveProductIds == null || comp.ActiveProductIds.Count == 0)
        {
            comp.ReputationPoints -= 5;
            if (comp.ReputationPoints < 0) comp.ReputationPoints = 0;
        }

        if (profit > 0 && comp.ActiveProductIds != null && comp.ActiveProductIds.Count >= 3)
            comp.ReputationPoints += 1;
    }

    private long EstimateMonthlyRevenue(Competitor comp)
    {
        long revenue = 0L;
        int count = comp.ActiveProductIds != null ? comp.ActiveProductIds.Count : 0;
        for (int i = 0; i < count; i++)
        {
            ProductId pid = comp.ActiveProductIds[i];
            Product product = GetShippedProduct(pid);
            if (product == null || !product.IsOnMarket) continue;
            long productRevenue = product.MonthlyRevenue;

            if (product.RequiredToolIds != null) {
                for (int t = 0; t < product.RequiredToolIds.Length; t++) {
                    var tool = GetShippedProduct(product.RequiredToolIds[t]);
                    if (tool == null) continue;
                    if (tool.DistributionModel != ToolDistributionModel.Licensed) continue;
                    if (tool.PlayerLicensingRate <= 0f) continue;
                    if (tool.OwnerCompanyId == comp.Id.ToCompanyId()) continue;
                    productRevenue -= (long)(product.MonthlyRevenue * tool.PlayerLicensingRate);
                }
            }
            if (product.TargetPlatformIds != null) {
                for (int p = 0; p < product.TargetPlatformIds.Length; p++) {
                    var plat = GetShippedProduct(product.TargetPlatformIds[p]);
                    if (plat == null) continue;
                    if (plat.DistributionModel != ToolDistributionModel.Licensed) continue;
                    if (plat.PlayerLicensingRate <= 0f) continue;
                    if (plat.OwnerCompanyId == comp.Id.ToCompanyId()) continue;
                    productRevenue -= (long)(product.MonthlyRevenue * plat.PlayerLicensingRate);
                }
            }
            revenue += productRevenue > 0 ? productRevenue : 0;
        }
        return revenue;
    }

    private static ProductCategory NicheToCategory(ProductNiche niche)
    {
        switch (niche)
        {
            case ProductNiche.DesktopOS:       return ProductCategory.OperatingSystem;
            case ProductNiche.RPG:
            case ProductNiche.FPS:
            case ProductNiche.Strategy:
            case ProductNiche.Puzzle:
            case ProductNiche.Platformer:
            case ProductNiche.Simulation:
            case ProductNiche.Racing:
            case ProductNiche.Sports:
            case ProductNiche.Horror:
            case ProductNiche.Adventure:
            case ProductNiche.MMORPG:
            case ProductNiche.Sandbox:
            case ProductNiche.Fighting:        return ProductCategory.VideoGame;
            default:                           return ProductCategory.VideoGame;
        }
    }

    private bool IsTemplateForCategoryTier2(ProductCategory category)
    {
        foreach (var kvp in _templateLookup)
        {
            var tmpl = kvp.Value;
            if (tmpl.category == category && !tmpl.HasNiches)
                return true;
        }
        return false;
    }

    private string FindTemplateForNiche(ProductNiche niche)
    {
        if (_templateLookup == null) return null;
        foreach (var kvp in _templateLookup)
            if (kvp.Key.Contains(niche.ToString())) return kvp.Key;
        foreach (var kvp in _templateLookup)
            return kvp.Key;
        return null;
    }

    private string FindTemplateForCategory(ProductCategory category)
    {
        if (_templateLookup == null) return null;
        foreach (var kvp in _templateLookup)
            if (kvp.Value.category == category) return kvp.Key;
        return null;
    }

    public void InstantShipCompetitorProduct(Competitor comp, ProductId productId, int tick)
    {
        Product product = GetDevProduct(productId);
        if (product == null) return;

        float finalQuality = 50f;
        CompetitorArchetypeConfig archetypeCfg = GetArchetypeConfig(comp.Archetype);
        if (archetypeCfg != null)
        {
            float baseSkill = (archetypeCfg.baseSkillRange.x + archetypeCfg.baseSkillRange.y) * 0.5f;
            float skillNormalized = baseSkill / 20f;
            float rdFactor = 0.6f + comp.Personality.RdSpeed * 0.4f;
            float innovationBonus = comp.Personality.InnovationBias * 0.15f;
            float rawQuality = (skillNormalized * rdFactor + innovationBonus) * 100f;
            float variance = _rng.NextFloat01() * 20f - 10f;
            finalQuality = Clamp(rawQuality + variance, 25f, 95f);
        }

        int selectedCount = product.SelectedFeatureIds?.Length ?? 0;
        int optimalCount = 3;
        if (product.TemplateId != null && _templateLookup.TryGetValue(product.TemplateId, out var scopeTemplate))
            optimalCount = Math.Max(3, (scopeTemplate.availableFeatures?.Length ?? 9) / 3);

        if (selectedCount > optimalCount) {
            float scopeRatio = (float)selectedCount / optimalCount;
            float scopeExponent = _tuning?.ScopeComplexityExponent ?? 0.6f;
            float scopePenalty = 1f / (float)Math.Pow(scopeRatio, scopeExponent);
            finalQuality *= scopePenalty;
        }

        float eraBonus = comp.CompetitorEra * 2f;
        finalQuality = Clamp(finalQuality + eraBonus, 5f, 100f);

        product.OverallQuality = finalQuality;
        product.IsShipped = true;
        product.ShipTick = tick;
        product.IsOnMarket = true;
        product.IsInDevelopment = false;
        product.LifecycleStage = ProductLifecycleStage.Launch;
        product.HasAnnouncedReleaseDate = false;
        product.TotalDevelopmentTicks = EstimateDevTicks(product.TemplateId, finalQuality);

        float maintenanceRatio = archetypeCfg != null ? archetypeCfg.maintenanceBudgetRatio : 0.1f;
        long initialBudget = (long)(comp.Finance.MonthlyRevenue * maintenanceRatio / Math.Max(1, comp.ActiveProductIds.Count + 1));
        product.MaintenanceBudgetMonthly = initialBudget;
        product.IsMaintained = initialBudget > 0;
        if (product.IsMaintained) {
            const float budgetRef = 5000f;
            float budgetMult = (float)(Math.Log10(initialBudget + 1) / Math.Log10(budgetRef + 1));
            if (budgetMult < 0.1f) budgetMult = 0.1f;
            if (budgetMult > 2.0f) budgetMult = 2.0f;
            product.MaintenanceQuality = Math.Clamp(budgetMult * 50f, 10f, 80f);
        }

        if (product.TemplateId != null && _templateLookup.TryGetValue(product.TemplateId, out var featureTemplate))
            _productSystem.InitializeFeatureStates(product, featureTemplate, finalQuality);

        int devMonths = Math.Max(1, product.TotalDevelopmentTicks / (TimeState.TicksPerDay * 30));
        int employeeCount = comp.EmployeeIds?.Count ?? 5;
        product.AccumulatedSalaryCost = (long)(devMonths * employeeCount * 5000);

        float bugBase = (1f - finalQuality / 100f) * 12f;
        product.BugsRemaining = bugBase + _rng.NextFloat01() * 3f;

        ReputationTier compTier = ReputationSystem.CalculateTier(comp.ReputationPoints, null);
        float reputationMult = ReputationSystem.GetLaunchMultForTier(compTier);
        float marketDemandMult = _marketSystem?.GetCombinedDemandMultiplier(product) ?? 1.0f;
        float fanLaunchMult = ProductLaunchEngine.ComputeFanAppealBonus(comp.CompanyFans, comp.FanSentiment, 50000f);

        ProductEconomyConfig econ = null;
        if (product.TemplateId != null && _templateLookup.TryGetValue(product.TemplateId, out var tmpl))
            econ = tmpl.economyConfig;
        int launchSalesBase = econ?.launchSalesBase ?? 5000;

        int baseSales = ProductLaunchEngine.ComputeLaunchSales(
            finalQuality, launchSalesBase, reputationMult, 0.8f, marketDemandMult, 1.0f, fanLaunchMult, 1.0f);

        ProductLaunchEngine.RollBreakout(
            finalQuality, product.PopularityScore, compTier,
            econ?.breakoutBaseChance ?? 0.02f,
            econ?.breakoutMinMultiplier ?? 2f,
            econ?.breakoutMaxMultiplier ?? 5f,
            baseSales, _rng,
            out int finalSales, out bool isBreakout, out float breakoutMult, out int breakoutDays);

        product.IsBreakout = isBreakout;
        product.BreakoutMultiplier = breakoutMult;
        product.BreakoutDaysRemaining = breakoutDays;
        product.ActiveUserCount = finalSales;
        product.TotalUnitsSold = finalSales;
        product.PeakMonthlySales = finalSales;

        float pricePerUnit = product.PriceOverride > 0f ? product.PriceOverride : (econ?.pricePerUnit ?? 20f);
        int launchRevenue = (int)(finalSales * pricePerUnit);
        product.LaunchRevenue = launchRevenue;
        product.MonthlyRevenue = launchRevenue;
        product.AccumulatedMonthlyRevenue = launchRevenue;
        product.DailyRevenue = launchRevenue / 30;
        product.TotalLifetimeRevenue = launchRevenue;
        product.PopularityScore = ProductLaunchEngine.ComputePopularityScore(finalQuality);
        product.FeatureRelevanceAtShip = ComputeCompetitorFeatureRelevance(product, finalQuality);
        product.FanAppealBonus = fanLaunchMult;

        if (_reviewSystem != null && product.TemplateId != null && _templateLookup.TryGetValue(product.TemplateId, out var reviewTemplate)) {
            product.ReviewResult = _reviewSystem.GenerateReviews(product, reviewTemplate, product.FeatureRelevanceAtShip);
            product.PublicReceptionScore = product.ReviewResult.AggregateScore;
        }

        int repGain = ProductLaunchEngine.ComputeLaunchReputation(finalQuality, launchRevenue, 5f);
        comp.ReputationPoints += repGain;
        int fanGain = (int)(finalQuality * 0.5f * comp.ReputationPoints / 1000f);
        comp.CompanyFans += fanGain;
        if (finalQuality >= 70f) {
            comp.FanSentiment = Clamp(comp.FanSentiment + (finalQuality - 70f) * 0.5f, 0f, 100f);
        } else if (finalQuality < 40f) {
            comp.FanSentiment = Clamp(comp.FanSentiment - (40f - finalQuality) * 1.5f, 0f, 100f);
        }
        comp.Finance.MonthlyRevenue += launchRevenue;

        if (_marketSystem != null && finalQuality >= 75f)
        {
            float uplift = (finalQuality - 75f) / 25f * MarketSystem.MaxDemandUpliftPerRelease * 0.5f;
            _marketSystem.ApplyNicheUplift(product.Niche, uplift);
        }

        _productState.developmentProducts.Remove(productId);
        _productState.shippedProducts[productId] = product;
        comp.InDevelopmentProductIds.Remove(productId);
        if (comp.ActiveProductIds == null) comp.ActiveProductIds = new List<ProductId>();
        comp.ActiveProductIds.Add(productId);

        if (tick > 0)
            _pendingEvents.Add(new PendingCompetitorEvent { Type = PendingEventType.ProductLaunched, CompId = comp.Id, ProductId = productId });

        _logger.Log($"[CompetitorSystem] {comp.CompanyName} instant-shipped '{product.ProductName}' (quality: {finalQuality:F1}, revenue: {launchRevenue}).");
    }

    private void AssignDevTeamToProduct(Competitor comp, Product product)
    {
        if (_teamSystem == null || _productState == null) return;

        int phaseCount = product.Phases != null ? product.Phases.Length : 0;
        for (int p = 0; p < phaseCount; p++)
        {
            var phase = product.Phases[p];
            ProductTeamRole role = phase.primaryRole;
            if (product.TeamAssignments.ContainsKey(role)) continue;

            TeamType neededType = TeamTypeMapping.ToTeamType(role);
            CompanyId companyId = comp.Id.ToCompanyId();
            var candidates = _teamSystem.GetFreeTeamsByTypeForCompany(neededType, companyId);
            int candidateCount = candidates.Count;
            for (int i = 0; i < candidateCount; i++)
            {
                TeamId teamId = candidates[i];
                if (_productState.teamToProduct.ContainsKey(teamId)) continue;
                product.TeamAssignments[role] = teamId;
                _productState.teamToProduct[teamId] = product.Id;
                break;
            }
        }
    }

    private void ProcessBankruptcy(CompetitorId id, int tick)
    {
        if (!_state.competitors.TryGetValue(id, out var comp)) return;
        if (comp.IsBankrupt) return;

        comp.IsBankrupt = true;

        _productState.developmentProducts.Remove(default);
        if (comp.InDevelopmentProductIds != null)
        {
            int devCount = comp.InDevelopmentProductIds.Count;
            for (int i = 0; i < devCount; i++)
            {
                ProductId pid = comp.InDevelopmentProductIds[i];
                _productState.developmentProducts.Remove(pid);
            }
            comp.InDevelopmentProductIds.Clear();
        }

        if (comp.ActiveProductIds != null)
        {
            int activeCount = comp.ActiveProductIds.Count;
            for (int i = 0; i < activeCount; i++)
            {
                ProductId pid = comp.ActiveProductIds[i];
                if (_productState.shippedProducts.TryGetValue(pid, out var p))
                {
                    if (p.Category.IsCriticalCategory() && _productState.IsLastOnMarketInCategory(pid))
                    {
                        _logger.Log($"[CompetitorSystem] Preserving last {p.Category} product {pid.Value} during {comp.CompanyName} bankruptcy.");
                        continue;
                    }
                    p.IsOnMarket = false;
                    _productState.shippedProducts.Remove(pid);
                    _productState.archivedProducts[pid] = p;
                }
            }
            comp.ActiveProductIds.Clear();
        }

        _pendingEvents.Add(new PendingCompetitorEvent { Type = PendingEventType.Bankrupt, CompId = id });
        _logger.Log($"[CompetitorSystem] {comp.CompanyName} went bankrupt at tick {tick}.");
    }

    private void ProcessSunset(Competitor comp, ProductId productId, int tick)
    {
        Product product = GetShippedProduct(productId);
        if (product == null) return;

        product.IsOnMarket = false;
        comp.ActiveProductIds.Remove(productId);
        _productState.shippedProducts.Remove(productId);
        _productState.archivedProducts[productId] = product;

        _pendingEvents.Add(new PendingCompetitorEvent { Type = PendingEventType.ProductSunset, CompId = comp.Id, ProductId = productId });
        _logger.Log($"[CompetitorSystem] {comp.CompanyName} sunset product {productId.Value}.");
    }

    private float CountCompetitorProductsInNiche(ProductNiche niche)
    {
        float count = 0f;
        foreach (var kvp in _productState.shippedProducts)
        {
            var product = kvp.Value;
            if (!product.IsCompetitorProduct || !product.IsOnMarket) continue;
            if (product.Niche == niche) count += 1f;
        }
        return count;
    }

    private float CountCompetitorProductsInCategory(ProductCategory cat)
    {
        float count = 0f;
        foreach (var kvp in _productState.shippedProducts)
        {
            var product = kvp.Value;
            if (!product.IsCompetitorProduct || !product.IsOnMarket) continue;
            if (product.Category == cat && product.Niche == ProductNiche.None) count += 1f;
        }
        return count;
    }

    private void CheckSpawnConditions(int tick)
    {
        if (ActiveCompetitorCount >= _state.maxCompetitorCap) return;
        if (tick - _state.lastSpawnCheckTick < MonthlyEvalIntervalTicks) return;

        bool shouldSpawn = false;
        int currentGen = _generationSystem != null ? _generationSystem.GetCurrentGeneration() : 1;

        foreach (var kvp in _marketState.nicheDemand)
        {
            float demand = kvp.Value;
            float supply = CountCompetitorProductsInNiche(kvp.Key);
            if (demand > supply * 1.5f)
            {
                shouldSpawn = true;
                break;
            }
        }

        if (!shouldSpawn && _marketState.categoryDemand != null)
        {
            foreach (var kvp in _marketState.categoryDemand)
            {
                float demand = kvp.Value;
                float supply = CountCompetitorProductsInCategory(kvp.Key);
                if (demand > supply * 1.5f)
                {
                    shouldSpawn = true;
                    break;
                }
            }
        }

        if (!shouldSpawn && currentGen > 1)
        {
            shouldSpawn = ShouldSpawnForGenerationShift(currentGen);
        }

        if (shouldSpawn)
        {
            Competitor spawned = GenerateCompetitor(_rng, tick);
            if (spawned != null && currentGen > 1)
            {
                spawned.Personality = new CompetitorPersonality {
                    RiskTolerance = 0.6f + _rng.NextFloat01() * 0.3f,
                    RdSpeed = 0.6f + _rng.NextFloat01() * 0.3f,
                    BrandInvestment = 0.2f + _rng.NextFloat01() * 0.3f,
                    PricingAggression = 0.3f + _rng.NextFloat01() * 0.4f,
                    InnovationBias = 0.65f + _rng.NextFloat01() * 0.3f
                };
                spawned.Finance.Cash = spawned.Finance.Cash / 2;
            }

            if (ActiveCompetitorCount < _state.maxCompetitorCap / 2)
            {
                Competitor second = GenerateCompetitor(_rng, tick);
                if (second != null)
                    _logger.Log($"[CompetitorSystem] Bonus spawn: {second.CompanyName} due to depleted market.");
            }
        }

        _state.lastSpawnCheckTick = tick;
    }

    private bool ShouldSpawnForGenerationShift(int currentGen)
    {
        foreach (var kvp in _marketState.nicheDemand)
        {
            ProductCategory cat = NicheToCategory(kvp.Key);
            int competitorCount = 0;
            foreach (var compKvp in _state.competitors)
            {
                var c = compKvp.Value;
                if (c.IsBankrupt || c.IsAbsorbed) continue;
                if (c.Specializations == null) continue;
                int specLen = c.Specializations.Length;
                for (int i = 0; i < specLen; i++)
                {
                    if (c.Specializations[i] == cat) { competitorCount++; break; }
                }
            }
            if (competitorCount < 2) return true;
        }
        return false;
    }

    private void EvaluatePlatformMaintenance(Competitor comp, int tick)
    {
        if (_platformState == null) return;
        if (comp.ActiveProductIds == null) return;

        int count = comp.ActiveProductIds.Count;
        for (int i = 0; i < count; i++)
        {
            ProductId pid = comp.ActiveProductIds[i];
            if (!_productState.shippedProducts.TryGetValue(pid, out var product)) continue;
            if (!product.IsOnMarket) continue;
            if (!NicheToCategory(product.Niche).IsPlatform()) continue;
            if (!_platformState.platformShares.TryGetValue(pid, out var entry)) continue;

            float shareDecline = entry.MarketSharePercent < 30f ? 0.02f : 0f;
            if (shareDecline > 0f)
            {
                float updateQualityBoost = comp.Personality.RdSpeed * 2f;
                entry.QualityCeiling = Clamp(entry.QualityCeiling + updateQualityBoost, 0f, 100f);
                _platformState.platformShares[pid] = entry;
            }

            float newRate = 0.05f + comp.Personality.PricingAggression * 0.25f
                - comp.Personality.BrandInvestment * 0.1f;
            newRate = Clamp(newRate, 0.02f, 0.30f);
            if (Math.Abs(newRate - entry.LicensingRate) > 0.005f)
            {
                entry.LicensingRate = newRate;
                _platformState.platformShares[pid] = entry;
            }
        }
    }

    private void ProcessScheduledUpdates(Competitor comp, int tick)
    {
        if (comp.ScheduledUpdates == null || comp.ScheduledUpdates.Count == 0) return;

        for (int i = comp.ScheduledUpdates.Count - 1; i >= 0; i--)
        {
            var update = comp.ScheduledUpdates[i];
            if (tick < update.ScheduledTick) continue;

            if (!_productState.shippedProducts.TryGetValue(update.ProductId, out var product)) {
                comp.ScheduledUpdates.RemoveAt(i);
                continue;
            }

            product.UpdateCount++;
            product.TicksSinceLastUpdate = 0;
            product.ProductVersion++;
            product.PopularityScore = Math.Min(100f, product.PopularityScore + 10f);
            product.BugsRemaining = Math.Max(0f, product.BugsRemaining * 0.5f);
            product.IsLegacy = false;
            _productState.shippedProducts[update.ProductId] = product;

            comp.ScheduledUpdates.RemoveAt(i);
            _logger.Log($"[CompetitorSystem] {comp.CompanyName} applied scheduled update to '{product.ProductName}' (version {product.ProductVersion}) at tick {tick}.");
        }
    }

    private Competitor GenerateCompetitor(IRng rng, int tick, ProductNiche? forcedNiche = null)
    {
        if (_archetypeConfigs == null || _archetypeConfigs.Length == 0) return null;

        int configIndex = PickWeightedArchetypeIndex(rng);
        if (_archetypeConfigs[configIndex] == null) {
            int fallback = -1;
            for (int j = 0; j < _archetypeConfigs.Length; j++) {
                if (_archetypeConfigs[j] != null) { fallback = j; break; }
            }
            if (fallback < 0) return null;
            configIndex = fallback;
        }
        var id = new CompetitorId(_state.nextCompetitorId++);
        var archetype = _archetypeConfigs[configIndex].archetype;

        string companyName = GenerateCompanyName(rng);
        string founderName = GenerateFounderName(rng);
        bool isFounderNamed = rng.Chance(0.4f);

        CompetitorPersonality personality = default;
        CompetitorArchetypeConfig archetypeCfg = GetArchetypeConfig(archetype);
        if (archetypeCfg != null)
            personality = CompetitorPersonality.Roll(rng, archetypeCfg.personalityMin, archetypeCfg.personalityMax);

        ProductCategory[] specializations = archetypeCfg?.primaryCategories;

        var comp = new Competitor
        {
            Id = id,
            CompanyName = isFounderNamed ? $"{founderName}'s Software" : companyName,
            FounderName = founderName,
            Archetype = archetype,
            Personality = personality,
            Specializations = specializations,
            IsFounderNamed = isFounderNamed,
            Finance = default,
            IsBankrupt = false,
            IsAbsorbed = false,
            FoundedTick = tick,
            NicheMarketShare = new Dictionary<ProductNiche, float>(),
            ActiveProductIds = new List<ProductId>(),
            InDevelopmentProductIds = new List<ProductId>(),
            ScheduledUpdates = new List<ScheduledCompetitorUpdate>(),
            EmployeeIds = new List<EmployeeId>(),
            TeamAssignments = new Dictionary<TeamId, ProductId>(),
            LastProductEvalTick = tick,
            LastFinanceEvalTick = tick,
            ReputationPoints = GetStartConfigReputation(archetype, rng),
            LastHireTick = -1,
            LastProductStartedTick = -1,
            LastPricingReviewTick = -1,
            Memory = CompetitorMemory.CreateNew(),
        };

        _state.competitors[id] = comp;
        comp.CompanyFans = comp.ReputationPoints * rng.Range(5, 16);
        comp.FanSentiment = 40f + rng.NextFloat01() * 30f;

        ApplyRealEmployees(comp, rng, tick);
        SetInitialCash(comp, rng);

        ProductNiche startingNiche;
        bool hasNiche = forcedNiche.HasValue
            ? (startingNiche = forcedNiche.Value) == startingNiche
            : TryPickStartingNiche(rng, out startingNiche);

        if (hasNiche) {
            int productCount = rng.Range(1, 4);
            for (int p = 0; p < productCount; p++) {
                ProductNiche nicheForProduct;
                if (p == 0) {
                    nicheForProduct = startingNiche;
                } else {
                    if (!TryPickStartingNiche(rng, out nicheForProduct))
                        break;
                }
                bool willComplete = (p == 0);
                Product product = GenerateCompetitorProduct(comp, nicheForProduct, tick, suppressDevEvent: willComplete);
                if (product != null && willComplete) {
                    InstantShipCompetitorProduct(comp, product.Id, tick);
                }
            }
        }

        if (comp.ActiveProductIds != null && comp.ActiveProductIds.Count > 0) {
            long totalHistoricalRevenue = 0L;
            int activeCount = comp.ActiveProductIds.Count;
            bool isMidGameSpawn = tick > 0;
            for (int p = 0; p < activeCount; p++) {
                ProductId pid = comp.ActiveProductIds[p];
                if (!_productState.shippedProducts.TryGetValue(pid, out var product)) continue;

                int ageInMonths = isMidGameSpawn ? rng.Range(1, 12) : rng.Range(3, 24);
                int ageInTicks = ageInMonths * TimeState.TicksPerDay * 30;
                int devMonths = rng.Range(2, 8);
                int devTicks = devMonths * TimeState.TicksPerDay * 30;
                product.TotalDevelopmentTicks = EstimateDevTicks(product.TemplateId, product.OverallQuality);
                product.CreationTick = isMidGameSpawn ? tick - ageInTicks - devTicks : -ageInTicks - devTicks;
                product.ShipTick = isMidGameSpawn ? tick - ageInTicks : -ageInTicks;
                product.TicksSinceShip = ageInTicks;
                if (!isMidGameSpawn) {
                    int graceTicks = 6 * TimeState.TicksPerDay * 30;
                    product.WorldStartSunsetGraceUntilTick = graceTicks;
                }
                product.HasCompletedFirstMonth = true;
                product.SnapshotMonthlyTrend = "Stable";
                float ageDecayFactor = 1f / (1f + ageInMonths / 12f);
                product.ActiveUserCount = (int)(product.ActiveUserCount * ageDecayFactor);

                if (ageInMonths >= 18) {
                    product.LifecycleStage = ProductLifecycleStage.Decline;
                } else if (ageInMonths >= 10) {
                    product.LifecycleStage = ProductLifecycleStage.Plateau;
                } else if (ageInMonths >= 4) {
                    product.LifecycleStage = ProductLifecycleStage.Growth;
                }

                if (product.TemplateId != null && _templateLookup != null
                    && _templateLookup.TryGetValue(product.TemplateId, out var tailTmpl)
                    && tailTmpl.economyConfig != null) {
                    var cfg = tailTmpl.economyConfig;
                    float dailyDecay = cfg.tailDecayRate / 30f;
                    if (product.IsMaintained)
                        dailyDecay = Math.Max(0f, dailyDecay - cfg.maintenancePopDecayReduction / 30f);
                    product.TailDecayFactor = Math.Max(cfg.minTailFactor, (float)Math.Pow(1f - dailyDecay, ageInMonths * 30));
                } else {
                    product.TailDecayFactor = Math.Max(0.15f, (float)Math.Pow(0.997, ageInMonths * 30));
                }

                if (isMidGameSpawn) {
                    product.TotalLifetimeRevenue = (long)product.MonthlyRevenue * ageInMonths;
                    totalHistoricalRevenue += product.TotalLifetimeRevenue;
                }
            }
            if (isMidGameSpawn) {
                long totalCurrentMonthly = 0L;
                for (int p2 = 0; p2 < activeCount; p2++) {
                    ProductId pid2 = comp.ActiveProductIds[p2];
                    if (_productState.shippedProducts.TryGetValue(pid2, out var prod2))
                        totalCurrentMonthly += (long)prod2.MonthlyRevenue;
                }
                long retainedCash = Math.Min(totalHistoricalRevenue / 10, totalCurrentMonthly * 12);
                comp.Finance.Cash += retainedCash;
            }
        }

        var capturedId = id;
        InitializeMoraleForEmployees(comp);
        CreateInitialTeams(comp, tick);
        _pendingEvents.Add(new PendingCompetitorEvent { Type = PendingEventType.Spawned, CompId = capturedId });
        _logger.Log($"[CompetitorSystem] Spawned competitor '{comp.CompanyName}' ({archetype}) at tick {tick}.");
        return comp;
    }

    private Product GenerateCompetitorProduct(Competitor comp, ProductNiche niche, int tick, bool suppressDevEvent = false)
    {
        CompetitorArchetypeConfig config = GetArchetypeConfig(comp.Archetype);
        if (config == null) return null;

        var productId = new ProductId(_productState.nextProductId++);

        int devMonths = (int)(_rng.NextFloat01()
            * (config.releaseIntervalMonthsMax - config.releaseIntervalMonthsMin)
            + config.releaseIntervalMonthsMin);
        if (devMonths < 1) devMonths = 1;
        int devTicks = devMonths * TimeState.TicksPerDay * 30;
        int targetReleaseTick = tick + devTicks;

        ProductCategory resolvedCategory;
        ProductNiche resolvedNiche;

        if (niche == ProductNiche.None)
        {
            resolvedNiche = ProductNiche.None;
            resolvedCategory = comp.Specializations != null && comp.Specializations.Length > 0
                ? comp.Specializations[0] : ProductCategory.VideoGame;
        }
        else
        {
            resolvedCategory = NicheToCategory(niche);
            resolvedNiche = niche;

            if (_templateLookup != null)
            {
                foreach (var kvp in _templateLookup)
                {
                    var tmpl = kvp.Value;
                    if (tmpl.category == resolvedCategory && !tmpl.HasNiches)
                    {
                        resolvedNiche = ProductNiche.None;
                        break;
                    }
                }
            }
        }

        string templateId = resolvedNiche != ProductNiche.None
            ? FindTemplateForNiche(resolvedNiche)
            : FindTemplateForCategory(resolvedCategory);

        ProductId[] selectedToolIds = SelectToolsForCompetitor(comp, resolvedCategory, resolvedNiche, tick);
        ProductId[] selectedPlatformIds = resolvedCategory.IsApplication()
            ? SelectPlatformsForCompetitor(comp, resolvedCategory, resolvedNiche, tick)
            : null;

        string[] selectedFeatureIds = SelectFeaturesForCompetitor(comp, niche, templateId, selectedToolIds, selectedPlatformIds);

        // Hardware configuration for competitor console products
        bool hasHardwareConfig = false;
        HardwareConfiguration hardwareConfig = default;
        int manufactureCostPerUnit = 0;
        if (resolvedCategory == ProductCategory.GameConsole)
        {
            hardwareConfig = SelectHardwareForCompetitor(comp);
            hasHardwareConfig = true;
            if (_productSystem != null)
            {
                int currentGen = _generationSystem?.GetCurrentGeneration() ?? 1;
                HardwareGenerationConfig genConfig = _productSystem.GetHardwareGenerationConfig(currentGen);
                if (genConfig != null)
                    manufactureCostPerUnit = genConfig.CalculateManufactureCost(hardwareConfig);
            }
            selectedFeatureIds = FilterFeaturesByHardware(selectedFeatureIds, hardwareConfig);
        }

        ProductPhaseRuntime[] phases;
        if (_productSystem != null && templateId != null)
        {
            float nicheDevTimeMult = 1f;
            phases = _productSystem.BuildPhasesForTemplate(templateId, selectedFeatureIds, nicheDevTimeMult);
        }
        else
        {
            phases = new ProductPhaseRuntime[]
            {
                new ProductPhaseRuntime
                {
                    phaseType = ProductPhaseType.Programming,
                    phaseQuality = 0f,
                    totalWorkRequired = 1000f,
                    workCompleted = 0f,
                    isComplete = false,
                    isUnlocked = true
                }
            };
        }

        var product = new Product
        {
            Id = productId,
            ProductName = GenerateProductName(comp, niche, resolvedCategory),
            OwnerCompanyId = comp.Id.ToCompanyId(),
            IsInDevelopment = true,
            IsShipped = false,
            IsOnMarket = false,
            TargetReleaseTick = targetReleaseTick,
            OriginalReleaseTick = targetReleaseTick,
            HasAnnouncedReleaseDate = true,
            CreationTick = tick,
            TemplateId = templateId,
            Phases = phases,
            TeamAssignments = new Dictionary<ProductTeamRole, TeamId>(),
            ActiveUserCount = 0,
            OverallQuality = 0f,
            PopularityScore = 0f,
            MonthlyRevenue = 0,
            LifecycleStage = ProductLifecycleStage.PreLaunch,
            Niche = resolvedNiche,
            Category = resolvedCategory,
            SelectedFeatureIds = selectedFeatureIds,
            RequiredToolIds = selectedToolIds,
            TargetPlatformIds = selectedPlatformIds,
            TotalDevelopmentTicks = 0,
            DroppedFeatureIds = new List<string>(),
            SequelIds = new List<ProductId>(),
            HasHardwareConfig = hasHardwareConfig,
            HardwareConfig = hardwareConfig,
            ManufactureCostPerUnit = manufactureCostPerUnit
        };

        _productState.developmentProducts[productId] = product;
        comp.InDevelopmentProductIds.Add(productId);

        AssignDevTeamToProduct(comp, product);

        if (!suppressDevEvent)
            _pendingEvents.Add(new PendingCompetitorEvent { Type = PendingEventType.DevStarted, CompId = comp.Id, ProductId = productId });

        comp.Finance.Cash = Clamp(comp.Finance.Cash - (long)(_rng.Range(5000, 20000)), MinCashFloor, MaxCashCeiling);

        return product;
    }

    private HardwareConfiguration SelectHardwareForCompetitor(Competitor comp)
    {
        var personality = comp.Personality;
        HardwareTier processing;
        HardwareTier graphics;
        HardwareTier memory;
        HardwareTier storage;
        ConsoleFormFactor formFactor;

        if (personality.RiskTolerance > 0.6f && personality.PricingAggression < 0.4f)
        {
            // High-end play (PS3 style)
            processing = HardwareTier.HighEnd;
            graphics = HardwareTier.Enthusiast;
            memory = HardwareTier.HighEnd;
            storage = HardwareTier.HighEnd;
        }
        else if (personality.RiskTolerance < 0.4f && personality.PricingAggression > 0.6f)
        {
            // Value play (Wii style)
            processing = HardwareTier.Budget;
            graphics = HardwareTier.Budget;
            memory = HardwareTier.MidRange;
            storage = HardwareTier.Budget;
        }
        else
        {
            // Balanced (default)
            processing = HardwareTier.MidRange;
            graphics = HardwareTier.MidRange;
            memory = HardwareTier.MidRange;
            storage = HardwareTier.MidRange;
        }

        formFactor = personality.InnovationBias > 0.6f
            ? ConsoleFormFactor.Hybrid
            : ConsoleFormFactor.Standard;

        return new HardwareConfiguration
        {
            processingTier = processing,
            graphicsTier = graphics,
            memoryTier = memory,
            storageTier = storage,
            formFactor = formFactor,
            manufactureCostPerUnit = 0 // calculated externally
        };
    }

    private string[] FilterFeaturesByHardware(string[] featureIds, HardwareConfiguration config)
    {
        if (featureIds == null || featureIds.Length == 0 || _productSystem == null) return featureIds;
        var filtered = new List<string>(featureIds.Length);
        for (int i = 0; i < featureIds.Length; i++)
        {
            string fid = featureIds[i];
            if (string.IsNullOrEmpty(fid)) continue;
            ProductFeatureDefinition def = FindFeatureDefinition(fid);
            if (def == null || _productSystem.IsFeatureAvailableForHardware(def, config))
                filtered.Add(fid);
        }
        return filtered.Count == featureIds.Length ? featureIds : filtered.ToArray();
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

    private string[] SelectFeaturesForCompetitor(Competitor comp, ProductNiche niche, string templateId, ProductId[] toolIds, ProductId[] platformIds)
    {
        if (_marketSystem == null) return null;

        int currentGen = _generationSystem != null ? _generationSystem.GetCurrentGeneration() : 1;
        int baseMin = 2 + (int)(comp.Personality.InnovationBias * 2f);
        int baseMax = 4 + (int)(comp.Personality.InnovationBias * 3f);
        int targetCount = _rng.Range(baseMin, baseMax + 1);

        var candidates = new List<ProductFeatureDefinition>(16);
        if (templateId != null && _templateLookup != null && _templateLookup.TryGetValue(templateId, out var resolvedTemplate) && resolvedTemplate.availableFeatures != null)
        {
            int len = resolvedTemplate.availableFeatures.Length;
            for (int i = 0; i < len; i++)
            {
                var feat = resolvedTemplate.availableFeatures[i];
                if (feat == null || string.IsNullOrEmpty(feat.featureId)) continue;
                candidates.Add(feat);
            }
        }

        for (int i = candidates.Count - 1; i >= 0; i--)
        {
            if (!IsFeatureGateSatisfied(candidates[i], toolIds, platformIds))
                candidates.RemoveAt(i);
        }

        int candidateCount = candidates.Count;
        if (candidateCount == 0) return null;

        var scores = new float[candidateCount];
        for (int i = 0; i < candidateCount; i++)
        {
            var feat = candidates[i];
            float baseDemand = _marketSystem.GetNicheDemand(niche);
            float nicheAffinity = _marketSystem.GetFeatureCategoryAffinity(niche, feat.featureCategory);
            float innovationWeight = comp.Personality.InnovationBias;
            float noise = 0.7f + _rng.NextFloat01() * 0.6f;
            scores[i] = baseDemand * nicheAffinity * innovationWeight * noise;
        }

        var indices = new int[candidateCount];
        for (int i = 0; i < candidateCount; i++) indices[i] = i;
        for (int i = 1; i < candidateCount; i++)
        {
            int key = indices[i];
            float keyScore = scores[key];
            int j = i - 1;
            while (j >= 0 && scores[indices[j]] < keyScore)
            {
                indices[j + 1] = indices[j];
                j--;
            }
            indices[j + 1] = key;
        }

        int pickCount = Math.Min(targetCount, candidateCount);
        var result = new string[pickCount];
        for (int i = 0; i < pickCount; i++)
            result[i] = candidates[indices[i]].featureId;

        return result;
    }

    private bool IsFeatureGateSatisfied(ProductFeatureDefinition feat, ProductId[] toolIds, ProductId[] platformIds)
    {
        if (!string.IsNullOrEmpty(feat.requiresToolFeature))
        {
            bool found = false;
            if (toolIds != null)
            {
                int toolCount = toolIds.Length;
                for (int t = 0; t < toolCount && !found; t++)
                {
                    if (!_productState.shippedProducts.TryGetValue(toolIds[t], out var tool)) continue;
                    if (tool.SelectedFeatureIds == null) continue;
                    int fc = tool.SelectedFeatureIds.Length;
                    for (int f = 0; f < fc; f++)
                    {
                        if (tool.SelectedFeatureIds[f] == feat.requiresToolFeature) { found = true; break; }
                    }
                }
            }
            if (!found) return false;
        }

        if (!string.IsNullOrEmpty(feat.requiresPlatformFeature))
        {
            bool found = false;
            if (platformIds != null)
            {
                int platCount = platformIds.Length;
                for (int p = 0; p < platCount && !found; p++)
                {
                    if (!_productState.shippedProducts.TryGetValue(platformIds[p], out var platform)) continue;
                    if (platform.SelectedFeatureIds == null) continue;
                    int fc = platform.SelectedFeatureIds.Length;
                    for (int f = 0; f < fc; f++)
                    {
                        if (platform.SelectedFeatureIds[f] == feat.requiresPlatformFeature) { found = true; break; }
                    }
                }
            }
            if (!found) return false;
        }

        return true;
    }

    private float ScoreToolByDesiredFeatures(Competitor comp, Product tool, ProductNiche targetNiche, int currentGen)
    {
        float toolScore = 0f;
        if (_marketSystem == null || _templateLookup == null) return toolScore;

        float personalityBase = comp.Personality.InnovationBias * 0.5f + 0.5f;
        float pricingAggression = comp.Personality.PricingAggression > 0f ? comp.Personality.PricingAggression : 0.5f;

        foreach (var kvp in _templateLookup)
        {
            var tmpl = kvp.Value;
            if (tmpl.availableFeatures == null) continue;
            int len = tmpl.availableFeatures.Length;
            for (int i = 0; i < len; i++)
            {
                var feat = tmpl.availableFeatures[i];
                if (feat == null || string.IsNullOrEmpty(feat.requiresToolFeature)) continue;

                float nicheDemandAffinity = _marketSystem.GetFeatureCategoryAffinity(targetNiche, feat.featureCategory);
                float generationMod = feat.availableFromGeneration <= currentGen ? 1.0f : 0.3f;
                float trendNoise = 0.7f + _rng.NextFloat01() * 0.6f;
                float weight = personalityBase * nicheDemandAffinity * generationMod * trendNoise;

                bool toolHasFeature = false;
                float upstreamQuality = 0f;
                if (tool.SelectedFeatureIds != null)
                {
                    int fc = tool.SelectedFeatureIds.Length;
                    for (int f = 0; f < fc; f++)
                    {
                        if (tool.SelectedFeatureIds[f] == feat.requiresToolFeature)
                        {
                            toolHasFeature = true;
                            upstreamQuality = tool.OverallQuality;
                            break;
                        }
                    }
                }

                if (toolHasFeature)
                    toolScore += weight * (upstreamQuality / 100f);
                else
                    toolScore -= weight * 0.5f;
            }
        }

        toolScore += tool.OverallQuality * 0.3f;

        float licensingCost = 0f;
        if (!tool.IsCompetitorProduct && tool.DistributionModel == ToolDistributionModel.Licensed)
            licensingCost = tool.PlayerLicensingRate;
        else if (tool.IsCompetitorProduct && tool.OwnerCompanyId != comp.Id.ToCompanyId() && tool.DistributionModel == ToolDistributionModel.Licensed)
            licensingCost = tool.PlayerLicensingRate;

        toolScore -= licensingCost * 100f * pricingAggression;
        return toolScore;
    }

    private string[] SelectStartingFeatures(Competitor comp, ProductNiche niche, ProductCategory category, IRng rng, ProductId[] toolIds = null, ProductId[] platformIds = null)
    {
        if (_templateLookup == null) return null;

        float bias = comp.Personality.InnovationBias;
        int baseMin = 2 + (int)(bias * 2f);
        int baseMax = 4 + (int)(bias * 3f);
        int targetCount = rng.Range(baseMin, baseMax + 1);

        var candidates = new List<ProductFeatureDefinition>(16);
        foreach (var kvp in _templateLookup)
        {
            var tmpl = kvp.Value;
            if (tmpl.availableFeatures == null) continue;

            bool nicheMatch = false;
            if (niche != ProductNiche.None)
            {
                if (tmpl.nicheConfigs != null)
                {
                    int nc = tmpl.nicheConfigs.Length;
                    for (int n = 0; n < nc; n++)
                    {
                        if (tmpl.nicheConfigs[n].niche == niche) { nicheMatch = true; break; }
                    }
                }
            }
            else
            {
                nicheMatch = tmpl.category == category;
            }

            if (!nicheMatch) continue;

            int len = tmpl.availableFeatures.Length;
            for (int i = 0; i < len; i++)
            {
                var feat = tmpl.availableFeatures[i];
                if (feat == null || string.IsNullOrEmpty(feat.featureId)) continue;
                if (feat.availableFromGeneration > 1) continue;
                bool already = false;
                int cc = candidates.Count;
                for (int c = 0; c < cc; c++)
                {
                    if (candidates[c].featureId == feat.featureId) { already = true; break; }
                }
                if (!already) candidates.Add(feat);
            }
        }

        for (int i = candidates.Count - 1; i >= 0; i--)
        {
            if (!IsFeatureGateSatisfied(candidates[i], toolIds, platformIds))
                candidates.RemoveAt(i);
        }

        int candidateCount = candidates.Count;
        if (candidateCount == 0) return null;

        var scores = new float[candidateCount];
        for (int i = 0; i < candidateCount; i++)
            scores[i] = bias * (0.7f + rng.NextFloat01() * 0.6f);

        var indices = new int[candidateCount];
        for (int i = 0; i < candidateCount; i++) indices[i] = i;
        for (int i = 1; i < candidateCount; i++)
        {
            int key = indices[i];
            float keyScore = scores[key];
            int j = i - 1;
            while (j >= 0 && scores[indices[j]] < keyScore)
            {
                indices[j + 1] = indices[j];
                j--;
            }
            indices[j + 1] = key;
        }

        int pickCount2 = Math.Min(targetCount, candidateCount);
        var result2 = new string[pickCount2];
        for (int i = 0; i < pickCount2; i++)
            result2[i] = candidates[indices[i]].featureId;

        return result2;
    }

    private ProductId[] SelectToolsForCompetitor(Competitor comp, ProductCategory productCategory, ProductNiche targetNiche, int tick)
    {
        if (_templateLookup == null || _productState == null) return null;

        ProductTemplateDefinition template = null;
        foreach (var kvp in _templateLookup)
        {
            if (kvp.Value.category == productCategory)
            {
                template = kvp.Value;
                break;
            }
        }

        if (template == null || template.requiredToolTypes == null || template.requiredToolTypes.Length == 0)
            return null;

        int currentGen = _generationSystem != null ? _generationSystem.GetCurrentGeneration() : 1;
        var selectedIds = new List<ProductId>(template.requiredToolTypes.Length);

        for (int ti = 0; ti < template.requiredToolTypes.Length; ti++)
        {
            ProductCategory requiredCategory = template.requiredToolTypes[ti];

            var candidates = new List<ProductId>(8);
            foreach (var kvp in _productState.shippedProducts)
            {
                var tool = kvp.Value;
                if (!tool.IsOnMarket) continue;
                if (tool.Category != requiredCategory) continue;
                if (!tool.IsCompetitorProduct && tool.DistributionModel == ToolDistributionModel.Proprietary) continue;
                if (tool.IsCompetitorProduct && tool.OwnerCompanyId == comp.Id.ToCompanyId()) { candidates.Add(kvp.Key); continue; }
                candidates.Add(kvp.Key);
            }

            if (candidates.Count == 0) continue;

            float bestScore = float.MinValue;
            ProductId bestId = candidates[0];
            float pricingAggression = comp.Personality.PricingAggression > 0f ? comp.Personality.PricingAggression : 0.5f;

            for (int ci = 0; ci < candidates.Count; ci++)
            {
                if (!_productState.shippedProducts.TryGetValue(candidates[ci], out var tool)) continue;
                float ownershipBonus;
                if (tool.IsCompetitorProduct && tool.OwnerCompanyId == comp.Id.ToCompanyId())
                    ownershipBonus = 1.1f;
                else if (!tool.IsCompetitorProduct && tool.DistributionModel == ToolDistributionModel.OpenSource)
                    ownershipBonus = 1.05f;
                else
                    ownershipBonus = 0.95f;

                float desireScore = ScoreToolByDesiredFeatures(comp, tool, targetNiche, currentGen);
                float score = desireScore * ownershipBonus - (tool.DistributionModel == ToolDistributionModel.Licensed ? tool.PlayerLicensingRate * 100f * pricingAggression : 0f);
                if (score > bestScore) { bestScore = score; bestId = candidates[ci]; }
            }

            selectedIds.Add(bestId);
        }

        return selectedIds.Count > 0 ? selectedIds.ToArray() : null;
    }

    private ProductId[] SelectPlatformsForCompetitor(Competitor comp, ProductCategory productCategory, ProductNiche targetNiche, int tick)
    {
        if (_templateLookup == null || _productState == null) return null;

        ProductTemplateDefinition template = null;
        foreach (var kvp in _templateLookup)
        {
            if (kvp.Value.category == productCategory)
            {
                template = kvp.Value;
                break;
            }
        }

        if (template == null || template.validTargetPlatforms == null || template.validTargetPlatforms.Length == 0)
            return null;

        int currentGen = _generationSystem != null ? _generationSystem.GetCurrentGeneration() : 1;
        var selectedIds = new List<ProductId>(template.validTargetPlatforms.Length);

        for (int ti = 0; ti < template.validTargetPlatforms.Length; ti++)
        {
            ProductCategory requiredCategory = template.validTargetPlatforms[ti];

            var candidates = new List<ProductId>(8);
            foreach (var kvp in _productState.shippedProducts)
            {
                var platform = kvp.Value;
                if (!platform.IsOnMarket) continue;
                if (platform.Category != requiredCategory) continue;
                if (requiredCategory == ProductCategory.OperatingSystem
                    && template.validPlatformNiches != null && template.validPlatformNiches.Length > 0
                    && !CompNicheArrayContains(template.validPlatformNiches, platform.Niche)) continue;
                candidates.Add(kvp.Key);
            }

            if (candidates.Count == 0) continue;

            float bestScore = float.MinValue;
            ProductId bestId = candidates[0];
            float pricingAggression = comp.Personality.PricingAggression > 0f ? comp.Personality.PricingAggression : 0.5f;
            float personalityBase = comp.Personality.InnovationBias * 0.5f + 0.5f;

            for (int ci = 0; ci < candidates.Count; ci++)
            {
                if (!_productState.shippedProducts.TryGetValue(candidates[ci], out var platform)) continue;
                float ownershipBonus = (platform.IsCompetitorProduct && platform.OwnerCompanyId == comp.Id.ToCompanyId()) ? 1.1f : 0.95f;

                float platformScore = 0f;
                if (_templateLookup != null)
                {
                    foreach (var kvp2 in _templateLookup)
                    {
                        var tmpl = kvp2.Value;
                        if (tmpl.availableFeatures == null) continue;
                        int len = tmpl.availableFeatures.Length;
                        for (int i = 0; i < len; i++)
                        {
                            var feat = tmpl.availableFeatures[i];
                            if (feat == null || string.IsNullOrEmpty(feat.requiresPlatformFeature)) continue;

                            float nicheDemandAffinity = _marketSystem != null ? _marketSystem.GetFeatureCategoryAffinity(targetNiche, feat.featureCategory) : 1f;
                            float generationMod = feat.availableFromGeneration <= currentGen ? 1.0f : 0.3f;
                            float trendNoise = 0.7f + _rng.NextFloat01() * 0.6f;
                            float weight = personalityBase * nicheDemandAffinity * generationMod * trendNoise;

                            bool platformHasFeature = false;
                            float upstreamQuality = 0f;
                            if (platform.SelectedFeatureIds != null)
                            {
                                int fc = platform.SelectedFeatureIds.Length;
                                for (int f = 0; f < fc; f++)
                                {
                                    if (platform.SelectedFeatureIds[f] == feat.requiresPlatformFeature)
                                    {
                                        platformHasFeature = true;
                                        upstreamQuality = platform.OverallQuality;
                                        break;
                                    }
                                }
                            }

                            if (platformHasFeature)
                                platformScore += weight * (upstreamQuality / 100f);
                            else
                                platformScore -= weight * 0.5f;
                        }
                    }
                }

                platformScore += platform.OverallQuality * 0.3f;
                if (_platformState != null && _platformState.platformShares.TryGetValue(candidates[ci], out var entry))
                    platformScore -= entry.LicensingRate * 100f * pricingAggression;

                float score = platformScore * ownershipBonus;
                if (score > bestScore) { bestScore = score; bestId = candidates[ci]; }
            }

            selectedIds.Add(bestId);
        }

        return selectedIds.Count > 0 ? selectedIds.ToArray() : null;
    }

    private static bool CompNicheArrayContains(ProductNiche[] arr, ProductNiche value) {
        int len = arr.Length;
        for (int i = 0; i < len; i++) {
            if (arr[i] == value) return true;
        }
        return false;
    }

    private string GenerateCompanyName(IRng rng)
    {
        return CompetitorNameGenerator.GenerateCompanyName(_nameData, rng);
    }

    private string GenerateFounderName(IRng rng)
    {
        return CompetitorNameGenerator.GenerateFounderName(rng);
    }

    private string GenerateProductName(Competitor comp, ProductNiche niche, ProductCategory category)
    {
        return CompetitorNameGenerator.GenerateProductName(_nameData, _rng, comp.CompanyName, category, niche);
    }

    private bool TryPickStartingNiche(IRng rng, out ProductNiche niche)
    {
        niche = default;
        if (_marketState == null || _marketState.nicheDemand == null || _marketState.nicheDemand.Count == 0)
            return false;

        var keys = new List<ProductNiche>(_marketState.nicheDemand.Count);
        foreach (var kvp in _marketState.nicheDemand)
            keys.Add(kvp.Key);

        niche = keys[rng.Range(0, keys.Count)];
        return true;
    }

    private int PickWeightedArchetypeIndex(IRng rng) {
        if (_archetypeConfigs == null || _archetypeConfigs.Length == 0) return 0;
        int count = _archetypeConfigs.Length;
        float totalWeight = 0f;
        for (int i = 0; i < count; i++) {
            if (_archetypeConfigs[i] == null) continue;
            bool hasVideoGame = false;
            var primary = _archetypeConfigs[i].primaryCategories;
            if (primary != null) {
                for (int j = 0; j < primary.Length; j++) {
                    if (primary[j] == ProductCategory.VideoGame) { hasVideoGame = true; break; }
                }
            }
            totalWeight += hasVideoGame ? 2f : 1f;
        }
        float roll = rng.NextFloat01() * totalWeight;
        float accumulated = 0f;
        for (int i = 0; i < count; i++) {
            if (_archetypeConfigs[i] == null) continue;
            bool hasVideoGame = false;
            var primary = _archetypeConfigs[i].primaryCategories;
            if (primary != null) {
                for (int j = 0; j < primary.Length; j++) {
                    if (primary[j] == ProductCategory.VideoGame) { hasVideoGame = true; break; }
                }
            }
            accumulated += hasVideoGame ? 2f : 1f;
            if (roll < accumulated) return i;
        }
        return count - 1;
    }

    private CompetitorArchetypeConfig GetArchetypeConfig(CompetitorArchetype archetype)
    {
        if (_archetypeConfigs == null) return null;
        for (int i = 0; i < _archetypeConfigs.Length; i++)
        {
            if (_archetypeConfigs[i] != null && _archetypeConfigs[i].archetype == archetype)
                return _archetypeConfigs[i];
        }
        return null;
    }

    private int EstimateDevTicks(string templateId, float quality)
    {
        if (templateId == null || _templateLookup == null) return 48000;
        if (!_templateLookup.TryGetValue(templateId, out var template) || template.phases == null) return 48000;
        float workMultiplier = _tuning?.ProductBaseWorkMultiplier ?? 100f;
        int featureCount = template.availableFeatures != null ? template.availableFeatures.Length : 0;
        float difficultyScale = 1.0f + (template.difficultyTier - 1) * 0.75f;
        float featureScale = 1.0f + featureCount * 0.12f + (float)Math.Pow(featureCount, 1.5) * 0.02f;
        float expected = 0f;
        int count = template.phases.Length;
        for (int i = 0; i < count; i++) {
            var phase = template.phases[i];
            if (phase != null) expected += phase.baseWorkUnits * workMultiplier * difficultyScale * featureScale;
        }
        if (expected <= 0f) expected = 48000f;
        float qualityFactor = 0.5f + (quality / 100f);
        return (int)(expected * qualityFactor);
    }

    private Product GetDevProduct(ProductId id)
    {
        _productState.developmentProducts.TryGetValue(id, out var p);
        return p;
    }

    private Product GetShippedProduct(ProductId id)
    {
        _productState.shippedProducts.TryGetValue(id, out var p);
        return p;
    }

    private static long Clamp(long value, long min, long max)
    {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }

    private static float Clamp(float value, float min, float max)
    {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }

    public void ForceInitialFinanceEval(int tick)
    {
        _scratchIds.Clear();
        foreach (var kvp in _state.competitors)
        {
            if (!kvp.Value.IsBankrupt && !kvp.Value.IsAbsorbed)
                _scratchIds.Add(kvp.Key);
        }
        int count = _scratchIds.Count;
        for (int i = 0; i < count; i++)
        {
            CompetitorId id = _scratchIds[i];
            if (!_state.competitors.TryGetValue(id, out var comp)) continue;
            if (comp.IsBankrupt || comp.IsAbsorbed) continue;
            EvaluateMonthlyFinances(comp, tick);
        }
    }

    private float ComputeCompetitorFeatureRelevance(Product product, float fallbackQuality)
    {
        if (_marketSystem == null || product.SelectedFeatureIds == null || product.SelectedFeatureIds.Length == 0
            || product.TemplateId == null || _templateLookup == null || !_templateLookup.TryGetValue(product.TemplateId, out var template)
            || template.availableFeatures == null)
            return Clamp(0.2f + fallbackQuality / 120f, 0.2f, 0.95f);

        int currentGen = _generationSystem != null ? _generationSystem.GetCurrentGeneration() : 1;
        int selectedCount = product.SelectedFeatureIds.Length;
        float innovationSum = 0f;
        float missingPenaltySum = 0f;

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
            FeatureDemandStage stage = featDef != null
                ? FeatureDemandHelper.GetDemandStage(currentGen, featDef.demandIntroductionGen, featDef.demandMaturitySpeed, featDef.isFoundational, adoptionRate)
                : FeatureDemandStage.Standard;
            innovationSum += FeatureDemandHelper.GetInnovationValue(stage);
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
            float coverage = _marketSystem.GetFeatureAdoptionRate(featDef.featureId, product.Niche, product.TemplateId);
            FeatureDemandStage poolStage = FeatureDemandHelper.GetDemandStage(currentGen, featDef.demandIntroductionGen, featDef.demandMaturitySpeed, featDef.isFoundational, coverage);
            missingPenaltySum += FeatureDemandHelper.GetMissingPenalty(poolStage, coverage);
        }

        float avgInnovation = innovationSum / selectedCount;
        float raw = (avgInnovation - missingPenaltySum * 0.1f) / 50f;
        return Clamp(raw, 0.2f, 0.95f);
    }

    public void SyncNicheMarketShares(MarketState marketState)
    {
        if (marketState == null || marketState.currentMarketShares == null) return;

        foreach (var kvp in _state.competitors)
        {
            var comp = kvp.Value;
            if (comp.NicheMarketShare == null)
                comp.NicheMarketShare = new Dictionary<ProductNiche, float>();
            else
                comp.NicheMarketShare.Clear();
        }

        foreach (var nicheKvp in marketState.currentMarketShares)
        {
            var niche = nicheKvp.Key;
            var entries = nicheKvp.Value;
            if (entries == null) continue;
            int entryCount = entries.Count;
            for (int i = 0; i < entryCount; i++)
            {
                var entry = entries[i];
                if (!entry.OwnerId.HasValue) continue;
                var compId = entry.OwnerId.Value;
                if (!_state.competitors.TryGetValue(compId, out var comp)) continue;
                if (comp.NicheMarketShare.TryGetValue(niche, out float existing))
                    comp.NicheMarketShare[niche] = existing + entry.MarketSharePercent;
                else
                    comp.NicheMarketShare[niche] = entry.MarketSharePercent;
            }
        }
    }
}
