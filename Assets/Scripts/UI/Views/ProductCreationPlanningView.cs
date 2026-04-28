using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// View for the product creation planning wizard.
/// Manages step navigation, card rendering, and forecast panel binding.
/// </summary>
public class ProductCreationPlanningView : IGameView
{
    private readonly ICommandDispatcher _dispatcher;
    private readonly IModalPresenter _modal;

    private VisualElement _root;
    private ProductCreationPlanningViewModel _viewModel;

    // ── Header Elements ─────────────────────────────────────────────────────
    private Label _wizardTitle;
    private TextField _productNameField;
    private Label _stepLabel;

    // ── Stepper ─────────────────────────────────────────────────────────────
    private VisualElement _stepper;

    // ── Step Panels ─────────────────────────────────────────────────────────
    private VisualElement _stepProductType;
    private VisualElement _stepCategory;
    private VisualElement _stepMarket;
    private VisualElement _stepGenre;
    private VisualElement _stepPlatform;
    private VisualElement _stepFeatures;
    private VisualElement _stepHardware;
    private VisualElement _stepTeams;
    private VisualElement _stepBudget;
    private VisualElement _stepReview;

    // ── Card Containers ─────────────────────────────────────────────────────
    private VisualElement _typeCardsContainer;
    private VisualElement _categoryCardsContainer;
    private VisualElement _nicheCardsContainer;
    private VisualElement _genreCardsContainer;
    private VisualElement _formatCardsContainer;
    private VisualElement _platformCardsContainer;
    private Label _multiPlatformWarning;

    // ── Feature Step Elements ────────────────────────────────────────────────
    private VisualElement _featureCategoryTabs;
    private VisualElement _featureListContainer;
    private Label _featSelectedCount;
    private Label _featScopeTotal;
    private Label _featSynergyScore;
    private Label _featMissingWarning;
    private Label _featConflictWarning;

    // ── Hardware Step Elements ───────────────────────────────────────────────
    private DropdownField _hwProcessingDropdown;
    private DropdownField _hwGraphicsDropdown;
    private DropdownField _hwMemoryDropdown;
    private DropdownField _hwStorageDropdown;
    private DropdownField _hwFormFactorDropdown;
    private Label _hwPerformanceScore;
    private Label _hwManufactureCost;
    private Label _hwDevCost;
    private Label _hwThermalRisk;
    private Label _hwDefectRisk;
    private Label _hwReliability;
    private Label _hwDevFriendliness;
    private VisualElement _hwWarningsContainer;

    // ── Team Step Elements ───────────────────────────────────────────────────
    private VisualElement _teamSlotsContainer;
    private Label _teamOverallReadiness;
    private Label _teamTotalSalary;
    private Label _teamMissingCoverage;
    private VisualElement _teamSuggestionCards;

    // ── Budget Step Elements ─────────────────────────────────────────────────
    private TextField _budgetNameInput;
    private Label _budgetNameError;
    private SliderInt _priceSlider;
    private DropdownField _distributionDropdown;
    private Button _marketingLowBtn;
    private Button _marketingMediumBtn;
    private Button _marketingHighBtn;
    private Label _previewUpfrontCost;
    private Label _previewMonthlyBurn;
    private Label _previewTotalCost;
    private Label _previewRunway;
    private Label _previewBreakEven;
    private Label _pricingYourPrice;
    private Label _pricingMarketExpect;
    private Label _pricingValueRisk;
    private Label _pricingCompetitor;

    // ── Review Step Elements ─────────────────────────────────────────────────
    private Label _revProductName;
    private Label _revCategory;
    private Label _revNiche;
    private Label _revGenre;
    private Label _revPlatforms;
    private Label _revFeaturesCount;
    private Label _revMarketDemand;
    private Label _revMarketCompetition;
    private Label _revMarketFit;
    private Label _revMarketTrend;
    private Label _revCostRange;
    private Label _revDurationRange;
    private Label _revMonthlyBurn;
    private VisualElement _reviewTeamSlotsContainer;
    private Label _revTeamReadiness;
    private Label _revQualityRange;
    private Label _revInnovationRange;
    private Label _revBugRisk;
    private Label _revScopeRisk;
    private Label _revConfidence;
    private VisualElement _reviewErrorsContainer;
    private VisualElement _reviewWarningsContainer;
    private Button _btnStartDevelopment;

    // ── Forecast Elements ───────────────────────────────────────────────────
    private Label _forecastQuality;
    private Label _forecastInnovation;
    private Label _forecastMarketFit;
    private Label _forecastScopeRisk;
    private Label _forecastBugRisk;
    private Label _forecastTechnicalRisk;
    private Label _forecastCommercialRisk;
    private Label _forecastDuration;
    private Label _forecastCost;
    private Label _forecastConfidence;
    private VisualElement _diagnosticsContainer;

    // ── Bottom Risk Bar ─────────────────────────────────────────────────────
    private Label _riskScope;
    private Label _riskCost;
    private Label _riskDuration;
    private Label _riskBug;
    private Label _riskMarketFit;
    private Label _riskCoverage;

    // ── Footer Buttons ──────────────────────────────────────────────────────
    private Button _btnBack;
    private Button _btnContinue;
    private Button _btnSaveDraft;
    private Button _btnCancel;

    // ── USS Path ────────────────────────────────────────────────────────────
    private const string UssPath = "Assets/UI/USS/Screens/product-creation-planning.uss";

    // ── Events ──────────────────────────────────────────────────────────────
    public event Action OnCancelRequested;
    public event Action OnProductCreated;

    // ── Step Panel Map ──────────────────────────────────────────────────────
    private readonly Dictionary<ProductCreationPlanningViewModel.WizardStepId, VisualElement> _stepPanelMap =
        new Dictionary<ProductCreationPlanningViewModel.WizardStepId, VisualElement>();

    // ── Element Pools ───────────────────────────────────────────────────────
    private ElementPool _typeCardPool;
    private ElementPool _categoryCardPool;
    private ElementPool _nicheCardPool;
    private ElementPool _genreCardPool;
    private ElementPool _formatCardPool;
    private ElementPool _platformCardPool;
    private ElementPool _diagnosticPool;
    private ElementPool _featureCardPool;
    private ElementPool _teamSlotPool;
    private ElementPool _teamSuggestionPool;

    // ── Tier Label Cache ─────────────────────────────────────────────────────
    private static readonly List<string> HwTierChoices = new List<string> { "Budget", "Mid", "Premium", "Ultra" };
    private static readonly List<string> HwFormFactorChoices = new List<string> { "Standard", "Portable", "Hybrid" };

    // ── Team Slot Binding Cache ──────────────────────────────────────────────
    private readonly List<ProductSlotData> _teamSlotsListCache = new List<ProductSlotData>(4);

    public ProductCreationPlanningView(ICommandDispatcher dispatcher, IModalPresenter modal)
    {
        _dispatcher = dispatcher;
        _modal = modal;
    }

    // ── IGameView ───────────────────────────────────────────────────────────

    public void Initialize(VisualElement root)
    {
        _root = root;

        // Load USS
        var uss = UnityEngine.Resources.Load<StyleSheet>(UssPath);
        if (uss == null)
        {
            var ussAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<StyleSheet>(UssPath);
            if (ussAsset != null) _root.styleSheets.Add(ussAsset);
        }
        else
        {
            _root.styleSheets.Add(uss);
        }

        QueryElements();
        WireHandlers();
        BuildElementPools();
    }

    public void Bind(IViewModel viewModel)
    {
        if (_viewModel != null)
        {
            _viewModel.OnCancelRequested -= HandleVmCancel;
        }

        _viewModel = viewModel as ProductCreationPlanningViewModel;
        if (_viewModel == null)
        {
            Debug.LogError("[ProductCreationPlanningView] Bind called with incompatible ViewModel.");
            return;
        }

        _viewModel.OnCancelRequested += HandleVmCancel;

        // Build genre options once
        _viewModel.BuildGenreOptions();

        RefreshAll();
    }

    public void Dispose()
    {
        if (_viewModel != null)
        {
            _viewModel.OnCancelRequested -= HandleVmCancel;
        }

        UnwireHandlers();
        _viewModel = null;
    }

    /// <summary>
    /// Called externally after ViewModel.Refresh() to update the view.
    /// </summary>
    public void RefreshAfterChange()
    {
        RefreshAll();
    }

    // ── Query Elements ──────────────────────────────────────────────────────

