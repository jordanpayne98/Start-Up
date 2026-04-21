using System.Collections.Generic;
using UnityEngine.UIElements;

public class FinanceView : IGameView
{
    private readonly ICommandDispatcher _dispatcher;
    private readonly IModalPresenter _modal;
    private readonly ITooltipProvider _tooltipProvider;
    private VisualElement _root;
    private Label _balanceLabel;
    private Label _revenueLabel;
    private Label _expensesLabel;
    private Label _netLabel;
    private Label _financialHealthLabel;
    private Label _runwayLabel;
    private Label _dailyObligationsLabel;
    private VisualElement _balanceRow;
    private VisualElement _revenueRow;
    private VisualElement _expensesRow;
    private VisualElement _netRow;
    private VisualElement _financialHealthRow;
    private VisualElement _runwayRow;
    private VisualElement _dailyObligationsRow;
    private VisualElement _expenseBreakdown;
    private ElementPool _expensePool;

    // Product revenue card
    private Label _totalMonthlyProductRevenueLabel;
    private Label _totalLifetimeProductRevenueLabel;
    private VisualElement _productRevenueListContainer;
    private ElementPool _productRevenuePool;

    // Investments card
    private VisualElement _investmentsCard;
    private Label _portfolioValueLabel;
    private Label _dividendIncomeLabel;
    private Label _productSaleProceedsLabel;
    private VisualElement _productSaleRow;

    // Bankruptcy warning card
    private VisualElement _bankruptcyWarningCard;
    private Label _bankruptcyCashLabel;
    private Label _bankruptcyBurnLabel;
    private Label _bankruptcyMonthsLabel;
    private Button _takeEmergencyLoanBtn;
    private Button _sellStockBtn;

    // Single active loan card
    private VisualElement _activeLoanCard;
    private Label _loanPrincipalLabel;
    private Label _loanRateLabel;
    private Label _loanRemainingLabel;
    private Label _loanMonthlyLabel;
    private Label _loanMonthsLabel;
    private Label _loanRiskLabel;
    private VisualElement _loanProgressFill;
    private Button _repayEarlyButton;
    private Label _earlyRepayFeedbackLabel;

    // Loan info header
    private Label _totalDebtLabel;
    private Button _takeLoanButton;
    private Label _loanInfoLabel;

    // Tax card
    private VisualElement _taxCard;
    private Label _taxRateLabel;
    private Label _taxProfitLabel;
    private Label _taxEstimatedLabel;
    private Label _taxDueDateLabel;
    private Label _taxDaysUntilDueLabel;
    private Label _taxNextCycleLabel;
    private VisualElement _taxOverdueSection;
    private Label _taxPendingLabel;
    private Label _taxLateFeesLabel;
    private Label _taxTotalOwedLabel;
    private Label _taxOverdueStatusLabel;
    private VisualElement _taxBankruptcyWarning;
    private Button _taxPayButton;

    private FinanceViewModel _viewModel;

    public FinanceView(ICommandDispatcher dispatcher, IModalPresenter modal, ITooltipProvider tooltipProvider) {
        _dispatcher = dispatcher;
        _modal = modal;
        _tooltipProvider = tooltipProvider;
    }

