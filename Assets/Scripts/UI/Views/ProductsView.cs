using System.Collections.Generic;
using DG.Tweening;
using UnityEngine.UIElements;

public class ProductsView : IGameView
{
    private readonly ICommandDispatcher _dispatcher;
    private readonly IModalPresenter _modal;
    private readonly ITooltipProvider _tooltipProvider;
    private VisualElement _root;
    private ProductsViewModel _viewModel;

    // ── Tab buttons ──────────────────────────────────────────────────────────
    private Button _tabDevBtn;
    private Button _tabShippedBtn;
    private VisualElement _devSection;
    private VisualElement _shippedSection;

    // Left column — dev product list
    private VisualElement _devContainer;
    private ElementPool _devPool;
    private VisualElement _devEmptyState;

    // Right column — detail panel
    private VisualElement _detailPanel;
    private VisualElement _detailEmptyState;
    private Label _detailNameLabel;
    private Label _detailTemplateLabel;
    private Label _detailDurationLabel;

    // Phase rows (pre-built, max 8)
    private const int MaxPhaseRows = 8;
    private VisualElement[] _phaseRows;
    private Label[] _phaseLabels;
    private VisualElement[] _phaseProgressFills;
    private Label[] _phaseQualityLabels;
    private Label[] _phaseBadges;
    private Label[] _phaseTeamLabels;
    private Button[] _phaseIterateButtons;
    private Button[] _phaseAssignButtons;
    private Label _devBugIndicator;
    private Label[] _phaseBugLabels;

    // Action buttons
    private Button _shipButton;
    private Button _abandonButton;

    // Release date panel
    private Label _releaseDateLabel;
    private Label _daysUntilReleaseLabel;
    private Label _shiftCountBadge;
    private Button _changeDateButton;
    private VisualElement _changeDateFlyout;
    private SliderInt _changeDateSlider;
    private Label _changeDateValueLabel;
    private Label _changeDatePenaltyLabel;
    private Button _changeDateConfirmBtn;
    private Button _setReleaseDateButton;
    private VisualElement _setReleaseDateFlyout;
    private SliderInt _setReleaseDateSlider;
    private Label _setReleaseDateValueLabel;
    private Button _setReleaseDateConfirmBtn;

    // ── Shipped panel ────────────────────────────────────────────────────────
    private VisualElement _shippedContainer;
    private ElementPool _shippedPool;
    private VisualElement _shippedEmptyState;
    private VisualElement _shippedDetailPanel;
    private VisualElement _shippedDetailEmptyState;

    // Shipped detail fields
    private Label _shippedDetailName;
    private Label _shippedDetailStageBadge;
    private Label _shippedDetailTypeLabel;
    private Label _shippedDetailInfoLabel;
    private VisualElement _shippedDetailPlatformGenreRow;
    private Label _shippedDetailUsers;
    private Label _shippedDetailUsersTrend;
    private Label _shippedDetailRevenue;
    private Label _shippedDetailRevenueTrend;
    private Label _shippedDetailTotalRevenue;
    private Label _shippedDetailProductionCost;
    private Label _shippedDetailPopularity;
    private Label _shippedDetailBugs;
    private VisualElement _shippedUpdateProgressFill;
    private Label _shippedUpdateProgressLabel;
    private VisualElement _shippedUpdateSection;
    private Toggle _shippedMaintenanceToggle;
    private Button _updateProductButton;
    private Button _removeFromMarketButton;
    private Button _sequelButton;

    // Marketing controls — shipped detail
    private VisualElement _shippedMarketingPanel;
    private VisualElement _shippedHypeBarFill;
    private Label _shippedHypeLabel;
    private Button _shippedStartMarketingBtn;
    private Button _shippedStopMarketingBtn;
    private Button _shippedRunAdsBtn;
    private Button _shippedAnnounceUpdateBtn;
    private Label _shippedAdStatusLabel;
    private VisualElement _shippedUpdateHypeBarFill;
    private Label _shippedUpdateHypeLabel;

    // Budget controls — shipped detail
    private VisualElement _shippedBudgetSection;
    private Label _shippedMaintBudgetLabel;
    private Label _shippedMaintDrainLabel;
    private Label _shippedMaintCoverageLabel;
    private Label _shippedMaintQualityLabel;
    private TextField _shippedMaintBudgetField;
    private Label _shippedMktBudgetLabel;
    private Label _shippedMktDrainLabel;
    private Label _shippedMktCoverageLabel;
    private TextField _shippedMktBudgetField;

    // Distribution model — shipped detail (Layer 1 tools only)
    private VisualElement _distributionPanel;
    private Label _distributionModelLabel;
    private Label _distributionLicenseeLabel;
    private Label _distributionRevenueLabel;
    private Slider _licensingRateSlider;
    private Label _licensingRateValueLabel;
    private Button _releaseToMarketBtn;
    private Button _openSourceBtn;
    private Button _pullFromMarketBtn;
    private Label _subscriberCountLabel;
    private Label _subscriptionRevenueLabel;
    private Label _totalSubscriptionRevenueLabel;
    private SliderInt _shippedSubscriptionPriceSlider;
    private Label _shippedSubscriptionPriceValueLabel;

    // Marketing controls — dev detail
    private VisualElement _devHypeBarFill;
    private Label _devHypeLabel;
    private Label _devMarketingTeamLabel;
    private TextField _devMktBudgetField;
    private Label _devMktBudgetAllocLabel;
    private Label _devMktBudgetStatusLabel;
    private Button _crunchAllBtn;

    // Shipped team assignment controls
    private Button _shippedAssignQABtn;
    private Button _shippedAssignMktBtn;
    private Label _shippedQATeamLabel;
    private Label _shippedMktTeamLabel;

    // Shipped selection
    private ProductId _selectedShippedId;
    private bool _hasShippedSelection;

    // Per-fill tween tracking
    private readonly Dictionary<VisualElement, float> _fillPercents = new Dictionary<VisualElement, float>();

    // Ship distribution flyout (tool products only)
    private VisualElement _shipDistributionFlyout;
    private ToolDistributionModel _pendingDistributionModel = ToolDistributionModel.Proprietary;
    private float _pendingLicensingRate = 0.15f;
    private Button _shipDistProprietaryBtn;
    private Button _shipDistLicensedBtn;
    private Button _shipDistOpenSourceBtn;
    private Slider _shipDistRateSlider;
    private Label _shipDistRateValueLabel;
    private VisualElement _shipDistRateRow;
    private Label _shipDistWarningLabel;
    private Button _shipConfirmBtn;    private readonly Dictionary<VisualElement, Tweener> _fillTweeners = new Dictionary<VisualElement, Tweener>();

    // Per-badge class tracking
    private readonly Dictionary<VisualElement, string> _badgeClasses = new Dictionary<VisualElement, string>();

    // Phase badge classes (by row index)
    private readonly string[] _phaseBadgeCurrentClass = new string[MaxPhaseRows];

    // Stagger scratch list — reused, never allocated in Bind
    private readonly List<VisualElement> _staggerScratch = new List<VisualElement>();

    // Stagger guard
    private bool _hasAnimatedIn;
    private bool _hasAnimatedInShipped;

    // Selection state
    private ProductId _selectedProductId;
    private bool _hasSelection;

    // Tab state
    private bool _showingShipped;

    // ── Create product mode ───────────────────────────────────────────────────
    private ProductCreationPlanningView _createProductView;
    private ProductCreationPlanningViewModel _createProductVm;
    private VisualElement _createProductContainer;
    private bool _isInCreateMode;
    private VisualElement _tabRow;
    private readonly ProductsViewMode _viewMode;

    public ProductsView(ICommandDispatcher dispatcher, IModalPresenter modal, ITooltipProvider tooltipProvider = null,
        ProductsViewMode viewMode = ProductsViewMode.InDevelopment)
    {
        _dispatcher = dispatcher;
        _modal = modal;
        _tooltipProvider = tooltipProvider;
        _viewMode = viewMode;
    }

    public void Initialize(VisualElement root)
    {
        _root = root;

        // ── Tab row — hidden; section shown via _viewMode ────────────────────
        _tabRow = new VisualElement();
        _tabRow.AddToClassList("tab-row");
        _tabRow.style.display = DisplayStyle.None;

        _tabDevBtn = new Button { text = "In Development" };
        _tabDevBtn.AddToClassList("tab-btn");
        _tabDevBtn.AddToClassList("tab-btn--active");
        _tabRow.Add(_tabDevBtn);

        _tabShippedBtn = new Button { text = "Live Products" };
        _tabShippedBtn.AddToClassList("tab-btn");
        _tabRow.Add(_tabShippedBtn);

        _root.Add(_tabRow);

        // ── Dev Section ─────────────────────────────────────────────────────
        _devSection = new VisualElement();
        _devSection.style.flexGrow = 1;
        _devSection.style.display = _viewMode == ProductsViewMode.InDevelopment ? DisplayStyle.Flex : DisplayStyle.None;
        BuildDevSection(_devSection);
        _root.Add(_devSection);

        // ── Shipped Section ─────────────────────────────────────────────────
        _shippedSection = new VisualElement();
        _shippedSection.style.flexGrow = 1;
        _shippedSection.style.display = _viewMode == ProductsViewMode.Live ? DisplayStyle.Flex : DisplayStyle.None;
        BuildShippedSection(_shippedSection);
        _root.Add(_shippedSection);

        // ── Create product container (built once, reused per attempt) ────────
        _createProductContainer = new VisualElement();
        _createProductContainer.style.flexGrow = 1;
        _createProductContainer.style.display = DisplayStyle.None;
        _root.Add(_createProductContainer);
    }

    // ─── Build Dev Section ───────────────────────────────────────────────────

