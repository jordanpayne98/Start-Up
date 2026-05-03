using System.Collections.Generic;

public class ProductDetailViewModel : IViewModel
{
    public string ProductName { get; private set; }
    public string CompanyName { get; private set; }
    public CompetitorId? OwnerId { get; private set; }
    public string Niche { get; private set; }
    public string Quality { get; private set; }
    public string FeatureList { get; private set; }
    public string SalesPerMonth { get; private set; }
    public string UsersPerMonth { get; private set; }
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
    public string LifetimeSales { get; private set; }

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

    // Market Identity display properties
    public ProductIdentitySnapshot IdentityAtShip { get; private set; }
    public ProductIdentitySnapshot CurrentIdentity { get; private set; }
    public bool HasIdentity => IdentityAtShip.IsValid;
    public string ShipTag1 { get; private set; }
    public string ShipTag2 { get; private set; }
    public string ShipTag3 { get; private set; }
    public string CurrentTag1 { get; private set; }
    public string CurrentTag2 { get; private set; }
    public string CurrentTag3 { get; private set; }
    public string[] ShiftLabels { get; private set; }
    public bool HasShifts => ShiftLabels != null && ShiftLabels.Length > 0;

    private readonly List<string> _teamAssignments = new List<string>();
    private ProductTemplateDefinition[] _templates;
    private long _estimatedMaintDrain;
    private long _estimatedMktDrain;
    private readonly List<string> _shiftLabelsCache = new List<string>();

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

    public bool IsDirty { get; private set; }
    public void ClearDirty() => IsDirty = false;

    public void Refresh(GameStateSnapshot snapshot) { IsDirty = true; }

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
            SalesPerMonth = "--";
            UsersPerMonth = "--";
            LifetimeSales = "--";
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
            IdentityAtShip = default;
            CurrentIdentity = default;
            ShipTag1 = ""; ShipTag2 = ""; ShipTag3 = "";
            CurrentTag1 = ""; CurrentTag2 = ""; CurrentTag3 = "";
            ShiftLabels = null;
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

        SalesPerMonth = product.HasCompletedFirstMonth ? product.SnapshotMonthlySales.ToString("N0") : "New";
        LifetimeRevenue = UIFormatting.FormatMoney(product.TotalLifetimeRevenue);
        MonthlyRevenue = UIFormatting.FormatMoney(product.HasCompletedFirstMonth ? product.SnapshotMonthlyRevenue : 0L);
        if (product.IsShipped) {
            int launchTick = product.ShipTick;
            int launchDay = launchTick / TimeState.TicksPerDay;
            int year = TimeState.GetYear(launchDay);
            int month = TimeState.GetMonth(launchDay);
            int dayOfMonth = TimeState.GetDayOfMonth(launchDay);
            LaunchDate = UIFormatting.FormatDate(dayOfMonth, month, year);
        } else {
            LaunchDate = "In Development";
        }
        LifecycleStage = product.LifecycleStage.ToString();
        UserTrend = product.HasCompletedFirstMonth ? (product.SnapshotMonthlyTrend ?? "--") : "New";
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

        UsersPerMonth = product.HasCompletedFirstMonth ? product.SnapshotMonthlyUsers.ToString("N0") : "New";

        if (product.IsSubscriptionBased) {
            LifetimeSales = product.TotalSubscribers.ToString("N0") + " active";
        } else {
            LifetimeSales = product.TotalUnitsSold.ToString("N0");
        }

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

        IdentityAtShip = product.IdentityAtShip;
        CurrentIdentity = product.CurrentIdentity;
        ShipTag1 = TagToLabel(IdentityAtShip.PrimaryTag);
        ShipTag2 = TagToLabel(IdentityAtShip.SecondaryTag);
        ShipTag3 = TagToLabel(IdentityAtShip.TertiaryTag);
        CurrentTag1 = TagToLabel(CurrentIdentity.PrimaryTag);
        CurrentTag2 = TagToLabel(CurrentIdentity.SecondaryTag);
        CurrentTag3 = TagToLabel(CurrentIdentity.TertiaryTag);
        ComputeShiftLabels();
    }

    private string TagToLabel(ProductIdentityTag tag) {
        switch (tag) {
            case ProductIdentityTag.Accessible:    return "Accessible";
            case ProductIdentityTag.Premium:       return "Premium";
            case ProductIdentityTag.Safe:          return "Safe";
            case ProductIdentityTag.Experimental:  return "Experimental";
            case ProductIdentityTag.Specialist:    return "Specialist";
            case ProductIdentityTag.Broad:         return "Broad";
            case ProductIdentityTag.Refined:       return "Refined";
            case ProductIdentityTag.FeatureHeavy:  return "Feature-Heavy";
            case ProductIdentityTag.Chaotic:       return "Chaotic";
            case ProductIdentityTag.Disciplined:   return "Disciplined";
            case ProductIdentityTag.Standard:      return "Standard";
            case ProductIdentityTag.Balanced:      return "Balanced";
            case ProductIdentityTag.General:       return "General";
            case ProductIdentityTag.Flexible:      return "Flexible";
            default:                               return "";
        }
    }

    private void ComputeShiftLabels() {
        _shiftLabelsCache.Clear();
        if (!IdentityAtShip.IsValid || !CurrentIdentity.IsValid) {
            ShiftLabels = null;
            return;
        }
        int threshold = 20;
        if (System.Math.Abs((int)CurrentIdentity.PricePositioning - (int)IdentityAtShip.PricePositioning) >= threshold)
            _shiftLabelsCache.Add("Price: " + TagToLabel(IdentityAtShip.PrimaryTag) + " -> " + TagToLabel(CurrentIdentity.PrimaryTag));
        if (System.Math.Abs((int)CurrentIdentity.InnovationRisk - (int)IdentityAtShip.InnovationRisk) >= threshold)
            _shiftLabelsCache.Add("Innovation risk shifted significantly");
        if (System.Math.Abs((int)CurrentIdentity.AudienceBreadth - (int)IdentityAtShip.AudienceBreadth) >= threshold)
            _shiftLabelsCache.Add("Audience breadth shifted significantly");
        if (System.Math.Abs((int)CurrentIdentity.FeatureScope - (int)IdentityAtShip.FeatureScope) >= threshold)
            _shiftLabelsCache.Add("Feature scope shifted significantly");
        if (System.Math.Abs((int)CurrentIdentity.ProductionDiscipline - (int)IdentityAtShip.ProductionDiscipline) >= threshold)
            _shiftLabelsCache.Add("Production discipline shifted significantly");
        ShiftLabels = _shiftLabelsCache.Count > 0 ? _shiftLabelsCache.ToArray() : null;
    }
}
