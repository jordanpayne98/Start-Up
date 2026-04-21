using System;
using System.Collections.Generic;

public enum FinanceCategory
{
    // Revenue
    ContractReward,
    Bonus,
    Refund,
    LoanPrincipal,
    MiscIncome,

    // Expenses
    Salary,
    UpgradePurchase,
    UpgradeMaintenance,
    InterviewCost,
    HRSearchCost,
    LoanPrincipalPayment,
    LoanInterestPayment,
    Rent,
    Utilities,
    MiscExpense,
    TaxPayment,
    ProductMaintenance,
    ProductMarketing
}

public enum RecurringInterval
{
    Daily,
    Monthly
}

public enum FinancialHealthState
{
    Stable,
    Tight,
    Distressed,
    Insolvent,
    Bankrupt
}

[Serializable]
public struct FinanceTransaction
{
    public int amount;
    public FinanceCategory category;
    public int tick;
    public string sourceId;
}

[Serializable]
public struct RecurringCostEntry
{
    public string id;
    public FinanceCategory category;
    public int amount;
    public RecurringInterval interval;
    public string sourceId;
    public bool isActive;
}

[Serializable]
public class FinanceState
{
    public int money;
    public List<FinanceTransaction> transactions;
    public List<RecurringCostEntry> recurringCosts;
    public int consecutiveDaysNegativeCash;
    public int missedObligationCount;
    public FinancialHealthState financialHealth;

    private const int MaxTransactions = 500;

    public bool IsInDebt => money < 0;
    public bool IsBankrupt => financialHealth == FinancialHealthState.Bankrupt;

    public void AddTransaction(FinanceTransaction transaction)
    {
        if (transactions == null) transactions = new List<FinanceTransaction>();
        if (transactions.Count >= MaxTransactions)
            transactions.RemoveAt(0);
        transactions.Add(transaction);
    }

    public static FinanceState CreateNew(int startingMoney = 100000)
    {
        return new FinanceState
        {
            money = startingMoney,
            transactions = new List<FinanceTransaction>(),
            recurringCosts = new List<RecurringCostEntry>(),
            consecutiveDaysNegativeCash = 0,
            missedObligationCount = 0,
            financialHealth = FinancialHealthState.Stable
        };
    }
}
