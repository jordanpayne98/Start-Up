using System.Collections.Generic;

public class ProductDetailViewModel : IViewModel
{
    public string ProductName { get; private set; }
    public string CompanyName { get; private set; }
    public CompetitorId? OwnerId { get; private set; }
    public string Niche { get; private set; }
    public string Quality { get; private set; }
    public string FeatureList { get; private set; }
    public string ActiveUsers { get; private set; }
    public string MarketSharePercent { get; private set; }
    public string LifetimeRevenue { get; private set; }
    public string MonthlyRevenue { get; private set; }
    public string LaunchDate { get; private set; }
    public string LifecycleStage { get; private set; }
    public string UserTrend { get; private set; }
    public string MaintenanceStatus { get; private set; }
    public bool IsPlayerOwned { get; private set; }
    public bool HasCrisis { get; private set; }
    public string CrisisDescription { get; private set; }
    public List<string> TeamAssignments { get; private set; }
    public ProductReviewViewModel ReviewVM { get; private set; }

    // Budget display properties
    public string MaintenanceBudgetMonthly { get; private set; }
    public string MaintenanceDrainRate { get; private set; }
    public string MaintenanceMonthsCoverage { get; private set; }
    public string MaintenanceQualityDisplay { get; private set; }
    public bool HasQATeamAssigned { get; private set; }
    public bool IsMaintenanceUnderfunded { get; private set; }

    public string MarketingBudgetMonthly { get; private set; }
    public string MarketingDrainRate { get; private set; }
    public string MarketingMonthsCoverage { get; private set; }
    public bool HasMarketingTeamAssigned { get; private set; }
    public bool IsMarketingUnderfunded { get; private set; }

    private readonly List<string> _teamAssignments = new List<string>();
    private ProductTemplateDefinition[] _templates;
    private long _estimatedMaintDrain;
    private long _estimatedMktDrain;

    public void SetTemplates(ProductTemplateDefinition[] templates) {
        _templates = templates;
    }

    public void SetEstimatedDrains(long maintDrain, long mktDrain) {
        _estimatedMaintDrain = maintDrain;
        _estimatedMktDrain = mktDrain;
    }

    public ProductDetailViewModel() {
        TeamAssignments = _teamAssignments;
        ReviewVM = new ProductReviewViewModel();
    }

    public void Refresh(IReadOnlyGameState state) { }

