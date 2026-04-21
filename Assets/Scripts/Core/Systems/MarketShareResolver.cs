// MarketShareResolver Version: Clean v1
using System;
using System.Collections.Generic;

[Serializable]
public struct ShowdownResult
{
    public ProductId WinnerId;
    public ProductId LoserId;
    public ProductNiche Niche;
    public string WinnerName;
    public string LoserName;
    public bool Occurred;
}

public static class MarketShareResolver
{
    private const float SaturationCap = 0.65f;
    private const float AnticipationUserSuppression = 0.7f;
    private const float AnticipationDemandGrowth = 1.1f;
    private const float ShowdownWinnerBoost = 1.3f;
    private const float ShowdownChurnMultiplierValue = 2.0f;
    private const int ShowdownChurnDurationMonths = 3;
    private const int TicksPerMonth = TimeState.TicksPerDay * 30;

    public static List<MarketShareEntry> ResolveNiche(
        ProductNiche niche,
        List<Product> productsInNiche,
        float nicheDemand,
        float saturationCap,
        int nicheMaxUsers,
        float nicheBasePrice,
        float nicheSubscriptionPrice,
        GenerationSystem generationSystem = null,
        PlatformSystem platformSystem = null,
        Dictionary<string, ProductFeatureDefinition> featureLookup = null,
        MarketNicheData nicheData = null,
        float interestRate = 0.15f)
    {
        var results = new List<MarketShareEntry>(productsInNiche.Count);
        if (productsInNiche.Count == 0) return results;

        float[] appeals = new float[productsInNiche.Count];
        float totalAppeal = 0f;

        for (int i = 0; i < productsInNiche.Count; i++)
        {
            var product = productsInNiche[i];
            float trendMult = 1f;
            float appeal = CalculateProductAppeal(product, nicheDemand, trendMult, false, generationSystem, platformSystem, featureLookup, nicheData);
            appeals[i] = appeal;
            totalAppeal += appeal;
        }

        if (totalAppeal <= 0f)
        {
            for (int i = 0; i < productsInNiche.Count; i++)
                totalAppeal += 0.1f;
            totalAppeal = Math.Max(totalAppeal, 1f);
        }

        float cap = saturationCap > 0f ? saturationCap : SaturationCap;

        float[] shares = new float[productsInNiche.Count];
        float redistributable = 0f;

        for (int i = 0; i < productsInNiche.Count; i++)
        {
            float rawShare = totalAppeal > 0f ? appeals[i] / totalAppeal : 0f;
            if (rawShare > cap)
            {
                redistributable += rawShare - cap;
                shares[i] = cap;
            }
            else
            {
                shares[i] = rawShare;
            }
        }

        if (redistributable > 0f)
        {
            float remainingShare = 0f;
            for (int i = 0; i < productsInNiche.Count; i++)
                if (shares[i] < cap) remainingShare += shares[i];

            if (remainingShare > 0f)
            {
                for (int i = 0; i < productsInNiche.Count; i++)
                {
                    if (shares[i] < cap)
                        shares[i] += redistributable * (shares[i] / remainingShare);
                }
            }
        }

        float totalShare = 0f;
        for (int i = 0; i < productsInNiche.Count; i++)
            totalShare += shares[i];
        if (totalShare > 0f && Math.Abs(totalShare - 1f) > 0.001f)
        {
            float scale = 1f / totalShare;
            for (int i = 0; i < productsInNiche.Count; i++)
                shares[i] *= scale;
        }

        if (productsInNiche.Count == 1)
        {
            float maxSoloShare = 0.75f;
            if (shares[0] > maxSoloShare)
                shares[0] = maxSoloShare;
        }

        for (int i = 0; i < productsInNiche.Count; i++)
        {
            var product = productsInNiche[i];
            float sharePercent = shares[i];

            float penetration = CalculatePenetrationRate(product);

            float demandScale = nicheDemand / 100f;
            float effectiveInterest = interestRate > 0f ? interestRate : 0.15f;
            long nicheCurrentUsers = (long)(nicheMaxUsers * effectiveInterest * demandScale);
            int activeUsers = (int)(nicheCurrentUsers * sharePercent * penetration);

            int monthlyRevenue = 0;
            if (product.IsSubscriptionBased) {
                float subPrice = product.PriceOverride > 0f ? product.PriceOverride : nicheSubscriptionPrice;
                if (subPrice > 0f) {
                    float conversionRate = subPrice > 20f ? 0.06f : subPrice > 12f ? 0.12f : 0.25f;
                    monthlyRevenue = (int)(activeUsers * conversionRate * subPrice);
                }
            } else {
                float unitPrice = product.PriceOverride > 0f ? product.PriceOverride : nicheBasePrice;
                int userDelta = activeUsers - product.ActiveUserCount;
                int newBuyersMonthly = userDelta > 0 ? userDelta : 0;
                int monthlyFloor = (int)(activeUsers * 0.02f);
                int totalMonthlyBuyers = newBuyersMonthly > monthlyFloor ? newBuyersMonthly : monthlyFloor;
                monthlyRevenue = (int)(totalMonthlyBuyers * unitPrice);
            }

            results.Add(new MarketShareEntry
            {
                ProductId = product.Id,
                OwnerId = product.IsCompetitorProduct ? (CompetitorId?)product.OwnerCompanyId.ToCompetitorId() : null,
                Appeal = appeals[i],
                MarketSharePercent = sharePercent,
                PenetrationRate = penetration,
                ActiveUsers = activeUsers,
                MonthlyRevenue = monthlyRevenue,
                GlobalUserSharePercent = 0f
            });
        }

        return results;
    }

