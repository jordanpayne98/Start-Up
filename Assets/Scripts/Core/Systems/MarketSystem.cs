// MarketSystem Version: Clean v1
using System;
using System.Collections.Generic;

public class MarketSystem : ISystem
{
    public const int DemandUpdateIntervalTicks = TimeState.TicksPerDay * 30;
    public const float MaxDemandUpliftPerRelease = 15f;
    private const float MarketShareSaturationCap = 0.65f;
    private const int ProjectionDays = MarketState.ProjectionDays;
    private const float ForecastMaxNoise = 20f;
    private const float ForecastNoiseExponent = 1.3f;
    private const float UtilizationLerpRate = 0.06f;

    public event Action<ProductNiche, float, MarketTrend> OnNicheDemandUpdated;
    public event Action<ProductNiche, float> OnNicheDemandSpiked;
    public event Action<ProductNiche, float> OnNicheDemandCrashed;
    public event Action<ProductNiche, ShowdownResult> OnShowdownResolved;

    private readonly MarketState _state;
    private readonly ProductState _productState;
    private readonly IRng _rng;
    private readonly ILogger _logger;

    private CompetitorState _competitorState;
    private DisruptionSystem _disruptionSystem;
    private PlatformSystem _platformSystem;
    private GenerationSystem _generationSystem;

    private int _masterSeed;
    private float[] _projScratchDemand;
    private float[] _projScratchMomentum;
    private float[] _projScratchRecoveryRate;
    private float[] _projMonthlySnapshots;

    // Pre-allocated scratch for market share resolution
    private readonly List<Product> _nicheProducts;
    private readonly List<Product> _upcomingProducts;
    private readonly List<Product> _newReleasesThisMonth;
    private int _lastDayShareResolutionTick;
    private int _lastDayProjectionTick;
    private bool _suppressShowdowns;
    private bool _debugLoggedEngine;

    private Dictionary<ProductNiche, MarketNicheData> _nicheLookup;
    private Dictionary<string, ProductFeatureDefinition> _featureLookup;
    private Dictionary<string, ProductTemplateDefinition> _templateLookup;
    private Dictionary<ProductCategory, ProductTemplateDefinition> _categoryTemplateMap;

    private enum PendingEventKind : byte { DemandUpdated, DemandSpiked, DemandCrashed, ShowdownResolved }
    private struct PendingMarketEvent
    {
        public PendingEventKind Kind;
        public ProductNiche Niche;
        public float Demand;
        public MarketTrend Trend;
        public ShowdownResult Showdown;
    }
    private readonly List<PendingMarketEvent> _pendingEvents;

    // Scratch lists to avoid allocations during iteration
    private readonly List<ProductNiche> _nicheKeys;
    private readonly List<ProductCategory> _categoryKeys;

    // Scratch buffers for platform/tool user recalculation
    private readonly List<ProductId> _platformProductIds;
    private readonly Dictionary<ProductId, int> _platformUserAccum;

    public MarketSystem(MarketState state, ProductState productState, IRng rng, ILogger logger)
    {
        _state = state ?? throw new ArgumentNullException(nameof(state));
        _productState = productState ?? throw new ArgumentNullException(nameof(productState));
        _rng = rng ?? throw new ArgumentNullException(nameof(rng));
        _logger = logger ?? new NullLogger();
        _nicheLookup = new Dictionary<ProductNiche, MarketNicheData>();
        _featureLookup = new Dictionary<string, ProductFeatureDefinition>();
        _templateLookup = new Dictionary<string, ProductTemplateDefinition>();
        _categoryTemplateMap = new Dictionary<ProductCategory, ProductTemplateDefinition>();
        _pendingEvents = new List<PendingMarketEvent>();
        _nicheKeys = new List<ProductNiche>(64);
        _categoryKeys = new List<ProductCategory>(8);
        _nicheProducts = new List<Product>(32);
        _upcomingProducts = new List<Product>(16);
        _newReleasesThisMonth = new List<Product>(8);
        _platformProductIds = new List<ProductId>(16);
        _platformUserAccum = new Dictionary<ProductId, int>(16);
    }

    public void SetCompetitorState(CompetitorState compState)
    {
        _competitorState = compState;
    }

    public void SetDisruptionSystem(DisruptionSystem ds)
    {
        _disruptionSystem = ds;
    }

    public void SetPlatformSystem(PlatformSystem ps)
    {
        _platformSystem = ps;
    }

    public void SetGenerationSystem(GenerationSystem gs)
    {
        _generationSystem = gs;
    }

    public void SetMasterSeed(int seed)
    {
        _masterSeed = seed;
    }

    public void RegisterNicheConfigs(MarketNicheData[] configs)
    {
        _nicheLookup.Clear();
        if (configs == null) return;
        for (int i = 0; i < configs.Length; i++)
        {
            var cfg = configs[i];
            if (cfg != null)
                _nicheLookup[cfg.niche] = cfg;
        }
    }

    public void RegisterFeatureConfigs(ProductFeatureDefinition[] configs)
    {
        _featureLookup.Clear();
        if (configs == null) return;
        for (int i = 0; i < configs.Length; i++)
        {
            var cfg = configs[i];
            if (cfg == null || string.IsNullOrEmpty(cfg.featureId)) continue;
            _featureLookup[cfg.featureId] = cfg;
        }
    }

    public void RegisterTemplates(ProductTemplateDefinition[] templates)
    {
        _templateLookup.Clear();
        _categoryTemplateMap.Clear();
        if (templates == null) return;
        for (int i = 0; i < templates.Length; i++)
        {
            var t = templates[i];
            if (t == null || string.IsNullOrEmpty(t.templateId)) continue;
            _templateLookup[t.templateId] = t;
            if (MarketState.IsConsumerCategory(t.category))
                _categoryTemplateMap[t.category] = t;
        }
    }

    public float GetNicheDevTimeMultiplier(ProductNiche niche)
    {
        if (_nicheLookup.TryGetValue(niche, out var data))
            return data.devTimeMultiplier;
        return 1f;
    }

    public void ForceInitialShareResolution(int tick)
    {
        _suppressShowdowns = true;
        ResolveDailyMarketShares(tick);
        _suppressShowdowns = false;
        _lastDayShareResolutionTick = tick;
    }

    public void PreTick(int tick) { }

    public void Tick(int tick)
    {
        if ((tick - _state.lastUpdateTick) >= DemandUpdateIntervalTicks)
        {
            UpdateNicheDemands(tick);
            UpdateCategoryDemands(tick);
            _state.lastUpdateTick = tick;
        }

        if ((tick - _lastDayShareResolutionTick) >= TimeState.TicksPerDay)
        {
            _lastDayShareResolutionTick = tick;
            TickViralSpikes(tick);
            ResolveDailyMarketShares(tick);
        }

        if ((tick - _lastDayProjectionTick) >= TimeState.TicksPerDay)
        {
            _lastDayProjectionTick = tick;
            int currentDay = tick / TimeState.TicksPerDay;
            AdvanceProjectionDaily(currentDay);
            AdvanceCategoryProjectionDaily(currentDay);
        }
    }