    private void BuildDevSection(VisualElement parent)
    {
        var layout = new VisualElement();
        layout.AddToClassList("flex-row");
        layout.style.flexGrow = 1;

        // ── Left column: dev product list ────────────────────────────────────
        var leftPanel = new VisualElement();
        leftPanel.style.flexGrow = 1;
        leftPanel.style.flexBasis = 0;
        leftPanel.style.marginRight = 16;

        var header = new VisualElement();
        header.AddToClassList("flex-row");
        header.AddToClassList("justify-between");
        header.AddToClassList("align-center");
        header.style.marginBottom = 8;

        var title = new Label("Products in Development");
        title.AddToClassList("section-header");
        header.Add(title);

        var newProductBtn = new Button { text = "+ New Product" };
        newProductBtn.AddToClassList("btn-secondary");
        newProductBtn.AddToClassList("btn-sm");
        newProductBtn.clicked += OnNewProductClicked;
        header.Add(newProductBtn);
        leftPanel.Add(header);

        _devEmptyState = UICardHelper.CreateEmptyState("📦", "No products yet. Create one to get started!", "+ New Product", OnNewProductClicked);
        _devEmptyState.AddToClassList("empty-state--hidden");
        leftPanel.Add(_devEmptyState);

        var devScroll = new ScrollView();
        devScroll.style.flexGrow = 1;
        _devContainer = devScroll.contentContainer;
        _devPool = new ElementPool(CreateDevCard, _devContainer);
        leftPanel.Add(devScroll);

        layout.Add(leftPanel);

        // ── Right column: detail panel ────────────────────────────────────────
        var rightPanel = new VisualElement();
        rightPanel.style.flexGrow = 2;
        rightPanel.style.flexBasis = 0;

        _detailPanel = new VisualElement();
        _detailPanel.AddToClassList("product-detail");
        _detailPanel.style.display = DisplayStyle.None;

        var detailHeader = new VisualElement();
        detailHeader.AddToClassList("product-detail__header");

        _detailNameLabel = new Label();
        _detailNameLabel.AddToClassList("card__title");
        detailHeader.Add(_detailNameLabel);

        _detailTemplateLabel = new Label();
        _detailTemplateLabel.AddToClassList("card__subtitle");
        detailHeader.Add(_detailTemplateLabel);

        _detailDurationLabel = new Label();
        _detailDurationLabel.AddToClassList("metric-tertiary");
        detailHeader.Add(_detailDurationLabel);

        _devBugIndicator = new Label();
        _devBugIndicator.name = "dev-bug-indicator";
        _devBugIndicator.AddToClassList("metric-tertiary");
        detailHeader.Add(_devBugIndicator);

        _detailPanel.Add(detailHeader);

        // ── Dev hype bar ─────────────────────────────────────────────────────
        var devHypeContainer = new VisualElement();
        devHypeContainer.AddToClassList("hype-bar-container");
        devHypeContainer.style.marginBottom = 8;

        _devHypeLabel = new Label("No Marketing");
        _devHypeLabel.AddToClassList("hype-label");
        devHypeContainer.Add(_devHypeLabel);

        var devHypeTrack = new VisualElement();
        devHypeTrack.AddToClassList("hype-bar-track");
        _devHypeBarFill = new VisualElement();
        _devHypeBarFill.AddToClassList("hype-bar-fill");
        _devHypeBarFill.AddToClassList("hype-low");
        devHypeTrack.Add(_devHypeBarFill);
        devHypeContainer.Add(devHypeTrack);

        _detailPanel.Add(devHypeContainer);

        var devMktRow = new VisualElement();
        devMktRow.AddToClassList("flex-row");
        devMktRow.AddToClassList("align-center");
        devMktRow.style.marginBottom = 8;

        _devMarketingTeamLabel = new Label("No Marketing Team");
        _devMarketingTeamLabel.AddToClassList("metric-tertiary");
        _devMarketingTeamLabel.style.flexGrow = 1;
        devMktRow.Add(_devMarketingTeamLabel);

        _detailPanel.Add(devMktRow);

        // Dev marketing budget field
        var devMktBudgetSection = new VisualElement();
        devMktBudgetSection.style.marginBottom = 8;

        var devMktBudgetInputRow = new VisualElement();
        devMktBudgetInputRow.AddToClassList("flex-row");
        devMktBudgetInputRow.AddToClassList("align-center");
        devMktBudgetInputRow.style.marginBottom = 4;

        var devMktInputLabel = new Label("Marketing Budget ($/mo):");
        devMktInputLabel.AddToClassList("metric-tertiary");
        devMktInputLabel.style.marginRight = 8;
        devMktBudgetInputRow.Add(devMktInputLabel);

        _devMktBudgetField = new TextField();
        _devMktBudgetField.name = "dev-mkt-budget-input";
        _devMktBudgetField.style.width = 100;
        _devMktBudgetField.RegisterCallback<FocusOutEvent>(OnDevMktBudgetFocusOut);
        devMktBudgetInputRow.Add(_devMktBudgetField);

        devMktBudgetSection.Add(devMktBudgetInputRow);

        var devMktStatusRow = new VisualElement();
        devMktStatusRow.AddToClassList("flex-row");
        devMktStatusRow.style.marginBottom = 4;

        _devMktBudgetAllocLabel = new Label("--");
        _devMktBudgetAllocLabel.AddToClassList("metric-tertiary");
        _devMktBudgetAllocLabel.style.marginRight = 8;
        devMktStatusRow.Add(_devMktBudgetAllocLabel);

        _devMktBudgetStatusLabel = new Label("--");
        _devMktBudgetStatusLabel.AddToClassList("metric-tertiary");
        devMktStatusRow.Add(_devMktBudgetStatusLabel);

        devMktBudgetSection.Add(devMktStatusRow);
        _detailPanel.Add(devMktBudgetSection);

        var phaseSectionHeader = new Label("Development Phases");
        phaseSectionHeader.AddToClassList("section-header");
        phaseSectionHeader.style.marginBottom = 8;
        _detailPanel.Add(phaseSectionHeader);

        // Pre-build phase rows
        var phaseContainer = new VisualElement();
        _phaseRows = new VisualElement[MaxPhaseRows];
        _phaseLabels = new Label[MaxPhaseRows];
        _phaseProgressFills = new VisualElement[MaxPhaseRows];
        _phaseQualityLabels = new Label[MaxPhaseRows];
        _phaseBadges = new Label[MaxPhaseRows];
        _phaseTeamLabels = new Label[MaxPhaseRows];
        _phaseIterateButtons = new Button[MaxPhaseRows];
        _phaseAssignButtons = new Button[MaxPhaseRows];
        _phaseBugLabels = new Label[MaxPhaseRows];

        for (int i = 0; i < MaxPhaseRows; i++)
        {
            var row = BuildPhaseRow(i);
            _phaseRows[i] = row;
            row.style.display = DisplayStyle.None;
            phaseContainer.Add(row);
        }
        _detailPanel.Add(phaseContainer);

        var divider = new VisualElement();
        divider.AddToClassList("divider");
        _detailPanel.Add(divider);

        // ── Release Date section ─────────────────────────────────────────────
        var releaseDateSection = new VisualElement();
        releaseDateSection.style.marginTop = 8;
        releaseDateSection.style.marginBottom = 8;

        var releaseDateRow = new VisualElement();
        releaseDateRow.AddToClassList("flex-row");
        releaseDateRow.AddToClassList("align-center");
        releaseDateRow.style.marginBottom = 4;

        _releaseDateLabel = new Label("No release date");
        _releaseDateLabel.AddToClassList("metric-secondary");
        _releaseDateLabel.style.flexGrow = 1;
        releaseDateRow.Add(_releaseDateLabel);

        _changeDateButton = new Button { text = "Change Date" };
        _changeDateButton.AddToClassList("btn-secondary");
        _changeDateButton.AddToClassList("btn-sm");
        _changeDateButton.clicked += OnChangeDateClicked;
        releaseDateRow.Add(_changeDateButton);

        _setReleaseDateButton = new Button { text = "Set Release Date" };
        _setReleaseDateButton.AddToClassList("btn-primary");
        _setReleaseDateButton.AddToClassList("btn-sm");
        _setReleaseDateButton.style.marginLeft = 4;
        _setReleaseDateButton.clicked += OnSetReleaseDateClicked;
        releaseDateRow.Add(_setReleaseDateButton);

        releaseDateSection.Add(releaseDateRow);

        var daysRow = new VisualElement();
        daysRow.AddToClassList("flex-row");
        daysRow.AddToClassList("align-center");
        daysRow.style.marginBottom = 4;

        _daysUntilReleaseLabel = new Label("");
        _daysUntilReleaseLabel.AddToClassList("metric-tertiary");
        _daysUntilReleaseLabel.style.flexGrow = 1;
        daysRow.Add(_daysUntilReleaseLabel);

        _shiftCountBadge = new Label("");
        _shiftCountBadge.AddToClassList("badge");
        _shiftCountBadge.AddToClassList("badge--warning");
        _shiftCountBadge.style.display = DisplayStyle.None;
        daysRow.Add(_shiftCountBadge);

        releaseDateSection.Add(daysRow);

        _detailPanel.Add(releaseDateSection);

        // ── Change Date flyout ────────────────────────────────────────────────
        _changeDateFlyout = new VisualElement();
        _changeDateFlyout.AddToClassList("card");
        _changeDateFlyout.style.marginBottom = 8;
        _changeDateFlyout.style.display = DisplayStyle.None;

        var cdfHeader = new Label("Change Release Date");
        cdfHeader.AddToClassList("section-header");
        cdfHeader.style.marginBottom = 8;
        _changeDateFlyout.Add(cdfHeader);

        _changeDateValueLabel = new Label("Select a date");
        _changeDateValueLabel.AddToClassList("metric-primary");
        _changeDateValueLabel.AddToClassList("text-accent");
        _changeDateValueLabel.style.marginBottom = 4;
        _changeDateFlyout.Add(_changeDateValueLabel);

        _changeDateSlider = new SliderInt(1, 730);
        _changeDateSlider.SetValueWithoutNotify(180);
        _changeDateSlider.style.marginBottom = 4;
        _changeDateSlider.RegisterValueChangedCallback(OnChangeDateSliderChanged);
        _changeDateFlyout.Add(_changeDateSlider);

        _changeDatePenaltyLabel = new Label("");
        _changeDatePenaltyLabel.AddToClassList("text-warning");
        _changeDatePenaltyLabel.style.display = DisplayStyle.None;
        _changeDatePenaltyLabel.style.marginBottom = 8;
        _changeDateFlyout.Add(_changeDatePenaltyLabel);

        _changeDateConfirmBtn = new Button { text = "Confirm Change" };
        _changeDateConfirmBtn.AddToClassList("btn-primary");
        _changeDateConfirmBtn.clicked += OnChangeDateConfirmed;
        _changeDateFlyout.Add(_changeDateConfirmBtn);

        _detailPanel.Add(_changeDateFlyout);

        // ── Set Release Date flyout ───────────────────────────────────────────
        _setReleaseDateFlyout = new VisualElement();
        _setReleaseDateFlyout.AddToClassList("card");
        _setReleaseDateFlyout.style.marginBottom = 8;
        _setReleaseDateFlyout.style.display = DisplayStyle.None;

        var srdfHeader = new Label("Set Release Date");
        srdfHeader.AddToClassList("section-header");
        srdfHeader.style.marginBottom = 8;
        _setReleaseDateFlyout.Add(srdfHeader);

        _setReleaseDateValueLabel = new Label("Select a date");
        _setReleaseDateValueLabel.AddToClassList("metric-primary");
        _setReleaseDateValueLabel.AddToClassList("text-accent");
        _setReleaseDateValueLabel.style.marginBottom = 4;
        _setReleaseDateFlyout.Add(_setReleaseDateValueLabel);

        _setReleaseDateSlider = new SliderInt(30, 730);
        _setReleaseDateSlider.SetValueWithoutNotify(180);
        _setReleaseDateSlider.style.marginBottom = 4;
        _setReleaseDateSlider.RegisterValueChangedCallback(OnSetReleaseDateSliderChanged);
        _setReleaseDateFlyout.Add(_setReleaseDateSlider);

        _setReleaseDateConfirmBtn = new Button { text = "Announce Release Date" };
        _setReleaseDateConfirmBtn.AddToClassList("btn-primary");
        _setReleaseDateConfirmBtn.clicked += OnSetReleaseDateConfirmed;
        _setReleaseDateFlyout.Add(_setReleaseDateConfirmBtn);

        _detailPanel.Add(_setReleaseDateFlyout);

        var actionRow = new VisualElement();
        actionRow.AddToClassList("flex-row");
        actionRow.style.marginTop = 12;

        _shipButton = new Button { text = "Ship Product" };
        _shipButton.AddToClassList("btn-primary");
        _shipButton.clicked += OnShipClicked;
        actionRow.Add(_shipButton);

        _abandonButton = new Button { text = "Abandon" };
        _abandonButton.AddToClassList("btn-danger");
        _abandonButton.AddToClassList("btn-sm");
        _abandonButton.style.marginLeft = 8;
        _abandonButton.clicked += OnAbandonClicked;
        actionRow.Add(_abandonButton);

        _crunchAllBtn = new Button { text = "Crunch All Teams" };
        _crunchAllBtn.AddToClassList("btn-warning");
        _crunchAllBtn.AddToClassList("btn-sm");
        _crunchAllBtn.style.marginLeft = 8;
        _crunchAllBtn.RegisterCallback<ClickEvent>(OnCrunchAllClicked);
        actionRow.Add(_crunchAllBtn);

        _detailPanel.Add(actionRow);

        // ── Ship Distribution Flyout (tools only) ────────────────────────────
        _shipDistributionFlyout = new VisualElement();
        _shipDistributionFlyout.AddToClassList("card");
        _shipDistributionFlyout.style.marginTop = 8;
        _shipDistributionFlyout.style.display = DisplayStyle.None;

        var distFlyoutHeader = new Label("Distribution Model");
        distFlyoutHeader.AddToClassList("section-header");
        distFlyoutHeader.style.marginBottom = 8;
        _shipDistributionFlyout.Add(distFlyoutHeader);

        var distBtnRow = new VisualElement();
        distBtnRow.AddToClassList("flex-row");
        distBtnRow.style.marginBottom = 8;

        _shipDistProprietaryBtn = new Button { text = "Proprietary" };
        _shipDistProprietaryBtn.AddToClassList("btn-primary");
        _shipDistProprietaryBtn.AddToClassList("btn-sm");
        _shipDistProprietaryBtn.clicked += OnShipDistProprietaryClicked;
        distBtnRow.Add(_shipDistProprietaryBtn);

        _shipDistLicensedBtn = new Button { text = "Licensed" };
        _shipDistLicensedBtn.AddToClassList("btn-secondary");
        _shipDistLicensedBtn.AddToClassList("btn-sm");
        _shipDistLicensedBtn.style.marginLeft = 4;
        _shipDistLicensedBtn.clicked += OnShipDistLicensedClicked;
        distBtnRow.Add(_shipDistLicensedBtn);

        _shipDistOpenSourceBtn = new Button { text = "Open Source" };
        _shipDistOpenSourceBtn.AddToClassList("btn-secondary");
        _shipDistOpenSourceBtn.AddToClassList("btn-sm");
        _shipDistOpenSourceBtn.style.marginLeft = 4;
        _shipDistOpenSourceBtn.clicked += OnShipDistOpenSourceClicked;
        distBtnRow.Add(_shipDistOpenSourceBtn);

        _shipDistributionFlyout.Add(distBtnRow);

        _shipDistRateRow = new VisualElement();
        _shipDistRateRow.AddToClassList("flex-row");
        _shipDistRateRow.AddToClassList("align-center");
        _shipDistRateRow.style.marginBottom = 8;
        _shipDistRateRow.style.display = DisplayStyle.None;

        var rateLabel = new Label("Royalty Rate:");
        rateLabel.AddToClassList("metric-secondary");
        rateLabel.style.marginRight = 8;
        _shipDistRateRow.Add(rateLabel);

        _shipDistRateSlider = new Slider(5f, 30f);
        _shipDistRateSlider.SetValueWithoutNotify(15f);
        _shipDistRateSlider.style.flexGrow = 1;
        _shipDistRateSlider.RegisterValueChangedCallback(OnShipDistRateChanged);
        _shipDistRateRow.Add(_shipDistRateSlider);

        _shipDistRateValueLabel = new Label("15%");
        _shipDistRateValueLabel.AddToClassList("metric-primary");
        _shipDistRateValueLabel.style.marginLeft = 8;
        _shipDistRateValueLabel.style.minWidth = 36;
        _shipDistRateRow.Add(_shipDistRateValueLabel);
        _shipDistributionFlyout.Add(_shipDistRateRow);

        _shipDistWarningLabel = new Label("Open Source is permanent. This cannot be undone.");
        _shipDistWarningLabel.AddToClassList("text-warning");
        _shipDistWarningLabel.style.marginBottom = 8;
        _shipDistWarningLabel.style.display = DisplayStyle.None;
        _shipDistributionFlyout.Add(_shipDistWarningLabel);

        _shipConfirmBtn = new Button { text = "Confirm Ship" };
        _shipConfirmBtn.AddToClassList("btn-primary");
        _shipConfirmBtn.clicked += OnConfirmShipClicked;
        _shipDistributionFlyout.Add(_shipConfirmBtn);

        _detailPanel.Add(_shipDistributionFlyout);
        rightPanel.Add(_detailPanel);

        _detailEmptyState = UICardHelper.CreateEmptyState("👈", "Select a product to view details");
        rightPanel.Add(_detailEmptyState);

        layout.Add(rightPanel);
        parent.Add(layout);
    }

    // ─── Build Shipped Section ───────────────────────────────────────────────

