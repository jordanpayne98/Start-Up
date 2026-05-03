using System.Collections.Generic;
using UnityEngine.UIElements;

public class ProductsBrowserView : IGameView
{
    private readonly IModalPresenter _modal;
    private readonly INavigationService _navigation;
    private VisualElement _root;
    private ProductsBrowserViewModel _viewModel;

    private VisualElement _listContainer;
    private ElementPool _listPool;

    private Label _resultCountLabel;
    private readonly List<Button> _headerButtons = new List<Button>();
    private readonly List<Button> _ownerPills = new List<Button>();
    private readonly List<Button> _statusPills = new List<Button>();
    private Label _summaryRevenueLabel;
    private Label _summaryMaintenanceLabel;
    private Label _summaryNetLabel;
    private VisualElement _summaryCard;
    private Button _revenueHeaderBtn;

    private static readonly OwnerFilter[] _ownerEnums = { OwnerFilter.All, OwnerFilter.MyProducts, OwnerFilter.Competitor };
    private static readonly StatusFilter[] _statusEnums = { StatusFilter.Live, StatusFilter.Archived, StatusFilter.All };

    public ProductsBrowserView(IModalPresenter modal, INavigationService navigation) {
        _modal = modal;
        _navigation = navigation;
    }

    public void Initialize(VisualElement root, UIServices services) {
        _root = root;

        var title = new Label("Products Browser");
        title.AddToClassList("section-header");
        _root.Add(title);

        _resultCountLabel = new Label("0 products");
        _resultCountLabel.AddToClassList("text-sm");
        _resultCountLabel.AddToClassList("text-muted");
        _resultCountLabel.style.marginBottom = 8;
        _root.Add(_resultCountLabel);

        var filterBar = new VisualElement();
        filterBar.AddToClassList("flex-row");
        filterBar.style.marginBottom = 8;
        filterBar.style.flexWrap = Wrap.Wrap;

        var ownerLabels = new[] { "All", "My Products", "Competitors" };
        for (int i = 0; i < ownerLabels.Length; i++) {
            var btn = new Button { text = ownerLabels[i] };
            btn.AddToClassList("sub-tab");
            if (i == 0) btn.AddToClassList("sub-tab--active");
            var capturedFilter = _ownerEnums[i];
            btn.clicked += () => OnOwnerFilterClicked(capturedFilter);
            filterBar.Add(btn);
            _ownerPills.Add(btn);
        }

        var separator = new VisualElement();
        separator.style.width = 16;
        filterBar.Add(separator);

        var statusLabels = new[] { "Live", "Archived", "All" };
        for (int i = 0; i < statusLabels.Length; i++) {
            var btn = new Button { text = statusLabels[i] };
            btn.AddToClassList("sub-tab");
            if (i == 0) btn.AddToClassList("sub-tab--active");
            var capturedFilter = _statusEnums[i];
            btn.clicked += () => OnStatusFilterClicked(capturedFilter);
            filterBar.Add(btn);
            _statusPills.Add(btn);
        }

        _root.Add(filterBar);

        _summaryCard = new VisualElement();
        _summaryCard.AddToClassList("card");
        _summaryCard.style.marginBottom = 12;
        var summaryRow = new VisualElement();
        summaryRow.AddToClassList("flex-row");
        _summaryCard.Add(summaryRow);

        _summaryRevenueLabel = CreateSummaryCell(summaryRow, "Monthly Revenue");
        _summaryMaintenanceLabel = CreateSummaryCell(summaryRow, "Maintenance Cost");
        _summaryNetLabel = CreateSummaryCell(summaryRow, "Net Income");
        _root.Add(_summaryCard);

        var scroll = new ScrollView();
        scroll.style.flexGrow = 1;
        var content = scroll.contentContainer;

        var headerRow = new VisualElement();
        headerRow.AddToClassList("column-header");

        var columns = new[] {
            ("Product",    ProductBrowserSortColumn.Name,          3),
            ("Company",    ProductBrowserSortColumn.Company,       2),
            ("Niche",      ProductBrowserSortColumn.Niche,         2),
            ("Review",     ProductBrowserSortColumn.Quality,       1),
            ("Sales /mo",  ProductBrowserSortColumn.SalesPerMonth, 2),
            ("Monthly Rev.", ProductBrowserSortColumn.Revenue,     2),
            ("Users /mo",  ProductBrowserSortColumn.UsersPerMonth, 2),
            ("Trend",      ProductBrowserSortColumn.Name,          1),
        };

        for (int i = 0; i < columns.Length; i++) {
            var (label, sortCol, flex) = columns[i];
            var btn = new Button { text = label };
            btn.AddToClassList("column-header__cell");
            btn.style.flexGrow = flex;
            btn.style.flexBasis = 0;
            btn.style.backgroundColor = new StyleColor(UnityEngine.Color.clear);
            btn.style.borderTopWidth = 0;
            btn.style.borderBottomWidth = 0;
            btn.style.borderLeftWidth = 0;
            btn.style.borderRightWidth = 0;
            var capturedCol = sortCol;
            btn.clicked += () => {
                _viewModel?.SetSort(capturedCol);
                Bind(_viewModel);
            };
            headerRow.Add(btn);
            _headerButtons.Add(btn);
        }
        content.Add(headerRow);
        _revenueHeaderBtn = _headerButtons[5];

        _listContainer = new VisualElement();
        _listPool = new ElementPool(CreateProductRow, _listContainer);
        content.Add(_listContainer);

        _root.Add(scroll);
    }