    public void PostTick(int tick)
    {
        int count = _pendingEvents.Count;
        for (int i = 0; i < count; i++)
        {
            var e = _pendingEvents[i];
            switch (e.Kind)
            {
                case PendingEventKind.DemandUpdated:
                    OnNicheDemandUpdated?.Invoke(e.Niche, e.Demand, e.Trend);
                    break;
                case PendingEventKind.DemandSpiked:
                    OnNicheDemandSpiked?.Invoke(e.Niche, e.Demand);
                    break;
                case PendingEventKind.DemandCrashed:
                    OnNicheDemandCrashed?.Invoke(e.Niche, e.Demand);
                    break;
                case PendingEventKind.ShowdownResolved:
                    OnShowdownResolved?.Invoke(e.Niche, e.Showdown);
                    break;
            }
        }
        _pendingEvents.Clear();
    }

    public void ApplyCommand(ICommand command) { }

    public void Dispose() { }

    private void ResolveDailyMarketShares(int tick)
    {
        _nicheKeys.Clear();
        foreach (var key in _state.nicheDemand.Keys)
            _nicheKeys.Add(key);

        if (_state.currentMarketShares == null)
            _state.currentMarketShares = new Dictionary<ProductNiche, List<MarketShareEntry>>();

        int nicheCount = _nicheKeys.Count;
        for (int n = 0; n < nicheCount; n++)
        {
            var niche = _nicheKeys[n];
            float nicheDemand = _state.nicheDemand[niche];

            ApplyDisruptionDemandEffect(niche, ref nicheDemand);

            _nicheProducts.Clear();
            _upcomingProducts.Clear();
            _newReleasesThisMonth.Clear();

            CollectProductsInNiche(niche, tick, _nicheProducts, _upcomingProducts, _newReleasesThisMonth);

            if (_upcomingProducts.Count > 0)
                MarketShareResolver.ApplyAnticipationEffect(niche, _nicheProducts, _upcomingProducts);

            int nicheMaxUsers = 50_000_000;
            float nicheBasePrice = 20f;
            float nicheSubPrice = 10f;
            if (_nicheLookup.TryGetValue(niche, out var nicheData)) {
                nicheMaxUsers = nicheData.maxUserPool;
                nicheBasePrice = nicheData.basePricePerUnit;
                nicheSubPrice = nicheData.baseSubscriptionPrice;
            }

            float utilization = _state.nichePoolUtilization != null && _state.nichePoolUtilization.TryGetValue(niche, out float u) ? u : 0.1f;
            int effectiveMaxUsers = (int)(nicheMaxUsers * utilization);
            if (effectiveMaxUsers < 1) effectiveMaxUsers = 1;

            float nicheInterestRate = nicheData != null ? nicheData.interestRate : 0.15f;
            if (nicheInterestRate <= 0f) nicheInterestRate = 0.15f;

            var entries = MarketShareResolver.ResolveNiche(niche, _nicheProducts, nicheDemand, MarketShareSaturationCap, effectiveMaxUsers, nicheBasePrice, nicheSubPrice,
                _generationSystem, _platformSystem, _featureLookup, nicheData, nicheInterestRate);
            UpdateProductsFromEntries(entries);

            _state.currentMarketShares[niche] = entries;

            if (_newReleasesThisMonth.Count >= 2)
            {
                var todayReleases = _newReleasesThisMonth;
                int launchCount = 0;
                for (int r = 0; r < todayReleases.Count; r++)
                {
                    if ((tick - todayReleases[r].ShipTick) < TimeState.TicksPerDay)
                        launchCount++;
                }
                if (!_suppressShowdowns && launchCount >= 2)
                {
                    var showdown = MarketShareResolver.DetectAndResolveShowdown(niche, _newReleasesThisMonth, tick);
                    if (showdown.Occurred)
                    {
                        _pendingEvents.Add(new PendingMarketEvent {
                            Kind = PendingEventKind.ShowdownResolved,
                            Niche = niche,
                            Showdown = showdown
                        });
                    }
                }
            }
        }

        RecalculatePlatformUsers();
        ResolveCategoryMarketShares(tick);
        RecalculateToolUsers();

        foreach (var nicheEntries in _state.currentMarketShares.Values)
        {
            long nicheTotal = 0L;
            int eCount = nicheEntries.Count;
            for (int i = 0; i < eCount; i++)
                nicheTotal += nicheEntries[i].ActiveUsers;
            for (int i = 0; i < eCount; i++)
            {
                var entry = nicheEntries[i];
                entry.GlobalUserSharePercent = nicheTotal > 0 ? (float)entry.ActiveUsers / nicheTotal : 0f;
                nicheEntries[i] = entry;
            }
        }
        if (_state.categoryMarketShares != null)
        {
            foreach (var catEntries in _state.categoryMarketShares.Values)
            {
                long catTotal = 0L;
                int eCount = catEntries.Count;
                for (int i = 0; i < eCount; i++)
                    catTotal += catEntries[i].ActiveUsers;
                for (int i = 0; i < eCount; i++)
                {
                    var entry = catEntries[i];
                    entry.GlobalUserSharePercent = catTotal > 0 ? (float)entry.ActiveUsers / catTotal : 0f;
                    catEntries[i] = entry;
                }
            }
        }

    }

    private void ApplyDisruptionDemandEffect(ProductNiche niche, ref float nicheDemand)
    {
        if (_disruptionSystem == null) return;

        var active = _disruptionSystem.GetActiveDisruptions();
        int count = active.Count;
        for (int i = 0; i < count; i++)
        {
            var d = active[i];
            if (d.EventType == DisruptionEventType.Recession)
            {
                nicheDemand *= 1f - (d.Magnitude / 100f * 0.3f);
            }
            else if (d.EventType == DisruptionEventType.EconomicBoom)
            {
                nicheDemand *= 1f + (d.Magnitude / 100f * 0.15f);
            }
            else if (d.EventType == DisruptionEventType.EconomicDip)
            {
                nicheDemand *= 1f - (d.Magnitude / 100f * 0.15f);
            }
            else if (d.EventType == DisruptionEventType.TechParadigmShift && d.AffectedNiche.HasValue && d.AffectedNiche.Value == niche)
            {
                nicheDemand *= 1f + (d.Magnitude / 100f * 0.25f);
            }
        }
    }