    public void Initialize(VisualElement root) {
        _root = root;

        var title = new Label("Finance");
        title.AddToClassList("section-header");
        _root.Add(title);

        var layout = new VisualElement();
        layout.AddToClassList("flex-row");
        layout.style.flexGrow = 1;

        // Left: Summary + Financial Health + Expenses
        var leftPanel = new VisualElement();
        leftPanel.style.flexGrow = 1;
        leftPanel.style.flexBasis = 0;
        leftPanel.style.marginRight = 16;

        // Summary card
        var summaryCard = new VisualElement();
        summaryCard.AddToClassList("card");
        summaryCard.style.marginBottom = 16;

        var ts = _tooltipProvider.TooltipService;
        (_balanceRow,  _balanceLabel)  = CreateFinanceRow(summaryCard, "Balance");
        (_revenueRow,  _revenueLabel)  = CreateFinanceRow(summaryCard, "Total Revenue");
        (_expensesRow, _expensesLabel) = CreateFinanceRow(summaryCard, "Monthly Expenses");

        _balanceRow.SetRichTooltip("topbar.balance", ts);
        _revenueRow.SetRichTooltip("finance.revenue", ts);
        _expensesRow.SetRichTooltip("finance.expenses", ts);

        var divider = new VisualElement();
        divider.AddToClassList("divider");
        summaryCard.Add(divider);

        (_netRow, _netLabel) = CreateFinanceRow(summaryCard, "Net Income");
        _netRow.SetRichTooltip("topbar.net-income", ts);

        var divider2 = new VisualElement();
        divider2.AddToClassList("divider");
        summaryCard.Add(divider2);

        (_financialHealthRow, _financialHealthLabel) = CreateFinanceRow(summaryCard, "Financial Health");
        (_runwayRow,          _runwayLabel)           = CreateFinanceRow(summaryCard, "Cash Remaining");
        (_dailyObligationsRow, _dailyObligationsLabel) = CreateFinanceRow(summaryCard, "Daily Obligations");

        _financialHealthRow.SetSimpleTooltip("Overall company financial stability score.", ts);
        _runwayRow.SetSimpleTooltip("Days of cash remaining at current burn rate before funds run out.", ts);
        _dailyObligationsRow.SetSimpleTooltip("Total daily recurring costs including salary and loan payments.", ts);

        leftPanel.Add(summaryCard);

        // Expense breakdown card
        var expCard = new VisualElement();
        expCard.AddToClassList("card");

        var expTitle = new Label("Expense Breakdown");
        expTitle.AddToClassList("text-bold");
        expTitle.style.marginBottom = 8;
        expCard.Add(expTitle);

        _expenseBreakdown = new VisualElement();
        _expensePool = new ElementPool(CreateExpenseRow, _expenseBreakdown);
        expCard.Add(_expenseBreakdown);

        leftPanel.Add(expCard);

        // Product Revenue card
        var productRevCard = new VisualElement();
        productRevCard.AddToClassList("card");
        productRevCard.style.marginTop = 16;

        var productRevTitle = new Label("Product Revenue");
        productRevTitle.AddToClassList("text-bold");
        productRevTitle.style.marginBottom = 8;
        productRevCard.Add(productRevTitle);

        (_, _totalMonthlyProductRevenueLabel) = CreateFinanceRow(productRevCard, "Monthly Revenue");
        (_, _totalLifetimeProductRevenueLabel) = CreateFinanceRow(productRevCard, "Total Lifetime Revenue");

        var productRevDivider = new VisualElement();
        productRevDivider.AddToClassList("divider");
        productRevCard.Add(productRevDivider);

        _productRevenueListContainer = new VisualElement();
        _productRevenuePool = new ElementPool(CreateProductRevenueRow, _productRevenueListContainer);
        productRevCard.Add(_productRevenueListContainer);

        leftPanel.Add(productRevCard);

        // Investments card (stock portfolio + dividends)
        _investmentsCard = new VisualElement();
        _investmentsCard.AddToClassList("card");
        _investmentsCard.style.marginTop = 16;
        _investmentsCard.style.display = DisplayStyle.None;

        var investTitle = new Label("Investments");
        investTitle.AddToClassList("text-bold");
        investTitle.style.marginBottom = 8;
        _investmentsCard.Add(investTitle);

        (_, _portfolioValueLabel)  = CreateFinanceRow(_investmentsCard, "Portfolio Value");
        (_, _dividendIncomeLabel)  = CreateFinanceRow(_investmentsCard, "Dividend Income");

        _productSaleRow = new VisualElement();
        _productSaleRow.AddToClassList("flex-row");
        _productSaleRow.AddToClassList("justify-between");
        _productSaleRow.style.marginBottom = 4;
        _productSaleRow.style.display = DisplayStyle.None;

        var saleLabel = new Label("Product Sale Proceeds");
        saleLabel.AddToClassList("text-muted");
        _productSaleRow.Add(saleLabel);

        _productSaleProceedsLabel = new Label("$0");
        _productSaleProceedsLabel.AddToClassList("text-bold");
        _productSaleProceedsLabel.AddToClassList("text-success");
        _productSaleRow.Add(_productSaleProceedsLabel);

        _investmentsCard.Add(_productSaleRow);
        leftPanel.Add(_investmentsCard);

        // Bankruptcy warning card
        _bankruptcyWarningCard = new VisualElement();
        _bankruptcyWarningCard.AddToClassList("card");
        _bankruptcyWarningCard.AddToClassList("card--danger");
        _bankruptcyWarningCard.style.marginTop = 16;
        _bankruptcyWarningCard.style.display = DisplayStyle.None;

        var warnTitle = new Label("Bankruptcy Warning");
        warnTitle.AddToClassList("text-bold");
        warnTitle.AddToClassList("text-danger");
        warnTitle.style.marginBottom = 8;
        _bankruptcyWarningCard.Add(warnTitle);

        (_, _bankruptcyCashLabel)   = CreateFinanceRow(_bankruptcyWarningCard, "Current Cash");
        (_, _bankruptcyBurnLabel)   = CreateFinanceRow(_bankruptcyWarningCard, "Monthly Burn Rate");
        (_, _bankruptcyMonthsLabel) = CreateFinanceRow(_bankruptcyWarningCard, "Months Remaining");

        var lifelineRow = new VisualElement();
        lifelineRow.AddToClassList("flex-row");
        lifelineRow.style.marginTop = 8;

        _takeEmergencyLoanBtn = new Button { text = "Emergency Loan" };
        _takeEmergencyLoanBtn.AddToClassList("btn-warning");
        _takeEmergencyLoanBtn.AddToClassList("btn-sm");
        _takeEmergencyLoanBtn.style.marginRight = 8;
        _takeEmergencyLoanBtn.clicked += OnTakeLoanClicked;
        lifelineRow.Add(_takeEmergencyLoanBtn);

        _sellStockBtn = new Button { text = "Sell Stock" };
        _sellStockBtn.AddToClassList("btn-secondary");
        _sellStockBtn.AddToClassList("btn-sm");
        _sellStockBtn.style.marginRight = 8;
        _sellStockBtn.clicked += OnSellStockClicked;
        lifelineRow.Add(_sellStockBtn);

        _bankruptcyWarningCard.Add(lifelineRow);
        leftPanel.Add(_bankruptcyWarningCard);

        layout.Add(leftPanel);

        // Right: Loan panel
        var rightPanel = new VisualElement();
        rightPanel.style.flexGrow = 1;
        rightPanel.style.flexBasis = 0;

        var loanCard = new VisualElement();
        loanCard.AddToClassList("card");

        var loanHeader = new VisualElement();
        loanHeader.AddToClassList("flex-row");
        loanHeader.AddToClassList("justify-between");
        loanHeader.AddToClassList("align-center");
        loanHeader.style.marginBottom = 8;

        var loanTitle = new Label("Loans");
        loanTitle.AddToClassList("text-bold");
        loanHeader.Add(loanTitle);

        _takeLoanButton = new Button { text = "Take Loan" };
        _takeLoanButton.AddToClassList("btn-primary");
        _takeLoanButton.AddToClassList("btn-sm");
        _takeLoanButton.clicked += OnTakeLoanClicked;
        loanHeader.Add(_takeLoanButton);

        loanCard.Add(loanHeader);

        _loanInfoLabel = new Label();
        _loanInfoLabel.AddToClassList("text-sm");
        _loanInfoLabel.AddToClassList("text-muted");
        _loanInfoLabel.style.marginBottom = 8;
        loanCard.Add(_loanInfoLabel);

        _totalDebtLabel = new Label();
        _totalDebtLabel.style.marginBottom = 12;
        loanCard.Add(_totalDebtLabel);

        // Active loan card
        _activeLoanCard = new VisualElement();
        _activeLoanCard.AddToClassList("card");
        _activeLoanCard.style.marginBottom = 8;
        _activeLoanCard.style.backgroundColor = new StyleColor(new UnityEngine.Color(0.12f, 0.16f, 0.14f, 1f));
        _activeLoanCard.style.display = DisplayStyle.None;

        var loanTopRow = new VisualElement();
        loanTopRow.AddToClassList("flex-row");
        loanTopRow.AddToClassList("justify-between");

        _loanPrincipalLabel = new Label();
        _loanPrincipalLabel.AddToClassList("text-bold");
        loanTopRow.Add(_loanPrincipalLabel);

        _loanRateLabel = new Label();
        _loanRateLabel.AddToClassList("text-sm");
        _loanRateLabel.AddToClassList("text-muted");
        loanTopRow.Add(_loanRateLabel);
        _activeLoanCard.Add(loanTopRow);

        var loanMidRow = new VisualElement();
        loanMidRow.AddToClassList("flex-row");
        loanMidRow.AddToClassList("justify-between");
        loanMidRow.style.marginTop = 4;

        _loanRemainingLabel = new Label();
        _loanRemainingLabel.AddToClassList("text-sm");
        loanMidRow.Add(_loanRemainingLabel);

        _loanMonthlyLabel = new Label();
        _loanMonthlyLabel.AddToClassList("text-sm");
        _loanMonthlyLabel.AddToClassList("text-muted");
        loanMidRow.Add(_loanMonthlyLabel);
        _activeLoanCard.Add(loanMidRow);

        var loanBottomRow = new VisualElement();
        loanBottomRow.AddToClassList("flex-row");
        loanBottomRow.AddToClassList("justify-between");
        loanBottomRow.style.marginTop = 4;

        _loanMonthsLabel = new Label();
        _loanMonthsLabel.AddToClassList("text-sm");
        loanBottomRow.Add(_loanMonthsLabel);

        _loanRiskLabel = new Label();
        _loanRiskLabel.AddToClassList("text-sm");
        loanBottomRow.Add(_loanRiskLabel);
        _activeLoanCard.Add(loanBottomRow);

        var progressBar = new VisualElement();
        progressBar.AddToClassList("progress-bar");
        progressBar.style.marginTop = 4;
        _loanProgressFill = new VisualElement();
        _loanProgressFill.AddToClassList("progress-bar__fill");
        _loanProgressFill.AddToClassList("progress-bar__fill--success");
        progressBar.Add(_loanProgressFill);
        _activeLoanCard.Add(progressBar);

        _repayEarlyButton = new Button { text = "Repay Early (Full)" };
        _repayEarlyButton.AddToClassList("btn-secondary");
        _repayEarlyButton.AddToClassList("btn-sm");
        _repayEarlyButton.style.marginTop = 8;
        _repayEarlyButton.clicked += OnRepayEarlyClicked;
        _activeLoanCard.Add(_repayEarlyButton);

        _earlyRepayFeedbackLabel = new Label();
        _earlyRepayFeedbackLabel.AddToClassList("text-sm");
        _earlyRepayFeedbackLabel.AddToClassList("text-success");
        _earlyRepayFeedbackLabel.style.marginTop = 4;
        _earlyRepayFeedbackLabel.style.display = DisplayStyle.None;
        _activeLoanCard.Add(_earlyRepayFeedbackLabel);

        loanCard.Add(_activeLoanCard);
        rightPanel.Add(loanCard);

        // Tax card
        _taxCard = new VisualElement();
        _taxCard.AddToClassList("card");
        _taxCard.style.marginTop = 16;

        var taxTitle = new Label("Tax Report");
        taxTitle.AddToClassList("text-bold");
        taxTitle.style.marginBottom = 8;
        _taxCard.Add(taxTitle);

        (_, _taxRateLabel)         = CreateFinanceRow(_taxCard, "Tax Rate");
        (_, _taxProfitLabel)       = CreateFinanceRow(_taxCard, "Accumulated Profit");
        (_, _taxEstimatedLabel)    = CreateFinanceRow(_taxCard, "Estimated Tax");
        (_, _taxDueDateLabel)      = CreateFinanceRow(_taxCard, "Due Date");
        (_, _taxDaysUntilDueLabel) = CreateFinanceRow(_taxCard, "Days Until Due");
        (_, _taxNextCycleLabel)    = CreateFinanceRow(_taxCard, "Next Cycle Estimate");

        var taxDivider = new VisualElement();
        taxDivider.AddToClassList("divider");
        _taxCard.Add(taxDivider);

        _taxOverdueSection = new VisualElement();
        _taxOverdueSection.AddToClassList("hidden");

        var overdueTitle = new Label("Overdue Details");
        overdueTitle.AddToClassList("text-sm");
        overdueTitle.AddToClassList("text-danger");
        overdueTitle.style.marginBottom = 4;
        _taxOverdueSection.Add(overdueTitle);

        (_, _taxPendingLabel)      = CreateFinanceRow(_taxOverdueSection, "Pending Tax");
        (_, _taxLateFeesLabel)     = CreateFinanceRow(_taxOverdueSection, "Late Fees");
        (_, _taxTotalOwedLabel)    = CreateFinanceRow(_taxOverdueSection, "Total Owed");
        (_, _taxOverdueStatusLabel) = CreateFinanceRow(_taxOverdueSection, "Status");

        _taxCard.Add(_taxOverdueSection);

        _taxBankruptcyWarning = new VisualElement();
        _taxBankruptcyWarning.AddToClassList("hidden");
        var taxWarnLabel = new Label("⚠ Tax debt may cause bankruptcy");
        taxWarnLabel.AddToClassList("text-sm");
        taxWarnLabel.AddToClassList("text-danger");
        _taxBankruptcyWarning.Add(taxWarnLabel);
        _taxCard.Add(_taxBankruptcyWarning);

        _taxPayButton = new Button { text = "Pay Tax" };
        _taxPayButton.AddToClassList("btn-primary");
        _taxPayButton.AddToClassList("btn-sm");
        _taxPayButton.AddToClassList("hidden");
        _taxPayButton.style.marginTop = 8;
        _taxPayButton.clicked += OnPayTaxClicked;
        _taxCard.Add(_taxPayButton);

        rightPanel.Add(_taxCard);
        layout.Add(rightPanel);

        _root.Add(layout);
    }