    private void BuildShippedSection(VisualElement parent)
    {
        var layout = new VisualElement();
        layout.AddToClassList("flex-row");
        layout.style.flexGrow = 1;

        // ── Left: shipped product list ───────────────────────────────────────
        var leftPanel = new VisualElement();
        leftPanel.style.flexGrow = 1;
        leftPanel.style.flexBasis = 0;
        leftPanel.style.marginRight = 16;

        var header = new VisualElement();
        header.AddToClassList("flex-row");
        header.AddToClassList("align-center");
        header.style.marginBottom = 8;
        var title = new Label("Live Products");
        title.AddToClassList("section-header");
        header.Add(title);
        leftPanel.Add(header);

        _shippedEmptyState = UICardHelper.CreateEmptyState("🚀", "No live products yet. Ship one to start earning revenue!");
        _shippedEmptyState.AddToClassList("empty-state--hidden");
        leftPanel.Add(_shippedEmptyState);

        var shippedScroll = new ScrollView();
        shippedScroll.style.flexGrow = 1;
        _shippedContainer = shippedScroll.contentContainer;
        _shippedPool = new ElementPool(CreateShippedCard, _shippedContainer);
        leftPanel.Add(shippedScroll);

        layout.Add(leftPanel);

        // ── Right: shipped detail ────────────────────────────────────────────
        var rightPanel = new VisualElement();
        rightPanel.style.flexGrow = 2;
        rightPanel.style.flexBasis = 0;

        _shippedDetailPanel = new VisualElement();
        _shippedDetailPanel.AddToClassList("product-detail");
        _shippedDetailPanel.style.display = DisplayStyle.None;

        // Detail header
        var detailHeader = new VisualElement();
        detailHeader.AddToClassList("product-detail__header");

        _shippedDetailName = new Label();
        _shippedDetailName.AddToClassList("card__title");
        detailHeader.Add(_shippedDetailName);

        _shippedDetailStageBadge = new Label();
        _shippedDetailStageBadge.AddToClassList("badge");
        _shippedDetailStageBadge.style.marginLeft = 8;
        detailHeader.Add(_shippedDetailStageBadge);

        _shippedDetailPanel.Add(detailHeader);

        // Info section (type, platforms/genres, pricing)
        var infoSection = new VisualElement();
        infoSection.AddToClassList("product-detail__info");

        _shippedDetailTypeLabel = new Label();
        _shippedDetailTypeLabel.AddToClassList("card__subtitle");
        infoSection.Add(_shippedDetailTypeLabel);

        _shippedDetailPlatformGenreRow = new VisualElement();
        _shippedDetailPlatformGenreRow.AddToClassList("flex-row");
        _shippedDetailPlatformGenreRow.style.marginTop = 2;
        _shippedDetailInfoLabel = new Label();
        _shippedDetailInfoLabel.AddToClassList("metric-tertiary");
        _shippedDetailPlatformGenreRow.Add(_shippedDetailInfoLabel);
        infoSection.Add(_shippedDetailPlatformGenreRow);

        _shippedDetailPanel.Add(infoSection);

        // Metrics grid
        var metricsGrid = new VisualElement();
        metricsGrid.AddToClassList("metrics-grid");
        metricsGrid.style.marginTop = 8;
        metricsGrid.style.marginBottom = 8;

        _shippedDetailUsers = BuildMetricLabel(metricsGrid, "Active Users", out _shippedDetailUsersTrend);
        _shippedDetailRevenue = BuildMetricLabel(metricsGrid, "Monthly Revenue", out _shippedDetailRevenueTrend);
        _shippedDetailTotalRevenue = BuildMetricLabel(metricsGrid, "Total Revenue");
        _shippedDetailProductionCost = BuildMetricLabel(metricsGrid, "Production Cost");
        _shippedDetailPopularity = BuildMetricLabel(metricsGrid, "Popularity");
        _shippedDetailBugs = BuildMetricLabel(metricsGrid, "Bug Level");

        _shippedDetailPanel.Add(metricsGrid);

        // Update progress section (hidden when no update)
        _shippedUpdateSection = new VisualElement();
        _shippedUpdateSection.style.marginTop = 8;
        _shippedUpdateSection.style.display = DisplayStyle.None;

        _shippedUpdateProgressLabel = new Label();
        _shippedUpdateProgressLabel.AddToClassList("metric-tertiary");
        _shippedUpdateSection.Add(_shippedUpdateProgressLabel);

        var updateProgressBar = new VisualElement();
        updateProgressBar.AddToClassList("progress-bar");
        _shippedUpdateProgressFill = new VisualElement();
        _shippedUpdateProgressFill.AddToClassList("progress-bar__fill--accent");
        updateProgressBar.Add(_shippedUpdateProgressFill);
        _shippedUpdateSection.Add(updateProgressBar);

        _shippedDetailPanel.Add(_shippedUpdateSection);

        // ── Marketing panel ───────────────────────────────────────────────────
        _shippedMarketingPanel = new VisualElement();
        _shippedMarketingPanel.style.marginTop = 8;

        var hypeRow = new VisualElement();
        hypeRow.AddToClassList("hype-bar-container");

        _shippedHypeLabel = new Label("No Marketing");
        _shippedHypeLabel.AddToClassList("hype-label");
        hypeRow.Add(_shippedHypeLabel);

        var hypeTrack = new VisualElement();
        hypeTrack.AddToClassList("hype-bar-track");
        _shippedHypeBarFill = new VisualElement();
        _shippedHypeBarFill.AddToClassList("hype-bar-fill");
        _shippedHypeBarFill.AddToClassList("hype-low");
        hypeTrack.Add(_shippedHypeBarFill);
        hypeRow.Add(hypeTrack);

        _shippedMarketingPanel.Add(hypeRow);

        // Update hype row (shown when update is announced)
        var updateHypeRow = new VisualElement();
        updateHypeRow.AddToClassList("hype-bar-container");
        updateHypeRow.style.marginTop = 4;

        _shippedUpdateHypeLabel = new Label("");
        _shippedUpdateHypeLabel.AddToClassList("hype-label");
        updateHypeRow.Add(_shippedUpdateHypeLabel);

        var updateHypeTrack = new VisualElement();
        updateHypeTrack.AddToClassList("hype-bar-track");
        _shippedUpdateHypeBarFill = new VisualElement();
        _shippedUpdateHypeBarFill.AddToClassList("hype-bar-fill");
        _shippedUpdateHypeBarFill.AddToClassList("hype-low");
        updateHypeTrack.Add(_shippedUpdateHypeBarFill);
        updateHypeRow.Add(updateHypeTrack);

        _shippedMarketingPanel.Add(updateHypeRow);

        // Ad status
        _shippedAdStatusLabel = new Label("Ads Available");
        _shippedAdStatusLabel.AddToClassList("metric-tertiary");
        _shippedAdStatusLabel.style.marginTop = 4;
        _shippedMarketingPanel.Add(_shippedAdStatusLabel);

        // Marketing action buttons row
        var mktActionRow = new VisualElement();
        mktActionRow.AddToClassList("flex-row");
        mktActionRow.style.marginTop = 6;

        _shippedStartMarketingBtn = new Button { text = "Start Campaign" };
        _shippedStartMarketingBtn.AddToClassList("btn-secondary");
        _shippedStartMarketingBtn.AddToClassList("btn-sm");
        _shippedStartMarketingBtn.clicked += OnShippedStartMarketingClicked;
        mktActionRow.Add(_shippedStartMarketingBtn);

        _shippedStopMarketingBtn = new Button { text = "Stop Campaign" };
        _shippedStopMarketingBtn.AddToClassList("btn-danger");
        _shippedStopMarketingBtn.AddToClassList("btn-sm");
        _shippedStopMarketingBtn.style.marginLeft = 4;
        _shippedStopMarketingBtn.clicked += OnShippedStopMarketingClicked;
        mktActionRow.Add(_shippedStopMarketingBtn);

        _shippedRunAdsBtn = new Button { text = "Run Ads" };
        _shippedRunAdsBtn.AddToClassList("btn-secondary");
        _shippedRunAdsBtn.AddToClassList("btn-sm");
        _shippedRunAdsBtn.style.marginLeft = 4;
        _shippedRunAdsBtn.clicked += OnShippedRunAdsClicked;
        mktActionRow.Add(_shippedRunAdsBtn);

        _shippedAnnounceUpdateBtn = new Button { text = "Announce Update" };
        _shippedAnnounceUpdateBtn.AddToClassList("btn-secondary");
        _shippedAnnounceUpdateBtn.AddToClassList("btn-sm");
        _shippedAnnounceUpdateBtn.style.marginLeft = 4;
        _shippedAnnounceUpdateBtn.clicked += OnShippedAnnounceUpdateClicked;
        mktActionRow.Add(_shippedAnnounceUpdateBtn);

        _shippedMarketingPanel.Add(mktActionRow);
        _shippedDetailPanel.Add(_shippedMarketingPanel);
        var maintainRow = new VisualElement();
        maintainRow.AddToClassList("flex-row");
        maintainRow.AddToClassList("align-center");
        maintainRow.style.marginTop = 8;

        _shippedMaintenanceToggle = new Toggle("Actively Maintained");
        _shippedMaintenanceToggle.AddToClassList("toggle--inline");
        _shippedMaintenanceToggle.RegisterCallback<ChangeEvent<bool>>(OnMaintenanceChanged);
        maintainRow.Add(_shippedMaintenanceToggle);
        _shippedDetailPanel.Add(maintainRow);

        // ── Budget sections ──────────────────────────────────────────────────
        _shippedBudgetSection = new VisualElement();
        _shippedBudgetSection.AddToClassList("budget-section");
        _shippedBudgetSection.style.marginTop = 12;
        _shippedBudgetSection.style.display = DisplayStyle.None;

        // Maintenance budget
        var maintBudgetHeader = new Label("Maintenance Budget");
        maintBudgetHeader.AddToClassList("section-header");
        maintBudgetHeader.style.marginBottom = 4;
        _shippedBudgetSection.Add(maintBudgetHeader);

        var maintBudgetInputRow = new VisualElement();
        maintBudgetInputRow.AddToClassList("flex-row");
        maintBudgetInputRow.AddToClassList("align-center");
        maintBudgetInputRow.style.marginBottom = 4;

        var maintBudgetInputLabel = new Label("Monthly ($):");
        maintBudgetInputLabel.AddToClassList("metric-tertiary");
        maintBudgetInputLabel.style.marginRight = 8;
        maintBudgetInputRow.Add(maintBudgetInputLabel);

        _shippedMaintBudgetField = new TextField();
        _shippedMaintBudgetField.name = "maint-budget-input";
        _shippedMaintBudgetField.style.width = 100;
        _shippedMaintBudgetField.RegisterCallback<FocusOutEvent>(OnMaintBudgetFocusOut);
        maintBudgetInputRow.Add(_shippedMaintBudgetField);

        _shippedBudgetSection.Add(maintBudgetInputRow);

        var maintMetricsRow = new VisualElement();
        maintMetricsRow.AddToClassList("flex-row");
        maintMetricsRow.style.marginBottom = 8;

        _shippedMaintBudgetLabel = new Label("--");
        _shippedMaintBudgetLabel.AddToClassList("metric-tertiary");
        _shippedMaintBudgetLabel.style.marginRight = 8;
        maintMetricsRow.Add(_shippedMaintBudgetLabel);

        _shippedMaintDrainLabel = new Label("--");
        _shippedMaintDrainLabel.AddToClassList("metric-tertiary");
        _shippedMaintDrainLabel.style.marginRight = 8;
        maintMetricsRow.Add(_shippedMaintDrainLabel);

        _shippedMaintCoverageLabel = new Label("--");
        _shippedMaintCoverageLabel.AddToClassList("metric-tertiary");
        _shippedMaintCoverageLabel.style.marginRight = 8;
        maintMetricsRow.Add(_shippedMaintCoverageLabel);

        _shippedMaintQualityLabel = new Label("--");
        _shippedMaintQualityLabel.AddToClassList("metric-tertiary");
        maintMetricsRow.Add(_shippedMaintQualityLabel);

        _shippedBudgetSection.Add(maintMetricsRow);

        // Marketing budget
        var mktBudgetHeader = new Label("Marketing Budget");
        mktBudgetHeader.AddToClassList("section-header");
        mktBudgetHeader.style.marginBottom = 4;
        _shippedBudgetSection.Add(mktBudgetHeader);

        var mktBudgetInputRow = new VisualElement();
        mktBudgetInputRow.AddToClassList("flex-row");
        mktBudgetInputRow.AddToClassList("align-center");
        mktBudgetInputRow.style.marginBottom = 4;

        var mktBudgetInputLabel = new Label("Monthly ($):");
        mktBudgetInputLabel.AddToClassList("metric-tertiary");
        mktBudgetInputLabel.style.marginRight = 8;
        mktBudgetInputRow.Add(mktBudgetInputLabel);

        _shippedMktBudgetField = new TextField();
        _shippedMktBudgetField.name = "mkt-budget-input";
        _shippedMktBudgetField.style.width = 100;
        _shippedMktBudgetField.RegisterCallback<FocusOutEvent>(OnMktBudgetFocusOut);
        mktBudgetInputRow.Add(_shippedMktBudgetField);

        _shippedBudgetSection.Add(mktBudgetInputRow);

        var mktMetricsRow = new VisualElement();
        mktMetricsRow.AddToClassList("flex-row");
        mktMetricsRow.style.marginBottom = 8;

        _shippedMktBudgetLabel = new Label("--");
        _shippedMktBudgetLabel.AddToClassList("metric-tertiary");
        _shippedMktBudgetLabel.style.marginRight = 8;
        mktMetricsRow.Add(_shippedMktBudgetLabel);

        _shippedMktDrainLabel = new Label("--");
        _shippedMktDrainLabel.AddToClassList("metric-tertiary");
        _shippedMktDrainLabel.style.marginRight = 8;
        mktMetricsRow.Add(_shippedMktDrainLabel);

        _shippedMktCoverageLabel = new Label("--");
        _shippedMktCoverageLabel.AddToClassList("metric-tertiary");
        mktMetricsRow.Add(_shippedMktCoverageLabel);

        _shippedBudgetSection.Add(mktMetricsRow);

        // QA team assignment row
        var qaTeamAssignHeader = new Label("QA Team");
        qaTeamAssignHeader.AddToClassList("section-header");
        qaTeamAssignHeader.style.marginTop = 8;
        qaTeamAssignHeader.style.marginBottom = 4;
        _shippedBudgetSection.Add(qaTeamAssignHeader);

        var qaTeamRow = new VisualElement();
        qaTeamRow.AddToClassList("flex-row");
        qaTeamRow.AddToClassList("align-center");
        qaTeamRow.style.marginBottom = 8;

        _shippedQATeamLabel = new Label("None");
        _shippedQATeamLabel.AddToClassList("metric-tertiary");
        _shippedQATeamLabel.style.flexGrow = 1;
        qaTeamRow.Add(_shippedQATeamLabel);

        _shippedAssignQABtn = new Button { text = "Assign" };
        _shippedAssignQABtn.AddToClassList("btn-secondary");
        _shippedAssignQABtn.AddToClassList("btn-sm");
        _shippedAssignQABtn.clicked += OnShippedAssignQAClicked;
        qaTeamRow.Add(_shippedAssignQABtn);

        _shippedBudgetSection.Add(qaTeamRow);

        // Marketing team assignment row
        var mktTeamAssignHeader = new Label("Marketing Team");
        mktTeamAssignHeader.AddToClassList("section-header");
        mktTeamAssignHeader.style.marginBottom = 4;
        _shippedBudgetSection.Add(mktTeamAssignHeader);

        var mktTeamRow = new VisualElement();
        mktTeamRow.AddToClassList("flex-row");
        mktTeamRow.AddToClassList("align-center");
        mktTeamRow.style.marginBottom = 8;

        _shippedMktTeamLabel = new Label("None");
        _shippedMktTeamLabel.AddToClassList("metric-tertiary");
        _shippedMktTeamLabel.style.flexGrow = 1;
        mktTeamRow.Add(_shippedMktTeamLabel);

        _shippedAssignMktBtn = new Button { text = "Assign" };
        _shippedAssignMktBtn.AddToClassList("btn-secondary");
        _shippedAssignMktBtn.AddToClassList("btn-sm");
        _shippedAssignMktBtn.clicked += OnShippedAssignMktClicked;
        mktTeamRow.Add(_shippedAssignMktBtn);

        _shippedBudgetSection.Add(mktTeamRow);

        _shippedDetailPanel.Add(_shippedBudgetSection);

        // ── Distribution model panel (tools only) ────────────────────────────
        _distributionPanel = new VisualElement();
        _distributionPanel.style.marginTop = 12;
        _distributionPanel.style.display = DisplayStyle.None;

        var distHeader = new Label("Distribution");
        distHeader.AddToClassList("section-header");
        distHeader.style.marginBottom = 4;
        _distributionPanel.Add(distHeader);

        _distributionModelLabel = new Label("Proprietary");
        _distributionModelLabel.AddToClassList("metric-primary");
        _distributionModelLabel.style.marginBottom = 4;
        _distributionPanel.Add(_distributionModelLabel);

        _distributionLicenseeLabel = new Label("Licensees: 0");
        _distributionLicenseeLabel.AddToClassList("metric-secondary");
        _distributionLicenseeLabel.style.display = DisplayStyle.None;
        _distributionPanel.Add(_distributionLicenseeLabel);

        _distributionRevenueLabel = new Label("Total Licensing Revenue: $0");
        _distributionRevenueLabel.AddToClassList("metric-secondary");
        _distributionRevenueLabel.style.display = DisplayStyle.None;
        _distributionPanel.Add(_distributionRevenueLabel);

        _subscriberCountLabel = new Label("Subscribers: 0");
        _subscriberCountLabel.AddToClassList("metric-secondary");
        _subscriberCountLabel.style.display = DisplayStyle.None;
        _distributionPanel.Add(_subscriberCountLabel);

        _subscriptionRevenueLabel = new Label("Monthly Sub Revenue: $0");
        _subscriptionRevenueLabel.AddToClassList("metric-secondary");
        _subscriptionRevenueLabel.style.display = DisplayStyle.None;
        _distributionPanel.Add(_subscriptionRevenueLabel);

        _totalSubscriptionRevenueLabel = new Label("Total Sub Revenue: $0");
        _totalSubscriptionRevenueLabel.AddToClassList("metric-secondary");
        _totalSubscriptionRevenueLabel.style.display = DisplayStyle.None;
        _distributionPanel.Add(_totalSubscriptionRevenueLabel);

        var subPriceRow = new VisualElement();
        subPriceRow.AddToClassList("flex-row");
        subPriceRow.AddToClassList("align-center");
        subPriceRow.style.marginTop = 4;
        subPriceRow.style.display = DisplayStyle.None;

        var subPriceLabel = new Label("Sub Price:");
        subPriceLabel.AddToClassList("metric-secondary");
        subPriceLabel.style.marginRight = 8;
        subPriceRow.Add(subPriceLabel);

        _shippedSubscriptionPriceSlider = new SliderInt(5, 100);
        _shippedSubscriptionPriceSlider.style.flexGrow = 1;
        _shippedSubscriptionPriceSlider.RegisterValueChangedCallback(OnShippedSubscriptionPriceChanged);
        subPriceRow.Add(_shippedSubscriptionPriceSlider);

        _shippedSubscriptionPriceValueLabel = new Label("$20");
        _shippedSubscriptionPriceValueLabel.AddToClassList("metric-primary");
        _shippedSubscriptionPriceValueLabel.style.marginLeft = 8;
        _shippedSubscriptionPriceValueLabel.style.minWidth = 36;
        subPriceRow.Add(_shippedSubscriptionPriceValueLabel);
        _distributionPanel.Add(subPriceRow);

        var licensingRateRow = new VisualElement();
        licensingRateRow.AddToClassList("flex-row");
        licensingRateRow.AddToClassList("align-center");
        licensingRateRow.style.marginTop = 4;
        licensingRateRow.style.display = DisplayStyle.None;

        var rateLabel = new Label("Royalty Rate:");
        rateLabel.AddToClassList("metric-secondary");
        rateLabel.style.marginRight = 8;
        licensingRateRow.Add(rateLabel);

        _licensingRateSlider = new Slider(5f, 30f);
        _licensingRateSlider.style.flexGrow = 1;
        _licensingRateSlider.RegisterValueChangedCallback(OnLicensingRateChanged);
        licensingRateRow.Add(_licensingRateSlider);

        _licensingRateValueLabel = new Label("15%");
        _licensingRateValueLabel.AddToClassList("metric-primary");
        _licensingRateValueLabel.style.marginLeft = 8;
        _licensingRateValueLabel.style.minWidth = 36;
        licensingRateRow.Add(_licensingRateValueLabel);
        _distributionPanel.Add(licensingRateRow);

        var distActionRow = new VisualElement();
        distActionRow.AddToClassList("flex-row");
        distActionRow.style.marginTop = 6;

        _releaseToMarketBtn = new Button { text = "Release to Market (Licensed)" };
        _releaseToMarketBtn.AddToClassList("btn-secondary");
        _releaseToMarketBtn.AddToClassList("btn-sm");
        _releaseToMarketBtn.clicked += OnReleaseToMarketClicked;
        distActionRow.Add(_releaseToMarketBtn);

        _pullFromMarketBtn = new Button { text = "Pull From Market" };
        _pullFromMarketBtn.AddToClassList("btn-secondary");
        _pullFromMarketBtn.AddToClassList("btn-sm");
        _pullFromMarketBtn.style.marginLeft = 4;
        _pullFromMarketBtn.clicked += OnPullFromMarketClicked;
        distActionRow.Add(_pullFromMarketBtn);

        _openSourceBtn = new Button { text = "Open Source It" };
        _openSourceBtn.AddToClassList("btn-danger");
        _openSourceBtn.AddToClassList("btn-sm");
        _openSourceBtn.style.marginLeft = 4;
        _openSourceBtn.clicked += OnOpenSourceClicked;
        distActionRow.Add(_openSourceBtn);

        _distributionPanel.Add(distActionRow);
        _shippedDetailPanel.Add(_distributionPanel);

        // Actions
        var divider = new VisualElement();
        divider.AddToClassList("divider");
        _shippedDetailPanel.Add(divider);

        var actionRow = new VisualElement();
        actionRow.AddToClassList("flex-row");
        actionRow.style.marginTop = 12;

        _updateProductButton = new Button { text = "Update Product" };
        _updateProductButton.AddToClassList("btn-primary");
        _updateProductButton.clicked += OnUpdateProductClicked;
        actionRow.Add(_updateProductButton);

        _removeFromMarketButton = new Button { text = "Remove from Market" };
        _removeFromMarketButton.AddToClassList("btn-danger");
        _removeFromMarketButton.AddToClassList("btn-sm");
        _removeFromMarketButton.style.marginLeft = 8;
        _removeFromMarketButton.clicked += OnRemoveFromMarketClicked;
        actionRow.Add(_removeFromMarketButton);

        _sequelButton = new Button { text = "Create Sequel" };
        _sequelButton.AddToClassList("btn-secondary");
        _sequelButton.AddToClassList("btn-sm");
        _sequelButton.style.marginLeft = 8;
        _sequelButton.clicked += OnSequelClicked;
        actionRow.Add(_sequelButton);

        _shippedDetailPanel.Add(actionRow);
        rightPanel.Add(_shippedDetailPanel);

        _shippedDetailEmptyState = UICardHelper.CreateEmptyState("👈", "Select a product to view live metrics");
        rightPanel.Add(_shippedDetailEmptyState);

        layout.Add(rightPanel);
        parent.Add(layout);
    }