    public void Bind(IViewModel viewModel) {
        _viewModel = viewModel as ProductsBrowserViewModel;
        if (_viewModel == null) return;

        _listPool.UpdateList(_viewModel.AllProducts, BindProductRow);

        int count = _viewModel.AllProducts != null ? _viewModel.AllProducts.Count : 0;
        _resultCountLabel.text = count + " product" + (count != 1 ? "s" : "");

        if (_summaryRevenueLabel != null) _summaryRevenueLabel.text = _viewModel.SummaryMonthlyRevenue ?? "--";
        if (_summaryMaintenanceLabel != null) _summaryMaintenanceLabel.text = _viewModel.SummaryMaintenanceCost ?? "--";
        if (_summaryNetLabel != null) _summaryNetLabel.text = _viewModel.SummaryNetIncome ?? "--";

        int ownerCount = _ownerPills.Count;
        for (int i = 0; i < ownerCount; i++) {
            if (_ownerEnums[i] == _viewModel.CurrentOwner)
                _ownerPills[i].AddToClassList("sub-tab--active");
            else
                _ownerPills[i].RemoveFromClassList("sub-tab--active");
        }

        int statusCount = _statusPills.Count;
        for (int i = 0; i < statusCount; i++) {
            if (_statusEnums[i] == _viewModel.CurrentStatus)
                _statusPills[i].AddToClassList("sub-tab--active");
            else
                _statusPills[i].RemoveFromClassList("sub-tab--active");
        }
    }

    public void Dispose() {
        _viewModel = null;
        _listPool = null;
        _headerButtons.Clear();
        _ownerPills.Clear();
        _statusPills.Clear();
        _summaryRevenueLabel = null;
        _summaryMaintenanceLabel = null;
        _summaryNetLabel = null;
        _summaryCard = null;
        _revenueHeaderBtn = null;
    }

    private void OnOwnerFilterClicked(OwnerFilter filter) {
        if (_viewModel == null) return;
        _viewModel.SetOwnerFilter(filter);
        Bind(_viewModel);
    }

    private void OnStatusFilterClicked(StatusFilter filter) {
        if (_viewModel == null) return;
        _viewModel.SetStatusFilter(filter);
        if (_revenueHeaderBtn != null)
            _revenueHeaderBtn.text = filter == StatusFilter.Archived ? "Lifetime Rev."
                                   : filter == StatusFilter.All      ? "Revenue"
                                   :                                    "Monthly Rev.";
        Bind(_viewModel);
    }