    public void Bind(IViewModel viewModel) {
        _viewModel = viewModel as FinanceViewModel;
        if (_viewModel == null) return;

        _balanceLabel.text = _viewModel.MoneyDisplay;
        _revenueLabel.text = _viewModel.MonthlyRevenueDisplay;
        _expensesLabel.text = _viewModel.MonthlyExpensesDisplay;
        _netLabel.text = _viewModel.NetIncomeDisplay;

        _netLabel.RemoveFromClassList("text-success");
        _netLabel.RemoveFromClassList("text-danger");
        _netLabel.AddToClassList(_viewModel.IsNetPositive ? "text-success" : "text-danger");

        // Financial health
        _financialHealthLabel.text = _viewModel.FinancialHealthDisplay;
        _financialHealthLabel.RemoveFromClassList("health-stable");
        _financialHealthLabel.RemoveFromClassList("health-tight");
        _financialHealthLabel.RemoveFromClassList("health-distressed");
        _financialHealthLabel.RemoveFromClassList("health-insolvent");
        _financialHealthLabel.RemoveFromClassList("health-bankrupt");
        _financialHealthLabel.AddToClassList(_viewModel.FinancialHealthClass);

        _runwayLabel.text = _viewModel.RunwayDisplay;
        _runwayLabel.RemoveFromClassList("finance-runway-warning");
        _runwayLabel.RemoveFromClassList("finance-runway-critical");
        if (_viewModel.IsRunwayWarning)
            _runwayLabel.AddToClassList(_viewModel.RunwayWarningClass);
        _dailyObligationsLabel.text = _viewModel.DailyObligationsDisplay;

        // Loan header
        _takeLoanButton.SetEnabled(_viewModel.CanTakeLoan);
        _loanInfoLabel.text = "Max: " + _viewModel.MaxLoanDisplay + " | Rate: " + _viewModel.LoanInterestDisplay;
        _totalDebtLabel.text = "Total Debt: " + _viewModel.TotalDebtDisplay;

        _expensePool.UpdateList(_viewModel.Expenses, BindExpenseRow);

        _totalMonthlyProductRevenueLabel.text = _viewModel.TotalProductMonthlyRevenueDisplay;
        _totalLifetimeProductRevenueLabel.text = _viewModel.TotalProductLifetimeRevenueDisplay;
        _productRevenuePool.UpdateList(_viewModel.ProductRevenues, BindProductRevenueRow);

        // Investments card
        if (_investmentsCard != null) {
            bool showInvest = _viewModel.HasStockPortfolio;
            _investmentsCard.style.display = showInvest ? DisplayStyle.Flex : DisplayStyle.None;
            if (showInvest) {
                _portfolioValueLabel.text = _viewModel.StockPortfolioValue;
                _dividendIncomeLabel.text = _viewModel.DividendIncome;
            }
            if (_productSaleRow != null) {
                _productSaleRow.style.display = _viewModel.HasProductSaleProceeds ? DisplayStyle.Flex : DisplayStyle.None;
                if (_viewModel.HasProductSaleProceeds && _productSaleProceedsLabel != null)
                    _productSaleProceedsLabel.text = _viewModel.ProductSaleProceeds;
            }
        }

        // Bankruptcy warning card
        if (_bankruptcyWarningCard != null) {
            _bankruptcyWarningCard.style.display = _viewModel.IsBankruptcyWarning ? DisplayStyle.Flex : DisplayStyle.None;
            if (_viewModel.IsBankruptcyWarning) {
                var bvm = _viewModel.BankruptcyVM;
                if (_bankruptcyCashLabel != null)   _bankruptcyCashLabel.text   = bvm.CurrentCash;
                if (_bankruptcyBurnLabel != null)   _bankruptcyBurnLabel.text   = bvm.MonthlyBurnRate;
                if (_bankruptcyMonthsLabel != null) _bankruptcyMonthsLabel.text = bvm.MonthsRemaining + " months";
                if (_sellStockBtn != null)   _sellStockBtn.SetEnabled(bvm.HasStockToSell);
                if (_takeEmergencyLoanBtn != null) _takeEmergencyLoanBtn.SetEnabled(bvm.CanTakeLoan);
            }
        }

        // Active loan card
        if (_viewModel.HasActiveLoan)
        {
            _activeLoanCard.style.display = DisplayStyle.Flex;
            var l = _viewModel.ActiveLoanDisplay;
            _loanPrincipalLabel.text = "Loan: " + l.PrincipalDisplay;
            _loanRateLabel.text = l.InterestRate;
            _loanRemainingLabel.text = "Remaining: " + l.RemainingDisplay;
            _loanMonthlyLabel.text = l.MonthlyPaymentDisplay + "/mo";
            _loanMonthsLabel.text = l.RemainingMonthsDisplay + " left";

            _loanRiskLabel.text = l.RiskBandDisplay;
            _loanRiskLabel.RemoveFromClassList("risk-safe");
            _loanRiskLabel.RemoveFromClassList("risk-standard");
            _loanRiskLabel.RemoveFromClassList("risk-aggressive");
            _loanRiskLabel.RemoveFromClassList("risk-extreme");
            _loanRiskLabel.AddToClassList(l.RiskBandClass);

            if (_loanProgressFill != null)
                _loanProgressFill.style.width = Length.Percent(l.RepayPercent * 100f);
        }
        else
        {
            _activeLoanCard.style.display = DisplayStyle.None;
            _earlyRepayFeedbackLabel.style.display = DisplayStyle.None;
        }

        // Tax card
        var tax = _viewModel.TaxVM;
        if (_taxRateLabel != null)         _taxRateLabel.text         = tax.TaxRate;
        if (_taxProfitLabel != null)       _taxProfitLabel.text       = tax.AccumulatedProfit;
        if (_taxEstimatedLabel != null)    _taxEstimatedLabel.text    = tax.EstimatedTaxOwed;
        if (_taxDueDateLabel != null)      _taxDueDateLabel.text      = tax.DueDate;
        if (_taxDaysUntilDueLabel != null) _taxDaysUntilDueLabel.text = tax.DaysUntilDue;
        if (_taxNextCycleLabel != null)    _taxNextCycleLabel.text    = tax.NextCycleEstimate;
        if (_taxPendingLabel != null)      _taxPendingLabel.text      = tax.PendingTaxAmount;
        if (_taxLateFeesLabel != null)     _taxLateFeesLabel.text     = tax.PendingLateFees;
        if (_taxTotalOwedLabel != null)    _taxTotalOwedLabel.text    = tax.TotalOwed;
        if (_taxOverdueStatusLabel != null) _taxOverdueStatusLabel.text = tax.OverdueStatus;

        if (_taxOverdueSection != null)
        {
            if (tax.ShowOverdueSection)
                _taxOverdueSection.RemoveFromClassList("hidden");
            else
                _taxOverdueSection.AddToClassList("hidden");
        }
        if (_taxBankruptcyWarning != null)
        {
            if (tax.ShowBankruptcyWarning)
                _taxBankruptcyWarning.RemoveFromClassList("hidden");
            else
                _taxBankruptcyWarning.AddToClassList("hidden");
        }
        if (_taxPayButton != null)
        {
            if (tax.ShowPayButton)
                _taxPayButton.RemoveFromClassList("hidden");
            else
                _taxPayButton.AddToClassList("hidden");
        }
    }

