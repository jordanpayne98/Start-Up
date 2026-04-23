using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;

public class ProductIdConverter : TypeConverter {
    public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) {
        return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
    }

    public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) {
        if (value is string s && int.TryParse(s, out int id))
            return new ProductId(id);
        return base.ConvertFrom(context, culture, value);
    }

    public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType) {
        if (destinationType == typeof(string) && value is ProductId pid)
            return pid.Value.ToString();
        return base.ConvertTo(context, culture, value, destinationType);
    }
}

[TypeConverter(typeof(ProductIdConverter))]
[Serializable]
public struct ProductId
{
    public int Value;

    public ProductId(int value)
    {
        Value = value;
    }

    public override bool Equals(object obj)
    {
        if (obj is ProductId other)
            return Value == other.Value;
        return false;
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public static bool operator ==(ProductId a, ProductId b)
    {
        return a.Value == b.Value;
    }

    public static bool operator !=(ProductId a, ProductId b)
    {
        return a.Value != b.Value;
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}

[Serializable]
public class ProductPhaseRuntime
{
    public ProductPhaseType phaseType;
    public ProductTeamRole primaryRole;
    public ProductPhaseType[] prerequisites;
    public float totalWorkRequired;
    public float workCompleted;
    public float phaseQuality;
    public float qualitySoftCap;
    public float minSkillThreshold;
    public float targetSkillThreshold;
    public float excellenceSkillThreshold;
    public int iterationCount;
    public bool isComplete;
    public bool isIterating;
    public float bonusWorkTarget;
    public float bonusWorkCompleted;
    public float bugAccumulation;
    public bool isUnlocked;

    public float ProgressPercent => totalWorkRequired > 0f ? workCompleted / totalWorkRequired : 0f;
}

[Serializable]
public class ProductUpdateRuntime
{
    public bool isUpdating;
    public ProductUpdateType updateType;
    public float updateWorkRequired;
    public float updateWorkCompleted;
    public string[] targetFeatureIds;
}

[Serializable]
public class Product
{
    public ProductId Id;
    public string TemplateId;
    public string ProductName;
    public ProductCategory Category;
    public ProductNiche Niche;
    public string[] SelectedFeatureIds;
    public ProductPhaseRuntime[] Phases;
    public bool IsInDevelopment;
    public bool IsShipped;
    public bool IsOnMarket;
    public bool IsMaintained;
    public float OverallQuality;
    public int UpfrontCostPaid;
    public int TotalDevelopmentTicks;
    public Dictionary<ProductTeamRole, TeamId> TeamAssignments;
    public int ActiveUserCount;
    public float PopularityScore;
    public int MonthlyRevenue;
    public int LaunchRevenue;
    public int UpdateCount;
    public float BugsRemaining;
    public ProductLifecycleStage LifecycleStage;
    public int TicksSinceShip;
    public int ShipTick;
    public int LastStageChangeTick;
    public int TicksSinceLastUpdate;
    public ProductUpdateRuntime CurrentUpdate;
    public bool IsOnSale;
    public int SaleTicksRemaining;
    public int TicksSinceLastSale;
    public int TotalSalesTriggered;

    // --- Revenue Model V2 ---
    public int TotalUnitsSold;            // lifetime units sold (one-time purchase)
    public int TotalSubscribers;          // current subscriber count (subscription)
    public int PeakMonthlySales;          // highest monthly sales achieved
    public bool IsBreakout;               // whether this product hit breakout status
    public float BreakoutMultiplier;      // the multiplier applied if breakout
    public int BreakoutMonthsRemaining;   // deprecated: kept for save compat, use BreakoutDaysRemaining
    public int BreakoutDaysRemaining;     // days of boosted performance left (replaces BreakoutMonthsRemaining)
    public long TotalLifetimeRevenue;     // all revenue ever earned
    public long AccumulatedSalaryCost;    // total team salary costs during development
    public float TailDecayFactor;         // current tail decay position (starts at 1.0, decays)
    public int PreviousActiveUsers;
    public int PreviousMonthlyRevenue;
    public int ProjectedActiveUsers;
    public int ProjectedMonthlyRevenue;

    // --- Daily Revenue Tracking ---
    public int DailyRevenue;              // latest single-day revenue for display
    public int AccumulatedMonthlyRevenue; // running total for current month
    public float DailyRevenueRemainder;   // float remainder to prevent small products from earning $0/day
    public int PreviousDailyActiveUsers;  // previous day's user count
    public int PreviousMonthActiveUsers;  // snapshot at month boundary for trend comparison

    // --- Monthly Snapshot (written once per month at month boundary) ---
    public int SnapshotMonthlySales;     // one-time: units sold this month; subscription: active subscribers at month end
    public int SnapshotMonthlyUsers;     // ActiveUserCount at month end
    public long SnapshotMonthlyRevenue;  // finalized monthly revenue
    public string SnapshotMonthlyTrend;  // "Growth", "Decline", "Stable", "New", or "--"
    public int PreviousMonthUnitsSold;   // TotalUnitsSold at previous month boundary (for delta calc)
    public bool HasCompletedFirstMonth;  // false until first OnMonthChanged fires; controls "New"/"--" display
    public int WorldStartSunsetGraceUntilTick; // tick after which normal sunset evaluation applies; 0 = no grace

    public long TotalProductionCost => UpfrontCostPaid + AccumulatedSalaryCost;

    // --- Budget Mechanics ---
    public long MaintenanceBudgetMonthly;  // monthly allocation from company cash for QA/maintenance
    public float MaintenanceQuality;       // 0-100, derived from QA team effectiveness + coverage
    public long MarketingBudgetMonthly;    // monthly allocation from company cash for marketing

    // --- Wizard V2 fields ---
    public bool IsSubscriptionBased;       // player-chosen pricing model (overrides template default)
    public float PriceOverride;            // player-chosen price (one-time or monthly)

    // --- Layered Architecture fields ---
    public ProductId[] TargetPlatformIds;      // platform products this targets
    public ProductId[] RequiredToolIds;         // tool products used (e.g., game engine, graphics editor)
    public ProductFeatureState[] Features;      // per-feature quality/debt
    public int ArchitectureGeneration;          // generation stamp (immutable after creation)
    public GenerationStance Stance;             // Standard or CrossGen
    public int? SecondaryGeneration;            // if CrossGen, the next gen being straddled
    public ProductId? PredecessorProductId;     // predecessor in lineage chain
    public LineageId Lineage;                   // lineage chain identifier
    public bool IsLegacy;                       // true when total debt exceeds threshold
    public int ProductVersion;                  // increments with each shipped update/version

    // --- Hardware Configuration (Game Console only) ---
    public bool HasHardwareConfig;                   // true for console products with hardware selected
    public HardwareConfiguration HardwareConfig;     // struct; valid only when HasHardwareConfig == true
    public int ManufactureCostPerUnit;               // derived from HardwareConfig at creation, 0 for non-console
    public long TotalManufacturingCost;              // accumulated manufacturing cost over lifetime
    public long TotalHardwareRevenue;                // gross revenue (units * price) before manufacturing cost
    public long TotalPlatformLicensingRevenue;       // revenue from games sold on this console platform

    // --- Tool Distribution (Layer 1 products only) ---
    public ToolDistributionModel DistributionModel;  // Proprietary by default
    public float PlayerLicensingRate;                // 0.05-0.30, Licensed only
    public int ActiveLicenseeCount;                  // how many products currently use this tool
    public long TotalLicensingRevenue;               // cumulative royalty revenue

    // --- Tool Subscription B2C (Licensed tool products only) ---
    public int ActiveSubscriberCount;               // current monthly subscribers from market resolution
    public float MonthlySubscriptionPrice;          // player-set price per month ($5-$100)
    public long TotalSubscriptionRevenue;           // cumulative subscription revenue

    // --- Late Pivot fields ---
    public int PivotsUsed;                // how many pivots have been used
    public int MaxPivots;                 // max pivots allowed (default 1)
    public List<string> DroppedFeatureIds; // feature IDs dropped mid-development

    // --- XP Tracking ---
    public int CreationTick;              // sim tick when the product was created (for duration-based XP)

    // --- Market Relevance (set at ship time) ---
    public float FeatureRelevanceAtShip;  // computed at ship time, used in monthly revenue
    public float PublicReceptionScore;    // 0-100, generated at launch, visible to player
    public ProductReviewResult ReviewResult;  // full multi-outlet review data (set at launch)

    // --- Sequel Tracking ---
    public ProductId? SequelOfId;              // the original product this is a sequel to (null if original)
    public int SequelNumber;                   // 0 = original, 1 = first sequel, 2 = second, etc.
    public List<ProductId> SequelIds;          // IDs of sequels made from this product

    // --- Marketing / Hype ---
    public float HypeScore;
    public bool IsMarketingActive;
    public int MarketingStartedTick;
    public int TotalMarketingSpend;
    public int PaidBoostCount;
    public int LastPaidBoostTick;
    public float HypeAtShip;
    public float PeakHype;
    public int PeakHypeTick;
    public bool IsRunningAds;
    public int AdTicksRemaining;
    public int LastAdTick;
    public int TotalAdSpend;
    public float UpdateHype;
    public bool HasAnnouncedUpdate;
    public int UpdateAnnounceTick;
    public int LastHypeEventTick;

    // --- Competitor Ownership ---
    public CompanyId OwnerCompanyId;
    public bool IsCompetitorProduct => !OwnerCompanyId.IsPlayer;
    public bool OutcomeRecorded;
    public int TargetReleaseTick;

    // --- Release Date System ---
    public int OriginalReleaseTick;
    public int DateShiftCount;
    public bool HasAnnouncedReleaseDate;

    // --- Showdown ---
    public float ShowdownChurnMultiplier;
    public int ShowdownChurnExpiryTick;

    public float FanAppealBonus;  // set at launch, used by MarketShareResolver for appeal calc

    // --- Crisis ---
    public int CrisisLevel;
    public int LastCrisisTick;
    public int UnmaintainedMonths;

    // --- Valuation ---
    public long SaleValue;

    // --- Successor Migration ---
    public int SuccessorMigrationTicksTotal;      // total ticks over which migration happens
    public int SuccessorMigrationTicksElapsed;    // ticks elapsed since migration started
    public int SuccessorMigrationUsersPerTick;    // users to transfer per tick (spread over duration)
}

[Serializable]
public class ProductState
{
    public Dictionary<ProductId, Product> developmentProducts;
    public Dictionary<ProductId, Product> shippedProducts;
    public Dictionary<ProductId, Product> archivedProducts;
    public Dictionary<TeamId, ProductId> teamToProduct;
    public int nextProductId;
    public int nextLineageId;
    public Dictionary<ProductId, LicenseAgreement> activeLicenses;

    public static ProductState CreateNew()
    {
        return new ProductState
        {
            developmentProducts = new Dictionary<ProductId, Product>(),
            shippedProducts = new Dictionary<ProductId, Product>(),
            archivedProducts = new Dictionary<ProductId, Product>(),
            teamToProduct = new Dictionary<TeamId, ProductId>(),
            nextProductId = 1,
            nextLineageId = 1,
            activeLicenses = new Dictionary<ProductId, LicenseAgreement>()
        };
    }
}
