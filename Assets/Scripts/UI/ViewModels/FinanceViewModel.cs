using System.Collections.Generic;

public struct LoanDisplay
{
    public string PrincipalDisplay;
    public string InterestRate;
    public string TotalOwedDisplay;
    public string RemainingDisplay;
    public string MonthlyPaymentDisplay;
    public string RemainingMonthsDisplay;
    public string RiskBandDisplay;
    public string RiskBandClass;
    public float RepayPercent;
}

public struct ExpenseBreakdownItem
{
    public string Label;
    public string Amount;
}

public struct ProductRevenueItem
{
    public string Name;
    public string MonthlyRevenue;
    public string TotalRevenue;
    public string ProductionCost;
    public string NetProfit;
}

public class FinanceViewModel : IViewModel
{
    public string MoneyDisplay { get; private set; }
    public string MonthlyRevenueDisplay { get; private set; }
    public string MonthlyExpensesDisplay { get; private set; }
    public string NetIncomeDisplay { get; private set; }
    public bool IsNetPositive { get; private set; }

    // Financial health
    public string FinancialHealthDisplay { get; private set; }
    public string FinancialHealthClass { get; private set; }
    public string RunwayDisplay { get; private set; }
    public string DailyObligationsDisplay { get; private set; }

    // Credit score
    public int CreditScore { get; private set; }
    public string CreditScoreDisplay { get; private set; }   // "68 / 100"
    public string CreditTierDisplay { get; private set; }    // "Good"
    public string CreditTierClass { get; private set; }      // USS class

    // Expense breakdown
    private readonly List<ExpenseBreakdownItem> _expenses = new List<ExpenseBreakdownItem>();
    public List<ExpenseBreakdownItem> Expenses => _expenses;

    // Product revenue breakdown
    public string TotalProductMonthlyRevenueDisplay { get; private set; }
    public string TotalProductLifetimeRevenueDisplay { get; private set; }
    private readonly List<ProductRevenueItem> _productRevenues = new List<ProductRevenueItem>();
    public List<ProductRevenueItem> ProductRevenues => _productRevenues;

    // Single active loan
    public bool HasActiveLoan { get; private set; }
    public LoanDisplay ActiveLoanDisplay { get; private set; }

    public bool CanTakeLoan { get; private set; }
    public string MaxLoanDisplay { get; private set; }
    public string LoanInterestDisplay { get; private set; }
    public string TotalDebtDisplay { get; private set; }
    public int DaysInDebt { get; private set; }

    // Stock portfolio (from StockState)
    public bool HasStockPortfolio { get; private set; }
    public string StockPortfolioValue { get; private set; }
    public string DividendIncome { get; private set; }

    // Product sale proceeds (from StockState or ProductState: player sold a product this period)
    public string ProductSaleProceeds { get; private set; }
    public bool HasProductSaleProceeds { get; private set; }

    // Bankruptcy warning
    public bool IsBankruptcyWarning { get; private set; }
    public BankruptcyWarningViewModel BankruptcyVM { get; private set; }

    // Runway warning (3-month threshold based on total burn including product budgets)
    public bool IsRunwayWarning { get; private set; }
    public string RunwayWarningClass { get; private set; }

    // Tax report (composed sub-VM)
    public TaxReportViewModel TaxVM { get; private set; }

    public FinanceViewModel() {
        MoneyDisplay = "$0";
        MonthlyRevenueDisplay = "$0";
        MonthlyExpensesDisplay = "$0";
        NetIncomeDisplay = "$0";
        MaxLoanDisplay = "$0";
        LoanInterestDisplay = "0%";
        TotalDebtDisplay = "$0";
        FinancialHealthDisplay = "Stable";
        FinancialHealthClass = "health-stable";
        RunwayDisplay = "--";
        DailyObligationsDisplay = "$0/day";
        CreditScore = 40;
        CreditScoreDisplay = "40 / 100";
        CreditTierDisplay = "Fair";
        CreditTierClass = "credit-fair";
        TotalProductMonthlyRevenueDisplay = "$0";
        TotalProductLifetimeRevenueDisplay = "$0";
        StockPortfolioValue = "$0";
        DividendIncome = "$0";
        ProductSaleProceeds = "$0";
        BankruptcyVM = new BankruptcyWarningViewModel();
        TaxVM = new TaxReportViewModel();
        RunwayWarningClass = "";
    }

