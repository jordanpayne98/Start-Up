using System.Collections.Generic;
using UnityEngine.UIElements;

public class ProductDetailView : IGameView
{
    private readonly IModalPresenter _modal;
    private readonly INavigationService _navigation;
    private VisualElement _root;
    private ProductDetailViewModel _vm;

    private Label _productNameLabel;
    private Label _companyLabel;
    private Label _nicheLabel;
    private Label _qualityLabel;
    private Label _featuresLabel;
    private Label _salesPerMonthLabel;
    private Label _usersPerMonthLabel;
    private Label _revenueLabel;
    private Label _monthlyRevenueLabel;
    private Label _lifetimeSalesLabel;
    private Label _launchDateLabel;
    private Label _lifecycleLabel;
    private Label _maintenanceLabel;
    private Label _crisisLabel;
    private VisualElement _crisisRow;
    private VisualElement _teamSection;
    private Label _teamAssignmentsLabel;
    private Button _viewInBrowserButton;
    private Button _closeButton;
    private ElementPool _teamPool;
    private VisualElement _teamContainer;

    private VisualElement _budgetSection;
    private Label _maintBudgetLabel;
    private Label _maintDrainLabel;
    private Label _maintCoverageLabel;
    private Label _mktBudgetLabel;
    private Label _mktDrainLabel;
    private Label _mktCoverageLabel;

    private VisualElement _reviewSection;
    private Label _reviewAggregateLabel;
    private Label _reviewRatingLabel;
    private VisualElement _dimensionContainer;
    private VisualElement _outletContainer;
    private ElementPool _dimensionPool;
    private ElementPool _outletPool;

    // ── Market Identity ────────────────────────────────────────────────────────
    private VisualElement _identityContainer;
    private Label _shipTag1;
    private Label _shipTag2;
    private Label _shipTag3;
    private Label _currentTag1;
    private Label _currentTag2;
    private Label _currentTag3;
    private VisualElement _identityShifts;
    private readonly List<Label> _shiftLabelPool = new List<Label>();
    private const int MaxShiftLabels = 5;

    private readonly List<string> _scratchTeams = new List<string>();

    public ProductDetailView(IModalPresenter modal, INavigationService navigation) {
        _modal = modal;
        _navigation = navigation;
    }

