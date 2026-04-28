using UnityEngine.UIElements;

public class AcquisitionView : IGameView
{
    private readonly IModalPresenter _modal;
    private readonly ICommandDispatcher _dispatcher;
    private VisualElement _root;
    private AcquisitionViewModel _vm;

    private Label _companyNameLabel;
    private Label _totalPriceLabel;
    private Label _employeesLabel;
    private Label _marketShareLabel;
    private Label _unprofitableLabel;
    private Label _salaryIncreaseLabel;
    private Label _maintenanceIncreaseLabel;
    private Label _profitChangeLabel;
    private VisualElement _productsContainer;
    private ElementPool _productsPool;
    private Button _confirmButton;
    private Button _cancelButton;

    public AcquisitionView(IModalPresenter modal, ICommandDispatcher dispatcher) {
        _modal = modal;
        _dispatcher = dispatcher;
    }

    public void Initialize(VisualElement root) {
        _root = root;
        _root.AddToClassList("acquisition-modal");

        // Header
        var header = new VisualElement();
        header.AddToClassList("modal-header");
        header.style.flexDirection = FlexDirection.Row;
        header.style.justifyContent = Justify.SpaceBetween;
        header.style.alignItems = Align.Center;

        _companyNameLabel = new Label("Acquire Company");
        _companyNameLabel.AddToClassList("text-xl");
        _companyNameLabel.AddToClassList("text-bold");
        header.Add(_companyNameLabel);

        _cancelButton = new Button { text = "X" };
        _cancelButton.AddToClassList("btn-sm");
        _cancelButton.style.minWidth = 30;
        header.Add(_cancelButton);
        _root.Add(header);

        // Scrollable body
        var bodyScroll = new ScrollView();
        bodyScroll.AddToClassList("modal-body");
        bodyScroll.style.flexGrow = 1;
        bodyScroll.style.flexShrink = 1;
        var body = bodyScroll.contentContainer;
        _root.Add(bodyScroll);

        // Price card
        var priceCard = new VisualElement();
        priceCard.AddToClassList("card");
        priceCard.style.marginBottom = 12;
        var priceTitle = new Label("Acquisition Cost");
        priceTitle.AddToClassList("text-bold");
        priceTitle.style.marginBottom = 4;
        priceCard.Add(priceTitle);
        _totalPriceLabel = new Label("--");
        _totalPriceLabel.AddToClassList("text-2xl");
        _totalPriceLabel.AddToClassList("text-bold");
        _totalPriceLabel.AddToClassList("text-accent");
        priceCard.Add(_totalPriceLabel);
        body.Add(priceCard);

        // Assets gained card
        var assetsCard = new VisualElement();
        assetsCard.AddToClassList("card");
        assetsCard.style.marginBottom = 12;
        var assetsTitle = new Label("Assets Gained");
        assetsTitle.AddToClassList("text-bold");
        assetsTitle.style.marginBottom = 8;
        assetsCard.Add(assetsTitle);
        _employeesLabel = CreateInfoRow(assetsCard, "Employees");
        _marketShareLabel = CreateInfoRow(assetsCard, "Market Share");

        var productsTitle = new Label("Products");
        productsTitle.AddToClassList("text-sm");
        productsTitle.AddToClassList("text-secondary");
        productsTitle.style.marginTop = 8;
        productsTitle.style.marginBottom = 4;
        assetsCard.Add(productsTitle);
        _productsContainer = new VisualElement();
        _productsPool = new ElementPool(CreateProductLine, _productsContainer);
        assetsCard.Add(_productsContainer);
        body.Add(assetsCard);

        // Liabilities card
        var liabCard = new VisualElement();
        liabCard.AddToClassList("card");
        liabCard.style.marginBottom = 12;
        var liabTitle = new Label("Liabilities");
        liabTitle.AddToClassList("text-bold");
        liabTitle.style.marginBottom = 8;
        liabCard.Add(liabTitle);
        _unprofitableLabel = CreateInfoRow(liabCard, "Unprofitable Products");
        _salaryIncreaseLabel = CreateInfoRow(liabCard, "Monthly Salary Cost");
        _maintenanceIncreaseLabel = CreateInfoRow(liabCard, "Monthly Maintenance");
        body.Add(liabCard);

        // Profit impact card
        var impactCard = new VisualElement();
        impactCard.AddToClassList("card");
        impactCard.style.marginBottom = 12;
        var impactTitle = new Label("Estimated Monthly Profit Change");
        impactTitle.AddToClassList("text-bold");
        impactTitle.style.marginBottom = 4;
        impactCard.Add(impactTitle);
        _profitChangeLabel = new Label("--");
        _profitChangeLabel.AddToClassList("text-xl");
        _profitChangeLabel.AddToClassList("text-bold");
        impactCard.Add(_profitChangeLabel);
        body.Add(impactCard);

        // Footer
        var footer = new VisualElement();
        footer.AddToClassList("modal-footer");
        footer.style.flexDirection = FlexDirection.Row;
        footer.style.justifyContent = Justify.FlexEnd;
        _root.Add(footer);

        var cancelFooterBtn = new Button { text = "Cancel" };
        cancelFooterBtn.AddToClassList("btn-secondary");
        cancelFooterBtn.style.marginRight = 8;
        cancelFooterBtn.clicked += OnCancelClicked;
        footer.Add(cancelFooterBtn);

        _confirmButton = new Button { text = "Confirm Acquisition" };
        _confirmButton.AddToClassList("btn-primary");
        footer.Add(_confirmButton);

        _cancelButton.clicked += OnCancelClicked;
        _confirmButton.clicked += OnConfirmClicked;
    }