    private void CollectProductsInNiche(
        ProductNiche niche,
        int tick,
        List<Product> onMarket,
        List<Product> upcoming,
        List<Product> newThisMonth)
    {
        foreach (var kvp in _productState.shippedProducts)
        {
            var product = kvp.Value;
            if (!product.IsOnMarket) continue;
            if (!ProductIsInNiche(product, niche)) continue;
            onMarket.Add(product);
            if ((tick - product.ShipTick) < TimeState.TicksPerDay)
                newThisMonth.Add(product);
        }

        if (_competitorState != null)
        {
            foreach (var compKvp in _competitorState.competitors)
            {
                var comp = compKvp.Value;
                if (comp.IsBankrupt || comp.IsAbsorbed) continue;
                if (comp.InDevelopmentProductIds == null) continue;

                int count = comp.InDevelopmentProductIds.Count;
                for (int i = 0; i < count; i++)
                {
                    var productId = comp.InDevelopmentProductIds[i];
                    if (_productState.developmentProducts.TryGetValue(productId, out var devProduct))
                    {
                        if (devProduct.TargetReleaseTick > 0 &&
                            devProduct.TargetReleaseTick <= tick + TimeState.TicksPerDay &&
                            ProductIsInNiche(devProduct, niche))
                        {
                            upcoming.Add(devProduct);
                        }
                    }
                }
            }
        }
    }

    private void UpdateProductsFromEntries(List<MarketShareEntry> entries)
    {
        int count = entries.Count;
        for (int i = 0; i < count; i++)
        {
            var entry = entries[i];
            if (_productState.shippedProducts.TryGetValue(entry.ProductId, out var product))
            {
                product.PreviousActiveUsers = product.ActiveUserCount;
                product.ActiveUserCount = entry.ActiveUsers;
                product.MonthlyRevenue = entry.MonthlyRevenue;
                if (product.Category.IsTool() && product.DistributionModel != ToolDistributionModel.Proprietary)
                {
                    product.ActiveSubscriberCount = entry.ActiveUsers;
                    product.TotalSubscriptionRevenue += entry.MonthlyRevenue;
                }
                if (product.IsCompetitorProduct)
                    product.TotalLifetimeRevenue += entry.MonthlyRevenue / 30;
            }
        }
    }

    private void RecalculatePlatformUsers()
    {
        _platformProductIds.Clear();
        _platformUserAccum.Clear();

        foreach (var kvp in _productState.shippedProducts)
        {
            var product = kvp.Value;
            if (!product.IsOnMarket || !product.Category.IsPlatform()) continue;
            _platformProductIds.Add(kvp.Key);
            _platformUserAccum[kvp.Key] = 0;
        }

        int platformCount = _platformProductIds.Count;
        if (platformCount == 0) return;

        foreach (var kvp in _productState.shippedProducts)
        {
            var product = kvp.Value;
            if (!product.IsOnMarket || product.TargetPlatformIds == null) continue;
            for (int p = 0; p < platformCount; p++)
            {
                var platformId = _platformProductIds[p];
                bool targets = false;
                for (int t = 0; t < product.TargetPlatformIds.Length; t++)
                {
                    if (product.TargetPlatformIds[t] == platformId) { targets = true; break; }
                }
                if (targets)
                    _platformUserAccum[platformId] = _platformUserAccum[platformId] + product.ActiveUserCount;
            }
        }

        for (int p = 0; p < platformCount; p++)
        {
            var platformId = _platformProductIds[p];
            if (!_productState.shippedProducts.TryGetValue(platformId, out var platform)) continue;
            platform.ActiveUserCount = platform.ActiveUserCount + _platformUserAccum[platformId];
        }
    }

    private void RecalculateToolUsers()
    {
        foreach (var kvp in _productState.shippedProducts)
        {
            var tool = kvp.Value;
            if (!tool.IsOnMarket || !tool.Category.IsTool()) continue;

            if (tool.DistributionModel == ToolDistributionModel.Proprietary)
            {
                tool.ActiveSubscriberCount = 0;
                tool.ActiveUserCount = tool.ActiveLicenseeCount;
                continue;
            }

            // B2B: licensees + downstream end-users of products that use this tool
            int endUserSum = 0;
            var toolId = kvp.Key;
            foreach (var innerKvp in _productState.shippedProducts)
            {
                var user = innerKvp.Value;
                if (!user.IsOnMarket || user.RequiredToolIds == null) continue;
                bool usesTool = false;
                for (int t = 0; t < user.RequiredToolIds.Length; t++)
                {
                    if (user.RequiredToolIds[t] == toolId) { usesTool = true; break; }
                }
                if (usesTool) endUserSum += user.ActiveUserCount;
            }
            float endUserFraction = 0.05f + (tool.OverallQuality / 100f) * 0.10f;
            int b2bUsers = tool.ActiveLicenseeCount + (int)(endUserSum * endUserFraction);

            // B2C: ActiveSubscriberCount was set by UpdateProductsFromEntries during ResolveCategoryMarketShares
            tool.ActiveUserCount = b2bUsers + tool.ActiveSubscriberCount;
        }
    }

    private bool ProductIsInNiche(Product product, ProductNiche niche)
    {
        return product.Niche == niche;
    }

