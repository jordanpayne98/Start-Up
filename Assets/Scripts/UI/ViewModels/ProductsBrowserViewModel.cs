using System.Collections.Generic;

public struct BrowserProductRowVM
{
    public ProductId Id;
    public CompetitorId? OwnerId;
    public string ProductName;
    public string CompanyName;
    public string Niche;
    public string Quality;
    public string ActiveUsers;
    public string Revenue;
    public string MarketShare;
    public string LaunchDate;
    public string LifecycleStage;
    public string UserTrend;
    public string ReviewScore;
    public string ReviewRating;
    public int QualityRaw;
    public int ActiveUsersRaw;
    public long RevenueRaw;
    public float MarketShareRaw;
    public bool IsPlayerOwned;
    public bool IsArchived;
    public long MonthlyRevenueRaw;
    public long MaintenanceCostRaw;
    public string MonthlyRevenueDisplay;
    public string MaintenanceCostDisplay;
    public string ProfitDisplay;
    public bool HasCrisis;
    public CrisisEventType? CrisisType;
}

public enum ProductBrowserSortColumn
{
    Name,
    Company,
    Niche,
    Quality,
    ActiveUsers,
    Revenue,
    MarketShare
}

public enum OwnerFilter { All, MyProducts, Competitor }
public enum StatusFilter { All, Live, Archived }

public class ProductsBrowserViewModel : IViewModel
{
    private readonly List<BrowserProductRowVM> _allProducts = new List<BrowserProductRowVM>();
    private readonly List<BrowserProductRowVM> _filteredProducts = new List<BrowserProductRowVM>();

    private ProductState _cachedProductState;
    private CompetitorState _cachedCompState;
    private MarketState _cachedMarketState;
    private ProductTemplateDefinition[] _templates;

    public List<BrowserProductRowVM> AllProducts => _filteredProducts;
    public ProductNiche? FilterNiche { get; private set; }
    public CompetitorId? FilterCompany { get; private set; }
    public ProductBrowserSortColumn CurrentSort { get; private set; }
    public SortDirection CurrentDirection { get; private set; }
    public OwnerFilter CurrentOwner { get; private set; }
    public StatusFilter CurrentStatus { get; private set; }
    public string SummaryMonthlyRevenue { get; private set; }
    public string SummaryMaintenanceCost { get; private set; }
    public string SummaryNetIncome { get; private set; }
    public string SummaryTotalBudgets { get; private set; }

    public void SetTemplates(ProductTemplateDefinition[] templates) {
        _templates = templates;
    }

    public ProductsBrowserViewModel() {
        CurrentSort = ProductBrowserSortColumn.ActiveUsers;
        CurrentDirection = SortDirection.Descending;
        CurrentOwner = OwnerFilter.All;
        CurrentStatus = StatusFilter.Live;
    }

    public void SetFilter(ProductNiche? niche, CompetitorId? company) {
        FilterNiche = niche;
        FilterCompany = company;
        ApplyFilterAndSort();
    }

    public void SetOwnerFilter(OwnerFilter filter) {
        CurrentOwner = filter;
        ApplyFilterAndSort();
    }

    public void SetStatusFilter(StatusFilter filter) {
        CurrentStatus = filter;
        ApplyFilterAndSort();
    }

    public void SetSort(ProductBrowserSortColumn column) {
        if (CurrentSort == column) {
            CurrentDirection = CurrentDirection == SortDirection.Ascending
                ? SortDirection.Descending : SortDirection.Ascending;
        } else {
            CurrentSort = column;
            CurrentDirection = SortDirection.Descending;
        }
        ApplyFilterAndSort();
    }

    public void Refresh(IReadOnlyGameState state) {
        if (state == null) return;
        var snapshot = state as GameStateSnapshot;
        if (snapshot == null) return;
        Refresh(snapshot.ProductStateRef, snapshot.CompetitorState, snapshot.MarketStateRef,
                snapshot.CompanyName, snapshot.CurrentTick);
    }

