using System.Collections.Generic;
using UnityEngine.UIElements;

public class CompetitorProfileView : IGameView
{
    private readonly IModalPresenter _modal;
    private readonly ICommandDispatcher _dispatcher;
    private CompetitorProfileViewModel _vm;

    // Root
    private VisualElement _root;

    // Header
    private Label _companyNameLabel;
    private Label _reputationBadge;
    private Button _closeButton;

    // Tab buttons
    private Button _tabOverview;
    private Button _tabProducts;
    private Button _tabEmployees;
    private Button _tabFinance;

    // Panels
    private VisualElement _panelOverview;
    private VisualElement _panelProducts;
    private VisualElement _panelEmployees;
    private VisualElement _panelFinance;

    // Overview panel
    private Label _founderLabel;
    private Label _archetypeLabel;
    private Label _employeeCountLabel;
    private Label _productCountLabel;
    private Label _stockPriceLabel;
    private Label _playerOwnershipLabel;
    private Button _buyStockButton;
    private VisualElement _nicheContainer;
    private ElementPool _nichePool;

    // Products panel
    private VisualElement _activeProductsContainer;
    private ElementPool _activeProductPool;
    private VisualElement _inDevProductsContainer;
    private ElementPool _inDevProductPool;
    private Label _noActiveProductsLabel;
    private Label _noInDevProductsLabel;

    // Employees panel
    private Label _headcountLabel;
    private Label _avgSkillLabel;
    private Label _avgMoraleLabel;
    private Label _avgSalaryLabel;
    private VisualElement _employeeListContainer;
    private ElementPool _employeePool;

    // Finance panel
    private Label _cashLabel;
    private Label _revenueLabel;
    private Label _expensesLabel;
    private Label _profitLabel;
    private Label _healthLabel;
    private VisualElement _revenueBreakdownContainer;
    private ElementPool _revenuePool;
    private VisualElement _expenseBreakdownContainer;
    private ElementPool _expensePool;

    public CompetitorProfileView(IModalPresenter modal, ICommandDispatcher dispatcher) {
        _modal = modal;
        _dispatcher = dispatcher;
    }