    private void QueryElements()
    {
        // Header
        _wizardTitle = _root.Q<Label>("wizard-title");
        _productNameField = _root.Q<TextField>("product-name-field");
        _stepLabel = _root.Q<Label>("step-label");

        // Stepper
        _stepper = _root.Q<VisualElement>("progress-stepper");

        // Step panels
        _stepProductType = _root.Q<VisualElement>("step-product-type");
        _stepCategory = _root.Q<VisualElement>("step-category");
        _stepMarket = _root.Q<VisualElement>("step-market");
        _stepGenre = _root.Q<VisualElement>("step-genre");
        _stepPlatform = _root.Q<VisualElement>("step-platform");
        _stepFeatures = _root.Q<VisualElement>("step-features");
        _stepHardware = _root.Q<VisualElement>("step-hardware");
        _stepTeams = _root.Q<VisualElement>("step-teams");
        _stepBudget = _root.Q<VisualElement>("step-budget");
        _stepReview = _root.Q<VisualElement>("step-review");

        // Build step panel map
        _stepPanelMap[ProductCreationPlanningViewModel.WizardStepId.ProductType] = _stepProductType;
        _stepPanelMap[ProductCreationPlanningViewModel.WizardStepId.Category] = _stepCategory;
        _stepPanelMap[ProductCreationPlanningViewModel.WizardStepId.Market] = _stepMarket;
        _stepPanelMap[ProductCreationPlanningViewModel.WizardStepId.Genre] = _stepGenre;
        _stepPanelMap[ProductCreationPlanningViewModel.WizardStepId.Platform] = _stepPlatform;
        _stepPanelMap[ProductCreationPlanningViewModel.WizardStepId.Features] = _stepFeatures;
        _stepPanelMap[ProductCreationPlanningViewModel.WizardStepId.Hardware] = _stepHardware;
        _stepPanelMap[ProductCreationPlanningViewModel.WizardStepId.Teams] = _stepTeams;
        _stepPanelMap[ProductCreationPlanningViewModel.WizardStepId.Budget] = _stepBudget;
        _stepPanelMap[ProductCreationPlanningViewModel.WizardStepId.Review] = _stepReview;

        // Card containers
        _typeCardsContainer = _root.Q<VisualElement>("type-cards-container");
        _categoryCardsContainer = _root.Q<VisualElement>("category-cards-container");
        _nicheCardsContainer = _root.Q<VisualElement>("niche-cards-container");
        _genreCardsContainer = _root.Q<VisualElement>("genre-cards-container");
        _formatCardsContainer = _root.Q<VisualElement>("format-cards-container");
        _platformCardsContainer = _root.Q<VisualElement>("platform-cards-container");
        _multiPlatformWarning = _root.Q<Label>("multi-platform-warning");

        // Feature step
        _featureCategoryTabs = _root.Q<VisualElement>("feature-category-tabs");
        _featureListContainer = _root.Q<VisualElement>("feature-list-container");
        _featSelectedCount = _root.Q<Label>("feat-selected-count");
        _featScopeTotal = _root.Q<Label>("feat-scope-total");
        _featSynergyScore = _root.Q<Label>("feat-synergy-score");
        _featMissingWarning = _root.Q<Label>("feat-missing-warning");
        _featConflictWarning = _root.Q<Label>("feat-conflict-warning");

        // Hardware step
        _hwProcessingDropdown = _root.Q<DropdownField>("hw-processing-dropdown");
        _hwGraphicsDropdown = _root.Q<DropdownField>("hw-graphics-dropdown");
        _hwMemoryDropdown = _root.Q<DropdownField>("hw-memory-dropdown");
        _hwStorageDropdown = _root.Q<DropdownField>("hw-storage-dropdown");
        _hwFormFactorDropdown = _root.Q<DropdownField>("hw-formfactor-dropdown");
        _hwPerformanceScore = _root.Q<Label>("hw-performance-score");
        _hwManufactureCost = _root.Q<Label>("hw-manufacture-cost");
        _hwDevCost = _root.Q<Label>("hw-dev-cost");
        _hwThermalRisk = _root.Q<Label>("hw-thermal-risk");
        _hwDefectRisk = _root.Q<Label>("hw-defect-risk");
        _hwReliability = _root.Q<Label>("hw-reliability");
        _hwDevFriendliness = _root.Q<Label>("hw-dev-friendliness");
        _hwWarningsContainer = _root.Q<VisualElement>("hardware-warnings");

        // Team step
        _teamSlotsContainer = _root.Q<VisualElement>("team-slots-container");
        _teamOverallReadiness = _root.Q<Label>("team-overall-readiness");
        _teamTotalSalary = _root.Q<Label>("team-total-salary");
        _teamMissingCoverage = _root.Q<Label>("team-missing-coverage");
        _teamSuggestionCards = _root.Q<VisualElement>("team-suggestion-cards");

        // Budget step
        _budgetNameInput = _root.Q<TextField>("budget-name-input");
        _budgetNameError = _root.Q<Label>("budget-name-error");
        _priceSlider = _root.Q<SliderInt>("price-slider");
        _distributionDropdown = _root.Q<DropdownField>("distribution-dropdown");
        _marketingLowBtn = _root.Q<Button>("marketing-low-btn");
        _marketingMediumBtn = _root.Q<Button>("marketing-medium-btn");
        _marketingHighBtn = _root.Q<Button>("marketing-high-btn");
        _previewUpfrontCost = _root.Q<Label>("preview-upfront-cost");
        _previewMonthlyBurn = _root.Q<Label>("preview-monthly-burn");
        _previewTotalCost = _root.Q<Label>("preview-total-cost");
        _previewRunway = _root.Q<Label>("preview-runway");
        _previewBreakEven = _root.Q<Label>("preview-break-even");
        _pricingYourPrice = _root.Q<Label>("pricing-your-price");
        _pricingMarketExpect = _root.Q<Label>("pricing-market-expect");
        _pricingValueRisk = _root.Q<Label>("pricing-value-risk");
        _pricingCompetitor = _root.Q<Label>("pricing-competitor");

        // Review step
        _revProductName = _root.Q<Label>("rev-product-name");
        _revCategory = _root.Q<Label>("rev-category");
        _revNiche = _root.Q<Label>("rev-niche");
        _revGenre = _root.Q<Label>("rev-genre");
        _revPlatforms = _root.Q<Label>("rev-platforms");
        _revFeaturesCount = _root.Q<Label>("rev-features-count");
        _revMarketDemand = _root.Q<Label>("rev-market-demand");
        _revMarketCompetition = _root.Q<Label>("rev-market-competition");
        _revMarketFit = _root.Q<Label>("rev-market-fit");
        _revMarketTrend = _root.Q<Label>("rev-market-trend");
        _revCostRange = _root.Q<Label>("rev-cost-range");
        _revDurationRange = _root.Q<Label>("rev-duration-range");
        _revMonthlyBurn = _root.Q<Label>("rev-monthly-burn");
        _reviewTeamSlotsContainer = _root.Q<VisualElement>("review-team-slots-container");
        _revTeamReadiness = _root.Q<Label>("rev-team-readiness");
        _revQualityRange = _root.Q<Label>("rev-quality-range");
        _revInnovationRange = _root.Q<Label>("rev-innovation-range");
        _revBugRisk = _root.Q<Label>("rev-bug-risk");
        _revScopeRisk = _root.Q<Label>("rev-scope-risk");
        _revConfidence = _root.Q<Label>("rev-confidence");
        _reviewErrorsContainer = _root.Q<VisualElement>("review-errors-container");
        _reviewWarningsContainer = _root.Q<VisualElement>("review-warnings-container");
        _btnStartDevelopment = _root.Q<Button>("btn-start-development");

        // Forecast
        _forecastQuality = _root.Q<Label>("forecast-quality");
        _forecastInnovation = _root.Q<Label>("forecast-innovation");
        _forecastMarketFit = _root.Q<Label>("forecast-market-fit");
        _forecastScopeRisk = _root.Q<Label>("forecast-scope-risk");
        _forecastBugRisk = _root.Q<Label>("forecast-bug-risk");
        _forecastTechnicalRisk = _root.Q<Label>("forecast-technical-risk");
        _forecastCommercialRisk = _root.Q<Label>("forecast-commercial-risk");
        _forecastDuration = _root.Q<Label>("forecast-duration");
        _forecastCost = _root.Q<Label>("forecast-cost");
        _forecastConfidence = _root.Q<Label>("forecast-confidence");
        _diagnosticsContainer = _root.Q<VisualElement>("diagnostic-cards-container");

        // Risk bar
        _riskScope = _root.Q<Label>("risk-scope");
        _riskCost = _root.Q<Label>("risk-cost");
        _riskDuration = _root.Q<Label>("risk-duration");
        _riskBug = _root.Q<Label>("risk-bug");
        _riskMarketFit = _root.Q<Label>("risk-market-fit");
        _riskCoverage = _root.Q<Label>("risk-coverage");

        // Footer
        _btnBack = _root.Q<Button>("btn-back");
        _btnContinue = _root.Q<Button>("btn-continue");
        _btnSaveDraft = _root.Q<Button>("btn-save-draft");
        _btnCancel = _root.Q<Button>("btn-cancel");
    }

    // ── Element Pools ───────────────────────────────────────────────────────

    private void BuildElementPools()
    {
        _typeCardPool = new ElementPool(CreateCard, _typeCardsContainer);
        _categoryCardPool = new ElementPool(CreateCard, _categoryCardsContainer);
        _nicheCardPool = new ElementPool(CreateNicheCard, _nicheCardsContainer);
        _genreCardPool = new ElementPool(CreateCard, _genreCardsContainer);
        _formatCardPool = new ElementPool(CreateFormatCard, _formatCardsContainer);
        _platformCardPool = new ElementPool(CreatePlatformCard, _platformCardsContainer);
        _diagnosticPool = new ElementPool(CreateDiagnosticCard, _diagnosticsContainer);
        _featureCardPool = new ElementPool(CreateFeatureCard, _featureListContainer);
        _teamSlotPool = new ElementPool(CreateTeamSlotCard, _teamSlotsContainer);
        _teamSuggestionPool = new ElementPool(CreateTeamSuggestionCard, _teamSuggestionCards);

        SetupHardwareDropdowns();
        SetupBudgetDropdown();
    }

    private void SetupBudgetDropdown()
    {
        if (_distributionDropdown != null)
        {
            var labels = ProductCreationPlanningViewModel.GetDistributionModelLabels();
            var choices = new List<string>();
            for (int i = 0; i < labels.Length; i++)
                choices.Add(labels[i]);
            _distributionDropdown.choices = choices;
            _distributionDropdown.index = 0;
        }
    }

    private void SetupHardwareDropdowns()
    {
        if (_hwProcessingDropdown != null)
        {
            _hwProcessingDropdown.choices = HwTierChoices;
            _hwProcessingDropdown.index = 0;
            _hwProcessingDropdown.RegisterValueChangedCallback(OnProcessingTierChanged);
        }
        if (_hwGraphicsDropdown != null)
        {
            _hwGraphicsDropdown.choices = HwTierChoices;
            _hwGraphicsDropdown.index = 0;
            _hwGraphicsDropdown.RegisterValueChangedCallback(OnGraphicsTierChanged);
        }
        if (_hwMemoryDropdown != null)
        {
            _hwMemoryDropdown.choices = HwTierChoices;
            _hwMemoryDropdown.index = 0;
            _hwMemoryDropdown.RegisterValueChangedCallback(OnMemoryTierChanged);
        }
        if (_hwStorageDropdown != null)
        {
            _hwStorageDropdown.choices = HwTierChoices;
            _hwStorageDropdown.index = 0;
            _hwStorageDropdown.RegisterValueChangedCallback(OnStorageTierChanged);
        }
        if (_hwFormFactorDropdown != null)
        {
            _hwFormFactorDropdown.choices = HwFormFactorChoices;
            _hwFormFactorDropdown.index = 0;
            _hwFormFactorDropdown.RegisterValueChangedCallback(OnFormFactorChanged);
        }
    }

    // ── Card Factories ──────────────────────────────────────────────────────