    private Label BuildMetricLabel(VisualElement grid, string title)
    {
        var cell = new VisualElement();
        cell.AddToClassList("metric-cell");

        var titleLabel = new Label(title);
        titleLabel.AddToClassList("metric-tertiary");
        cell.Add(titleLabel);

        var valueLabel = new Label("—");
        valueLabel.AddToClassList("metric-value");
        cell.Add(valueLabel);

        grid.Add(cell);
        return valueLabel;
    }

    private Label BuildMetricLabel(VisualElement grid, string title, out Label trendLabel)
    {
        var cell = new VisualElement();
        cell.AddToClassList("metric-cell");

        var titleLabel = new Label(title);
        titleLabel.AddToClassList("metric-tertiary");
        cell.Add(titleLabel);

        var valueLabel = new Label("—");
        valueLabel.AddToClassList("metric-value");
        cell.Add(valueLabel);

        var trend = new Label("—");
        trend.AddToClassList("metric-trend");
        cell.Add(trend);

        trendLabel = trend;
        grid.Add(cell);
        return valueLabel;
    }

    private static void ApplyTrendClass(VisualElement el, int direction)
    {
        el.RemoveFromClassList("trend--up");
        el.RemoveFromClassList("trend--down");
        el.RemoveFromClassList("trend--flat");
        if (direction > 0) el.AddToClassList("trend--up");
        else if (direction < 0) el.AddToClassList("trend--down");
        else el.AddToClassList("trend--flat");
    }

    private VisualElement BuildPhaseRow(int index)
    {
        var row = new VisualElement();
        row.AddToClassList("phase-row");
        row.style.flexDirection = FlexDirection.Column;

        // Top row: label + progress + quality + badge
        var topRow = new VisualElement();
        topRow.style.flexDirection = FlexDirection.Row;
        topRow.style.alignItems = Align.Center;
        topRow.style.flexGrow = 1;

        var phaseLabel = new Label();
        phaseLabel.AddToClassList("phase-row__label");
        topRow.Add(phaseLabel);
        _phaseLabels[index] = phaseLabel;

        var progressBar = new VisualElement();
        progressBar.AddToClassList("progress-bar");
        progressBar.AddToClassList("phase-row__progress");

        var fill = new VisualElement();
        fill.AddToClassList("progress-bar__fill");
        progressBar.Add(fill);
        topRow.Add(progressBar);
        _phaseProgressFills[index] = fill;

        var qualLabel = new Label();
        qualLabel.AddToClassList("phase-row__quality");
        topRow.Add(qualLabel);
        _phaseQualityLabels[index] = qualLabel;

        var badge = new Label();
        badge.AddToClassList("badge");
        badge.style.marginLeft = 4;
        topRow.Add(badge);
        _phaseBadges[index] = badge;

        row.Add(topRow);

        // Bottom row: actions + team + bugs
        var bottomRow = new VisualElement();
        bottomRow.style.flexDirection = FlexDirection.Row;
        bottomRow.style.alignItems = Align.Center;
        bottomRow.style.marginTop = 2;

        var actions = new VisualElement();
        actions.AddToClassList("phase-row__actions");
        actions.style.flexShrink = 0;
        actions.style.marginLeft = 0;

        var iterateBtn = new Button { text = "Iterate" };
        iterateBtn.AddToClassList("btn-secondary");
        iterateBtn.AddToClassList("btn-sm");
        iterateBtn.style.display = DisplayStyle.None;
        actions.Add(iterateBtn);
        _phaseIterateButtons[index] = iterateBtn;

        var assignBtn = new Button { text = "Assign Team" };
        assignBtn.AddToClassList("btn-secondary");
        assignBtn.AddToClassList("btn-sm");
        assignBtn.style.marginLeft = 4;
        assignBtn.style.display = DisplayStyle.None;
        actions.Add(assignBtn);
        _phaseAssignButtons[index] = assignBtn;

        bottomRow.Add(actions);

        var teamLabel = new Label();
        teamLabel.AddToClassList("phase-row__team");
        teamLabel.style.marginLeft = 4;
        bottomRow.Add(teamLabel);
        _phaseTeamLabels[index] = teamLabel;

        var bugLabel = new Label();
        bugLabel.AddToClassList("phase-row__bugs");
        bugLabel.style.flexShrink = 0;
        bottomRow.Add(bugLabel);
        _phaseBugLabels[index] = bugLabel;

        row.Add(bottomRow);

        return row;
    }

    // ─── Tab switching ────────────────────────────────────────────────────────

    private void OnTabDevClicked()
    {
        _showingShipped = false;
        _devSection.style.display = DisplayStyle.Flex;
        _shippedSection.style.display = DisplayStyle.None;
        _tabDevBtn.AddToClassList("tab-btn--active");
        _tabShippedBtn.RemoveFromClassList("tab-btn--active");
    }

    private void OnTabShippedClicked()
    {
        _showingShipped = true;
        _devSection.style.display = DisplayStyle.None;
        _shippedSection.style.display = DisplayStyle.Flex;
        _tabDevBtn.RemoveFromClassList("tab-btn--active");
        _tabShippedBtn.AddToClassList("tab-btn--active");
        _hasAnimatedInShipped = false; // trigger re-stagger
        if (_viewModel != null) BindShipped(_viewModel);
    }

    // ─── IGameView ────────────────────────────────────────────────────────────

    public void Bind(IViewModel viewModel)
    {
        _viewModel = viewModel as ProductsViewModel;
        if (_viewModel == null) return;

        // Wire gate config for capability tooltips
        if (_dispatcher is WindowManager wmProducts && wmProducts.GameController != null)
        {
            _viewModel.SetGateConfig(wmProducts.GameController.CrossProductGateConfig, wmProducts.GameController.ProductTemplates);
            _viewModel.SetNiches(wmProducts.GameController.MarketNiches);
        }

        // ── Create mode routing ──────────────────────────────────────────────
        if (_isInCreateMode && _createProductView != null && _createProductVm != null)
        {
            var snapshot = viewModel as IReadOnlyGameState ?? GetSnapshotFromViewModel(viewModel);
            if (snapshot != null) _createProductVm.Refresh(snapshot);
            _createProductView.Bind(_createProductVm);
            return;
        }

        // ── Dev tab ──────────────────────────────────────────────────────────
        _devPool.UpdateList(_viewModel.DevProducts, BindDevCard);

        bool hasDev = _viewModel.DevProducts != null && _viewModel.DevProducts.Count > 0;
        if (_devEmptyState != null)
        {
            if (hasDev) _devEmptyState.AddToClassList("empty-state--hidden");
            else _devEmptyState.RemoveFromClassList("empty-state--hidden");
        }

        _viewModel.HasSelection = _hasSelection;
        _viewModel.SelectedProductId = _selectedProductId;
        _viewModel.RefreshSelectedDetail();

        if (_viewModel.HasSelection)
        {
            _detailPanel.style.display = DisplayStyle.Flex;
            if (_detailEmptyState != null) _detailEmptyState.style.display = DisplayStyle.None;
            BindDetail(_viewModel);
        }
        else
        {
            _detailPanel.style.display = DisplayStyle.None;
            if (_detailEmptyState != null) _detailEmptyState.style.display = DisplayStyle.Flex;
        }

        if (!_hasAnimatedIn)
        {
            _hasAnimatedIn = true;
            _staggerScratch.Clear();
            int childCount = _devContainer.childCount;
            for (int i = 0; i < childCount; i++)
            {
                var el = _devContainer[i];
                if (el.style.display != DisplayStyle.None) _staggerScratch.Add(el);
            }
            UIAnimator.StaggerIn(_staggerScratch);
        }

        // ── Shipped tab (if visible) ─────────────────────────────────────────
        if (_showingShipped) BindShipped(_viewModel);
    }

    private IReadOnlyGameState GetSnapshotFromViewModel(IViewModel viewModel)
    {
        if (_dispatcher is WindowManager wm)
            return wm.GetCurrentSnapshot();
        return null;
    }

    public void Dispose()
    {
        // Clean up create product view if active
        if (_createProductView != null)
        {
            _createProductView.OnCancelRequested -= OnCreateCancelled;
            _createProductView.OnProductCreated -= OnCreateConfirmed;
            _createProductView.Dispose();
            _createProductView = null;
            _createProductVm = null;
        }
        _isInCreateMode = false;

        _hasAnimatedIn = false;
        _hasAnimatedInShipped = false;
        _fillPercents.Clear();
        _fillTweeners.Clear();
        _badgeClasses.Clear();
        _staggerScratch.Clear();
        _viewModel = null;
        _devPool = null;
        _shippedPool = null;
        _shippedDetailUsersTrend = null;
        _shippedDetailRevenueTrend = null;
        _devBugIndicator = null;
        _phaseBugLabels = null;
        _sequelButton = null;

        if (_crunchAllBtn != null) _crunchAllBtn.UnregisterCallback<ClickEvent>(OnCrunchAllClicked);
        if (_shippedAssignQABtn != null) _shippedAssignQABtn.clicked -= OnShippedAssignQAClicked;
        if (_shippedAssignMktBtn != null) _shippedAssignMktBtn.clicked -= OnShippedAssignMktClicked;
        if (_devMktBudgetField != null) _devMktBudgetField.UnregisterCallback<FocusOutEvent>(OnDevMktBudgetFocusOut);
        if (_shippedStartMarketingBtn != null) _shippedStartMarketingBtn.clicked -= OnShippedStartMarketingClicked;
        if (_shippedStopMarketingBtn != null) _shippedStopMarketingBtn.clicked -= OnShippedStopMarketingClicked;
        if (_shippedRunAdsBtn != null) _shippedRunAdsBtn.clicked -= OnShippedRunAdsClicked;
        if (_shippedAnnounceUpdateBtn != null) _shippedAnnounceUpdateBtn.clicked -= OnShippedAnnounceUpdateClicked;
        if (_releaseToMarketBtn != null) _releaseToMarketBtn.clicked -= OnReleaseToMarketClicked;
        if (_pullFromMarketBtn != null) _pullFromMarketBtn.clicked -= OnPullFromMarketClicked;
        if (_openSourceBtn != null) _openSourceBtn.clicked -= OnOpenSourceClicked;
        if (_shipDistProprietaryBtn != null) _shipDistProprietaryBtn.clicked -= OnShipDistProprietaryClicked;
        if (_shipDistLicensedBtn != null) _shipDistLicensedBtn.clicked -= OnShipDistLicensedClicked;
        if (_shipDistOpenSourceBtn != null) _shipDistOpenSourceBtn.clicked -= OnShipDistOpenSourceClicked;
        if (_shipConfirmBtn != null) _shipConfirmBtn.clicked -= OnConfirmShipClicked;
        if (_changeDateButton != null) _changeDateButton.clicked -= OnChangeDateClicked;
        if (_changeDateConfirmBtn != null) _changeDateConfirmBtn.clicked -= OnChangeDateConfirmed;
        if (_changeDateSlider != null) _changeDateSlider.UnregisterValueChangedCallback(OnChangeDateSliderChanged);
        if (_setReleaseDateButton != null) _setReleaseDateButton.clicked -= OnSetReleaseDateClicked;
        if (_setReleaseDateConfirmBtn != null) _setReleaseDateConfirmBtn.clicked -= OnSetReleaseDateConfirmed;
        if (_setReleaseDateSlider != null) _setReleaseDateSlider.UnregisterValueChangedCallback(OnSetReleaseDateSliderChanged);
        if (_licensingRateSlider != null) _licensingRateSlider.UnregisterValueChangedCallback(OnLicensingRateChanged);
        if (_shippedSubscriptionPriceSlider != null) _shippedSubscriptionPriceSlider.UnregisterValueChangedCallback(OnShippedSubscriptionPriceChanged);
        if (_shipDistRateSlider != null) _shipDistRateSlider.UnregisterValueChangedCallback(OnShipDistRateChanged);
        if (_shippedMaintBudgetField != null) _shippedMaintBudgetField.UnregisterCallback<FocusOutEvent>(OnMaintBudgetFocusOut);
        if (_shippedMktBudgetField != null) _shippedMktBudgetField.UnregisterCallback<FocusOutEvent>(OnMktBudgetFocusOut);
        _releaseDateLabel = null;
        _daysUntilReleaseLabel = null;
        _shiftCountBadge = null;
        _changeDateButton = null;
        _changeDateFlyout = null;
        _changeDateSlider = null;
        _changeDateValueLabel = null;
        _changeDatePenaltyLabel = null;
        _changeDateConfirmBtn = null;
        _setReleaseDateButton = null;
        _setReleaseDateFlyout = null;
        _setReleaseDateSlider = null;
        _setReleaseDateValueLabel = null;
        _setReleaseDateConfirmBtn = null;

        _shippedMarketingPanel = null;
        _shippedHypeBarFill = null;
        _shippedHypeLabel = null;
        _shippedStartMarketingBtn = null;
        _shippedStopMarketingBtn = null;
        _shippedRunAdsBtn = null;
        _shippedAnnounceUpdateBtn = null;
        _shippedAdStatusLabel = null;
        _shippedUpdateHypeBarFill = null;
        _shippedUpdateHypeLabel = null;
        _devHypeBarFill = null;
        _devHypeLabel = null;
        _devMarketingTeamLabel = null;
        _devMktBudgetField = null;
        _devMktBudgetAllocLabel = null;
        _devMktBudgetStatusLabel = null;
        _crunchAllBtn = null;
        _shippedAssignQABtn = null;
        _shippedAssignMktBtn = null;
        _shippedQATeamLabel = null;
        _shippedMktTeamLabel = null;
        _distributionPanel = null;
        _distributionModelLabel = null;
        _distributionLicenseeLabel = null;
        _distributionRevenueLabel = null;
        _licensingRateSlider = null;
        _licensingRateValueLabel = null;
        _releaseToMarketBtn = null;
        _openSourceBtn = null;
        _pullFromMarketBtn = null;
        _subscriberCountLabel = null;
        _subscriptionRevenueLabel = null;
        _totalSubscriptionRevenueLabel = null;
        _shippedSubscriptionPriceSlider = null;
        _shippedSubscriptionPriceValueLabel = null;
        _shipDistributionFlyout = null;
        _shipDistProprietaryBtn = null;
        _shipDistLicensedBtn = null;
        _shipDistOpenSourceBtn = null;
        _shipDistRateSlider = null;
        _shipDistRateValueLabel = null;
        _shipDistRateRow = null;
        _shipDistWarningLabel = null;
        _shipConfirmBtn = null;

        _shippedBudgetSection = null;
        _shippedMaintBudgetLabel = null;
        _shippedMaintDrainLabel = null;
        _shippedMaintCoverageLabel = null;
        _shippedMaintQualityLabel = null;
        _shippedMaintBudgetField = null;
        _shippedMktBudgetLabel = null;
        _shippedMktDrainLabel = null;
        _shippedMktCoverageLabel = null;
        _shippedMktBudgetField = null;
    }