    public void Initialize(VisualElement root) {
        _root = root;
        _root.AddToClassList("competitor-detail-modal");

        // Header
        var header = _root.Q<VisualElement>("header");
        if (header == null) {
            header = new VisualElement();
            header.name = "header";
            header.AddToClassList("modal-header");
            _root.Add(header);
        }
        _companyNameLabel = header.Q<Label>("company-name");
        if (_companyNameLabel == null) {
            _companyNameLabel = new Label("Competitor");
            _companyNameLabel.name = "company-name";
            _companyNameLabel.AddToClassList("text-xl");
            _companyNameLabel.AddToClassList("text-bold");
            header.Add(_companyNameLabel);
        }
        _reputationBadge = header.Q<Label>("reputation-badge");
        if (_reputationBadge == null) {
            _reputationBadge = new Label("--");
            _reputationBadge.name = "reputation-badge";
            _reputationBadge.AddToClassList("badge");
            _reputationBadge.AddToClassList("badge--neutral");
            header.Add(_reputationBadge);
        }
        _closeButton = header.Q<Button>("btn-close");
        if (_closeButton == null) {
            _closeButton = new Button { text = "✕" };
            _closeButton.name = "btn-close";
            _closeButton.AddToClassList("btn-sm");
            header.Add(_closeButton);
        }

        // Tab bar
        var tabBar = _root.Q<VisualElement>("tab-bar");
        if (tabBar == null) {
            tabBar = new VisualElement();
            tabBar.name = "tab-bar";
            tabBar.AddToClassList("detail-modal__tab-bar");
            _root.Add(tabBar);
        }
        _tabOverview  = EnsureTabButton(tabBar, "tab-overview",  "Overview",  true);
        _tabProducts  = EnsureTabButton(tabBar, "tab-products",  "Products",  false);
        _tabEmployees = EnsureTabButton(tabBar, "tab-employees", "Employees", false);
        _tabFinance   = EnsureTabButton(tabBar, "tab-finance",   "Finance",   false);

        // Body
        var body = _root.Q<VisualElement>("body");
        if (body == null) {
            body = new VisualElement();
            body.name = "body";
            body.AddToClassList("detail-modal__body");
            _root.Add(body);
        }

        // Overview panel
        _panelOverview = EnsurePanel(body, "panel-overview");
        _founderLabel = OverviewInfoRow(_panelOverview, "Founder", "overview-founder");
        _archetypeLabel = OverviewInfoRow(_panelOverview, "Archetype", "overview-archetype");
        _employeeCountLabel = OverviewInfoRow(_panelOverview, "Employees", "overview-employees");
        _productCountLabel = OverviewInfoRow(_panelOverview, "Products", "overview-products");

        var stockCard = new VisualElement();
        stockCard.AddToClassList("card");
        stockCard.AddToClassList("detail-modal__section");
        var stockTitle = new Label("Stock");
        stockTitle.AddToClassList("text-bold");
        stockTitle.AddToClassList("detail-modal__section-header");
        stockCard.Add(stockTitle);
        _stockPriceLabel = OverviewInfoRow(stockCard, "Stock Price", "stock-price");
        _playerOwnershipLabel = OverviewInfoRow(stockCard, "Your Stake", "player-stake");
        _buyStockButton = new Button { text = "Buy 5% Stake" };
        _buyStockButton.name = "btn-buy-stock";
        _buyStockButton.AddToClassList("btn-primary");
        _buyStockButton.AddToClassList("btn-sm");
        stockCard.Add(_buyStockButton);
        _panelOverview.Add(stockCard);

        var nicheCard = new VisualElement();
        nicheCard.AddToClassList("card");
        nicheCard.AddToClassList("detail-modal__section");
        var nicheTitle = new Label("Niche Presence");
        nicheTitle.AddToClassList("text-bold");
        nicheTitle.AddToClassList("detail-modal__section-header");
        nicheCard.Add(nicheTitle);
        _nicheContainer = new VisualElement();
        _nicheContainer.name = "niche-container";
        _nichePool = new ElementPool(CreateNicheRow, _nicheContainer);
        nicheCard.Add(_nicheContainer);
        _panelOverview.Add(nicheCard);

        // Products panel
        _panelProducts = EnsurePanel(body, "panel-products");
        var activeSectionHeader = new Label("Active Products");
        activeSectionHeader.AddToClassList("text-bold");
        activeSectionHeader.AddToClassList("detail-modal__section-header");
        _panelProducts.Add(activeSectionHeader);
        _noActiveProductsLabel = new Label("No active products.");
        _noActiveProductsLabel.AddToClassList("empty-state");
        _noActiveProductsLabel.name = "no-active-products";
        _panelProducts.Add(_noActiveProductsLabel);
        _activeProductsContainer = new VisualElement();
        _activeProductsContainer.name = "active-products-container";
        _activeProductPool = new ElementPool(CreateActiveProductRow, _activeProductsContainer);
        _panelProducts.Add(_activeProductsContainer);

        var inDevSectionHeader = new Label("In Development");
        inDevSectionHeader.AddToClassList("text-bold");
        inDevSectionHeader.AddToClassList("detail-modal__section-header");
        _panelProducts.Add(inDevSectionHeader);
        _noInDevProductsLabel = new Label("No products in development.");
        _noInDevProductsLabel.AddToClassList("empty-state");
        _noInDevProductsLabel.name = "no-indev-products";
        _panelProducts.Add(_noInDevProductsLabel);
        _inDevProductsContainer = new VisualElement();
        _inDevProductsContainer.name = "indev-products-container";
        _inDevProductPool = new ElementPool(CreateInDevProductRow, _inDevProductsContainer);
        _panelProducts.Add(_inDevProductsContainer);

        // Employees panel
        _panelEmployees = EnsurePanel(body, "panel-employees");
        var summaryBar = new VisualElement();
        summaryBar.AddToClassList("summary-bar");
        _headcountLabel  = SummaryStatBox(summaryBar, "Headcount",  "emp-headcount");
        _avgSkillLabel   = SummaryStatBox(summaryBar, "Avg Skill",  "emp-avg-skill");
        _avgMoraleLabel  = SummaryStatBox(summaryBar, "Avg Morale", "emp-avg-morale");
        _avgSalaryLabel  = SummaryStatBox(summaryBar, "Avg Salary", "emp-avg-salary");
        _panelEmployees.Add(summaryBar);
        _employeeListContainer = new VisualElement();
        _employeeListContainer.name = "employee-list";
        _employeePool = new ElementPool(CreateEmployeeRow, _employeeListContainer);
        _panelEmployees.Add(_employeeListContainer);

        // Finance panel
        _panelFinance = EnsurePanel(body, "panel-finance");
        var finSummary = new VisualElement();
        finSummary.AddToClassList("card");
        finSummary.AddToClassList("detail-modal__section");
        _cashLabel     = FinanceInfoRow(finSummary, "Cash",     "fin-cash");
        _revenueLabel  = FinanceInfoRow(finSummary, "Monthly Revenue",  "fin-revenue");
        _expensesLabel = FinanceInfoRow(finSummary, "Monthly Expenses", "fin-expenses");
        _profitLabel   = FinanceInfoRow(finSummary, "Monthly Profit",   "fin-profit");
        _healthLabel   = FinanceInfoRow(finSummary, "Financial Health", "fin-health");
        _panelFinance.Add(finSummary);

        var revTitle = new Label("Revenue Breakdown");
        revTitle.AddToClassList("text-bold");
        revTitle.AddToClassList("detail-modal__section-header");
        _panelFinance.Add(revTitle);
        _revenueBreakdownContainer = new VisualElement();
        _revenueBreakdownContainer.name = "revenue-breakdown";
        _revenuePool = new ElementPool(CreateRevenueRow, _revenueBreakdownContainer);
        _panelFinance.Add(_revenueBreakdownContainer);

        var expTitle = new Label("Expense Breakdown");
        expTitle.AddToClassList("text-bold");
        expTitle.AddToClassList("detail-modal__section-header");
        _panelFinance.Add(expTitle);
        _expenseBreakdownContainer = new VisualElement();
        _expenseBreakdownContainer.name = "expense-breakdown";
        _expensePool = new ElementPool(CreateExpenseRow, _expenseBreakdownContainer);
        _panelFinance.Add(_expenseBreakdownContainer);

        // Wire handlers
        _closeButton.clicked    += OnCloseClicked;
        _tabOverview.clicked    += OnTabOverviewClicked;
        _tabProducts.clicked    += OnTabProductsClicked;
        _tabEmployees.clicked   += OnTabEmployeesClicked;
        _tabFinance.clicked     += OnTabFinanceClicked;
        _buyStockButton.clicked += OnBuyStockClicked;
    }