    private static VisualElement CreateCard()
    {
        var card = new VisualElement();
        card.AddToClassList("pcw__card");

        var title = new Label();
        title.AddToClassList("pcw__card-title");
        title.name = "card-title";
        card.Add(title);

        var desc = new Label();
        desc.AddToClassList("pcw__card-desc");
        desc.name = "card-desc";
        card.Add(desc);

        var meta = new VisualElement();
        meta.AddToClassList("pcw__card-meta");
        meta.name = "card-meta";
        card.Add(meta);

        var lockReason = new Label();
        lockReason.AddToClassList("pcw__card-lock-reason");
        lockReason.name = "card-lock-reason";
        lockReason.style.display = DisplayStyle.None;
        card.Add(lockReason);

        return card;
    }

    private static VisualElement CreateNicheCard()
    {
        var card = CreateCard();

        var demandBar = new VisualElement();
        demandBar.AddToClassList("pcw__demand-bar");
        var demandFill = new VisualElement();
        demandFill.AddToClassList("pcw__demand-bar-fill");
        demandFill.name = "demand-fill";
        demandBar.Add(demandFill);
        card.Add(demandBar);

        var satBar = new VisualElement();
        satBar.AddToClassList("pcw__saturation-bar");
        var satFill = new VisualElement();
        satFill.AddToClassList("pcw__saturation-bar-fill");
        satFill.name = "saturation-fill";
        satBar.Add(satFill);
        card.Add(satBar);

        return card;
    }

    private static VisualElement CreateFormatCard()
    {
        var card = CreateCard();
        card.AddToClassList("pcw__card--format");
        return card;
    }

    private static VisualElement CreatePlatformCard()
    {
        var card = new VisualElement();
        card.AddToClassList("pcw__card");
        card.AddToClassList("pcw__card--platform");

        var toggle = new Toggle();
        toggle.AddToClassList("pcw__platform-toggle");
        toggle.name = "platform-toggle";
        card.Add(toggle);

        var title = new Label();
        title.AddToClassList("pcw__card-title");
        title.name = "card-title";
        card.Add(title);

        var desc = new Label();
        desc.AddToClassList("pcw__card-desc");
        desc.name = "card-desc";
        card.Add(desc);

        var meta = new VisualElement();
        meta.AddToClassList("pcw__card-meta");
        meta.name = "card-meta";
        card.Add(meta);

        var shareBar = new VisualElement();
        shareBar.AddToClassList("pcw__market-share-bar");
        var shareFill = new VisualElement();
        shareFill.AddToClassList("pcw__market-share-bar-fill");
        shareFill.name = "share-fill";
        shareBar.Add(shareFill);
        card.Add(shareBar);

        return card;
    }

    private static VisualElement CreateDiagnosticCard()
    {
        var card = new VisualElement();
        card.AddToClassList("pcw__diagnostic-card");

        var title = new Label();
        title.AddToClassList("pcw__diagnostic-title");
        title.name = "diag-title";
        card.Add(title);

        var desc = new Label();
        desc.AddToClassList("pcw__diagnostic-desc");
        desc.name = "diag-desc";
        card.Add(desc);

        return card;
    }

    // ── Wire / Unwire Handlers ──────────────────────────────────────────────

    private void WireHandlers()
    {
        if (_btnBack != null) _btnBack.clicked += OnBackClicked;
        if (_btnContinue != null) _btnContinue.clicked += OnContinueClicked;
        if (_btnSaveDraft != null) _btnSaveDraft.clicked += OnSaveDraftClicked;
        if (_btnCancel != null) _btnCancel.clicked += OnCancelClicked;
        if (_productNameField != null) _productNameField.RegisterValueChangedCallback(OnProductNameChanged);
        if (_budgetNameInput != null) _budgetNameInput.RegisterValueChangedCallback(OnBudgetNameChanged);
        if (_priceSlider != null) _priceSlider.RegisterValueChangedCallback(OnPriceChanged);
        if (_distributionDropdown != null) _distributionDropdown.RegisterValueChangedCallback(OnDistributionChanged);
        if (_marketingLowBtn != null) _marketingLowBtn.clicked += OnMarketingLowClicked;
        if (_marketingMediumBtn != null) _marketingMediumBtn.clicked += OnMarketingMediumClicked;
        if (_marketingHighBtn != null) _marketingHighBtn.clicked += OnMarketingHighClicked;
        if (_btnStartDevelopment != null) _btnStartDevelopment.clicked += OnStartDevelopmentClicked;
    }

    private void UnwireHandlers()
    {
        if (_btnBack != null) _btnBack.clicked -= OnBackClicked;
        if (_btnContinue != null) _btnContinue.clicked -= OnContinueClicked;
        if (_btnSaveDraft != null) _btnSaveDraft.clicked -= OnSaveDraftClicked;
        if (_btnCancel != null) _btnCancel.clicked -= OnCancelClicked;
        if (_productNameField != null) _productNameField.UnregisterValueChangedCallback(OnProductNameChanged);
        if (_budgetNameInput != null) _budgetNameInput.UnregisterValueChangedCallback(OnBudgetNameChanged);
        if (_priceSlider != null) _priceSlider.UnregisterValueChangedCallback(OnPriceChanged);
        if (_distributionDropdown != null) _distributionDropdown.UnregisterValueChangedCallback(OnDistributionChanged);
        if (_marketingLowBtn != null) _marketingLowBtn.clicked -= OnMarketingLowClicked;
        if (_marketingMediumBtn != null) _marketingMediumBtn.clicked -= OnMarketingMediumClicked;
        if (_marketingHighBtn != null) _marketingHighBtn.clicked -= OnMarketingHighClicked;
        if (_btnStartDevelopment != null) _btnStartDevelopment.clicked -= OnStartDevelopmentClicked;

        if (_hwProcessingDropdown != null) _hwProcessingDropdown.UnregisterValueChangedCallback(OnProcessingTierChanged);
        if (_hwGraphicsDropdown != null) _hwGraphicsDropdown.UnregisterValueChangedCallback(OnGraphicsTierChanged);
        if (_hwMemoryDropdown != null) _hwMemoryDropdown.UnregisterValueChangedCallback(OnMemoryTierChanged);
        if (_hwStorageDropdown != null) _hwStorageDropdown.UnregisterValueChangedCallback(OnStorageTierChanged);
        if (_hwFormFactorDropdown != null) _hwFormFactorDropdown.UnregisterValueChangedCallback(OnFormFactorChanged);
    }

    // ── Event Handlers ──────────────────────────────────────────────────────

    private void OnBackClicked()
    {
        if (_viewModel == null) return;
        _viewModel.GoToPreviousStep();
        RefreshAll();
    }

    private void OnContinueClicked()
    {
        if (_viewModel == null) return;
        _viewModel.GoToNextStep();
        RefreshAll();
    }

    private void OnSaveDraftClicked()
    {
        _viewModel?.RequestSaveDraft();
    }

    private void OnCancelClicked()
    {
        _viewModel?.RequestCancel();
        OnCancelRequested?.Invoke();
    }

    private void OnProductNameChanged(ChangeEvent<string> evt)
    {
        if (_viewModel != null)
        {
            _viewModel.Draft.ProductName = evt.newValue;
        }
    }

    private void HandleVmCancel()
    {
        OnCancelRequested?.Invoke();
    }

    // ── Card Click Handlers ─────────────────────────────────────────────────

    private void OnProductTypeCardClicked(int index)
    {
        if (_viewModel == null) return;
        _viewModel.SelectProductType(index);
        RefreshAll();
    }

    private void OnCategoryCardClicked(int index)
    {
        if (_viewModel == null) return;
        _viewModel.SelectCategory(index);
        RefreshAll();
    }

    private void OnNicheCardClicked(int index)
    {
        if (_viewModel == null) return;
        _viewModel.SelectNiche(index);
        RefreshAll();
    }

    private void OnGenreCardClicked(int index)
    {
        if (_viewModel == null) return;
        _viewModel.SelectGenre(index);
        RefreshAll();
    }

    private void OnFormatCardClicked(int index)
    {
        if (_viewModel == null) return;
        _viewModel.SelectFormat(index);
        RefreshAll();
    }

    private void OnPlatformToggled(int index)
    {
        if (_viewModel == null) return;
        _viewModel.TogglePlatform(index);
        RefreshAll();
    }

    // ── Refresh All ─────────────────────────────────────────────────────────

    private void RefreshAll()
    {
        if (_viewModel == null) return;

        // Update header
        if (_stepLabel != null) _stepLabel.text = _viewModel.StepLabel;

        // Show current step
        ShowCurrentStep();

        // Update stepper
        UpdateStepper();

        // Bind current step content
        BindCurrentStep();

        // Update forecast panel
        BindForecast();

        // Update bottom risk bar
        BindRiskBar();

        // Update footer buttons
        UpdateFooterButtons();
    }

    // ── Step Management ─────────────────────────────────────────────────────

    private void ShowCurrentStep()
    {
        // Hide all step panels
        foreach (var kvp in _stepPanelMap)
        {
            if (kvp.Value != null)
            {
                kvp.Value.RemoveFromClassList("pcw__step--active");
            }
        }

        // Show current step panel
        var currentStepId = _viewModel.CurrentStepId;
        if (_stepPanelMap.TryGetValue(currentStepId, out var panel) && panel != null)
        {
            panel.AddToClassList("pcw__step--active");
        }
    }

    private void UpdateStepper()
    {
        if (_stepper == null) return;
        _stepper.Clear();

        var stepOrder = _viewModel.StepOrder;
        int currentStep = _viewModel.CurrentStep;

        for (int i = 0; i < stepOrder.Count; i++)
        {
            if (i > 0)
            {
                var line = new VisualElement();
                line.AddToClassList("pcw__stepper-line");
                if (i <= currentStep)
                    line.AddToClassList("pcw__stepper-line--completed");
                _stepper.Add(line);
            }

            var dot = new VisualElement();
            dot.AddToClassList("pcw__stepper-dot");
            if (i == currentStep)
                dot.AddToClassList("pcw__stepper-dot--active");
            else if (i < currentStep)
                dot.AddToClassList("pcw__stepper-dot--completed");
            _stepper.Add(dot);
        }
    }

    // ── Bind Per Step ───────────────────────────────────────────────────────

