using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class CreateProductView : IGameView
{
    private readonly ICommandDispatcher _dispatcher;
    private readonly ITooltipProvider _tooltip;
    private readonly ProductTemplateDefinition[] _definitions;
    private readonly MarketNicheData[] _nicheData;
    private readonly ProductId? _updateProductId;
    private readonly ProductId? _sequelOfId;

    private CreateProductViewModel _viewModel;
    private VisualElement _root;

    // ── Header ────────────────────────────────────────────────────────────────
    private Button _cancelBtn;
    private Label _headerTitleLabel;
    private Button _startDevBtn;

    // ── Right panel — feature selection ──────────────────────────────────────
    private VisualElement _rightPanel;
    private VisualElement _featureCategoryTabBar;
    private readonly List<Button> _categoryTabButtons = new List<Button>();
    private VisualElement _filterBar;
    private readonly List<Button> _selectedFilterBtns = new List<Button>();
    private readonly List<Button> _sortModeBtns = new List<Button>();
    private ScrollView _featureScroll;
    private VisualElement _featureContainer;
    private ElementPool _featurePool;
    private Label _featureSummaryLabel;
    private string _lastFeatureTemplateId;

    // Pre-allocated list for pool binding — avoids per-Bind allocation
    private readonly List<int> _filteredIndicesBind = new List<int>();

    // ── Product Summary (left panel read-only) ────────────────────────────────
    private Label _summaryName;
    private Label _summaryType;
    private Label _summaryPlatform;
    private Label _summaryReach;
    private Label _summaryGenre;

    // ── Product Info (setup page inputs) ─────────────────────────────────────
    private TextField _productNameField;
    private DropdownField _productTypeDropdown;
    private VisualElement _platformToggleContainer;
    private readonly List<Toggle> _platformToggles = new List<Toggle>();
    private DropdownField _genreDropdown;

    // ── Setup page labels (conditional visibility) ────────────────────────────
    private Label _platformLabel;
    private Label _genreLabel;

    // ── Product Fit ───────────────────────────────────────────────────────────
    private Label _expectedInterestLabel;
    private Label _wastedInterestLabel;
    private Label _scopeLabel;

    // ── Radar Chart ───────────────────────────────────────────────────────────
    private UI.Elements.RadarChartElement _radarChart;

    // ── Estimates ─────────────────────────────────────────────────────────────
    private Label _etaLabel;
    private Label _estQualityLabel;
    private Label _upfrontCostLabel;

    // ── Pricing ───────────────────────────────────────────────────────────────
    private DropdownField _pricingModelDropdown;
    private FloatField _priceField;
    private Label _basePriceLabel;
    private Label _featurePriceLabel;
    private Label _marketPriceLabel;
    private Label _priceComparisonLabel;
    private Label _priceRatingLabel;
    private Label _priceWarningLabel;

    // ── Teams (max 6 pre-built rows) ─────────────────────────────────────────
    private const int MaxTeamRows = 6;
    private VisualElement[] _teamRows;
    private Label[] _teamRoleLabels;
    private DropdownField[] _teamDropdowns;
    private Label[] _teamSizeLabels;
    private Label[] _teamSkillLabels;
    private VisualElement[] _teamStatusIndicators;
    private string[] _teamCurrentStatusClass;

    // ── Dependencies (toggle-based) ────────────────────────────────────────────
    private VisualElement _depToggleContainer;
    private readonly List<Toggle> _depToggles = new List<Toggle>();
    private Label _totalRoyaltyLabel;
    private Label _qualityCeilingLabel;
    private Label _techPenaltyLabel;

    // ── Release Date ──────────────────────────────────────────────────────────
    private SliderInt _releaseDateSlider;
    private Label _releaseDateValueLabel;
    private Label _releaseDateHintLabel;

    // ── Validation (left panel bottom) ────────────────────────────────────────
    private const int MaxValidationMessages = 12;
    private VisualElement _validationContainer;
    private Label[] _validationMessages;
    private Button _startDevBtnBar;

    // ── Page navigation ───────────────────────────────────────────────────────
    private int _currentPage;
    private VisualElement[] _pages;
    private Button _prevPageBtn;
    private Button _nextPageBtn;
    private Label _pageIndicator;
    private const int PageSetup    = 0;
    private const int PageFeatures = 1;
    private const int PageTeams    = 2;
    private const int PageHardware = 3;
    private const int TotalPages   = 4;

    // ── Hardware Design (page 4) ──────────────────────────────────────────────
    private UI.Elements.RadarChartElement _hardwareRadarChart;
    private Button _hardwareBackBtn;
    private Button _hardwareConfirmBtn;
    private readonly List<Button> _processingTierButtons = new List<Button>();
    private readonly List<Button> _graphicsTierButtons   = new List<Button>();
    private readonly List<Button> _memoryTierButtons     = new List<Button>();
    private readonly List<Button> _storageTierButtons    = new List<Button>();
    private readonly List<Button> _formFactorButtons     = new List<Button>();
    private Label _hwManufactureCostLabel;
    private Label _hwDevCostLabel;
    private Label _hwFeatureAvailLabel;
    private Label _hwLockedFeaturesLabel;

    // Tier button group context — used by shared handler to know which group
    private const int HwGroupProcessing = 0;
    private const int HwGroupGraphics   = 1;
    private const int HwGroupMemory     = 2;
    private const int HwGroupStorage    = 3;
    private const int HwGroupFormFactor = 4;

    // ── Update type selector ─────────────────────────────────────────────────
    private VisualElement _updateTypeSelectorContainer;
    private Button _updateTypeBugFixBtn;
    private Button _updateTypeAddFeaturesBtn;
    private Button _updateTypeRemoveFeatureBtn;

    // ── Predecessor info (sequel/update) ─────────────────────────────────────
    private Label _predecessorInfoLabel;

    // ── Internal state ────────────────────────────────────────────────────────
    private string _lastTemplateId;
    private bool _modeApplied;
    private readonly List<string> _templateChoices = new List<string>();
    private readonly List<string> _genreChoices = new List<string>();
    private readonly List<string> _pricingModelChoices = new List<string> { "One-Time", "Subscription" };

    // Team dropdown choice lists — reused to avoid allocations in Bind
    private readonly List<string>[] _teamDropdownChoices = new List<string>[MaxTeamRows];

    public event Action OnCancelRequested;
    public event Action OnProductCreated;

    public CreateProductView(ICommandDispatcher dispatcher, ITooltipProvider tooltip,
        ProductTemplateDefinition[] definitions, MarketNicheData[] nicheData,
        ProductId? updateProductId = null, ProductId? sequelOfId = null)
    {
        _dispatcher = dispatcher;
        _tooltip = tooltip;
        _definitions = definitions;
        _nicheData = nicheData;
        _updateProductId = updateProductId;
        _sequelOfId = sequelOfId;

        for (int i = 0; i < MaxTeamRows; i++)
            _teamDropdownChoices[i] = new List<string>();

        _teamCurrentStatusClass = new string[MaxTeamRows];
    }

    public void Initialize(VisualElement root)
    {
        _root = root;
        _root.AddToClassList("create-product-root");

        // ── Header ────────────────────────────────────────────────────────────
        var header = new VisualElement();
        header.AddToClassList("create-product-header");

        _cancelBtn = new Button { text = "\u2190 Cancel" };
        _cancelBtn.AddToClassList("btn-ghost");
        _cancelBtn.clicked += OnCancelClicked;
        header.Add(_cancelBtn);

        _headerTitleLabel = new Label("Create Product");
        _headerTitleLabel.AddToClassList("create-product-title");
        header.Add(_headerTitleLabel);

        _startDevBtn = new Button { text = "Start Development" };
        _startDevBtn.AddToClassList("btn-primary");
        _startDevBtn.clicked += OnStartDevClicked;
        header.Add(_startDevBtn);

        _root.Add(header);

        // ── Body ──────────────────────────────────────────────────────────────
        var body = new VisualElement();
        body.AddToClassList("create-product-body");

        // Left panel — info only
        var leftPanel = new ScrollView();
        leftPanel.AddToClassList("create-product-left");

        BuildProductSummarySection(leftPanel.contentContainer);
        BuildProductFitSection(leftPanel.contentContainer);
        BuildEstimatesSection(leftPanel.contentContainer);
        BuildDependencyMetricsSection(leftPanel.contentContainer);
        BuildPricingSection(leftPanel.contentContainer);
        BuildReleaseDateSection(leftPanel.contentContainer);
        BuildValidationSection(leftPanel.contentContainer);

        body.Add(leftPanel);

        // Right panel — paginated wizard
        _rightPanel = new VisualElement();
        _rightPanel.AddToClassList("create-product-right");
        _rightPanel.style.flexDirection = FlexDirection.Column;

        _pages = new VisualElement[TotalPages];
        for (int i = 0; i < TotalPages; i++)
        {
            var page = new VisualElement();
            page.style.flexGrow = 1;
            page.style.display = DisplayStyle.None;
            _pages[i] = page;
            _rightPanel.Add(page);
        }

        BuildSetupPage(_pages[PageSetup]);
        BuildFeaturesPage(_pages[PageFeatures]);
        BuildTeamsPage(_pages[PageTeams]);
        BuildHardwarePage(_pages[PageHardware]);

        body.Add(_rightPanel);
        _root.Add(body);

        // ── Footer navigation ─────────────────────────────────────────────────
        BuildPageNavFooter(_root);

        // Show first page
        ShowPage(PageSetup);
    }

    // ── Section builders ──────────────────────────────────────────────────────

    private void BuildProductSummarySection(VisualElement parent)
    {
        var section = new VisualElement();
        section.AddToClassList("create-product-section");

        var title = new Label("PRODUCT SUMMARY");
        title.AddToClassList("create-product-section-title");
        section.Add(title);

        _predecessorInfoLabel = new Label();
        _predecessorInfoLabel.AddToClassList("predecessor-info");
        _predecessorInfoLabel.style.display = DisplayStyle.None;
        section.Add(_predecessorInfoLabel);

        _summaryName     = AddMetricRow(section, "Name");
        _summaryType     = AddMetricRow(section, "Type");
        _summaryPlatform = AddMetricRow(section, "Platform");
        _summaryReach    = AddMetricRow(section, "Reach");
        _summaryGenre    = AddMetricRow(section, "Genre");

        parent.Add(section);
    }

    private void BuildProductFitSection(VisualElement parent)
    {
        var section = new VisualElement();
        section.AddToClassList("create-product-section");

        var title = new Label("PRODUCT FIT");
        title.AddToClassList("create-product-section-title");
        section.Add(title);

        _expectedInterestLabel = AddMetricRow(section, "Expected Interest");
        _wastedInterestLabel = AddMetricRow(section, "Wasted Effort");
        _scopeLabel = AddMetricRow(section, "Scope");

        parent.Add(section);
    }

    private void BuildRadarChartSection(VisualElement parent)
    {
        var section = new VisualElement();
        section.AddToClassList("create-product-section");

        var title = new Label("FEATURE ALIGNMENT");
        title.AddToClassList("create-product-section-title");
        section.Add(title);

        var chartContainer = new VisualElement();
        chartContainer.AddToClassList("radar-chart-container");

        _radarChart = new UI.Elements.RadarChartElement();
        _radarChart.ChartRadius = 90f;
        _radarChart.style.width = 260f;
        _radarChart.style.height = 260f;
        chartContainer.Add(_radarChart);

        section.Add(chartContainer);
        parent.Add(section);
    }

    private void BuildEstimatesSection(VisualElement parent)
    {
        var section = new VisualElement();
        section.AddToClassList("create-product-section");

        var title = new Label("ESTIMATES");
        title.AddToClassList("create-product-section-title");
        section.Add(title);

        _etaLabel = AddMetricRow(section, "Dev Time");
        _estQualityLabel = AddMetricRow(section, "Est. Quality");
        _upfrontCostLabel = AddMetricRow(section, "Upfront Cost");

        parent.Add(section);
    }

    private void BuildDependencyMetricsSection(VisualElement parent) {
        var section = new VisualElement();
        section.AddToClassList("create-product-section");

        var title = new Label("TOOL COSTS");
        title.AddToClassList("create-product-section-title");
        section.Add(title);

        _totalRoyaltyLabel = new Label();
        _totalRoyaltyLabel.AddToClassList("metric-secondary");
        section.Add(_totalRoyaltyLabel);

        _qualityCeilingLabel = new Label();
        _qualityCeilingLabel.AddToClassList("metric-secondary");
        section.Add(_qualityCeilingLabel);

        _techPenaltyLabel = new Label();
        _techPenaltyLabel.AddToClassList("text-warning");
        _techPenaltyLabel.style.display = DisplayStyle.None;
        section.Add(_techPenaltyLabel);

        parent.Add(section);
    }

    private void BuildValidationSection(VisualElement parent)
    {
        var section = new VisualElement();
        section.AddToClassList("create-product-section");
        section.style.marginTop = 8;

        _validationMessages = new Label[MaxValidationMessages];
        for (int i = 0; i < MaxValidationMessages; i++)
        {
            var lbl = new Label();
            lbl.style.display = DisplayStyle.None;
            _validationMessages[i] = lbl;
            section.Add(lbl);
        }

        parent.Add(section);
    }

    private void BuildSetupPage(VisualElement parent)
    {
        var section = new VisualElement();
        section.AddToClassList("create-product-section");

        var title = new Label("SETUP");
        title.AddToClassList("create-product-section-title");
        section.Add(title);

        var nameLbl = new Label("Product Name");
        nameLbl.AddToClassList("metric-secondary");
        section.Add(nameLbl);

        _productNameField = new TextField();
        _productNameField.style.marginBottom = 8;
        _productNameField.RegisterCallback<ChangeEvent<string>>(OnProductNameChanged);
        section.Add(_productNameField);

        var typeLbl = new Label("Product Type");
        typeLbl.AddToClassList("metric-secondary");
        section.Add(typeLbl);

        _productTypeDropdown = new DropdownField();
        _productTypeDropdown.choices = _templateChoices;
        _productTypeDropdown.style.marginBottom = 8;
        _productTypeDropdown.RegisterCallback<ChangeEvent<string>>(OnProductTypeChanged);
        section.Add(_productTypeDropdown);

        parent.Add(section);

        // ── Dependencies (tools) ──────────────────────────────────────────────
        var depSection = new VisualElement();
        depSection.AddToClassList("create-product-section");

        var depTitle = new Label("DEPENDENCIES");
        depTitle.AddToClassList("create-product-section-title");
        depSection.Add(depTitle);

        _depToggleContainer = new VisualElement();
        _depToggleContainer.AddToClassList("dep-toggle-container");
        depSection.Add(_depToggleContainer);

        parent.Add(depSection);

        // ── Platform / Genre ──────────────────────────────────────────────────
        var platformSection = new VisualElement();
        platformSection.AddToClassList("create-product-section");

        _platformLabel = new Label("Target Platform(s)");
        _platformLabel.AddToClassList("metric-secondary");
        platformSection.Add(_platformLabel);

        _platformToggleContainer = new VisualElement();
        _platformToggleContainer.AddToClassList("platform-toggle-container");
        _platformToggleContainer.style.marginBottom = 8;
        platformSection.Add(_platformToggleContainer);

        _genreLabel = new Label("Genre / Niche");
        _genreLabel.AddToClassList("metric-secondary");
        platformSection.Add(_genreLabel);

        _genreDropdown = new DropdownField();
        _genreDropdown.choices = _genreChoices;
        _genreDropdown.style.marginBottom = 4;
        _genreDropdown.RegisterCallback<ChangeEvent<string>>(OnGenreChanged);
        platformSection.Add(_genreDropdown);

        parent.Add(platformSection);

        // ── Team Assignments ──────────────────────────────────────────────────
        var teamSection = new VisualElement();
        teamSection.AddToClassList("create-product-section");

        var teamTitle = new Label("TEAM ASSIGNMENTS");
        teamTitle.AddToClassList("create-product-section-title");
        teamSection.Add(teamTitle);

        _teamRows = new VisualElement[MaxTeamRows];
        _teamRoleLabels = new Label[MaxTeamRows];
        _teamDropdowns = new DropdownField[MaxTeamRows];
        _teamSizeLabels = new Label[MaxTeamRows];
        _teamSkillLabels = new Label[MaxTeamRows];
        _teamStatusIndicators = new VisualElement[MaxTeamRows];

        for (int i = 0; i < MaxTeamRows; i++)
        {
            int capturedIndex = i;

            var row = new VisualElement();
            row.AddToClassList("team-row");
            row.style.display = DisplayStyle.None;

            var statusBar = new VisualElement();
            statusBar.style.width = 3f;
            statusBar.style.height = Length.Percent(100f);
            statusBar.style.marginRight = 6f;
            _teamStatusIndicators[i] = statusBar;
            row.Add(statusBar);

            var roleLabel = new Label();
            roleLabel.AddToClassList("team-role-label");
            _teamRoleLabels[i] = roleLabel;
            row.Add(roleLabel);

            var dropdown = new DropdownField();
            dropdown.choices = _teamDropdownChoices[i];
            dropdown.style.flexGrow = 1;
            dropdown.style.marginRight = 4;
            dropdown.userData = capturedIndex;
            dropdown.RegisterCallback<ChangeEvent<string>>(OnTeamDropdownChanged);
            _teamDropdowns[i] = dropdown;
            row.Add(dropdown);

            var sizeLabel = new Label();
            sizeLabel.AddToClassList("team-size-label");
            _teamSizeLabels[i] = sizeLabel;
            row.Add(sizeLabel);

            var skillLabel = new Label();
            skillLabel.AddToClassList("team-skill-label");
            _teamSkillLabels[i] = skillLabel;
            row.Add(skillLabel);

            _teamRows[i] = row;
            teamSection.Add(row);
        }

        parent.Add(teamSection);
    }

    private void BuildFeaturesPage(VisualElement parent)
    {
        BuildUpdateTypeSelectorInPage(parent);

        var featBody = new VisualElement();
        featBody.AddToClassList("hw-overlay-body");

        var featLeft = new VisualElement();
        featLeft.style.flexGrow = 1;
        featLeft.style.flexShrink = 1;
        featLeft.style.overflow = Overflow.Hidden;
        BuildRightPanel(featLeft);

        featBody.Add(featLeft);

        var featRight = new VisualElement();
        featRight.AddToClassList("hw-overlay-right");
        BuildRadarChartSection(featRight);

        featBody.Add(featRight);
        parent.Add(featBody);
    }

    private void BuildTeamsPage(VisualElement parent)
    {
        // Teams are now built on the setup page — this slot is kept to preserve page index stability.
    }

    private void BuildPricingSection(VisualElement parent)
    {
        var section = new VisualElement();
        section.AddToClassList("create-product-section");

        var title = new Label("PRICING");
        title.AddToClassList("create-product-section-title");
        section.Add(title);

        _basePriceLabel = AddMetricRow(section, "Base Price");
        _featurePriceLabel = AddMetricRow(section, "Feature Value");
        _marketPriceLabel = AddMetricRow(section, "Market Price");

        var modelLbl = new Label("Pricing Model");
        modelLbl.AddToClassList("metric-secondary");
        modelLbl.style.marginTop = 8;
        section.Add(modelLbl);

        _pricingModelDropdown = new DropdownField();
        _pricingModelDropdown.choices = _pricingModelChoices;
        _pricingModelDropdown.value = "One-Time";
        _pricingModelDropdown.style.marginBottom = 8;
        _pricingModelDropdown.RegisterCallback<ChangeEvent<string>>(OnPricingModelChanged);
        section.Add(_pricingModelDropdown);

        var priceLbl = new Label("Your Price ($)");
        priceLbl.AddToClassList("metric-secondary");
        section.Add(priceLbl);

        _priceField = new FloatField();
        _priceField.style.marginBottom = 4;
        _priceField.RegisterCallback<ChangeEvent<float>>(OnPriceChanged);
        section.Add(_priceField);

        _priceComparisonLabel = new Label();
        _priceComparisonLabel.AddToClassList("metric-secondary");
        _priceComparisonLabel.style.marginBottom = 2;
        _priceComparisonLabel.style.display = DisplayStyle.None;
        section.Add(_priceComparisonLabel);

        _priceRatingLabel = new Label();
        _priceRatingLabel.style.marginBottom = 2;
        section.Add(_priceRatingLabel);

        _priceWarningLabel = new Label();
        _priceWarningLabel.AddToClassList("text-warning");
        _priceWarningLabel.style.display = DisplayStyle.None;
        section.Add(_priceWarningLabel);

        parent.Add(section);
    }

    private void BuildReleaseDateSection(VisualElement parent)
    {
        var section = new VisualElement();
        section.AddToClassList("create-product-section");

        var title = new Label("TARGET RELEASE DATE");
        title.AddToClassList("create-product-section-title");
        section.Add(title);

        _releaseDateValueLabel = new Label("Not set");
        _releaseDateValueLabel.AddToClassList("metric-primary");
        _releaseDateValueLabel.AddToClassList("text-accent");
        _releaseDateValueLabel.style.marginBottom = 4;
        section.Add(_releaseDateValueLabel);

        _releaseDateSlider = new SliderInt(30, 730);
        _releaseDateSlider.SetValueWithoutNotify(180);
        _releaseDateSlider.style.marginBottom = 4;
        _releaseDateSlider.RegisterValueChangedCallback(OnReleaseDateSliderChanged);
        section.Add(_releaseDateSlider);

        _releaseDateHintLabel = new Label("Days from today until release");
        _releaseDateHintLabel.AddToClassList("text-muted");
        _releaseDateHintLabel.style.fontSize = 11;
        section.Add(_releaseDateHintLabel);

        parent.Add(section);
    }

    private void BuildHardwarePage(VisualElement parent)
    {
        var hwBody = new VisualElement();
        hwBody.AddToClassList("hw-overlay-body");

        var hwLeft = new VisualElement();
        hwLeft.AddToClassList("hw-overlay-left");

        BuildHardwareTierSelectors(hwLeft);
        BuildHardwarePreviewSection(hwLeft);

        hwBody.Add(hwLeft);

        var hwRight = new VisualElement();
        hwRight.AddToClassList("hw-overlay-right");

        var chartTitle = new Label("SPEC vs MARKET DEMAND");
        chartTitle.AddToClassList("create-product-section-title");
        hwRight.Add(chartTitle);

        var chartContainer = new VisualElement();
        chartContainer.AddToClassList("hw-radar-container");

        _hardwareRadarChart = new UI.Elements.RadarChartElement();
        _hardwareRadarChart.ChartRadius = 90f;
        _hardwareRadarChart.style.width = 260f;
        _hardwareRadarChart.style.height = 260f;
        _hardwareRadarChart.SetAxisCount(5);
        _hardwareRadarChart.SetAxisLabels(new[] { "CPU", "GPU", "RAM", "Storage", "Form" });
        chartContainer.Add(_hardwareRadarChart);
        hwRight.Add(chartContainer);

        _hwLockedFeaturesLabel = new Label();
        _hwLockedFeaturesLabel.AddToClassList("hw-locked-features");
        _hwLockedFeaturesLabel.style.display = DisplayStyle.None;
        hwRight.Add(_hwLockedFeaturesLabel);

        hwBody.Add(hwRight);
        parent.Add(hwBody);
    }

    private void BuildUpdateTypeSelectorInPage(VisualElement parent)
    {
        _updateTypeSelectorContainer = new VisualElement();
        _updateTypeSelectorContainer.AddToClassList("update-type-selector");
        _updateTypeSelectorContainer.style.display = DisplayStyle.None;

        var sectionTitle = new Label("UPDATE TYPE");
        sectionTitle.AddToClassList("create-product-section-title");
        _updateTypeSelectorContainer.Add(sectionTitle);

        var btnRow = new VisualElement();
        btnRow.AddToClassList("update-type-btn-row");

        _updateTypeBugFixBtn = new Button { text = "Bug Fix" };
        _updateTypeBugFixBtn.AddToClassList("btn-secondary");
        _updateTypeBugFixBtn.AddToClassList("update-type-btn");
        _updateTypeBugFixBtn.userData = ProductUpdateType.BugFix;
        _updateTypeBugFixBtn.RegisterCallback<ClickEvent>(OnUpdateTypeClicked);
        btnRow.Add(_updateTypeBugFixBtn);

        _updateTypeAddFeaturesBtn = new Button { text = "Add Features" };
        _updateTypeAddFeaturesBtn.AddToClassList("btn-secondary");
        _updateTypeAddFeaturesBtn.AddToClassList("update-type-btn");
        _updateTypeAddFeaturesBtn.userData = ProductUpdateType.AddFeatures;
        _updateTypeAddFeaturesBtn.RegisterCallback<ClickEvent>(OnUpdateTypeClicked);
        btnRow.Add(_updateTypeAddFeaturesBtn);

        _updateTypeRemoveFeatureBtn = new Button { text = "Remove Feature" };
        _updateTypeRemoveFeatureBtn.AddToClassList("btn-secondary");
        _updateTypeRemoveFeatureBtn.AddToClassList("update-type-btn");
        _updateTypeRemoveFeatureBtn.userData = ProductUpdateType.RemoveFeature;
        _updateTypeRemoveFeatureBtn.RegisterCallback<ClickEvent>(OnUpdateTypeClicked);
        btnRow.Add(_updateTypeRemoveFeatureBtn);

        _updateTypeSelectorContainer.Add(btnRow);
        parent.Add(_updateTypeSelectorContainer);
    }

    private void BuildPageNavFooter(VisualElement parent)
    {
        var footer = new VisualElement();
        footer.AddToClassList("create-product-nav-footer");

        _prevPageBtn = new Button { text = "\u2190 Previous" };
        _prevPageBtn.AddToClassList("btn-secondary");
        _prevPageBtn.clicked += OnPrevPageClicked;
        footer.Add(_prevPageBtn);

        _pageIndicator = new Label();
        _pageIndicator.AddToClassList("page-indicator");
        footer.Add(_pageIndicator);

        _nextPageBtn = new Button { text = "Next \u2192" };
        _nextPageBtn.AddToClassList("btn-secondary");
        _nextPageBtn.clicked += OnNextPageClicked;
        footer.Add(_nextPageBtn);

        _startDevBtnBar = new Button { text = "Start Development" };
        _startDevBtnBar.AddToClassList("btn-primary");
        _startDevBtnBar.clicked += OnStartDevClicked;
        footer.Add(_startDevBtnBar);

        parent.Add(footer);
    }

    private static void LockDropdown(DropdownField dropdown, string lockedValue)
    {
        if (dropdown == null) return;
        dropdown.SetValueWithoutNotify(lockedValue);
        dropdown.SetEnabled(false);
        dropdown.AddToClassList("field-locked");
    }

    private void BuildHardwareTierSelectors(VisualElement parent)
    {
        var selectorTitle = new Label("COMPONENT TIERS");
        selectorTitle.AddToClassList("create-product-section-title");
        parent.Add(selectorTitle);

        parent.Add(BuildHwTierRow("Processing (CPU)", new[] { "Low", "Mid", "High", "Max" }, _processingTierButtons, HwGroupProcessing));
        parent.Add(BuildHwTierRow("Graphics (GPU)", new[] { "Low", "Mid", "High", "Max" }, _graphicsTierButtons, HwGroupGraphics));
        parent.Add(BuildHwTierRow("Memory (RAM)", new[] { "Low", "Mid", "High" }, _memoryTierButtons, HwGroupMemory));
        parent.Add(BuildHwTierRow("Storage", new[] { "Low", "Mid", "High" }, _storageTierButtons, HwGroupStorage));
        parent.Add(BuildHwTierRow("Form Factor", new[] { "Standard", "Portable", "Hybrid" }, _formFactorButtons, HwGroupFormFactor));
    }

    private VisualElement BuildHwTierRow(string label, string[] tierLabels, List<Button> buttonList, int group)
    {
        var container = new VisualElement();
        container.AddToClassList("hw-tier-row");

        var lbl = new Label(label);
        lbl.AddToClassList("metric-secondary");
        lbl.AddToClassList("hw-tier-label");
        container.Add(lbl);

        var btnRow = new VisualElement();
        btnRow.AddToClassList("hw-tier-btn-row");
        buttonList.Clear();

        for (int i = 0; i < tierLabels.Length; i++)
        {
            var btn = new Button { text = tierLabels[i] };
            bool isFirst = i == 0;
            if (isFirst) { btn.AddToClassList("btn-primary"); btn.AddToClassList("hw-tier-btn--active"); }
            else btn.AddToClassList("btn-secondary");
            btn.AddToClassList("hw-tier-btn");
            btn.userData = new HwTierButtonData { Group = group, Index = i };
            btn.RegisterCallback<ClickEvent>(OnHwTierButtonClicked);
            btnRow.Add(btn);
            buttonList.Add(btn);
        }

        container.Add(btnRow);
        return container;
    }

    private void BuildHardwarePreviewSection(VisualElement parent)
    {
        var previewTitle = new Label("HARDWARE PREVIEW");
        previewTitle.AddToClassList("create-product-section-title");
        previewTitle.style.marginTop = 16;
        parent.Add(previewTitle);

        _hwManufactureCostLabel = new Label("Mfg Cost: —");
        _hwManufactureCostLabel.AddToClassList("metric-secondary");
        parent.Add(_hwManufactureCostLabel);

        _hwDevCostLabel = new Label("R&D Add: —");
        _hwDevCostLabel.AddToClassList("metric-secondary");
        parent.Add(_hwDevCostLabel);

        _hwFeatureAvailLabel = new Label("Features: —");
        _hwFeatureAvailLabel.AddToClassList("metric-secondary");
        parent.Add(_hwFeatureAvailLabel);
    }

    private void BuildRightPanel(VisualElement parent)    {
        // Category tab bar
        _featureCategoryTabBar = new VisualElement();
        _featureCategoryTabBar.AddToClassList("feature-category-tab-bar");
        parent.Add(_featureCategoryTabBar);

        // Filter bar
        _filterBar = BuildFeatureFilterBar();
        parent.Add(_filterBar);

        // Demand legend bar
        var legendRow = new VisualElement();
        legendRow.AddToClassList("feature-legend-bar");
        string[] stageNames = { "Cutting Edge", "Trending", "Expected", "Fading", "Outdated" };
        string[] stageClasses = { "demand-cutting-edge", "demand-trending", "demand-expected", "demand-fading", "demand-outdated" };
        string[] stageTips = { "New innovation, high bonus", "Rising demand, good bonus", "Consumers expect this", "Losing relevance", "Negative impact" };
        for (int i = 0; i < 5; i++) {
            var item = new VisualElement();
            item.style.flexDirection = FlexDirection.Row;
            item.style.alignItems = Align.Center;
            item.style.marginRight = 12;
            var badge = new Label(stageNames[i]);
            badge.AddToClassList("feature-demand-badge");
            badge.AddToClassList(stageClasses[i]);
            item.Add(badge);
            var tip = new Label(stageTips[i]);
            tip.AddToClassList("feature-legend-tip");
            item.Add(tip);
            legendRow.Add(item);
        }
        parent.Add(legendRow);

        // Scroll + container
        _featureScroll = new ScrollView();
        _featureScroll.style.flexGrow = 1;
        _featureContainer = new VisualElement();
        _featureContainer.AddToClassList("feature-container");
        _featureScroll.Add(_featureContainer);
        parent.Add(_featureScroll);

        // Feature pool — factory wires toggle handler once
        _featurePool = new ElementPool(CreateFeatureRow, _featureContainer);

        // Summary label at bottom
        _featureSummaryLabel = new Label();
        _featureSummaryLabel.AddToClassList("feature-summary-label");
        parent.Add(_featureSummaryLabel);
    }

    private VisualElement BuildFeatureFilterBar()
    {
        var bar = new VisualElement();
        bar.AddToClassList("feature-filter-bar");

        var showLbl = new Label("Show");
        showLbl.AddToClassList("filter-group__label");
        bar.Add(showLbl);

        string[] showOptions = { "All", "Selected", "Unselected" };
        for (int i = 0; i < showOptions.Length; i++)
        {
            var btn = new Button { text = showOptions[i] };
            btn.AddToClassList("filter-btn");
            btn.userData = i;
            if (i == 0) btn.AddToClassList("filter-btn--active");
            btn.RegisterCallback<ClickEvent>(OnSelectedFilterClicked);
            bar.Add(btn);
            _selectedFilterBtns.Add(btn);
        }

        var sortLbl = new Label("Sort");
        sortLbl.AddToClassList("filter-group__label");
        sortLbl.style.marginLeft = 12;
        bar.Add(sortLbl);

        string[] sortOptions = { "Name", "Demand" };
        for (int i = 0; i < sortOptions.Length; i++)
        {
            var btn = new Button { text = sortOptions[i] };
            btn.AddToClassList("filter-btn");
            btn.userData = i;
            if (i == 0) btn.AddToClassList("filter-btn--active");
            btn.RegisterCallback<ClickEvent>(OnSortModeClicked);
            bar.Add(btn);
            _sortModeBtns.Add(btn);
        }

        return bar;
    }

    private VisualElement CreateFeatureRow()
    {
        var row = new VisualElement();
        row.AddToClassList("feature-row");

        // Left group: toggle + name
        var leftGroup = new VisualElement();
        leftGroup.AddToClassList("feature-row__left");

        var toggle = new Toggle();
        toggle.AddToClassList("feature-toggle");
        toggle.RegisterCallback<ChangeEvent<bool>>(OnFeatureToggled);
        leftGroup.Add(toggle);

        var nameLabel = new Label();
        nameLabel.AddToClassList("feature-name");
        leftGroup.Add(nameLabel);

        row.Add(leftGroup);

        // Right group: pips + demand badge + cost
        var rightGroup = new VisualElement();
        rightGroup.AddToClassList("feature-row__right");

        var pips = new VisualElement();
        pips.AddToClassList("feature-pips");
        for (int p = 0; p < 3; p++)
        {
            var pip = new VisualElement();
            pip.AddToClassList("feature-pip");
            pip.style.display = DisplayStyle.None;
            pips.Add(pip);
        }
        rightGroup.Add(pips);

        var demandLabel = new Label();
        demandLabel.AddToClassList("feature-demand-badge");
        rightGroup.Add(demandLabel);

        var costLabel = new Label();
        costLabel.AddToClassList("feature-cost-badge");
        rightGroup.Add(costLabel);

        row.Add(rightGroup);

        // Below-row elements (synergy/lock)
        var synergyLabel = new Label();
        synergyLabel.AddToClassList("feature-synergy-indicator");
        synergyLabel.style.display = DisplayStyle.None;
        row.Add(synergyLabel);

        var lockLabel = new Label();
        lockLabel.AddToClassList("feature-lock-reason");
        lockLabel.style.display = DisplayStyle.None;
        row.Add(lockLabel);

        return row;
    }

    private void RebuildCategoryTabBar()
    {
        _featureCategoryTabBar.Clear();
        _categoryTabButtons.Clear();
        if (_viewModel == null) return;

        var categories = _viewModel.GetDistinctFeatureCategories();
        int count = categories.Count;
        for (int i = 0; i < count; i++)
        {
            var cat = categories[i];
            var btn = new Button();
            btn.AddToClassList("tab-btn");
            btn.userData = cat;
            btn.RegisterCallback<ClickEvent>(OnCategoryTabClicked);
            _featureCategoryTabBar.Add(btn);
            _categoryTabButtons.Add(btn);
        }
    }

    private void UpdateCategoryTabLabels()
    {
        if (_viewModel == null) return;
        var features = _viewModel.Features;
        int tabCount = _categoryTabButtons.Count;

        for (int t = 0; t < tabCount; t++)
        {
            var btn = _categoryTabButtons[t];
            if (!(btn.userData is FeatureCategory cat)) continue;

            int total = 0;
            int selected = 0;
            int fc = features.Count;
            for (int i = 0; i < fc; i++)
            {
                if (features[i].FeatureCategory != cat) continue;
                total++;
                if (features[i].IsSelected) selected++;
            }

            btn.text = cat.ToString() + " (" + selected + "/" + total + ")";

            if (cat == _viewModel.SelectedCategory)
                btn.AddToClassList("tab-btn--active");
            else
                btn.RemoveFromClassList("tab-btn--active");
        }
    }

    private void BindFeaturePool()
    {
        if (_viewModel == null) return;

        _filteredIndicesBind.Clear();
        var srcIndices = _viewModel.FilteredIndices;
        int idxCount = srcIndices.Count;
        for (int i = 0; i < idxCount; i++)
            _filteredIndicesBind.Add(srcIndices[i]);

        _featurePool.UpdateList(_filteredIndicesBind, BindFeatureRow);
        UpdateFeatureSummaryLabel();
    }

    private void BindFeatureRow(VisualElement row, int featureIndex)
    {
        if (_viewModel == null) return;
        var features = _viewModel.Features;
        if (featureIndex < 0 || featureIndex >= features.Count) return;

        var feat = features[featureIndex];
        row.userData = featureIndex;

        // Toggle
        var leftGroup = row.ElementAt(0);
        var toggle = leftGroup?.ElementAt(0) as Toggle;
        if (toggle != null)
        {
            toggle.userData = featureIndex;
            toggle.SetValueWithoutNotify(feat.IsSelected);
            toggle.SetEnabled(!feat.IsLocked && !feat.IsPreSelected);
        }

        // Name
        var nameLabel = leftGroup?.ElementAt(1) as Label;
        if (nameLabel != null)
            nameLabel.text = feat.DisplayName;

        // Pips
        var rightGroup = row.ElementAt(1);
        var pips = rightGroup?.ElementAt(0);
        if (pips != null)
        {
            int[] axes = _viewModel.GetFeatureAffinityAxes(feat.FeatureId);
            var axisColors = _viewModel.RadarAxisColors;
            int pipCount = pips.childCount;
            for (int p = 0; p < pipCount; p++)
            {
                var pip = pips.ElementAt(p);
                if (p < axes.Length)
                {
                    pip.style.display = DisplayStyle.Flex;
                    int axisIdx = axes[p];
                    Color c = (axisColors != null && axisIdx < axisColors.Length)
                        ? axisColors[axisIdx]
                        : new Color(0.5f, 0.5f, 0.5f, 1f);
                    pip.style.backgroundColor = c;
                }
                else
                {
                    pip.style.display = DisplayStyle.None;
                }
            }
        }

        // Demand badge
        var demandLabel = rightGroup?.ElementAt(1) as Label;
        if (demandLabel != null)
        {
            demandLabel.text = feat.DemandStageLabel ?? "";
            demandLabel.RemoveFromClassList("demand-cutting-edge");
            demandLabel.RemoveFromClassList("demand-trending");
            demandLabel.RemoveFromClassList("demand-expected");
            demandLabel.RemoveFromClassList("demand-fading");
            demandLabel.RemoveFromClassList("demand-outdated");
            switch (feat.DemandStage)
            {
                case FeatureDemandStage.Emerging:  demandLabel.AddToClassList("demand-cutting-edge"); break;
                case FeatureDemandStage.Growing:   demandLabel.AddToClassList("demand-trending");     break;
                case FeatureDemandStage.Standard:  demandLabel.AddToClassList("demand-expected");     break;
                case FeatureDemandStage.Declining: demandLabel.AddToClassList("demand-fading");       break;
                case FeatureDemandStage.Legacy:    demandLabel.AddToClassList("demand-outdated");     break;
            }
            demandLabel.style.display = string.IsNullOrEmpty(feat.DemandStageLabel)
                ? DisplayStyle.None : DisplayStyle.Flex;
        }

        // Cost badge
        var costLabel = rightGroup?.ElementAt(2) as Label;
        if (costLabel != null)
        {
            string devCostText = _viewModel.GetFeatureCostLabel(feat.FeatureId);
            string costText = !string.IsNullOrEmpty(devCostText)
                ? devCostText + " dev | +" + UIFormatting.FormatMoney(feat.PriceContribution) + " price"
                : "";
            costLabel.text = costText;
            costLabel.style.display = string.IsNullOrEmpty(costText) ? DisplayStyle.None : DisplayStyle.Flex;
        }

        // Synergy / conflict indicator
        var synergyLabel = row.ElementAt(2) as Label;
        if (synergyLabel != null)
        {
            if (feat.HasSynergyWithSelected && !string.IsNullOrEmpty(feat.SynergyLabel))
            {
                synergyLabel.text = "\u25b2";
                synergyLabel.RemoveFromClassList("feature-conflict-indicator");
                synergyLabel.AddToClassList("feature-synergy-indicator");
                synergyLabel.style.display = DisplayStyle.Flex;
            }
            else if (feat.HasConflictWithSelected && !string.IsNullOrEmpty(feat.ConflictLabel))
            {
                synergyLabel.text = "\u25bc";
                synergyLabel.RemoveFromClassList("feature-synergy-indicator");
                synergyLabel.AddToClassList("feature-conflict-indicator");
                synergyLabel.style.display = DisplayStyle.Flex;
            }
            else
            {
                synergyLabel.style.display = DisplayStyle.None;
            }
        }

        // Lock reason
        var lockLabel = row.ElementAt(3) as Label;
        if (lockLabel != null)
        {
            lockLabel.text = feat.LockReason ?? "";
            lockLabel.style.display = (feat.IsLocked && !string.IsNullOrEmpty(feat.LockReason))
                ? DisplayStyle.Flex : DisplayStyle.None;
        }

        // Row-level USS classes
        SetClass(row, "feature-row--selected", feat.IsSelected);
        SetClass(row, "feature-row--locked", feat.IsLocked);
        SetClass(row, "feature-row--synergy", feat.HasSynergyWithSelected);
        SetClass(row, "feature-row--conflict", feat.HasConflictWithSelected);

        // Tooltip
        if (_tooltip != null)
        {
            var tooltipData = _viewModel.BuildFeatureTooltip(feat.FeatureId);
            row.SetRichTooltip(tooltipData, _tooltip.TooltipService);
        }
    }

    private void UpdateFeatureSummaryLabel()
    {
        if (_featureSummaryLabel == null || _viewModel == null) return;
        int selected = _viewModel.SelectedFeatureCount;
        int upfront = _viewModel.TotalSelectedUpfrontCost;
        string estTime = _viewModel.EstimatedCompletionLabel;

        string text = selected + " selected";
        if (upfront > 0) text += "  |  +" + UIFormatting.FormatMoney(upfront);
        if (!string.IsNullOrEmpty(estTime) && estTime != "Unknown") text += "  |  ~" + estTime;
        string scopeLabel = _viewModel.ScopeEfficiencyLabel;
        if (!string.IsNullOrEmpty(scopeLabel)) text += "  |  " + scopeLabel;

        _featureSummaryLabel.text = text;

        string scopeClass = _viewModel.ScopeEfficiencyClass;
        _featureSummaryLabel.RemoveFromClassList("scope-good");
        _featureSummaryLabel.RemoveFromClassList("scope-warning");
        _featureSummaryLabel.RemoveFromClassList("scope-critical");
        if (!string.IsNullOrEmpty(scopeClass))
            _featureSummaryLabel.AddToClassList(scopeClass);
    }

    private void BuildValidationBar(VisualElement parent)
    {
        // Validation is now in the left panel (BuildValidationSection) — kept as no-op for compatibility
    }

    // ── IGameView ─────────────────────────────────────────────────────────────

    public void Bind(IViewModel viewModel)
    {
        _viewModel = viewModel as CreateProductViewModel;
        if (_viewModel == null) return;

        RefreshDropdownsIfTemplateChanged();
        ApplyModeOnce();
        BindProductSummary();
        BindProductInfo();
        BindConditionalSections();
        BindProductFit();
        BindRadarChart();
        BindEstimates();
        BindPricingSummary();
        BindTeamRows();
        BindDependencyToggles();
        BindValidationSection();
        BindReleaseDate();
        BindUpdateTypeSelector();
        BindFeatures();
        BindHardwarePageVisibility();
        BindPlatformToggles();
    }

    private void RefreshAfterChange() {
        if (_viewModel == null) return;
        RefreshDropdownsIfTemplateChanged();
        BindProductSummary();
        BindProductInfo();
        BindConditionalSections();
        BindProductFit();
        BindRadarChart();
        BindEstimates();
        BindPricingSummary();
        BindTeamRows();
        BindDependencyToggles();
        BindValidationSection();
        BindReleaseDate();
        BindFeatures();
        BindPlatformToggles();
        BindHardwarePageVisibility();
    }

    private void ShowPage(int page)
    {
        int effectivePageCount = GetEffectivePageCount();
        page = Math.Max(0, Math.Min(page, effectivePageCount - 1));
        _currentPage = page;

        int[] pageMap = BuildPageMap();

        for (int i = 0; i < TotalPages; i++)
            if (_pages[i] != null) _pages[i].style.display = DisplayStyle.None;

        int mappedPage = pageMap[_currentPage];
        if (mappedPage >= 0 && mappedPage < TotalPages && _pages[mappedPage] != null)
            _pages[mappedPage].style.display = DisplayStyle.Flex;

        if (_pageIndicator != null)
            _pageIndicator.text = (_currentPage + 1) + " / " + effectivePageCount;

        if (_prevPageBtn != null)
            _prevPageBtn.SetEnabled(_currentPage > 0);

        if (_nextPageBtn != null)
            _nextPageBtn.SetEnabled(_currentPage < effectivePageCount - 1);
    }

    private int GetEffectivePageCount()
    {
        bool isConsole = _viewModel != null && _viewModel.IsConsoleTemplate;
        return isConsole ? 3 : 2;
    }

    private int[] BuildPageMap()
    {
        bool isConsole = _viewModel != null && _viewModel.IsConsoleTemplate;
        if (isConsole)
            return new[] { PageSetup, PageHardware, PageFeatures };
        else
            return new[] { PageSetup, PageFeatures };
    }

    private void ApplyModeOnce()
    {
        if (_modeApplied || _viewModel == null) return;
        _modeApplied = true;

        if (_viewModel.IsUpdateMode)
        {
            _headerTitleLabel.text = "Create Update for " + _viewModel.OriginalProductName;
            if (_updateTypeSelectorContainer != null)
                _updateTypeSelectorContainer.style.display = DisplayStyle.Flex;

            LockDropdown(_productTypeDropdown, _productTypeDropdown.value);
            LockDropdown(_genreDropdown, _genreDropdown.value);
        }
        else if (_viewModel.IsSequelMode)
        {
            _headerTitleLabel.text = "Create Sequel to " + _viewModel.OriginalProductName;
            LockDropdown(_productTypeDropdown, _productTypeDropdown.value);
        }
    }

    private void RefreshDropdownsIfTemplateChanged()
    {
        if (_viewModel.SelectedTemplateId == _lastTemplateId) return;
        _lastTemplateId = _viewModel.SelectedTemplateId;

        // Rebuild template choices
        _templateChoices.Clear();
        int tc = _viewModel.Templates.Count;
        for (int i = 0; i < tc; i++)
            _templateChoices.Add(_viewModel.Templates[i].DisplayName);
        _productTypeDropdown.choices = _templateChoices;

        // Rebuild genre choices
        _genreChoices.Clear();
        _genreChoices.Add("(None)");
        int nc = _viewModel.AvailableNiches.Count;
        for (int i = 0; i < nc; i++)
            _genreChoices.Add(_viewModel.AvailableNiches[i].DisplayName);
        _genreDropdown.choices = _genreChoices;

        // Rebuild platform toggles
        RebuildPlatformToggles();
        RebuildDependencyToggles();
    }

    private void RebuildPlatformToggles()
    {
        if (_platformToggleContainer == null) return;
        _platformToggleContainer.Clear();
        _platformToggles.Clear();

        string lastType = null;
        int pc = _viewModel.AvailablePlatforms.Count;
        for (int i = 0; i < pc; i++)
        {
            var platform = _viewModel.AvailablePlatforms[i];
            if (platform.PlatformTypeLabel != lastType)
            {
                var header = new Label(platform.PlatformTypeLabel);
                header.AddToClassList("platform-type-header");
                _platformToggleContainer.Add(header);
                lastType = platform.PlatformTypeLabel;
            }

            var row = new VisualElement();
            row.AddToClassList("platform-toggle");
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;

            var toggle = new Toggle(platform.DisplayName);
            toggle.style.flexShrink = 0;
            toggle.userData = platform.PlatformId;
            toggle.RegisterCallback<ChangeEvent<bool>>(OnPlatformToggleChanged);
            row.Add(toggle);

            var usersLbl = new Label(UIFormatting.FormatUserCount(platform.ActiveUsers) + " users");
            usersLbl.AddToClassList("dep-quality-label");
            usersLbl.style.marginLeft = 4;
            row.Add(usersLbl);

            var licenseLbl = new Label(platform.LicensingCostLabel);
            licenseLbl.AddToClassList("dep-licensing-label");
            licenseLbl.style.marginLeft = 4;
            row.Add(licenseLbl);

            _platformToggleContainer.Add(row);
            _platformToggles.Add(toggle);
        }
    }

    private void BindPlatformToggles()
    {
        int count = _platformToggles.Count;
        for (int i = 0; i < count; i++)
        {
            var toggle = _platformToggles[i];
            if (!(toggle.userData is ProductId platId)) continue;
            bool isSelected = _viewModel.IsPlatformSelected(platId);
            if (toggle.value != isSelected)
                toggle.SetValueWithoutNotify(isSelected);
        }
    }

    private void RebuildDependencyToggles() {
        if (_depToggleContainer == null) return;
        _depToggleContainer.Clear();

        int toggleCount = _depToggles.Count;
        for (int i = 0; i < toggleCount; i++)
            if (_depToggles[i] != null)
                _depToggles[i].UnregisterCallback<ChangeEvent<bool>>(OnDepToggleChanged);
        _depToggles.Clear();

        int catCount = _viewModel.RequiredToolCategories.Count;
        for (int c = 0; c < catCount; c++) {
            ProductCategory cat = _viewModel.RequiredToolCategories[c];

            var header = new Label(UIFormatting.FormatCategory(cat));
            header.AddToClassList("dep-category-header");
            _depToggleContainer.Add(header);

            int toolCount = _viewModel.AvailableRequiredTools.Count;
            for (int t = 0; t < toolCount; t++) {
                var tool = _viewModel.AvailableRequiredTools[t];
                if (tool.Category != cat) continue;

                var row = new VisualElement();
                row.AddToClassList("dep-tool-row");

                var toggle = new Toggle(tool.DisplayName);
                toggle.AddToClassList("dep-tool-toggle");
                toggle.userData = tool.ToolId;
                toggle.RegisterCallback<ChangeEvent<bool>>(OnDepToggleChanged);
                row.Add(toggle);

                var qualityLabel = new Label(tool.QualitativeLabel);
                qualityLabel.AddToClassList("dep-quality-label");
                row.Add(qualityLabel);

                var licensingLabel = new Label(tool.LicensingCostLabel);
                licensingLabel.AddToClassList("dep-licensing-label");
                row.Add(licensingLabel);

                _depToggleContainer.Add(row);
                _depToggles.Add(toggle);
            }
        }
    }

    private void BindDependencyToggles() {
        int count = _depToggles.Count;
        var tools = _viewModel.AvailableRequiredTools;
        for (int i = 0; i < count; i++) {
            var toggle = _depToggles[i];
            if (!(toggle.userData is ProductId toolId)) continue;

            ProductCategory cat = default;
            int toolCount = tools.Count;
            for (int t = 0; t < toolCount; t++) {
                if (tools[t].ToolId == toolId) { cat = tools[t].Category; break; }
            }

            bool isSelected = _viewModel.IsToolSelected(cat, toolId);
            if (toggle.value != isSelected)
                toggle.SetValueWithoutNotify(isSelected);
        }

        bool hasDeps = _viewModel.RequiredToolCategories.Count > 0;
        _totalRoyaltyLabel.text = hasDeps
            ? "Total royalty cut: " + (_viewModel.TotalRoyaltyCut * 100f).ToString("F1") + "%"
            : "";
        _qualityCeilingLabel.text = _viewModel.QualityCeilingLabel;

        bool hasTechPenalty = _viewModel.TechLevelPenalty > 0f;
        _techPenaltyLabel.text = hasTechPenalty
            ? "Tech penalty: -" + (_viewModel.TechLevelPenalty * 100f).ToString("F0") + "% dev speed"
            : "";
        _techPenaltyLabel.style.display = hasTechPenalty ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private void OnDepToggleChanged(ChangeEvent<bool> evt) {
        if (_viewModel == null) return;
        var toggle = evt.currentTarget as Toggle;
        if (toggle == null || !(toggle.userData is ProductId toolId)) return;

        var tools = _viewModel.AvailableRequiredTools;
        ProductCategory cat = default;
        int toolCount = tools.Count;
        for (int i = 0; i < toolCount; i++) {
            if (tools[i].ToolId == toolId) { cat = tools[i].Category; break; }
        }

        if (evt.newValue) {
            _viewModel.SetToolSelection(cat, toolId);
        } else {
            _viewModel.ClearToolSelection(cat);
        }

        BindDependencyToggles();
        RefreshAfterChange();
    }

    private void BindProductSummary()
    {
        if (_summaryName == null) return;

        _summaryName.text = string.IsNullOrEmpty(_viewModel.ProductName) ? "—" : _viewModel.ProductName;

        string typeName = "—";
        int tc = _viewModel.Templates.Count;
        for (int i = 0; i < tc; i++)
        {
            if (_viewModel.Templates[i].TemplateId == _viewModel.SelectedTemplateId)
            {
                typeName = _viewModel.Templates[i].DisplayName;
                break;
            }
        }
        _summaryType.text = typeName;

        string platformNames = "—";
        var platIds = _viewModel.SelectedPlatformIds;
        if (platIds.Count > 0)
        {
            int pc = _viewModel.AvailablePlatforms.Count;
            var sb = new System.Text.StringBuilder();
            int found = 0;
            for (int pi = 0; pi < pc; pi++)
            {
                for (int si = 0; si < platIds.Count; si++)
                {
                    if (_viewModel.AvailablePlatforms[pi].PlatformId == platIds[si])
                    {
                        if (found > 0) sb.Append(", ");
                        sb.Append(_viewModel.AvailablePlatforms[pi].DisplayName);
                        found++;
                        break;
                    }
                }
            }
            if (found > 0) platformNames = sb.ToString();
        }
        _summaryPlatform.text = platformNames;

        int totalReach = _viewModel.TotalPlatformUserReach;
        if (_summaryReach != null) {
            _summaryReach.text = totalReach > 0
                ? UIFormatting.FormatUserCount(totalReach) + " users"
                : "—";
        }

        string nicheName = "—";
        if (_viewModel.SelectedNiche.HasValue)
        {
            int nc = _viewModel.AvailableNiches.Count;
            for (int i = 0; i < nc; i++)
            {
                if (_viewModel.AvailableNiches[i].Niche == _viewModel.SelectedNiche.Value)
                {
                    nicheName = _viewModel.AvailableNiches[i].DisplayName;
                    break;
                }
            }
        }
        _summaryGenre.text = nicheName;

        if (_predecessorInfoLabel != null)
        {
            if (_viewModel.IsSequelMode)
            {
                _predecessorInfoLabel.text = "Sequel of " + _viewModel.OriginalProductName;
                _predecessorInfoLabel.style.display = DisplayStyle.Flex;
            }
            else if (_viewModel.IsUpdateMode)
            {
                string deltaEta = _viewModel.EstimatedCompletionLabel;
                long deltaCost = _viewModel.CalculatedCost;
                string deltaText = "Update for " + _viewModel.OriginalProductName;
                if (deltaCost > 0) deltaText += "  |  Cost: " + UIFormatting.FormatMoney(deltaCost);
                if (!string.IsNullOrEmpty(deltaEta) && deltaEta != "Unknown") deltaText += "  |  ETA: " + deltaEta;
                _predecessorInfoLabel.text = deltaText;
                _predecessorInfoLabel.style.display = DisplayStyle.Flex;
            }
            else
            {
                _predecessorInfoLabel.style.display = DisplayStyle.None;
            }
        }
    }

    private void BindProductInfo()
    {
        // Name field — avoid circular update
        if (_productNameField.value != _viewModel.ProductName)
            _productNameField.SetValueWithoutNotify(_viewModel.ProductName);

        // Template dropdown — skip if locked in update/sequel mode
        if (!_productTypeDropdown.ClassListContains("field-locked"))
        {
            string templateName = "";
            int tc = _viewModel.Templates.Count;
            for (int i = 0; i < tc; i++)
            {
                if (_viewModel.Templates[i].TemplateId == _viewModel.SelectedTemplateId)
                {
                    templateName = _viewModel.Templates[i].DisplayName;
                    break;
                }
            }
            if (_productTypeDropdown.value != templateName)
                _productTypeDropdown.SetValueWithoutNotify(templateName);
        }

        // Genre dropdown — skip if locked
        if (!_genreDropdown.ClassListContains("field-locked"))
        {
            string nicheName = "(None)";
            if (_viewModel.SelectedNiche.HasValue)
            {
                int nc = _viewModel.AvailableNiches.Count;
                for (int i = 0; i < nc; i++)
                {
                    if (_viewModel.AvailableNiches[i].Niche == _viewModel.SelectedNiche.Value)
                    {
                        nicheName = _viewModel.AvailableNiches[i].DisplayName;
                        break;
                    }
                }
            }
            if (_genreDropdown.value != nicheName)
                _genreDropdown.SetValueWithoutNotify(nicheName);
        }
    }

    private void BindConditionalSections()
    {
        if (_viewModel == null) return;
        bool showNiche = _viewModel.HasNiches;
        _genreLabel.style.display = showNiche ? DisplayStyle.Flex : DisplayStyle.None;
        _genreDropdown.style.display = showNiche ? DisplayStyle.Flex : DisplayStyle.None;

        bool showPlatform = _viewModel.HasTargetPlatforms;
        _platformLabel.style.display = showPlatform ? DisplayStyle.Flex : DisplayStyle.None;
        _platformToggleContainer.style.display = showPlatform ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private void BindProductFit()
    {
        _expectedInterestLabel.text = (_viewModel.ExpectedInterest * 100f).ToString("F1") + "%";
        _wastedInterestLabel.text = ((int)_viewModel.WastedInterest) + "%";
        _scopeLabel.text = _viewModel.ScopeDisplay + " | " + _viewModel.ScopeEfficiencyLabel;
    }

    private void BindRadarChart()
    {
        _radarChart.SetAxisCount(_viewModel.MarketProfile.Length);
        _radarChart.SetAxisLabels(_viewModel.RadarAxisLabels);
        _radarChart.SetMarketProfile(_viewModel.MarketProfile);
        _radarChart.SetProductProfile(_viewModel.ProductProfile);
        _radarChart.AxisColors = _viewModel.RadarAxisColors;
    }

    private void BindEstimates()
    {
        _etaLabel.text = _viewModel.EstimatedCompletionLabel;
        _estQualityLabel.text = _viewModel.EstimatedQualityLabel;
        _upfrontCostLabel.text = UIFormatting.FormatMoney(_viewModel.CalculatedCost);
    }

    private void BindPricingSummary()
    {
        if (_viewModel == null) return;
        int basePrice = _viewModel.BaseProductPrice;
        int featureTotal = _viewModel.FeaturePriceTotal;

        _basePriceLabel.text = UIFormatting.FormatMoney(basePrice);
        _featurePriceLabel.text = featureTotal > 0
            ? "+" + UIFormatting.FormatMoney(featureTotal)
            : "$0";
        _marketPriceLabel.text = _viewModel.SweetSpotPrice > 0f
            ? UIFormatting.FormatMoney((long)_viewModel.SweetSpotPrice)
            : "—";

        string pricingModelValue = _viewModel.IsSubscriptionBased ? "Subscription" : "One-Time";
        if (_pricingModelDropdown.value != pricingModelValue)
            _pricingModelDropdown.SetValueWithoutNotify(pricingModelValue);

        if (Math.Abs(_priceField.value - _viewModel.Price) > 0.001f)
            _priceField.SetValueWithoutNotify(_viewModel.Price);

        if (_viewModel.Price > 0f && _viewModel.SweetSpotPrice > 0f) {
            float pctDiff = ((_viewModel.Price - _viewModel.SweetSpotPrice) / _viewModel.SweetSpotPrice) * 100f;
            if (pctDiff > 1f)
                _priceComparisonLabel.text = pctDiff.ToString("F0") + "% above market — fewer users, more revenue";
            else if (pctDiff < -1f)
                _priceComparisonLabel.text = (-pctDiff).ToString("F0") + "% below market — more users, less revenue";
            else
                _priceComparisonLabel.text = "At market price";
            _priceComparisonLabel.style.display = DisplayStyle.Flex;
        } else {
            _priceComparisonLabel.style.display = DisplayStyle.None;
        }

        _priceRatingLabel.text = _viewModel.PriceRatingLabel;
        SetClass(_priceRatingLabel, "price-rating--good", _viewModel.PriceRatingClass == "price-rating--good");
        SetClass(_priceRatingLabel, "price-rating--okay", _viewModel.PriceRatingClass == "price-rating--okay");
        SetClass(_priceRatingLabel, "price-rating--bad", _viewModel.PriceRatingClass == "price-rating--bad");

        bool hasPriceWarning = !string.IsNullOrEmpty(_viewModel.PriceWarningMessage);
        _priceWarningLabel.text = _viewModel.PriceWarningMessage;
        _priceWarningLabel.style.display = hasPriceWarning ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private void BindTeamRows()
    {
        var recommendations = _viewModel.TeamRecommendations;
        int recCount = recommendations.Count;
        int rowsToShow = Math.Min(recCount, MaxTeamRows);

        for (int i = 0; i < MaxTeamRows; i++)
        {
            if (i >= rowsToShow)
            {
                _teamRows[i].style.display = DisplayStyle.None;
                continue;
            }

            _teamRows[i].style.display = DisplayStyle.Flex;
            var rec = recommendations[i];

            _teamRoleLabels[i].text = rec.Role.ToString();

            // Status class
            string newClass = rec.StatusClass;
            if (_teamCurrentStatusClass[i] != newClass)
            {
                if (!string.IsNullOrEmpty(_teamCurrentStatusClass[i]))
                    _teamRows[i].RemoveFromClassList(_teamCurrentStatusClass[i]);
                if (!string.IsNullOrEmpty(newClass))
                    _teamRows[i].AddToClassList(newClass);
                _teamCurrentStatusClass[i] = newClass;
            }

            // Rebuild team dropdown choices
            _teamDropdownChoices[i].Clear();
            _teamDropdownChoices[i].Add("(Unassigned)");

            if (rec.Role == ProductTeamRole.Marketing) {
                int mc = _viewModel.AvailableMarketingTeams.Count;
                for (int j = 0; j < mc; j++)
                    _teamDropdownChoices[i].Add(_viewModel.AvailableMarketingTeams[j].Name);
            } else {
                TeamType targetType = RoleToTeamType(rec.Role);
                int ac = _viewModel.AvailableTeams.Count;
                for (int j = 0; j < ac; j++) {
                    if (_viewModel.AvailableTeams[j].TeamTypeEnum == targetType)
                        _teamDropdownChoices[i].Add(_viewModel.AvailableTeams[j].Name);
                }
                int bc = _viewModel.BusyTeams.Count;
                for (int j = 0; j < bc; j++) {
                    if (_viewModel.BusyTeams[j].TeamTypeEnum == targetType)
                        _teamDropdownChoices[i].Add(_viewModel.BusyTeams[j].Name + " [busy]");
                }
            }
            _teamDropdowns[i].choices = _teamDropdownChoices[i];

            string selectedTeamName = rec.AssignedTeamName != null ? rec.AssignedTeamName : "(Unassigned)";
            if (_teamDropdowns[i].value != selectedTeamName)
                _teamDropdowns[i].SetValueWithoutNotify(selectedTeamName);

            _teamSizeLabels[i].text = rec.AssignedTeamName != null
                ? rec.ActualSize + "/" + rec.RecommendedSize
                : "";

            _teamSkillLabels[i].text = rec.AssignedTeamName != null
                ? rec.ActualAvgSkill.ToString("F0") + "/" + rec.RecommendedAvgSkill.ToString("F0")
                : "";
        }
    }

    private void BindValidationSection()
    {
        var msgs = _viewModel.ValidationMessages;
        int msgCount = msgs.Count;
        int visibleCount = Math.Min(msgCount, MaxValidationMessages);

        for (int i = 0; i < MaxValidationMessages; i++)
        {
            if (i >= visibleCount)
            {
                _validationMessages[i].style.display = DisplayStyle.None;
                continue;
            }

            var msg = msgs[i];
            _validationMessages[i].text = msg.Message;
            _validationMessages[i].style.display = DisplayStyle.Flex;
            SetClass(_validationMessages[i], "validation-error", msg.IsError);
            SetClass(_validationMessages[i], "validation-warning", !msg.IsError);
        }

        bool canStart = _viewModel.CanStartDevelopment;
        SetClass(_startDevBtn, "btn-start-dev--disabled", !canStart);
        _startDevBtn.SetEnabled(canStart);
        SetClass(_startDevBtnBar, "btn-start-dev--disabled", !canStart);
        _startDevBtnBar.SetEnabled(canStart);
    }

    private void BindReleaseDate()
    {
        if (_viewModel == null || _releaseDateValueLabel == null) return;
        if (_viewModel.SelectedTargetDay > 0)
            _releaseDateValueLabel.text = _viewModel.ReleaseDateDisplay;
        else
            _releaseDateValueLabel.text = "Not set";
    }

    private void OnReleaseDateSliderChanged(ChangeEvent<int> evt)
    {
        if (_viewModel == null) return;
        int currentDay = 0;
        if (_dispatcher is WindowManager wm && wm.GameController != null)
        {
            var gs = wm.GameController.GetGameState();
            currentDay = gs?.timeState?.currentDay ?? 0;
        }
        _viewModel.SetTargetDay(currentDay + evt.newValue);
        if (_releaseDateValueLabel != null)
            _releaseDateValueLabel.text = _viewModel.ReleaseDateDisplay;
    }

    private void BindFeatures()
    {
        if (_viewModel == null) return;

        // Update mode — hide entire feature panel for BugFix; for others adjust filter
        if (_viewModel.IsUpdateMode && _viewModel.SelectedUpdateType == ProductUpdateType.BugFix)
        {
            _featureScroll.style.display = DisplayStyle.None;
            _featureCategoryTabBar.style.display = DisplayStyle.None;
            _filterBar.style.display = DisplayStyle.None;
            _featureSummaryLabel.style.display = DisplayStyle.None;
            return;
        }

        _featureScroll.style.display = DisplayStyle.Flex;

        bool templateChanged = _viewModel.SelectedTemplateId != _lastFeatureTemplateId;
        if (templateChanged)
        {
            _lastFeatureTemplateId = _viewModel.SelectedTemplateId;
            RebuildCategoryTabBar();
        }

        bool showTabs = _viewModel.ShouldShowCategoryTabs;
        _featureCategoryTabBar.style.display = showTabs ? DisplayStyle.Flex : DisplayStyle.None;
        _filterBar.style.display = _viewModel.Features.Count > 0 ? DisplayStyle.Flex : DisplayStyle.None;
        _featureSummaryLabel.style.display = DisplayStyle.Flex;

        UpdateCategoryTabLabels();
        _viewModel.RebuildFilteredList();
        BindFeaturePool();
    }

    private void BindUpdateTypeSelector()
    {
        if (!_viewModel.IsUpdateMode || _updateTypeSelectorContainer == null) return;

        bool typeSet = _viewModel.SelectedUpdateType != default(ProductUpdateType) || _viewModel.IsUpdateMode;
        var selected = _viewModel.SelectedUpdateType;

        SetUpdateTypeBtnActive(_updateTypeBugFixBtn, selected == ProductUpdateType.BugFix);
        SetUpdateTypeBtnActive(_updateTypeAddFeaturesBtn, selected == ProductUpdateType.AddFeatures);
        SetUpdateTypeBtnActive(_updateTypeRemoveFeatureBtn, selected == ProductUpdateType.RemoveFeature);
    }

    private void SetUpdateTypeBtnActive(Button btn, bool active)
    {
        if (btn == null) return;
        if (active)
        {
            btn.AddToClassList("btn-primary");
            btn.RemoveFromClassList("btn-secondary");
            btn.AddToClassList("update-type-btn--active");
        }
        else
        {
            btn.RemoveFromClassList("btn-primary");
            btn.AddToClassList("btn-secondary");
            btn.RemoveFromClassList("update-type-btn--active");
        }
    }

    private void BindHardwarePageVisibility()
    {
        bool isConsole = _viewModel != null && _viewModel.IsConsoleTemplate;
        ShowPage(_currentPage);

        if (isConsole)
            RefreshHardwarePreviewLabels();
    }

    private void RefreshHardwareRadarChart()
    {
        if (_hardwareRadarChart == null || _viewModel == null) return;

        var cfg = _viewModel.CurrentHardwareGenConfig;
        var hw = _viewModel.HardwareConfig;

        float[] product = {
            TierToNormalized((int)hw.processingTier),
            TierToNormalized((int)hw.graphicsTier),
            TierToNormalized((int)hw.memoryTier),
            TierToNormalized((int)hw.storageTier),
            FormFactorToNormalized((int)hw.formFactor)
        };

        float[] market;
        if (cfg != null) {
            market = new float[] {
                TierToNormalized((int)cfg.expectedProcessingTier),
                TierToNormalized((int)cfg.expectedGraphicsTier),
                TierToNormalized((int)cfg.expectedMemoryTier),
                TierToNormalized((int)cfg.expectedStorageTier),
                0.33f
            };
        } else {
            market = new float[] { 0.5f, 0.5f, 0.25f, 0.25f, 0.33f };
        }

        _hardwareRadarChart.SetMarketProfile(market);
        _hardwareRadarChart.SetProductProfile(product);
        _hardwareRadarChart.MarkDirtyRepaint();
    }

    private static float TierToNormalized(int tierIndex)
    {
        switch (tierIndex)
        {
            case 0: return 0.25f;
            case 1: return 0.5f;
            case 2: return 0.75f;
            case 3: return 1.0f;
            default: return 0.25f;
        }
    }

    private static float FormFactorToNormalized(int formFactorIndex)
    {
        switch (formFactorIndex)
        {
            case 0: return 0.33f;
            case 1: return 0.66f;
            case 2: return 1.0f;
            default: return 0.33f;
        }
    }

    private void RefreshHardwarePreviewLabels()
    {
        if (_viewModel == null) return;
        _hwManufactureCostLabel.text = "Mfg Cost: " + UIFormatting.FormatMoney(_viewModel.ManufactureCostPerUnit) + "/unit";
        _hwDevCostLabel.text = "R&D Add: " + UIFormatting.FormatMoney(_viewModel.HardwareDevCostAdd);
        _hwFeatureAvailLabel.text = "Features: " + _viewModel.AvailableFeatureCount + " unlocked / " + _viewModel.LockedFeatureCount + " locked";

        var locked = _viewModel.LockedFeatureNames;
        if (locked != null && locked.Length > 0)
        {
            _hwLockedFeaturesLabel.text = "Locked: " + string.Join(", ", locked);
            _hwLockedFeaturesLabel.style.display = DisplayStyle.Flex;
        }
        else
        {
            _hwLockedFeaturesLabel.style.display = DisplayStyle.None;
        }

        RefreshHardwareRadarChart();
    }

    private void SetHwTierButtonActive(List<Button> buttons, int activeIndex)
    {
        int count = buttons.Count;
        for (int i = 0; i < count; i++)
        {
            bool active = i == activeIndex;
            if (active) { buttons[i].AddToClassList("btn-primary"); buttons[i].RemoveFromClassList("btn-secondary"); buttons[i].AddToClassList("hw-tier-btn--active"); }
            else { buttons[i].RemoveFromClassList("btn-primary"); buttons[i].AddToClassList("btn-secondary"); buttons[i].RemoveFromClassList("hw-tier-btn--active"); }
        }
    }

    public void Dispose()
    {
        if (_cancelBtn != null) _cancelBtn.clicked -= OnCancelClicked;
        if (_startDevBtn != null) _startDevBtn.clicked -= OnStartDevClicked;
        if (_startDevBtnBar != null) _startDevBtnBar.clicked -= OnStartDevClicked;
        if (_prevPageBtn != null) _prevPageBtn.clicked -= OnPrevPageClicked;
        if (_nextPageBtn != null) _nextPageBtn.clicked -= OnNextPageClicked;

        if (_updateTypeBugFixBtn != null) _updateTypeBugFixBtn.UnregisterCallback<ClickEvent>(OnUpdateTypeClicked);
        if (_updateTypeAddFeaturesBtn != null) _updateTypeAddFeaturesBtn.UnregisterCallback<ClickEvent>(OnUpdateTypeClicked);
        if (_updateTypeRemoveFeatureBtn != null) _updateTypeRemoveFeatureBtn.UnregisterCallback<ClickEvent>(OnUpdateTypeClicked);

        int pCount = _processingTierButtons.Count;
        for (int i = 0; i < pCount; i++)
            if (_processingTierButtons[i] != null) _processingTierButtons[i].UnregisterCallback<ClickEvent>(OnHwTierButtonClicked);
        int gCount = _graphicsTierButtons.Count;
        for (int i = 0; i < gCount; i++)
            if (_graphicsTierButtons[i] != null) _graphicsTierButtons[i].UnregisterCallback<ClickEvent>(OnHwTierButtonClicked);
        int mCount = _memoryTierButtons.Count;
        for (int i = 0; i < mCount; i++)
            if (_memoryTierButtons[i] != null) _memoryTierButtons[i].UnregisterCallback<ClickEvent>(OnHwTierButtonClicked);
        int sCount = _storageTierButtons.Count;
        for (int i = 0; i < sCount; i++)
            if (_storageTierButtons[i] != null) _storageTierButtons[i].UnregisterCallback<ClickEvent>(OnHwTierButtonClicked);
        int ffCount = _formFactorButtons.Count;
        for (int i = 0; i < ffCount; i++)
            if (_formFactorButtons[i] != null) _formFactorButtons[i].UnregisterCallback<ClickEvent>(OnHwTierButtonClicked);

        if (_productNameField != null)
            _productNameField.UnregisterCallback<ChangeEvent<string>>(OnProductNameChanged);
        if (_productTypeDropdown != null)
            _productTypeDropdown.UnregisterCallback<ChangeEvent<string>>(OnProductTypeChanged);
        if (_genreDropdown != null)
            _genreDropdown.UnregisterCallback<ChangeEvent<string>>(OnGenreChanged);
        if (_pricingModelDropdown != null)
            _pricingModelDropdown.UnregisterCallback<ChangeEvent<string>>(OnPricingModelChanged);
        if (_priceField != null)
            _priceField.UnregisterCallback<ChangeEvent<float>>(OnPriceChanged);
        if (_releaseDateSlider != null)
            _releaseDateSlider.UnregisterValueChangedCallback(OnReleaseDateSliderChanged);
        _releaseDateSlider = null;
        _releaseDateValueLabel = null;
        _releaseDateHintLabel = null;

        int platToggleCount = _platformToggles.Count;
        for (int i = 0; i < platToggleCount; i++)
            if (_platformToggles[i] != null)
                _platformToggles[i].UnregisterCallback<ChangeEvent<bool>>(OnPlatformToggleChanged);
        _platformToggles.Clear();

        int depToggleCount = _depToggles.Count;
        for (int i = 0; i < depToggleCount; i++)
            if (_depToggles[i] != null)
                _depToggles[i].UnregisterCallback<ChangeEvent<bool>>(OnDepToggleChanged);
        _depToggles.Clear();

        if (_teamDropdowns != null)
        {
            for (int i = 0; i < MaxTeamRows; i++)
            {
                if (_teamDropdowns[i] != null)
                    _teamDropdowns[i].UnregisterCallback<ChangeEvent<string>>(OnTeamDropdownChanged);
            }
        }

        int catTabCount = _categoryTabButtons.Count;
        for (int i = 0; i < catTabCount; i++)
        {
            if (_categoryTabButtons[i] != null)
                _categoryTabButtons[i].UnregisterCallback<ClickEvent>(OnCategoryTabClicked);
        }
        _categoryTabButtons.Clear();

        int selFiltCount = _selectedFilterBtns.Count;
        for (int i = 0; i < selFiltCount; i++)
        {
            if (_selectedFilterBtns[i] != null)
                _selectedFilterBtns[i].UnregisterCallback<ClickEvent>(OnSelectedFilterClicked);
        }
        _selectedFilterBtns.Clear();

        int sortCount = _sortModeBtns.Count;
        for (int i = 0; i < sortCount; i++)
        {
            if (_sortModeBtns[i] != null)
                _sortModeBtns[i].UnregisterCallback<ClickEvent>(OnSortModeClicked);
        }
        _sortModeBtns.Clear();

        _featurePool = null;
        _lastFeatureTemplateId = null;
        _filteredIndicesBind.Clear();

        _updateTypeSelectorContainer = null;
        _updateTypeBugFixBtn = null;
        _updateTypeAddFeaturesBtn = null;
        _updateTypeRemoveFeatureBtn = null;
        _predecessorInfoLabel = null;
        _modeApplied = false;

        _viewModel = null;
        _root = null;
    }

    // ── Event handlers ────────────────────────────────────────────────────────

    private void OnPrevPageClicked()
    {
        ShowPage(_currentPage - 1);
    }

    private void OnNextPageClicked()
    {
        ShowPage(_currentPage + 1);
    }

    private void OnHwTierButtonClicked(ClickEvent evt)
    {
        var btn = evt.currentTarget as Button;
        if (btn == null || _viewModel == null) return;
        if (!(btn.userData is HwTierButtonData data)) return;

        switch (data.Group)
        {
            case HwGroupProcessing:
                _viewModel.SetProcessingTier((HardwareTier)data.Index);
                SetHwTierButtonActive(_processingTierButtons, data.Index);
                break;
            case HwGroupGraphics:
                _viewModel.SetGraphicsTier((HardwareTier)data.Index);
                SetHwTierButtonActive(_graphicsTierButtons, data.Index);
                break;
            case HwGroupMemory:
                _viewModel.SetMemoryTier((HardwareTier)data.Index);
                SetHwTierButtonActive(_memoryTierButtons, data.Index);
                break;
            case HwGroupStorage:
                _viewModel.SetStorageTier((HardwareTier)data.Index);
                SetHwTierButtonActive(_storageTierButtons, data.Index);
                break;
            case HwGroupFormFactor:
                _viewModel.SetFormFactor((ConsoleFormFactor)data.Index);
                SetHwTierButtonActive(_formFactorButtons, data.Index);
                break;
        }

        RefreshHardwarePreviewLabels();
    }

    private void OnUpdateTypeClicked(ClickEvent evt)
    {
        var btn = evt.currentTarget as Button;
        if (btn == null || _viewModel == null) return;
        if (!(btn.userData is ProductUpdateType updateType)) return;

        _viewModel.SelectUpdateType(updateType);
        BindUpdateTypeSelector();
        BindFeatures();
    }

    private void OnCancelClicked()
    {
        OnCancelRequested?.Invoke();
    }

    private void OnStartDevClicked()
    {
        if (_viewModel == null || !_viewModel.CanStartDevelopment) return;

        var assignments = _viewModel.GetTeamAssignments();
        var teamAssignmentArray = new TeamAssignment[assignments.Length];
        for (int i = 0; i < assignments.Length; i++)
            teamAssignmentArray[i] = assignments[i];

        var cmd = new CreateProductCommand
        {
            TemplateId = _viewModel.SelectedTemplateId,
            ProductName = _viewModel.ProductName,
            SelectedFeatureIds = _viewModel.GetSelectedFeatureIds(),
            IsSubscriptionBased = _viewModel.IsSubscriptionBased,
            Price = _viewModel.Price,
            TargetPlatformIds = _viewModel.GetSelectedPlatformIds(),
            RequiredToolIds = _viewModel.GetSelectedToolIds(),
            Stance = _viewModel.SelectedStance,
            PredecessorProductId = _viewModel.SelectedPredecessorId,
            InitialTeamAssignments = teamAssignmentArray,
            SequelOfId = _sequelOfId,
            HasHardwareConfig = _viewModel.IsConsoleTemplate,
            HardwareConfig = _viewModel.HardwareConfig,
            TargetDay = _viewModel.SelectedTargetDay,
            DistributionModel = _viewModel.SelectedDistribution,
            LicensingRate = _viewModel.SelectedLicensingRate,
            MonthlySubscriptionPrice = _viewModel.SubscriptionPrice,
            SelectedNiche = _viewModel.SelectedNiche ?? ProductNiche.None
        };

        _dispatcher.Dispatch(cmd);
        OnProductCreated?.Invoke();
    }

    private void OnProductNameChanged(ChangeEvent<string> evt)
    {
        _viewModel?.SetProductName(evt.newValue);
        RefreshAfterChange();
    }

    private void OnProductTypeChanged(ChangeEvent<string> evt)
    {
        if (_viewModel == null || _definitions == null) return;
        string chosen = evt.newValue;
        int tc = _viewModel.Templates.Count;
        for (int i = 0; i < tc; i++)
        {
            if (_viewModel.Templates[i].DisplayName == chosen)
            {
                string templateId = _viewModel.Templates[i].TemplateId;
                _viewModel.SelectTemplate(templateId, _definitions);

                ProductTemplateDefinition matchedDef = null;
                for (int d = 0; d < _definitions.Length; d++)
                {
                    if (_definitions[d] != null && _definitions[d].templateId == templateId)
                    {
                        matchedDef = _definitions[d];
                        break;
                    }
                }
                _viewModel.RepopulateDependencies(matchedDef);

                _lastTemplateId = null; // Force dropdown refresh on next Bind
                break;
            }
        }
        RefreshAfterChange();
    }

    private void OnPlatformToggleChanged(ChangeEvent<bool> evt)
    {
        if (_viewModel == null) return;
        var toggle = evt.currentTarget as Toggle;
        if (toggle == null || !(toggle.userData is ProductId platId)) return;
        _viewModel.TogglePlatform(platId, evt.newValue);
        RefreshAfterChange();
    }

    private void OnGenreChanged(ChangeEvent<string> evt)
    {
        if (_viewModel == null) return;
        string chosen = evt.newValue;
        if (chosen == "(None)") return;

        int nc = _viewModel.AvailableNiches.Count;
        for (int i = 0; i < nc; i++)
        {
            if (_viewModel.AvailableNiches[i].DisplayName == chosen)
            {
                _viewModel.SelectNiche(_viewModel.AvailableNiches[i].Niche);
                break;
            }
        }
        RefreshAfterChange();
    }

    private void OnPricingModelChanged(ChangeEvent<string> evt)
    {
        _viewModel?.SetPricingModel(evt.newValue == "Subscription");
        RefreshAfterChange();
    }

    private void OnPriceChanged(ChangeEvent<float> evt)
    {
        _viewModel?.SetPrice(evt.newValue);
        BindPricingSummary();
    }

    private void OnTeamDropdownChanged(ChangeEvent<string> evt) {
        if (_viewModel == null) return;
        var dropdown = evt.currentTarget as DropdownField;
        if (dropdown == null) return;

        int rowIndex = (int)dropdown.userData;
        var recommendations = _viewModel.TeamRecommendations;
        if (rowIndex < 0 || rowIndex >= recommendations.Count) return;

        ProductTeamRole role = recommendations[rowIndex].Role;
        string chosen = evt.newValue;

        if (chosen == "(Unassigned)") {
            _viewModel.UnassignTeam(role);
            RefreshAfterChange();
            return;
        }

        if (role == ProductTeamRole.Marketing) {
            int mc = _viewModel.AvailableMarketingTeams.Count;
            for (int i = 0; i < mc; i++) {
                if (_viewModel.AvailableMarketingTeams[i].Name == chosen) {
                    _viewModel.AssignTeam(role, _viewModel.AvailableMarketingTeams[i].Id);
                    RefreshAfterChange();
                    return;
                }
            }
        } else {
            TeamType targetType = RoleToTeamType(role);
            int ac = _viewModel.AvailableTeams.Count;
            for (int i = 0; i < ac; i++) {
                if (_viewModel.AvailableTeams[i].TeamTypeEnum == targetType
                    && _viewModel.AvailableTeams[i].Name == chosen) {
                    _viewModel.AssignTeam(role, _viewModel.AvailableTeams[i].Id);
                    RefreshAfterChange();
                    return;
                }
            }

            string busyStripped = chosen.EndsWith(" [busy]")
                ? chosen.Substring(0, chosen.Length - 7) : chosen;
            int bc = _viewModel.BusyTeams.Count;
            for (int i = 0; i < bc; i++) {
                if (_viewModel.BusyTeams[i].TeamTypeEnum == targetType
                    && _viewModel.BusyTeams[i].Name == busyStripped) {
                    _viewModel.AssignTeam(role, _viewModel.BusyTeams[i].Id);
                    RefreshAfterChange();
                    return;
                }
            }
        }
    }

    private static TeamType RoleToTeamType(ProductTeamRole role) {
        switch (role) {
            case ProductTeamRole.Programming: return TeamType.Programming;
            case ProductTeamRole.Design:      return TeamType.Design;
            case ProductTeamRole.QA:          return TeamType.QA;
            case ProductTeamRole.SFX:         return TeamType.SFX;
            case ProductTeamRole.VFX:         return TeamType.VFX;
            case ProductTeamRole.Marketing:   return TeamType.Marketing;
            default:                          return TeamType.Contracts;
        }
    }

    private void OnCategoryTabClicked(ClickEvent evt)
    {
        var btn = evt.currentTarget as Button;
        if (btn == null || _viewModel == null) return;
        if (!(btn.userData is FeatureCategory cat)) return;
        _viewModel.SelectCategory(cat);
        UpdateCategoryTabLabels();
        _viewModel.RebuildFilteredList();
        BindFeaturePool();
    }

    private void OnSelectedFilterClicked(ClickEvent evt)
    {
        var btn = evt.currentTarget as Button;
        if (btn == null || _viewModel == null) return;
        int idx = (int)btn.userData;
        _viewModel.SetSelectedFilter((CreateProductViewModel.FeatureSelectedFilter)idx);

        int count = _selectedFilterBtns.Count;
        for (int i = 0; i < count; i++)
            SetClass(_selectedFilterBtns[i], "filter-btn--active", i == idx);

        _viewModel.RebuildFilteredList();
        BindFeaturePool();
    }

    private void OnSortModeClicked(ClickEvent evt)
    {
        var btn = evt.currentTarget as Button;
        if (btn == null || _viewModel == null) return;
        int idx = (int)btn.userData;
        _viewModel.SetSortMode((CreateProductViewModel.FeatureSortMode)idx);

        int count = _sortModeBtns.Count;
        for (int i = 0; i < count; i++)
            SetClass(_sortModeBtns[i], "filter-btn--active", i == idx);

        _viewModel.RebuildFilteredList();
        BindFeaturePool();
    }

    private void OnFeatureToggled(ChangeEvent<bool> evt)
    {
        if (_viewModel == null) return;
        var toggle = evt.currentTarget as Toggle;
        if (toggle == null) return;

        int featureIndex = toggle.userData is int idx ? idx : -1;
        if (featureIndex < 0 || featureIndex >= _viewModel.Features.Count) return;

        string featureId = _viewModel.Features[featureIndex].FeatureId;
        _viewModel.ToggleFeature(featureId, evt.newValue);
        _viewModel.RebuildFilteredList();
        UpdateCategoryTabLabels();
        RefreshAfterChange();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Label AddMetricRow(VisualElement parent, string keyText)
    {
        var row = new VisualElement();
        row.AddToClassList("create-product-metric-row");

        var key = new Label(keyText);
        key.AddToClassList("create-product-metric-key");
        row.Add(key);

        var value = new Label("—");
        value.AddToClassList("create-product-metric-value");
        row.Add(value);

        parent.Add(row);
        return value;
    }

    private static void SetClass(VisualElement element, string className, bool enabled)
    {
        if (enabled) element.AddToClassList(className);
        else element.RemoveFromClassList(className);
    }

    private struct HwTierButtonData
    {
        public int Group;
        public int Index;
    }
}