    public void Bind(IViewModel viewModel) {
        _vm = viewModel as CompetitorProfileViewModel;
        if (_vm == null) return;

        _companyNameLabel.text = _vm.CompanyName;
        _reputationBadge.text  = _vm.ReputationDescription;

        ApplyTabSelection(_vm.ActiveTab);

        _panelOverview.EnableInClassList("tab-panel--active",   _vm.ActiveTab == CompetitorDetailTab.Overview);
        _panelProducts.EnableInClassList("tab-panel--active",   _vm.ActiveTab == CompetitorDetailTab.Products);
        _panelEmployees.EnableInClassList("tab-panel--active",  _vm.ActiveTab == CompetitorDetailTab.Employees);
        _panelFinance.EnableInClassList("tab-panel--active",    _vm.ActiveTab == CompetitorDetailTab.Finance);

        switch (_vm.ActiveTab) {
            case CompetitorDetailTab.Overview:  BindOverview();  break;
            case CompetitorDetailTab.Products:  BindProducts();  break;
            case CompetitorDetailTab.Employees: BindEmployees(); break;
            case CompetitorDetailTab.Finance:   BindFinance();   break;
        }
    }

    public void Dispose() {
        if (_closeButton    != null) _closeButton.clicked    -= OnCloseClicked;
        if (_tabOverview    != null) _tabOverview.clicked    -= OnTabOverviewClicked;
        if (_tabProducts    != null) _tabProducts.clicked    -= OnTabProductsClicked;
        if (_tabEmployees   != null) _tabEmployees.clicked   -= OnTabEmployeesClicked;
        if (_tabFinance     != null) _tabFinance.clicked     -= OnTabFinanceClicked;
        if (_buyStockButton != null) _buyStockButton.clicked -= OnBuyStockClicked;

        _nichePool    = null;
        _activeProductPool  = null;
        _inDevProductPool   = null;
        _employeePool = null;
        _revenuePool  = null;
        _expensePool  = null;
        _vm = null;
    }