    public void Initialize(VisualElement root, UIServices services) {
        _root = root;
        _root.AddToClassList("product-detail-modal");

        // Header
        var header = new VisualElement();
        header.AddToClassList("modal-header");
        header.style.flexDirection = FlexDirection.Row;
        header.style.justifyContent = Justify.SpaceBetween;
        header.style.alignItems = Align.Center;

        _productNameLabel = new Label("Product");
        _productNameLabel.AddToClassList("text-xl");
        _productNameLabel.AddToClassList("text-bold");
        header.Add(_productNameLabel);

        _closeButton = new Button { text = "X" };
        _closeButton.AddToClassList("btn-sm");
        _closeButton.style.minWidth = 30;
        header.Add(_closeButton);
        _root.Add(header);

        // Body scroll
        var bodyScroll = new ScrollView();
        bodyScroll.AddToClassList("modal-body");
        bodyScroll.style.flexGrow = 1;
        bodyScroll.style.flexShrink = 1;
        var body = bodyScroll.contentContainer;
        _root.Add(bodyScroll);

        // Owner card
        var ownerCard = new VisualElement();
        ownerCard.AddToClassList("card");
        ownerCard.style.marginBottom = 12;
        _companyLabel = CreateInfoRow(ownerCard, "Company");
        _nicheLabel = CreateInfoRow(ownerCard, "Niche");
        _qualityLabel = CreateInfoRow(ownerCard, "Review Score");
        _lifecycleLabel = CreateInfoRow(ownerCard, "Trend");
        _maintenanceLabel = CreateInfoRow(ownerCard, "Maintenance");
        body.Add(ownerCard);

        // Market card
        var marketCard = new VisualElement();
        marketCard.AddToClassList("card");
        marketCard.style.marginBottom = 12;
        var marketTitle = new Label("Market Performance");
        marketTitle.AddToClassList("text-bold");
        marketTitle.style.marginBottom = 8;
        marketCard.Add(marketTitle);
        _salesPerMonthLabel = CreateInfoRow(marketCard, "Sales /mo");
        _usersPerMonthLabel = CreateInfoRow(marketCard, "Users /mo");
        _revenueLabel = CreateInfoRow(marketCard, "Lifetime Revenue");
        _monthlyRevenueLabel = CreateInfoRow(marketCard, "Monthly Revenue");
        _lifetimeSalesLabel = CreateInfoRow(marketCard, "Lifetime Sales");
        _launchDateLabel = CreateInfoRow(marketCard, "Launch");
        body.Add(marketCard);

        // Features card
        var featuresCard = new VisualElement();
        featuresCard.AddToClassList("card");
        featuresCard.style.marginBottom = 12;
        var featuresTitle = new Label("Features");
        featuresTitle.AddToClassList("text-bold");
        featuresTitle.style.marginBottom = 4;
        featuresCard.Add(featuresTitle);
        _featuresLabel = new Label();
        _featuresLabel.AddToClassList("text-sm");
        _featuresLabel.AddToClassList("text-muted");
        _featuresLabel.style.whiteSpace = WhiteSpace.Normal;
        featuresCard.Add(_featuresLabel);
        body.Add(featuresCard);

        // Review card
        _reviewSection = new VisualElement();
        _reviewSection.AddToClassList("card");
        _reviewSection.style.marginBottom = 12;

        var reviewHeader = new VisualElement();
        reviewHeader.style.flexDirection = FlexDirection.Row;
        reviewHeader.style.justifyContent = Justify.SpaceBetween;
        reviewHeader.style.alignItems = Align.Center;
        reviewHeader.style.marginBottom = 8;
        var reviewTitle = new Label("Reviews");
        reviewTitle.AddToClassList("text-bold");
        reviewHeader.Add(reviewTitle);
        var reviewScoreRow = new VisualElement();
        reviewScoreRow.style.flexDirection = FlexDirection.Row;
        reviewScoreRow.style.alignItems = Align.Center;
        _reviewAggregateLabel = new Label("--");
        _reviewAggregateLabel.AddToClassList("text-xl");
        _reviewAggregateLabel.AddToClassList("text-bold");
        _reviewAggregateLabel.style.marginRight = 6;
        reviewScoreRow.Add(_reviewAggregateLabel);
        _reviewRatingLabel = new Label("--");
        _reviewRatingLabel.AddToClassList("text-sm");
        _reviewRatingLabel.AddToClassList("text-muted");
        reviewScoreRow.Add(_reviewRatingLabel);
        reviewHeader.Add(reviewScoreRow);
        _reviewSection.Add(reviewHeader);

        var dimTitle = new Label("Dimension Breakdown");
        dimTitle.AddToClassList("text-sm");
        dimTitle.AddToClassList("text-muted");
        dimTitle.style.marginBottom = 6;
        _reviewSection.Add(dimTitle);

        _dimensionContainer = new VisualElement();
        _dimensionPool = new ElementPool(CreateDimensionRow, _dimensionContainer);
        _reviewSection.Add(_dimensionContainer);

        var outletTitle = new Label("Outlet Reviews");
        outletTitle.AddToClassList("text-sm");
        outletTitle.AddToClassList("text-muted");
        outletTitle.style.marginTop = 10;
        outletTitle.style.marginBottom = 6;
        _reviewSection.Add(outletTitle);

        _outletContainer = new VisualElement();
        _outletPool = new ElementPool(CreateOutletRow, _outletContainer);
        _reviewSection.Add(_outletContainer);

        body.Add(_reviewSection);

        // Market Identity card
        _identityContainer = new VisualElement();
        _identityContainer.AddToClassList("identity-container");

        var idTitle = new Label("Market Identity");
        idTitle.AddToClassList("identity__title");
        _identityContainer.Add(idTitle);

        var shipLabel = new Label("At Launch");
        shipLabel.AddToClassList("identity__section-label");
        _identityContainer.Add(shipLabel);

        var shipTagsRow = new VisualElement();
        shipTagsRow.AddToClassList("identity__tags-row");
        _shipTag1 = new Label(); _shipTag1.AddToClassList("identity__tag");
        _shipTag2 = new Label(); _shipTag2.AddToClassList("identity__tag");
        _shipTag3 = new Label(); _shipTag3.AddToClassList("identity__tag");
        shipTagsRow.Add(_shipTag1);
        shipTagsRow.Add(_shipTag2);
        shipTagsRow.Add(_shipTag3);
        _identityContainer.Add(shipTagsRow);

        var currentLabel = new Label("Current");
        currentLabel.AddToClassList("identity__section-label");
        _identityContainer.Add(currentLabel);

        var currentTagsRow = new VisualElement();
        currentTagsRow.AddToClassList("identity__tags-row");
        _currentTag1 = new Label(); _currentTag1.AddToClassList("identity__tag");
        _currentTag2 = new Label(); _currentTag2.AddToClassList("identity__tag");
        _currentTag3 = new Label(); _currentTag3.AddToClassList("identity__tag");
        currentTagsRow.Add(_currentTag1);
        currentTagsRow.Add(_currentTag2);
        currentTagsRow.Add(_currentTag3);
        _identityContainer.Add(currentTagsRow);

        _identityShifts = new VisualElement();
        _identityShifts.AddToClassList("identity__shifts");
        for (int s = 0; s < MaxShiftLabels; s++) {
            var sl = new Label();
            sl.AddToClassList("identity__shift-label");
            sl.style.display = DisplayStyle.None;
            _shiftLabelPool.Add(sl);
            _identityShifts.Add(sl);
        }
        _identityContainer.Add(_identityShifts);

        body.Add(_identityContainer);

        // Crisis row
        _crisisRow = new VisualElement();
        _crisisRow.AddToClassList("card");
        _crisisRow.style.marginBottom = 12;
        _crisisRow.style.backgroundColor = new UnityEngine.UIElements.StyleColor(new UnityEngine.Color(0.4f, 0.08f, 0.08f));
        var crisisTitle = new Label("Active Crisis");
        crisisTitle.AddToClassList("text-bold");
        crisisTitle.AddToClassList("text-danger");
        crisisTitle.style.marginBottom = 4;
        _crisisRow.Add(crisisTitle);
        _crisisLabel = new Label();
        _crisisLabel.AddToClassList("text-sm");
        _crisisRow.Add(_crisisLabel);
        body.Add(_crisisRow);

        // Budget section (player-owned shipped products only)
        _budgetSection = new VisualElement();
        _budgetSection.AddToClassList("card");
        _budgetSection.style.marginBottom = 12;

        var budgetTitle = new Label("Budgets");
        budgetTitle.AddToClassList("text-bold");
        budgetTitle.style.marginBottom = 8;
        _budgetSection.Add(budgetTitle);

        var maintHeader = new Label("Maintenance");
        maintHeader.AddToClassList("text-sm");
        maintHeader.AddToClassList("text-muted");
        maintHeader.style.marginBottom = 4;
        _budgetSection.Add(maintHeader);
        _maintBudgetLabel = CreateInfoRow(_budgetSection, "Monthly Budget");
        _maintDrainLabel = CreateInfoRow(_budgetSection, "Monthly Cost");
        _maintCoverageLabel = CreateInfoRow(_budgetSection, "Status");

        _budgetSection.style.marginTop = 8;
        var mktHeader = new Label("Marketing");
        mktHeader.AddToClassList("text-sm");
        mktHeader.AddToClassList("text-muted");
        mktHeader.style.marginTop = 8;
        mktHeader.style.marginBottom = 4;
        _budgetSection.Add(mktHeader);
        _mktBudgetLabel = CreateInfoRow(_budgetSection, "Monthly Budget");
        _mktDrainLabel = CreateInfoRow(_budgetSection, "Monthly Cost");
        _mktCoverageLabel = CreateInfoRow(_budgetSection, "Status");

        body.Add(_budgetSection);

        // Team section (player products only)
        _teamSection = new VisualElement();
        _teamSection.AddToClassList("card");
        _teamSection.style.marginBottom = 12;
        var teamTitle = new Label("Team Assignments");
        teamTitle.AddToClassList("text-bold");
        teamTitle.style.marginBottom = 4;
        _teamSection.Add(teamTitle);
        _teamContainer = new VisualElement();
        _teamPool = new ElementPool(CreateTeamRow, _teamContainer);
        _teamSection.Add(_teamContainer);
        body.Add(_teamSection);

        // Footer
        var footer = new VisualElement();
        footer.AddToClassList("modal-footer");
        footer.style.flexDirection = FlexDirection.Row;
        footer.style.justifyContent = Justify.SpaceBetween;
        _root.Add(footer);

        _viewInBrowserButton = new Button { text = "View in Browser" };
        _viewInBrowserButton.AddToClassList("btn-secondary");
        footer.Add(_viewInBrowserButton);

        _closeButton.clicked += OnCloseClicked;
        _viewInBrowserButton.clicked += OnViewInBrowserClicked;
    }