    // ── Dev card factory / binding ────────────────────────────────────────────

    private VisualElement CreateDevCard()
    {
        var card = new VisualElement();
        card.AddToClassList("card");
        card.AddToClassList("card--hover");
        UICardHelper.ApplyBevel(card);
        UICardHelper.AddGradient(card);
        card.style.marginBottom = 8;

        var nameLabel = new Label();
        nameLabel.name = "dev-name";
        nameLabel.AddToClassList("card__title");
        card.Add(nameLabel);

        var subtitleRow = new VisualElement();
        subtitleRow.AddToClassList("flex-row");
        subtitleRow.AddToClassList("align-center");
        subtitleRow.style.marginBottom = 4;

        var templateLabel = new Label();
        templateLabel.name = "dev-template";
        templateLabel.AddToClassList("card__subtitle");
        subtitleRow.Add(templateLabel);

        var statusBadge = new Label();
        statusBadge.name = "dev-status-badge";
        statusBadge.AddToClassList("badge");
        statusBadge.style.marginLeft = 8;
        subtitleRow.Add(statusBadge);

        card.Add(subtitleRow);

        var progressBar = new VisualElement();
        progressBar.AddToClassList("progress-bar");
        progressBar.style.marginBottom = 4;
        var fill = new VisualElement();
        fill.name = "dev-progress";
        fill.AddToClassList("progress-bar__fill");
        progressBar.Add(fill);
        card.Add(progressBar);

        var statusLabel = new Label();
        statusLabel.name = "dev-status";
        statusLabel.AddToClassList("metric-tertiary");
        card.Add(statusLabel);

        var durationLabel = new Label();
        durationLabel.name = "dev-duration";
        durationLabel.AddToClassList("metric-tertiary");
        card.Add(durationLabel);

        return card;
    }

    private void BindDevCard(VisualElement el, DevProductDisplay data)
    {
        el.Q<Label>("dev-name").text = data.Name;

        var templateLabel = el.Q<Label>("dev-template");
        if (templateLabel != null)
        {
            string subtitle = data.ProductTypeLabel;
            templateLabel.text = subtitle;
        }

        el.Q<Label>("dev-status").text = data.StatusLabel;

        var durationLabel = el.Q<Label>("dev-duration");
        if (durationLabel != null) durationLabel.text = data.DevDurationLabel;

        var badge = el.Q<Label>("dev-status-badge");
        if (badge != null)
        {
            string newClass = data.AllPhasesComplete ? "badge--success" : "badge--accent";
            string oldClass;
            _badgeClasses.TryGetValue(badge, out oldClass);
            if (oldClass != newClass)
            {
                badge.RemoveFromClassList("badge--success");
                badge.RemoveFromClassList("badge--accent");
                badge.AddToClassList(newClass);
                _badgeClasses[badge] = newClass;
            }
            badge.text = data.AllPhasesComplete ? "Ready to Ship" : "In Progress";
        }

        var progressFill = el.Q<VisualElement>("dev-progress");
        if (progressFill != null)
        {
            float targetPercent = data.OverallProgressPercent * 100f;
            if (!_fillPercents.TryGetValue(progressFill, out float cur)) cur = 0f;
            if (_fillTweeners.TryGetValue(progressFill, out var t)) t?.Kill();
            _fillTweeners[progressFill] = UIAnimator.ProgressFill(progressFill, cur, targetPercent);
            _fillPercents[progressFill] = targetPercent;
        }

        var capturedId = data.Id;
        System.Action onSelect = () => {
            _selectedProductId = capturedId;
            _hasSelection = true;
            if (_viewModel != null)
            {
                _viewModel.HasSelection = true;
                _viewModel.SelectedProductId = capturedId;
                _viewModel.RefreshSelectedDetail();
                BindDetail(_viewModel);
                _detailPanel.style.display = DisplayStyle.Flex;
                if (_detailEmptyState != null) _detailEmptyState.style.display = DisplayStyle.None;
            }
        };
        el.userData = onSelect;

        el.UnregisterCallback<ClickEvent>(OnDevCardClicked);
        el.RegisterCallback<ClickEvent>(OnDevCardClicked);
        el.userData = (capturedId, onSelect);
    }

    private void OnDevCardClicked(ClickEvent evt)
    {
        var el = evt.currentTarget as VisualElement;
        if (el?.userData is System.ValueTuple<ProductId, System.Action> tuple)
            tuple.Item2?.Invoke();
    }

    // ── Shipped panel binding ─────────────────────────────────────────────────

    private VisualElement CreateShippedCard()
    {
        var card = new VisualElement();
        card.AddToClassList("card");
        card.AddToClassList("card--hover");
        UICardHelper.ApplyBevel(card);
        UICardHelper.AddGradient(card);
        card.style.marginBottom = 8;

        var headerRow = new VisualElement();
        headerRow.AddToClassList("flex-row");
        headerRow.AddToClassList("align-center");
        headerRow.AddToClassList("justify-between");

        var nameLabel = new Label();
        nameLabel.name = "shipped-name";
        nameLabel.AddToClassList("card__title");
        headerRow.Add(nameLabel);

        var stageBadge = new Label();
        stageBadge.name = "shipped-stage";
        stageBadge.AddToClassList("badge");
        headerRow.Add(stageBadge);

        card.Add(headerRow);

        var typeLabel = new Label();
        typeLabel.name = "shipped-type";
        typeLabel.AddToClassList("card__subtitle");
        typeLabel.style.marginBottom = 2;
        card.Add(typeLabel);

        var metricsRow = new VisualElement();
        metricsRow.AddToClassList("flex-row");
        metricsRow.style.marginTop = 4;

        var usersLabel = new Label();
        usersLabel.name = "shipped-users";
        usersLabel.AddToClassList("metric-tertiary");
        usersLabel.style.marginRight = 12;
        metricsRow.Add(usersLabel);

        var revenueLabel = new Label();
        revenueLabel.name = "shipped-revenue";
        revenueLabel.AddToClassList("metric-tertiary");
        metricsRow.Add(revenueLabel);

        var totalRevenueLabel = new Label();
        totalRevenueLabel.name = "shipped-total-revenue";
        totalRevenueLabel.AddToClassList("metric-tertiary");
        totalRevenueLabel.style.marginLeft = 12;
        metricsRow.Add(totalRevenueLabel);

        var productionCostLabel = new Label();
        productionCostLabel.name = "shipped-production-cost";
        productionCostLabel.AddToClassList("metric-tertiary");
        productionCostLabel.style.marginLeft = 12;
        metricsRow.Add(productionCostLabel);

        card.Add(metricsRow);

        // Crisis indicator badge
        var crisisBadge = new Label();
        crisisBadge.name = "shipped-crisis";
        crisisBadge.AddToClassList("badge");
        crisisBadge.AddToClassList("badge--warning");
        crisisBadge.style.marginTop = 6;
        crisisBadge.style.display = DisplayStyle.None;
        card.Add(crisisBadge);

        // Update-in-progress badge
        var updateBadge = new Label();
        updateBadge.name = "shipped-updating";
        updateBadge.AddToClassList("badge");
        updateBadge.AddToClassList("badge--info");
        updateBadge.style.marginTop = 4;
        updateBadge.style.display = DisplayStyle.None;
        card.Add(updateBadge);

        // Sell button
        var sellBtn = new Button { text = "Sell Product" };
        sellBtn.name = "shipped-sell";
        sellBtn.AddToClassList("btn-secondary");
        sellBtn.AddToClassList("btn-sm");
        sellBtn.style.marginTop = 8;
        sellBtn.style.display = DisplayStyle.None;
        card.Add(sellBtn);

        return card;
    }

    private void BindShipped(ProductsViewModel vm)
    {
        if (_shippedPool == null) return;

        var shippedList = vm.ShippedProducts;
        _shippedPool.UpdateList(shippedList, BindShippedCard);

        bool hasShipped = shippedList != null && shippedList.Count > 0;
        if (_shippedEmptyState != null)
        {
            if (hasShipped) _shippedEmptyState.AddToClassList("empty-state--hidden");
            else _shippedEmptyState.RemoveFromClassList("empty-state--hidden");
        }

        if (_hasShippedSelection)
        {
            bool found = false;
            int sc = shippedList != null ? shippedList.Count : 0;
            for (int i = 0; i < sc; i++)
            {
                if (shippedList[i].Id == _selectedShippedId)
                {
                    _shippedDetailPanel.style.display = DisplayStyle.Flex;
                    if (_shippedDetailEmptyState != null) _shippedDetailEmptyState.style.display = DisplayStyle.None;
                    BindShippedDetail(shippedList[i]);
                    found = true;
                    break;
                }
            }
            if (!found)
            {
                _hasShippedSelection = false;
                _shippedDetailPanel.style.display = DisplayStyle.None;
                if (_shippedDetailEmptyState != null) _shippedDetailEmptyState.style.display = DisplayStyle.Flex;
            }
        }
        else
        {
            _shippedDetailPanel.style.display = DisplayStyle.None;
            if (_shippedDetailEmptyState != null) _shippedDetailEmptyState.style.display = DisplayStyle.Flex;
        }

        if (!_hasAnimatedInShipped)
        {
            _hasAnimatedInShipped = true;
            _staggerScratch.Clear();
            int childCount = _shippedContainer.childCount;
            for (int i = 0; i < childCount; i++)
            {
                var el = _shippedContainer[i];
                if (el.style.display != DisplayStyle.None) _staggerScratch.Add(el);
            }
            UIAnimator.StaggerIn(_staggerScratch);
        }
    }

    private void BindShippedCard(VisualElement el, ShippedProductDisplay data)
    {
        el.Q<Label>("shipped-name").text = data.Name;

        var typeLabel = el.Q<Label>("shipped-type");
        if (typeLabel != null)
        {
            string subtitle = data.ProductTypeLabel ?? "";
            typeLabel.text = subtitle;
        }

        var stageBadge = el.Q<Label>("shipped-stage");
        if (stageBadge != null)
        {
            string oldClass;
            _badgeClasses.TryGetValue(stageBadge, out oldClass);
            if (oldClass != data.LifecycleBadgeClass)
            {
                if (!string.IsNullOrEmpty(oldClass)) stageBadge.RemoveFromClassList(oldClass);
                stageBadge.AddToClassList(data.LifecycleBadgeClass);
                _badgeClasses[stageBadge] = data.LifecycleBadgeClass;
            }
            stageBadge.text = data.LifecycleStageLabel;
        }

        var usersLabel = el.Q<Label>("shipped-users");
        if (usersLabel != null)
        {
            usersLabel.text = data.ActiveUsersDisplay + " users";
            ApplyTrendClass(usersLabel, data.CurrentUsersTrendDirection);
        }

        var revenueLabel = el.Q<Label>("shipped-revenue");
        if (revenueLabel != null)
        {
            revenueLabel.text = data.MonthlyRevenueDisplay + "/mo";
            ApplyTrendClass(revenueLabel, data.CurrentRevenueTrendDirection);
        }

        var totalRevLabel = el.Q<Label>("shipped-total-revenue");
        if (totalRevLabel != null) totalRevLabel.text = data.TotalRevenueDisplay + " total";

        var productionCostLabel = el.Q<Label>("shipped-production-cost");
        if (productionCostLabel != null) productionCostLabel.text = data.ProductionCostDisplay + " cost";

        // Crisis indicator
        var crisisBadge = el.Q<Label>("shipped-crisis");
        if (crisisBadge != null) {
            crisisBadge.style.display = data.HasCrisis ? DisplayStyle.Flex : DisplayStyle.None;
            if (data.HasCrisis) {
                string crisisLabel = data.CrisisType switch {
                    CrisisEventType.Catastrophic   => "Catastrophic Crisis",
                    CrisisEventType.ModerateBreach => "Moderate Crisis",
                    _                              => "Minor Crisis"
                };
                crisisBadge.text = crisisLabel;
                crisisBadge.RemoveFromClassList("badge--warning");
                crisisBadge.RemoveFromClassList("badge--danger");
                if (data.CrisisType == CrisisEventType.Catastrophic)
                    crisisBadge.AddToClassList("badge--danger");
                else
                    crisisBadge.AddToClassList("badge--warning");
            }
        }

        var updateBadge = el.Q<Label>("shipped-updating");
        if (updateBadge != null) {
            updateBadge.style.display = data.IsUpdating ? DisplayStyle.Flex : DisplayStyle.None;
            if (data.IsUpdating)
                updateBadge.text = data.UpdateTypeLabel + " " + (data.UpdateProgressPercent * 100f).ToString("F0") + "%";
        }

        // Sell button — wired with named handler via userData pattern
        var sellBtn = el.Q<Button>("shipped-sell");
        if (sellBtn != null) {
            sellBtn.style.display = data.CanSell ? DisplayStyle.Flex : DisplayStyle.None;
            if (sellBtn.userData is System.Action prevSell)
                sellBtn.clicked -= prevSell;
            if (data.CanSell) {
                var capturedId = data.Id;
                System.Action onSell = () => OnSellProductClicked(capturedId);
                sellBtn.userData = onSell;
                sellBtn.clicked += onSell;
            }
        }

        el.UnregisterCallback<ClickEvent>(OnShippedCardClicked);
        el.RegisterCallback<ClickEvent>(OnShippedCardClicked);
        el.userData = data.Id;
    }

    private void OnSellProductClicked(ProductId productId) { }

    private void OnShippedCardClicked(ClickEvent evt)
    {
        var el = evt.currentTarget as VisualElement;
        if (el?.userData is ProductId productId)
        {
            _selectedShippedId = productId;
            _hasShippedSelection = true;
            if (_viewModel == null) return;

            int count = _viewModel.ShippedProducts.Count;
            for (int i = 0; i < count; i++)
            {
                if (_viewModel.ShippedProducts[i].Id == productId)
                {
                    _shippedDetailPanel.style.display = DisplayStyle.Flex;
                    if (_shippedDetailEmptyState != null) _shippedDetailEmptyState.style.display = DisplayStyle.None;
                    BindShippedDetail(_viewModel.ShippedProducts[i]);
                    break;
                }
            }
        }
    }