    public void Dispose() {
        var ts = _tooltipProvider.TooltipService;
        _balanceRow?.ClearTooltip(ts);
        _revenueRow?.ClearTooltip(ts);
        _expensesRow?.ClearTooltip(ts);
        _netRow?.ClearTooltip(ts);
        _financialHealthRow?.ClearTooltip(ts);
        _runwayRow?.ClearTooltip(ts);
        _dailyObligationsRow?.ClearTooltip(ts);

        if (_takeLoanButton != null) _takeLoanButton.clicked -= OnTakeLoanClicked;
        if (_repayEarlyButton != null) _repayEarlyButton.clicked -= OnRepayEarlyClicked;
        if (_takeEmergencyLoanBtn != null) _takeEmergencyLoanBtn.clicked -= OnTakeLoanClicked;
        if (_sellStockBtn != null) _sellStockBtn.clicked -= OnSellStockClicked;
        if (_taxPayButton != null) _taxPayButton.clicked -= OnPayTaxClicked;

        _viewModel = null;
        _expensePool = null;
        _productRevenuePool = null;
        _investmentsCard = null;
        _portfolioValueLabel = null;
        _dividendIncomeLabel = null;
        _productSaleProceedsLabel = null;
        _productSaleRow = null;
        _bankruptcyWarningCard = null;
        _bankruptcyCashLabel = null;
        _bankruptcyBurnLabel = null;
        _bankruptcyMonthsLabel = null;
        _takeEmergencyLoanBtn = null;
        _sellStockBtn = null;
        _taxCard = null;
        _taxRateLabel = null;
        _taxProfitLabel = null;
        _taxEstimatedLabel = null;
        _taxDueDateLabel = null;
        _taxDaysUntilDueLabel = null;
        _taxNextCycleLabel = null;
        _taxOverdueSection = null;
        _taxPendingLabel = null;
        _taxLateFeesLabel = null;
        _taxTotalOwedLabel = null;
        _taxOverdueStatusLabel = null;
        _taxBankruptcyWarning = null;
        _taxPayButton = null;
    }