    public void Bind(IViewModel viewModel) {
        _vm = viewModel as ProductDetailViewModel;
        if (_vm == null) return;

        _productNameLabel.text = _vm.ProductName;
        _companyLabel.text = _vm.CompanyName;
        _nicheLabel.text = _vm.Niche;
        _qualityLabel.text = _vm.Quality;
        _lifecycleLabel.text = _vm.UserTrend;
        _maintenanceLabel.text = _vm.MaintenanceStatus;
        _salesPerMonthLabel.text = _vm.SalesPerMonth;
        _usersPerMonthLabel.text = _vm.UsersPerMonth;
        _revenueLabel.text = _vm.LifetimeRevenue;
        _monthlyRevenueLabel.text = _vm.MonthlyRevenue;
        _lifetimeSalesLabel.text = _vm.LifetimeSales;
        _launchDateLabel.text = _vm.LaunchDate;
        _featuresLabel.text = _vm.FeatureList;

        _crisisRow.style.display = _vm.HasCrisis ? DisplayStyle.Flex : DisplayStyle.None;
        if (_vm.HasCrisis) _crisisLabel.text = _vm.CrisisDescription;

        bool showBudget = _vm.IsPlayerOwned;
        _budgetSection.style.display = showBudget ? DisplayStyle.Flex : DisplayStyle.None;
        if (showBudget) {
            _maintBudgetLabel.text = _vm.MaintenanceBudgetMonthly;
            _maintDrainLabel.text = _vm.MaintenanceDrainRate;
            _maintCoverageLabel.text = _vm.MaintenanceMonthsCoverage;
            _mktBudgetLabel.text = _vm.MarketingBudgetMonthly;
            _mktDrainLabel.text = _vm.MarketingDrainRate;
            _mktCoverageLabel.text = _vm.MarketingMonthsCoverage;
        }

        _teamSection.style.display = _vm.IsPlayerOwned ? DisplayStyle.Flex : DisplayStyle.None;
        if (_vm.IsPlayerOwned && _vm.TeamAssignments != null) {
            _teamPool.UpdateList(_vm.TeamAssignments, BindTeamRow);
        }

        BindReviewSection(_vm.ReviewVM);
        BindIdentitySection();
    }