    // Tab handlers
    private void OnTabOverviewClicked()  { SetActiveTab(CompetitorDetailTab.Overview); }
    private void OnTabProductsClicked()  { SetActiveTab(CompetitorDetailTab.Products); }
    private void OnTabEmployeesClicked() { SetActiveTab(CompetitorDetailTab.Employees); }
    private void OnTabFinanceClicked()   { SetActiveTab(CompetitorDetailTab.Finance); }

    private void SetActiveTab(CompetitorDetailTab tab) {
        if (_vm == null) return;
        _vm.ActiveTab = tab;
        Bind(_vm);
    }

    private void OnCloseClicked() {
        _modal?.DismissModal();
    }

    private void OnBuyStockClicked() {
        if (_vm == null || !_vm.CanBuyStock) return;
        _dispatcher?.Dispatch(new BuyStockCommand(_dispatcher.CurrentTick, _vm.Id, 0.05f));
        _modal?.DismissModal();
    }

    // Bind helpers
    private void BindOverview() {
        _founderLabel.text = _vm.FounderName;
        _archetypeLabel.text = _vm.ArchetypeLabel;
        _employeeCountLabel.text = _vm.TotalEmployees.ToString();
        _productCountLabel.text = _vm.TotalProducts.ToString();
        _stockPriceLabel.text = _vm.StockPrice;
        _playerOwnershipLabel.text = _vm.PlayerOwnership;
        _buyStockButton.SetEnabled(_vm.CanBuyStock);

        _nichePool.UpdateList(_vm.NichePresenceKeys, BindNicheRow);
    }

    private void BindProducts() {
        bool hasActive = _vm.ActiveProducts != null && _vm.ActiveProducts.Count > 0;
        _noActiveProductsLabel.EnableInClassList("hidden", hasActive);
        _activeProductsContainer.EnableInClassList("hidden", !hasActive);
        if (hasActive) _activeProductPool.UpdateList(_vm.ActiveProducts, BindActiveProductRow);

        bool hasInDev = _vm.InDevProducts != null && _vm.InDevProducts.Count > 0;
        _noInDevProductsLabel.EnableInClassList("hidden", hasInDev);
        _inDevProductsContainer.EnableInClassList("hidden", !hasInDev);
        if (hasInDev) _inDevProductPool.UpdateList(_vm.InDevProducts, BindInDevProductRow);
    }

    private void BindEmployees() {
        _headcountLabel.text = _vm.Headcount;
        _avgSkillLabel.text  = _vm.AvgSkillLevel;
        _avgMoraleLabel.text = _vm.AvgMorale;
        _avgSalaryLabel.text = _vm.AvgSalary;
        _employeePool.UpdateList(_vm.Employees, BindEmployeeRow);
    }

    private void BindFinance() {
        _cashLabel.text     = _vm.Cash;
        _revenueLabel.text  = _vm.MonthlyRevenue;
        _expensesLabel.text = _vm.MonthlyExpenses;
        _profitLabel.text   = _vm.MonthlyProfit;
        _healthLabel.text   = _vm.FinancialHealthLabel;
        _revenuePool.UpdateList(_vm.RevenueBreakdown, BindRevenueRow);
        _expensePool.UpdateList(_vm.ExpenseBreakdown, BindExpenseRow);
    }

    // Pool: niche row
    private VisualElement CreateNicheRow() {
        var row = new VisualElement();
        row.AddToClassList("niche-row");
        var keyLabel = new Label();
        keyLabel.name = "niche-key";
        keyLabel.AddToClassList("text-sm");
        keyLabel.AddToClassList("text-muted");
        var valLabel = new Label();
        valLabel.name = "niche-val";
        valLabel.AddToClassList("text-sm");
        valLabel.AddToClassList("text-bold");
        row.Add(keyLabel);
        row.Add(valLabel);
        return row;
    }

    private void BindNicheRow(VisualElement el, string nicheName) {
        el.Q<Label>("niche-key").text = nicheName;
        int idx = _vm?.NichePresenceKeys?.IndexOf(nicheName) ?? -1;
        el.Q<Label>("niche-val").text = idx >= 0 && _vm.NichePresenceValues != null && idx < _vm.NichePresenceValues.Count
            ? _vm.NichePresenceValues[idx] : "--";
    }