    private void UpdateNicheDemands(int tick)
    {
        _nicheKeys.Clear();
        foreach (var key in _state.nicheDemand.Keys)
            _nicheKeys.Add(key);

        int count = _nicheKeys.Count;
        for (int i = 0; i < count; i++)
        {
            var niche = _nicheKeys[i];
            if (!_nicheLookup.TryGetValue(niche, out var config)) continue;

            float currentDemand = _state.nicheDemand[niche];

            float momentum = _state.nicheMomentum[niche];

            // Per-niche volatility from NicheConfig on the template (if any product in niche
            // has a template with a matching nicheConfig, use that volatility override).
            float nicheVolatility = ResolveNicheVolatility(niche, config.volatility);
            float drift = (_rng.NextFloat01() * 2f - 1f) * nicheVolatility;
            momentum = Lerp(momentum, drift, 0.3f);
            _state.nicheMomentum[niche] = momentum;

            // Variable recovery rate — 20% chance per month to re-roll
            if (_rng.Chance(0.2f))
            {
                float newRate = config.recoveryRateMin +
                    _rng.NextFloat01() * (config.recoveryRateMax - config.recoveryRateMin);
                _state.nicheRecoveryRate[niche] = newRate;
            }
            float currentRate = _state.nicheRecoveryRate[niche];

            // Per-niche retention from NicheConfig feeds into lifecycle duration.
            // Higher retention = products live longer = better recovery multiplier per good product.
            float retentionBonus = ResolveNicheRetentionBonus(niche);
            int goodProducts = CountGoodProductsInNiche(niche);
            float recoveryMult = 1f + System.Math.Min(goodProducts * 0.2f * (1f + retentionBonus), 1.5f);
            float effectiveRecoveryRate = currentRate * recoveryMult;
            float attractorTarget = _state.nicheAttractor != null && _state.nicheAttractor.TryGetValue(niche, out float att) ? att : config.baseDemand;
            float recovery = (attractorTarget - currentDemand) * effectiveRecoveryRate;

            // Per-niche saturation with configurable threshold
            int totalProducts = CountAllProductsInNiche(niche);
            float saturation = totalProducts > config.saturationThreshold
                ? (totalProducts - config.saturationThreshold) * config.saturationPenaltyPerProduct
                : 0f;
            float newDemand = Clamp(currentDemand + momentum + recovery - saturation, config.demandFloor, config.demandCeiling);

            // Platform-aware demand scaling: multiply by the aggregate addressable market
            // coverage of products in this niche relative to all available platforms.
            float platformFactor = ComputeNichePlatformFactor(niche);
            newDemand = Clamp(newDemand * platformFactor, config.demandFloor, config.demandCeiling);

            MarketTrend trend;
            if (newDemand > currentDemand + 1f)
                trend = MarketTrend.Rising;
            else if (newDemand < currentDemand - 1f)
                trend = MarketTrend.Falling;
            else
                trend = MarketTrend.Stable;

            float oldDemand = currentDemand;
            _state.nicheDemand[niche] = newDemand;
            _state.nicheTrends[niche] = trend;

            float delta = newDemand - oldDemand;

            if (delta > 15f)
                _pendingEvents.Add(new PendingMarketEvent { Kind = PendingEventKind.DemandSpiked, Niche = niche, Demand = newDemand });
            else if (delta < -15f)
                _pendingEvents.Add(new PendingMarketEvent { Kind = PendingEventKind.DemandCrashed, Niche = niche, Demand = newDemand });

            _pendingEvents.Add(new PendingMarketEvent { Kind = PendingEventKind.DemandUpdated, Niche = niche, Demand = newDemand, Trend = trend });
            RerollPoolUtilization(niche, config, newDemand);        }
    }

    private float ResolveNicheVolatility(ProductNiche niche, float defaultVolatility)
    {
        float maxVolatility = defaultVolatility;
        foreach (var kvp in _productState.shippedProducts)
        {
            var p = kvp.Value;
            if (!p.IsOnMarket || p.Niche != niche) continue;
            if (string.IsNullOrEmpty(p.TemplateId)) continue;
            if (!_templateLookup.TryGetValue(p.TemplateId, out var template)) continue;
            float templateVolatility = template.economyConfig != null ? template.economyConfig.nicheVolatility : 0f;
            if (templateVolatility > maxVolatility)
                maxVolatility = templateVolatility;
        }
        return maxVolatility;
    }

    private float ResolveNicheRetentionBonus(ProductNiche niche)
    {
        int maxRetention = 0;
        foreach (var kvp in _productState.shippedProducts)
        {
            var p = kvp.Value;
            if (!p.IsOnMarket || p.Niche != niche) continue;
            if (string.IsNullOrEmpty(p.TemplateId)) continue;
            if (!_templateLookup.TryGetValue(p.TemplateId, out var template)) continue;
            int retention = template.economyConfig != null ? template.economyConfig.retentionMonths : 0;
            if (retention > maxRetention)
                maxRetention = retention;
        }
        float t = (float)(maxRetention - 12) / 24f;
        if (t < 0f) t = 0f;
        if (t > 1f) t = 1f;
        return t * 0.5f;
    }

    private float ComputeNichePlatformFactor(ProductNiche niche)
    {
        if (_platformSystem == null) return 1f;

        // Collect all unique target platform IDs used by products in this niche
        _nicheProducts.Clear();
        foreach (var kvp in _productState.shippedProducts)
        {
            var p = kvp.Value;
            if (p.IsOnMarket && p.Niche == niche && p.TargetPlatformIds != null && p.TargetPlatformIds.Length > 0)
                _nicheProducts.Add(p);
        }

        if (_nicheProducts.Count == 0) return 1f;

        // Aggregate max addressable market across all products in niche
        float maxAddressable = 0f;
        for (int i = 0; i < _nicheProducts.Count; i++)
        {
            var p = _nicheProducts[i];
            if (p.TargetPlatformIds == null) continue;
            float addressable = _platformSystem.GetAddressableMarket(niche, p.TargetPlatformIds);
            if (addressable > maxAddressable) maxAddressable = addressable;
        }

        // Scale demand: no platforms (generic) = factor 1.0, full coverage = factor 1.3
        // Products with generic targeting use factor 1.0 (no boost, no penalty)
        if (maxAddressable <= 0f) return 1f;
        return 1f + maxAddressable * 0.3f;
    }

    private int CountGoodProductsInNiche(ProductNiche niche)
    {
        int count = 0;
        foreach (var product in _productState.shippedProducts.Values)
        {
            if (!product.IsOnMarket) continue;
            float aggregateQuality = ComputeAggregateFeatureQuality(product);
            if (product.PopularityScore <= 60f || aggregateQuality <= 70f) continue;
            if (product.Niche == niche) count++;
        }
        return count;
    }

    private float ComputeAggregateFeatureQuality(Product product) {
        if (product.Features == null || product.Features.Length == 0)
            return product.OverallQuality;
        float sum = 0f;
        int count = product.Features.Length;
        for (int i = 0; i < count; i++) {
            if (product.Features[i] != null)
                sum += product.Features[i].EffectiveQuality;
        }
        return count > 0 ? sum / count : product.OverallQuality;
    }

    private int CountAllProductsInNiche(ProductNiche niche)
    {
        int count = 0;
        foreach (var product in _productState.shippedProducts.Values)
        {
            if (!product.IsOnMarket) continue;
            if (product.Niche == niche) count++;
        }
        return count;
    }

    private void RerollPoolUtilization(ProductNiche niche, MarketNicheData nicheData, float nicheDemand)
    {
        if (_state.nichePoolUtilization == null) return;
        if (_state.nicheViralSpikeDaysRemaining == null) return;

        int productCount = CountAllProductsInNiche(niche);
        float baseGrowth = 0.02f + 0.08f * (float)System.Math.Log(1.0 + productCount);
        if (baseGrowth < 0.02f) baseGrowth = 0.02f;
        if (baseGrowth > 0.35f) baseGrowth = 0.35f;

        float demandFactor = nicheDemand / 100f;

        int gameMonth = (int)System.Math.Round((float)_state.lastUpdateTick / DemandUpdateIntervalTicks);
        float maturityFactor = 1.0f + 0.5f * System.Math.Min(gameMonth / 120f, 1.0f);

        float avgQuality = ComputeAverageNicheQuality(niche);
        float qualityMult = avgQuality > 0f ? avgQuality / 100f : 0.5f;
        if (qualityMult < 0.3f) qualityMult = 0.3f;

        float target = baseGrowth * demandFactor * maturityFactor * qualityMult;
        if (target < 0.01f) target = 0.01f;
        if (target > 0.50f) target = 0.50f;

        float current = _state.nichePoolUtilization.TryGetValue(niche, out float cur) ? cur : 0.02f;
        float utilization = Lerp(current, target, UtilizationLerpRate);
        if (utilization < 0.01f) utilization = 0.01f;
        if (utilization > 0.50f) utilization = 0.50f;

        if (_rng.Chance(0.001f))
        {
            utilization = 1.0f;
            _state.nicheViralSpikeDaysRemaining[niche] = _rng.Range(1, 31);
        }

        _state.nichePoolUtilization[niche] = utilization;
    }

