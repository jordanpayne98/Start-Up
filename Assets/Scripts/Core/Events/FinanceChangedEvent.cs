public class FinanceChangedEvent : GameEvent
{
    public int NewMoney { get; }
    public int NewMonthlyExpenses { get; }
    public int NewTotalRevenue { get; }
    public FinancialHealthState FinancialHealth { get; }
    public int DailyObligations { get; }
    public int RunwayDays { get; }

    public FinanceChangedEvent(int tick, int newMoney, int newMonthlyExpenses, int newTotalRevenue,
        FinancialHealthState financialHealth = FinancialHealthState.Stable,
        int dailyObligations = 0,
        int runwayDays = 0) : base(tick)
    {
        NewMoney = newMoney;
        NewMonthlyExpenses = newMonthlyExpenses;
        NewTotalRevenue = newTotalRevenue;
        FinancialHealth = financialHealth;
        DailyObligations = dailyObligations;
        RunwayDays = runwayDays;
    }
}