    private void BindCurrentStep()
    {
        switch (_viewModel.CurrentStepId)
        {
            case ProductCreationPlanningViewModel.WizardStepId.ProductType:
                BindTypeCards();
                break;
            case ProductCreationPlanningViewModel.WizardStepId.Category:
                BindCategoryCards();
                break;
            case ProductCreationPlanningViewModel.WizardStepId.Market:
                BindNicheCards();
                break;
            case ProductCreationPlanningViewModel.WizardStepId.Genre:
                BindGenreCards();
                BindFormatCards();
                break;
            case ProductCreationPlanningViewModel.WizardStepId.Platform:
                BindPlatformCards();
                break;
            case ProductCreationPlanningViewModel.WizardStepId.Features:
                BindFeatureStep();
                break;
            case ProductCreationPlanningViewModel.WizardStepId.Hardware:
                BindHardwareStep();
                break;
            case ProductCreationPlanningViewModel.WizardStepId.Teams:
                BindTeamStep();
                break;
            case ProductCreationPlanningViewModel.WizardStepId.Budget:
                BindBudgetStep();
                break;
            case ProductCreationPlanningViewModel.WizardStepId.Review:
                BindReviewStep();
                break;
        }
    }

    private void BindTypeCards()
    {
        var options = _viewModel.TypeOptions;
        _typeCardPool.UpdateList(options, (element, option) =>
        {
            int index = options.IndexOf(option);
            var title = element.Q<Label>("card-title");
            var desc = element.Q<Label>("card-desc");
            var meta = element.Q<VisualElement>("card-meta");

            if (title != null) title.text = option.DisplayName;
            if (desc != null) desc.text = option.Description;

            if (meta != null)
            {
                meta.Clear();
                AddMetaRow(meta, "Skills", option.SkillNeeds);
                AddMetaRow(meta, "Teams", option.TeamNeeds);
                AddMetaRow(meta, "Market Risk", option.MarketRisk);
                AddMetaRow(meta, "Stage", option.RecommendedStage);
                AddMetaRow(meta, "Avg Cost", $"${option.AvgCost:N0}");
            }

            element.focusable = true;
            element.EnableInClassList("pcw__card--selected", index == _viewModel.SelectedTypeIndex);

            element.userData = index;
            element.UnregisterCallback<ClickEvent>(OnTypeCardClickEvent);
            element.RegisterCallback<ClickEvent>(OnTypeCardClickEvent);
            element.UnregisterCallback<KeyDownEvent>(OnCardKeyDown);
            element.RegisterCallback<KeyDownEvent>(OnCardKeyDown);
        });
    }

    private void OnTypeCardClickEvent(ClickEvent evt)
    {
        var element = evt.currentTarget as VisualElement;
        if (element?.userData is int index)
        {
            OnProductTypeCardClicked(index);
        }
    }

    private void BindCategoryCards()
    {
        var options = _viewModel.CategoryOptions;
        _categoryCardPool.UpdateList(options, (element, option) =>
        {
            int index = options.IndexOf(option);
            var title = element.Q<Label>("card-title");
            var desc = element.Q<Label>("card-desc");
            var meta = element.Q<VisualElement>("card-meta");
            var lockReason = element.Q<Label>("card-lock-reason");

            if (title != null) title.text = option.DisplayName;
            if (desc != null) desc.text = option.Description ?? "";

            if (meta != null)
            {
                meta.Clear();
                AddMetaRow(meta, "Difficulty", $"{option.DifficultyTier}/5");
                AddMetaRow(meta, "Base Cost", $"${option.BaseCost:N0}");
                AddMetaRow(meta, "Phases", option.PhaseCount.ToString());
                AddMetaRow(meta, "Features", option.FeatureCount.ToString());
                if (option.GenerationRequirement > 1)
                    AddMetaRow(meta, "Min Gen", option.GenerationRequirement.ToString());
            }

            // Lock state
            element.EnableInClassList("pcw__card--locked", option.IsLocked);
            if (lockReason != null)
            {
                lockReason.text = option.LockedReason ?? "";
                lockReason.style.display = option.IsLocked ? DisplayStyle.Flex : DisplayStyle.None;
            }

            element.focusable = true;
            element.EnableInClassList("pcw__card--selected", index == _viewModel.SelectedCategoryIndex);

            element.userData = index;
            element.UnregisterCallback<ClickEvent>(OnCategoryCardClickEvent);
            element.RegisterCallback<ClickEvent>(OnCategoryCardClickEvent);
            element.UnregisterCallback<KeyDownEvent>(OnCardKeyDown);
            element.RegisterCallback<KeyDownEvent>(OnCardKeyDown);
        });
    }

    private void OnCategoryCardClickEvent(ClickEvent evt)
    {
        var element = evt.currentTarget as VisualElement;
        if (element?.userData is int index)
        {
            OnCategoryCardClicked(index);
        }
    }

    private void BindNicheCards()
    {
        var options = _viewModel.NicheOptions;
        _nicheCardPool.UpdateList(options, (element, option) =>
        {
            int index = options.IndexOf(option);
            var title = element.Q<Label>("card-title");
            var desc = element.Q<Label>("card-desc");
            var meta = element.Q<VisualElement>("card-meta");
            var demandFill = element.Q<VisualElement>("demand-fill");
            var satFill = element.Q<VisualElement>("saturation-fill");

            if (title != null) title.text = option.DisplayName;
            if (desc != null)
            {
                desc.text = $"Demand: {option.DemandText}  |  Trend: {option.TrendText}";
                desc.RemoveFromClassList("pcw__trend-rising");
                desc.RemoveFromClassList("pcw__trend-stable");
                desc.RemoveFromClassList("pcw__trend-falling");
                switch (option.Trend)
                {
                    case MarketTrend.Rising: desc.AddToClassList("pcw__trend-rising"); break;
                    case MarketTrend.Falling: desc.AddToClassList("pcw__trend-falling"); break;
                    default: desc.AddToClassList("pcw__trend-stable"); break;
                }
            }

            if (meta != null)
            {
                meta.Clear();
                AddMetaRow(meta, "Competition", option.CompetitionText);
                AddMetaRow(meta, "Growth", option.ProjectedGrowth);
                AddMetaRow(meta, "Risk", option.RiskLabel);
            }

            if (demandFill != null)
                demandFill.style.width = new Length(Mathf.Clamp(option.DemandPercent, 0f, 100f), LengthUnit.Percent);

            if (satFill != null)
                satFill.style.width = new Length(Mathf.Clamp(option.SaturationPercent * 100f, 0f, 100f), LengthUnit.Percent);

            element.focusable = true;
            element.EnableInClassList("pcw__card--selected", index == _viewModel.SelectedNicheIndex);

            element.userData = index;
            element.UnregisterCallback<ClickEvent>(OnNicheCardClickEvent);
            element.RegisterCallback<ClickEvent>(OnNicheCardClickEvent);
            element.UnregisterCallback<KeyDownEvent>(OnCardKeyDown);
            element.RegisterCallback<KeyDownEvent>(OnCardKeyDown);
        });
    }

    private void OnNicheCardClickEvent(ClickEvent evt)
    {
        var element = evt.currentTarget as VisualElement;
        if (element?.userData is int index)
        {
            OnNicheCardClicked(index);
        }
    }

    private void BindGenreCards()
    {
        var options = _viewModel.GenreOptions;
        _genreCardPool.UpdateList(options, (element, option) =>
        {
            int index = options.IndexOf(option);
            var title = element.Q<Label>("card-title");
            var desc = element.Q<Label>("card-desc");
            var meta = element.Q<VisualElement>("card-meta");

            if (title != null) title.text = option.DisplayName;
            if (desc != null) desc.text = option.Description;

            if (meta != null)
            {
                meta.Clear();
                AddMetaRow(meta, "Audience", option.Audience);
                AddMetaRow(meta, "Core Features", option.CoreFeatures);
                AddMetaRow(meta, "Skills", option.RelevantSkills);
                AddMetaRow(meta, "Risk", option.RiskProfile);
            }

            element.focusable = true;
            element.EnableInClassList("pcw__card--selected", index == _viewModel.SelectedGenreIndex);

            element.userData = index;
            element.UnregisterCallback<ClickEvent>(OnGenreCardClickEvent);
            element.RegisterCallback<ClickEvent>(OnGenreCardClickEvent);
            element.UnregisterCallback<KeyDownEvent>(OnCardKeyDown);
            element.RegisterCallback<KeyDownEvent>(OnCardKeyDown);
        });
    }

    private void OnGenreCardClickEvent(ClickEvent evt)
    {
        var element = evt.currentTarget as VisualElement;
        if (element?.userData is int index)
        {
            OnGenreCardClicked(index);
        }
    }

    private void BindFormatCards()
    {
        var options = _viewModel.FormatOptions;
        _formatCardPool.UpdateList(options, (element, option) =>
        {
            int index = options.IndexOf(option);
            var title = element.Q<Label>("card-title");
            var desc = element.Q<Label>("card-desc");
            var meta = element.Q<VisualElement>("card-meta");

            if (title != null) title.text = option.DisplayName;
            if (desc != null) desc.text = option.Description;

            if (meta != null)
            {
                meta.Clear();
                AddMetaRow(meta, "Scope", option.ScopeImpact);
            }

            element.focusable = true;
            element.EnableInClassList("pcw__card--selected", index == _viewModel.SelectedFormatIndex);

            element.userData = index;
            element.UnregisterCallback<ClickEvent>(OnFormatCardClickEvent);
            element.RegisterCallback<ClickEvent>(OnFormatCardClickEvent);
            element.UnregisterCallback<KeyDownEvent>(OnCardKeyDown);
            element.RegisterCallback<KeyDownEvent>(OnCardKeyDown);
        });
    }

    private void OnFormatCardClickEvent(ClickEvent evt)
    {
        var element = evt.currentTarget as VisualElement;
        if (element?.userData is int index)
        {
            OnFormatCardClicked(index);
        }
    }

    private void OnCardKeyDown(KeyDownEvent evt)
    {
        if (evt.keyCode != KeyCode.Return && evt.keyCode != KeyCode.Space) return;
        var element = evt.currentTarget as VisualElement;
        if (element == null) return;

        using var clickEvt = ClickEvent.GetPooled();
        clickEvt.target = element;
        element.SendEvent(clickEvt);
        evt.StopPropagation();
    }

