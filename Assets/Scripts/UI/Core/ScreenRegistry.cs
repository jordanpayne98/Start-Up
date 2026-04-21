using System.Collections.Generic;

public class ScreenRegistry
{
    private readonly Dictionary<ScreenId, ScreenConfig> _configs = new Dictionary<ScreenId, ScreenConfig>();
    private NavNode _navigationTree;

    public ScreenRegistry(ICommandDispatcher dispatcher, IModalPresenter modal, INavigationService nav, ITooltipProvider tooltip) {
        // --- Dashboard ---
        RegisterScreen(new ScreenConfig {
            ScreenId = ScreenId.DashboardInbox,
            Category = NavCategory.Dashboard,
            DisplayName = "Inbox",
            UxmlPath = "Assets/UI/UXML/Screens/PortalInbox.uxml",
            ViewFactory = () => new InboxView(dispatcher, modal, nav),
            ViewModelFactory = () => new InboxViewModel()
        });
        RegisterScreen(new ScreenConfig {
            ScreenId = ScreenId.DashboardCalendar,
            Category = NavCategory.Dashboard,
            DisplayName = "Calendar",
            UxmlPath = "Assets/UI/UXML/Screens/PortalCalendar.uxml",
            ViewFactory = () => new CalendarView(modal),
            ViewModelFactory = () => new CalendarViewModel()
        });

        // --- HR ---
        RegisterScreen(new ScreenConfig {
            ScreenId = ScreenId.HREmployees,
            Category = NavCategory.HR,
            DisplayName = "Employees",
            UxmlPath = "Assets/UI/UXML/Screens/StaffEmployees.uxml",
            ViewFactory = () => new EmployeesView(dispatcher, modal, tooltip),
            ViewModelFactory = () => new EmployeesViewModel()
        });
        RegisterScreen(new ScreenConfig {
            ScreenId = ScreenId.HRTeams,
            Category = NavCategory.HR,
            DisplayName = "Teams",
            UxmlPath = "Assets/UI/UXML/Screens/StaffTeams.uxml",
            ViewFactory = () => new TeamsView(dispatcher, modal, tooltip),
            ViewModelFactory = () => new TeamsViewModel()
        });
        RegisterScreen(new ScreenConfig {
            ScreenId = ScreenId.HRCandidates,
            Category = NavCategory.HR,
            DisplayName = "Candidates",
            UxmlPath = "Assets/UI/UXML/Screens/StaffHR.uxml",
            ViewFactory = () => new HRView(dispatcher, modal),
            ViewModelFactory = () => new HRViewModel()
        });

        // --- Business: Finance ---
        RegisterScreen(new ScreenConfig {
            ScreenId = ScreenId.FinanceOverview,
            Category = NavCategory.Finance,
            DisplayName = "Financial Overview",
            UxmlPath = "Assets/UI/UXML/Screens/BusinessFinance.uxml",
            ViewFactory = () => new FinanceView(dispatcher, modal, tooltip),
            ViewModelFactory = () => new FinanceViewModel()
        });
        RegisterScreen(new ScreenConfig {
            ScreenId = ScreenId.FinanceStockInvestments,
            Category = NavCategory.Finance,
            DisplayName = "Stock / Investments",
            UxmlPath = "Assets/UI/UXML/Screens/BusinessStockInvestments.uxml",
            ViewFactory = () => new StockInvestmentsView(dispatcher),
            ViewModelFactory = () => new StockInvestmentsViewModel()
        });

        // --- Business: Finance > Market ---
        RegisterScreen(new ScreenConfig {
            ScreenId = ScreenId.MarketOverview,
            Category = NavCategory.Market,
            DisplayName = "Market Overview",
            UxmlPath = "Assets/UI/UXML/Screens/BusinessMarket.uxml",
            ViewFactory = () => new MarketView(dispatcher, tooltip),
            ViewModelFactory = () => new MarketViewModel()
        });
        RegisterScreen(new ScreenConfig {
            ScreenId = ScreenId.MarketProductsBrowser,
            Category = NavCategory.Market,
            DisplayName = "Products Browser",
            UxmlPath = "Assets/UI/UXML/Screens/BusinessProductsBrowser.uxml",
            ViewFactory = () => new ProductsBrowserView(modal, nav),
            ViewModelFactory = () => new ProductsBrowserViewModel()
        });

        // --- Business: Operations ---
        RegisterScreen(new ScreenConfig {
            ScreenId = ScreenId.ProductionContracts,
            Category = NavCategory.Production,
            DisplayName = "Contracts",
            UxmlPath = "Assets/UI/UXML/Screens/BusinessContracts.uxml",
            ViewFactory = () => new ContractsView(dispatcher, tooltip),
            ViewModelFactory = () => new ContractsOverviewViewModel()
        });
        RegisterScreen(new ScreenConfig {
            ScreenId = ScreenId.ProductionProductsInDev,
            Category = NavCategory.Production,
            DisplayName = "In Development",
            UxmlPath = "Assets/UI/UXML/Screens/BusinessProducts.uxml",
            ViewFactory = () => new ProductsView(dispatcher, modal, tooltip, ProductsViewMode.InDevelopment),
            ViewModelFactory = () => new ProductsViewModel()
        });
        RegisterScreen(new ScreenConfig {
            ScreenId = ScreenId.ProductionProductsLive,
            Category = NavCategory.Production,
            DisplayName = "Live Products",
            UxmlPath = "Assets/UI/UXML/Screens/BusinessProducts.uxml",
            ViewFactory = () => new ProductsView(dispatcher, modal, tooltip, ProductsViewMode.Live),
            ViewModelFactory = () => new ProductsViewModel()
        });

        // --- Business: Competitors ---
        RegisterScreen(new ScreenConfig {
            ScreenId = ScreenId.CompetitorsList,
            Category = NavCategory.Competitors,
            DisplayName = "Competitor List",
            UxmlPath = "Assets/UI/UXML/Screens/BusinessCompetitorList.uxml",
            ViewFactory = () => new CompetitorListView(modal),
            ViewModelFactory = () => new CompetitorListViewModel()
        });
        RegisterScreen(new ScreenConfig {
            ScreenId = ScreenId.FinanceMyInvestments,
            Category = NavCategory.Finance,
            DisplayName = "My Investments",
            UxmlPath = "Assets/UI/UXML/Screens/BusinessMyInvestments.uxml",
            ViewFactory = () => new MyInvestmentsView(dispatcher),
            ViewModelFactory = () => new MyInvestmentsViewModel()
        });
        RegisterScreen(new ScreenConfig {
            ScreenId = ScreenId.CompetitorsIndustryOverview,
            Category = NavCategory.Competitors,
            DisplayName = "Industry Overview",
            UxmlPath = "Assets/UI/UXML/Screens/BusinessIndustryOverview.uxml",
            ViewFactory = () => new IndustryOverviewView(),
            ViewModelFactory = () => new IndustryOverviewViewModel()
        });

        // --- Dashboard: Reputation ---
        RegisterScreen(new ScreenConfig {
            ScreenId = ScreenId.DashboardReputation,
            Category = NavCategory.Dashboard,
            DisplayName = "Reputation",
            UxmlPath = "Assets/UI/UXML/Screens/BusinessReputation.uxml",
            ViewFactory = () => new ReputationView(tooltip),
            ViewModelFactory = () => new ReputationViewModel()
        });

        BuildNavigationTree();
    }

