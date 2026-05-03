public class GameOverViewModel : IViewModel
{
    public string CompanyName { get; private set; }
    public string GameOverReason { get; private set; }
    public string TimeSurvived { get; private set; }
    public string PeakRevenue { get; private set; }
    public string PeakMarketShare { get; private set; }
    public int TotalProductsShipped { get; private set; }
    public int TotalEmployeesHired { get; private set; }
    public string BiggestCompetitor { get; private set; }

    public GameOverViewModel() {
        CompanyName = "--";
        GameOverReason = "Bankruptcy";
        TimeSurvived = "--";
        PeakRevenue = "--";
        PeakMarketShare = "--";
        TotalProductsShipped = 0;
        TotalEmployeesHired = 0;
        BiggestCompetitor = "--";
    }

    public bool IsDirty { get; private set; }
    public void ClearDirty() => IsDirty = false;

    public void Refresh(GameStateSnapshot snapshot) { IsDirty = true; }

    public void Refresh(GameState gameState, CompetitorState compState, string reason) {
        if (gameState == null) return;

        CompanyName = gameState.companyName ?? "Your Company";
        GameOverReason = reason ?? "Bankruptcy";

        int totalTicks = gameState.currentTick;
        int days = totalTicks / TimeState.TicksPerDay;
        int months = days / 30;
        int years = months / 12;
        int remMonths = months % 12;
        if (years > 0)
            TimeSurvived = years + "y " + remMonths + "m";
        else if (months > 0)
            TimeSurvived = months + " months";
        else
            TimeSurvived = days + " days";

        long peakRev = 0L;
        if (gameState.financeState?.transactions != null) {
            int count = gameState.financeState.transactions.Count;
            for (int i = 0; i < count; i++) {
                var tx = gameState.financeState.transactions[i];
                if (tx.amount > 0 && tx.amount > peakRev)
                    peakRev = tx.amount;
            }
        }
        PeakRevenue = UIFormatting.FormatMoney(peakRev);

        float peakShare = 0f;
        if (gameState.productState?.shippedProducts != null && gameState.marketState?.currentMarketShares != null) {
            foreach (var kvp in gameState.marketState.currentMarketShares) {
                var entries = kvp.Value;
                int count = entries.Count;
                for (int i = 0; i < count; i++) {
                    if (gameState.productState.shippedProducts.ContainsKey(entries[i].ProductId)) {
                        peakShare += entries[i].MarketSharePercent;
                    }
                }
            }
        }
        PeakMarketShare = UIFormatting.FormatPercent(peakShare);

        TotalProductsShipped = gameState.productState?.shippedProducts != null
            ? gameState.productState.shippedProducts.Count : 0;

        TotalEmployeesHired = gameState.employeeState?.employees != null
            ? gameState.employeeState.employees.Count : 0;

        BiggestCompetitor = "--";
        if (compState?.competitors != null) {
            long biggestRev = -1L;
            foreach (var kvp in compState.competitors) {
                var comp = kvp.Value;
                if (comp.IsBankrupt || comp.IsAbsorbed) continue;
                if (comp.Finance.MonthlyRevenue > biggestRev) {
                    biggestRev = comp.Finance.MonthlyRevenue;
                    BiggestCompetitor = comp.CompanyName;
                }
            }
        }
    }
}
