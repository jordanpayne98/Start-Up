public class BankruptcyWarningViewModel : IViewModel
{
    public string CurrentCash { get; private set; }
    public string MonthlyBurnRate { get; private set; }
    public int MonthsRemaining { get; private set; }
    public bool CanTakeLoan { get; private set; }
    public bool HasStockToSell { get; private set; }
    public bool HasProductsToSell { get; private set; }
    public bool IsActive { get; private set; }

    public bool IsDirty { get; private set; }
    public void ClearDirty() => IsDirty = false;

    public void Refresh(GameStateSnapshot snapshot) { IsDirty = true; }

    public void Refresh(FinanceState financeState, StockState stockState,
        ProductState productState, LoanSystem loanSystem) {
        if (financeState == null) {
            IsActive = false;
            return;
        }

        bool isDistressed = financeState.financialHealth == FinancialHealthState.Distressed
            || financeState.financialHealth == FinancialHealthState.Insolvent;
        IsActive = financeState.money < 0 || isDistressed;

        CurrentCash = UIFormatting.FormatMoney(financeState.money);

        long monthlyBurn = 0L;
        if (financeState.recurringCosts != null) {
            int count = financeState.recurringCosts.Count;
            for (int i = 0; i < count; i++) {
                var cost = financeState.recurringCosts[i];
                if (!cost.isActive) continue;
                if (cost.interval == RecurringInterval.Monthly)
                    monthlyBurn += cost.amount;
                else if (cost.interval == RecurringInterval.Daily)
                    monthlyBurn += (long)(cost.amount * 30);
            }
        }
        MonthlyBurnRate = UIFormatting.FormatMoney(monthlyBurn);

        if (monthlyBurn > 0 && financeState.money < 0) {
            long absBalance = -financeState.money;
            MonthsRemaining = (int)(absBalance / monthlyBurn);
            if (MonthsRemaining < 0) MonthsRemaining = 0;
        } else {
            MonthsRemaining = 3;
        }

        CanTakeLoan = loanSystem?.CanTakeLoan() ?? false;

        HasStockToSell = false;
        if (stockState?.holdings != null) {
            foreach (var kvp in stockState.holdings) {
                if (kvp.Value.IsPlayerOwned) {
                    HasStockToSell = true;
                    break;
                }
            }
        }

        HasProductsToSell = false;
        if (productState?.shippedProducts != null) {
            foreach (var kvp in productState.shippedProducts) {
                var prod = kvp.Value;
                if (!prod.IsCompetitorProduct && prod.IsOnMarket) {
                    HasProductsToSell = true;
                    break;
                }
            }
        }
    }
}