    private void BindShippedDetail(ShippedProductDisplay d)
    {
        if (_shippedDetailName != null) _shippedDetailName.text = d.Name;

        if (_shippedDetailStageBadge != null)
        {
            string oldClass;
            _badgeClasses.TryGetValue(_shippedDetailStageBadge, out oldClass);
            if (oldClass != d.LifecycleBadgeClass)
            {
                if (!string.IsNullOrEmpty(oldClass)) _shippedDetailStageBadge.RemoveFromClassList(oldClass);
                _shippedDetailStageBadge.AddToClassList(d.LifecycleBadgeClass);
                _badgeClasses[_shippedDetailStageBadge] = d.LifecycleBadgeClass;
            }
            _shippedDetailStageBadge.text = d.LifecycleStageLabel;
        }

        if (_shippedDetailTypeLabel != null)
        {
            string info = d.ProductTypeLabel ?? "";
            if (!string.IsNullOrEmpty(d.PricingLabel)) info += " — " + d.PricingLabel;
            if (d.IsTool || d.IsPlatform) info += "  [hover for capabilities]";
            _shippedDetailTypeLabel.text = info;
        }

        // Capabilities tooltip for tools and platforms
        if (_tooltipProvider != null && _viewModel != null && (d.IsTool || d.IsPlatform))
        {
            if (_shippedDetailPanel != null) {
                var tooltipData = _viewModel.BuildShippedCapabilityTooltip(d.Id);
                _shippedDetailPanel.SetRichTooltip(tooltipData, _tooltipProvider.TooltipService);
            }
        }
        else if (_tooltipProvider != null && _shippedDetailPanel != null)
        {
            _shippedDetailPanel.ClearTooltip(_tooltipProvider.TooltipService);
        }

        if (_shippedDetailPlatformGenreRow != null)
            _shippedDetailPlatformGenreRow.style.display = DisplayStyle.None;

        if (_shippedDetailUsers != null)
        {
            _shippedDetailUsers.text = d.ActiveUsersDisplay;
            ApplyTrendClass(_shippedDetailUsers, d.CurrentUsersTrendDirection);
        }
        if (_shippedDetailUsersTrend != null)
        {
            _shippedDetailUsersTrend.text = d.ProjectedUsersTrendLabel + " next mo.";
            ApplyTrendClass(_shippedDetailUsersTrend, d.ProjectedUsersTrendDirection);
        }
        if (_shippedDetailRevenue != null)
        {
            _shippedDetailRevenue.text = d.MonthlyRevenueDisplay;
            ApplyTrendClass(_shippedDetailRevenue, d.CurrentRevenueTrendDirection);
        }
        if (_shippedDetailRevenueTrend != null)
        {
            _shippedDetailRevenueTrend.text = d.ProjectedRevenueTrendLabel + " next mo.";
            ApplyTrendClass(_shippedDetailRevenueTrend, d.ProjectedRevenueTrendDirection);
        }
        if (_shippedDetailTotalRevenue != null) _shippedDetailTotalRevenue.text = d.TotalRevenueDisplay;
        if (_shippedDetailProductionCost != null) _shippedDetailProductionCost.text = d.ProductionCostDisplay;
        if (_shippedDetailPopularity != null) _shippedDetailPopularity.text = d.PopularityPercent.ToString("F0") + "%";
        if (_shippedDetailBugs != null) _shippedDetailBugs.text = d.BugsRemainingPercent.ToString("F0") + "%";

        // Update progress bar
        if (_shippedUpdateSection != null)
        {
            _shippedUpdateSection.style.display = d.IsUpdating ? DisplayStyle.Flex : DisplayStyle.None;
            if (d.IsUpdating)
            {
                if (_shippedUpdateProgressLabel != null)
                    _shippedUpdateProgressLabel.text = d.UpdateTypeLabel + " — " + (d.UpdateProgressPercent * 100f).ToString("F0") + "%";
                if (_shippedUpdateProgressFill != null)
                {
                    if (!_fillPercents.TryGetValue(_shippedUpdateProgressFill, out float cur)) cur = 0f;
                    if (_fillTweeners.TryGetValue(_shippedUpdateProgressFill, out var t)) t?.Kill();
                    float target = d.UpdateProgressPercent * 100f;
                    _fillTweeners[_shippedUpdateProgressFill] = UIAnimator.ProgressFill(_shippedUpdateProgressFill, cur, target);
                    _fillPercents[_shippedUpdateProgressFill] = target;
                }
            }
        }

        // Maintenance toggle — suppress event during bind
        if (_shippedMaintenanceToggle != null)
        {
            _shippedMaintenanceToggle.UnregisterCallback<ChangeEvent<bool>>(OnMaintenanceChanged);
            _shippedMaintenanceToggle.value = d.IsMaintained;
            _shippedMaintenanceToggle.RegisterCallback<ChangeEvent<bool>>(OnMaintenanceChanged);
        }

        if (_updateProductButton != null) _updateProductButton.SetEnabled(!d.IsUpdating && d.IsOnMarket);
        if (_removeFromMarketButton != null) _removeFromMarketButton.SetEnabled(d.IsOnMarket);
        if (_sequelButton != null) _sequelButton.SetEnabled(d.IsOnMarket && !d.IsUpdating);

        // Marketing panel
        BindShippedMarketingPanel(d);

        // Budget section (player-owned shipped products)
        BindShippedBudgetSection(d);

        // Distribution model panel (tool products only)
        BindDistributionPanel(d);
    }

    private void BindShippedBudgetSection(ShippedProductDisplay d)
    {
        if (_shippedBudgetSection == null) return;

        // Only show for player-owned shipped products
        if (!d.IsOnMarket) {
            _shippedBudgetSection.style.display = DisplayStyle.None;
            return;
        }

        _shippedBudgetSection.style.display = DisplayStyle.Flex;

        // Bind using ProductDetailViewModel cached on viewmodel
        var detailVM = _viewModel?.GetDetailVM(_selectedShippedId);
        if (detailVM == null) return;

        // Maintenance
        if (_shippedMaintBudgetLabel != null) _shippedMaintBudgetLabel.text = "Alloc: " + detailVM.MaintenanceBudgetMonthly;
        if (_shippedMaintDrainLabel != null) _shippedMaintDrainLabel.text = "Drain: " + detailVM.MaintenanceDrainRate;
        if (_shippedMaintCoverageLabel != null) {
            _shippedMaintCoverageLabel.text = detailVM.MaintenanceMonthsCoverage;
            _shippedMaintCoverageLabel.RemoveFromClassList("budget-warning");
            _shippedMaintCoverageLabel.RemoveFromClassList("budget-funded");
            _shippedMaintCoverageLabel.AddToClassList(detailVM.IsMaintenanceUnderfunded ? "budget-warning" : "budget-funded");
        }
        if (_shippedMaintQualityLabel != null) _shippedMaintQualityLabel.text = "Quality: " + detailVM.MaintenanceQualityDisplay;

        // Marketing
        if (_shippedMktBudgetLabel != null) _shippedMktBudgetLabel.text = "Alloc: " + detailVM.MarketingBudgetMonthly;
        if (_shippedMktDrainLabel != null) _shippedMktDrainLabel.text = "Drain: " + detailVM.MarketingDrainRate;
        if (_shippedMktCoverageLabel != null) {
            _shippedMktCoverageLabel.text = detailVM.MarketingMonthsCoverage;
            _shippedMktCoverageLabel.RemoveFromClassList("budget-warning");
            _shippedMktCoverageLabel.RemoveFromClassList("budget-funded");
            _shippedMktCoverageLabel.AddToClassList(detailVM.IsMarketingUnderfunded ? "budget-warning" : "budget-funded");
        }

        // Populate text fields with raw values (without notify to avoid trigger while focused)
        if (_shippedMaintBudgetField != null) {
            bool maintFocused = _shippedMaintBudgetField.focusController?.focusedElement == _shippedMaintBudgetField;
            bool mktFocused = _shippedMktBudgetField != null && _shippedMktBudgetField.focusController?.focusedElement == _shippedMktBudgetField;
            if (!maintFocused || !mktFocused) {
                var gs = GetCurrentGameState();
                if (gs?.productState?.shippedProducts != null &&
                    gs.productState.shippedProducts.TryGetValue(_selectedShippedId, out var prod)) {
                    if (!maintFocused) _shippedMaintBudgetField.SetValueWithoutNotify(prod.MaintenanceBudgetMonthly.ToString());
                    if (!mktFocused) _shippedMktBudgetField?.SetValueWithoutNotify(prod.MarketingBudgetMonthly.ToString());

                    // Team assignment labels
                    if (_shippedQATeamLabel != null || _shippedMktTeamLabel != null)
                    {
                        string qaName = "None";
                        string mktName = "None";
                        var gs2 = GetCurrentGameState();
                        var allTeams = gs2?.teamState?.teams;
                        if (prod.TeamAssignments != null && allTeams != null)
                        {
                            if (prod.TeamAssignments.TryGetValue(ProductTeamRole.QA, out var qaId))
                            {
                                if (allTeams.TryGetValue(qaId, out var qaTeam)) qaName = qaTeam.name;
                            }
                            if (prod.TeamAssignments.TryGetValue(ProductTeamRole.Marketing, out var mktId))
                            {
                                if (allTeams.TryGetValue(mktId, out var mktTeam)) mktName = mktTeam.name;
                            }
                        }
                        if (_shippedQATeamLabel != null) _shippedQATeamLabel.text = qaName;
                        if (_shippedMktTeamLabel != null) _shippedMktTeamLabel.text = mktName;
                        if (_shippedAssignQABtn != null)
                            _shippedAssignQABtn.text = qaName == "None" ? "Assign" : "Change";
                        if (_shippedAssignMktBtn != null)
                            _shippedAssignMktBtn.text = mktName == "None" ? "Assign" : "Change";
                    }
                }
            }
        }
    }

    private void OnMaintBudgetFocusOut(FocusOutEvent evt)
    {
        if (!_hasShippedSelection) return;
        if (_shippedMaintBudgetField == null) return;
        if (long.TryParse(_shippedMaintBudgetField.value, out long amount) && amount >= 0) {
            _dispatcher.Dispatch(new SetProductBudgetCommand {
                Tick = _dispatcher.CurrentTick,
                ProductId = _selectedShippedId,
                BudgetType = ProductBudgetType.Maintenance,
                MonthlyAllocation = amount
            });
            RefreshSelectedShippedDetail();
        }
    }

    private void OnMktBudgetFocusOut(FocusOutEvent evt)
    {
        if (!_hasShippedSelection) return;
        if (_shippedMktBudgetField == null) return;
        if (long.TryParse(_shippedMktBudgetField.value, out long amount) && amount >= 0) {
            _dispatcher.Dispatch(new SetProductBudgetCommand {
                Tick = _dispatcher.CurrentTick,
                ProductId = _selectedShippedId,
                BudgetType = ProductBudgetType.Marketing,
                MonthlyAllocation = amount
            });
            RefreshSelectedShippedDetail();
        }
    }

    private void RefreshSelectedShippedDetail()
    {
        if (_viewModel == null || !_hasShippedSelection) return;
        int count = _viewModel.ShippedProducts.Count;
        for (int i = 0; i < count; i++)
        {
            if (_viewModel.ShippedProducts[i].Id == _selectedShippedId)
            {
                BindShippedDetail(_viewModel.ShippedProducts[i]);
                break;
            }
        }
    }

    private GameState GetCurrentGameState()
    {
        if (_dispatcher is WindowManager wm && wm.GameController != null)
            return wm.GameController.GetGameState();
        return null;
    }

