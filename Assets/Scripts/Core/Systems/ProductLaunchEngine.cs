// ProductLaunchEngine Version: Clean v1
using System;

public static class ProductLaunchEngine
{
    public static int ComputeLaunchSales(
        float effectiveQuality,
        int launchSalesBase,
        float reputationLaunchMult,
        float genreMult,
        float marketDemandMult,
        float priceElasticityMult,
        float fanLaunchMult,
        float hypeLaunchMult)
    {
        float qualityMult = effectiveQuality / 100f * 1.5f;
        int baseSales = (int)(launchSalesBase * qualityMult * reputationLaunchMult * genreMult * marketDemandMult * priceElasticityMult * fanLaunchMult * hypeLaunchMult);
        if (baseSales < 1) baseSales = 1;
        return baseSales;
    }

    public static void RollBreakout(
        float quality,
        float popularity,
        ReputationTier ownerTier,
        float breakoutBaseChance,
        float breakoutMinMult,
        float breakoutMaxMult,
        int baseSales,
        IRng rng,
        out int finalSales,
        out bool isBreakout,
        out float breakoutMultiplier,
        out int breakoutDaysRemaining)
    {
        float breakoutChance = breakoutBaseChance;

        if (quality > 95f) breakoutChance += 0.05f;
        else if (quality > 90f) breakoutChance += 0.03f;

        switch (ownerTier)
        {
            case ReputationTier.IndustryLeader: breakoutChance += 0.05f; break;
            case ReputationTier.Respected:      breakoutChance += 0.03f; break;
        }

        float roll = rng.NextFloat01();
        if (roll < breakoutChance && breakoutMinMult > 0f && breakoutMaxMult > 0f)
        {
            float range = breakoutMaxMult - breakoutMinMult;
            breakoutMultiplier = breakoutMinMult + rng.NextFloat01() * range;
            isBreakout = true;
            int breakoutMonths = 6 + (int)(rng.NextFloat01() * 12f);
            breakoutDaysRemaining = breakoutMonths * 30;
            finalSales = (int)(baseSales * breakoutMultiplier);
        }
        else
        {
            isBreakout = false;
            breakoutMultiplier = 1f;
            breakoutDaysRemaining = 0;
            finalSales = baseSales;
        }
    }

    public static float ComputeFanAppealBonus(
        int companyFans,
        float fanSentiment,
        float fanLaunchBonusDivisor)
    {
        float effectiveFans = companyFans * (fanSentiment / 100f);
        return 1.0f + effectiveFans / fanLaunchBonusDivisor;
    }

    public static int ComputeLaunchReputation(
        float effectiveQuality,
        int launchRevenue,
        float launchReputationBase)
    {
        float qualityFactor = effectiveQuality / 100f;
        float revenueFactor = Math.Min(2f, launchRevenue / 50000f);
        return (int)(launchReputationBase * qualityFactor * (1f + revenueFactor));
    }

    public static float ComputePopularityScore(float quality)
    {
        return Math.Min(100f, Math.Max(0f, quality * 0.9f));
    }
}