    private void BindIdentitySection() {
        if (_identityContainer == null || _vm == null) return;
        if (!_vm.HasIdentity) {
            _identityContainer.style.display = DisplayStyle.None;
            return;
        }
        _identityContainer.style.display = DisplayStyle.Flex;

        SetTagLabel(_shipTag1, _vm.ShipTag1);
        SetTagLabel(_shipTag2, _vm.ShipTag2);
        SetTagLabel(_shipTag3, _vm.ShipTag3);
        SetTagLabel(_currentTag1, _vm.CurrentTag1);
        SetTagLabel(_currentTag2, _vm.CurrentTag2);
        SetTagLabel(_currentTag3, _vm.CurrentTag3);

        int shiftCount = _vm.ShiftLabels != null ? _vm.ShiftLabels.Length : 0;
        bool hasShifts = shiftCount > 0;
        _identityShifts.style.display = hasShifts ? DisplayStyle.Flex : DisplayStyle.None;
        int poolCount = _shiftLabelPool.Count;
        for (int i = 0; i < poolCount; i++) {
            if (i < shiftCount) {
                _shiftLabelPool[i].text = _vm.ShiftLabels[i];
                _shiftLabelPool[i].style.display = DisplayStyle.Flex;
            } else {
                _shiftLabelPool[i].style.display = DisplayStyle.None;
            }
        }
    }

