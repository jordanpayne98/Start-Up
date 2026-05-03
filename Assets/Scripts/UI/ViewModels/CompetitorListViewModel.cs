using System.Collections.Generic;

public struct CompetitorRowVM
{
    public CompetitorId Id;
    public string CompanyName;
    public string ReputationLabel;
    public int ReputationPoints;
    public int EmployeeCount;
    public int ProductCount;
    public string Revenue;
    public string Cash;
    public float PlayerOwnershipPercent;
    public bool IsNearBankruptcy;
    public long RevenueRaw;
    public long CashRaw;
}

public enum CompetitorSortColumn
{
    Name,
    Reputation,
    Employees,
    Products,
    Revenue,
    Cash,
    PlayerStake
}

public class CompetitorListViewModel : IViewModel
{
    private readonly List<CompetitorRowVM> _competitorRows = new List<CompetitorRowVM>();
    public List<CompetitorRowVM> CompetitorRows => _competitorRows;

    public CompetitorSortColumn CurrentSort { get; private set; }
    public SortDirection CurrentDirection { get; private set; }

    public CompetitorListViewModel() {
        CurrentSort = CompetitorSortColumn.Revenue;
        CurrentDirection = SortDirection.Descending;
    }

    public void Sort(CompetitorSortColumn column) {
        if (CurrentSort == column) {
            CurrentDirection = CurrentDirection == SortDirection.Ascending
                ? SortDirection.Descending : SortDirection.Ascending;
        } else {
            CurrentSort = column;
            CurrentDirection = SortDirection.Descending;
        }
        ApplySort();
    }

    public bool IsDirty { get; private set; }
    public void ClearDirty() => IsDirty = false;

    public void Refresh(GameStateSnapshot snapshot) {
        if (snapshot == null) return;
        Refresh(snapshot.CompetitorState, snapshot.StockState, snapshot.MarketStateRef);
        IsDirty = true;
    }

    public void Refresh(CompetitorState compState, StockState stockState, MarketState marketState) {
        if (compState == null) return;
        _competitorRows.Clear();

        foreach (var kvp in compState.competitors) {
            var comp = kvp.Value;
            if (comp.IsBankrupt || comp.IsAbsorbed) continue;

            float playerOwnership = 0f;
            if (stockState != null && stockState.listings.TryGetValue(comp.Id, out var listing)) {
                if (listing.OwnershipBreakdown != null) {
                    foreach (var ownerKvp in listing.OwnershipBreakdown) {
                        if (ownerKvp.Key.Value == 0) {
                            playerOwnership = ownerKvp.Value;
                            break;
                        }
                    }
                }
            }

            _competitorRows.Add(new CompetitorRowVM {
                Id = comp.Id,
                CompanyName = comp.CompanyName,
                ReputationLabel = FormatReputationTier(ReputationSystem.CalculateTier(comp.ReputationPoints)),
                ReputationPoints = comp.ReputationPoints,
                EmployeeCount = comp.EmployeeIds != null ? comp.EmployeeIds.Count : 0,
                ProductCount = comp.ActiveProductIds != null ? comp.ActiveProductIds.Count : 0,
                Revenue = UIFormatting.FormatMoney(comp.Finance.MonthlyRevenue),
                Cash = UIFormatting.FormatMoney(comp.Finance.Cash),
                PlayerOwnershipPercent = playerOwnership,
                IsNearBankruptcy = comp.Finance.ConsecutiveNegativeCashMonths >= 3,
                RevenueRaw = comp.Finance.MonthlyRevenue,
                CashRaw = comp.Finance.Cash
            });
        }

        ApplySort();
    }

    private void ApplySort() {
        int count = _competitorRows.Count;
        if (count <= 1) return;
        for (int i = 1; i < count; i++) {
            var key = _competitorRows[i];
            int j = i - 1;
            while (j >= 0 && CompareRows(_competitorRows[j], key) > 0) {
                _competitorRows[j + 1] = _competitorRows[j];
                j--;
            }
            _competitorRows[j + 1] = key;
        }
    }

    private int CompareRows(CompetitorRowVM a, CompetitorRowVM b) {
        int result = 0;
        switch (CurrentSort) {
            case CompetitorSortColumn.Name:
                result = string.Compare(a.CompanyName, b.CompanyName, System.StringComparison.Ordinal);
                break;
            case CompetitorSortColumn.Reputation:
                result = a.ReputationPoints.CompareTo(b.ReputationPoints);
                break;
            case CompetitorSortColumn.Employees:
                result = a.EmployeeCount.CompareTo(b.EmployeeCount);
                break;
            case CompetitorSortColumn.Products:
                result = a.ProductCount.CompareTo(b.ProductCount);
                break;
            case CompetitorSortColumn.Revenue:
                result = a.RevenueRaw.CompareTo(b.RevenueRaw);
                break;
            case CompetitorSortColumn.Cash:
                result = a.CashRaw.CompareTo(b.CashRaw);
                break;
            case CompetitorSortColumn.PlayerStake:
                result = a.PlayerOwnershipPercent.CompareTo(b.PlayerOwnershipPercent);
                break;
        }
        return CurrentDirection == SortDirection.Descending ? -result : result;
    }

    private static string FormatReputationTier(ReputationTier tier) {
        switch (tier) {
            case ReputationTier.Unknown:        return "Unknown";
            case ReputationTier.Startup:        return "Startup";
            case ReputationTier.Established:    return "Established";
            case ReputationTier.Respected:      return "Respected";
            case ReputationTier.IndustryLeader: return "Industry Leader";
            default:                            return tier.ToString();
        }
    }
}