    public void Refresh(ProductState productState, CompetitorState compState, MarketState marketState,
        string playerCompanyName, int currentTick) {
        if (productState == null) return;
        _cachedProductState = productState;
        _cachedCompState = compState;
        _cachedMarketState = marketState;
        _allProducts.Clear();

        foreach (var kvp in productState.shippedProducts) {
            var product = kvp.Value;
            if (!product.IsOnMarket) continue;

            string companyName = product.IsCompetitorProduct
                ? GetCompetitorName(compState, product.OwnerCompanyId.ToCompetitorId())
                : (playerCompanyName ?? "Player");

            float marketShare = 0f;
            if (marketState != null) {
                if (marketState.currentMarketShares != null) {
                    foreach (var msKvp in marketState.currentMarketShares) {
                        var entries = msKvp.Value;
                        int eCount = entries.Count;
                        for (int e = 0; e < eCount; e++) {
                            if (entries[e].ProductId == product.Id) {
                                marketShare = entries[e].GlobalUserSharePercent;
                                goto shareFound;
                            }
                        }
                    }
                }
                if (marketState.categoryMarketShares != null) {
                    foreach (var catKvp in marketState.categoryMarketShares) {
                        var entries = catKvp.Value;
                        int eCount = entries.Count;
                        for (int e = 0; e < eCount; e++) {
                            if (entries[e].ProductId == product.Id) {
                                marketShare = entries[e].GlobalUserSharePercent;
                                goto shareFound;
                            }
                        }
                    }
                }
                shareFound:;
            }

            int launchTick = product.CreationTick + product.TotalDevelopmentTicks;
            int launchDay = launchTick / TimeState.TicksPerDay;
            int launchYear = TimeState.GetYear(launchDay);
            int launchMonth = TimeState.GetMonth(launchDay);
            int launchDayOfMonth = TimeState.GetDayOfMonth(launchDay);

            long monthlyRevenue = product.MonthlyRevenue;
            long maintenanceCostPerMonth = product.MaintenanceBudgetMonthly;
            long marketingCostPerMonth = product.MarketingBudgetMonthly;
            long totalBudgets = maintenanceCostPerMonth + marketingCostPerMonth;
            long profit = monthlyRevenue - totalBudgets;

            bool hasCrisis = product.CrisisLevel > 0;
            CrisisEventType? crisisType = null;
            if (hasCrisis) {
                if (product.CrisisLevel >= 3) crisisType = CrisisEventType.Catastrophic;
                else if (product.CrisisLevel >= 2) crisisType = CrisisEventType.ModerateBreach;
                else crisisType = CrisisEventType.MinorBug;
            }

            _allProducts.Add(new BrowserProductRowVM {
                Id = product.Id,
                OwnerId = product.IsCompetitorProduct ? (CompetitorId?)product.OwnerCompanyId.ToCompetitorId() : null,
                ProductName = product.ProductName,
                CompanyName = companyName,
                Niche = UIFormatting.FormatNicheOrCategory(product),
                Quality = product.ReviewResult != null
                    ? ProductReviewViewModel.GetRatingLabel((int)product.ReviewResult.AggregateScore)
                    : "--",
                ActiveUsers = product.ActiveUserCount.ToString("N0"),
                Revenue = UIFormatting.FormatMoney(DeriveRevenue(product, currentTick)),
                MarketShare = UIFormatting.FormatPercent(marketShare),
                LaunchDate = UIFormatting.FormatDate(launchDayOfMonth, launchMonth, launchYear),
                LifecycleStage = FormatLifecycle(product.LifecycleStage),
                UserTrend = ComputeUserTrend(product.ActiveUserCount, product.PreviousDailyActiveUsers, product.LifecycleStage),
                ReviewScore = product.ReviewResult != null ? ((int)product.ReviewResult.AggregateScore).ToString() : "--",
                ReviewRating = product.ReviewResult != null ? ProductReviewViewModel.GetRatingLabel((int)product.ReviewResult.AggregateScore) : "--",
                QualityRaw = product.ReviewResult != null ? (int)product.ReviewResult.AggregateScore : 0,
                ActiveUsersRaw = product.ActiveUserCount,
                RevenueRaw = DeriveRevenue(product, currentTick),
                MarketShareRaw = marketShare,
                IsPlayerOwned = !product.IsCompetitorProduct,
                IsArchived = false,
                MonthlyRevenueRaw = monthlyRevenue,
                MaintenanceCostRaw = totalBudgets,
                MonthlyRevenueDisplay = UIFormatting.FormatMoney(monthlyRevenue),
                MaintenanceCostDisplay = UIFormatting.FormatMoney(totalBudgets),
                ProfitDisplay = UIFormatting.FormatMoney(profit),
                HasCrisis = hasCrisis,
                CrisisType = crisisType
            });
        }

        if (productState.archivedProducts != null) {
            foreach (var kvp in productState.archivedProducts) {
                var product = kvp.Value;

                string companyName = product.IsCompetitorProduct
                    ? GetCompetitorName(compState, product.OwnerCompanyId.ToCompetitorId())
                    : (playerCompanyName ?? "Player");

                int launchTick = product.CreationTick + product.TotalDevelopmentTicks;
                int launchDay = launchTick / TimeState.TicksPerDay;
                int launchYear = TimeState.GetYear(launchDay);
                int launchMonth = TimeState.GetMonth(launchDay);
                int launchDayOfMonth = TimeState.GetDayOfMonth(launchDay);

                _allProducts.Add(new BrowserProductRowVM {
                    Id = product.Id,
                    OwnerId = product.IsCompetitorProduct ? (CompetitorId?)product.OwnerCompanyId.ToCompetitorId() : null,
                    ProductName = product.ProductName,
                    CompanyName = companyName,
                    Niche = UIFormatting.FormatNicheOrCategory(product),
                    Quality = product.ReviewResult != null
                        ? ProductReviewViewModel.GetRatingLabel((int)product.ReviewResult.AggregateScore)
                        : "--",
                    ActiveUsers = product.ActiveUserCount.ToString("N0"),
                    Revenue = UIFormatting.FormatMoney(product.TotalLifetimeRevenue),
                    MarketShare = "--",
                    LaunchDate = UIFormatting.FormatDate(launchDayOfMonth, launchMonth, launchYear),
                    LifecycleStage = "Archived",
                    UserTrend = "--",
                    ReviewScore = product.ReviewResult != null ? ((int)product.ReviewResult.AggregateScore).ToString() : "--",
                    ReviewRating = product.ReviewResult != null ? ProductReviewViewModel.GetRatingLabel((int)product.ReviewResult.AggregateScore) : "--",
                    QualityRaw = product.ReviewResult != null ? (int)product.ReviewResult.AggregateScore : 0,
                    ActiveUsersRaw = product.ActiveUserCount,
                    RevenueRaw = product.TotalLifetimeRevenue,
                    MarketShareRaw = 0f,
                    IsPlayerOwned = !product.IsCompetitorProduct,
                    IsArchived = true,
                    MonthlyRevenueRaw = 0,
                    MonthlyRevenueDisplay = "--",
                    MaintenanceCostDisplay = "--",
                    ProfitDisplay = "--",
                    HasCrisis = false,
                    CrisisType = null
                });
            }
        }

        long summaryRev = 0L;
        long summaryMaint = 0L;
        long summaryMkt = 0L;
        int allCount = _allProducts.Count;
        for (int i = 0; i < allCount; i++) {
            var row = _allProducts[i];
            if (!row.IsPlayerOwned || row.IsArchived) continue;
            summaryRev += row.MonthlyRevenueRaw;
            summaryMaint += row.MaintenanceCostRaw;
        }
        // MaintenanceCostRaw now contains maintenance + marketing budgets combined
        summaryMkt = 0L; // already folded into summaryMaint via MaintenanceCostRaw = totalBudgets
        SummaryMonthlyRevenue = UIFormatting.FormatMoney(summaryRev);
        SummaryMaintenanceCost = UIFormatting.FormatMoney(summaryMaint);
        SummaryTotalBudgets = UIFormatting.FormatMoney(summaryMaint);
        SummaryNetIncome = UIFormatting.FormatMoney(summaryRev - summaryMaint);

        ApplyFilterAndSort();
    }