    private void SetTagLabel(Label label, string text) {
        if (label == null) return;
        bool hasText = !string.IsNullOrEmpty(text);
        label.style.display = hasText ? DisplayStyle.Flex : DisplayStyle.None;
        if (hasText) label.text = text;
    }

    private void BindReviewSection(ProductReviewViewModel reviewVm) {
        if (reviewVm == null || !reviewVm.HasReview) {
            _reviewSection.style.display = DisplayStyle.None;
            return;
        }

        _reviewSection.style.display = DisplayStyle.Flex;
        _reviewAggregateLabel.text = reviewVm.AggregateScoreLabel;
        _reviewRatingLabel.text = reviewVm.AggregateScoreRating;

        _dimensionPool.UpdateList(reviewVm.DimensionScores, BindDimensionRow);
        _outletPool.UpdateList(reviewVm.OutletScores, BindOutletRow);
    }

    public void Dispose() {
        if (_closeButton != null) _closeButton.clicked -= OnCloseClicked;
        if (_viewInBrowserButton != null) _viewInBrowserButton.clicked -= OnViewInBrowserClicked;
        _teamPool = null;
        _dimensionPool = null;
        _outletPool = null;
        _budgetSection = null;
        _maintBudgetLabel = null;
        _maintDrainLabel = null;
        _maintCoverageLabel = null;
        _mktBudgetLabel = null;
        _mktDrainLabel = null;
        _mktCoverageLabel = null;
        _monthlyRevenueLabel = null;
        _lifetimeSalesLabel = null;
        _identityContainer = null;
        _shipTag1 = null; _shipTag2 = null; _shipTag3 = null;
        _currentTag1 = null; _currentTag2 = null; _currentTag3 = null;
        _identityShifts = null;
        _shiftLabelPool.Clear();
        _vm = null;
    }

    private void OnCloseClicked() {
        _modal?.DismissModal();
    }

    private void OnViewInBrowserClicked() {
        _modal?.DismissModal();
        _navigation?.NavigateTo(ScreenId.MarketProductsBrowser);
    }

    private Label CreateInfoRow(VisualElement parent, string labelText) {
        var row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        row.style.justifyContent = Justify.SpaceBetween;
        row.style.marginBottom = 4;

        var label = new Label(labelText);
        label.AddToClassList("text-muted");
        label.AddToClassList("text-sm");
        row.Add(label);

        var value = new Label("--");
        value.AddToClassList("text-bold");
        value.AddToClassList("text-sm");
        row.Add(value);

        parent.Add(row);
        return value;
    }

    private VisualElement CreateTeamRow() {
        var row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        row.style.marginBottom = 2;

        var label = new Label();
        label.name = "team-label";
        label.AddToClassList("text-sm");
        label.AddToClassList("text-muted");
        row.Add(label);

        return row;
    }

    private void BindTeamRow(VisualElement el, string data) {
        var label = el.Q<Label>("team-label");
        if (label != null) label.text = data;
    }