    private void BindDistributionPanel(ShippedProductDisplay d)
    {
        if (_distributionPanel == null) return;

        if (!d.IsTool)
        {
            _distributionPanel.style.display = DisplayStyle.None;
            return;
        }

        _distributionPanel.style.display = DisplayStyle.Flex;

        if (_distributionModelLabel != null)
            _distributionModelLabel.text = d.DistributionModelLabel;

        bool isLicensed = d.DistributionModel == ToolDistributionModel.Licensed;
        bool isOpenSource = d.DistributionModel == ToolDistributionModel.OpenSource;
        bool isProprietary = d.DistributionModel == ToolDistributionModel.Proprietary;

        if (_distributionLicenseeLabel != null)
        {
            _distributionLicenseeLabel.style.display = isLicensed ? DisplayStyle.Flex : DisplayStyle.None;
            _distributionLicenseeLabel.text = "Active Licensees: " + d.ActiveLicenseeCount;
        }

        if (_distributionRevenueLabel != null)
        {
            _distributionRevenueLabel.style.display = (isLicensed || isOpenSource) ? DisplayStyle.Flex : DisplayStyle.None;
            _distributionRevenueLabel.text = "Total Licensing Revenue: " + d.TotalLicensingRevenueDisplay;
        }

        if (_subscriberCountLabel != null)
        {
            _subscriberCountLabel.style.display = isLicensed ? DisplayStyle.Flex : DisplayStyle.None;
            _subscriberCountLabel.text = "Subscribers: " + d.ActiveSubscriberCount;
        }

        if (_subscriptionRevenueLabel != null)
        {
            _subscriptionRevenueLabel.style.display = isLicensed ? DisplayStyle.Flex : DisplayStyle.None;
            _subscriptionRevenueLabel.text = "Monthly Sub Revenue: " + d.MonthlySubscriptionRevenueDisplay;
        }

        if (_totalSubscriptionRevenueLabel != null)
        {
            _totalSubscriptionRevenueLabel.style.display = isLicensed ? DisplayStyle.Flex : DisplayStyle.None;
            _totalSubscriptionRevenueLabel.text = "Total Sub Revenue: " + d.TotalSubscriptionRevenueDisplay;
        }

        var subPriceRow = _shippedSubscriptionPriceSlider?.parent;
        if (subPriceRow != null)
        {
            subPriceRow.style.display = isLicensed ? DisplayStyle.Flex : DisplayStyle.None;
            if (isLicensed && _shippedSubscriptionPriceSlider != null)
            {
                int priceInt = d.MonthlySubscriptionPrice > 0f ? (int)d.MonthlySubscriptionPrice : 20;
                _shippedSubscriptionPriceSlider.UnregisterValueChangedCallback(OnShippedSubscriptionPriceChanged);
                _shippedSubscriptionPriceSlider.SetValueWithoutNotify(priceInt);
                _shippedSubscriptionPriceSlider.RegisterValueChangedCallback(OnShippedSubscriptionPriceChanged);
                if (_shippedSubscriptionPriceValueLabel != null)
                    _shippedSubscriptionPriceValueLabel.text = "$" + priceInt;
            }
        }

        var licensingRateRow = _licensingRateSlider?.parent;
        if (licensingRateRow != null)
        {
            licensingRateRow.style.display = isLicensed ? DisplayStyle.Flex : DisplayStyle.None;
            if (isLicensed && _licensingRateSlider != null)
            {
                _licensingRateSlider.UnregisterValueChangedCallback(OnLicensingRateChanged);
                _licensingRateSlider.SetValueWithoutNotify(d.PlayerLicensingRate * 100f);
                _licensingRateSlider.RegisterValueChangedCallback(OnLicensingRateChanged);
                if (_licensingRateValueLabel != null)
                    _licensingRateValueLabel.text = (d.PlayerLicensingRate * 100f).ToString("F0") + "%";
            }
        }

        if (_releaseToMarketBtn != null)
            _releaseToMarketBtn.style.display = isProprietary ? DisplayStyle.Flex : DisplayStyle.None;
        if (_pullFromMarketBtn != null)
            _pullFromMarketBtn.style.display = isLicensed ? DisplayStyle.Flex : DisplayStyle.None;
        if (_openSourceBtn != null)
            _openSourceBtn.style.display = !isOpenSource ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private void OnShippedSubscriptionPriceChanged(ChangeEvent<int> evt)
    {
        if (!_hasShippedSelection) return;
        if (_shippedSubscriptionPriceValueLabel != null)
            _shippedSubscriptionPriceValueLabel.text = "$" + evt.newValue;
        var d = _viewModel?.SelectedShippedProduct ?? default;
        _dispatcher.Dispatch(new SetToolDistributionCommand {
            ProductId = _selectedShippedId,
            Model = ToolDistributionModel.Licensed,
            LicensingRate = d.PlayerLicensingRate,
            MonthlySubscriptionPrice = evt.newValue
        });
    }

    private void OnLicensingRateChanged(ChangeEvent<float> evt)
    {
        if (!_hasShippedSelection) return;
        float rate = evt.newValue / 100f;
        if (_licensingRateValueLabel != null)
            _licensingRateValueLabel.text = evt.newValue.ToString("F0") + "%";
        _dispatcher.Dispatch(new SetToolDistributionCommand {
            ProductId = _selectedShippedId,
            Model = ToolDistributionModel.Licensed,
            LicensingRate = rate
        });
    }

    private void OnReleaseToMarketClicked()
    {
        if (!_hasShippedSelection) return;
        var d = _viewModel?.SelectedShippedProduct ?? default;
        float subPrice = d.MonthlySubscriptionPrice > 0f ? d.MonthlySubscriptionPrice : 20f;
        _dispatcher.Dispatch(new SetToolDistributionCommand {
            ProductId = _selectedShippedId,
            Model = ToolDistributionModel.Licensed,
            LicensingRate = 0.15f,
            MonthlySubscriptionPrice = subPrice
        });
    }

    private void OnPullFromMarketClicked()
    {
        if (!_hasShippedSelection) return;
        _dispatcher.Dispatch(new SetToolDistributionCommand {
            ProductId = _selectedShippedId,
            Model = ToolDistributionModel.Proprietary,
            LicensingRate = 0f
        });
    }

    private void OnOpenSourceClicked()
    {
        if (!_hasShippedSelection) return;
        _dispatcher.Dispatch(new SetToolDistributionCommand {
            ProductId = _selectedShippedId,
            Model = ToolDistributionModel.OpenSource,
            LicensingRate = 0f
        });
    }

    private void BindShippedMarketingPanel(ShippedProductDisplay d)
    {
        if (_shippedMarketingPanel == null) return;

        _shippedMarketingPanel.style.display = d.HasMarketingTeam ? DisplayStyle.Flex : DisplayStyle.None;
        if (!d.HasMarketingTeam) return;

        if (_shippedHypeLabel != null) _shippedHypeLabel.text = d.HypeScoreDisplay;

        if (_shippedHypeBarFill != null)
        {
            _shippedHypeBarFill.style.width = Length.Percent(d.HypeScoreNormalized * 100f);
            _shippedHypeBarFill.RemoveFromClassList("hype-low");
            _shippedHypeBarFill.RemoveFromClassList("hype-mid");
            _shippedHypeBarFill.RemoveFromClassList("hype-high");
            string hypeClass = d.HypeScore >= 67f ? "hype-high" : d.HypeScore >= 34f ? "hype-mid" : "hype-low";
            _shippedHypeBarFill.AddToClassList(hypeClass);
        }

        bool hasUpdateHype = d.HasAnnouncedUpdate && !string.IsNullOrEmpty(d.UpdateHypeDisplay);
        if (_shippedUpdateHypeLabel != null)
        {
            _shippedUpdateHypeLabel.text = d.UpdateHypeDisplay;
            _shippedUpdateHypeLabel.parent.style.display = hasUpdateHype ? DisplayStyle.Flex : DisplayStyle.None;
        }
        if (_shippedUpdateHypeBarFill != null && hasUpdateHype)
        {
            _shippedUpdateHypeBarFill.style.width = Length.Percent(d.UpdateHypeNormalized * 100f);
            _shippedUpdateHypeBarFill.RemoveFromClassList("hype-low");
            _shippedUpdateHypeBarFill.RemoveFromClassList("hype-mid");
            _shippedUpdateHypeBarFill.RemoveFromClassList("hype-high");
            float updateHypeScore = d.UpdateHypeNormalized * 100f;
            string uhClass = updateHypeScore >= 67f ? "hype-high" : updateHypeScore >= 34f ? "hype-mid" : "hype-low";
            _shippedUpdateHypeBarFill.AddToClassList(uhClass);
        }

        if (_shippedAdStatusLabel != null) _shippedAdStatusLabel.text = d.AdStatusDisplay;
        if (_shippedStartMarketingBtn != null) _shippedStartMarketingBtn.style.display = d.IsMarketingActive ? DisplayStyle.None : DisplayStyle.Flex;
        if (_shippedStopMarketingBtn != null) _shippedStopMarketingBtn.style.display = d.IsMarketingActive ? DisplayStyle.Flex : DisplayStyle.None;
        if (_shippedRunAdsBtn != null) _shippedRunAdsBtn.SetEnabled(d.CanRunAds);
        if (_shippedAnnounceUpdateBtn != null) _shippedAnnounceUpdateBtn.SetEnabled(d.CanAnnounceUpdate);
    }

    // ── Detail binding ─────────────────────────────────────────────────────────

    private void BindDetail(ProductsViewModel vm)
    {
        if (_detailNameLabel != null) _detailNameLabel.text = vm.SelectedProductName ?? "";
        if (_detailTemplateLabel != null)
        {
            string typeLabel = vm.SelectedProductTypeLabel ?? vm.SelectedTemplateName ?? "";
            string pricingLabel = vm.SelectedPricingLabel;

            string detail = typeLabel;
            if (!string.IsNullOrEmpty(pricingLabel)) detail += " — " + pricingLabel;
            _detailTemplateLabel.text = detail;
        }

        if (_detailDurationLabel != null)
            _detailDurationLabel.text = vm.SelectedCreatedDateLabel + " — " + vm.SelectedDevDurationLabel;

        if (_devBugIndicator != null)
        {
            _devBugIndicator.text = vm.CodeHealthLabel;
            _devBugIndicator.RemoveFromClassList("trend--up");
            _devBugIndicator.RemoveFromClassList("trend--down");
            _devBugIndicator.RemoveFromClassList("bug-indicator--warning");
            _devBugIndicator.RemoveFromClassList("code-health--good");
            _devBugIndicator.RemoveFromClassList("code-health--fair");
            _devBugIndicator.RemoveFromClassList("code-health--poor");
            if (!string.IsNullOrEmpty(vm.CodeHealthClass))
                _devBugIndicator.AddToClassList(vm.CodeHealthClass);
        }

        int phaseCount = vm.PhaseCount;

        for (int i = 0; i < MaxPhaseRows; i++)
        {
            if (_phaseRows[i] == null) continue;

            if (i >= phaseCount)
            {
                _phaseRows[i].style.display = DisplayStyle.None;
                continue;
            }

            _phaseRows[i].style.display = DisplayStyle.Flex;
            var ph = vm.Phases[i];

            _phaseLabels[i].text = ph.PhaseLabel;

            if (_phaseProgressFills[i] != null)
            {
                float target = ph.IsIterating
                    ? ph.IterationProgressPercent * 100f
                    : ph.WorkProgressPercent * 100f;
                if (!_fillPercents.TryGetValue(_phaseProgressFills[i], out float cur)) cur = 0f;
                if (_fillTweeners.TryGetValue(_phaseProgressFills[i], out var t)) t?.Kill();
                _fillTweeners[_phaseProgressFills[i]] = UIAnimator.ProgressFill(_phaseProgressFills[i], cur, target);
                _fillPercents[_phaseProgressFills[i]] = target;

                _phaseProgressFills[i].RemoveFromClassList("progress-bar__fill");
                _phaseProgressFills[i].RemoveFromClassList("progress-bar__fill--success");
                _phaseProgressFills[i].RemoveFromClassList("progress-bar__fill--warning");
                _phaseProgressFills[i].RemoveFromClassList("progress-bar__fill--iterating");
                if (ph.IsIterating)
                    _phaseProgressFills[i].AddToClassList("progress-bar__fill--iterating");
                else
                    _phaseProgressFills[i].AddToClassList(ph.FillClass);
            }

            _phaseQualityLabels[i].text = ph.IsComplete ? ((int)ph.Quality + "%") : "";

            UpdatePhaseBadge(i, ph.StatusBadgeText, ph.StatusBadgeClass);

            _phaseTeamLabels[i].text = ph.AssignedTeamName;
            _phaseTeamLabels[i].RemoveFromClassList("text-accent");
            _phaseTeamLabels[i].RemoveFromClassList("text-muted");
            _phaseTeamLabels[i].AddToClassList(ph.AssignedTeamName == "Unassigned" ? "text-muted" : "text-accent");

            if (_phaseBugLabels != null && i < _phaseBugLabels.Length && _phaseBugLabels[i] != null)
            {
                if (ph.PhaseType == ProductPhaseType.QA || ph.BugAccumulation <= 0f)
                    _phaseBugLabels[i].style.display = DisplayStyle.None;
                else
                {
                    _phaseBugLabels[i].style.display = DisplayStyle.Flex;
                    _phaseBugLabels[i].text = ph.BugAccumulation.ToString("F0") + " bugs";
                }
            }

            _phaseIterateButtons[i].style.display = ph.CanIterate ? DisplayStyle.Flex : DisplayStyle.None;
            WireIterateButton(_phaseIterateButtons[i], vm.SelectedProductId, ph.PhaseType);

            _phaseAssignButtons[i].style.display = !ph.IsLocked ? DisplayStyle.Flex : DisplayStyle.None;
            WireAssignButton(_phaseAssignButtons[i], vm.SelectedProductId, ph.PrimaryRole, ph.PhaseType);
        }

        if (_shipButton != null) _shipButton.SetEnabled(vm.CanShip);

        // Release date binding
        if (_releaseDateLabel != null)
            _releaseDateLabel.text = vm.SelectedReleaseDateLabel;

        if (_daysUntilReleaseLabel != null)
        {
            _daysUntilReleaseLabel.text = vm.SelectedDaysUntilReleaseLabel;
            _daysUntilReleaseLabel.RemoveFromClassList("text-success");
            _daysUntilReleaseLabel.RemoveFromClassList("text-warning");
            _daysUntilReleaseLabel.RemoveFromClassList("trend--down");
            if (vm.SelectedIsOverdue)
                _daysUntilReleaseLabel.AddToClassList("trend--down");
            else if (vm.SelectedHasReleaseDate)
            {
                if (vm.SelectedDaysUntilRelease <= 7)
                    _daysUntilReleaseLabel.AddToClassList("trend--down");
                else if (vm.SelectedDaysUntilRelease <= 30)
                    _daysUntilReleaseLabel.AddToClassList("text-warning");
                else
                    _daysUntilReleaseLabel.AddToClassList("text-success");
            }
        }

        if (_shiftCountBadge != null)
        {
            bool hasShifts = vm.SelectedDateShiftCount > 0;
            _shiftCountBadge.style.display = hasShifts ? DisplayStyle.Flex : DisplayStyle.None;
            if (hasShifts)
                _shiftCountBadge.text = "Shifted " + vm.SelectedDateShiftCount + "x";
        }

        if (_changeDateButton != null)
            _changeDateButton.style.display = vm.SelectedHasReleaseDate ? DisplayStyle.Flex : DisplayStyle.None;

        if (_setReleaseDateButton != null)
            _setReleaseDateButton.style.display = vm.SelectedHasReleaseDate ? DisplayStyle.None : DisplayStyle.Flex;

        if (_setReleaseDateFlyout != null && vm.SelectedHasReleaseDate)
            _setReleaseDateFlyout.style.display = DisplayStyle.None;

        // Dev marketing binding
        if (_devHypeLabel != null) _devHypeLabel.text = vm.DevHypeScoreDisplay;
        if (_devMarketingTeamLabel != null) _devMarketingTeamLabel.text = vm.DevMarketingTeamDisplay;
        if (_devHypeBarFill != null)
        {
            _devHypeBarFill.style.width = Length.Percent(vm.DevHypeScoreNormalized * 100f);
            _devHypeBarFill.RemoveFromClassList("hype-low");
            _devHypeBarFill.RemoveFromClassList("hype-mid");
            _devHypeBarFill.RemoveFromClassList("hype-high");
            string hypeClass = vm.DevHypeScore >= 67f ? "hype-high" : vm.DevHypeScore >= 34f ? "hype-mid" : "hype-low";
            _devHypeBarFill.AddToClassList(hypeClass);
        }

        // Dev marketing budget field
        if (_devMktBudgetField != null)
        {
            bool devMktFocused = _devMktBudgetField.focusController?.focusedElement == _devMktBudgetField;
            if (!devMktFocused)
                _devMktBudgetField.SetValueWithoutNotify(vm.DevMarketingBudgetRaw.ToString());
        }
        if (_devMktBudgetAllocLabel != null)
            _devMktBudgetAllocLabel.text = vm.DevMarketingBudgetDisplay;
        if (_devMktBudgetStatusLabel != null)
        {
            _devMktBudgetStatusLabel.RemoveFromClassList("text-accent");
            _devMktBudgetStatusLabel.RemoveFromClassList("text-muted");
            _devMktBudgetStatusLabel.RemoveFromClassList("text-warning");
            if (!vm.DevHasMarketingTeam)
            {
                _devMktBudgetStatusLabel.text = "No Marketing Team";
                _devMktBudgetStatusLabel.AddToClassList("text-warning");
            }
            else if (vm.DevMarketingBudgetRaw > 0)
            {
                _devMktBudgetStatusLabel.text = "Active";
                _devMktBudgetStatusLabel.AddToClassList("text-accent");
            }
            else
            {
                _devMktBudgetStatusLabel.text = "Inactive";
                _devMktBudgetStatusLabel.AddToClassList("text-muted");
            }
        }

        // Crunch all button
        if (_crunchAllBtn != null)
        {
            _crunchAllBtn.style.display = vm.HasActivePhasesWithTeams ? DisplayStyle.Flex : DisplayStyle.None;
            _crunchAllBtn.text = vm.AnyActivePhaseCrunching ? "Stop Crunch" : "Crunch All Teams";
            _crunchAllBtn.RemoveFromClassList("btn-warning");
            _crunchAllBtn.RemoveFromClassList("btn-danger");
            _crunchAllBtn.AddToClassList(vm.AnyActivePhaseCrunching ? "btn-danger" : "btn-warning");
        }
    }

    private void UpdatePhaseBadge(int index, string text, string newClass)
    {
        var badge = _phaseBadges[index];
        if (badge == null) return;
        badge.text = text;
        string oldClass = _phaseBadgeCurrentClass[index];
        if (oldClass != newClass)
        {
            if (!string.IsNullOrEmpty(oldClass)) badge.RemoveFromClassList(oldClass);
            if (!string.IsNullOrEmpty(newClass)) badge.AddToClassList(newClass);
            _phaseBadgeCurrentClass[index] = newClass;
        }
    }

    private void WireIterateButton(Button btn, ProductId productId, ProductPhaseType phaseType)
    {
        if (btn.userData is System.Action prevAction)
            btn.clicked -= prevAction;

        System.Action handler = () => _dispatcher.Dispatch(new IteratePhaseCommand {
            ProductId = productId,
            PhaseType = phaseType
        });
        btn.userData = handler;
        btn.clicked += handler;
    }

    private void WireAssignButton(Button btn, ProductId productId, ProductTeamRole role, ProductPhaseType phaseType)
    {
        if (btn.userData is System.Action prevAction)
            btn.clicked -= prevAction;

        System.Action handler = () => ShowTeamAssignFlyout(btn, productId, role);
        btn.userData = handler;
        btn.clicked += handler;
    }

    private void ShowTeamAssignFlyout(Button anchor, ProductId productId, ProductTeamRole role)
    {
        var flyout = anchor.parent?.Q<VisualElement>("phase-team-flyout-" + role);
        if (flyout == null)
        {
            flyout = new VisualElement();
            flyout.name = "phase-team-flyout-" + role;
            flyout.AddToClassList("card");
            flyout.style.position = Position.Absolute;
            flyout.style.top = anchor.layout.height + 4;
            flyout.style.right = 0;
            flyout.style.minWidth = 160;
            flyout.style.maxHeight = 160;
            flyout.style.overflow = Overflow.Hidden;
            anchor.parent?.Add(flyout);
        }

        bool isVisible = flyout.style.display == DisplayStyle.Flex;
        flyout.style.display = isVisible ? DisplayStyle.None : DisplayStyle.Flex;

        if (!isVisible)
        {
            flyout.Clear();
            var scroll = new ScrollView();
            scroll.style.maxHeight = 150;
            var teams = _viewModel?.AvailableTeams;
            int tc = teams != null ? teams.Count : 0;
            for (int i = 0; i < tc; i++)
            {
                var td = teams[i];
                var optBtn = new Button { text = td.Name };
                optBtn.AddToClassList("btn-secondary");
                optBtn.AddToClassList("btn-sm");
                optBtn.style.marginBottom = 2;
                var capturedTeamId = td.Id;
                var capturedRole = role;
                var capturedProductId = productId;
                var capturedFlyout = flyout;
                optBtn.clicked += () => {
                    _dispatcher.Dispatch(new AssignTeamToProductCommand {
                        ProductId = capturedProductId,
                        TeamId = capturedTeamId,
                        RoleSlot = capturedRole
                    });
                    capturedFlyout.style.display = DisplayStyle.None;
                };
                scroll.Add(optBtn);
            }
            if (tc == 0)
                scroll.Add(new Label("No available teams") { });
            flyout.Add(scroll);
        }
    }

    // ── Button handlers ────────────────────────────────────────────────────────

    private void OnShipClicked()
    {
        if (!_hasSelection) return;

        bool isTool = false;
        if (_dispatcher is WindowManager wm && wm.GameController != null)
        {
            var gs = wm.GameController.GetGameState();
            if (gs?.productState?.developmentProducts != null &&
                gs.productState.developmentProducts.TryGetValue(_selectedProductId, out var product))
                isTool = product.Category.IsTool();
        }

        if (isTool)
        {
            _pendingDistributionModel = ToolDistributionModel.Proprietary;
            _pendingLicensingRate = 0.15f;
            _shipDistributionFlyout.style.display = DisplayStyle.Flex;
            UpdateShipDistributionFlyout();
        }
        else
        {
            _dispatcher.Dispatch(new ShipProductCommand {
                ProductId = _selectedProductId,
                DistributionModel = ToolDistributionModel.Proprietary,
                LicensingRate = 0f
            });
        }
    }

    private void OnShipDistProprietaryClicked()
    {
        _pendingDistributionModel = ToolDistributionModel.Proprietary;
        UpdateShipDistributionFlyout();
    }

    private void OnShipDistLicensedClicked()
    {
        _pendingDistributionModel = ToolDistributionModel.Licensed;
        UpdateShipDistributionFlyout();
    }

    private void OnShipDistOpenSourceClicked()
    {
        _pendingDistributionModel = ToolDistributionModel.OpenSource;
        UpdateShipDistributionFlyout();
    }

    private void OnShipDistRateChanged(ChangeEvent<float> evt)
    {
        _pendingLicensingRate = evt.newValue / 100f;
        if (_shipDistRateValueLabel != null)
            _shipDistRateValueLabel.text = evt.newValue.ToString("F0") + "%";
    }

    private void UpdateShipDistributionFlyout()
    {
        if (_shipDistProprietaryBtn != null)
        {
            bool p = _pendingDistributionModel == ToolDistributionModel.Proprietary;
            if (p) _shipDistProprietaryBtn.AddToClassList("btn-primary");
            else _shipDistProprietaryBtn.RemoveFromClassList("btn-primary");
            if (!p) _shipDistProprietaryBtn.AddToClassList("btn-secondary");
            else _shipDistProprietaryBtn.RemoveFromClassList("btn-secondary");
        }
        if (_shipDistLicensedBtn != null)
        {
            bool l = _pendingDistributionModel == ToolDistributionModel.Licensed;
            if (l) _shipDistLicensedBtn.AddToClassList("btn-primary");
            else _shipDistLicensedBtn.RemoveFromClassList("btn-primary");
            if (!l) _shipDistLicensedBtn.AddToClassList("btn-secondary");
            else _shipDistLicensedBtn.RemoveFromClassList("btn-secondary");
        }
        if (_shipDistOpenSourceBtn != null)
        {
            bool o = _pendingDistributionModel == ToolDistributionModel.OpenSource;
            if (o) _shipDistOpenSourceBtn.AddToClassList("btn-primary");
            else _shipDistOpenSourceBtn.RemoveFromClassList("btn-primary");
            if (!o) _shipDistOpenSourceBtn.AddToClassList("btn-secondary");
            else _shipDistOpenSourceBtn.RemoveFromClassList("btn-secondary");
        }
        if (_shipDistRateRow != null)
            _shipDistRateRow.style.display = _pendingDistributionModel == ToolDistributionModel.Licensed ? DisplayStyle.Flex : DisplayStyle.None;
        if (_shipDistWarningLabel != null)
            _shipDistWarningLabel.style.display = _pendingDistributionModel == ToolDistributionModel.OpenSource ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private void OnConfirmShipClicked()
    {
        if (!_hasSelection) return;
        _shipDistributionFlyout.style.display = DisplayStyle.None;
        _dispatcher.Dispatch(new ShipProductCommand {
            ProductId = _selectedProductId,
            DistributionModel = _pendingDistributionModel,
            LicensingRate = _pendingDistributionModel == ToolDistributionModel.Licensed ? _pendingLicensingRate : 0f
        });
    }

    private void OnAbandonClicked()
    {
        if (_hasSelection)
        {
            _dispatcher.Dispatch(new AbandonProductCommand { ProductId = _selectedProductId });
            _hasSelection = false;
            _detailPanel.style.display = DisplayStyle.None;
            if (_detailEmptyState != null) _detailEmptyState.style.display = DisplayStyle.Flex;
        }
    }

    private void OnChangeDateClicked()
    {
        if (!_hasSelection || _changeDateFlyout == null) return;
        bool isVisible = _changeDateFlyout.style.display == DisplayStyle.Flex;
        _changeDateFlyout.style.display = isVisible ? DisplayStyle.None : DisplayStyle.Flex;
        if (!isVisible)
        {
            int currentDay = 0;
            int targetTick = 0;
            if (_dispatcher is WindowManager wm && wm.GameController != null)
            {
                var gs = wm.GameController.GetGameState();
                currentDay = gs?.timeState?.currentDay ?? 0;
                if (gs?.productState?.developmentProducts != null &&
                    gs.productState.developmentProducts.TryGetValue(_selectedProductId, out var prod))
                    targetTick = prod.TargetReleaseTick;
            }
            int currentOffset = targetTick > 0
                ? (targetTick / TimeState.TicksPerDay) - currentDay
                : 180;
            if (currentOffset < 1) currentOffset = 1;
            if (currentOffset > 730) currentOffset = 730;
            if (_changeDateSlider != null)
                _changeDateSlider.SetValueWithoutNotify(currentOffset);
            UpdateChangeDateLabel(currentDay, currentOffset, targetTick);
        }
    }

    private void OnChangeDateSliderChanged(ChangeEvent<int> evt)
    {
        if (!_hasSelection) return;
        int currentDay = 0;
        int originalTargetTick = 0;
        if (_dispatcher is WindowManager wm && wm.GameController != null)
        {
            var gs = wm.GameController.GetGameState();
            currentDay = gs?.timeState?.currentDay ?? 0;
            if (gs?.productState?.developmentProducts != null &&
                gs.productState.developmentProducts.TryGetValue(_selectedProductId, out var prod))
                originalTargetTick = prod.TargetReleaseTick;
        }
        UpdateChangeDateLabel(currentDay, evt.newValue, originalTargetTick);
    }

    private void UpdateChangeDateLabel(int currentDay, int offsetDays, int originalTargetTick)
    {
        int newAbsoluteDay = currentDay + offsetDays;
        int dom = TimeState.GetDayOfMonth(newAbsoluteDay);
        int mon = TimeState.GetMonth(newAbsoluteDay);
        int yr = TimeState.GetYear(newAbsoluteDay);
        if (_changeDateValueLabel != null)
            _changeDateValueLabel.text = UIFormatting.FormatDate(dom, mon, yr);

        if (_changeDatePenaltyLabel != null)
        {
            if (originalTargetTick > 0)
            {
                int originalDay = originalTargetTick / TimeState.TicksPerDay;
                int diff = newAbsoluteDay - originalDay;
                if (diff < -7)
                    _changeDatePenaltyLabel.text = "Rushing: overtime costs and hype decay";
                else if (diff > 7)
                    _changeDatePenaltyLabel.text = "Delaying: reputation penalty and hype decay";
                else
                    _changeDatePenaltyLabel.text = "";
                _changeDatePenaltyLabel.style.display = _changeDatePenaltyLabel.text.Length > 0
                    ? DisplayStyle.Flex : DisplayStyle.None;
            }
            else
            {
                _changeDatePenaltyLabel.style.display = DisplayStyle.None;
            }
        }
    }

    private void OnChangeDateConfirmed()
    {
        if (!_hasSelection || _changeDateSlider == null) return;
        int currentDay = 0;
        if (_dispatcher is WindowManager wm && wm.GameController != null)
        {
            var gs = wm.GameController.GetGameState();
            currentDay = gs?.timeState?.currentDay ?? 0;
        }
        int newAbsoluteDay = currentDay + _changeDateSlider.value;
        _dispatcher.Dispatch(new ChangeReleaseDateCommand {
            ProductId = _selectedProductId,
            NewTargetDay = newAbsoluteDay
        });
        if (_changeDateFlyout != null)
            _changeDateFlyout.style.display = DisplayStyle.None;
    }

    private void OnSetReleaseDateClicked()
    {
        if (!_hasSelection || _setReleaseDateFlyout == null) return;
        bool isVisible = _setReleaseDateFlyout.style.display == DisplayStyle.Flex;
        _setReleaseDateFlyout.style.display = isVisible ? DisplayStyle.None : DisplayStyle.Flex;
        if (!isVisible)
        {
            _setReleaseDateSlider?.SetValueWithoutNotify(180);
            UpdateSetReleaseDateLabel(180);
        }
    }

    private void OnSetReleaseDateSliderChanged(ChangeEvent<int> evt)
    {
        UpdateSetReleaseDateLabel(evt.newValue);
    }

    private void UpdateSetReleaseDateLabel(int offsetDays)
    {
        int currentDay = 0;
        if (_dispatcher is WindowManager wm && wm.GameController != null)
        {
            var gs = wm.GameController.GetGameState();
            currentDay = gs?.timeState?.currentDay ?? 0;
        }
        int newAbsoluteDay = currentDay + offsetDays;
        int dom = TimeState.GetDayOfMonth(newAbsoluteDay);
        int mon = TimeState.GetMonth(newAbsoluteDay);
        int yr = TimeState.GetYear(newAbsoluteDay);
        if (_setReleaseDateValueLabel != null)
            _setReleaseDateValueLabel.text = UIFormatting.FormatDate(dom, mon, yr);
    }

    private void OnSetReleaseDateConfirmed()
    {
        if (!_hasSelection || _setReleaseDateSlider == null) return;
        int currentDay = 0;
        if (_dispatcher is WindowManager wm && wm.GameController != null)
        {
            var gs = wm.GameController.GetGameState();
            currentDay = gs?.timeState?.currentDay ?? 0;
        }
        int newAbsoluteDay = currentDay + _setReleaseDateSlider.value;
        _dispatcher.Dispatch(new AnnounceReleaseDateCommand {
            ProductId = _selectedProductId,
            TargetDay = newAbsoluteDay
        });
        if (_setReleaseDateFlyout != null)
            _setReleaseDateFlyout.style.display = DisplayStyle.None;
    }

    private void OnUpdateProductClicked()
    {
        if (!_hasShippedSelection) return;
        SwapToCreateMode(_selectedShippedId, null);
    }

    private void OnSequelClicked()
    {
        if (!_hasShippedSelection) return;
        SwapToCreateMode(null, _selectedShippedId);
    }

    private void OnRemoveFromMarketClicked()
    {
        if (!_hasShippedSelection) return;
        _dispatcher.Dispatch(new RemoveProductFromMarketCommand { ProductId = _selectedShippedId });
        _hasShippedSelection = false;
        _shippedDetailPanel.style.display = DisplayStyle.None;
        if (_shippedDetailEmptyState != null) _shippedDetailEmptyState.style.display = DisplayStyle.Flex;
    }

    private void OnMaintenanceChanged(ChangeEvent<bool> evt)
    {
        if (!_hasShippedSelection) return;
        long allocation = evt.newValue ? 5000L : 0L;
        _dispatcher.Dispatch(new SetProductBudgetCommand {
            Tick = _dispatcher.CurrentTick,
            ProductId = _selectedShippedId,
            BudgetType = ProductBudgetType.Maintenance,
            MonthlyAllocation = allocation
        });
    }

    private void OnNewProductClicked()
    {
        SwapToCreateMode(null, null);
    }

    // ── Create mode swap ───────────────────────────────────────────────────────

    private void SwapToCreateMode(ProductId? updateId, ProductId? sequelOfId)
    {
        _isInCreateMode = true;

        if (_devSection != null) _devSection.style.display = DisplayStyle.None;
        if (_shippedSection != null) _shippedSection.style.display = DisplayStyle.None;

        var definitions = _viewModel?.CachedTemplates ?? new ProductTemplateDefinition[0];
        var niches = _viewModel?.CachedNicheData ?? new MarketNicheData[0];

        _createProductVm = new ProductCreationPlanningViewModel();
        _createProductVm.SetNicheData(niches);
        _createProductVm.SetTemplates(definitions);

        if (_dispatcher is WindowManager wmCreate && wmCreate.GameController != null)
        {
            var snapshot = wmCreate.GetCurrentSnapshot() as GameStateSnapshot;

            if (snapshot != null)
            {
                _createProductVm.Refresh(snapshot);

                if (_dispatcher is WindowManager wmHw && wmHw.GameController != null)
                {
                    var gc = wmHw.GameController;
                    int currentGen = gc.GenerationSystem?.GetCurrentGeneration() ?? 1;
                    var hwConfigs = gc.HardwareGenerationConfigs;
                    if (hwConfigs != null && hwConfigs.Length > 0)
                    {
                        HardwareGenerationConfig hwFound = null;
                        for (int i = 0; i < hwConfigs.Length; i++)
                        {
                            if (hwConfigs[i] != null && hwConfigs[i].generation == currentGen)
                            {
                                hwFound = hwConfigs[i];
                                break;
                            }
                        }
                        if (hwFound == null)
                        {
                            for (int i = 0; i < hwConfigs.Length; i++)
                            {
                                if (hwConfigs[i] != null && hwConfigs[i].generation <= currentGen)
                                {
                                    if (hwFound == null || hwConfigs[i].generation > hwFound.generation)
                                        hwFound = hwConfigs[i];
                                }
                            }
                        }
                        if (hwFound == null) hwFound = hwConfigs[0];
                        _createProductVm.SetHardwareGenConfig(hwFound);
                    }
                }
            }
        }

        _createProductView = new ProductCreationPlanningView(_dispatcher, _modal);
        _createProductView.OnCancelRequested += OnCreateCancelled;
        _createProductView.OnProductCreated += OnCreateConfirmed;

        _createProductContainer.Clear();
        _createProductView.Initialize(_createProductContainer);
        _createProductView.Bind(_createProductVm);
        _createProductContainer.style.display = DisplayStyle.Flex;
    }

    private void OnCreateCancelled()
    {
        SwapToListMode();
    }

    private void OnCreateConfirmed()
    {
        SwapToListMode();
    }

    private void SwapToListMode()
    {
        _isInCreateMode = false;

        if (_createProductView != null)
        {
            _createProductView.OnCancelRequested -= OnCreateCancelled;
            _createProductView.OnProductCreated -= OnCreateConfirmed;
            _createProductView.Dispose();
            _createProductView = null;
            _createProductVm = null;
        }

        _createProductContainer.Clear();
        _createProductContainer.style.display = DisplayStyle.None;

        if (_devSection != null) _devSection.style.display = _viewMode == ProductsViewMode.InDevelopment ? DisplayStyle.Flex : DisplayStyle.None;
        if (_shippedSection != null) _shippedSection.style.display = _viewMode == ProductsViewMode.Live ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private void OnDevMktBudgetFocusOut(FocusOutEvent evt)
    {
        if (!_hasSelection) return;
        if (_devMktBudgetField == null) return;
        if (long.TryParse(_devMktBudgetField.value, out long amount) && amount >= 0)
        {
            _dispatcher.Dispatch(new SetProductBudgetCommand {
                Tick = _dispatcher.CurrentTick,
                ProductId = _selectedProductId,
                BudgetType = ProductBudgetType.Marketing,
                MonthlyAllocation = amount
            });
        }
    }

    private void OnCrunchAllClicked(ClickEvent evt)
    {
        if (_viewModel == null || !_hasSelection) return;
        var phases = _viewModel.Phases;
        bool enabling = !_viewModel.AnyActivePhaseCrunching;
        int count = _viewModel.PhaseCount;
        for (int i = 0; i < count; i++)
        {
            var ph = phases[i];
            if (ph.AssignedTeamId.Equals(default)) continue;
            if (enabling && (ph.IsLocked || ph.IsComplete)) continue;
            _dispatcher.Dispatch(new SetCrunchModeCommand {
                Tick = _dispatcher.CurrentTick,
                TeamId = ph.AssignedTeamId,
                Enable = enabling
            });
        }
    }

    private void OnShippedAssignQAClicked()
    {
        if (!_hasShippedSelection || _shippedAssignQABtn == null) return;
        ShowTeamAssignFlyout(_shippedAssignQABtn, _selectedShippedId, ProductTeamRole.QA);
    }

    private void OnShippedAssignMktClicked()
    {
        if (!_hasShippedSelection || _shippedAssignMktBtn == null) return;
        ShowTeamAssignFlyout(_shippedAssignMktBtn, _selectedShippedId, ProductTeamRole.Marketing);
    }

    private void OnShippedStartMarketingClicked()
    {
        if (!_hasShippedSelection) return;
        _dispatcher.Dispatch(new SetProductBudgetCommand {
            Tick = _dispatcher.CurrentTick,
            ProductId = _selectedShippedId,
            BudgetType = ProductBudgetType.Marketing,
            MonthlyAllocation = 3000L
        });
    }

    private void OnShippedStopMarketingClicked()
    {
        if (!_hasShippedSelection) return;
        _dispatcher.Dispatch(new SetProductBudgetCommand {
            Tick = _dispatcher.CurrentTick,
            ProductId = _selectedShippedId,
            BudgetType = ProductBudgetType.Marketing,
            MonthlyAllocation = 0L
        });
    }

    private void OnShippedRunAdsClicked()
    {
        if (!_hasShippedSelection) return;
        _dispatcher.Dispatch(new RunAdsCommand {
            Tick = _dispatcher.CurrentTick,
            ProductId = _selectedShippedId,
            SpendAmount = 5000
        });
    }

    private void OnShippedAnnounceUpdateClicked()
    {
        if (!_hasShippedSelection) return;
        _dispatcher.Dispatch(new AnnounceUpdateCommand {
            Tick = _dispatcher.CurrentTick,
            ProductId = _selectedShippedId
        });
    }
}
