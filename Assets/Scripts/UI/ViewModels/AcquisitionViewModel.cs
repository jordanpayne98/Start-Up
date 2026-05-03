using System.Collections.Generic;

public class AcquisitionViewModel : IViewModel
{
    public CompetitorId TargetId { get; private set; }
    public string TargetCompanyName { get; private set; }
    public string TotalPrice { get; private set; }
    public string EmployeesGained { get; private set; }
    public string AvgSkillGained { get; private set; }
    public List<string> ProductsGained { get; private set; }
    public string MarketShareGained { get; private set; }
    public string UnprofitableProducts { get; private set; }
    public string EstimatedMonthlySalaryIncrease { get; private set; }
    public string EstimatedMaintenanceCostIncrease { get; private set; }
    public string EstimatedMonthlyProfitChange { get; private set; }
    public bool IsProfitChangePositive { get; private set; }

    private readonly List<string> _productsGained = new List<string>();

    public AcquisitionViewModel() {
        ProductsGained = _productsGained;
    }

    public bool IsDirty { get; private set; }
    public void ClearDirty() => IsDirty = false;

    public void Refresh(GameStateSnapshot snapshot) { IsDirty = true; }

    public void Refresh(CompetitorId id, CompetitorState compState, ProductState productState,
        StockState stockState, FinanceState financeState) {
        _productsGained.Clear();
        TargetId = id;

        if (compState?.competitors == null || !compState.competitors.TryGetValue(id, out var comp)) {
            TargetCompanyName = "Unknown";
            TotalPrice = "--";
            EmployeesGained = "--";
            AvgSkillGained = "--";
            MarketShareGained = "--";
            UnprofitableProducts = "--";
            EstimatedMonthlySalaryIncrease = "--";
            EstimatedMaintenanceCostIncrease = "--";
            EstimatedMonthlyProfitChange = "--";
            IsProfitChangePositive = false;
            return;
        }

        TargetCompanyName = comp.CompanyName;
        EmployeesGained = (comp.EmployeeIds != null ? comp.EmployeeIds.Count : 0).ToString();
        AvgSkillGained = "--";

        long remainingCost = 0L;
        if (stockState?.listings != null && stockState.listings.TryGetValue(id, out var listing)) {
            float unownedPct = listing.UnownedPercentage;
            float playerPct = 0f;
            if (listing.OwnershipBreakdown != null) {
                foreach (var kvp in listing.OwnershipBreakdown) {
                    if (kvp.Key.Value == 0) { playerPct = kvp.Value; break; }
                }
            }
            float toBuyPct = unownedPct;
            remainingCost = (long)(listing.StockPrice * toBuyPct);
        }
        TotalPrice = UIFormatting.FormatMoney(remainingCost);

        float totalShare = 0f;
        if (comp.NicheMarketShare != null) {
            foreach (var ms in comp.NicheMarketShare) totalShare += ms.Value;
        }
        MarketShareGained = UIFormatting.FormatPercent(totalShare);

        int unprofitable = 0;
        long monthlyMaintenanceIncrease = 0L;
        if (comp.ActiveProductIds != null && productState != null) {
            int count = comp.ActiveProductIds.Count;
            for (int i = 0; i < count; i++) {
                var pid = comp.ActiveProductIds[i];
                Product prod = null;
                if (productState.shippedProducts != null) productState.shippedProducts.TryGetValue(pid, out prod);
                if (prod == null) continue;

                _productsGained.Add(prod.ProductName + " (" + UIFormatting.FormatMoney(prod.MonthlyRevenue) + "/mo)");
                if (prod.MonthlyRevenue <= 0) unprofitable++;
                monthlyMaintenanceIncrease += prod.MaintenanceBudgetMonthly;
            }
        }
        UnprofitableProducts = unprofitable.ToString();

        long salaryIncrease = comp.Finance.MonthlyExpenses;
        EstimatedMonthlySalaryIncrease = UIFormatting.FormatMoney(salaryIncrease);
        EstimatedMaintenanceCostIncrease = UIFormatting.FormatMoney(monthlyMaintenanceIncrease);

        long profitChange = comp.Finance.MonthlyRevenue - comp.Finance.MonthlyExpenses - monthlyMaintenanceIncrease;
        IsProfitChangePositive = profitChange >= 0;
        EstimatedMonthlyProfitChange = (IsProfitChangePositive ? "+" : "") + UIFormatting.FormatMoney(profitChange) + "/mo";
    }
}
