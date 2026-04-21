// MarketingEventProcessor Version: Clean v1
using System;

public struct HypeEventResult
{
    public bool EventOccurred;
    public HypeEventType EventType;
    public string Headline;
    public string Body;
    public float HypeChange;
    public float PopularityChange;
    public float SentimentChange;
    public float ReputationChange;
    public int UserChange;
    public bool WasMitigated;
}

public static class MarketingEventProcessor
{
    // Pre-launch weights sum: DevelopmentLeak=30, InsiderPreview=20, CompetitorAnnouncement=25, CommunityBuzz=25
    // Post-launch weights sum: ViralMoment=15, BadReview=30, IndustryAward=10, SecurityBreach=15, InfluencerCoverage=30

    private static readonly int[] _preLaunchWeights  = new[] { 30, 20, 25, 25 };
    private static readonly HypeEventType[] _preLaunchTypes = new[] {
        HypeEventType.DevelopmentLeak,
        HypeEventType.InsiderPreview,
        HypeEventType.CompetitorAnnouncement,
        HypeEventType.CommunityBuzz
    };

    private static readonly int[] _postLaunchWeights = new[] { 15, 30, 10, 15, 30 };
    private static readonly HypeEventType[] _postLaunchTypes = new[] {
        HypeEventType.ViralMoment,
        HypeEventType.BadReview,
        HypeEventType.IndustryAward,
        HypeEventType.SecurityBreach,
        HypeEventType.InfluencerCoverage
    };

    public static HypeEventResult TryRollEvent(
        Product product,
        ProductCategory category,
        bool hasMarketingTeam,
        float marketingSkillMult,
        int currentTick,
        IRng rng,
        TuningConfig tuning)
    {
        float baseChance = tuning?.HypeEventBaseChance ?? 0.02f;
        int cooldownTicks = tuning?.HypeEventCooldownTicks ?? 144000;

        if ((currentTick - product.LastHypeEventTick) < cooldownTicks)
            return default;

        if (rng.NextFloat01() >= baseChance)
            return default;

        bool isPreLaunch = product.IsInDevelopment;
        HypeEventType eventType = isPreLaunch
            ? PickWeighted(_preLaunchTypes, _preLaunchWeights, rng)
            : PickWeightedPostLaunch(_postLaunchTypes, _postLaunchWeights, product, rng);

        var result = new HypeEventResult { EventOccurred = true, EventType = eventType };

        CalculateEffects(ref result, product, category, eventType, hasMarketingTeam, rng);

        string categoryName = category.ToString();
        float primaryChange = isPreLaunch ? result.HypeChange : result.PopularityChange;
        (result.Headline, result.Body) = MarketingEventTemplates.GetTemplate(
            eventType, product.ProductName, categoryName,
            primaryChange, result.UserChange, result.WasMitigated, rng);

        return result;
    }

    private static HypeEventType PickWeighted(HypeEventType[] types, int[] weights, IRng rng)
    {
        int total = 0;
        for (int i = 0; i < weights.Length; i++) total += weights[i];
        int roll = (int)(rng.NextFloat01() * total);
        int cumulative = 0;
        for (int i = 0; i < types.Length; i++)
        {
            cumulative += weights[i];
            if (roll < cumulative) return types[i];
        }
        return types[types.Length - 1];
    }

    private static HypeEventType PickWeightedPostLaunch(HypeEventType[] types, int[] weights, Product product, IRng rng)
    {
        int total = 0;
        for (int i = 0; i < weights.Length; i++) total += weights[i];

        for (int attempt = 0; attempt < 3; attempt++)
        {
            int roll = (int)(rng.NextFloat01() * total);
            int cumulative = 0;
            for (int i = 0; i < types.Length; i++)
            {
                cumulative += weights[i];
                if (roll < cumulative)
                {
                    if (types[i] == HypeEventType.IndustryAward && product.OverallQuality <= 80f)
                        break;
                    return types[i];
                }
            }
        }

        // Fallback — skip IndustryAward entirely
        return HypeEventType.InfluencerCoverage;
    }

    private static void CalculateEffects(
        ref HypeEventResult result,
        Product product,
        ProductCategory category,
        HypeEventType eventType,
        bool hasTeam,
        IRng rng)
    {
        switch (eventType)
        {
            case HypeEventType.DevelopmentLeak:
            {
                float raw = 5f + rng.NextFloat01() * 10f;
                if (hasTeam) raw += 5f;
                result.HypeChange = raw;
                result.WasMitigated = hasTeam;
                break;
            }
            case HypeEventType.InsiderPreview:
            {
                float raw = 10f + rng.NextFloat01() * 10f;
                result.HypeChange = raw;
                result.WasMitigated = false;
                break;
            }
            case HypeEventType.CompetitorAnnouncement:
            {
                float raw = -(5f + rng.NextFloat01() * 5f);
                if (hasTeam) { raw *= 0.5f; result.WasMitigated = true; }
                result.HypeChange = raw;
                break;
            }
            case HypeEventType.CommunityBuzz:
            {
                float raw = 8f + rng.NextFloat01() * 4f;
                if (hasTeam) raw += 5f;
                result.HypeChange = raw;
                result.WasMitigated = hasTeam;
                break;
            }
            case HypeEventType.ViralMoment:
            {
                float raw = 15f;
                if (hasTeam) { raw += 10f; result.WasMitigated = true; }
                result.PopularityChange = raw;
                result.UserChange = (int)(raw * 500f);
                break;
            }
            case HypeEventType.BadReview:
            {
                float raw = -10f;
                if (hasTeam)
                {
                    result.PopularityChange = raw * 0.5f;
                    result.SentimentChange = 0f;
                    result.WasMitigated = true;
                }
                else
                {
                    result.PopularityChange = raw;
                    result.SentimentChange = -3f;
                }
                break;
            }
            case HypeEventType.IndustryAward:
            {
                float raw = 20f;
                float rep = 5f;
                if (hasTeam) { rep += 5f; result.WasMitigated = true; }
                result.PopularityChange = raw;
                result.ReputationChange = rep;
                result.UserChange = (int)(raw * 300f);
                break;
            }
            case HypeEventType.SecurityBreach:
            {
                float rawPop = -15f;
                int rawUsers = -(int)(Math.Max(1, product.ActiveUserCount) * 0.05f);
                if (hasTeam)
                {
                    result.PopularityChange = rawPop;
                    result.UserChange = rawUsers / 2;
                    result.WasMitigated = true;
                }
                else
                {
                    result.PopularityChange = rawPop;
                    result.UserChange = rawUsers;
                }
                break;
            }
            case HypeEventType.InfluencerCoverage:
            {
                float raw = 8f;
                int users = (int)(raw * 400f);
                if (hasTeam) { users *= 2; result.WasMitigated = true; }
                result.PopularityChange = raw;
                result.UserChange = users;
                break;
            }
        }
    }
}