    private void TickViralSpikes(int tick)
    {
        if (_state.nicheViralSpikeDaysRemaining == null) return;
        if (_state.nichePoolUtilization == null) return;
        if (tick % TimeState.TicksPerDay != 0) return;

        _nicheKeys.Clear();
        foreach (var key in _state.nicheViralSpikeDaysRemaining.Keys)
            _nicheKeys.Add(key);

        int count = _nicheKeys.Count;
        for (int i = 0; i < count; i++)
        {
            var niche = _nicheKeys[i];
            int remaining = _state.nicheViralSpikeDaysRemaining[niche];
            if (remaining <= 0) continue;
            remaining--;
            _state.nicheViralSpikeDaysRemaining[niche] = remaining;
            if (remaining == 0)
            {
                if (_nicheLookup.TryGetValue(niche, out var config))
                {
                    float demand = _state.nicheDemand.TryGetValue(niche, out float d) ? d : 50f;
                    RerollPoolUtilization(niche, config, demand);
                }
            }
        }
    }

    private void UpdateCategoryDemands(int tick)
    {
        if (_state.categoryDemand == null || _state.categoryDemand.Count == 0) return;

        _categoryKeys.Clear();
        foreach (var key in _state.categoryDemand.Keys)
            _categoryKeys.Add(key);

        int count = _categoryKeys.Count;
        for (int i = 0; i < count; i++)
        {
            var cat = _categoryKeys[i];
            if (!_categoryTemplateMap.TryGetValue(cat, out var template)) continue;
            var ec = template.economyConfig;
            if (ec == null) continue;

            float currentDemand = _state.categoryDemand[cat];
            float momentum = _state.categoryMomentum[cat];

            float drift = (_rng.NextFloat01() * 2f - 1f) * ec.nicheVolatility;
            momentum = Lerp(momentum, drift, 0.3f);
            _state.categoryMomentum[cat] = momentum;

            if (_rng.Chance(0.2f))
            {
                float newRate = ec.recoveryRateMin + _rng.NextFloat01() * (ec.recoveryRateMax - ec.recoveryRateMin);
                _state.categoryRecoveryRate[cat] = newRate;
            }
            float currentRate = _state.categoryRecoveryRate[cat];

            float attractorTarget = _state.categoryAttractor != null && _state.categoryAttractor.TryGetValue(cat, out float att) ? att : ec.baseDemand;
            float recovery = (attractorTarget - currentDemand) * currentRate;

            int totalProducts = CountAllProductsInCategory(cat);
            float saturation = totalProducts > ec.saturationThreshold
                ? (totalProducts - ec.saturationThreshold) * ec.saturationPenaltyPerProduct
                : 0f;

            float newDemand = Clamp(currentDemand + momentum + recovery - saturation, ec.demandFloor, ec.demandCeiling);

            MarketTrend trend;
            if (newDemand > currentDemand + 1f)
                trend = MarketTrend.Rising;
            else if (newDemand < currentDemand - 1f)
                trend = MarketTrend.Falling;
            else
                trend = MarketTrend.Stable;

            _state.categoryDemand[cat] = newDemand;
            _state.categoryTrends[cat] = trend;
            UpdateCategoryPoolUtilization(cat, ec, newDemand);
        }
    }

    private int CountAllProductsInCategory(ProductCategory category)
    {
        int count = 0;
        foreach (var product in _productState.shippedProducts.Values)
        {
            if (!product.IsOnMarket) continue;
            if (product.Category == category && product.Niche == ProductNiche.None) count++;
        }
        return count;
    }

    private void UpdateCategoryPoolUtilization(ProductCategory cat, ProductEconomyConfig ec, float catDemand)
    {
        if (_state.categoryPoolUtilization == null) return;

        int productCount = CountAllProductsInCategory(cat);
        float baseGrowth = 0.02f + 0.08f * (float)System.Math.Log(1.0 + productCount);
        if (baseGrowth < 0.02f) baseGrowth = 0.02f;
        if (baseGrowth > 0.35f) baseGrowth = 0.35f;

        float demandFactor = catDemand / 100f;
        int gameMonth = (int)System.Math.Round((float)_state.lastUpdateTick / DemandUpdateIntervalTicks);
        float maturityFactor = 1.0f + 0.5f * System.Math.Min(gameMonth / 120f, 1.0f);

        float avgCatQuality = ComputeAverageCategoryQuality(cat);
        float catQualityMult = avgCatQuality > 0f ? avgCatQuality / 100f : 0.5f;
        if (catQualityMult < 0.3f) catQualityMult = 0.3f;

        float target = baseGrowth * demandFactor * maturityFactor * catQualityMult;
        if (target < 0.01f) target = 0.01f;
        if (target > 0.50f) target = 0.50f;

        float current = _state.categoryPoolUtilization.TryGetValue(cat, out float cur) ? cur : 0.02f;
        float utilization = Lerp(current, target, UtilizationLerpRate);
        if (utilization < 0.01f) utilization = 0.01f;
        if (utilization > 0.50f) utilization = 0.50f;

        _state.categoryPoolUtilization[cat] = utilization;
    }

    private float GetCategoryUtilization(ProductCategory cat, ProductEconomyConfig ec, float catDemand)
    {
        if (_state.categoryPoolUtilization == null) return 0.02f;
        if (_state.categoryPoolUtilization.TryGetValue(cat, out float util))
            return util;
        return 0.02f;
    }

    private float ComputeAverageNicheQuality(ProductNiche niche)
    {
        float qualitySum = 0f;
        int count = 0;
        foreach (var kvp in _productState.shippedProducts)
        {
            var product = kvp.Value;
            if (!product.IsOnMarket || product.Niche != niche) continue;
            qualitySum += product.OverallQuality;
            count++;
        }
        return count > 0 ? qualitySum / count : 0f;
    }

    private float ComputeAverageCategoryQuality(ProductCategory cat)
    {
        float qualitySum = 0f;
        int count = 0;
        foreach (var kvp in _productState.shippedProducts)
        {
            var product = kvp.Value;
            if (!product.IsOnMarket || product.Category != cat || product.Niche != ProductNiche.None) continue;
            qualitySum += product.OverallQuality;
            count++;
        }
        return count > 0 ? qualitySum / count : 0f;
    }

