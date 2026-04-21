using System;
using UnityEngine;

[Serializable]
public struct ProductPhaseWorkEntry
{
    public ProductPhaseType phaseType;
    public float workUnits;
}

[Serializable]
public class ProductPhaseDefinition
{
    public ProductPhaseType phaseType;
    public ProductTeamRole primaryRole;
    public float baseWorkUnits;
    public ProductPhaseType[] prerequisites;
    public float qualitySoftCapBase;
}

[Serializable]
public class ProductEconomyConfig
{
    public bool isSubscriptionBased;

    // --- One-Time Purchase Path ---
    public int launchSalesBase;             // base units sold at launch
    public float pricePerUnit;              // price per copy/license sold
    public float tailDecayRate;             // how fast monthly sales decay after launch (lower = longer tail)
    public float minTailFactor;             // floor for tail decay (sales never drop below this % of peak)

    // --- Subscription Path ---
    public float monthlySubscriptionPrice;  // monthly price per subscriber

    // --- Shared ---
    public float baseChurnRate;             // monthly % of users lost (e.g. 0.02 = 2% one-time, 0.03 = 3% subscription)
    public float maintenanceChurnReduction; // churn reduction when maintained (e.g. 0.01)
    public float organicGrowthRate;         // monthly new user/buyer rate as fraction of current base
    public float updateSalesSpikeMult;      // sales multiplier when a major update ships
    public float updatePopularityBoost;     // popularity boost from shipping an update
    public float maintenancePopDecayReduction; // popularity decay reduction when maintained

    // --- Breakout ---
    public float breakoutBaseChance;        // base % chance of breakout (e.g. 0.02 = 2%)
    public float breakoutMinMultiplier;     // minimum revenue multiplier if breakout (e.g. 3.0)
    public float breakoutMaxMultiplier;     // maximum revenue multiplier if breakout (e.g. 10.0)

    // --- Lifecycle ---
    public float plateauDecayRatePerMonth;
    public float declineDecayRatePerMonth;
    public int ticksToGrowthStage;
    public float bugChurnMultiplier;

    // --- Bugs ---
    public float shipBugPopularityPenalty;
    public float maxMonthlyBugPopularityPenalty;
    public float unmaintainedBugGrowthBase;

    // --- Sequels ---
    public float sequelPopularityBoostMax;       // max popularity bonus from popular original (e.g. 25)

    // --- Tier 2 Market Fields (used when product.Niche == ProductNiche.None) ---
    public int maxUserPool;                      // total addressable market (Tier 2 only)
    public float basePricePerUnit;               // market's expected base price (Tier 2 only)
    public float baseSubscriptionPrice;          // market's expected subscription price (Tier 2 only)
    public float priceElasticityExponent;        // how sensitive demand is to price deviation (Tier 2 only)
    public float nicheVolatility;                // demand volatility for Tier 2 competition pool
    public int retentionMonths;                  // expected product lifespan in months (Tier 2 only)
    [Range(0.01f, 0.40f)] public float interestRate;  // fraction of maxUserPool actually in-market (Tier 2 only)

    // --- Category-Level Demand Fields ---
    [Range(0f, 100f)] public float baseDemand;
    public float demandFloor;
    public float demandCeiling;
    [Range(0.005f, 0.2f)] public float recoveryRateMin;
    [Range(0.005f, 0.2f)] public float recoveryRateMax;
    public int saturationThreshold;
    [Range(0.1f, 2f)] public float saturationPenaltyPerProduct;
}

// FUTURE: Marketing team auto-triggers sales when:
// 1. Marketing team assigned to product via ProductTeamRole.Marketing
// 2. Product popularity drops below autoSaleThreshold
// 3. Cooldown has elapsed (shorter than player cooldown)
// 4. Marketing team also boosts WoM rate by marketingEfficiencyMultiplier
[Serializable]
public class MarketingTeamConfig
{
    public int autoSaleCooldownTicks;        // marketing team triggers sales more frequently
    public float autoSaleThreshold;          // popularity below this triggers auto-sale
    public float marketingEfficiencyMultiplier; // scales WoM rate
}