    private void BindPlatformCards()
    {
        var options = _viewModel.PlatformOptions;
        _platformCardPool.UpdateList(options, (element, option) =>
        {
            int index = options.IndexOf(option);
            var title = element.Q<Label>("card-title");
            var desc = element.Q<Label>("card-desc");
            var meta = element.Q<VisualElement>("card-meta");
            var toggle = element.Q<Toggle>("platform-toggle");
            var shareFill = element.Q<VisualElement>("share-fill");

            if (title != null) title.text = option.DisplayName;
            if (desc != null) desc.text = $"Owner: {option.OwnerText}";

            if (meta != null)
            {
                meta.Clear();
                AddMetaRow(meta, "Quality Ceiling", $"{option.QualityCeiling:F0}%");
                AddMetaRow(meta, "Licensing", $"{option.LicensingRate:P0}");
            }

            bool isSelected = _viewModel.SelectedPlatformIndices.Contains(index);

            if (toggle != null)
            {
                toggle.SetValueWithoutNotify(isSelected);

                // Remove old callback by unregistering then re-registering
                toggle.UnregisterValueChangedCallback(OnPlatformToggleChanged);
                toggle.userData = index;
                toggle.RegisterValueChangedCallback(OnPlatformToggleChanged);
            }

            if (shareFill != null)
                shareFill.style.width = new Length(Mathf.Clamp(option.MarketSharePercent, 0f, 100f), LengthUnit.Percent);

            element.EnableInClassList("pcw__card--selected", isSelected);
        });

        // Update warning
        if (_multiPlatformWarning != null)
        {
            string warning = _viewModel.MultiPlatformWarningText;
            _multiPlatformWarning.text = warning;
            _multiPlatformWarning.EnableInClassList("pcw__warning-label--visible", !string.IsNullOrEmpty(warning));
        }
    }

    private void OnPlatformToggleChanged(ChangeEvent<bool> evt)
    {
        var toggle = evt.target as Toggle;
        if (toggle?.userData is int index)
        {
            OnPlatformToggled(index);
        }
    }

    // ── Feature Step ────────────────────────────────────────────────────────

    private static VisualElement CreateFeatureCard()
    {
        var card = new VisualElement();
        card.AddToClassList("pcw__feat-card");

        var toggle = new Toggle();
        toggle.AddToClassList("pcw__feat-card-toggle");
        toggle.name = "feat-toggle";
        card.Add(toggle);

        var body = new VisualElement();
        body.AddToClassList("pcw__feat-card-body");
        card.Add(body);

        var header = new VisualElement();
        header.AddToClassList("pcw__feat-card-header");
        body.Add(header);

        var name = new Label();
        name.AddToClassList("pcw__feat-card-name");
        name.name = "feat-name";
        header.Add(name);

        var demandPill = new Label();
        demandPill.AddToClassList("pcw__feat-demand-pill");
        demandPill.name = "feat-demand-pill";
        header.Add(demandPill);

        var scopeBadge = new Label();
        scopeBadge.AddToClassList("pcw__feat-scope-badge");
        scopeBadge.name = "feat-scope-badge";
        header.Add(scopeBadge);

        var desc = new Label();
        desc.AddToClassList("pcw__feat-card-desc");
        desc.name = "feat-desc";
        body.Add(desc);

        var tags = new VisualElement();
        tags.AddToClassList("pcw__feat-card-tags");
        tags.name = "feat-tags";
        body.Add(tags);

        var lockReason = new Label();
        lockReason.AddToClassList("pcw__feat-lock-reason");
        lockReason.name = "feat-lock-reason";
        lockReason.style.display = DisplayStyle.None;
        body.Add(lockReason);

        return card;
    }

    private void BindFeatureStep()
    {
        RebuildFeatureCategoryTabs();

        var features = _viewModel.FilteredFeatures;
        _featureCardPool.UpdateList(features, (element, option) =>
        {
            int idx = features.IndexOf(option);
            var toggle = element.Q<Toggle>("feat-toggle");
            var nameLabel = element.Q<Label>("feat-name");
            var demandPill = element.Q<Label>("feat-demand-pill");
            var scopeBadge = element.Q<Label>("feat-scope-badge");
            var descLabel = element.Q<Label>("feat-desc");
            var tags = element.Q<VisualElement>("feat-tags");
            var lockReason = element.Q<Label>("feat-lock-reason");

            if (nameLabel != null) nameLabel.text = option.DisplayName;
            if (descLabel != null) descLabel.text = option.Description;

            if (demandPill != null)
            {
                demandPill.text = option.DemandLabel;
                demandPill.RemoveFromClassList("pcw__feat-demand--expected");
                demandPill.RemoveFromClassList("pcw__feat-demand--trending");
                demandPill.RemoveFromClassList("pcw__feat-demand--cutting-edge");
                demandPill.RemoveFromClassList("pcw__feat-demand--fading");
                demandPill.RemoveFromClassList("pcw__feat-demand--outdated");
                demandPill.style.display = string.IsNullOrEmpty(option.DemandLabel) ? DisplayStyle.None : DisplayStyle.Flex;
                switch (option.DemandStage)
                {
                    case FeatureDemandStage.Standard: demandPill.AddToClassList("pcw__feat-demand--expected"); break;
                    case FeatureDemandStage.Growing: demandPill.AddToClassList("pcw__feat-demand--trending"); break;
                    case FeatureDemandStage.Emerging: demandPill.AddToClassList("pcw__feat-demand--cutting-edge"); break;
                    case FeatureDemandStage.Declining: demandPill.AddToClassList("pcw__feat-demand--fading"); break;
                    case FeatureDemandStage.Legacy: demandPill.AddToClassList("pcw__feat-demand--outdated"); break;
                }
            }

            if (scopeBadge != null)
            {
                scopeBadge.text = option.ScopeCost > 0 ? $"${option.ScopeCost:N0}" : "";
                scopeBadge.style.display = option.ScopeCost > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            }

            if (tags != null)
            {
                tags.Clear();
                if (option.RequiredSkill.HasValue && option.RequiredSkillPoints > 0)
                {
                    var tag = new Label($"{option.RequiredSkill.Value} {option.RequiredSkillPoints}+");
                    tag.AddToClassList("pcw__feat-skill-tag");
                    tags.Add(tag);
                }

                var synTags = option.SynergyTags;
                if (synTags != null)
                {
                    for (int s = 0; s < synTags.Length && s < 2; s++)
                    {
                        if (string.IsNullOrEmpty(synTags[s])) continue;
                        var synTag = new Label(synTags[s]);
                        synTag.AddToClassList("pcw__feat-synergy-tag");
                        tags.Add(synTag);
                    }
                }

                var riskTags = option.RiskTags;
                if (riskTags != null)
                {
                    for (int r = 0; r < riskTags.Length; r++)
                    {
                        if (string.IsNullOrEmpty(riskTags[r])) continue;
                        var riskTag = new Label(riskTags[r]);
                        riskTag.AddToClassList("pcw__feat-risk-tag");
                        tags.Add(riskTag);
                    }
                }

                if (!string.IsNullOrEmpty(option.ReviewImpact))
                {
                    var reviewTag = new Label(option.ReviewImpact);
                    reviewTag.AddToClassList("pcw__feat-review-tag");
                    reviewTag.EnableInClassList("pcw__feat-review-tag--positive", option.ReviewImpact.StartsWith("+"));
                    reviewTag.EnableInClassList("pcw__feat-review-tag--negative", option.ReviewImpact.StartsWith("–"));
                    tags.Add(reviewTag);
                }
            }

            if (lockReason != null)
            {
                lockReason.text = option.LockReason ?? "";
                lockReason.style.display = option.IsLocked ? DisplayStyle.Flex : DisplayStyle.None;
            }

            element.EnableInClassList("pcw__feat-card--locked", option.IsLocked);
            element.EnableInClassList("pcw__feat-card--selected", option.IsSelected);

            if (toggle != null)
            {
                toggle.SetValueWithoutNotify(option.IsSelected);
                toggle.SetEnabled(!option.IsLocked);
                toggle.UnregisterValueChangedCallback(OnFeatureToggleChanged);
                toggle.userData = option.FeatureId;
                toggle.RegisterValueChangedCallback(OnFeatureToggleChanged);
            }
        });

        // Summary
        if (_featSelectedCount != null) _featSelectedCount.text = _viewModel.FeatureSelectedCount.ToString();
        if (_featScopeTotal != null) _featScopeTotal.text = _viewModel.FeatureScopeTotal > 0 ? $"${_viewModel.FeatureScopeTotal:N0}" : "$0";
        if (_featSynergyScore != null) _featSynergyScore.text = _viewModel.FeatureSynergyScore;

        if (_featMissingWarning != null)
        {
            string missing = _viewModel.FeatureMissingExpected;
            _featMissingWarning.text = missing;
            _featMissingWarning.EnableInClassList("pcw__feat-missing-warning--visible", !string.IsNullOrEmpty(missing));
        }

        if (_featConflictWarning != null)
        {
            string conflict = _viewModel.FeatureConflictWarning;
            _featConflictWarning.text = conflict;
            _featConflictWarning.EnableInClassList("pcw__feat-conflict-warning--visible", !string.IsNullOrEmpty(conflict));
        }
    }

    private void RebuildFeatureCategoryTabs()
    {
        if (_featureCategoryTabs == null) return;
        _featureCategoryTabs.Clear();

        var categories = _viewModel.FeatureCategories;
        for (int i = 0; i < categories.Count; i++)
        {
            string cat = categories[i];
            var tab = new Label(cat);
            tab.AddToClassList("pcw__feat-tab");
            tab.EnableInClassList("pcw__feat-tab--active", cat == _viewModel.ActiveFeatureCategory);
            tab.userData = cat;
            tab.RegisterCallback<ClickEvent>(OnFeatureCategoryTabClicked);
            _featureCategoryTabs.Add(tab);
        }
    }

    private void OnFeatureCategoryTabClicked(ClickEvent evt)
    {
        var tab = evt.currentTarget as Label;
        if (tab?.userData is string category)
        {
            _viewModel?.SetFeatureCategoryFilter(category);
            RefreshAll();
        }
    }