    private static Label CreateSummaryCell(VisualElement parent, string labelText) {
        var cell = new VisualElement();
        cell.AddToClassList("card");
        cell.style.flexGrow = 1;
        cell.style.marginRight = 8;

        var lbl = new Label(labelText);
        lbl.AddToClassList("text-sm");
        lbl.AddToClassList("text-muted");
        cell.Add(lbl);

        var val = new Label("--");
        val.AddToClassList("text-bold");
        cell.Add(val);

        parent.Add(cell);
        return val;
    }

    private VisualElement CreateProductRow() {
        var row = new VisualElement();
        row.AddToClassList("list-item");
        row.RegisterCallback<ClickEvent>(OnProductRowClicked);

        var nameLabel = new Label();
        nameLabel.name = "pb-name";
        nameLabel.style.flexGrow = 3;
        nameLabel.style.flexBasis = 0;
        row.Add(nameLabel);

        var companyLabel = new Label();
        companyLabel.name = "pb-company";
        companyLabel.AddToClassList("text-muted");
        companyLabel.style.flexGrow = 2;
        companyLabel.style.flexBasis = 0;
        row.Add(companyLabel);

        var nicheLabel = new Label();
        nicheLabel.name = "pb-niche";
        nicheLabel.AddToClassList("metric-tertiary");
        nicheLabel.style.flexGrow = 2;
        nicheLabel.style.flexBasis = 0;
        row.Add(nicheLabel);

        var qualityLabel = new Label();
        qualityLabel.name = "pb-quality";
        qualityLabel.AddToClassList("metric-secondary");
        qualityLabel.style.flexGrow = 1;
        qualityLabel.style.flexBasis = 0;
        row.Add(qualityLabel);

        var usersLabel = new Label();
        usersLabel.name = "pb-users";
        usersLabel.AddToClassList("metric-secondary");
        usersLabel.style.flexGrow = 2;
        usersLabel.style.flexBasis = 0;
        row.Add(usersLabel);

        var revenueLabel = new Label();
        revenueLabel.name = "pb-revenue";
        revenueLabel.AddToClassList("metric-secondary");
        revenueLabel.style.flexGrow = 2;
        revenueLabel.style.flexBasis = 0;
        row.Add(revenueLabel);

        var shareLabel = new Label();
        shareLabel.name = "pb-share";
        shareLabel.AddToClassList("metric-tertiary");
        shareLabel.style.flexGrow = 1;
        shareLabel.style.flexBasis = 0;
        row.Add(shareLabel);

        var stageLabel = new Label();
        stageLabel.name = "pb-stage";
        stageLabel.AddToClassList("text-sm");
        stageLabel.style.flexGrow = 1;
        stageLabel.style.flexBasis = 0;
        row.Add(stageLabel);

        return row;
    }

    private static void BindProductRow(VisualElement el, BrowserProductRowVM data) {
        el.Q<Label>("pb-name").text = data.ProductName;
        el.Q<Label>("pb-company").text = data.CompanyName;
        el.Q<Label>("pb-niche").text = data.Niche;
        el.Q<Label>("pb-quality").text = data.ReviewScore;
        el.Q<Label>("pb-users").text = data.SalesPerMonth;
        el.Q<Label>("pb-revenue").text = data.Revenue;
        el.Q<Label>("pb-share").text = data.UsersPerMonth;
        el.Q<Label>("pb-stage").text = data.UserTrend;
        el.userData = data.Id;
    }

    private void OnProductRowClicked(ClickEvent evt) {
        var el = evt.currentTarget as VisualElement;
        if (el == null || !(el.userData is ProductId id)) return;
        if (_viewModel == null || _modal == null) return;
        var detailVM = new ProductDetailViewModel();
        _viewModel.RefreshProductDetail(detailVM, id);
        var detailView = new ProductDetailView(_modal, _navigation);
        _modal.ShowModal(detailView, detailVM);
    }
}