    // Pool: active product row
    private VisualElement CreateActiveProductRow() {
        var row = new VisualElement();
        row.AddToClassList("product-row");
        var left = new VisualElement();
        left.AddToClassList("product-row__left");
        var nameLabel = new Label();
        nameLabel.name = "prod-name";
        nameLabel.AddToClassList("text-sm");
        nameLabel.AddToClassList("text-bold");
        var catLabel = new Label();
        catLabel.name = "prod-cat";
        catLabel.AddToClassList("text-sm");
        catLabel.AddToClassList("text-muted");
        left.Add(nameLabel);
        left.Add(catLabel);
        var right = new VisualElement();
        right.AddToClassList("product-row__right");
        var qualLabel = new Label();
        qualLabel.name = "prod-quality";
        qualLabel.AddToClassList("text-sm");
        var revLabel = new Label();
        revLabel.name = "prod-rev";
        revLabel.AddToClassList("text-sm");
        var lifecycleLabel = new Label();
        lifecycleLabel.name = "prod-lifecycle";
        lifecycleLabel.AddToClassList("text-sm");
        lifecycleLabel.AddToClassList("text-muted");
        right.Add(qualLabel);
        right.Add(revLabel);
        right.Add(lifecycleLabel);
        row.Add(left);
        row.Add(right);
        return row;
    }

    private void BindActiveProductRow(VisualElement el, CompetitorProductRowVM data) {
        el.Q<Label>("prod-name").text      = data.Name;
        el.Q<Label>("prod-cat").text       = data.Category;
        el.Q<Label>("prod-quality").text   = "Q: " + data.Quality;
        el.Q<Label>("prod-rev").text       = data.Revenue;
        el.Q<Label>("prod-lifecycle").text = data.LifecycleStage;
        el.userData = data.Id;
    }

    // Pool: in-dev product row
    private VisualElement CreateInDevProductRow() {
        var row = new VisualElement();
        row.AddToClassList("product-row");
        var left = new VisualElement();
        left.AddToClassList("product-row__left");
        var nameLabel = new Label();
        nameLabel.name = "dev-name";
        nameLabel.AddToClassList("text-sm");
        nameLabel.AddToClassList("text-bold");
        var catLabel = new Label();
        catLabel.name = "dev-cat";
        catLabel.AddToClassList("text-sm");
        catLabel.AddToClassList("text-muted");
        left.Add(nameLabel);
        left.Add(catLabel);
        var right = new VisualElement();
        right.AddToClassList("product-row__right");
        var progressLabel = new Label();
        progressLabel.name = "dev-progress";
        progressLabel.AddToClassList("text-sm");
        right.Add(progressLabel);
        row.Add(left);
        row.Add(right);
        return row;
    }

    private void BindInDevProductRow(VisualElement el, CompetitorProductRowVM data) {
        el.Q<Label>("dev-name").text     = data.Name;
        el.Q<Label>("dev-cat").text      = data.Category;
        el.Q<Label>("dev-progress").text = data.Progress;
        el.userData = data.Id;
    }

    // Pool: employee row
    private VisualElement CreateEmployeeRow() {
        var row = new VisualElement();
        row.AddToClassList("employee-row");
        var nameLabel = new Label();
        nameLabel.name = "emp-name";
        nameLabel.AddToClassList("text-sm");
        nameLabel.AddToClassList("text-bold");
        nameLabel.style.flexGrow = 2;
        nameLabel.style.flexBasis = 0;
        var roleLabel = new Label();
        roleLabel.name = "emp-role";
        roleLabel.AddToClassList("text-sm");
        roleLabel.AddToClassList("text-muted");
        roleLabel.style.flexGrow = 2;
        roleLabel.style.flexBasis = 0;
        var skillLabel = new Label();
        skillLabel.name = "emp-skill";
        skillLabel.AddToClassList("text-sm");
        skillLabel.style.flexGrow = 1;
        skillLabel.style.flexBasis = 0;
        var moraleLabel = new Label();
        moraleLabel.name = "emp-morale";
        moraleLabel.AddToClassList("text-sm");
        moraleLabel.style.flexGrow = 1;
        moraleLabel.style.flexBasis = 0;
        var salaryLabel = new Label();
        salaryLabel.name = "emp-salary";
        salaryLabel.AddToClassList("text-sm");
        salaryLabel.style.flexGrow = 1;
        salaryLabel.style.flexBasis = 0;
        salaryLabel.style.unityTextAlign = UnityEngine.TextAnchor.MiddleRight;
        row.Add(nameLabel);
        row.Add(roleLabel);
        row.Add(skillLabel);
        row.Add(moraleLabel);
        row.Add(salaryLabel);
        return row;
    }