    private void RegisterScreen(ScreenConfig config) {
        _configs[config.ScreenId] = config;
    }

    private void BuildNavigationTree() {
        var root = new NavNode { Id = "root", Label = "Root" };

        // Dashboard
        var dashboard = new NavNode { Id = "dashboard", Label = "Dashboard", Icon = "▦", Hotkey = "1" };
        dashboard.AddChild(new NavNode { Id = "dashboard-inbox",      Label = "Inbox",      ScreenId = ScreenId.DashboardInbox,      Hotkey = "I" });
        dashboard.AddChild(new NavNode { Id = "dashboard-calendar",   Label = "Calendar",   ScreenId = ScreenId.DashboardCalendar,   Hotkey = "C" });
        dashboard.AddChild(new NavNode { Id = "dashboard-reputation", Label = "Reputation", ScreenId = ScreenId.DashboardReputation, Hotkey = "R" });
        root.AddChild(dashboard);

        // HR
        var hr = new NavNode { Id = "hr", Label = "HR", Icon = "♟", Hotkey = "2" };
        hr.AddChild(new NavNode { Id = "hr-employees",  Label = "Employees",  ScreenId = ScreenId.HREmployees,  Hotkey = "E" });
        hr.AddChild(new NavNode { Id = "hr-teams",      Label = "Teams",      ScreenId = ScreenId.HRTeams,      Hotkey = "T" });
        hr.AddChild(new NavNode { Id = "hr-candidates", Label = "Candidates", ScreenId = ScreenId.HRCandidates, Hotkey = "H" });
        root.AddChild(hr);

        // Finance
        var finance = new NavNode { Id = "finance", Label = "Finance", Icon = "◈", Hotkey = "3" };
        finance.AddChild(new NavNode { Id = "finance-overview",    Label = "Financial Overview",  ScreenId = ScreenId.FinanceOverview,         Hotkey = "F" });
        finance.AddChild(new NavNode { Id = "finance-stock",       Label = "Stock / Investments", ScreenId = ScreenId.FinanceStockInvestments, Hotkey = "S" });
        finance.AddChild(new NavNode { Id = "finance-investments", Label = "My Investments",      ScreenId = ScreenId.FinanceMyInvestments,    Hotkey = "I" });
        root.AddChild(finance);

        // Production
        var production = new NavNode { Id = "production", Label = "Production", Icon = "⚙", Hotkey = "4" };
        production.AddChild(new NavNode { Id = "production-contracts", Label = "Contracts", ScreenId = ScreenId.ProductionContracts, Hotkey = "C" });

        var productsGroup = new NavNode { Id = "production-products-group", Label = "Products", Icon = "", Hotkey = "P" };
        productsGroup.AddChild(new NavNode { Id = "production-products-indev", Label = "In Development", ScreenId = ScreenId.ProductionProductsInDev });
        productsGroup.AddChild(new NavNode { Id = "production-products-live",  Label = "Live Products",  ScreenId = ScreenId.ProductionProductsLive });
        production.AddChild(productsGroup);

        root.AddChild(production);

        // Market
        var market = new NavNode { Id = "market", Label = "Market", Icon = "◆", Hotkey = "5" };
        market.AddChild(new NavNode { Id = "market-overview", Label = "Market Overview",  ScreenId = ScreenId.MarketOverview,        Hotkey = "O" });
        market.AddChild(new NavNode { Id = "market-browser",  Label = "Products Browser", ScreenId = ScreenId.MarketProductsBrowser, Hotkey = "B" });
        root.AddChild(market);

        // Competitors
        var competitors = new NavNode { Id = "competitors", Label = "Competitors", Icon = "⚔", Hotkey = "6" };
        competitors.AddChild(new NavNode { Id = "competitors-list",     Label = "Competitor List",   ScreenId = ScreenId.CompetitorsList,             Hotkey = "L" });
        competitors.AddChild(new NavNode { Id = "competitors-industry", Label = "Industry Overview", ScreenId = ScreenId.CompetitorsIndustryOverview, Hotkey = "V" });
        root.AddChild(competitors);

        _navigationTree = root;
    }

    public NavNode GetNavigationTree() => _navigationTree;

    public ScreenConfig GetConfig(ScreenId id) {
        if (_configs.TryGetValue(id, out var config)) return config;
        return default;
    }
}
