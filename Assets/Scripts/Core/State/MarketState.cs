using System;
using System.Collections.Generic;

[Serializable]
public class MarketState
{
    public const int ProjectionDays = 365;

    public Dictionary<ProductNiche, float> nicheDemand;
    public Dictionary<ProductNiche, MarketTrend> nicheTrends;
    public Dictionary<ProductNiche, float> nicheMomentum;
    public Dictionary<ProductNiche, float> nicheRecoveryRate;

    // Market share per niche — updated monthly by MarketSystem
    public Dictionary<ProductNiche, List<MarketShareEntry>> currentMarketShares;

    // Rolling 365-day demand projection circular buffer per niche
    public Dictionary<ProductNiche, float[]> nicheProjections;
    public int projectionHead;
    public int projectionAnchorDay;

    public int lastUpdateTick;

    // Pool utilization — determines what fraction of maxUserPool is available
    public Dictionary<ProductNiche, float> nichePoolUtilization;
    public Dictionary<ProductNiche, int> nicheViralSpikeDaysRemaining;

    // Per-playthrough mean-reversion attractors (randomized at game start)
    public Dictionary<ProductNiche, float> nicheAttractor;
    public Dictionary<ProductCategory, float> categoryAttractor;

    // Category-level demand for niche-less consumer products
    public Dictionary<ProductCategory, float> categoryDemand;
    public Dictionary<ProductCategory, MarketTrend> categoryTrends;
    public Dictionary<ProductCategory, float> categoryMomentum;
    public Dictionary<ProductCategory, float> categoryRecoveryRate;
    public Dictionary<ProductCategory, List<MarketShareEntry>> categoryMarketShares;
    public Dictionary<ProductCategory, float[]> categoryProjections;
    public Dictionary<ProductCategory, float> categoryPoolUtilization;

    public static MarketState CreateNew(MarketNicheData[] nicheConfigs, IRng rng, ProductTemplateDefinition[] templates = null)
    {
        var state = new MarketState
        {
            nicheDemand          = new Dictionary<ProductNiche, float>(),
            nicheTrends          = new Dictionary<ProductNiche, MarketTrend>(),
            nicheMomentum        = new Dictionary<ProductNiche, float>(),
            nicheRecoveryRate    = new Dictionary<ProductNiche, float>(),
            currentMarketShares  = new Dictionary<ProductNiche, List<MarketShareEntry>>(),
            nicheProjections     = new Dictionary<ProductNiche, float[]>(),
            nichePoolUtilization         = new Dictionary<ProductNiche, float>(),
            nicheViralSpikeDaysRemaining = new Dictionary<ProductNiche, int>(),
            projectionHead       = 0,
            projectionAnchorDay  = 0,
            lastUpdateTick       = 0,
            nicheAttractor       = new Dictionary<ProductNiche, float>(),
            categoryAttractor    = new Dictionary<ProductCategory, float>(),
            categoryDemand       = new Dictionary<ProductCategory, float>(),
            categoryTrends       = new Dictionary<ProductCategory, MarketTrend>(),
            categoryMomentum     = new Dictionary<ProductCategory, float>(),
            categoryRecoveryRate = new Dictionary<ProductCategory, float>(),
            categoryMarketShares = new Dictionary<ProductCategory, List<MarketShareEntry>>(),
            categoryProjections  = new Dictionary<ProductCategory, float[]>(),
            categoryPoolUtilization = new Dictionary<ProductCategory, float>()
        };

        if (nicheConfigs != null)
        {
            for (int i = 0; i < nicheConfigs.Length; i++)
            {
                var cfg = nicheConfigs[i];
                if (cfg == null) continue;

                float attractorSpread = 25f;
                float attractor = cfg.baseDemand + (rng.NextFloat01() * 2f - 1f) * attractorSpread;
                if (attractor < cfg.demandFloor + 5f) attractor = cfg.demandFloor + 5f;
                if (attractor > cfg.demandCeiling - 5f) attractor = cfg.demandCeiling - 5f;
                state.nicheAttractor[cfg.niche] = attractor;

                float demandSpread = 30f + cfg.volatility * 2f;
                float raw = attractor + (rng.NextFloat01() * 2f - 1f) * demandSpread;
                if (raw < cfg.demandFloor) raw = cfg.demandFloor;
                if (raw > cfg.demandCeiling) raw = cfg.demandCeiling;
                float minInitial = cfg.baseDemand * 0.25f;
                if (raw < minInitial) raw = minInitial;
                state.nicheDemand[cfg.niche]       = raw;
                state.nicheTrends[cfg.niche]       = MarketTrend.Stable;
                state.nicheMomentum[cfg.niche]     = (rng.NextFloat01() * 2f - 1f) * cfg.volatility * 0.5f;
                state.nicheRecoveryRate[cfg.niche] = cfg.recoveryRateMin + rng.NextFloat01() * (cfg.recoveryRateMax - cfg.recoveryRateMin);
                state.nichePoolUtilization[cfg.niche]         = 0.02f;
                state.nicheViralSpikeDaysRemaining[cfg.niche] = 0;
            }
        }

        if (templates != null)
        {
            for (int i = 0; i < templates.Length; i++)
            {
                var t = templates[i];
                if (t == null || t.economyConfig == null) continue;
                if (!IsConsumerCategory(t.category)) continue;
                if (t.economyConfig.baseDemand <= 0f) continue;

                var ec = t.economyConfig;
                float attractorSpread = 25f;
                float attractor = ec.baseDemand + (rng.NextFloat01() * 2f - 1f) * attractorSpread;
                if (attractor < ec.demandFloor + 5f) attractor = ec.demandFloor + 5f;
                if (attractor > ec.demandCeiling - 5f) attractor = ec.demandCeiling - 5f;
                state.categoryAttractor[t.category] = attractor;

                float demandSpread = 30f + ec.nicheVolatility * 2f;
                float raw = attractor + (rng.NextFloat01() * 2f - 1f) * demandSpread;
                if (raw < ec.demandFloor) raw = ec.demandFloor;
                if (raw > ec.demandCeiling) raw = ec.demandCeiling;
                float minInitial = ec.baseDemand * 0.25f;
                if (raw < minInitial) raw = minInitial;
                state.categoryDemand[t.category]       = raw;
                state.categoryTrends[t.category]       = MarketTrend.Stable;
                state.categoryMomentum[t.category]     = (rng.NextFloat01() * 2f - 1f) * ec.nicheVolatility * 0.5f;
                state.categoryRecoveryRate[t.category] = ec.recoveryRateMin + rng.NextFloat01() * (ec.recoveryRateMax - ec.recoveryRateMin);
                state.categoryPoolUtilization[t.category] = 0.02f;
            }
        }

        return state;
    }

    public static bool IsConsumerCategory(ProductCategory cat)
    {
        return cat == ProductCategory.DesktopSoftware
            || cat == ProductCategory.WebApplication
            || cat == ProductCategory.SecuritySoftware
            || cat == ProductCategory.CloudInfrastructure
            || cat == ProductCategory.AIProduct
            || cat == ProductCategory.GameConsole
            || cat == ProductCategory.GameEngine
            || cat == ProductCategory.GraphicsEditor
            || cat == ProductCategory.AudioTool
            || cat == ProductCategory.DevFramework;
    }
}