    public void Refresh(ProductId id, ProductState productState, CompetitorState compState, MarketState marketState) {
        _teamAssignments.Clear();

        Product product = null;
        if (productState.shippedProducts != null && productState.shippedProducts.TryGetValue(id, out var shipped))
            product = shipped;
        else if (productState.developmentProducts != null && productState.developmentProducts.TryGetValue(id, out var dev))
            product = dev;

        if (product == null) {
            ProductName = "Unknown";
            CompanyName = "--";
            OwnerId = null;
            Niche = "--";
            Quality = "--";
            FeatureList = "--";
            ActiveUsers = "--";
            MarketSharePercent = "--";
            LifetimeRevenue = "--";
            MonthlyRevenue = "--";
            LaunchDate = "--";
            LifecycleStage = "--";
            UserTrend = "--";
            MaintenanceStatus = "--";
            IsPlayerOwned = false;
            HasCrisis = false;
            CrisisDescription = "";
            MaintenanceBudgetMonthly = "--";
            MaintenanceDrainRate = "--";
            MaintenanceMonthsCoverage = "--";
            MaintenanceQualityDisplay = "--";
            HasQATeamAssigned = false;
            IsMaintenanceUnderfunded = false;
            MarketingBudgetMonthly = "--";
            MarketingDrainRate = "--";
            MarketingMonthsCoverage = "--";
            HasMarketingTeamAssigned = false;
            IsMarketingUnderfunded = false;
            return;
        }

        ProductName = product.ProductName;
        OwnerId = product.IsCompetitorProduct ? (CompetitorId?)product.OwnerCompanyId.ToCompetitorId() : null;
        IsPlayerOwned = !product.IsCompetitorProduct;

        if (product.IsCompetitorProduct
            && compState?.competitors != null
            && compState.competitors.TryGetValue(product.OwnerCompanyId.ToCompetitorId(), out var comp)) {
            CompanyName = comp.CompanyName;
        } else if (!product.IsCompetitorProduct) {
            CompanyName = "Player";
        } else {
            CompanyName = "--";
        }

        Niche = UIFormatting.FormatNicheOrCategory(product);
        if (product.ReviewResult != null) {
            int score = (int)product.ReviewResult.AggregateScore;
            Quality = score + "/100 (" + ProductReviewViewModel.GetRatingLabel(score) + ")";
        } else {
            Quality = ((int)product.OverallQuality) + "/100";
        }

        FeatureList = UIFormatting.FormatFeatureList(product.SelectedFeatureIds, _templates);

        ActiveUsers = product.ActiveUserCount.ToString("N0");
        LifetimeRevenue = UIFormatting.FormatMoney(product.TotalLifetimeRevenue);
        MonthlyRevenue = UIFormatting.FormatMoney(product.MonthlyRevenue);
        if (product.IsShipped) {
            int launchTick = product.CreationTick + product.TotalDevelopmentTicks;
            int launchDay = launchTick / TimeState.TicksPerDay;
            int year = TimeState.GetYear(launchDay);
            int month = TimeState.GetMonth(launchDay);
            int dayOfMonth = TimeState.GetDayOfMonth(launchDay);
            LaunchDate = UIFormatting.FormatDate(dayOfMonth, month, year);
        } else {
            LaunchDate = "In Development";
        }
        LifecycleStage = product.LifecycleStage.ToString();
        if (product.PreviousDailyActiveUsers == 0)
            UserTrend = product.ActiveUserCount > 0 ? "New" : "--";
        else {
            int delta = product.ActiveUserCount - product.PreviousDailyActiveUsers;
            UserTrend = delta > 0 ? "Growth" : delta < 0 ? "Decline" : "Stable";
        }
        MaintenanceStatus = product.IsMaintained ? "Maintained" : "Unmaintained";
        HasCrisis = product.CrisisLevel > 0;
        switch (product.CrisisLevel)
        {
            case 0:
                HasCrisis = false;
                CrisisDescription = "";
                break;
            case 1:
                HasCrisis = true;
                CrisisDescription = "Bug Spike (Level 1) — Users are reporting increased bugs due to insufficient maintenance. Assign a QA team and fund the maintenance budget to prevent escalation.";
                break;
            case 2:
                HasCrisis = true;
                CrisisDescription = "Security Breach (Level 2) — A vulnerability has been detected. User trust is declining and churn is accelerating. Increase maintenance budget and assign a QA team immediately.";
                break;
            default:
                HasCrisis = true;
                CrisisDescription = "CATASTROPHIC FAILURE (Level 3) — The product is failing. Users are abandoning rapidly and a 50% revenue penalty is in effect. Emergency maintenance action required or the product may become unrecoverable.";
                break;
        }

        float marketShare = 0f;
        if (marketState?.currentMarketShares != null) {
            foreach (var kvp in marketState.currentMarketShares) {
                var entries = kvp.Value;
                int count = entries.Count;
                for (int i = 0; i < count; i++) {
                    if (entries[i].ProductId == id) {
                        marketShare += entries[i].GlobalUserSharePercent;
                        break;
                    }
                }
            }
        }
        MarketSharePercent = UIFormatting.FormatPercent(marketShare);

        if (IsPlayerOwned && product.TeamAssignments != null) {
            foreach (var kvp in product.TeamAssignments) {
                _teamAssignments.Add(kvp.Key.ToString() + ": " + kvp.Value.ToString());
            }
        }

        if (IsPlayerOwned && product.IsShipped) {
            bool hasQA = product.TeamAssignments != null && product.TeamAssignments.ContainsKey(ProductTeamRole.QA);
            bool hasMkt = product.TeamAssignments != null && product.TeamAssignments.ContainsKey(ProductTeamRole.Marketing);
            HasQATeamAssigned = hasQA;
            HasMarketingTeamAssigned = hasMkt;

            long maintBudget = product.MaintenanceBudgetMonthly;
            long maintDrain = _estimatedMaintDrain;
            MaintenanceBudgetMonthly = UIFormatting.FormatMoney(maintBudget) + "/mo";
            MaintenanceDrainRate = UIFormatting.FormatMoney(maintDrain) + "/mo";
            if (!hasQA) {
                MaintenanceMonthsCoverage = "No QA Team";
                IsMaintenanceUnderfunded = true;
            } else if (maintBudget <= 0) {
                MaintenanceMonthsCoverage = "Unfunded";
                IsMaintenanceUnderfunded = true;
            } else if (maintDrain <= 0) {
                MaintenanceMonthsCoverage = "--";
                IsMaintenanceUnderfunded = false;
            } else if (maintDrain > maintBudget) {
                MaintenanceMonthsCoverage = "Underfunded";
                IsMaintenanceUnderfunded = true;
            } else {
                float months = maintDrain > 0 ? (float)maintBudget / maintDrain : 0f;
                MaintenanceMonthsCoverage = months.ToString("F1") + " months";
                IsMaintenanceUnderfunded = false;
            }
            MaintenanceQualityDisplay = ((int)product.MaintenanceQuality) + "%";

            long mktBudget = product.MarketingBudgetMonthly;
            long mktDrain = _estimatedMktDrain;
            MarketingBudgetMonthly = UIFormatting.FormatMoney(mktBudget) + "/mo";
            MarketingDrainRate = UIFormatting.FormatMoney(mktDrain) + "/mo";
            if (!hasMkt) {
                MarketingMonthsCoverage = "No Marketing Team";
                IsMarketingUnderfunded = true;
            } else if (mktBudget <= 0) {
                MarketingMonthsCoverage = "Unfunded";
                IsMarketingUnderfunded = true;
            } else if (mktDrain <= 0) {
                MarketingMonthsCoverage = "--";
                IsMarketingUnderfunded = false;
            } else if (mktDrain > mktBudget) {
                MarketingMonthsCoverage = "Underfunded";
                IsMarketingUnderfunded = true;
            } else {
                float months = mktDrain > 0 ? (float)mktBudget / mktDrain : 0f;
                MarketingMonthsCoverage = months.ToString("F1") + " months";
                IsMarketingUnderfunded = false;
            }
        } else {
            HasQATeamAssigned = false;
            HasMarketingTeamAssigned = false;
            MaintenanceBudgetMonthly = "--";
            MaintenanceDrainRate = "--";
            MaintenanceMonthsCoverage = "--";
            MaintenanceQualityDisplay = "--";
            IsMaintenanceUnderfunded = false;
            MarketingBudgetMonthly = "--";
            MarketingDrainRate = "--";
            MarketingMonthsCoverage = "--";
            IsMarketingUnderfunded = false;
        }

        ReviewVM.SetProduct(id);
        ReviewVM.RefreshFromProduct(product);
    }
}