    public bool IsDirty { get; private set; }
    public void ClearDirty() => IsDirty = false;

    public void Refresh(GameStateSnapshot snapshot) {
        if (snapshot == null) return;
        RefreshWithStocks(snapshot, snapshot.StockState, null);
        IsDirty = true;
    }

    public void RefreshWithStocks(IReadOnlyGameState state, StockState stockState, FinanceState financeState) {
        if (state == null) return;

        MoneyDisplay = UIFormatting.FormatMoney(state.Money);
        MonthlyRevenueDisplay = UIFormatting.FormatMoney(state.TotalRevenue);
        MonthlyExpensesDisplay = UIFormatting.FormatMoney(state.MonthlyExpenses);

        long net = state.TotalRevenue - state.MonthlyExpenses;
        IsNetPositive = net >= 0;
        NetIncomeDisplay = UIFormatting.FormatMoney(net);

        // Financial health
        FinancialHealthDisplay = state.FinancialHealth.ToString();
        FinancialHealthClass = HealthToClass(state.FinancialHealth);
        DaysInDebt = state.DaysInDebt;

        int runway = state.RunwayDays;
        RunwayDisplay = runway == int.MaxValue ? "Infinite" : runway + " days";
        DailyObligationsDisplay = UIFormatting.FormatMoney(state.DailyObligations) + "/day";

        // Credit score
        CreditScore = state.CreditScore;
        CreditScoreDisplay = state.CreditScore + " / 100";
        CreditTierDisplay = state.CreditTier.ToString();
        CreditTierClass = CreditTierToClass(state.CreditTier);

        // Expense breakdown — salaries + product budgets + loan repayments
        _expenses.Clear();
        long totalProductBudgets = 0L;
        if (state.ShippedProducts != null) {
            foreach (var kvp in state.ShippedProducts) {
                var product = kvp.Value;
                if (product.IsCompetitorProduct) continue;
                totalProductBudgets += product.MaintenanceBudgetMonthly + product.MarketingBudgetMonthly;
            }
        }
        long salaryObligation = state.TotalSalaryCost;
        _expenses.Add(new ExpenseBreakdownItem { Label = "Salaries (est. monthly)", Amount = UIFormatting.FormatMoney(salaryObligation) });
        if (totalProductBudgets > 0) {
            _expenses.Add(new ExpenseBreakdownItem { Label = "Product Budgets/mo", Amount = UIFormatting.FormatMoney(totalProductBudgets) });
        }
        _expenses.Add(new ExpenseBreakdownItem { Label = "Loan Repayments/mo", Amount = UIFormatting.FormatMoney(state.LoanRepaymentCost) });
        long other = state.MonthlyExpenses - salaryObligation - totalProductBudgets - state.LoanRepaymentCost;
        if (other > 0) {
            _expenses.Add(new ExpenseBreakdownItem { Label = "Other/mo", Amount = UIFormatting.FormatMoney(other) });
        }

        // Runway warning — 3-month threshold based on total monthly burn
        long totalMonthlyBurn = state.MonthlyExpenses;
        if (totalMonthlyBurn > 0) {
            float monthsOfRunway = (float)state.Money / totalMonthlyBurn;
            IsRunwayWarning = monthsOfRunway < 3f && state.Money > 0;
            RunwayWarningClass = monthsOfRunway < 1f ? "finance-runway-critical" : "finance-runway-warning";
        } else {
            IsRunwayWarning = false;
            RunwayWarningClass = "";
        }

        // Product revenue breakdown
        _productRevenues.Clear();
        long monthlyTotal = 0;
        long lifetimeTotal = 0;
        if (state.ShippedProducts != null)
        {
            foreach (var kvp in state.ShippedProducts)
            {
                var product = kvp.Value;
                if (product.IsCompetitorProduct) continue;
                long monthly = product.MonthlyRevenue;
                long lifetime = product.TotalLifetimeRevenue;
                long cost = state.GetProductTotalProductionCost(kvp.Key);
                long netProfit = lifetime - cost;
                monthlyTotal += monthly;
                lifetimeTotal += lifetime;
                _productRevenues.Add(new ProductRevenueItem {
                    Name = product.ProductName,
                    MonthlyRevenue = UIFormatting.FormatMoney(monthly),
                    TotalRevenue = UIFormatting.FormatMoney(lifetime),
                    ProductionCost = UIFormatting.FormatMoney(cost),
                    NetProfit = UIFormatting.FormatMoney(netProfit)
                });
            }
        }
        TotalProductMonthlyRevenueDisplay = UIFormatting.FormatMoney(monthlyTotal);
        TotalProductLifetimeRevenueDisplay = UIFormatting.FormatMoney(lifetimeTotal);

        // Single active loan
        CanTakeLoan = state.CanTakeLoan;
        MaxLoanDisplay = UIFormatting.FormatMoney(state.MaxLoanAmount);
        LoanInterestDisplay = UIFormatting.FormatPercent(state.LoanInterestRate);
        TotalDebtDisplay = UIFormatting.FormatMoney(state.TotalLoanDebt);

        var loan = state.ActiveLoan;
        HasActiveLoan = loan.HasValue;
        if (loan.HasValue)
        {
            var l = loan.Value;
            float repayPercent = l.totalOwed > 0 ? 1f - ((float)l.remainingOwed / l.totalOwed) : 1f;
            ActiveLoanDisplay = new LoanDisplay
            {
                PrincipalDisplay = UIFormatting.FormatMoney(l.principal),
                InterestRate = UIFormatting.FormatPercent(l.interestRate),
                TotalOwedDisplay = UIFormatting.FormatMoney(l.totalOwed),
                RemainingDisplay = UIFormatting.FormatMoney(l.remainingOwed),
                MonthlyPaymentDisplay = UIFormatting.FormatMoney(l.monthlyPayment),
                RemainingMonthsDisplay = l.remainingMonths + " months",
                RiskBandDisplay = l.riskBand.ToString(),
                RiskBandClass = RiskBandToClass(l.riskBand),
                RepayPercent = repayPercent
            };
        }

        // Stock portfolio
        HasStockPortfolio = false;
        long portfolioTotal = 0L;
        long dividendTotal = 0L;
        if (stockState?.holdings != null && stockState?.listings != null)
        {
            foreach (var kvp in stockState.holdings)
            {
                var holding = kvp.Value;
                if (!holding.IsPlayerOwned) continue;
                HasStockPortfolio = true;
                if (stockState.listings.TryGetValue(holding.TargetCompanyId, out var listing))
                {
                    portfolioTotal += (long)(listing.StockPrice * holding.PercentageOwned);
                    dividendTotal += listing.LastDividendPayout;
                }
            }
        }
        StockPortfolioValue = UIFormatting.FormatMoney(portfolioTotal);
        DividendIncome = UIFormatting.FormatMoney(dividendTotal) + "/yr";

        // Product sale proceeds
        HasProductSaleProceeds = false;
        ProductSaleProceeds = "$0";

        // Bankruptcy warning
        bool isDistressed = financeState != null
            && (financeState.financialHealth == FinancialHealthState.Distressed
                || financeState.financialHealth == FinancialHealthState.Insolvent);
        IsBankruptcyWarning = state.Money < 0 || isDistressed;
        if (IsBankruptcyWarning)
        {
            BankruptcyVM.Refresh(state as GameStateSnapshot);
        }

        TaxVM.Refresh(state as GameStateSnapshot);
    }

    private static string HealthToClass(FinancialHealthState health)
    {
        switch (health)
        {
            case FinancialHealthState.Stable:    return "health-stable";
            case FinancialHealthState.Tight:     return "health-tight";
            case FinancialHealthState.Distressed: return "health-distressed";
            case FinancialHealthState.Insolvent: return "health-insolvent";
            case FinancialHealthState.Bankrupt:  return "health-bankrupt";
            default:                             return "health-stable";
        }
    }

    private static string RiskBandToClass(LoanRiskBand band)
    {
        switch (band)
        {
            case LoanRiskBand.Safe:       return "risk-safe";
            case LoanRiskBand.Standard:   return "risk-standard";
            case LoanRiskBand.Aggressive: return "risk-aggressive";
            case LoanRiskBand.Extreme:    return "risk-extreme";
            default:                      return "risk-safe";
        }
    }

    private static string CreditTierToClass(CreditTier tier)
    {
        switch (tier)
        {
            case CreditTier.Poor:      return "credit-poor";
            case CreditTier.Fair:      return "credit-fair";
            case CreditTier.Good:      return "credit-good";
            case CreditTier.Excellent: return "credit-excellent";
            default:                   return "credit-fair";
        }
    }
}