    public static float CalculateProductAppeal(
        Product product,
        float nicheDemand,
        float trendMultiplier,
        bool hasUpcomingRelease,
        GenerationSystem generationSystem = null,
        PlatformSystem platformSystem = null,
        Dictionary<string, ProductFeatureDefinition> featureLookup = null,
        MarketNicheData nicheData = null)
    {
        // Use per-feature weighted quality if features are populated
        float quality = ComputeFeatureWeightedQuality(product, generationSystem, featureLookup, nicheData);

        // Tool bonus: own tool or licensed tool
        float toolBonus = 0f;
        if (product.RequiredToolIds != null && product.RequiredToolIds.Length > 0)
        {
            toolBonus = product.IsCompetitorProduct ? 0.05f : 0.05f;
        }

        // Platform addressable market factor
        float platformMarketFactor = 1f;
        if (platformSystem != null && product.TargetPlatformIds != null && product.TargetPlatformIds.Length > 0)
        {
            float addressable = platformSystem.GetAddressableMarket(product.Niche, product.TargetPlatformIds);
            platformMarketFactor = 0.5f + addressable * 0.5f;
        }

        float featureRelevance = product.FeatureRelevanceAtShip > 0f ? product.FeatureRelevanceAtShip : 0.5f;
        float popReach = product.PopularityScore / 200f;
        float hypeReach = product.HypeScore / 400f;
        float marketingReach = 1f + popReach + hypeReach;
        float ageMonths = product.TicksSinceShip / (float)TicksPerMonth;
        float halfLifeMonths = GetNicheHalfLife(product.Niche);
        float ageDecay = 1f + ageMonths / halfLifeMonths;
        float trendMult = trendMultiplier > 0f ? trendMultiplier : 1f;
        float upcomingPenalty = hasUpcomingRelease ? 0.7f : 1.0f;
        float fanBonus = product.FanAppealBonus > 0f ? product.FanAppealBonus : 1f;

        float reviewMult = product.PublicReceptionScore > 0f
            ? product.PublicReceptionScore / 100f
            : quality / 100f;
        float reviewCurve = reviewMult < 0.5f
            ? reviewMult * 0.5f
            : reviewMult * reviewMult;

        float obsolescenceMod = 1f;
        if (generationSystem != null) {
            int currentGen = generationSystem.GetCurrentGeneration();
            int effectiveProductGen = (product.Stance == GenerationStance.CrossGen && product.SecondaryGeneration.HasValue)
                ? Math.Max(product.ArchitectureGeneration, product.SecondaryGeneration.Value)
                : product.ArchitectureGeneration;
            int genGap = currentGen - effectiveProductGen;
            if (genGap > 0)
                obsolescenceMod = Math.Max(0.30f, 1f - genGap * 0.15f);
        }

        float appeal = reviewCurve * (1f + toolBonus) * featureRelevance * trendMult * marketingReach
                     * (1f / ageDecay) * upcomingPenalty * fanBonus * platformMarketFactor * obsolescenceMod;
        if (product.ShowdownChurnMultiplier > 1f)
            appeal /= product.ShowdownChurnMultiplier;

        return Math.Max(0.01f, appeal);
    }