    private void ResolveCategoryMarketShares(int tick)
    {
        if (_state.categoryDemand == null || _state.categoryDemand.Count == 0) return;

        if (_state.categoryMarketShares == null)
            _state.categoryMarketShares = new Dictionary<ProductCategory, List<MarketShareEntry>>();

        _categoryKeys.Clear();
        foreach (var key in _state.categoryDemand.Keys)
            _categoryKeys.Add(key);

        int catCount = _categoryKeys.Count;
        for (int n = 0; n < catCount; n++)
        {
            var cat = _categoryKeys[n];
            if (!_categoryTemplateMap.TryGetValue(cat, out var template)) continue;
            var ec = template.economyConfig;
            if (ec == null) continue;

            float catDemand = _state.categoryDemand[cat];

            _nicheProducts.Clear();
            foreach (var kvp in _productState.shippedProducts)
            {
                var product = kvp.Value;
                if (!product.IsOnMarket) continue;
                if (product.Category != cat || product.Niche != ProductNiche.None) continue;
                if (product.Category.IsTool() && product.DistributionModel == ToolDistributionModel.Proprietary) continue;
                _nicheProducts.Add(product);
            }

            float catUtilization = GetCategoryUtilization(cat, ec, catDemand);
            int effectiveCatMaxUsers = (int)(ec.maxUserPool * catUtilization);
            if (effectiveCatMaxUsers < 1) effectiveCatMaxUsers = 1;

            float catInterestRate = ec.interestRate > 0f ? ec.interestRate : 0.15f;

            var entries = MarketShareResolver.ResolveNiche(
                ProductNiche.None, _nicheProducts, catDemand, MarketShareSaturationCap,
                effectiveCatMaxUsers, ec.basePricePerUnit, ec.baseSubscriptionPrice,
                _generationSystem, _platformSystem, _featureLookup, null, catInterestRate);

            UpdateProductsFromEntries(entries);

            if (!_debugLoggedEngine && cat == ProductCategory.GameEngine)
            {
                _debugLoggedEngine = true;
                _logger.Log($"[DEBUG-Engine] cat={cat} effectiveMaxUsers={effectiveCatMaxUsers} interestRate={catInterestRate:F4} demand={catDemand:F2} basePrice={ec.basePricePerUnit:F2}");
                int eCount = entries.Count;
                for (int ei = 0; ei < eCount; ei++)
                {
                    var e = entries[ei];
                    _logger.Log($"  [DEBUG-Engine] id={e.ProductId} activeUsers={e.ActiveUsers} monthlyRevenue={e.MonthlyRevenue:F0} sharePercent={e.MarketSharePercent:F4} penetration={e.PenetrationRate:F4}");
                }
            }

            _state.categoryMarketShares[cat] = entries;
        }
    }

    // ─── Projection API ─────────────────────────────────────────────────────────

    public List<float> ProjectNicheDemand(ProductNiche niche, int dataPoints)
    {
        int days = dataPoints;
        if (days > ProjectionDays) days = ProjectionDays;
        var result = new List<float>(days);
        if (_state.nicheProjections == null ||
            !_state.nicheProjections.TryGetValue(niche, out var buffer))
            return result;
        int head = _state.projectionHead;
        for (int i = 0; i < days; i++)
            result.Add(buffer[(head + i) % ProjectionDays]);
        return result;
    }

    public List<float> ProjectCategoryDemand(ProductCategory category, int dataPoints)
    {
        int days = dataPoints;
        if (days > ProjectionDays) days = ProjectionDays;
        var result = new List<float>(days);
        if (_state.categoryProjections == null ||
            !_state.categoryProjections.TryGetValue(category, out var buffer))
            return result;
        int head = _state.projectionHead;
        for (int i = 0; i < days; i++)
            result.Add(buffer[(head + i) % ProjectionDays]);
        return result;
    }

    // ─── Public Read API ────────────────────────────────────────────────────────

    public float GetNicheDemand(ProductNiche niche)
    {
        if (_state.nicheDemand.TryGetValue(niche, out float demand))
            return demand;
        return 50f;
    }

    public MarketTrend GetNicheTrend(ProductNiche niche)
    {
        if (_state.nicheTrends.TryGetValue(niche, out var trend))
            return trend;
        return MarketTrend.Stable;
    }

    public float GetCombinedDemandMultiplier(Product product)
    {
        if (product == null) return 1.0f;
        float nicheDemand = GetNicheDemand(product.Niche);
        return 0.5f + (nicheDemand / 100f);
    }

    public float GetCategoryDemand(ProductCategory category)
    {
        if (_state.categoryDemand != null && _state.categoryDemand.TryGetValue(category, out float demand))
            return demand;
        return 50f;
    }

    public MarketTrend GetCategoryTrend(ProductCategory category)
    {
        if (_state.categoryTrends != null && _state.categoryTrends.TryGetValue(category, out var trend))
            return trend;
        return MarketTrend.Stable;
    }

    // ─── Feature Demand Read API ─────────────────────────────────────────────────

    public float GetFeatureCategoryAffinity(ProductNiche niche, FeatureCategory category)
    {
        if (_nicheLookup.TryGetValue(niche, out var nicheData))
            return nicheData.GetAffinityForCategory(category);
        return 1.0f;
    }

    public FeatureCategory GetFeatureCategory(string featureId)
    {
        if (_featureLookup.TryGetValue(featureId, out var cfg))
            return cfg.featureCategory;
        return FeatureCategory.Core;
    }

    public float GetFeatureAdoptionRate(string featureId, ProductNiche niche, string templateId)
    {
        int totalInNiche = 0;
        int withFeature = 0;
        foreach (var kvp in _productState.shippedProducts)
        {
            var p = kvp.Value;
            if (!p.IsOnMarket || p.Niche != niche || p.TemplateId != templateId) continue;
            totalInNiche++;
            if (p.SelectedFeatureIds == null) continue;
            int fCount = p.SelectedFeatureIds.Length;
            for (int i = 0; i < fCount; i++)
            {
                if (p.SelectedFeatureIds[i] == featureId) { withFeature++; break; }
            }
        }
        if (totalInNiche == 0) return 0f;
        return (float)withFeature / totalInNiche;
    }

    // ─── Uplift API ─────────────────────────────────────────────────────────────

    public void ApplyNicheUplift(ProductNiche niche, float amount)
    {
        if (!_nicheLookup.TryGetValue(niche, out var config)) return;
        if (!_state.nicheDemand.ContainsKey(niche)) return;
        float newVal = _state.nicheDemand[niche] + amount;
        if (newVal > config.demandCeiling) newVal = config.demandCeiling;
        _state.nicheDemand[niche] = newVal;
    }