    public void Bind(IViewModel viewModel) {
        _vm = viewModel as AcquisitionViewModel;
        if (_vm == null) return;

        _companyNameLabel.text = "Acquire: " + _vm.TargetCompanyName;
        _totalPriceLabel.text = _vm.TotalPrice;
        _employeesLabel.text = _vm.EmployeesGained;
        _marketShareLabel.text = _vm.MarketShareGained;
        _unprofitableLabel.text = _vm.UnprofitableProducts;
        _salaryIncreaseLabel.text = _vm.EstimatedMonthlySalaryIncrease;
        _maintenanceIncreaseLabel.text = _vm.EstimatedMaintenanceCostIncrease;
        _profitChangeLabel.text = _vm.EstimatedMonthlyProfitChange;
        _profitChangeLabel.RemoveFromClassList("text-success");
        _profitChangeLabel.RemoveFromClassList("text-danger");
        _profitChangeLabel.AddToClassList(_vm.IsProfitChangePositive ? "text-success" : "text-danger");

        _productsPool.UpdateList(_vm.ProductsGained, BindProductLine);
    }

    public void Dispose() {
        if (_cancelButton != null) _cancelButton.clicked -= OnCancelClicked;
        if (_confirmButton != null) _confirmButton.clicked -= OnConfirmClicked;
        _productsPool = null;
        _vm = null;
    }

    private void OnCancelClicked() {
        _modal?.DismissModal();
    }

    private void OnConfirmClicked() {
        if (_vm == null) return;
        _dispatcher?.Dispatch(new BuyStockCommand(_dispatcher.CurrentTick, _vm.TargetId, 1.0f));
        _modal?.DismissModal();
    }

    private Label CreateInfoRow(VisualElement parent, string labelText) {
        var row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        row.style.justifyContent = Justify.SpaceBetween;
        row.style.marginBottom = 4;

        var label = new Label(labelText);
        label.AddToClassList("text-secondary");
        label.AddToClassList("text-sm");
        row.Add(label);

        var value = new Label("--");
        value.AddToClassList("text-bold");
        value.AddToClassList("text-sm");
        row.Add(value);

        parent.Add(row);
        return value;
    }

    private VisualElement CreateProductLine() {
        var label = new Label();
        label.name = "product-line";
        label.AddToClassList("text-sm");
        label.style.marginBottom = 2;
        return label;
    }

    private void BindProductLine(VisualElement el, string data) {
        var label = el.Q<Label>("product-line");
        if (label != null) label.text = "• " + data;
    }
}