    private void OnFeatureToggleChanged(ChangeEvent<bool> evt)
    {
        var toggle = evt.target as Toggle;
        if (toggle?.userData is string featureId)
        {
            _viewModel?.ToggleFeature(featureId);
            RefreshAll();
        }
    }

    // ── Hardware Step ────────────────────────────────────────────────────────

    private void BindHardwareStep()
    {
        if (_viewModel == null) return;

        if (_hwProcessingDropdown != null)
            _hwProcessingDropdown.SetValueWithoutNotify(((int)_viewModel.HwProcessingTier).ToString());
        if (_hwGraphicsDropdown != null)
            _hwGraphicsDropdown.SetValueWithoutNotify(((int)_viewModel.HwGraphicsTier).ToString());
        if (_hwMemoryDropdown != null)
            _hwMemoryDropdown.SetValueWithoutNotify(((int)_viewModel.HwMemoryTier).ToString());
        if (_hwStorageDropdown != null)
            _hwStorageDropdown.SetValueWithoutNotify(((int)_viewModel.HwStorageTier).ToString());
        if (_hwFormFactorDropdown != null)
            _hwFormFactorDropdown.SetValueWithoutNotify(((int)_viewModel.HwFormFactor).ToString());

        if (_hwPerformanceScore != null) _hwPerformanceScore.text = $"{_viewModel.HwPerformanceScore}/100";
        if (_hwManufactureCost != null) _hwManufactureCost.text = $"${_viewModel.HwManufactureCost:N0}";
        if (_hwDevCost != null) _hwDevCost.text = $"${_viewModel.HwDevCostAdd:N0}";

        BindHwRiskLabel(_hwThermalRisk, _viewModel.HwThermalRisk);
        BindHwRiskLabel(_hwDefectRisk, _viewModel.HwDefectRisk);
        if (_hwReliability != null) _hwReliability.text = _viewModel.HwReliability;
        if (_hwDevFriendliness != null) _hwDevFriendliness.text = _viewModel.HwDevFriendliness;

        if (_hwWarningsContainer != null)
        {
            _hwWarningsContainer.Clear();
            if (_viewModel.HwThermalRisk == "High")
                AddHwWarning(_hwWarningsContainer, "High thermal risk — may require expensive cooling solutions.");
            if (_viewModel.HwDefectRisk == "Medium" || _viewModel.HwDefectRisk == "High")
                AddHwWarning(_hwWarningsContainer, "Elevated defect risk — increase QA budget.");
        }
    }

    private static void BindHwRiskLabel(Label label, string risk)
    {
        if (label == null) return;
        label.text = risk;
        label.RemoveFromClassList("pcw__hw-metric-val--risk-low");
        label.RemoveFromClassList("pcw__hw-metric-val--risk-medium");
        label.RemoveFromClassList("pcw__hw-metric-val--risk-high");
        switch (risk)
        {
            case "Low": label.AddToClassList("pcw__hw-metric-val--risk-low"); break;
            case "Medium": label.AddToClassList("pcw__hw-metric-val--risk-medium"); break;
            case "High": label.AddToClassList("pcw__hw-metric-val--risk-high"); break;
        }
    }

    private static void AddHwWarning(VisualElement container, string message)
    {
        var warn = new Label(message);
        warn.AddToClassList("pcw__hw-warning-card");
        container.Add(warn);
    }

    private void OnProcessingTierChanged(ChangeEvent<string> evt)
    {
        if (_viewModel == null || _hwProcessingDropdown == null) return;
        _viewModel.SetProcessingTier((HardwareTier)_hwProcessingDropdown.index);
        BindHardwareStep();
        BindForecast();
        BindRiskBar();
    }

    private void OnGraphicsTierChanged(ChangeEvent<string> evt)
    {
        if (_viewModel == null || _hwGraphicsDropdown == null) return;
        _viewModel.SetGraphicsTier((HardwareTier)_hwGraphicsDropdown.index);
        BindHardwareStep();
        BindForecast();
        BindRiskBar();
    }

    private void OnMemoryTierChanged(ChangeEvent<string> evt)
    {
        if (_viewModel == null || _hwMemoryDropdown == null) return;
        _viewModel.SetMemoryTier((HardwareTier)_hwMemoryDropdown.index);
        BindHardwareStep();
        BindForecast();
        BindRiskBar();
    }

    private void OnStorageTierChanged(ChangeEvent<string> evt)
    {
        if (_viewModel == null || _hwStorageDropdown == null) return;
        _viewModel.SetStorageTier((HardwareTier)_hwStorageDropdown.index);
        BindHardwareStep();
        BindForecast();
        BindRiskBar();
    }

    private void OnFormFactorChanged(ChangeEvent<string> evt)
    {
        if (_viewModel == null || _hwFormFactorDropdown == null) return;
        _viewModel.SetFormFactor((ConsoleFormFactor)_hwFormFactorDropdown.index);
        BindHardwareStep();
        BindForecast();
        BindRiskBar();
    }

    // ── Team Step ────────────────────────────────────────────────────────────

    private static VisualElement CreateTeamSlotCard()
    {
        var card = new VisualElement();
        card.AddToClassList("pcw__team-slot-card");

        var title = new Label();
        title.AddToClassList("pcw__team-slot-title");
        title.name = "slot-title";
        card.Add(title);

        var dropdown = new DropdownField();
        dropdown.AddToClassList("pcw__team-slot-dropdown");
        dropdown.name = "slot-dropdown";
        card.Add(dropdown);

        var skillLabel = new Label("Skill Match");
        skillLabel.AddToClassList("pcw__team-slot-section-label");
        card.Add(skillLabel);

        var skillBarBg = new VisualElement();
        skillBarBg.AddToClassList("pcw__team-skill-bar-bg");
        var skillBarFill = new VisualElement();
        skillBarFill.AddToClassList("pcw__team-skill-bar-fill");
        skillBarFill.name = "skill-fill";
        skillBarBg.Add(skillBarFill);
        card.Add(skillBarBg);

        var matchLabel = new Label();
        matchLabel.AddToClassList("pcw__team-skill-match-label");
        matchLabel.name = "match-label";
        card.Add(matchLabel);

        var meterLabel = new Label("Team Meters");
        meterLabel.AddToClassList("pcw__team-slot-section-label");
        card.Add(meterLabel);

        var moraleRow = new VisualElement();
        moraleRow.AddToClassList("pcw__team-meter-row");
        var moraleKey = new Label("Morale");
        moraleKey.AddToClassList("pcw__team-meter-key");
        var moraleVal = new Label("--");
        moraleVal.AddToClassList("pcw__team-meter-val");
        moraleVal.name = "morale-val";
        moraleRow.Add(moraleKey);
        moraleRow.Add(moraleVal);
        card.Add(moraleRow);

        var energyRow = new VisualElement();
        energyRow.AddToClassList("pcw__team-meter-row");
        var energyKey = new Label("Energy");
        energyKey.AddToClassList("pcw__team-meter-key");
        var energyVal = new Label("--");
        energyVal.AddToClassList("pcw__team-meter-val");
        energyVal.name = "energy-val";
        energyRow.Add(energyKey);
        energyRow.Add(energyVal);
        card.Add(energyRow);

        var chemRow = new VisualElement();
        chemRow.AddToClassList("pcw__team-meter-row");
        var chemKey = new Label("Chemistry");
        chemKey.AddToClassList("pcw__team-meter-key");
        var chemVal = new Label("--");
        chemVal.AddToClassList("pcw__team-meter-val");
        chemVal.name = "chem-val";
        chemRow.Add(chemKey);
        chemRow.Add(chemVal);
        card.Add(chemRow);

        var missingSkillsLabel = new Label("Missing Skills");
        missingSkillsLabel.AddToClassList("pcw__team-slot-section-label");
        card.Add(missingSkillsLabel);

        var missingSkillsChips = new VisualElement();
        missingSkillsChips.AddToClassList("pcw__team-missing-skills-chips");
        missingSkillsChips.name = "missing-skills-chips";
        card.Add(missingSkillsChips);

        var warnBanner = new Label();
        warnBanner.AddToClassList("pcw__team-warning-banner");
        warnBanner.name = "warn-banner";
        card.Add(warnBanner);

        return card;
    }

    private static VisualElement CreateTeamSuggestionCard()
    {
        var card = new VisualElement();
        card.AddToClassList("pcw__team-suggestion-card");

        var slotLabel = new Label();
        slotLabel.AddToClassList("pcw__team-suggestion-slot");
        slotLabel.name = "sug-slot";
        card.Add(slotLabel);

        var nameLabel = new Label();
        nameLabel.AddToClassList("pcw__team-suggestion-name");
        nameLabel.name = "sug-name";
        card.Add(nameLabel);

        var scoreLabel = new Label();
        scoreLabel.AddToClassList("pcw__team-suggestion-score");
        scoreLabel.name = "sug-score";
        card.Add(scoreLabel);

        return card;
    }