    // ─── Projection Buffer ──────────────────────────────────────────────────────

    public void InitializeProjections(int currentDay)
    {
        if (_state.nicheProjections == null)
            _state.nicheProjections = new Dictionary<ProductNiche, float[]>();

        if (_projScratchDemand == null) _projScratchDemand = new float[64];
        if (_projScratchMomentum == null) _projScratchMomentum = new float[64];
        if (_projScratchRecoveryRate == null) _projScratchRecoveryRate = new float[64];
        if (_projMonthlySnapshots == null) _projMonthlySnapshots = new float[64 * 13];

        _nicheKeys.Clear();
        foreach (var key in _state.nicheDemand.Keys)
            _nicheKeys.Add(key);

        int count = _nicheKeys.Count;
        for (int n = 0; n < count; n++)
        {
            var niche = _nicheKeys[n];
            if (!_nicheLookup.TryGetValue(niche, out var config)) continue;

            if (!_state.nicheProjections.TryGetValue(niche, out var buffer) || buffer == null || buffer.Length != ProjectionDays)
            {
                buffer = new float[ProjectionDays];
                _state.nicheProjections[niche] = buffer;
            }
        }

        if (_state.categoryProjections == null)
            _state.categoryProjections = new Dictionary<ProductCategory, float[]>();

        if (_state.categoryDemand != null)
        {
            _categoryKeys.Clear();
            foreach (var key in _state.categoryDemand.Keys)
                _categoryKeys.Add(key);

            int catCount = _categoryKeys.Count;
            for (int n = 0; n < catCount; n++)
            {
                var cat = _categoryKeys[n];
                if (!_state.categoryProjections.TryGetValue(cat, out var buffer) || buffer == null || buffer.Length != ProjectionDays)
                {
                    buffer = new float[ProjectionDays];
                    _state.categoryProjections[cat] = buffer;
                }
            }
        }

        RebuildAllProjections(currentDay);
        RebuildCategoryProjections(currentDay);
    }

    public void AdvanceCategoryProjectionDaily(int currentDay)
    {
        if (_state.categoryProjections == null) return;
        RebuildCategoryProjections(currentDay);
    }

    private void RebuildCategoryProjections(int currentDay)
    {
        if (_state.categoryDemand == null || _state.categoryDemand.Count == 0) return;
        if (_state.categoryProjections == null)
            _state.categoryProjections = new Dictionary<ProductCategory, float[]>();

        int demandEpoch = _state.lastUpdateTick / DemandUpdateIntervalTicks;
        int forkSeed = _masterSeed ^ "category_market".GetHashCode() ^ demandEpoch;
        var projRng = new RngStream(forkSeed);

        _categoryKeys.Clear();
        foreach (var key in _state.categoryDemand.Keys)
            _categoryKeys.Add(key);

        int catCount = _categoryKeys.Count;
        if (catCount == 0) return;

        var scratchDemand = new float[catCount];
        var scratchMomentum = new float[catCount];
        var scratchRecovery = new float[catCount];
        var monthlySnapshots = new float[catCount * 13];

        for (int i = 0; i < catCount; i++)
        {
            var cat = _categoryKeys[i];
            scratchDemand[i] = _state.categoryDemand[cat];
            scratchMomentum[i] = _state.categoryMomentum.TryGetValue(cat, out float m) ? m : 0f;
            scratchRecovery[i] = _state.categoryRecoveryRate.TryGetValue(cat, out float r) ? r : 0.05f;
            monthlySnapshots[i * 13] = scratchDemand[i];
        }

        for (int month = 0; month < 12; month++)
        {
            for (int i = 0; i < catCount; i++)
            {
                var cat = _categoryKeys[i];
                if (!_categoryTemplateMap.TryGetValue(cat, out var template)) continue;
                var ec = template.economyConfig;
                if (ec == null) continue;

                float drift = (projRng.NextFloat01() * 2f - 1f) * ec.nicheVolatility;
                float momentum = Lerp(scratchMomentum[i], drift, 0.3f);
                scratchMomentum[i] = momentum;

                if (projRng.Chance(0.2f))
                {
                    float newRate = ec.recoveryRateMin + projRng.NextFloat01() * (ec.recoveryRateMax - ec.recoveryRateMin);
                    scratchRecovery[i] = newRate;
                }

                float attractorTarget = _state.categoryAttractor != null && _state.categoryAttractor.TryGetValue(cat, out float att) ? att : ec.baseDemand;
                float recovery = (attractorTarget - scratchDemand[i]) * scratchRecovery[i];
                int totalProducts = CountAllProductsInCategory(cat);
                float saturation = totalProducts > ec.saturationThreshold
                    ? (totalProducts - ec.saturationThreshold) * ec.saturationPenaltyPerProduct
                    : 0f;

                float newDemand = Clamp(scratchDemand[i] + momentum + recovery - saturation, ec.demandFloor, ec.demandCeiling);
                scratchDemand[i] = newDemand;

                if (month >= 3) {
                    float normalized = (float)(month - 2) / 9f;
                    float curve = (float)System.Math.Pow(normalized, ForecastNoiseExponent);
                    float noiseAmplitude = ForecastMaxNoise * curve;
                    float noise = (projRng.NextFloat01() * 2f - 1f) * noiseAmplitude;
                    newDemand = Clamp(newDemand + noise, ec.demandFloor, ec.demandCeiling);
                }

                monthlySnapshots[i * 13 + (month + 1)] = newDemand;
            }
        }

        for (int i = 0; i < catCount; i++)
        {
            var cat = _categoryKeys[i];
            if (!_state.categoryProjections.TryGetValue(cat, out var buffer) || buffer == null || buffer.Length != ProjectionDays)
            {
                buffer = new float[ProjectionDays];
                _state.categoryProjections[cat] = buffer;
            }

            for (int day = 0; day < ProjectionDays; day++)
            {
                int monthIdx = day / 30;
                if (monthIdx >= 12) monthIdx = 11;
                int nextMonthIdx = monthIdx + 1;
                if (nextMonthIdx > 12) nextMonthIdx = 12;
                float t = (day % 30) / 30f;
                buffer[day] = Lerp(monthlySnapshots[i * 13 + monthIdx], monthlySnapshots[i * 13 + nextMonthIdx], t);
            }
        }
    }

    public void AdvanceProjectionDaily(int currentDay)
    {
        if (_state.nicheProjections == null) return;
        RebuildAllProjections(currentDay);
    }