    private void OnTakeLoanClicked() {
        var vm = new LoanApplicationViewModel();
        _modal.ShowModal(new LoanApplicationView(), vm);
    }

    private void OnRepayEarlyClicked() {
        if (_viewModel == null || !_viewModel.HasActiveLoan) return;
        var cmd = new RepayLoanEarlyCommand(_dispatcher.CurrentTick, int.MaxValue);
        _dispatcher.Dispatch(cmd);
        _earlyRepayFeedbackLabel.text = "Early repayment submitted!";
        _earlyRepayFeedbackLabel.style.display = DisplayStyle.Flex;
    }

    private void OnSellStockClicked() { }

    private void OnPayTaxClicked() {
        if (_dispatcher == null) return;
        _dispatcher.Dispatch(new PayTaxCommand { Tick = _dispatcher.CurrentTick });
    }

    private (VisualElement row, Label value) CreateFinanceRow(VisualElement parent, string label) {
        var row = new VisualElement();
        row.AddToClassList("flex-row");
        row.AddToClassList("justify-between");
        row.style.marginBottom = 4;

        var labelEl = new Label(label);
        labelEl.AddToClassList("text-muted");
        row.Add(labelEl);

        var valueEl = new Label("--");
        valueEl.AddToClassList("text-bold");
        row.Add(valueEl);

        parent.Add(row);
        return (row, valueEl);
    }