    private void BindTeamStep()
    {
        if (_viewModel == null) return;

        var slots = _viewModel.TeamSlots;
        var availableTeams = _viewModel.AvailableTeams;

        var dropdownChoices = new List<string> { "— None —" };
        for (int t = 0; t < availableTeams.Count; t++)
            dropdownChoices.Add($"{availableTeams[t].Name} ({availableTeams[t].StatusText})");

        _teamSlotsListCache.Clear();
        for (int i = 0; i < slots.Length; i++)
            _teamSlotsListCache.Add(slots[i]);

        _teamSlotPool.UpdateList(_teamSlotsListCache, (element, slot) =>
        {
            int slotIndex = _teamSlotsListCache.IndexOf(slot);

            var title = element.Q<Label>("slot-title");
            var dropdown = element.Q<DropdownField>("slot-dropdown");
            var skillFill = element.Q<VisualElement>("skill-fill");
            var matchLabel = element.Q<Label>("match-label");
            var moraleVal = element.Q<Label>("morale-val");
            var energyVal = element.Q<Label>("energy-val");
            var chemVal = element.Q<Label>("chem-val");
            var missingSkillsChips = element.Q<VisualElement>("missing-skills-chips");
            var warnBanner = element.Q<Label>("warn-banner");

            if (title != null) title.text = slot.SlotName ?? $"Slot {slotIndex + 1}";

            if (dropdown != null)
            {
                dropdown.choices = dropdownChoices;

                int selectedIdx = 0;
                if (slot.AssignedTeamId.HasValue)
                {
                    for (int t = 0; t < availableTeams.Count; t++)
                    {
                        if (availableTeams[t].Id == slot.AssignedTeamId.Value)
                        {
                            selectedIdx = t + 1;
                            break;
                        }
                    }
                }
                dropdown.SetValueWithoutNotify(dropdownChoices[selectedIdx]);
                dropdown.UnregisterValueChangedCallback(OnTeamSlotDropdownChanged);
                dropdown.userData = slotIndex;
                dropdown.RegisterValueChangedCallback(OnTeamSlotDropdownChanged);
            }

            if (skillFill != null)
            {
                float matchPct = Mathf.Clamp(slot.SkillMatch, 0, 100);
                skillFill.style.width = new Length(matchPct, LengthUnit.Percent);
                skillFill.RemoveFromClassList("pcw__team-skill-bar-fill--low");
                skillFill.RemoveFromClassList("pcw__team-skill-bar-fill--medium");
                if (slot.SkillMatch < 40) skillFill.AddToClassList("pcw__team-skill-bar-fill--low");
                else if (slot.SkillMatch < 75) skillFill.AddToClassList("pcw__team-skill-bar-fill--medium");
            }

            if (matchLabel != null) matchLabel.text = slot.AssignedTeamId.HasValue ? $"{slot.SkillMatch}% match" : "No team assigned";

            if (moraleVal != null) moraleVal.text = slot.AssignedTeamId.HasValue ? $"{slot.MoraleValue}%" : "--";
            if (energyVal != null) energyVal.text = slot.AssignedTeamId.HasValue ? $"{slot.EnergyValue}%" : "--";
            if (chemVal != null) chemVal.text = slot.AssignedTeamId.HasValue ? $"{slot.ChemistryValue}%" : "--";

            if (missingSkillsChips != null)
            {
                missingSkillsChips.Clear();
                string[] missing = slot.MissingSkillNames;
                if (missing != null && missing.Length > 0 && slot.AssignedTeamId.HasValue)
                {
                    for (int ms = 0; ms < missing.Length; ms++)
                    {
                        if (string.IsNullOrEmpty(missing[ms])) continue;
                        var chip = new Label(missing[ms]);
                        chip.AddToClassList("pcw__team-missing-skill-chip");
                        missingSkillsChips.Add(chip);
                    }
                }
                else
                {
                    var noneLabel = new Label(slot.AssignedTeamId.HasValue ? "None" : "--");
                    noneLabel.AddToClassList("pcw__team-slot-section-label");
                    missingSkillsChips.Add(noneLabel);
                }
            }

            if (warnBanner != null)
            {
                string[] warnings = slot.Warnings;
                string warnText = warnings != null && warnings.Length > 0 ? warnings[0] : "";
                warnBanner.text = warnText;
                warnBanner.EnableInClassList("pcw__team-warning-banner--visible", !string.IsNullOrEmpty(warnText));
            }
        });

        if (_teamOverallReadiness != null) _teamOverallReadiness.text = _viewModel.TeamOverallReadiness;
        if (_teamTotalSalary != null) _teamTotalSalary.text = _viewModel.TeamTotalSalaryCost;

        if (_teamMissingCoverage != null)
        {
            string missing = _viewModel.TeamMissingCoverage;
            _teamMissingCoverage.text = missing;
            _teamMissingCoverage.EnableInClassList("pcw__team-missing-coverage--visible", !string.IsNullOrEmpty(missing));
        }

        var suggestions = _viewModel.TeamSuggestions;
        _teamSuggestionPool.UpdateList(suggestions, (element, sug) =>
        {
            var slotLabel = element.Q<Label>("sug-slot");
            var nameLabel = element.Q<Label>("sug-name");
            var scoreLabel = element.Q<Label>("sug-score");

            if (slotLabel != null) slotLabel.text = sug.SlotName;
            if (nameLabel != null) nameLabel.text = sug.TeamName;
            if (scoreLabel != null) scoreLabel.text = $"Match: {sug.MatchScore}%";
        });
    }

    private void OnTeamSlotDropdownChanged(ChangeEvent<string> evt)
    {
        var dropdown = evt.target as DropdownField;
        if (dropdown?.userData is int slotIndex && _viewModel != null)
        {
            int dropdownIdx = dropdown.index;
            if (dropdownIdx <= 0)
            {
                _viewModel.ClearTeamSlot(slotIndex);
            }
            else
            {
                int teamIdx = dropdownIdx - 1;
                var teams = _viewModel.AvailableTeams;
                if (teamIdx >= 0 && teamIdx < teams.Count)
                    _viewModel.AssignTeamToSlot(slotIndex, teams[teamIdx].Id);
            }
            RefreshAll();
        }
    }

    // ── Budget Step ──────────────────────────────────────────────────────────

    private void OnBudgetNameChanged(ChangeEvent<string> evt)
    {
        _viewModel?.SetProductName(evt.newValue);
        BindBudgetStep();
        UpdateFooterButtons();
    }

    private void OnPriceChanged(ChangeEvent<int> evt)
    {
        _viewModel?.SetTargetPrice(evt.newValue);
        BindBudgetPreviewCards();
    }

    private void OnDistributionChanged(ChangeEvent<string> evt)
    {
        if (_viewModel == null || _distributionDropdown == null) return;
        _viewModel.SetDistributionModel(_distributionDropdown.index);
        BindBudgetPreviewCards();
    }

    private void OnMarketingLowClicked()
    {
        _viewModel?.SetMarketingBudget(0);
        UpdateMarketingButtons();
        BindBudgetPreviewCards();
    }

    private void OnMarketingMediumClicked()
    {
        _viewModel?.SetMarketingBudget(1);
        UpdateMarketingButtons();
        BindBudgetPreviewCards();
    }

    private void OnMarketingHighClicked()
    {
        _viewModel?.SetMarketingBudget(2);
        UpdateMarketingButtons();
        BindBudgetPreviewCards();
    }

    private void UpdateMarketingButtons()
    {
        if (_viewModel == null) return;
        int level = _viewModel.MarketingBudgetLevel;
        if (_marketingLowBtn != null) _marketingLowBtn.EnableInClassList("pcw__budget-mkt-btn--active", level == 0);
        if (_marketingMediumBtn != null) _marketingMediumBtn.EnableInClassList("pcw__budget-mkt-btn--active", level == 1);
        if (_marketingHighBtn != null) _marketingHighBtn.EnableInClassList("pcw__budget-mkt-btn--active", level == 2);
    }

    private void BindBudgetStep()
    {
        if (_viewModel == null) return;

        if (_budgetNameInput != null)
            _budgetNameInput.SetValueWithoutNotify(_viewModel.Draft.ProductName ?? "");

        if (_budgetNameError != null)
        {
            _budgetNameError.text = _viewModel.NameError;
            _budgetNameError.EnableInClassList("pcw__budget-name-error--visible", !_viewModel.IsNameValid && !string.IsNullOrEmpty(_viewModel.NameError));
        }

        if (_budgetNameInput != null)
            _budgetNameInput.EnableInClassList("pcw__budget-name-field--error", !_viewModel.IsNameValid);

        if (_priceSlider != null)
            _priceSlider.SetValueWithoutNotify(_viewModel.TargetPrice);

        if (_distributionDropdown != null)
            _distributionDropdown.SetValueWithoutNotify(ProductCreationPlanningViewModel.GetDistributionModelLabels()[(int)_viewModel.SelectedDistributionModel]);

        UpdateMarketingButtons();
        BindBudgetPreviewCards();
    }

    private void BindBudgetPreviewCards()
    {
        if (_viewModel == null) return;

        if (_previewUpfrontCost != null) _previewUpfrontCost.text = _viewModel.UpfrontCostPreview;
        if (_previewMonthlyBurn != null) _previewMonthlyBurn.text = _viewModel.MonthlyBurnPreview;
        if (_previewTotalCost != null) _previewTotalCost.text = _viewModel.TotalEstimatedCostPreview;
        if (_previewRunway != null) _previewRunway.text = _viewModel.RunwayAfterStartPreview;
        if (_previewBreakEven != null) _previewBreakEven.text = _viewModel.BreakEvenEstimatePreview;
        if (_pricingYourPrice != null) _pricingYourPrice.text = $"${_viewModel.TargetPrice}";
        if (_pricingMarketExpect != null) _pricingMarketExpect.text = _viewModel.MarketExpectation;
        if (_pricingValueRisk != null) _pricingValueRisk.text = _viewModel.ValueRisk;
        if (_pricingCompetitor != null) _pricingCompetitor.text = _viewModel.CompetitorPriceComparison;
    }

    // ── Review Step ──────────────────────────────────────────────────────────