    private void RebuildAllProjections(int currentDay)
    {
        int demandEpoch = _state.lastUpdateTick / DemandUpdateIntervalTicks;
        int forkSeed = _masterSeed ^ "market".GetHashCode() ^ demandEpoch;
        var projRng = new RngStream(forkSeed);

        _nicheKeys.Clear();
        foreach (var key in _state.nicheDemand.Keys)
            _nicheKeys.Add(key);

        int nicheCount = _nicheKeys.Count;

        for (int i = 0; i < nicheCount; i++)
        {
            var niche = _nicheKeys[i];
            _projScratchDemand[i] = _state.nicheDemand[niche];
            _projScratchMomentum[i] = _state.nicheMomentum.TryGetValue(niche, out float m) ? m : 0f;
            _projScratchRecoveryRate[i] = _state.nicheRecoveryRate.TryGetValue(niche, out float r) ? r : 0.05f;
            _projMonthlySnapshots[i * 13] = _projScratchDemand[i];
        }

        for (int month = 0; month < 12; month++)
        {
            for (int i = 0; i < nicheCount; i++)
            {
                var niche = _nicheKeys[i];
                if (!_nicheLookup.TryGetValue(niche, out var config)) continue;

                float drift = (projRng.NextFloat01() * 2f - 1f) * config.volatility;
                float momentum = Lerp(_projScratchMomentum[i], drift, 0.3f);
                _projScratchMomentum[i] = momentum;

                // Mirror variable recovery re-roll
                if (projRng.Chance(0.2f))
                {
                    float newRate = config.recoveryRateMin +
                        projRng.NextFloat01() * (config.recoveryRateMax - config.recoveryRateMin);
                    _projScratchRecoveryRate[i] = newRate;
                }
                float currentRate = _projScratchRecoveryRate[i];

                int goodProducts = CountGoodProductsInNiche(niche);
                float recoveryMult = 1f + System.Math.Min(goodProducts * 0.2f, 1f);
                float effectiveRecoveryRate = currentRate * recoveryMult;
                float attractorTarget = _state.nicheAttractor != null && _state.nicheAttractor.TryGetValue(niche, out float att) ? att : config.baseDemand;
                float recovery = (attractorTarget - _projScratchDemand[i]) * effectiveRecoveryRate;

                int totalProducts = CountAllProductsInNiche(niche);
                float saturation = totalProducts > config.saturationThreshold
                    ? (totalProducts - config.saturationThreshold) * config.saturationPenaltyPerProduct
                    : 0f;

                float newDemand = Clamp(_projScratchDemand[i] + momentum + recovery - saturation, config.demandFloor, config.demandCeiling);
                _projScratchDemand[i] = newDemand;

                if (month >= 3) {
                    float normalized = (float)(month - 2) / 9f;
                    float curve = (float)System.Math.Pow(normalized, ForecastNoiseExponent);
                    float noiseAmplitude = ForecastMaxNoise * curve;
                    float noise = (projRng.NextFloat01() * 2f - 1f) * noiseAmplitude;
                    newDemand = Clamp(newDemand + noise, config.demandFloor, config.demandCeiling);
                }

                _projMonthlySnapshots[i * 13 + (month + 1)] = newDemand;
            }
        }

        for (int i = 0; i < nicheCount; i++)
        {
            var niche = _nicheKeys[i];
            if (!_state.nicheProjections.TryGetValue(niche, out var buffer)) continue;
            if (!_nicheLookup.TryGetValue(niche, out var config)) continue;

            for (int day = 0; day < ProjectionDays; day++)
            {
                int monthIdx = day / 30;
                if (monthIdx >= 12) monthIdx = 11;
                int nextMonthIdx = monthIdx + 1;
                if (nextMonthIdx > 12) nextMonthIdx = 12;
                float t = (day % 30) / 30f;
                buffer[day] = Lerp(_projMonthlySnapshots[i * 13 + monthIdx], _projMonthlySnapshots[i * 13 + nextMonthIdx], t);
            }
        }

        _state.projectionHead = 0;
        _state.projectionAnchorDay = currentDay;

        StampActiveDisruptionsIntoProjection(currentDay);
    }

    public void StampDisruptionIntoProjection(ActiveDisruption d, int currentDay)
    {
        if (_state.nicheProjections == null) return;
        if (d == null) return;

        int disruptStartDay = d.StartTick / TimeState.TicksPerDay;
        int disruptEndDay = (d.StartTick + d.DurationTicks) / TimeState.TicksPerDay;
        int startOffset = disruptStartDay - currentDay;
        if (startOffset < 0) startOffset = 0;
        int endOffset = disruptEndDay - currentDay;
        if (endOffset > ProjectionDays - 1) endOffset = ProjectionDays - 1;
        if (startOffset > endOffset) return;

        bool isNicheSpecific = d.EventType == DisruptionEventType.NicheDemandShift ||
                               d.EventType == DisruptionEventType.TechParadigmShift;

        _nicheKeys.Clear();
        foreach (var key in _state.nicheDemand.Keys)
            _nicheKeys.Add(key);

        int nicheCount = _nicheKeys.Count;
        int head = _state.projectionHead;

        for (int n = 0; n < nicheCount; n++)
        {
            var niche = _nicheKeys[n];

            if (isNicheSpecific)
            {
                if (!d.AffectedNiche.HasValue || d.AffectedNiche.Value != niche) continue;
            }

            if (!_state.nicheProjections.TryGetValue(niche, out var buffer)) continue;
            if (!_nicheLookup.TryGetValue(niche, out var config)) continue;

            for (int dayOffset = startOffset; dayOffset <= endOffset; dayOffset++)
            {
                int bufferIdx = (head + dayOffset) % ProjectionDays;

                switch (d.EventType)
                {
                    case DisruptionEventType.Recession:
                        buffer[bufferIdx] *= (1f - d.Magnitude / 100f * 0.3f);
                        break;
                    case DisruptionEventType.EconomicBoom:
                        buffer[bufferIdx] *= (1f + d.Magnitude / 100f * 0.15f);
                        break;
                    case DisruptionEventType.EconomicDip:
                        buffer[bufferIdx] *= (1f - d.Magnitude / 100f * 0.15f);
                        break;
                    case DisruptionEventType.NicheDemandShift:
                        buffer[bufferIdx] += d.Magnitude * 0.001f;
                        break;
                    case DisruptionEventType.TechParadigmShift:
                        buffer[bufferIdx] *= (1f + d.Magnitude / 100f * 0.25f);
                        break;
                }

                buffer[bufferIdx] = Clamp(buffer[bufferIdx], config.demandFloor, config.demandCeiling);
            }
        }
    }

    private void StampActiveDisruptionsIntoProjection(int currentDay)
    {
        if (_disruptionSystem == null) return;
        var active = _disruptionSystem.GetActiveDisruptions();
        int count = active.Count;
        for (int i = 0; i < count; i++)
        {
            StampDisruptionIntoProjection(active[i], currentDay);
        }
    }

    private static float Lerp(float a, float b, float t) => a + (b - a) * t;

    private static float Clamp(float value, float min, float max)
    {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }
}