    private VisualElement CreateDimensionRow() {
        var row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        row.style.alignItems = Align.Center;
        row.style.marginBottom = 4;

        var nameLabel = new Label();
        nameLabel.name = "dim-name";
        nameLabel.AddToClassList("text-sm");
        nameLabel.style.width = 110;
        row.Add(nameLabel);

        var barBg = new VisualElement();
        barBg.name = "dim-bar-bg";
        barBg.AddToClassList("review-bar-bg");
        barBg.style.flexGrow = 1;
        barBg.style.height = 8;
        barBg.style.backgroundColor = new UnityEngine.UIElements.StyleColor(new UnityEngine.Color(0.2f, 0.2f, 0.2f));
        barBg.style.marginLeft = 6;
        barBg.style.marginRight = 6;
        barBg.style.borderTopLeftRadius = 4;
        barBg.style.borderTopRightRadius = 4;
        barBg.style.borderBottomLeftRadius = 4;
        barBg.style.borderBottomRightRadius = 4;

        var barFill = new VisualElement();
        barFill.name = "dim-bar-fill";
        barFill.AddToClassList("review-bar-fill");
        barFill.style.height = UnityEngine.UIElements.Length.Percent(100);
        barFill.style.borderTopLeftRadius = 4;
        barFill.style.borderTopRightRadius = 4;
        barFill.style.borderBottomLeftRadius = 4;
        barFill.style.borderBottomRightRadius = 4;
        barBg.Add(barFill);
        row.Add(barBg);

        var scoreLabel = new Label();
        scoreLabel.name = "dim-score";
        scoreLabel.AddToClassList("text-sm");
        scoreLabel.AddToClassList("text-bold");
        scoreLabel.style.width = 28;
        row.Add(scoreLabel);

        return row;
    }

    private void BindDimensionRow(VisualElement el, DimensionScoreData data) {
        var nameLabel = el.Q<Label>("dim-name");
        var barFill = el.Q<VisualElement>("dim-bar-fill");
        var scoreLabel = el.Q<Label>("dim-score");

        if (nameLabel != null) nameLabel.text = data.DimensionName;
        if (scoreLabel != null) scoreLabel.text = data.ScoreLabel;

        if (barFill != null) {
            barFill.style.width = UnityEngine.UIElements.Length.Percent(data.ScoreNormalized * 100f);
            int score = 0;
            int.TryParse(data.ScoreLabel, out score);
            string ratingClass = ProductReviewViewModel.GetRatingUssClass(score);
            barFill.ClearClassList();
            barFill.AddToClassList("review-bar-fill");
            barFill.AddToClassList(ratingClass);
        }
    }

    private VisualElement CreateOutletRow() {
        var row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        row.style.justifyContent = Justify.SpaceBetween;
        row.style.marginBottom = 4;

        var nameLabel = new Label();
        nameLabel.name = "outlet-name";
        nameLabel.AddToClassList("text-sm");
        nameLabel.style.width = 90;
        row.Add(nameLabel);

        var styleLabel = new Label();
        styleLabel.name = "outlet-style";
        styleLabel.AddToClassList("text-sm");
        styleLabel.AddToClassList("text-muted");
        styleLabel.style.flexGrow = 1;
        row.Add(styleLabel);

        var scoreLabel = new Label();
        scoreLabel.name = "outlet-score";
        scoreLabel.AddToClassList("text-sm");
        scoreLabel.AddToClassList("text-bold");
        scoreLabel.style.width = 28;
        row.Add(scoreLabel);

        var topDimLabel = new Label();
        topDimLabel.name = "outlet-top";
        topDimLabel.AddToClassList("text-sm");
        topDimLabel.AddToClassList("text-muted");
        topDimLabel.style.width = 90;
        row.Add(topDimLabel);

        return row;
    }

    private void BindOutletRow(VisualElement el, OutletScoreData data) {
        var nameLabel = el.Q<Label>("outlet-name");
        var styleLabel = el.Q<Label>("outlet-style");
        var scoreLabel = el.Q<Label>("outlet-score");
        var topDimLabel = el.Q<Label>("outlet-top");

        if (nameLabel != null) nameLabel.text = data.OutletName;
        if (styleLabel != null) styleLabel.text = data.OutletStyle;
        if (scoreLabel != null) scoreLabel.text = data.ScoreLabel;
        if (topDimLabel != null) topDimLabel.text = "Best: " + data.TopDimension;
    }
}
