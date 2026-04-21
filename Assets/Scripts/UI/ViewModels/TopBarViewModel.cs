public class TopBarViewModel : IViewModel
{
    public string CompanyName { get; private set; }
    public string MoneyDisplay { get; private set; }
    public string DateDisplay { get; private set; }
    public string ReputationTier { get; private set; }
    public bool IsAdvancing { get; private set; }
    public string FinanceHealthClass { get; private set; }
    public string NetIncomeDisplay { get; private set; }
    public bool IsNetPositive { get; private set; }
    public string ReputationColourClass { get; private set; }

    public TopBarViewModel() {
        CompanyName = "My Company";
        MoneyDisplay = "$0";
        DateDisplay = "Day 1, Month 1";
        ReputationTier = "Unknown";
        IsAdvancing = false;
        FinanceHealthClass = "finance--stable";
        NetIncomeDisplay = "Net: $0/mo";
        IsNetPositive = true;
        ReputationColourClass = "text-muted";
    }

    public void Refresh(IReadOnlyGameState state) {
        if (state == null) return;
        CompanyName = state.CompanyName ?? "My Company";
        MoneyDisplay = UIFormatting.FormatMoney(state.Money);
        DateDisplay = UIFormatting.FormatDateTime(state.DayOfMonth, state.CurrentMonth, state.CurrentYear, state.CurrentHour, state.CurrentMinute);
        ReputationTier = UIFormatting.FormatReputationTier(state.CurrentReputationTier);
        IsAdvancing = state.IsAdvancing;
        ReputationColourClass = state.CurrentReputationTier switch {
            global::ReputationTier.Unknown        => "text-muted",
            global::ReputationTier.Startup        => "text-warning",
            global::ReputationTier.Established    => "text-accent",
            global::ReputationTier.Respected      => "text-success",
            global::ReputationTier.IndustryLeader => "text-special",
            _                                     => "text-muted"
        };
        FinanceHealthClass = state.FinancialHealth switch {
            FinancialHealthState.Stable     => "finance--stable",
            FinancialHealthState.Tight      => "finance--tight",
            FinancialHealthState.Distressed => "finance--tight",
            FinancialHealthState.Insolvent  => "finance--critical",
            FinancialHealthState.Bankrupt   => "finance--critical",
            _                               => "finance--stable"
        };

        int monthlyRevenue = 0;
        if (state.ShippedProducts != null) {
            foreach (var kvp in state.ShippedProducts) {
                if (kvp.Value.IsCompetitorProduct) continue;
                monthlyRevenue += state.GetProductMonthlyRevenue(kvp.Key);
            }
        }
        int net = monthlyRevenue - state.MonthlyExpenses;
        IsNetPositive = net >= 0;
        NetIncomeDisplay = "Net: " + (net >= 0 ? "+" : "") + UIFormatting.FormatMoney(net) + "/mo";
    }
}
