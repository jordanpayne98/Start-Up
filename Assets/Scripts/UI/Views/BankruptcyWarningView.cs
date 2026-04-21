using UnityEngine.UIElements;

public class BankruptcyWarningView : IGameView
{
    private readonly INavigationService _navigation;
    private readonly IModalPresenter _modal;
    private VisualElement _root;
    private BankruptcyWarningViewModel _vm;

    private Label _countdownLabel;
    private Label _cashLabel;
    private Label _burnRateLabel;
    private Button _emergencyLoanButton;
    private Button _sellStockButton;
    private Button _sellProductButton;

    public BankruptcyWarningView(INavigationService navigation, IModalPresenter modal) {
        _navigation = navigation;
        _modal = modal;
    }

    public void Initialize(VisualElement root) {
        _root = root;
        _root.AddToClassList("bankruptcy-warning");

        // Warning banner header
        var bannerHeader = new VisualElement();
        bannerHeader.style.flexDirection = FlexDirection.Row;
        bannerHeader.style.justifyContent = Justify.SpaceBetween;
        bannerHeader.style.alignItems = Align.Center;
        bannerHeader.style.marginBottom = 12;

        var warningIcon = new Label("⚠ BANKRUPTCY WARNING");
        warningIcon.AddToClassList("text-bold");
        warningIcon.AddToClassList("text-danger");
        bannerHeader.Add(warningIcon);

        _countdownLabel = new Label("3 months remaining");
        _countdownLabel.AddToClassList("text-bold");
        _countdownLabel.AddToClassList("text-danger");
        _countdownLabel.AddToClassList("text-xl");
        bannerHeader.Add(_countdownLabel);

        _root.Add(bannerHeader);

        // Cash info row
        var infoRow = new VisualElement();
        infoRow.style.flexDirection = FlexDirection.Row;
        infoRow.style.marginBottom = 12;

        var cashSection = new VisualElement();
        cashSection.style.flexGrow = 1;
        cashSection.style.marginRight = 16;
        var cashKey = new Label("Current Cash");
        cashKey.AddToClassList("text-sm");
        cashKey.AddToClassList("text-muted");
        cashSection.Add(cashKey);
        _cashLabel = new Label("--");
        _cashLabel.AddToClassList("text-xl");
        _cashLabel.AddToClassList("text-bold");
        _cashLabel.AddToClassList("text-danger");
        cashSection.Add(_cashLabel);
        infoRow.Add(cashSection);

        var burnSection = new VisualElement();
        burnSection.style.flexGrow = 1;
        var burnKey = new Label("Monthly Burn Rate");
        burnKey.AddToClassList("text-sm");
        burnKey.AddToClassList("text-muted");
        burnSection.Add(burnKey);
        _burnRateLabel = new Label("--");
        _burnRateLabel.AddToClassList("text-lg");
        _burnRateLabel.AddToClassList("text-bold");
        burnSection.Add(_burnRateLabel);
        infoRow.Add(burnSection);

        _root.Add(infoRow);

        // Lifelines
        var lifelineTitle = new Label("Lifelines");
        lifelineTitle.AddToClassList("text-bold");
        lifelineTitle.style.marginBottom = 8;
        _root.Add(lifelineTitle);

        var lifelineRow = new VisualElement();
        lifelineRow.style.flexDirection = FlexDirection.Row;

        _emergencyLoanButton = new Button { text = "Emergency Loan" };
        _emergencyLoanButton.AddToClassList("btn-warning");
        _emergencyLoanButton.style.marginRight = 8;
        lifelineRow.Add(_emergencyLoanButton);

        _sellStockButton = new Button { text = "Sell Stock" };
        _sellStockButton.AddToClassList("btn-secondary");
        _sellStockButton.style.marginRight = 8;
        lifelineRow.Add(_sellStockButton);

        _sellProductButton = new Button { text = "Sell Product" };
        _sellProductButton.AddToClassList("btn-secondary");
        lifelineRow.Add(_sellProductButton);

        _root.Add(lifelineRow);

        _emergencyLoanButton.clicked += OnEmergencyLoanClicked;
        _sellStockButton.clicked += OnSellStockClicked;
        _sellProductButton.clicked += OnSellProductClicked;
    }

    public void Bind(IViewModel viewModel) {
        _vm = viewModel as BankruptcyWarningViewModel;
        if (_vm == null) return;

        _root.style.display = _vm.IsActive ? DisplayStyle.Flex : DisplayStyle.None;
        if (!_vm.IsActive) return;

        _cashLabel.text = _vm.CurrentCash;
        _burnRateLabel.text = _vm.MonthlyBurnRate + "/mo";

        string monthsText = _vm.MonthsRemaining == 1 ? "1 month remaining" : _vm.MonthsRemaining + " months remaining";
        _countdownLabel.text = monthsText;

        _countdownLabel.RemoveFromClassList("text-danger");
        _countdownLabel.RemoveFromClassList("text-warning");
        _countdownLabel.AddToClassList(_vm.MonthsRemaining <= 1 ? "text-danger" : "text-warning");

        _emergencyLoanButton.SetEnabled(_vm.CanTakeLoan);
        _sellStockButton.SetEnabled(_vm.HasStockToSell);
        _sellProductButton.SetEnabled(_vm.HasProductsToSell);
    }

    public void Dispose() {
        if (_emergencyLoanButton != null) _emergencyLoanButton.clicked -= OnEmergencyLoanClicked;
        if (_sellStockButton != null) _sellStockButton.clicked -= OnSellStockClicked;
        if (_sellProductButton != null) _sellProductButton.clicked -= OnSellProductClicked;
        _vm = null;
    }

    private void OnEmergencyLoanClicked() {
        _navigation?.NavigateTo(ScreenId.FinanceOverview);
    }

    private void OnSellStockClicked() {
        _navigation?.NavigateTo(ScreenId.FinanceMyInvestments);
    }

    private void OnSellProductClicked() {
        _navigation?.NavigateTo(ScreenId.MarketProductsBrowser);
    }
}