    private void BindReviewStep()
    {
        if (_viewModel == null) return;

        // Product Summary
        if (_revProductName != null) _revProductName.text = string.IsNullOrWhiteSpace(_viewModel.Draft.ProductName) ? "—" : _viewModel.Draft.ProductName;

        string categoryText = "—";
        if (_viewModel.SelectedCategoryIndex >= 0 && _viewModel.SelectedCategoryIndex < _viewModel.CategoryOptions.Count)
            categoryText = _viewModel.CategoryOptions[_viewModel.SelectedCategoryIndex].DisplayName;
        if (_revCategory != null) _revCategory.text = categoryText;

        string nicheText = "—";
        if (_viewModel.SelectedNicheIndex >= 0 && _viewModel.SelectedNicheIndex < _viewModel.NicheOptions.Count)
            nicheText = _viewModel.NicheOptions[_viewModel.SelectedNicheIndex].DisplayName;
        if (_revNiche != null) _revNiche.text = nicheText;

        string genreText = "—";
        if (_viewModel.ShowGenreStep && _viewModel.SelectedGenreIndex >= 0 && _viewModel.SelectedGenreIndex < _viewModel.GenreOptions.Count)
            genreText = _viewModel.GenreOptions[_viewModel.SelectedGenreIndex].DisplayName;
        if (_revGenre != null) _revGenre.text = genreText;

        string platformsText = "—";
        var platIndices = _viewModel.SelectedPlatformIndices;
        if (platIndices.Count > 0)
        {
            var platNames = new System.Text.StringBuilder();
            for (int i = 0; i < platIndices.Count; i++)
            {
                int idx = platIndices[i];
                if (idx >= 0 && idx < _viewModel.PlatformOptions.Count)
                {
                    if (i > 0) platNames.Append(", ");
                    platNames.Append(_viewModel.PlatformOptions[idx].DisplayName);
                }
            }
            if (platNames.Length > 0) platformsText = platNames.ToString();
        }
        if (_revPlatforms != null) _revPlatforms.text = platformsText;
        if (_revFeaturesCount != null) _revFeaturesCount.text = _viewModel.FeatureSelectedCount.ToString();

        // Market Summary
        if (_viewModel.SelectedNicheIndex >= 0 && _viewModel.SelectedNicheIndex < _viewModel.NicheOptions.Count)
        {
            var niche = _viewModel.NicheOptions[_viewModel.SelectedNicheIndex];
            if (_revMarketDemand != null) _revMarketDemand.text = niche.DemandText;
            if (_revMarketCompetition != null) _revMarketCompetition.text = niche.CompetitionText;
            if (_revMarketFit != null) _revMarketFit.text = _viewModel.MarketFitRange;
            if (_revMarketTrend != null) _revMarketTrend.text = niche.TrendText;
        }
        else
        {
            if (_revMarketDemand != null) _revMarketDemand.text = "—";
            if (_revMarketCompetition != null) _revMarketCompetition.text = "—";
            if (_revMarketFit != null) _revMarketFit.text = "—";
            if (_revMarketTrend != null) _revMarketTrend.text = "—";
        }

        // Cost & Duration
        if (_revCostRange != null) _revCostRange.text = _viewModel.CostRange;
        if (_revDurationRange != null) _revDurationRange.text = _viewModel.DurationRange;
        if (_revMonthlyBurn != null) _revMonthlyBurn.text = _viewModel.MonthlyBurnPreview;

        // Team Readiness slots
        BindReviewTeamSlots();
        if (_revTeamReadiness != null) _revTeamReadiness.text = _viewModel.TeamOverallReadiness;

        // Quality Forecast
        BindReviewForecastMetric(_revQualityRange, _viewModel.QualityRange);
        BindReviewForecastMetric(_revInnovationRange, _viewModel.InnovationRange);
        BindReviewForecastMetric(_revBugRisk, _viewModel.BugRisk);
        BindReviewForecastMetric(_revScopeRisk, _viewModel.ScopeRisk);
        if (_revConfidence != null) _revConfidence.text = _viewModel.Confidence;

        // Validation
        BindReviewValidation();

        // Start Development button
        if (_btnStartDevelopment != null)
        {
            _btnStartDevelopment.SetEnabled(_viewModel.CanConfirm);
            _btnStartDevelopment.EnableInClassList("pcw__btn--disabled", !_viewModel.CanConfirm);
        }
    }

    private void BindReviewTeamSlots()
    {
        if (_reviewTeamSlotsContainer == null || _viewModel == null) return;
        _reviewTeamSlotsContainer.Clear();

        var slots = _viewModel.TeamSlots;
        for (int i = 0; i < slots.Length; i++)
        {
            var slot = slots[i];
            var row = new VisualElement();
            row.AddToClassList("pcw__review-team-slot-row");

            var nameLabel = new Label(slot.SlotName ?? $"Slot {i + 1}");
            nameLabel.AddToClassList("pcw__review-team-slot-name");
            row.Add(nameLabel);

            var teamLabel = new Label(slot.AssignedTeamId.HasValue ? (slot.AssignedTeamName ?? "Unknown") : "None");
            teamLabel.AddToClassList("pcw__review-team-slot-team");
            row.Add(teamLabel);

            var matchLabel = new Label(slot.AssignedTeamId.HasValue ? $"{slot.SkillMatch}%" : "—");
            matchLabel.AddToClassList("pcw__review-team-slot-match");
            row.Add(matchLabel);

            _reviewTeamSlotsContainer.Add(row);
        }
    }

    private static void BindReviewForecastMetric(Label label, string value)
    {
        if (label == null) return;
        label.text = value;
        label.RemoveFromClassList("pcw__review-metric-val--good");
        label.RemoveFromClassList("pcw__review-metric-val--warn");
        label.RemoveFromClassList("pcw__review-metric-val--bad");
        switch (value)
        {
            case "High": label.AddToClassList("pcw__review-metric-val--good"); break;
            case "Medium": label.AddToClassList("pcw__review-metric-val--warn"); break;
            case "Low": label.AddToClassList("pcw__review-metric-val--warn"); break;
        }
    }

    private void BindReviewValidation()
    {
        if (_reviewErrorsContainer != null)
        {
            _reviewErrorsContainer.Clear();
            var errors = _viewModel.BlockingErrors;
            for (int i = 0; i < errors.Count; i++)
            {
                var card = new Label(errors[i]);
                card.AddToClassList("pcw__review-error-card");
                _reviewErrorsContainer.Add(card);
            }
        }

        if (_reviewWarningsContainer != null)
        {
            _reviewWarningsContainer.Clear();
            var warnings = _viewModel.ValidationWarnings;
            for (int i = 0; i < warnings.Count; i++)
            {
                var card = new Label(warnings[i]);
                card.AddToClassList("pcw__review-warning-card");
                _reviewWarningsContainer.Add(card);
            }
        }
    }

    private void OnStartDevelopmentClicked()
    {
        if (_viewModel == null || !_viewModel.CanConfirm) return;

        var cmd = _viewModel.BuildCreateCommand(_dispatcher.CurrentTick);
        _dispatcher.Dispatch(cmd);
        OnProductCreated?.Invoke();
    }

    private void OnReviewModalConfirmed()
    {
        // Reserved for future modal integration.
    }

    // ── Forecast Panel Binding ──────────────────────────────────────────────

    private void BindForecast()
    {
        if (_forecastQuality != null) _forecastQuality.text = _viewModel.QualityRange;
        if (_forecastInnovation != null) _forecastInnovation.text = _viewModel.InnovationRange;
        if (_forecastMarketFit != null) _forecastMarketFit.text = _viewModel.MarketFitRange;
        if (_forecastScopeRisk != null) _forecastScopeRisk.text = _viewModel.ScopeRisk;
        if (_forecastBugRisk != null) _forecastBugRisk.text = _viewModel.BugRisk;
        if (_forecastTechnicalRisk != null) _forecastTechnicalRisk.text = _viewModel.TechnicalRisk;
        if (_forecastCommercialRisk != null) _forecastCommercialRisk.text = _viewModel.CommercialRisk;
        if (_forecastDuration != null) _forecastDuration.text = _viewModel.DurationRange;
        if (_forecastCost != null) _forecastCost.text = _viewModel.CostRange;

        if (_forecastConfidence != null)
        {
            _forecastConfidence.text = _viewModel.Confidence;
            _forecastConfidence.RemoveFromClassList("pcw__forecast-value--low");
            _forecastConfidence.RemoveFromClassList("pcw__forecast-value--medium");
            _forecastConfidence.RemoveFromClassList("pcw__forecast-value--high");

            switch (_viewModel.Confidence)
            {
                case "Low": _forecastConfidence.AddToClassList("pcw__forecast-value--low"); break;
                case "Medium": _forecastConfidence.AddToClassList("pcw__forecast-value--medium"); break;
                case "High": _forecastConfidence.AddToClassList("pcw__forecast-value--high"); break;
            }
        }

        // Diagnostics
        var diagnostics = _viewModel.TopDiagnostics;
        _diagnosticPool.UpdateList(diagnostics, (element, diag) =>
        {
            var title = element.Q<Label>("diag-title");
            var desc = element.Q<Label>("diag-desc");

            if (title != null) title.text = diag.Title;
            if (desc != null) desc.text = diag.Description;

            element.RemoveFromClassList("pcw__diagnostic-card--warning");
            element.RemoveFromClassList("pcw__diagnostic-card--info");
            element.RemoveFromClassList("pcw__diagnostic-card--success");

            switch (diag.Severity)
            {
                case DiagnosticSeverity.Warning: element.AddToClassList("pcw__diagnostic-card--warning"); break;
                case DiagnosticSeverity.Info: element.AddToClassList("pcw__diagnostic-card--info"); break;
                case DiagnosticSeverity.Success: element.AddToClassList("pcw__diagnostic-card--success"); break;
            }
        });
    }

    // ── Risk Bar Binding ────────────────────────────────────────────────────

    private void BindRiskBar()
    {
        if (_riskScope != null) _riskScope.text = _viewModel.ScopeLabel;
        if (_riskCost != null) _riskCost.text = _viewModel.CostLabel;
        if (_riskDuration != null) _riskDuration.text = _viewModel.DurationLabel;
        if (_riskBug != null) _riskBug.text = _viewModel.BugRiskLabel;
        if (_riskMarketFit != null) _riskMarketFit.text = _viewModel.MarketFitLabel;
        if (_riskCoverage != null) _riskCoverage.text = _viewModel.MissingCoverage;
    }

    // ── Footer Button State ─────────────────────────────────────────────────

    private void UpdateFooterButtons()
    {
        if (_btnBack != null)
        {
            _btnBack.SetEnabled(_viewModel.CanGoBack);
            _btnBack.EnableInClassList("pcw__btn--disabled", !_viewModel.CanGoBack);
        }

        if (_btnContinue != null)
        {
            bool canContinue = _viewModel.CanContinue;
            _btnContinue.SetEnabled(canContinue);
            _btnContinue.EnableInClassList("pcw__btn--disabled", !canContinue);

            // Change label on last step
            bool isLastStep = _viewModel.CurrentStep >= _viewModel.TotalSteps - 1;
            _btnContinue.text = isLastStep ? "Start Development" : "Continue";
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static void AddMetaRow(VisualElement container, string key, string value)
    {
        var row = new VisualElement();
        row.AddToClassList("pcw__card-meta-row");

        var keyLabel = new Label(key);
        keyLabel.AddToClassList("pcw__card-meta-key");
        row.Add(keyLabel);

        var valLabel = new Label(value);
        valLabel.AddToClassList("pcw__card-meta-value");
        row.Add(valLabel);

        container.Add(row);
    }
}