    private void ApplyFilterAndSort() {
        _filteredProducts.Clear();
        int count = _allProducts.Count;
        for (int i = 0; i < count; i++) {
            var row = _allProducts[i];
            if (CurrentOwner == OwnerFilter.MyProducts && !row.IsPlayerOwned) continue;
            if (CurrentOwner == OwnerFilter.Competitor && row.IsPlayerOwned) continue;
            if (CurrentStatus == StatusFilter.Live && row.IsArchived) continue;
            if (CurrentStatus == StatusFilter.Archived && !row.IsArchived) continue;
            if (FilterCompany.HasValue && row.OwnerId != FilterCompany.Value) continue;
            _filteredProducts.Add(row);
        }

        int fCount = _filteredProducts.Count;
        for (int i = 1; i < fCount; i++) {
            var key = _filteredProducts[i];
            int j = i - 1;
            while (j >= 0 && CompareRows(_filteredProducts[j], key) > 0) {
                _filteredProducts[j + 1] = _filteredProducts[j];
                j--;
            }
            _filteredProducts[j + 1] = key;
        }
    }

    private int CompareRows(BrowserProductRowVM a, BrowserProductRowVM b) {
        int result = 0;
        switch (CurrentSort) {
            case ProductBrowserSortColumn.Name:
                result = string.Compare(a.ProductName, b.ProductName, System.StringComparison.Ordinal);
                break;
            case ProductBrowserSortColumn.Company:
                result = string.Compare(a.CompanyName, b.CompanyName, System.StringComparison.Ordinal);
                break;
            case ProductBrowserSortColumn.Niche:
                result = string.Compare(a.Niche, b.Niche, System.StringComparison.Ordinal);
                break;
            case ProductBrowserSortColumn.Quality:
                result = a.QualityRaw.CompareTo(b.QualityRaw);
                break;
            case ProductBrowserSortColumn.ActiveUsers:
                result = a.ActiveUsersRaw.CompareTo(b.ActiveUsersRaw);
                break;
            case ProductBrowserSortColumn.Revenue:
                result = a.RevenueRaw.CompareTo(b.RevenueRaw);
                break;
            case ProductBrowserSortColumn.MarketShare:
                result = a.MarketShareRaw.CompareTo(b.MarketShareRaw);
                break;
        }
        return CurrentDirection == SortDirection.Descending ? -result : result;
    }