    private static float ComputeFeatureWeightedQuality(
        Product product,
        GenerationSystem generationSystem,
        Dictionary<string, ProductFeatureDefinition> featureLookup,
        MarketNicheData nicheData)
    {
        if (product.Features == null || product.Features.Length == 0)
            return product.OverallQuality;

        float weightedSum = 0f;
        float totalWeight = 0f;

        for (int i = 0; i < product.Features.Length; i++)
        {
            var fs = product.Features[i];
            if (fs == null) continue;

            ProductFeatureDefinition featureDef = null;
            if (featureLookup != null)
                featureLookup.TryGetValue(fs.FeatureId, out featureDef);

            float nicheWeight = 1f;
            if (featureDef != null && nicheData != null)
                nicheWeight = nicheData.GetAffinityForCategory(featureDef.featureCategory);

            float paradigmAffinity = 1f;
            if (featureDef != null && generationSystem != null)
                paradigmAffinity = generationSystem.GetFeatureAffinity(
                    featureDef,
                    product.ArchitectureGeneration,
                    product.Stance,
                    product.SecondaryGeneration);

            float effectiveQuality = fs.EffectiveQuality * paradigmAffinity;
            weightedSum += effectiveQuality * nicheWeight;
            totalWeight += nicheWeight;
        }

        return totalWeight > 0f ? weightedSum / totalWeight : product.OverallQuality;
    }

    public static float CalculatePenetrationRate(Product product)
    {
        float qualityFactor = product.OverallQuality / 100f;
        float reviewFactor = product.PublicReceptionScore > 0f
            ? product.PublicReceptionScore / 100f
            : qualityFactor;
        float popularityFactor = product.PopularityScore / 100f;
        float freshness = 1f / (1f + product.TicksSinceShip / (float)(TicksPerMonth * 24));

        float penetration = qualityFactor * 0.20f
                          + reviewFactor * 0.35f
                          + popularityFactor * 0.15f
                          + freshness * 0.30f;

        return Clamp(penetration, 0.01f, 0.50f);
    }

    public static void ApplyAnticipationEffect(
        ProductNiche niche,
        List<Product> existingProducts,
        List<Product> upcomingProducts)
    {
        if (upcomingProducts.Count == 0) return;

        for (int i = 0; i < existingProducts.Count; i++)
        {
            var p = existingProducts[i];
            p.PopularityScore *= AnticipationUserSuppression;
        }
    }

    public static ShowdownResult DetectAndResolveShowdown(
        ProductNiche niche,
        List<Product> newReleasesThisMonth,
        int currentTick)
    {
        if (newReleasesThisMonth.Count < 2)
            return default;

        Product winner = null;
        Product loser = null;
        float bestAppeal = float.MinValue;
        float worstAppeal = float.MaxValue;

        for (int i = 0; i < newReleasesThisMonth.Count; i++)
        {
            var product = newReleasesThisMonth[i];
            float appeal = CalculateProductAppeal(product, 50f, 1f, false);
            if (appeal > bestAppeal) { bestAppeal = appeal; winner = product; }
            if (appeal < worstAppeal) { worstAppeal = appeal; loser = product; }
        }
        if (winner == null || loser == null || winner.Id == loser.Id)
            return default;

        winner.PopularityScore = Math.Min(100f, winner.PopularityScore * ShowdownWinnerBoost);
        loser.ShowdownChurnMultiplier = ShowdownChurnMultiplierValue;
        loser.ShowdownChurnExpiryTick = currentTick + ShowdownChurnDurationMonths * TicksPerMonth;

        return new ShowdownResult
        {
            WinnerId = winner.Id,
            LoserId = loser.Id,
            Niche = niche,
            WinnerName = winner.ProductName,
            LoserName = loser.ProductName,
            Occurred = true
        };
    }

    private static float GetNicheHalfLife(ProductNiche niche)
    {
        switch (niche)
        {
            case ProductNiche.DesktopOS:
            case ProductNiche.MobileOS:
            case ProductNiche.ServerOS:
                return 72f;
            case ProductNiche.CRM:
            case ProductNiche.Analytics:
            case ProductNiche.Communication:
            case ProductNiche.AppProductivity:
                return 48f;
            case ProductNiche.AppUtility:
            case ProductNiche.AppSocial:
                return 36f;
            default:
                return 24f;
        }
    }

    private static float Clamp(float value, float min, float max)
    {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }
}
