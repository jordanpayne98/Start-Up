// FinancialImpactCalculator Version: Clean v1

public struct HireImpact
{
    public int NewMonthlyExpenses;
    public int RunwayDays;
    public float ExpenseIncreasePercent;
    public bool CanAffordImmediately;
    public bool IsRisky;
    public bool IsCritical;
    public string WarningMessage;
}

public static class FinancialImpactCalculator
{
    public static HireImpact CalculateHireImpact(FinanceSystem financeSystem, int newMonthlySalary)
    {
        int currentMoney = financeSystem.Money;
        int currentMonthlyExpenses = financeSystem.MonthlyExpenses;
        int newMonthlyExpenses = currentMonthlyExpenses + newMonthlySalary;

        // Use system's daily obligations + monthly obligations converted to daily equivalent
        int currentDailyBurn = financeSystem.DailyObligations + (financeSystem.MonthlyObligations / 30);
        int additionalDailyEquivalent = newMonthlySalary / 30;
        int newDailyBurn = currentDailyBurn + additionalDailyEquivalent;

        int runwayDays;
        if (newDailyBurn > 0)
        {
            int calculatedRunway = currentMoney / newDailyBurn;
            runwayDays = calculatedRunway > 0 ? calculatedRunway : 0;
        }
        else
        {
            runwayDays = currentMoney > 0 ? int.MaxValue : 0;
        }

        float expenseIncreasePercent = 0f;
        if (currentMonthlyExpenses > 0)
        {
            expenseIncreasePercent = ((float)newMonthlySalary / currentMonthlyExpenses) * 100f;
        }
        else
        {
            expenseIncreasePercent = 100f;
        }

        bool canAffordImmediately = currentMoney > 0;
        bool isRisky = runwayDays < 30 && runwayDays > 0;
        bool isCritical = runwayDays < 7 || currentMoney <= 0;

        string warningMessage = "";
        if (currentMoney <= 0)
        {
            warningMessage = "Critical: Cannot afford to hire! No money available.";
        }
        else if (runwayDays < 7)
        {
            warningMessage = $"Critical: Less than 7 days runway! ({runwayDays} days remaining)";
        }
        else if (runwayDays < 30)
        {
            warningMessage = $"Risky: Less than 30 days runway ({runwayDays} days remaining)";
        }

        return new HireImpact
        {
            NewMonthlyExpenses = newMonthlyExpenses,
            RunwayDays = runwayDays == int.MaxValue ? 999 : runwayDays,
            ExpenseIncreasePercent = expenseIncreasePercent,
            CanAffordImmediately = canAffordImmediately,
            IsRisky = isRisky,
            IsCritical = isCritical,
            WarningMessage = warningMessage
        };
    }
}