    private static string GetCompetitorName(CompetitorState compState, CompetitorId id) {
        if (compState == null) return "Unknown";
        if (compState.competitors.TryGetValue(id, out var comp))
            return comp.CompanyName;
        return "Unknown";
    }

    private static string FormatLifecycle(ProductLifecycleStage stage) {
        switch (stage) {
            case ProductLifecycleStage.PreLaunch: return "Pre-Launch";
            case ProductLifecycleStage.Launch:    return "Launch";
            case ProductLifecycleStage.Growth:    return "Growth";
            case ProductLifecycleStage.Plateau:   return "Plateau";
            case ProductLifecycleStage.Decline:   return "Decline";
            default:                              return stage.ToString();
        }
    }

    private static string ComputeUserTrend(int activeUsers, int previousUsers, ProductLifecycleStage stage) {
        if (previousUsers == 0) {
            return activeUsers > 0 ? "New" : "--";
        }
        int delta = activeUsers - previousUsers;
        if (delta > 0) return "Growth";
        if (delta < 0) return "Decline";
        return "Stable";
    }

    private static long DeriveRevenue(Product product, int currentTick) {
        return (long)product.MonthlyRevenue;
    }

    public void RefreshProductDetail(ProductDetailViewModel detailVM, ProductId id) {
        detailVM.SetTemplates(_templates);
        detailVM.Refresh(id, _cachedProductState, _cachedCompState, _cachedMarketState);
    }
}