    private void BindEmployeeRow(VisualElement el, CompetitorEmployeeRowVM data) {
        el.Q<Label>("emp-name").text   = data.Name;
        el.Q<Label>("emp-role").text   = data.Role;
        el.Q<Label>("emp-skill").text  = "Skill: " + data.SkillLevel;
        el.Q<Label>("emp-morale").text = "Morale: " + data.MoraleIndicator;
        el.Q<Label>("emp-salary").text = data.Salary;
        el.userData = data.Id;
    }

    // Pool: revenue row
    private VisualElement CreateRevenueRow() {
        var row = new VisualElement();
        row.AddToClassList("revenue-row");
        var name = new Label();
        name.name = "rev-name";
        name.AddToClassList("text-sm");
        name.AddToClassList("text-muted");
        var amt = new Label();
        amt.name = "rev-amount";
        amt.AddToClassList("text-sm");
        amt.AddToClassList("text-bold");
        row.Add(name);
        row.Add(amt);
        return row;
    }

    private void BindRevenueRow(VisualElement el, CompetitorRevenueRowVM data) {
        el.Q<Label>("rev-name").text   = data.ProductName;
        el.Q<Label>("rev-amount").text = data.MonthlyRevenue;
    }

    // Pool: expense row
    private VisualElement CreateExpenseRow() {
        var row = new VisualElement();
        row.AddToClassList("expense-row");
        var cat = new Label();
        cat.name = "exp-cat";
        cat.AddToClassList("text-sm");
        cat.AddToClassList("text-muted");
        var amt = new Label();
        amt.name = "exp-amount";
        amt.AddToClassList("text-sm");
        amt.AddToClassList("text-bold");
        row.Add(cat);
        row.Add(amt);
        return row;
    }

    private void BindExpenseRow(VisualElement el, CompetitorExpenseRowVM data) {
        el.Q<Label>("exp-cat").text    = data.Category;
        el.Q<Label>("exp-amount").text = data.MonthlyAmount;
    }

    // --- UI build helpers ---

    private void ApplyTabSelection(CompetitorDetailTab activeTab) {
        _tabOverview.EnableInClassList("tab-selected",  activeTab == CompetitorDetailTab.Overview);
        _tabProducts.EnableInClassList("tab-selected",  activeTab == CompetitorDetailTab.Products);
        _tabEmployees.EnableInClassList("tab-selected", activeTab == CompetitorDetailTab.Employees);
        _tabFinance.EnableInClassList("tab-selected",   activeTab == CompetitorDetailTab.Finance);
    }

    private static VisualElement EnsurePanel(VisualElement parent, string name) {
        var panel = parent.Q<VisualElement>(name);
        if (panel == null) {
            panel = new ScrollView();
            panel.name = name;
            panel.AddToClassList("tab-panel");
            parent.Add(panel);
        }
        return panel;
    }

    private static Button EnsureTabButton(VisualElement tabBar, string name, string text, bool isSelected) {
        var btn = tabBar.Q<Button>(name);
        if (btn == null) {
            btn = new Button { text = text };
            btn.name = name;
            btn.AddToClassList("tab-button");
            if (isSelected) btn.AddToClassList("tab-selected");
            tabBar.Add(btn);
        }
        return btn;
    }

    private static Label OverviewInfoRow(VisualElement parent, string labelText, string valueName) {
        var row = new VisualElement();
        row.AddToClassList("info-row");
        var key = new Label(labelText);
        key.AddToClassList("text-sm");
        key.AddToClassList("text-muted");
        var val = new Label("--");
        val.name = valueName;
        val.AddToClassList("text-sm");
        val.AddToClassList("text-bold");
        row.Add(key);
        row.Add(val);
        parent.Add(row);
        return val;
    }

    private static Label FinanceInfoRow(VisualElement parent, string labelText, string valueName) {
        return OverviewInfoRow(parent, labelText, valueName);
    }

    private static Label SummaryStatBox(VisualElement parent, string labelText, string valueName) {
        var box = new VisualElement();
        box.AddToClassList("summary-box");
        var val = new Label("--");
        val.name = valueName;
        val.AddToClassList("text-bold");
        var key = new Label(labelText);
        key.AddToClassList("text-sm");
        key.AddToClassList("text-muted");
        box.Add(val);
        box.Add(key);
        parent.Add(box);
        return val;
    }
}

