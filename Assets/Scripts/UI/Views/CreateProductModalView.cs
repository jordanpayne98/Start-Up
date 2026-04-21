#if false

// DELETED — replaced by CreateProductView. Delete this file from disk.
    private readonly ICommandDispatcher _dispatcher;
    private readonly IModalPresenter _modal;
    private readonly ITooltipProvider _tooltipProvider;
    private CreateProductModalViewModel _viewModel;

    // ── Root panels (one per step) ────────────────────────────────────────────
    private VisualElement _stepProductType;
    private VisualElement _stepPlatformTarget;
    private VisualElement _stepHardwareDesign;
    private VisualElement _stepToolSelection;
    private VisualElement _stepGenreNiche;
    private VisualElement _stepFeatures;
    private VisualElement _stepPricingName;
    private VisualElement _stepTeamAssignment;

    // ── Step 1 — Product Type ─────────────────────────────────────────────────
    private VisualElement _templateContainer;
    private ElementPool _templatePool;

    // ── Step 2 — Platform Target ──────────────────────────────────────────────
    private VisualElement _platformListContainer;
    private Label _platformMultiWarningLabel;

    // ── Step 2.5 — Hardware Design ────────────────────────────────────────────
    private readonly List<Button> _processingTierButtons = new List<Button>();
    private readonly List<Button> _graphicsTierButtons   = new List<Button>();
    private readonly List<Button> _memoryTierButtons     = new List<Button>();
    private readonly List<Button> _storageTierButtons    = new List<Button>();
    private readonly List<Button> _formFactorButtons     = new List<Button>();
    private Label _hwManufactureCostLabel;
    private Label _hwDevCostLabel;
    private Label _hwFeatureAvailLabel;
    private Label _hwLockedFeaturesLabel;

    // ── Step 3 — Tool Selection ───────────────────────────────────────────────
    private VisualElement _toolCategoryContainer;

    // ── Step 4 — Genre / Niche ────────────────────────────────────────────────
    private VisualElement _nicheListContainer;
    private VisualElement _stanceContainer;
    private Button _stanceStandardBtn;
    private Button _stanceCrossGenBtn;
    private Label _crossGenCostWarningLabel;
    private VisualElement _predecessorContainer;
    private Label _predecessorLabel;

    // ── Step 5 — Features ─────────────────────────────────────────────────────
    private VisualElement _featureContainer;
    private VisualElement _featureCategoryTabBar;
    private Label _featureSummaryLabel;
    private Label _missingExpectedWarningLabel;
    private readonly List<Button> _categoryTabButtons = new List<Button>();
    private VisualElement _filterBar;
    private readonly List<List<Button>> _filterGroupButtons = new List<List<Button>>();

    // ── Step 6 — Pricing & Name ───────────────────────────────────────────────
    private TextField _nameField;
    private Toggle _subscriptionToggle;
    private FloatField _priceField;
    private Label _priceRangeLabel;
    private Label _sweetSpotLabel;
    private Label _priceRatingLabel;
    private Label _priceWarningLabel;
    private Label _manufactureInfoLabel;
    private Label _marginLabel;
    private Label _lossLeaderLabel;
    private SliderInt _releaseDateSlider;
    private Label _releaseDateValueLabel;
    private Label _releaseDateHintLabel;

    // ── Step 6 — Distribution Model ──────────────────────────────────────────
    private VisualElement _distributionContainer;
    private Button _distributionProprietaryBtn;
    private Button _distributionLicensedBtn;
    private Button _distributionOpenSourceBtn;
    private SliderInt _royaltySlider;
    private Label _royaltyValueLabel;
    private VisualElement _subscriptionPriceContainer;
    private SliderInt _subscriptionPriceSlider;
    private Label _subscriptionPriceValueLabel;

    // ── Step 7 — Team Assignment ──────────────────────────────────────────────
    private VisualElement _teamRoleContainer;
    private Label _completionLabel;
    private Label _salaryCostLabel;

    // ── Shared footer ─────────────────────────────────────────────────────────
    private Label _stepIndicatorLabel;
    private Label _costLabel;
    private Button _backButton;
    private Button _nextButton;
    private Button _createButton;
    private Button _cancelButton;

    // ── Internal ──────────────────────────────────────────────────────────────
    private ProductTemplateDefinition[] _cachedDefinitions = new ProductTemplateDefinition[0];
    private Label _headerTitleLabel;
    private VisualElement _updateTypeContainer;

    private readonly ProductId? _updateProductId;
    private readonly ProductId? _sequelOfId;

    public CreateProductModalView(ICommandDispatcher dispatcher, IModalPresenter modal,
        ITooltipProvider tooltipProvider = null,
        ProductId? updateProductId = null, ProductId? sequelOfId = null)
    {
        _dispatcher = dispatcher;
        _modal = modal;
        _tooltipProvider = tooltipProvider;
        _updateProductId = updateProductId;
        _sequelOfId = sequelOfId;
    }

    public void Initialize(VisualElement root)
    {
        root.AddToClassList("create-product-wizard");

        // Header
        var header = new VisualElement();
        header.AddToClassList("modal-header");

        var title = new Label("Create New Product");
        title.AddToClassList("text-xl");
        title.AddToClassList("text-bold");
        header.Add(title);
        _headerTitleLabel = title;

        _stepIndicatorLabel = new Label("Step 1 of 7");
        _stepIndicatorLabel.AddToClassList("wizard-step-indicator");
        header.Add(_stepIndicatorLabel);

        root.Add(header);

        // Step panels
        _stepProductType    = BuildStepProductType();
        _stepPlatformTarget = BuildStepPlatformTarget();
        _stepHardwareDesign = BuildStepHardwareDesign();
        _stepToolSelection  = BuildStepToolSelection();
        _stepGenreNiche     = BuildStepGenreNiche();
        _stepFeatures       = BuildStepFeatures();
        _stepPricingName    = BuildStepPricingName();
        _stepTeamAssignment = BuildStepTeamAssignment();

        var body = new ScrollView();
        body.AddToClassList("modal-body");
        body.style.flexGrow = 1;
        body.style.flexShrink = 1;
        body.contentContainer.Add(_stepProductType);
        body.contentContainer.Add(_stepPlatformTarget);
        body.contentContainer.Add(_stepHardwareDesign);
        body.contentContainer.Add(_stepToolSelection);
        body.contentContainer.Add(_stepGenreNiche);
        body.contentContainer.Add(_stepFeatures);
        body.contentContainer.Add(_stepPricingName);
        body.contentContainer.Add(_stepTeamAssignment);
        root.Add(body);

        // Footer
        var footer = new VisualElement();
        footer.AddToClassList("modal-footer");

        var costRow = new VisualElement();
        costRow.AddToClassList("flex-row");
        costRow.AddToClassList("align-center");
        costRow.style.flexGrow = 1;

        var costKeyLabel = new Label("Total Cost:");
        costKeyLabel.AddToClassList("metric-secondary");
        costKeyLabel.style.marginRight = 8;
        costRow.Add(costKeyLabel);

        _costLabel = new Label("$0");
        _costLabel.AddToClassList("metric-primary");
        _costLabel.AddToClassList("text-accent");
        costRow.Add(_costLabel);
        footer.Add(costRow);

        var btnRow = new VisualElement();
        btnRow.AddToClassList("flex-row");

        _cancelButton = new Button { text = "Cancel" };
        _cancelButton.AddToClassList("btn-secondary");
        _cancelButton.style.marginRight = 8;
        _cancelButton.clicked += OnCancelClicked;
        btnRow.Add(_cancelButton);

        _backButton = new Button { text = "\u2190 Back" };
        _backButton.AddToClassList("btn-secondary");
        _backButton.style.marginRight = 8;
        _backButton.clicked += OnBackClicked;
        btnRow.Add(_backButton);

        _nextButton = new Button { text = "Next \u2192" };
        _nextButton.AddToClassList("btn-primary");
        _nextButton.style.marginRight = 8;
        _nextButton.clicked += OnNextClicked;
        btnRow.Add(_nextButton);

        _createButton = new Button { text = "Create Product" };
        _createButton.AddToClassList("btn-primary");
        _createButton.clicked += OnCreateClicked;
        btnRow.Add(_createButton);

        footer.Add(btnRow);
        root.Add(footer);
    }

    // ── Step builders ──────────────────────────────────────────────────────────

    private VisualElement BuildStepProductType()
    {
        var step = new VisualElement();
        step.name = "step-product-type";

        var header = new Label("Select a Product Type");
        header.AddToClassList("section-header");
        step.Add(header);

        var scroll = new ScrollView();
        scroll.style.maxHeight = 340;
        _templateContainer = scroll.contentContainer;
        _templatePool = new ElementPool(CreateTemplateItem, _templateContainer);
        step.Add(scroll);

        _updateTypeContainer = new VisualElement();
        _updateTypeContainer.style.display = DisplayStyle.None;
        step.Add(_updateTypeContainer);

        return step;
    }

    private VisualElement BuildStepPlatformTarget()
    {
        var step = new VisualElement();
        step.name = "step-platform-target";

        var header = new Label("Select Target Platform(s)");
        header.AddToClassList("section-header");
        step.Add(header);

        var note = new Label("Your product will target the selected platform(s). Multi-platform increases development cost.");
        note.AddToClassList("metric-secondary");
        note.style.marginBottom = 8;
        step.Add(note);

        _platformMultiWarningLabel = new Label("");
        _platformMultiWarningLabel.AddToClassList("text-warning");
        _platformMultiWarningLabel.style.marginBottom = 8;
        _platformMultiWarningLabel.style.display = DisplayStyle.None;
        step.Add(_platformMultiWarningLabel);

        _platformListContainer = new VisualElement();
        _platformListContainer.AddToClassList("platform-list");
        step.Add(_platformListContainer);

        return step;
    }

    private VisualElement BuildStepHardwareDesign()
    {
        var step = new VisualElement();
        step.name = "step-hardware-design";

        var header = new Label("Design Hardware");
        header.AddToClassList("section-header");
        step.Add(header);

        var note = new Label("Select hardware tiers for each component. Higher tiers unlock more features but increase manufacturing and R&D costs.");
        note.AddToClassList("metric-secondary");
        note.style.marginBottom = 12;
        step.Add(note);

        step.Add(BuildTierSelector("Processing (CPU)", new[] { "Low", "Mid", "High", "Max" }, _processingTierButtons, OnProcessingTierClicked));
        step.Add(BuildTierSelector("Graphics (GPU)", new[] { "Low", "Mid", "High", "Max" }, _graphicsTierButtons, OnGraphicsTierClicked));
        step.Add(BuildTierSelector("Memory (RAM)", new[] { "Low", "Mid", "High" }, _memoryTierButtons, OnMemoryTierClicked));
        step.Add(BuildTierSelector("Storage", new[] { "Low", "Mid", "High" }, _storageTierButtons, OnStorageTierClicked));
        step.Add(BuildFormFactorSelector());

        var previewHeader = new Label("Hardware Preview");
        previewHeader.AddToClassList("section-header");
        previewHeader.style.marginTop = 16;
        step.Add(previewHeader);

        var previewRow = new VisualElement();
        previewRow.AddToClassList("flex-row");
        previewRow.style.flexWrap = Wrap.Wrap;
        previewRow.style.marginBottom = 8;

        _hwManufactureCostLabel = new Label();
        _hwManufactureCostLabel.AddToClassList("metric-secondary");
        _hwManufactureCostLabel.style.marginRight = 16;
        previewRow.Add(_hwManufactureCostLabel);

        _hwDevCostLabel = new Label();
        _hwDevCostLabel.AddToClassList("metric-secondary");
        _hwDevCostLabel.style.marginRight = 16;
        previewRow.Add(_hwDevCostLabel);

        _hwFeatureAvailLabel = new Label();
        _hwFeatureAvailLabel.AddToClassList("metric-secondary");
        previewRow.Add(_hwFeatureAvailLabel);

        step.Add(previewRow);

        _hwLockedFeaturesLabel = new Label();
        _hwLockedFeaturesLabel.AddToClassList("metric-secondary");
        _hwLockedFeaturesLabel.style.color = new UnityEngine.Color(1f, 0.6f, 0.2f);
        _hwLockedFeaturesLabel.style.display = DisplayStyle.None;
        step.Add(_hwLockedFeaturesLabel);

        return step;
    }

    private VisualElement BuildTierSelector(string label, string[] tierLabels, List<Button> buttonList, System.Action<int> onClick)
    {
        var container = new VisualElement();
        container.style.marginBottom = 10;

        var lbl = new Label(label);
        lbl.AddToClassList("metric-secondary");
        lbl.style.marginBottom = 4;
        container.Add(lbl);

        var row = new VisualElement();
        row.AddToClassList("flex-row");
        buttonList.Clear();

        for (int i = 0; i < tierLabels.Length; i++)
        {
            var btn = new Button();
            btn.text = tierLabels[i];
            btn.AddToClassList("btn-secondary");
            btn.style.marginRight = 6;
            int capturedIdx = i;
            btn.userData = capturedIdx;
            row.Add(btn);
            buttonList.Add(btn);
        }

        if (buttonList.Count > 0)
        {
            buttonList[0].AddToClassList("btn-primary");
            buttonList[0].RemoveFromClassList("btn-secondary");
        }

        for (int i = 0; i < buttonList.Count; i++)
        {
            var btn = buttonList[i];
            btn.RegisterCallback<ClickEvent>(evt => {
                var b = evt.currentTarget as Button;
                if (b == null) return;
                int idx = (int)b.userData;
                for (int k = 0; k < buttonList.Count; k++)
                {
                    if (k == idx) { buttonList[k].AddToClassList("btn-primary"); buttonList[k].RemoveFromClassList("btn-secondary"); }
                    else { buttonList[k].RemoveFromClassList("btn-primary"); buttonList[k].AddToClassList("btn-secondary"); }
                }
                onClick(idx);
            });
        }

        container.Add(row);
        return container;
    }

    private VisualElement BuildFormFactorSelector()
    {
        var container = new VisualElement();
        container.style.marginBottom = 10;

        var lbl = new Label("Form Factor");
        lbl.AddToClassList("metric-secondary");
        lbl.style.marginBottom = 4;
        container.Add(lbl);

        var row = new VisualElement();
        row.AddToClassList("flex-row");
        _formFactorButtons.Clear();

        string[] labels = { "Standard", "Portable", "Hybrid" };
        for (int i = 0; i < labels.Length; i++)
        {
            var btn = new Button();
            btn.text = labels[i];
            btn.AddToClassList("btn-secondary");
            btn.style.marginRight = 6;
            btn.userData = i;
            row.Add(btn);
            _formFactorButtons.Add(btn);
        }

        if (_formFactorButtons.Count > 0)
        {
            _formFactorButtons[0].AddToClassList("btn-primary");
            _formFactorButtons[0].RemoveFromClassList("btn-secondary");
        }

        for (int i = 0; i < _formFactorButtons.Count; i++)
        {
            _formFactorButtons[i].RegisterCallback<ClickEvent>(OnFormFactorButtonClickedEvt);
        }

        container.Add(row);
        return container;
    }

    private void OnProcessingTierClicked(int idx)
    {
        _viewModel?.SetProcessingTier((HardwareTier)idx);
        UpdateHardwarePreviewLabels();
    }

    private void OnGraphicsTierClicked(int idx)
    {
        _viewModel?.SetGraphicsTier((HardwareTier)idx);
        UpdateHardwarePreviewLabels();
    }

    private void OnMemoryTierClicked(int idx)
    {
        _viewModel?.SetMemoryTier((HardwareTier)idx);
        UpdateHardwarePreviewLabels();
    }

    private void OnStorageTierClicked(int idx)
    {
        _viewModel?.SetStorageTier((HardwareTier)idx);
        UpdateHardwarePreviewLabels();
    }

    private void OnFormFactorButtonClickedEvt(ClickEvent evt)
    {
        var btn = evt.currentTarget as Button;
        if (btn == null || _viewModel == null) return;
        int idx = (int)btn.userData;
        for (int k = 0; k < _formFactorButtons.Count; k++)
        {
            if (k == idx) { _formFactorButtons[k].AddToClassList("btn-primary"); _formFactorButtons[k].RemoveFromClassList("btn-secondary"); }
            else { _formFactorButtons[k].RemoveFromClassList("btn-primary"); _formFactorButtons[k].AddToClassList("btn-secondary"); }
        }
        _viewModel.SetFormFactor((ConsoleFormFactor)idx);
        UpdateHardwarePreviewLabels();
    }

    private void UpdateHardwarePreviewLabels()
    {
        if (_viewModel == null) return;
        if (_hwManufactureCostLabel != null)
            _hwManufactureCostLabel.text = "Mfg Cost: " + UIFormatting.FormatMoney(_viewModel.ManufactureCostPerUnit) + "/unit";
        if (_hwDevCostLabel != null)
            _hwDevCostLabel.text = "R&D Add: " + UIFormatting.FormatMoney(_viewModel.HardwareDevCostAdd);
        if (_hwFeatureAvailLabel != null)
            _hwFeatureAvailLabel.text = "Features: " + _viewModel.AvailableFeatureCount + " unlocked / " + _viewModel.LockedFeatureCount + " locked";
        if (_hwLockedFeaturesLabel != null)
        {
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
        }
        UpdateCostLabel();
    }

    private VisualElement BuildStepToolSelection()    {
        var step = new VisualElement();
        step.name = "step-tool-selection";

        var header = new Label("Select Tools");
        header.AddToClassList("section-header");
        step.Add(header);

        var note = new Label("All tools are required. Select one option per category.");
        note.AddToClassList("metric-secondary");
        note.style.marginBottom = 8;
        step.Add(note);

        _toolCategoryContainer = new VisualElement();
        _toolCategoryContainer.AddToClassList("tool-list");
        step.Add(_toolCategoryContainer);

        return step;
    }

    private VisualElement BuildStepGenreNiche()
    {
        var step = new VisualElement();
        step.name = "step-genre-niche";

        var header = new Label("Select Niche");
        header.AddToClassList("section-header");
        step.Add(header);

        _nicheListContainer = new VisualElement();
        _nicheListContainer.AddToClassList("niche-list");
        step.Add(_nicheListContainer);

        var genHeader = new Label("Architecture Generation");
        genHeader.AddToClassList("section-header");
        genHeader.style.marginTop = 16;
        step.Add(genHeader);

        _stanceContainer = new VisualElement();
        _stanceContainer.AddToClassList("flex-row");
        _stanceContainer.style.marginBottom = 8;

        _stanceStandardBtn = new Button { text = "Standard" };
        _stanceStandardBtn.AddToClassList("btn-secondary");
        _stanceStandardBtn.style.marginRight = 8;
        _stanceStandardBtn.clicked += OnStanceStandardClicked;
        _stanceContainer.Add(_stanceStandardBtn);

        _stanceCrossGenBtn = new Button { text = "Cross-Gen (+40-60% cost)" };
        _stanceCrossGenBtn.AddToClassList("btn-secondary");
        _stanceCrossGenBtn.clicked += OnStanceCrossGenClicked;
        _stanceContainer.Add(_stanceCrossGenBtn);
        step.Add(_stanceContainer);

        _crossGenCostWarningLabel = new Label("Cross-Gen: Forward compatible with next generation. Higher cost, blended affinities.");
        _crossGenCostWarningLabel.AddToClassList("metric-secondary");
        _crossGenCostWarningLabel.style.display = DisplayStyle.None;
        step.Add(_crossGenCostWarningLabel);

        var predHeader = new Label("Predecessor (optional)");
        predHeader.AddToClassList("section-header");
        predHeader.style.marginTop = 16;
        step.Add(predHeader);

        _predecessorContainer = new VisualElement();
        _predecessorContainer.AddToClassList("flex-row");
        _predecessorContainer.AddToClassList("align-center");

        _predecessorLabel = new Label("None selected");
        _predecessorLabel.AddToClassList("metric-primary");
        _predecessorLabel.style.flexGrow = 1;
        _predecessorContainer.Add(_predecessorLabel);

        step.Add(_predecessorContainer);

        return step;
    }

    private VisualElement BuildStepFeatures()
    {
        var step = new VisualElement();
        step.name = "step-features";

        var header = new Label("Select Features");
        header.AddToClassList("section-header");
        step.Add(header);

        // Summary bar
        _featureSummaryLabel = new Label("");
        _featureSummaryLabel.AddToClassList("metric-secondary");
        _featureSummaryLabel.style.marginBottom = 4;
        step.Add(_featureSummaryLabel);

        // Category tab bar (shown/hidden in Bind)
        _featureCategoryTabBar = BuildCategoryTabBar();
        step.Add(_featureCategoryTabBar);

        _filterBar = BuildFeatureFilterBar();
        step.Add(_filterBar);

        _featureContainer = new VisualElement();
        _featureContainer.AddToClassList("feature-grid");
        step.Add(_featureContainer);

        _missingExpectedWarningLabel = new Label("");
        _missingExpectedWarningLabel.AddToClassList("warning-text");
        _missingExpectedWarningLabel.style.display = DisplayStyle.None;
        step.Add(_missingExpectedWarningLabel);

        return step;
    }

    private VisualElement BuildStepPricingName()
    {
        var step = new VisualElement();
        step.name = "step-pricing-name";

        var header = new Label("Product Details");
        header.AddToClassList("section-header");
        step.Add(header);

        var nameLabel = new Label("Product Name");
        nameLabel.AddToClassList("metric-secondary");
        step.Add(nameLabel);

        _nameField = new TextField();
        _nameField.style.marginBottom = 12;
        _nameField.RegisterValueChangedCallback(OnNameFieldChanged);
        step.Add(_nameField);

        var pricingLabel = new Label("Pricing Model");
        pricingLabel.AddToClassList("metric-secondary");
        step.Add(pricingLabel);

        _subscriptionToggle = new Toggle("Subscription (monthly billing)");
        _subscriptionToggle.style.marginBottom = 8;
        _subscriptionToggle.RegisterValueChangedCallback(OnSubscriptionToggleChanged);
        step.Add(_subscriptionToggle);

        var priceLabel = new Label("Price ($)");
        priceLabel.AddToClassList("metric-secondary");
        step.Add(priceLabel);

        _priceField = new FloatField();
        _priceField.style.marginBottom = 4;
        _priceField.RegisterValueChangedCallback(OnPriceFieldChanged);
        step.Add(_priceField);

        _priceRangeLabel = new Label("");
        _priceRangeLabel.AddToClassList("metric-secondary");
        _priceRangeLabel.style.marginBottom = 4;
        step.Add(_priceRangeLabel);

        _sweetSpotLabel = new Label("");
        _sweetSpotLabel.AddToClassList("metric-secondary");
        _sweetSpotLabel.style.marginBottom = 4;
        step.Add(_sweetSpotLabel);

        _priceRatingLabel = new Label("");
        _priceRatingLabel.style.marginBottom = 4;
        _priceRatingLabel.style.display = DisplayStyle.None;
        step.Add(_priceRatingLabel);

        _priceWarningLabel = new Label("");
        _priceWarningLabel.AddToClassList("text-warning");
        _priceWarningLabel.style.display = DisplayStyle.None;
        step.Add(_priceWarningLabel);

        _manufactureInfoLabel = new Label("");
        _manufactureInfoLabel.AddToClassList("metric-secondary");
        _manufactureInfoLabel.style.marginTop = 8;
        _manufactureInfoLabel.style.display = DisplayStyle.None;
        step.Add(_manufactureInfoLabel);

        _marginLabel = new Label("");
        _marginLabel.AddToClassList("metric-secondary");
        _marginLabel.style.display = DisplayStyle.None;
        step.Add(_marginLabel);

        _lossLeaderLabel = new Label("\u26a0 Selling below manufacturing cost — loss-leader pricing");
        _lossLeaderLabel.AddToClassList("text-warning");
        _lossLeaderLabel.style.display = DisplayStyle.None;
        step.Add(_lossLeaderLabel);

        // Distribution model (tools and platforms only)
        _distributionContainer = new VisualElement();
        _distributionContainer.style.marginTop = 16;
        _distributionContainer.style.display = DisplayStyle.None;

        var distHeader = new Label("Distribution Model");
        distHeader.AddToClassList("section-header");
        _distributionContainer.Add(distHeader);

        var distNote = new Label("How competitors can use this product.");
        distNote.AddToClassList("metric-secondary");
        distNote.style.marginBottom = 8;
        _distributionContainer.Add(distNote);

        var distBtnRow = new VisualElement();
        distBtnRow.AddToClassList("flex-row");
        distBtnRow.style.marginBottom = 8;

        _distributionProprietaryBtn = new Button { text = "Proprietary" };
        _distributionProprietaryBtn.AddToClassList("btn-primary");
        _distributionProprietaryBtn.style.marginRight = 6;
        _distributionProprietaryBtn.clicked += OnDistributionProprietaryClicked;
        distBtnRow.Add(_distributionProprietaryBtn);

        _distributionLicensedBtn = new Button { text = "Licensed" };
        _distributionLicensedBtn.AddToClassList("btn-secondary");
        _distributionLicensedBtn.style.marginRight = 6;
        _distributionLicensedBtn.clicked += OnDistributionLicensedClicked;
        distBtnRow.Add(_distributionLicensedBtn);

        _distributionOpenSourceBtn = new Button { text = "Open Source" };
        _distributionOpenSourceBtn.AddToClassList("btn-secondary");
        _distributionOpenSourceBtn.clicked += OnDistributionOpenSourceClicked;
        distBtnRow.Add(_distributionOpenSourceBtn);

        _distributionContainer.Add(distBtnRow);

        var royaltyRow = new VisualElement();
        royaltyRow.AddToClassList("flex-row");
        royaltyRow.style.alignItems = Align.Center;
        royaltyRow.style.marginBottom = 4;
        royaltyRow.style.display = DisplayStyle.None;

        var royaltyLabel = new Label("Royalty Rate:");
        royaltyLabel.AddToClassList("metric-secondary");
        royaltyLabel.style.marginRight = 8;
        royaltyRow.Add(royaltyLabel);

        _royaltySlider = new SliderInt(5, 30);
        _royaltySlider.SetValueWithoutNotify(10);
        _royaltySlider.style.flexGrow = 1;
        _royaltySlider.RegisterValueChangedCallback(OnRoyaltySliderChanged);
        royaltyRow.Add(_royaltySlider);

        _royaltyValueLabel = new Label("10%");
        _royaltyValueLabel.AddToClassList("metric-primary");
        _royaltyValueLabel.style.marginLeft = 8;
        royaltyRow.Add(_royaltyValueLabel);

        _distributionContainer.Add(royaltyRow);

        _subscriptionPriceContainer = new VisualElement();
        _subscriptionPriceContainer.AddToClassList("flex-row");
        _subscriptionPriceContainer.style.alignItems = Align.Center;
        _subscriptionPriceContainer.style.marginBottom = 4;
        _subscriptionPriceContainer.style.display = DisplayStyle.None;

        var subPriceLabel = new Label("Subscription Price:");
        subPriceLabel.AddToClassList("metric-secondary");
        subPriceLabel.style.marginRight = 8;
        _subscriptionPriceContainer.Add(subPriceLabel);

        _subscriptionPriceSlider = new SliderInt(5, 100);
        _subscriptionPriceSlider.SetValueWithoutNotify(20);
        _subscriptionPriceSlider.style.flexGrow = 1;
        _subscriptionPriceSlider.RegisterValueChangedCallback(OnSubscriptionPriceSliderChanged);
        _subscriptionPriceContainer.Add(_subscriptionPriceSlider);

        _subscriptionPriceValueLabel = new Label("$20/month");
        _subscriptionPriceValueLabel.AddToClassList("metric-primary");
        _subscriptionPriceValueLabel.style.marginLeft = 8;
        _subscriptionPriceContainer.Add(_subscriptionPriceValueLabel);

        _distributionContainer.Add(_subscriptionPriceContainer);
        step.Add(_distributionContainer);

        return step;
    }

    private VisualElement BuildStepTeamAssignment()
    {
        var step = new VisualElement();
        step.name = "step-team-assignment";

        var header = new Label("Assign Teams (Optional)");
        header.AddToClassList("section-header");
        step.Add(header);

        var note = new Label("You can skip and assign teams after creation. All roles can be assigned from the Products window.");
        note.AddToClassList("metric-secondary");
        note.style.marginBottom = 12;
        step.Add(note);

        _teamRoleContainer = new VisualElement();
        step.Add(_teamRoleContainer);

        _completionLabel = new Label("");
        _completionLabel.AddToClassList("metric-secondary");
        _completionLabel.style.marginTop = 12;
        step.Add(_completionLabel);

        _salaryCostLabel = new Label("");
        _salaryCostLabel.AddToClassList("metric-secondary");
        _salaryCostLabel.style.marginTop = 4;
        step.Add(_salaryCostLabel);

        var releaseDivider = new VisualElement();
        releaseDivider.AddToClassList("divider");
        releaseDivider.style.marginTop = 12;
        releaseDivider.style.marginBottom = 12;
        step.Add(releaseDivider);

        var releaseDateHeader = new Label("Target Release Date");
        releaseDateHeader.AddToClassList("metric-secondary");
        step.Add(releaseDateHeader);

        _releaseDateValueLabel = new Label("Not set");
        _releaseDateValueLabel.AddToClassList("metric-primary");
        _releaseDateValueLabel.AddToClassList("text-accent");
        _releaseDateValueLabel.style.marginBottom = 4;
        step.Add(_releaseDateValueLabel);

        _releaseDateSlider = new SliderInt(30, 730);
        _releaseDateSlider.SetValueWithoutNotify(180);
        _releaseDateSlider.style.marginBottom = 4;
        _releaseDateSlider.RegisterValueChangedCallback(OnReleaseDateSliderChanged);
        step.Add(_releaseDateSlider);

        _releaseDateHintLabel = new Label("Products auto-ship on this date. Changing the date later incurs penalties.");
        _releaseDateHintLabel.AddToClassList("metric-secondary");
        _releaseDateHintLabel.style.marginBottom = 4;
        step.Add(_releaseDateHintLabel);

        return step;
    }

    // ── Named event handlers ───────────────────────────────────────────────────

    private void OnReleaseDateSliderChanged(ChangeEvent<int> evt)
    {
        if (_viewModel == null || _viewModel.CurrentStep != CreateProductModalViewModel.WizardStep.TeamAssignment) return;
        _viewModel.SetTargetDay(_viewModel.LastKnownCurrentDay + evt.newValue);
        if (_releaseDateValueLabel != null)
            _releaseDateValueLabel.text = _viewModel.ReleaseDateDisplay;
        UpdateFooterButtons();
    }

    private void OnDistributionProprietaryClicked()
    {
        _viewModel?.SetDistributionModel(ToolDistributionModel.Proprietary);
        UpdateDistributionButtonStates();
    }

    private void OnDistributionLicensedClicked()
    {
        _viewModel?.SetDistributionModel(ToolDistributionModel.Licensed);
        UpdateDistributionButtonStates();
    }

    private void OnDistributionOpenSourceClicked()
    {
        _viewModel?.SetDistributionModel(ToolDistributionModel.OpenSource);
        UpdateDistributionButtonStates();
    }

    private void OnRoyaltySliderChanged(ChangeEvent<int> evt)
    {
        _viewModel?.SetLicensingRate(evt.newValue / 100f);
        if (_royaltyValueLabel != null)
            _royaltyValueLabel.text = evt.newValue + "%";
    }

    private void OnSubscriptionPriceSliderChanged(ChangeEvent<int> evt)
    {
        _viewModel?.SetSubscriptionPrice(evt.newValue);
        if (_subscriptionPriceValueLabel != null)
            _subscriptionPriceValueLabel.text = _viewModel?.SubscriptionPriceLabel ?? ("$" + evt.newValue + "/month");
    }

    private void UpdateDistributionButtonStates()
    {
        if (_viewModel == null) return;
        bool isProprietary = _viewModel.SelectedDistribution == ToolDistributionModel.Proprietary;
        bool isLicensed = _viewModel.SelectedDistribution == ToolDistributionModel.Licensed;
        bool isOpenSource = _viewModel.SelectedDistribution == ToolDistributionModel.OpenSource;
        if (_distributionProprietaryBtn != null) {
            if (isProprietary) { _distributionProprietaryBtn.AddToClassList("btn-primary"); _distributionProprietaryBtn.RemoveFromClassList("btn-secondary"); }
            else { _distributionProprietaryBtn.RemoveFromClassList("btn-primary"); _distributionProprietaryBtn.AddToClassList("btn-secondary"); }
        }
        if (_distributionLicensedBtn != null) {
            if (isLicensed) { _distributionLicensedBtn.AddToClassList("btn-primary"); _distributionLicensedBtn.RemoveFromClassList("btn-secondary"); }
            else { _distributionLicensedBtn.RemoveFromClassList("btn-primary"); _distributionLicensedBtn.AddToClassList("btn-secondary"); }
        }
        if (_distributionOpenSourceBtn != null) {
            if (isOpenSource) { _distributionOpenSourceBtn.AddToClassList("btn-primary"); _distributionOpenSourceBtn.RemoveFromClassList("btn-secondary"); }
            else { _distributionOpenSourceBtn.RemoveFromClassList("btn-primary"); _distributionOpenSourceBtn.AddToClassList("btn-secondary"); }
        }
        if (_royaltySlider != null) {
            var row = _royaltySlider.parent;
            if (row != null) row.style.display = isLicensed ? DisplayStyle.Flex : DisplayStyle.None;
        }
        if (_subscriptionPriceContainer != null) {
            bool showSubPrice = isLicensed && _viewModel != null && _viewModel.ShowSubscriptionPricing;
            _subscriptionPriceContainer.style.display = showSubPrice ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }

    private void BindReleaseDatePicker()
    {
        if (_viewModel == null) return;
        int min = _viewModel.MinReleaseDayFromNow;
        int max = _viewModel.MaxReleaseDayFromNow;
        if (_releaseDateSlider != null)
        {
            _releaseDateSlider.lowValue = min;
            _releaseDateSlider.highValue = max;
            int estimatedOffset = _viewModel.EstimatedDevDays;
            int currentOffset = _viewModel.SelectedTargetDay > 0 && _viewModel.LastKnownCurrentDay > 0
                ? _viewModel.SelectedTargetDay - _viewModel.LastKnownCurrentDay
                : (estimatedOffset > min ? estimatedOffset : min);
            if (currentOffset < min) currentOffset = min;
            if (currentOffset > max) currentOffset = max;
            _releaseDateSlider.SetValueWithoutNotify(currentOffset);
            if (_viewModel.SelectedTargetDay <= 0) {
                int defaultOffset = _viewModel.EstimatedDevDays > min ? _viewModel.EstimatedDevDays : min;
                _viewModel.SetTargetDay(_viewModel.LastKnownCurrentDay + defaultOffset);
            }
        }
        if (_releaseDateValueLabel != null)
            _releaseDateValueLabel.text = string.IsNullOrEmpty(_viewModel.ReleaseDateDisplay) ? "Not set" : _viewModel.ReleaseDateDisplay;
    }

    private void OnNameFieldChanged(ChangeEvent<string> evt)
    {
        _viewModel?.SetProductName(evt.newValue);
        UpdateFooterButtons();
    }

    private void OnSubscriptionToggleChanged(ChangeEvent<bool> evt)
    {
        _viewModel?.SetPricingModel(evt.newValue);
        if (_priceField != null) _priceField.SetValueWithoutNotify(_viewModel.Price);
        UpdatePriceRangeHint();
        UpdatePriceWarningLabel();
    }

    private void OnPriceFieldChanged(ChangeEvent<float> evt)
    {
        _viewModel?.SetPrice(evt.newValue);
        UpdatePriceRangeHint();
        UpdatePriceWarningLabel();
        UpdateConsolePricingInfo();
        UpdateFooterButtons();
    }

    private void OnStanceStandardClicked()
    {
        _viewModel?.SetGenerationStance(GenerationStance.Standard);
        UpdateStanceButtons();
        UpdateCostLabel();
    }

    private void OnStanceCrossGenClicked()
    {
        _viewModel?.SetGenerationStance(GenerationStance.CrossGen);
        UpdateStanceButtons();
        UpdateCostLabel();
    }

    private void UpdateStanceButtons()
    {
        if (_viewModel == null) return;
        bool isCrossGen = _viewModel.SelectedStance == GenerationStance.CrossGen;
        if (_stanceStandardBtn != null)
        {
            if (!isCrossGen) _stanceStandardBtn.AddToClassList("btn-primary");
            else _stanceStandardBtn.RemoveFromClassList("btn-primary");
        }
        if (_stanceCrossGenBtn != null)
        {
            if (isCrossGen) _stanceCrossGenBtn.AddToClassList("btn-primary");
            else _stanceCrossGenBtn.RemoveFromClassList("btn-primary");
        }
        if (_crossGenCostWarningLabel != null)
            _crossGenCostWarningLabel.style.display = isCrossGen ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private VisualElement BuildCategoryTabBar()
    {
        var bar = new VisualElement();
        bar.AddToClassList("category-tab-bar");
        bar.style.flexDirection = FlexDirection.Row;
        bar.style.marginBottom = 8;
        _categoryTabButtons.Clear();

        var seen = _viewModel != null ? _viewModel.GetDistinctFeatureCategories() : new System.Collections.Generic.List<FeatureCategory>();

        for (int i = 0; i < seen.Count; i++)
        {
            var cat = seen[i];
            var btn = new Button();
            btn.text = cat.ToString();
            btn.AddToClassList("tab-btn");
            btn.userData = cat;
            btn.RegisterCallback<ClickEvent>(OnCategoryTabButtonClickedEvt);
            bar.Add(btn);
            _categoryTabButtons.Add(btn);
        }

        return bar;
    }

    private void OnCategoryTabButtonClickedEvt(ClickEvent evt)
    {
        var el = evt.currentTarget as Button;
        if (el == null || _viewModel == null || !(el.userData is FeatureCategory cat)) return;
        _viewModel.SelectCategory(cat);
        UpdateCategoryTabActiveState();
        RebuildFeatureToggles();
    }

    private void UpdateCategoryTabActiveState()
    {
        if (_viewModel == null) return;
        int count = _categoryTabButtons.Count;
        for (int i = 0; i < count; i++)
        {
            var btn = _categoryTabButtons[i];
            if (btn.userData is FeatureCategory cat && cat == _viewModel.SelectedCategory)
                btn.AddToClassList("tab-btn--active");
            else
                btn.RemoveFromClassList("tab-btn--active");
        }
    }
    // ── Bind ──────────────────────────────────────────────────────────────────

    public void Bind(IViewModel viewModel)
    {
        _viewModel = viewModel as CreateProductModalViewModel;
        if (_viewModel == null) return;

        if (_dispatcher is WindowManager wm && wm.GameController != null)
            _cachedDefinitions = wm.GameController.ProductTemplates ?? new ProductTemplateDefinition[0];

        if (_updateProductId.HasValue)
        {
            Product product = null;
            if (_dispatcher is WindowManager wmUpdate && wmUpdate.GameController != null)
            {
                var gs = wmUpdate.GameController.GetGameState();
                gs?.productState?.shippedProducts?.TryGetValue(_updateProductId.Value, out product);
            }

            if (product != null)
            {
                _viewModel.InitAsUpdate(product, _cachedDefinitions);
                if (_headerTitleLabel != null)
                    _headerTitleLabel.text = "Update \u2014 " + product.ProductName;
            }
        }
        else if (_sequelOfId.HasValue)
        {
            Product original = null;
            if (_dispatcher is WindowManager wmSequel && wmSequel.GameController != null)
            {
                var gs = wmSequel.GameController.GetGameState();
                if (gs?.productState != null)
                {
                    gs.productState.shippedProducts?.TryGetValue(_sequelOfId.Value, out original);
                    if (original == null)
                        gs.productState.archivedProducts?.TryGetValue(_sequelOfId.Value, out original);
                }
            }

            if (original != null)
            {
                _viewModel.InitAsSequel(original, _cachedDefinitions);
                if (_headerTitleLabel != null)
                    _headerTitleLabel.text = "Create Sequel \u2014 " + original.ProductName;
            }
        }
        else
        {
            _viewModel.SetTemplates(_cachedDefinitions);
        }

        _viewModel.OnStepChanged -= OnStepChanged;
        _viewModel.OnStepChanged += OnStepChanged;

        BindCurrentStep();
        ShowCurrentStep();
        UpdateFooterButtons();
        UpdateCostLabel();
    }

    public void Dispose()
    {
        if (_viewModel != null)
            _viewModel.OnStepChanged -= OnStepChanged;
        _viewModel = null;
        _templatePool = null;
        _salaryCostLabel = null;
    }

    // ── Step rendering ────────────────────────────────────────────────────────

    private void ShowCurrentStep()
    {
        if (_viewModel == null) return;
        var step = _viewModel.CurrentStep;

        _stepProductType.style.display    = step == CreateProductModalViewModel.WizardStep.ProductType    ? DisplayStyle.Flex : DisplayStyle.None;
        _stepPlatformTarget.style.display = step == CreateProductModalViewModel.WizardStep.PlatformTarget ? DisplayStyle.Flex : DisplayStyle.None;
        _stepHardwareDesign.style.display = step == CreateProductModalViewModel.WizardStep.HardwareDesign ? DisplayStyle.Flex : DisplayStyle.None;
        _stepToolSelection.style.display  = step == CreateProductModalViewModel.WizardStep.ToolSelection  ? DisplayStyle.Flex : DisplayStyle.None;
        _stepGenreNiche.style.display     = step == CreateProductModalViewModel.WizardStep.GenreNiche     ? DisplayStyle.Flex : DisplayStyle.None;
        _stepFeatures.style.display       = step == CreateProductModalViewModel.WizardStep.Features       ? DisplayStyle.Flex : DisplayStyle.None;
        _stepPricingName.style.display    = step == CreateProductModalViewModel.WizardStep.PricingName    ? DisplayStyle.Flex : DisplayStyle.None;
        _stepTeamAssignment.style.display = step == CreateProductModalViewModel.WizardStep.TeamAssignment ? DisplayStyle.Flex : DisplayStyle.None;

        if (step == CreateProductModalViewModel.WizardStep.ProductType && _viewModel.IsUpdateMode)
        {
            if (_templateContainer?.parent != null)
                _templateContainer.parent.style.display = DisplayStyle.None;
            if (_updateTypeContainer != null)
                _updateTypeContainer.style.display = DisplayStyle.Flex;
        }
        else if (!_viewModel.IsUpdateMode)
        {
            if (_templateContainer?.parent != null)
                _templateContainer.parent.style.display = DisplayStyle.Flex;
            if (_updateTypeContainer != null)
                _updateTypeContainer.style.display = DisplayStyle.None;
        }

        if (_subscriptionToggle != null)
            _subscriptionToggle.style.display = (_viewModel.IsSequelMode && step == CreateProductModalViewModel.WizardStep.PricingName)
                ? DisplayStyle.None
                : DisplayStyle.Flex;

        int stepDisplay = _viewModel.GetCurrentStepDisplay();
        int total = _viewModel.TotalSteps;
        if (_stepIndicatorLabel != null)
            _stepIndicatorLabel.text = "Step " + stepDisplay + " of " + total;
    }

    private void BindCurrentStep()
    {
        if (_viewModel == null) return;

        switch (_viewModel.CurrentStep)
        {
            case CreateProductModalViewModel.WizardStep.ProductType:
                if (_viewModel.IsUpdateMode)
                    RebuildUpdateTypeCards();
                else
                {
                    _templatePool?.UpdateList(_viewModel.Templates, BindTemplateItem);
                    InsertCategoryHeaders();
                }
                break;

            case CreateProductModalViewModel.WizardStep.PlatformTarget:
                PopulatePlatformsFromGameState();
                BindPlatformTargetStep();
                break;

            case CreateProductModalViewModel.WizardStep.HardwareDesign:
                PopulateHardwareGenConfigFromGameState();
                UpdateHardwareTierButtonStates();
                UpdateHardwarePreviewLabels();
                break;

            case CreateProductModalViewModel.WizardStep.ToolSelection:
                PopulateToolsFromGameState();
                BindToolSelectionStep();
                break;

            case CreateProductModalViewModel.WizardStep.GenreNiche:
                BindGenreNicheStep();
                break;

            case CreateProductModalViewModel.WizardStep.Features:
                RebuildFeatureToggles();
                UpdateFeatureSummaryBar();
                UpdateCategoryTabVisibility();
                UpdateCategoryTabActiveState();
                break;

            case CreateProductModalViewModel.WizardStep.PricingName:
                if (_nameField != null) _nameField.SetValueWithoutNotify(_viewModel.ProductName ?? "");
                if (_subscriptionToggle != null) _subscriptionToggle.SetValueWithoutNotify(_viewModel.IsSubscriptionBased);
                if (_priceField != null) _priceField.SetValueWithoutNotify(_viewModel.Price);
                UpdatePriceRangeHint();
                UpdatePriceWarningLabel();
                UpdateConsolePricingInfo();
                if (_distributionContainer != null)
                    _distributionContainer.style.display = _viewModel.CanSetDistribution ? DisplayStyle.Flex : DisplayStyle.None;
                if (_viewModel.CanSetDistribution) {
                    UpdateDistributionButtonStates();
                    if (_royaltySlider != null)
                        _royaltySlider.SetValueWithoutNotify((int)(_viewModel.SelectedLicensingRate * 100f));
                    if (_royaltyValueLabel != null)
                        _royaltyValueLabel.text = ((int)(_viewModel.SelectedLicensingRate * 100f)) + "%";
                    if (_subscriptionPriceSlider != null)
                        _subscriptionPriceSlider.SetValueWithoutNotify((int)_viewModel.SubscriptionPrice);
                    if (_subscriptionPriceValueLabel != null)
                        _subscriptionPriceValueLabel.text = _viewModel.SubscriptionPriceLabel;
                }
                break;

            case CreateProductModalViewModel.WizardStep.TeamAssignment:
                RebuildTeamRoleAssignments();
                _viewModel.RecalculateMinReleaseDay();
                UpdateCompletionLabel();
                BindReleaseDatePicker();
                break;
        }
    }

    // ── State population helpers ───────────────────────────────────────────────

    private void PopulatePlatformsFromGameState()
    {
        if (_viewModel == null) return;
        if (!(_dispatcher is WindowManager wm) || wm.GameController == null) return;

        var gc = wm.GameController;
        var gs = gc.GetGameState();
        if (gs == null) return;

        // Find the template to get validTargetPlatforms
        ProductTemplateDefinition template = FindSelectedTemplate(gc);
        ProductCategory[] validPlatformCategories = template?.validTargetPlatforms;

        // Collect player-owned shipped platforms
        var ownedPlatforms = new System.Collections.Generic.Dictionary<ProductId, Product>();
        var competitorPlatforms = new System.Collections.Generic.Dictionary<ProductId, Product>();

        if (gs.productState?.shippedProducts != null)
        {
            foreach (var kvp in gs.productState.shippedProducts)
            {
                if (kvp.Value.IsOnMarket && kvp.Value.Category.IsPlatform() && !kvp.Value.IsCompetitorProduct)
                    ownedPlatforms[kvp.Key] = kvp.Value;
            }
        }

        if (gs.competitorState?.competitors != null)
        {
            foreach (var compKvp in gs.competitorState.competitors)
            {
                var comp = compKvp.Value;
                if (comp.IsBankrupt || comp.IsAbsorbed || comp.ActiveProductIds == null) continue;
                int count = comp.ActiveProductIds.Count;
                for (int i = 0; i < count; i++)
                {
                    var pid = comp.ActiveProductIds[i];
                    if (!gs.productState.shippedProducts.TryGetValue(pid, out var p)) continue;
                    if (p.IsOnMarket && p.Category.IsPlatform())
                        competitorPlatforms[pid] = p;
                }
            }
        }

        // Enrich display entries with live market share from PlatformSystem
        var platformSystem = gc.PlatformSystem;
        EnrichPlatformDisplaysWithMarketShare(ownedPlatforms, competitorPlatforms, platformSystem);

        _viewModel.PopulatePlatformsFromState(ownedPlatforms, competitorPlatforms, validPlatformCategories, template?.validPlatformNiches, true);

        if (_dispatcher is WindowManager wmGatePlat && wmGatePlat.GameController != null)
            _viewModel.SetGateConfig(wmGatePlat.GameController.CrossProductGateConfig, wmGatePlat.GameController.ProductTemplates);
    }

    private void EnrichPlatformDisplaysWithMarketShare(
        System.Collections.Generic.Dictionary<ProductId, Product> owned,
        System.Collections.Generic.Dictionary<ProductId, Product> competitor,
        PlatformSystem ps)
    {
        // Market share data is stored in PlatformSystem — after PopulatePlatformsFromState
        // the AvailablePlatforms list has entries with MarketSharePercent = 0f.
        // We patch them here before BindPlatformTargetStep renders them.
        if (ps == null) return;

        var platforms = _viewModel?.AvailablePlatforms;
        if (platforms == null) return;

        for (int i = 0; i < platforms.Count; i++)
        {
            var display = platforms[i];
            var entry = ps.GetPlatformEntry(display.PlatformId);
            display.MarketSharePercent = entry.MarketSharePercent;
            platforms[i] = display;
        }
    }

    private void PopulateToolsFromGameState()
    {
        if (_viewModel == null) return;
        if (!(_dispatcher is WindowManager wm) || wm.GameController == null) return;

        var gc = wm.GameController;
        var gs = gc.GetGameState();
        if (gs == null) return;

        ProductTemplateDefinition template = FindSelectedTemplate(gc);
        ProductCategory[] requiredToolTypes = template?.requiredToolTypes;

        var ownedTools = new System.Collections.Generic.Dictionary<ProductId, Product>();
        var competitorTools = new System.Collections.Generic.Dictionary<ProductId, Product>();

        if (gs.productState?.shippedProducts != null)
        {
            foreach (var kvp in gs.productState.shippedProducts)
            {
                if (kvp.Value.IsOnMarket && kvp.Value.Category.IsTool() && !kvp.Value.IsCompetitorProduct)
                    ownedTools[kvp.Key] = kvp.Value;
            }
        }

        if (gs.competitorState?.competitors != null)
        {
            foreach (var compKvp in gs.competitorState.competitors)
            {
                var comp = compKvp.Value;
                if (comp.IsBankrupt || comp.IsAbsorbed || comp.ActiveProductIds == null) continue;
                int count = comp.ActiveProductIds.Count;
                for (int i = 0; i < count; i++)
                {
                    var pid = comp.ActiveProductIds[i];
                    if (!gs.productState.shippedProducts.TryGetValue(pid, out var p)) continue;
                    if (p.IsOnMarket && p.Category.IsTool())
                        competitorTools[pid] = p;
                }
            }
        }

        _viewModel.PopulateToolsFromState(ownedTools, competitorTools, requiredToolTypes);

        if (_dispatcher is WindowManager wmGate && wmGate.GameController != null)
            _viewModel.SetGateConfig(wmGate.GameController.CrossProductGateConfig, wmGate.GameController.ProductTemplates);
    }

    private ProductTemplateDefinition FindSelectedTemplate(GameController gc)
    {
        if (_viewModel == null || string.IsNullOrEmpty(_viewModel.SelectedTemplateId)) return null;
        var templates = gc.ProductTemplates;
        if (templates == null) return null;
        for (int i = 0; i < templates.Length; i++)
        {
            if (templates[i] != null && templates[i].templateId == _viewModel.SelectedTemplateId)
                return templates[i];
        }
        return null;
    }

    private void PopulateHardwareGenConfigFromGameState()
    {
        if (_viewModel == null) return;
        if (!(_dispatcher is WindowManager wm) || wm.GameController == null) return;
        var gc = wm.GameController;
        int currentGen = gc.GenerationSystem?.GetCurrentGeneration() ?? 1;
        var configs = gc.HardwareGenerationConfigs;
        if (configs == null || configs.Length == 0) return;
        HardwareGenerationConfig found = null;
        for (int i = 0; i < configs.Length; i++)
        {
            if (configs[i] != null && configs[i].generation == currentGen) { found = configs[i]; break; }
        }
        if (found == null)
        {
            for (int i = 0; i < configs.Length; i++)
            {
                if (configs[i] != null && configs[i].generation <= currentGen)
                {
                    if (found == null || configs[i].generation > found.generation)
                        found = configs[i];
                }
            }
        }
        if (found == null) found = configs[0];
        _viewModel.SetHardwareGenConfig(found);
    }

    private void UpdateHardwareTierButtonStates()
    {
        if (_viewModel == null) return;
        var hw = _viewModel.HardwareConfig;
        UpdateTierButtonActiveState(_processingTierButtons, (int)hw.processingTier);
        UpdateTierButtonActiveState(_graphicsTierButtons, (int)hw.graphicsTier);
        UpdateTierButtonActiveState(_memoryTierButtons, (int)hw.memoryTier);
        UpdateTierButtonActiveState(_storageTierButtons, (int)hw.storageTier);
        UpdateTierButtonActiveState(_formFactorButtons, (int)hw.formFactor);
    }

    private static void UpdateTierButtonActiveState(List<Button> buttons, int activeIdx)
    {
        for (int k = 0; k < buttons.Count; k++)
        {
            if (k == activeIdx) { buttons[k].AddToClassList("btn-primary"); buttons[k].RemoveFromClassList("btn-secondary"); }
            else { buttons[k].RemoveFromClassList("btn-primary"); buttons[k].AddToClassList("btn-secondary"); }
        }
    }

    private void UpdateConsolePricingInfo()
    {
        if (_viewModel == null) return;
        bool isConsole = _viewModel.IsConsoleTemplate;

        if (_manufactureInfoLabel != null)
        {
            _manufactureInfoLabel.style.display = isConsole ? DisplayStyle.Flex : DisplayStyle.None;
            if (isConsole)
                _manufactureInfoLabel.text = "Manufacturing cost: " + UIFormatting.FormatMoney(_viewModel.ManufactureCostPerUnit) + " / unit";
        }
        if (_marginLabel != null)
        {
            _marginLabel.style.display = isConsole && _viewModel.Price > 0f ? DisplayStyle.Flex : DisplayStyle.None;
            if (isConsole && _viewModel.Price > 0f)
            {
                float margin = _viewModel.MarginPerUnit;
                string sign = margin >= 0f ? "+" : "";
                _marginLabel.text = "Margin per unit: " + sign + UIFormatting.FormatMoney((long)margin);
            }
        }
        if (_lossLeaderLabel != null)
            _lossLeaderLabel.style.display = _viewModel.IsBelowManufactureCost ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private void OnStepChanged()
    {
        ShowCurrentStep();
        BindCurrentStep();
        UpdateFooterButtons();
        UpdateCostLabel();
    }

    // ── Step 1 — Templates ─────────────────────────────────────────────────────

    // ── Step 2 — Platform Target ───────────────────────────────────────────────

    private void BindPlatformTargetStep()
    {
        if (_platformListContainer == null || _viewModel == null) return;

        if (_tooltipProvider != null) {
            int clearCount = _platformListContainer.childCount;
            for (int i = 0; i < clearCount; i++)
                _platformListContainer[i].ClearTooltip(_tooltipProvider.TooltipService);
        }
        _platformListContainer.Clear();

        var platforms = _viewModel.AvailablePlatforms;
        int count = platforms.Count;
        string lastType = "";
        for (int i = 0; i < count; i++)
        {
            var platform = platforms[i];
            if (platform.PlatformTypeLabel != lastType)
            {
                var header = new Label(platform.PlatformTypeLabel);
                header.AddToClassList("platform-group-header");
                _platformListContainer.Add(header);
                lastType = platform.PlatformTypeLabel;
            }
            var row = CreatePlatformRow(platform);
            _platformListContainer.Add(row);
        }

        bool showWarning = _viewModel.IsMultiPlatformWarningVisible;
        if (_platformMultiWarningLabel != null)
        {
            _platformMultiWarningLabel.text = _viewModel.MultiPlatformWarningLabel;
            _platformMultiWarningLabel.style.display = showWarning ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }

    private VisualElement CreatePlatformRow(PlatformTargetDisplay platform)
    {
        var row = new VisualElement();
        row.AddToClassList("platform-row");
        row.style.flexDirection = FlexDirection.Row;
        row.style.alignItems = Align.Center;
        row.style.marginBottom = 6;

        bool isSelected = _viewModel != null && _viewModel.IsPlatformSelected(platform.PlatformId);

        var toggle = new Toggle();
        toggle.SetValueWithoutNotify(isSelected);
        toggle.userData = platform;
        toggle.RegisterValueChangedCallback(OnPlatformToggleChanged);
        row.Add(toggle);

        var info = new VisualElement();
        info.style.flexGrow = 1;
        info.style.marginLeft = 8;

        var nameLbl = new Label(platform.DisplayName);
        nameLbl.AddToClassList(platform.IsPlayerOwned ? "text-accent" : "metric-primary");
        info.Add(nameLbl);

        var detailLbl = new Label(platform.PlatformTypeLabel + "  |  " + platform.OwnerLabel + "  |  Users: " + UIFormatting.FormatUserCount(platform.ActiveUsers) + "  |  " + platform.LicensingCostLabel);
        detailLbl.AddToClassList("metric-secondary");
        info.Add(detailLbl);

        row.Add(info);

        if (isSelected) row.AddToClassList("platform-row--selected");

        if (_viewModel != null && _tooltipProvider != null) {
            var tooltipData = _viewModel.BuildPlatformCapabilityTooltip(platform.PlatformId);
            row.SetRichTooltip(tooltipData, _tooltipProvider.TooltipService);
        }

        return row;
    }

    private void OnPlatformToggleChanged(ChangeEvent<bool> evt)
    {
        var toggle = evt.currentTarget as Toggle;
        if (toggle?.userData is PlatformTargetDisplay platform && _viewModel != null)
        {
            _viewModel.TogglePlatform(platform.PlatformId, evt.newValue);
            BindPlatformTargetStep();
            UpdateFooterButtons();
            UpdateCostLabel();
        }
    }

    // ── Step 3 — Tool Selection ────────────────────────────────────────────────

    private void BindToolSelectionStep()
    {
        if (_viewModel == null) return;
        if (_toolCategoryContainer == null) return;

        if (_tooltipProvider != null) {
            int clearCount = _toolCategoryContainer.childCount;
            for (int i = 0; i < clearCount; i++)
                _toolCategoryContainer[i].Query(className: "tool-row").ForEach(row => row.ClearTooltip(_tooltipProvider.TooltipService));
        }
        _toolCategoryContainer.Clear();

        if (!_viewModel.HasRequiredTools) return;

        var tools = _viewModel.AvailableRequiredTools;
        var requiredCategories = _viewModel.RequiredToolCategories;
        if (requiredCategories == null) return;

        int catCount = requiredCategories.Count;
        for (int c = 0; c < catCount; c++)
        {
            var category = requiredCategories[c];

            var categorySection = new VisualElement();
            categorySection.style.marginBottom = 12;

            var catHeader = new Label("Required: " + category.ToString());
            catHeader.AddToClassList("metric-secondary");
            catHeader.style.marginBottom = 4;
            categorySection.Add(catHeader);

            bool hasAnyOption = false;
            int toolCount = tools.Count;
            for (int i = 0; i < toolCount; i++)
            {
                if (tools[i].Category != category) continue;
                categorySection.Add(CreateRequiredToolRow(tools[i]));
                hasAnyOption = true;
            }

            if (!hasAnyOption)
            {
                var blockingMsg = new Label("No " + category.ToString() + " available. Build one or wait for a competitor to release one.");
                blockingMsg.AddToClassList("text-warning");
                blockingMsg.style.marginTop = 4;
                categorySection.Add(blockingMsg);
            }

            _toolCategoryContainer.Add(categorySection);
        }
    }

    private VisualElement CreateRequiredToolRow(ToolSelectionDisplay tool)
    {
        var row = new VisualElement();
        row.AddToClassList("tool-row");
        row.style.flexDirection = FlexDirection.Row;
        row.style.alignItems = Align.Center;
        row.style.marginBottom = 4;

        bool isSelected = _viewModel != null && _viewModel.IsToolSelected(tool.Category, tool.ToolId);
        var toggle = new Toggle();
        toggle.SetValueWithoutNotify(isSelected);
        toggle.userData = tool;
        toggle.RegisterValueChangedCallback(OnRequiredToolToggleChanged);
        row.Add(toggle);

        var info = new VisualElement();
        info.style.flexGrow = 1;
        info.style.marginLeft = 8;
        var nameLbl = new Label(tool.DisplayName);
        nameLbl.AddToClassList(tool.IsPlayerOwned ? "text-accent" : "metric-primary");
        info.Add(nameLbl);
        var detailLbl = new Label(tool.OwnerLabel + "  |  Quality: " + tool.QualityScore.ToString("F0") + "  |  " + tool.QualitativeLabel + "  |  " + tool.LicensingCostLabel);
        detailLbl.AddToClassList("metric-secondary");
        info.Add(detailLbl);
        row.Add(info);

        if (_viewModel != null && _tooltipProvider != null) {
            var tooltipData = _viewModel.BuildToolCapabilityTooltip(tool.ToolId);
            row.SetRichTooltip(tooltipData, _tooltipProvider.TooltipService);
        }

        return row;
    }

    private void OnRequiredToolToggleChanged(ChangeEvent<bool> evt)
    {
        var toggle = evt.currentTarget as Toggle;
        if (toggle?.userData is ToolSelectionDisplay tool && _viewModel != null)
        {
            if (evt.newValue)
                _viewModel.SetToolSelection(tool.Category, tool.ToolId);
            else
                _viewModel.ClearToolSelection(tool.Category);
            BindToolSelectionStep();
            UpdateFooterButtons();
        }
    }

    // ── Step 4 — Genre / Niche ─────────────────────────────────────────────────

    private void BindGenreNicheStep()
    {
        if (_viewModel == null) return;

        if (_nicheListContainer != null)
        {
            _nicheListContainer.Clear();
            var niches = _viewModel.AvailableNiches;
            int count = niches.Count;
            for (int i = 0; i < count; i++)
                _nicheListContainer.Add(CreateNicheRow(niches[i]));
        }

        UpdateStanceButtons();

        if (_predecessorLabel != null)
        {
            if (_viewModel.SelectedPredecessorId.HasValue)
                _predecessorLabel.text = "Predecessor set";
            else
                _predecessorLabel.text = "None selected";
        }
    }

    private VisualElement CreateNicheRow(NicheOptionDisplay niche)
    {
        var row = new VisualElement();
        row.AddToClassList("niche-row");
        row.style.flexDirection = FlexDirection.Row;
        row.style.alignItems = Align.Center;
        row.style.marginBottom = 6;

        bool isSelected = _viewModel != null && _viewModel.SelectedNiche.HasValue
            && _viewModel.SelectedNiche.Value == niche.Niche;

        var toggle = new Toggle();
        toggle.SetValueWithoutNotify(isSelected);
        toggle.userData = niche;
        toggle.RegisterValueChangedCallback(OnNicheToggleChanged);
        row.Add(toggle);

        var info = new VisualElement();
        info.style.flexGrow = 1;
        info.style.marginLeft = 8;
        var nameLbl = new Label(niche.DisplayName);
        nameLbl.AddToClassList("metric-primary");
        info.Add(nameLbl);
        var detailLbl = new Label(niche.RetentionLabel + "  |  " + niche.VolatilityLabel);
        detailLbl.AddToClassList("metric-secondary");
        info.Add(detailLbl);
        row.Add(info);

        return row;
    }

    private void OnNicheToggleChanged(ChangeEvent<bool> evt)
    {
        var toggle = evt.currentTarget as Toggle;
        if (toggle?.userData is NicheOptionDisplay niche && _viewModel != null && evt.newValue)
        {
            _viewModel.SelectNiche(niche.Niche);
            BindGenreNicheStep();
        }
    }

    private void RebuildUpdateTypeCards()
    {
        if (_updateTypeContainer == null || _viewModel == null) return;
        _updateTypeContainer.Clear();

        BuildUpdateTypeCard(ProductUpdateType.BugFix, "Bug Fix",
            "Fix 60\u201390% of outstanding bugs. Improves stability and user retention.");
        BuildUpdateTypeCard(ProductUpdateType.AddFeatures, "Add Features",
            "Select new features to add to the product. Increases feature points and appeal.");
        BuildUpdateTypeCard(ProductUpdateType.RemoveFeature, "Remove Feature",
            "Drop an existing feature to reduce complexity or cut underperforming content.");
    }

    private void BuildUpdateTypeCard(ProductUpdateType updateType, string title, string description)
    {
        var card = CreateUpdateTypeItem();
        BindUpdateTypeItem(card, updateType, title, description);
        _updateTypeContainer.Add(card);
    }

    private VisualElement CreateUpdateTypeItem()
    {
        var item = new VisualElement();
        item.AddToClassList("template-item");

        var header = new VisualElement();
        header.AddToClassList("template-item__header");

        var nameLabel = new Label();
        nameLabel.name = "upd-name";
        nameLabel.AddToClassList("card__title");
        header.Add(nameLabel);
        item.Add(header);

        var descLabel = new Label();
        descLabel.name = "upd-desc";
        descLabel.AddToClassList("card__description");
        item.Add(descLabel);

        return item;
    }

    private void BindUpdateTypeItem(VisualElement el, ProductUpdateType updateType, string title, string description)
    {
        el.Q<Label>("upd-name").text = title;
        el.Q<Label>("upd-desc").text = description;

        bool isSelected = _viewModel != null && _viewModel.SelectedUpdateType == updateType && _viewModel.IsUpdateMode
            && el.ClassListContains("template-item--selected");

        el.UnregisterCallback<ClickEvent>(OnUpdateTypeCardClicked);
        el.userData = updateType;
        el.RegisterCallback<ClickEvent>(OnUpdateTypeCardClicked);
    }

    private void OnUpdateTypeCardClicked(ClickEvent evt)
    {
        var el = evt.currentTarget as VisualElement;
        if (el?.userData is ProductUpdateType updateType)
        {
            _viewModel?.SelectUpdateType(updateType);

            int childCount = _updateTypeContainer.childCount;
            for (int i = 0; i < childCount; i++)
            {
                var child = _updateTypeContainer[i];
                bool selected = child.userData is ProductUpdateType t && t == updateType;
                if (selected) child.AddToClassList("template-item--selected");
                else child.RemoveFromClassList("template-item--selected");
            }

            UpdateFooterButtons();
        }
    }

    private VisualElement CreateTemplateItem()
    {
        var item = new VisualElement();
        item.AddToClassList("template-item");

        // Header row: name + cost
        var header = new VisualElement();
        header.AddToClassList("template-item__header");

        var nameLabel = new Label();
        nameLabel.name = "tpl-name";
        nameLabel.AddToClassList("card__title");
        header.Add(nameLabel);

        var gameTag = new Label("Game");
        gameTag.name = "tpl-game-tag";
        gameTag.AddToClassList("badge");
        gameTag.AddToClassList("badge--accent");
        header.Add(gameTag);

        var costLabel = new Label();
        costLabel.name = "tpl-cost";
        costLabel.AddToClassList("metric-primary");
        costLabel.AddToClassList("text-accent");
        header.Add(costLabel);

        item.Add(header);

        // Description
        var descLabel = new Label();
        descLabel.name = "tpl-desc";
        descLabel.AddToClassList("card__description");
        item.Add(descLabel);

        // Phase pills row
        var pillsRow = new VisualElement();
        pillsRow.name = "tpl-pills";
        pillsRow.AddToClassList("template-item__phases");
        item.Add(pillsRow);

        return item;
    }

    private void BindTemplateItem(VisualElement el, ProductTemplateDisplay data)
    {
        el.Q<Label>("tpl-name").text = data.DisplayName;
        el.Q<Label>("tpl-cost").text = UIFormatting.FormatMoney(data.BaseUpfrontCost);

        var descLabel = el.Q<Label>("tpl-desc");
        if (descLabel != null)
        {
            descLabel.text = data.Description ?? "";
            descLabel.style.display = string.IsNullOrEmpty(data.Description) ? DisplayStyle.None : DisplayStyle.Flex;
        }

        var gameTag = el.Q<Label>("tpl-game-tag");
        if (gameTag != null)
            gameTag.style.display = DisplayStyle.None;

        var pillsRow = el.Q<VisualElement>("tpl-pills");
        if (pillsRow != null)
        {
            pillsRow.Clear();
            if (data.PhasePills != null)
            {
                for (int p = 0; p < data.PhasePills.Length; p++)
                {
                    var pill = new Label(data.PhasePills[p]);
                    pill.AddToClassList("phase-pill");
                    pillsRow.Add(pill);
                }
            }
        }

        bool isSelected = data.TemplateId == _viewModel?.SelectedTemplateId;
        if (isSelected) el.AddToClassList("template-item--selected");
        else el.RemoveFromClassList("template-item--selected");

        el.UnregisterCallback<ClickEvent>(OnTemplateItemClicked);
        el.userData = data.TemplateId;
        el.RegisterCallback<ClickEvent>(OnTemplateItemClicked);
    }

    private void OnTemplateItemClicked(ClickEvent evt)
    {
        var el = evt.currentTarget as VisualElement;
        if (el?.userData is string templateId)
        {
            _viewModel?.SelectTemplate(templateId, _cachedDefinitions);

            int childCount = _templateContainer.childCount;
            for (int i = 0; i < childCount; i++)
            {
                var child = _templateContainer[i];
                bool selected = child.userData is string id && id == templateId;
                if (selected) child.AddToClassList("template-item--selected");
                else child.RemoveFromClassList("template-item--selected");
            }

            UpdateFooterButtons();
            UpdateCostLabel();
        }
    }

    private void InsertCategoryHeaders()
    {
        if (_viewModel == null || _templateContainer == null) return;

        // Remove existing category headers
        var toRemove = new System.Collections.Generic.List<VisualElement>();
        int childCount = _templateContainer.childCount;
        for (int i = 0; i < childCount; i++)
        {
            var child = _templateContainer[i];
            if (child.ClassListContains("template-category-header"))
                toRemove.Add(child);
        }
        for (int i = 0; i < toRemove.Count; i++)
            _templateContainer.Remove(toRemove[i]);

        // Insert headers by tracking group changes
        var templates = _viewModel.Templates;
        int insertOffset = 0;
        string lastGroup = null;

        for (int i = 0; i < templates.Count; i++)
        {
            string group = templates[i].CategoryGroupLabel;
            if (group != lastGroup)
            {
                var header = new Label(group ?? "");
                header.AddToClassList("template-category-header");
                _templateContainer.Insert(i + insertOffset, header);
                insertOffset++;
                lastGroup = group;
            }
        }
    }

    // ── Step 3 — Features ─────────────────────────────────────────────────────

    private void RebuildFeatureToggles()
    {
        _featureContainer.Clear();
        if (_viewModel == null) return;

        _viewModel.RebuildFilteredList();
        var indices = _viewModel.FilteredIndices;
        int count = indices.Count;

        if (_viewModel.Features.Count > 0 && count == 0)
        {
            var empty = new Label("No features match current filters.");
            empty.AddToClassList("metric-secondary");
            _featureContainer.Add(empty);
            return;
        }

        if (_viewModel.Features.Count == 0)
        {
            var empty = new Label("No features available for this selection.");
            empty.AddToClassList("metric-secondary");
            _featureContainer.Add(empty);
            return;
        }

        FeatureCategory? lastCategory = null;
        bool useFlatHeaders = !_viewModel.ShouldShowCategoryTabs;

        for (int i = 0; i < count; i++)
        {
            var feat = _viewModel.Features[indices[i]];
            if (useFlatHeaders && feat.FeatureCategory != lastCategory)
            {
                lastCategory = feat.FeatureCategory;
                var catHeader = new Label(feat.FeatureCategory.ToString());
                catHeader.AddToClassList("feature-category-header");
                _featureContainer.Add(catHeader);
            }
            _featureContainer.Add(CreateFeatureItem(_viewModel.Features[indices[i]]));
        }

        UpdateMissingExpectedWarning();
    }

    private VisualElement CreateFeatureItem(FeatureToggleDisplay feat)
    {
        var row = new VisualElement();
        row.AddToClassList("feature-row");

        // Checkbox
        var toggle = new Toggle();
        toggle.AddToClassList("feature-toggle");
        toggle.value = feat.IsSelected;
        if (feat.IsPreSelected) toggle.SetEnabled(false);

        if (feat.IsLocked) {
            row.AddToClassList("feature-row--locked");
            toggle.SetEnabled(false);
            toggle.SetValueWithoutNotify(false);
        }

        var capturedId = feat.FeatureId;
        toggle.RegisterValueChangedCallback(evt => {
            _viewModel?.ToggleFeature(capturedId, evt.newValue);
            UpdateCostLabel();
            UpdateFeatureSummaryBar();
            UpdateFooterButtons();
            RebuildFeatureToggles();
        });
        row.Add(toggle);

        // Info column
        var info = new VisualElement();
        info.AddToClassList("feature-row__info");

        // Name
        var nameLabel = new Label(feat.DisplayName);
        nameLabel.AddToClassList("feature-name");
        info.Add(nameLabel);

        // Description
        if (!string.IsNullOrEmpty(feat.Description))
        {
            var descLabel = new Label(feat.Description);
            descLabel.AddToClassList("feature-description");
            info.Add(descLabel);
        }

        // Lock reason label
        if (feat.IsLocked && !string.IsNullOrEmpty(feat.LockReason))
        {
            var lockLabel = new Label(feat.LockReason);
            lockLabel.AddToClassList("feature-lock-reason");
            info.Add(lockLabel);
        }

        // Cap label (available but quality-capped)
        if (!feat.IsLocked && !string.IsNullOrEmpty(feat.CapLabel))
        {
            var capLabel = new Label(feat.CapLabel);
            capLabel.AddToClassList("feature-cap-label");
            info.Add(capLabel);
        }

        // Synergy label
        if (feat.HasSynergyWithSelected && !string.IsNullOrEmpty(feat.SynergyLabel))
        {
            var synLabel = new Label(feat.SynergyLabel);
            synLabel.AddToClassList("synergy-label");
            info.Add(synLabel);
        }

        // Conflict label
        if (feat.HasConflictWithSelected && !string.IsNullOrEmpty(feat.ConflictLabel))
        {
            var confLabel = new Label(feat.ConflictLabel);
            confLabel.AddToClassList("conflict-label");
            info.Add(confLabel);
        }

        row.Add(info);

        // Cost / time column
        var metaCol = new VisualElement();
        metaCol.AddToClassList("feature-row__meta");
        metaCol.style.alignItems = Align.FlexEnd;
        metaCol.style.flexShrink = 0;

        if (!string.IsNullOrEmpty(feat.DemandStageLabel))
        {
            var demandLbl = new Label(feat.DemandStageLabel);
            demandLbl.AddToClassList("feature-demand-label");
            switch (feat.DemandStage)
            {
                case FeatureDemandStage.Emerging:  demandLbl.AddToClassList("demand-cutting-edge"); break;
                case FeatureDemandStage.Growing:   demandLbl.AddToClassList("demand-trending");     break;
                case FeatureDemandStage.Standard:  demandLbl.AddToClassList("demand-expected");     break;
                case FeatureDemandStage.Declining: demandLbl.AddToClassList("demand-fading");       break;
                case FeatureDemandStage.Legacy:    demandLbl.AddToClassList("demand-outdated");     break;
            }
            metaCol.Add(demandLbl);
        }

        string costLabel2 = _viewModel?.GetFeatureCostLabel(feat.FeatureId) ?? "";
        if (!string.IsNullOrEmpty(costLabel2))
        {
            var costEl = new Label(costLabel2);
            costEl.AddToClassList("feature-cost");
            metaCol.Add(costEl);
        }

        string devTime = _viewModel?.GetFeatureDevTimeLabel(feat.FeatureId) ?? "";
        if (!string.IsNullOrEmpty(devTime))
        {
            var devTimeLabel = new Label(devTime);
            devTimeLabel.AddToClassList("feature-devtime");
            metaCol.Add(devTimeLabel);
        }

        row.Add(metaCol);

        if (_tooltipProvider != null)
        {
            var tooltipData = _viewModel?.BuildFeatureTooltip(feat.FeatureId) ?? new TooltipData { Title = feat.DisplayName, Body = feat.Description };
            row.SetRichTooltip(tooltipData, _tooltipProvider.TooltipService);
        }

        return row;
    }

    private VisualElement BuildFeatureFilterBar()
    {
        var bar = new VisualElement();
        bar.AddToClassList("feature-filter-bar");
        _filterGroupButtons.Clear();

        // Show group
        bar.Add(BuildFilterGroup("Show", new[] { "All", "Picked", "Available" }, 0, idx => {
            _viewModel?.SetSelectedFilter((CreateProductModalViewModel.FeatureSelectedFilter)idx);
            RebuildFeatureToggles();
        }));

        return bar;
    }

    private VisualElement BuildFilterGroup(string label, string[] options, int defaultIdx, System.Action<int> onChanged)
    {
        var group = new VisualElement();
        group.AddToClassList("filter-group");

        var groupLabel = new Label(label);
        groupLabel.AddToClassList("filter-group__label");
        group.Add(groupLabel);

        var buttons = new List<Button>();
        for (int i = 0; i < options.Length; i++)
        {
            var btn = new Button();
            btn.text = options[i];
            btn.AddToClassList("filter-btn");
            if (i == defaultIdx) btn.AddToClassList("filter-btn--active");

            int capturedIdx = i;
            btn.clicked += () => {
                int bCount = buttons.Count;
                for (int b = 0; b < bCount; b++) {
                    if (b == capturedIdx)
                        buttons[b].AddToClassList("filter-btn--active");
                    else
                        buttons[b].RemoveFromClassList("filter-btn--active");
                }
                onChanged(capturedIdx);
            };

            group.Add(btn);
            buttons.Add(btn);
        }

        _filterGroupButtons.Add(buttons);
        return group;
    }

    private void UpdateFeatureSummaryBar()
    {
        if (_featureSummaryLabel == null || _viewModel == null) return;
        int selected = _viewModel.SelectedFeatureCount;
        int upfront = _viewModel.TotalSelectedUpfrontCost;
        int maint = _viewModel.TotalSelectedMaintenanceCost;
        string estTime = _viewModel.EstimatedCompletionLabel;
        string text = selected + " selected"
            + (upfront > 0 ? "  |  +" + UIFormatting.FormatMoney(upfront) : "")
            + (maint > 0 ? "  |  " + UIFormatting.FormatMoney(maint) + "/mo" : "")
            + (!string.IsNullOrEmpty(estTime) && estTime != "Unknown" ? "  |  ~" + estTime : "");
        string scopeLabel = _viewModel.ScopeEfficiencyLabel;
        if (!string.IsNullOrEmpty(scopeLabel))
            text += "  |  " + scopeLabel;
        _featureSummaryLabel.text = text;
        string scopeClass = _viewModel.ScopeEfficiencyClass;
        _featureSummaryLabel.RemoveFromClassList("scope-good");
        _featureSummaryLabel.RemoveFromClassList("scope-warning");
        _featureSummaryLabel.RemoveFromClassList("scope-critical");
        if (!string.IsNullOrEmpty(scopeClass))
            _featureSummaryLabel.AddToClassList(scopeClass);
    }

    private void UpdateMissingExpectedWarning()
    {
        if (_missingExpectedWarningLabel == null || _viewModel == null) return;
        string warning = _viewModel.MissingExpectedWarning;
        bool hasWarning = !string.IsNullOrEmpty(warning);
        _missingExpectedWarningLabel.text = hasWarning ? warning : "";
        _missingExpectedWarningLabel.style.display = hasWarning ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private void UpdateCategoryTabVisibility()
    {
        if (_featureCategoryTabBar == null || _viewModel == null) return;
        bool showTabs = _viewModel.ShouldShowCategoryTabs;
        _featureCategoryTabBar.style.display = showTabs ? DisplayStyle.Flex : DisplayStyle.None;
    }

    // ── Step 6 — Pricing & Name ────────────────────────────────────────────────

    private void UpdatePriceRangeHint()
    {
        if (_priceRangeLabel == null || _viewModel == null) return;
        if (_viewModel.SuggestedPriceMin > 0f)
            _priceRangeLabel.text = "Suggested range: $" + _viewModel.SuggestedPriceMin.ToString("F2") + " \u2013 $" + _viewModel.SuggestedPriceMax.ToString("F2");
        else
            _priceRangeLabel.text = "";

        if (_sweetSpotLabel != null)
        {
            if (_viewModel.SweetSpotPrice > 0f)
                _sweetSpotLabel.text = "Sweet spot: $" + _viewModel.SweetSpotPrice.ToString("F2");
            else
                _sweetSpotLabel.text = "";
        }

        if (_priceRatingLabel != null)
        {
            if (!string.IsNullOrEmpty(_viewModel.PriceRatingLabel))
            {
                _priceRatingLabel.text = _viewModel.PriceRatingLabel;
                _priceRatingLabel.style.display = DisplayStyle.Flex;
                _priceRatingLabel.RemoveFromClassList("price-rating--good");
                _priceRatingLabel.RemoveFromClassList("price-rating--okay");
                _priceRatingLabel.RemoveFromClassList("price-rating--bad");
                if (!string.IsNullOrEmpty(_viewModel.PriceRatingClass))
                    _priceRatingLabel.AddToClassList(_viewModel.PriceRatingClass);
            }
            else
            {
                _priceRatingLabel.style.display = DisplayStyle.None;
            }
        }
    }

    private void UpdatePriceWarningLabel()
    {
        if (_priceWarningLabel == null || _viewModel == null) return;
        if (_viewModel.IsPriceExtreme && !string.IsNullOrEmpty(_viewModel.PriceWarningMessage))
        {
            _priceWarningLabel.text = _viewModel.PriceWarningMessage;
            _priceWarningLabel.style.display = DisplayStyle.Flex;
        }
        else
        {
            _priceWarningLabel.style.display = DisplayStyle.None;
        }
    }

    // ── Step 7 — Team Assignment ──────────────────────────────────────────────

    private void RebuildTeamRoleAssignments()
    {
        _teamRoleContainer.Clear();
        if (_viewModel == null) return;

        var roles = _viewModel.RequiredRoles;
        int count = roles.Count;
        for (int i = 0; i < count; i++)
            _teamRoleContainer.Add(BuildTeamAssignmentRow(roles[i], required: true));

        var optional = _viewModel.OptionalRoles;
        if (optional.Count > 0)
        {
            var optHeader = new Label("Optional");
            optHeader.AddToClassList("metric-secondary");
            optHeader.style.marginTop = 10;
            optHeader.style.marginBottom = 4;
            _teamRoleContainer.Add(optHeader);
            for (int i = 0; i < optional.Count; i++)
                _teamRoleContainer.Add(BuildTeamAssignmentRow(optional[i], required: false));
        }
    }

    private int GetPhaseIndexForRole(ProductTeamRole role)
    {
        return _viewModel?.GetPhaseIndexForRole(role) ?? -1;
    }

    private VisualElement BuildTeamAssignmentRow(ProductTeamRole role, bool required = true)
    {
        var row = new VisualElement();
        row.AddToClassList("team-assignment-row");
        row.style.flexDirection = FlexDirection.Row;
        row.style.alignItems = Align.Center;
        row.style.marginBottom = 8;

        string roleText = role.ToString();
        if (!required) roleText += " (optional)";
        var roleLabel = new Label(roleText);
        roleLabel.AddToClassList("metric-secondary");
        roleLabel.style.width = 170;
        roleLabel.style.flexShrink = 0;
        row.Add(roleLabel);

        // Assigned team display
        string assignedName = "Unassigned";
        if (_viewModel?.TeamAssignments.TryGetValue(role, out var assignedId) == true)
        {
            var teams = _viewModel.AvailableTeams;
            for (int t = 0; t < teams.Count; t++)
            {
                if (teams[t].Id == assignedId) { assignedName = teams[t].Name; break; }
            }
            if (assignedName == "Unassigned")
            {
                var mktTeams = _viewModel.AvailableMarketingTeams;
                for (int t = 0; t < mktTeams.Count; t++)
                {
                    if (mktTeams[t].Id == assignedId) { assignedName = mktTeams[t].Name; break; }
                }
            }
            if (assignedName == "Unassigned")
            {
                var busyTeams = _viewModel.BusyTeams;
                for (int t = 0; t < busyTeams.Count; t++)
                {
                    if (busyTeams[t].Id == assignedId) { assignedName = busyTeams[t].Name + " (busy)"; break; }
                }
            }
        }

        var assignedLabel = new Label(assignedName);
        assignedLabel.name = "assigned-team-" + role;
        assignedLabel.AddToClassList("metric-primary");
        assignedLabel.style.flexGrow = 1;
        row.Add(assignedLabel);

        // Assign buttons per available team
        var assignBtn = new Button { text = "Change" };
        assignBtn.AddToClassList("btn-secondary");
        assignBtn.style.flexShrink = 0;
        assignBtn.clicked += () => ShowTeamPicker(role, assignedLabel);
        row.Add(assignBtn);

        if (_viewModel?.TeamAssignments.ContainsKey(role) == true)
        {
            var clearBtn = new Button { text = "Clear" };
            clearBtn.AddToClassList("btn-secondary");
            clearBtn.style.flexShrink = 0;
            clearBtn.style.marginLeft = 4;
            clearBtn.clicked += () => {
                _viewModel?.UnassignTeam(role);
                RebuildTeamRoleAssignments();
                UpdateCompletionLabel();
                UpdateFooterButtons();
            };
            row.Add(clearBtn);
        }

        if (_tooltipProvider != null)
        {
            int phaseIdx = GetPhaseIndexForRole(role);
            if (phaseIdx >= 0)
            {
                var phaseTooltip = _viewModel?.BuildPhaseTooltip(phaseIdx) ?? new TooltipData { Title = role.ToString() };
                row.SetRichTooltip(phaseTooltip, _tooltipProvider.TooltipService);
            }
        }

        return row;
    }

    private void ShowTeamPicker(ProductTeamRole role, Label targetLabel)
    {
        if (_viewModel == null) return;

        _teamRoleContainer.Clear();

        var roles = _viewModel.RequiredRoles;
        int count = roles.Count;
        for (int i = 0; i < count; i++)
        {
            var r = roles[i];
            if (r == role)
                _teamRoleContainer.Add(BuildTeamPickerRow(role, marketing: role == ProductTeamRole.Marketing));
            else
                _teamRoleContainer.Add(BuildTeamAssignmentRow(r, required: true));
        }

        var optional = _viewModel.OptionalRoles;
        bool hasOptionalSection = optional.Count > 0;
        bool optHeaderAdded = false;
        for (int i = 0; i < optional.Count; i++)
        {
            var r = optional[i];
            if (!optHeaderAdded)
            {
                var optHeader = new Label("Optional");
                optHeader.AddToClassList("metric-secondary");
                optHeader.style.marginTop = 10;
                optHeader.style.marginBottom = 4;
                _teamRoleContainer.Add(optHeader);
                optHeaderAdded = true;
            }
            if (r == role)
                _teamRoleContainer.Add(BuildTeamPickerRow(role, marketing: role == ProductTeamRole.Marketing));
            else
                _teamRoleContainer.Add(BuildTeamAssignmentRow(r, required: false));
        }
    }

    private VisualElement BuildTeamPickerRow(ProductTeamRole role, bool marketing = false)
    {
        var col = new VisualElement();
        col.style.marginBottom = 8;

        var header = new Label("Assign team to: " + role);
        header.AddToClassList("metric-secondary");
        col.Add(header);

        var available = marketing
            ? _viewModel?.AvailableMarketingTeams ?? new System.Collections.Generic.List<TeamSummaryDisplay>()
            : _viewModel?.AvailableTeams ?? new System.Collections.Generic.List<TeamSummaryDisplay>();

        if (available.Count == 0)
        {
            string noTeamsMsg = marketing
                ? "No free marketing teams available."
                : "No free teams available.";
            var none = new Label(noTeamsMsg);
            none.AddToClassList("metric-secondary");
            col.Add(none);
        }

        for (int t = 0; t < available.Count; t++)
        {
            var team = available[t];
            var btn = new Button { text = team.Name + " (" + team.MemberCount + " members)" };
            btn.AddToClassList("btn-secondary");
            btn.style.marginBottom = 4;
            btn.clicked += () => {
                _viewModel?.AssignTeam(role, team.Id);
                RebuildTeamRoleAssignments();
                UpdateCompletionLabel();
                UpdateFooterButtons();
            };
            col.Add(btn);
        }

        var cancelBtn = new Button { text = "Cancel" };
        cancelBtn.AddToClassList("btn-secondary");
        cancelBtn.clicked += () => {
            RebuildTeamRoleAssignments();
        };
        col.Add(cancelBtn);

        return col;
    }

    private void UpdateCompletionLabel()
    {
        if (_completionLabel == null || _viewModel == null) return;
        _completionLabel.text = "Estimated Completion: " + _viewModel.EstimatedCompletionLabel;
        if (_salaryCostLabel != null)
        {
            _salaryCostLabel.text = string.IsNullOrEmpty(_viewModel.EstimatedTotalCostLabel)
                ? ""
                : "Est. Cost: " + _viewModel.EstimatedTotalCostLabel;
            if (_tooltipProvider != null)
            {
                var costTooltip = _viewModel.BuildCostBreakdownTooltip();
                _salaryCostLabel.SetRichTooltip(costTooltip, _tooltipProvider.TooltipService);
            }
        }
    }

    // ── Shared helpers ────────────────────────────────────────────────────────

    private static VisualElement BuildCheckboxRow(bool initialValue, string nameText, string costText, EventCallback<ChangeEvent<bool>> onChange)
    {
        var row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        row.style.alignItems = Align.Center;
        row.style.paddingTop = 4;
        row.style.paddingBottom = 4;
        row.AddToClassList("feature-checkbox");

        var toggle = new Toggle();
        toggle.value = initialValue;
        var internalLabel = toggle.Q<Label>();
        if (internalLabel != null) internalLabel.style.display = DisplayStyle.None;
        toggle.style.flexShrink = 0;
        toggle.style.flexGrow = 0;
        toggle.style.marginRight = 8;
        toggle.RegisterValueChangedCallback(onChange);
        row.Add(toggle);

        var nameLabel = new Label(nameText);
        nameLabel.style.flexGrow = 1;
        nameLabel.style.flexShrink = 1;
        nameLabel.style.overflow = Overflow.Hidden;
        row.Add(nameLabel);

        if (!string.IsNullOrEmpty(costText))
        {
            var costLabel = new Label(costText);
            costLabel.AddToClassList("feature-checkbox__cost");
            costLabel.style.flexShrink = 0;
            costLabel.style.marginLeft = 8;
            row.Add(costLabel);
        }

        return row;
    }

    private void UpdateCostLabel()
    {
        if (_costLabel == null || _viewModel == null) return;
        _costLabel.text = UIFormatting.FormatMoney(_viewModel.CalculatedCost);
        _costLabel.RemoveFromClassList("text-accent");
        _costLabel.RemoveFromClassList("text-danger");
        _costLabel.AddToClassList(_viewModel.CanAfford ? "text-accent" : "text-danger");
    }

    private void UpdateFooterButtons()
    {
        if (_viewModel == null) return;

        var step = _viewModel.CurrentStep;
        bool isLastStep = step == CreateProductModalViewModel.WizardStep.TeamAssignment;
        bool isCreateStep = step == CreateProductModalViewModel.WizardStep.TeamAssignment;

        if (_backButton != null)
            _backButton.style.display = _viewModel.CanGoBack ? DisplayStyle.Flex : DisplayStyle.None;

        if (_nextButton != null)
        {
            _nextButton.style.display = (!isLastStep) ? DisplayStyle.Flex : DisplayStyle.None;
            _nextButton.SetEnabled(_viewModel.CanGoNext);
        }

        if (_createButton != null)
        {
            _createButton.style.display = isCreateStep ? DisplayStyle.Flex : DisplayStyle.None;
            if (_viewModel.IsUpdateMode)
            {
                _createButton.text = "Start Update";
                _createButton.SetEnabled(true);
            }
            else
            {
                _createButton.text = "Create Product";
                bool canCreate = !string.IsNullOrEmpty(_viewModel.ProductName) && _viewModel.Price > 0f && _viewModel.CanAfford;
                _createButton.SetEnabled(canCreate);
            }
        }
    }

    // ── Button handlers ───────────────────────────────────────────────────────

    private void OnBackClicked() { _viewModel?.GoBack(); }
    private void OnNextClicked() { _viewModel?.GoNext(); }
    private void OnCancelClicked() { _modal.DismissModal(); }

    private void OnCreateClicked()
    {
        if (_viewModel == null) return;

        if (_viewModel.IsUpdateMode)
        {
            _dispatcher.Dispatch(new TriggerProductUpdateCommand {
                ProductId = _viewModel.UpdateProductId,
                UpdateType = _viewModel.SelectedUpdateType,
                FeatureIds = _viewModel.GetSelectedFeatureIds(),
                TeamAssignments = _viewModel.GetTeamAssignments()
            });
        }
        else
        {
            _dispatcher.Dispatch(new CreateProductCommand {
                TemplateId = _viewModel.SelectedTemplateId,
                ProductName = _viewModel.ProductName?.Trim() ?? "",
                SelectedFeatureIds = _viewModel.GetSelectedFeatureIds(),
                IsSubscriptionBased = _viewModel.IsSubscriptionBased,
                Price = _viewModel.Price,
                TargetPlatformIds = _viewModel.GetSelectedPlatformIds(),
                RequiredToolIds = _viewModel.GetSelectedToolIds(),
                Stance = _viewModel.SelectedStance,
                PredecessorProductId = _viewModel.SelectedPredecessorId,
                InitialTeamAssignments = _viewModel.GetTeamAssignments(),
                SequelOfId = _viewModel.IsSequelMode ? (ProductId?)_viewModel.SequelOfId : null,
                HasHardwareConfig = _viewModel.IsConsoleTemplate,
                HardwareConfig = _viewModel.HardwareConfig,
                TargetDay = _viewModel.SelectedTargetDay,
                DistributionModel = _viewModel.SelectedDistribution,
                LicensingRate = _viewModel.SelectedLicensingRate,
                MonthlySubscriptionPrice = _viewModel.SubscriptionPrice,
                SelectedNiche = _viewModel.SelectedNiche ?? ProductNiche.None
            });
        }

        _modal.DismissModal();
    }
}
#endif