    private VisualElement CreateExpenseRow() {
        var row = new VisualElement();
        row.AddToClassList("flex-row");
        row.AddToClassList("justify-between");
        row.style.marginBottom = 4;

        var label = new Label();
        label.name = "exp-label";
        label.AddToClassList("text-sm");
        row.Add(label);

        var amount = new Label();
        amount.name = "exp-amount";
        amount.AddToClassList("text-sm");
        row.Add(amount);

        return row;
    }

    private void BindExpenseRow(VisualElement el, ExpenseBreakdownItem data) {
        el.Q<Label>("exp-label").text = data.Label;
        el.Q<Label>("exp-amount").text = data.Amount;
    }

    private VisualElement CreateProductRevenueRow() {
        var row = new VisualElement();
        row.AddToClassList("flex-row");
        row.AddToClassList("justify-between");
        row.style.marginBottom = 4;

        var name = new Label();
        name.name = "prod-rev-name";
        name.AddToClassList("text-sm");
        name.style.flexGrow = 1;
        row.Add(name);

        var monthly = new Label();
        monthly.name = "prod-rev-monthly";
        monthly.AddToClassList("text-sm");
        monthly.style.marginRight = 8;
        row.Add(monthly);

        var total = new Label();
        total.name = "prod-rev-total";
        total.AddToClassList("text-sm");
        total.AddToClassList("text-muted");
        total.style.marginRight = 8;
        row.Add(total);

        var cost = new Label();
        cost.name = "prod-rev-cost";
        cost.AddToClassList("text-sm");
        cost.AddToClassList("text-muted");
        cost.style.marginRight = 8;
        row.Add(cost);

        var net = new Label();
        net.name = "prod-rev-net";
        net.AddToClassList("text-sm");
        row.Add(net);

        return row;
    }

    private void BindProductRevenueRow(VisualElement el, ProductRevenueItem data) {
        el.Q<Label>("prod-rev-name").text = data.Name;
        el.Q<Label>("prod-rev-monthly").text = data.MonthlyRevenue + "/mo";
        el.Q<Label>("prod-rev-total").text = data.TotalRevenue + " total";
        el.Q<Label>("prod-rev-cost").text = data.ProductionCost + " cost";
        el.Q<Label>("prod-rev-net").text = data.NetProfit + " net";
    }
}
