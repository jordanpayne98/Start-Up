using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// View for the multi-step new game creation wizard.
/// Manages step display, footer navigation, card interactions, and preview panel.
/// </summary>
public class NewGameFounderCreationView : IGameView
{
    // ── Events ──
    public event Action OnCancelRequested;

    // ── State ──
    private NewGameFounderCreationViewModel _viewModel;
    private VisualElement _root;

    // ── Shared elements ──
    private Label _stepCounter;
    private VisualElement _progressStepper;
    private VisualElement _mainContentPanel;
    private VisualElement _previewPanel;

    // ── Footer ──
    private Button _btnBack;
    private Button _btnRandomise;
    private Button _btnSaveExit;
    private Button _btnContinue;
    private Label _helperText;

    // ── Step panels (indexed by step) ──
    private readonly List<VisualElement> _stepPanels = new List<VisualElement>();

    // ── Step indicator elements ──
    private readonly List<VisualElement> _stepIndicators = new List<VisualElement>();
    private readonly List<VisualElement> _stepConnectors = new List<VisualElement>();

    // ── Step 1 elements ──
    private TextField _companyNameInput;
    private DropdownField _industrySelector;
    private DropdownField _locationSelector;

    // ── Step 2 elements ──
    private VisualElement _backgroundCardsContainer;
    private readonly List<VisualElement> _backgroundCards = new List<VisualElement>();

    // ── Step 3 elements ──
    private VisualElement _founderCountCards;
    private VisualElement _founderCountComparison;

    // ── Founder step elements (per founder, index 0=Founder1, 1=Founder2) ──
    private readonly VisualElement[] _archetypeCardContainers = new VisualElement[2];
    private readonly List<VisualElement>[] _archetypeCards = { new List<VisualElement>(), new List<VisualElement>() };
    private readonly TextField[] _founderNameInputs = new TextField[2];
    private readonly Button[] _randomiseNameBtns = new Button[2];
    private readonly VisualElement[] _personalityStyleContainers = new VisualElement[2];
    private readonly List<VisualElement>[] _personalityStyleCards = { new List<VisualElement>(), new List<VisualElement>() };
    private readonly VisualElement[] _weaknessContainers = new VisualElement[2];
    private readonly List<VisualElement>[] _weaknessCards = { new List<VisualElement>(), new List<VisualElement>() };
    private readonly Button[][] _salaryButtons = { new Button[4], new Button[4] };
    private readonly Label[] _salaryRecommendations = new Label[2];
    private readonly Label[] _equityExpectations = new Label[2];
    private readonly Label[] _salaryMonthlyCosts = new Label[2];
    private readonly Label[] _salaryRunwayImpacts = new Label[2];
    private readonly Label[] _salaryPressures = new Label[2];
    private readonly Label[] _personalityValidations = new Label[2];
    private readonly Label[] _weaknessValidations = new Label[2];

    // ── Step 6: Team & Budget (removed — step deleted) ──
    // ── Step 7: Company Preview (removed — step deleted) ──

    // ── Step 8: Review ──
    private Label _reviewCompanyName;
    private Label _reviewIndustry;
    private Label _reviewBackground;
    private Label _reviewFoundersSummary;
    private Label _reviewSalaryCost;
    private Label _reviewStartingCash;
    private Label _reviewRunway;
    private Label _reviewTeamStrength;
    private VisualElement _reviewErrorsContainer;
    private VisualElement _reviewWarningsContainer;
    private Button _btnStartGame;

    // ── Start Game callback ──
    public event Action<NewGameSetupState, FoundingEmployeeData[]> OnStartGameConfirmed;
    private Label _founderPreviewName;
    private Label _founderPreviewArchetypeBadge;
    private Label _founderPreviewRole;
    private Label _founderPreviewLocation;
    private VisualElement _founderPreviewTopSkills;
    private VisualElement _founderPreviewCaStars;
    private VisualElement _founderPreviewPaStars;
    private VisualElement _founderPreviewStrengths;
    private VisualElement _founderPreviewRisks;
    private VisualElement _founderPreviewDeptBars;
    private Label _founderPreviewTeamTotal;
    private VisualElement _founderPreviewCompanyDetails;

    // ── Preview elements ──
    private Label _previewPlaceholder;
    private VisualElement _previewBackgroundDetails;
    private Label _previewBgName;
    private Label _previewBgDescription;
    private VisualElement _previewBgRoles;
    private VisualElement _previewBgStrengths;
    private VisualElement _previewBgRisks;
    private Label _previewBgDifficulty;
    private VisualElement _previewFounderCountDetails;
    private Label _previewFcSummary;
    private VisualElement _previewFounderDetails;

    public void Initialize(VisualElement root, UIServices services)
    {
        _root = root;

        // Header
        _stepCounter = _root.Q<Label>("step-counter");

        // Stepper
        _progressStepper = _root.Q<VisualElement>("progress-stepper");
        CacheStepperElements();

        // Body
        _mainContentPanel = _root.Q<VisualElement>("main-content-panel");
        _previewPanel = _root.Q<VisualElement>("preview-panel");

        // Cache step panels in order
        _stepPanels.Clear();
        _stepPanels.Add(_root.Q<VisualElement>("step-company"));                    // 0
        _stepPanels.Add(_root.Q<VisualElement>("step-background"));                 // 1
        _stepPanels.Add(_root.Q<VisualElement>("step-founder-count"));              // 2
        _stepPanels.Add(_root.Q<VisualElement>("step-f1-archetype"));               // 3
        _stepPanels.Add(_root.Q<VisualElement>("step-f1-identity-compensation"));   // 4
        _stepPanels.Add(_root.Q<VisualElement>("step-f1-personality-weakness"));    // 5
        _stepPanels.Add(_root.Q<VisualElement>("step-f2-archetype"));               // 6
        _stepPanels.Add(_root.Q<VisualElement>("step-f2-identity-compensation"));   // 7
        _stepPanels.Add(_root.Q<VisualElement>("step-f2-personality-weakness"));    // 8
        _stepPanels.Add(_root.Q<VisualElement>("step-review"));                     // 9

        // Footer
        _btnBack = _root.Q<Button>("btn-back");
        _btnRandomise = _root.Q<Button>("btn-randomise");
        _btnSaveExit = _root.Q<Button>("btn-save-exit");
        _btnContinue = _root.Q<Button>("btn-continue");
        _helperText = _root.Q<Label>("footer-helper-text");

        // Step 1
        _companyNameInput = _root.Q<TextField>("company-name-input");
        _industrySelector = _root.Q<DropdownField>("industry-selector");
        _locationSelector = _root.Q<DropdownField>("location-selector");

        // Step 2
        _backgroundCardsContainer = _root.Q<VisualElement>("background-cards-container");

        // Step 3
        _founderCountCards = _root.Q<VisualElement>("founder-count-cards");
        _founderCountComparison = _root.Q<VisualElement>("founder-count-comparison");

        // Preview
        _previewPlaceholder = _root.Q<Label>("preview-placeholder");
        _previewBackgroundDetails = _root.Q<VisualElement>("preview-background-details");
        _previewBgName = _root.Q<Label>("preview-bg-name");
        _previewBgDescription = _root.Q<Label>("preview-bg-description");
        _previewBgRoles = _root.Q<VisualElement>("preview-bg-roles");
        _previewBgStrengths = _root.Q<VisualElement>("preview-bg-strengths");
        _previewBgRisks = _root.Q<VisualElement>("preview-bg-risks");
        _previewBgDifficulty = _root.Q<Label>("preview-bg-difficulty");
        _previewFounderCountDetails = _root.Q<VisualElement>("preview-founder-count-details");
        _previewFcSummary = _root.Q<Label>("preview-fc-summary");

        // Wire handlers
        WireFooterHandlers();
        WireStepOneHandlers();
        SetupIndustryAndLocationDropdowns();
        CacheFounderStepElements();
        WireFounderStepHandlers();
        CacheFinalStepElements();
        WireFinalStepHandlers();
    }

    public void Bind(IViewModel viewModel)
    {
        if (_viewModel != null)
        {
            _viewModel.OnStateChanged -= OnViewModelChanged;
        }

        _viewModel = viewModel as NewGameFounderCreationViewModel;
        if (_viewModel == null)
        {
            Debug.LogError("[NewGameFounderCreationView] Bind called with incompatible ViewModel");
            return;
        }

        _viewModel.OnStateChanged += OnViewModelChanged;

        // Build dynamic content
        BuildBackgroundCards();
        BuildFounderCountCards();
        BuildFounderCards();

        // Initial display
        RefreshAll();
    }

    public void Dispose()
    {
        if (_viewModel != null)
        {
            _viewModel.OnStateChanged -= OnViewModelChanged;
            _viewModel = null;
        }

        UnwireFooterHandlers();
        UnwireStepOneHandlers();
        UnwireFounderStepHandlers();
        UnwireFinalStepHandlers();
    }

    // ── Handler Wiring ──

    private void WireFooterHandlers()
    {
        if (_btnBack != null) _btnBack.clicked += OnBackClicked;
        if (_btnRandomise != null) _btnRandomise.clicked += OnRandomiseClicked;
        if (_btnSaveExit != null) _btnSaveExit.clicked += OnSaveExitClicked;
        if (_btnContinue != null) _btnContinue.clicked += OnContinueClicked;
    }

    private void UnwireFooterHandlers()
    {
        if (_btnBack != null) _btnBack.clicked -= OnBackClicked;
        if (_btnRandomise != null) _btnRandomise.clicked -= OnRandomiseClicked;
        if (_btnSaveExit != null) _btnSaveExit.clicked -= OnSaveExitClicked;
        if (_btnContinue != null) _btnContinue.clicked -= OnContinueClicked;
    }

    private void WireStepOneHandlers()
    {
        if (_companyNameInput != null)
            _companyNameInput.RegisterCallback<ChangeEvent<string>>(OnCompanyNameChanged);
        if (_industrySelector != null)
            _industrySelector.RegisterCallback<ChangeEvent<string>>(OnIndustryChanged);
        if (_locationSelector != null)
            _locationSelector.RegisterCallback<ChangeEvent<string>>(OnLocationChanged);
    }

    private void UnwireStepOneHandlers()
    {
        if (_companyNameInput != null)
            _companyNameInput.UnregisterCallback<ChangeEvent<string>>(OnCompanyNameChanged);
        if (_industrySelector != null)
            _industrySelector.UnregisterCallback<ChangeEvent<string>>(OnIndustryChanged);
        if (_locationSelector != null)
            _locationSelector.UnregisterCallback<ChangeEvent<string>>(OnLocationChanged);
    }

    // ── Footer Handlers ──

    private void OnBackClicked()
    {
        if (_viewModel == null) return;

        if (!_viewModel.CanGoBack)
        {
            OnCancelRequested?.Invoke();
            return;
        }

        _viewModel.GoBack();
    }

    private void OnContinueClicked()
    {
        if (_viewModel == null) return;
        _viewModel.GoForward();
    }

    private void OnRandomiseClicked()
    {
        if (_viewModel == null) return;
        _viewModel.RandomiseCurrentStep();
        SyncStepOneFieldsFromViewModel();
    }

    private void OnSaveExitClicked()
    {
        OnCancelRequested?.Invoke();
    }

    // ── Step 1 Handlers ──

    private void OnCompanyNameChanged(ChangeEvent<string> evt)
    {
        if (_viewModel != null) _viewModel.CompanyName = evt.newValue;
    }

    private void OnIndustryChanged(ChangeEvent<string> evt)
    {
        if (_viewModel != null) _viewModel.Industry = evt.newValue;
    }

    private void OnLocationChanged(ChangeEvent<string> evt)
    {
        if (_viewModel != null) _viewModel.Location = evt.newValue;
    }

    // ── Setup ──

    private void SetupIndustryAndLocationDropdowns()
    {
        if (_industrySelector != null)
        {
            _industrySelector.choices = new List<string>
            {
                "Software", "Games", "Enterprise", "Hardware",
                "Design", "Marketing", "Fintech", "Healthcare Tech"
            };
            _industrySelector.index = 0;
        }

        if (_locationSelector != null)
        {
            _locationSelector.choices = new List<string>
            {
                "San Francisco, USA", "London, UK", "Berlin, Germany",
                "Tokyo, Japan", "Singapore", "Sydney, Australia",
                "Toronto, Canada", "Bangalore, India"
            };
            _locationSelector.index = 0;
        }
    }

    // ── Stepper Caching ──

    private void CacheStepperElements()
    {
        _stepIndicators.Clear();
        _stepConnectors.Clear();

        if (_progressStepper == null) return;

        for (int i = 0; i <= 7; i++)
        {
            var indicator = _progressStepper.Q<VisualElement>($"step-indicator-{i}");
            if (indicator != null) _stepIndicators.Add(indicator);
        }

        _progressStepper.Query<VisualElement>(className: "stepper__connector")
            .ForEach(c => _stepConnectors.Add(c));
    }

    // ── Background Cards ──

    private void BuildBackgroundCards()
    {
        if (_backgroundCardsContainer == null || _viewModel == null) return;

        _backgroundCardsContainer.Clear();
        _backgroundCards.Clear();

        var options = _viewModel.BackgroundOptions;
        for (int i = 0; i < options.Length; i++)
        {
            var option = options[i];
            int index = i;

            var card = new VisualElement();
            card.AddToClassList("wizard__card");
            card.userData = index;
            card.focusable = true;

            var title = new Label(option.Name);
            title.AddToClassList("wizard__card-title");
            title.AddToClassList("text--primary");
            card.Add(title);

            var desc = new Label(option.Description);
            desc.AddToClassList("wizard__card-description");
            desc.AddToClassList("text--secondary");
            card.Add(desc);

            var tags = new VisualElement();
            tags.AddToClassList("wizard__card-tags");
            for (int r = 0; r < option.RecommendedRoleNames.Length; r++)
            {
                var tag = new Label(option.RecommendedRoleNames[r]);
                tag.AddToClassList("wizard__card-tag");
                tags.Add(tag);
            }
            card.Add(tags);

            var difficulty = new Label(option.DifficultyLabel);
            difficulty.AddToClassList("wizard__card-difficulty");
            difficulty.AddToClassList("text--muted");
            card.Add(difficulty);

            card.RegisterCallback<ClickEvent>(evt =>
            {
                if (_viewModel != null)
                    _viewModel.SelectBackground(index);
            });
            card.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode == UnityEngine.KeyCode.Return || evt.keyCode == UnityEngine.KeyCode.Space)
                    if (_viewModel != null) _viewModel.SelectBackground(index);
            });

            _backgroundCards.Add(card);
            _backgroundCardsContainer.Add(card);
        }
    }

    // ── Founder Count Cards ──

    private void BuildFounderCountCards()
    {
        if (_founderCountComparison == null || _viewModel == null) return;

        _founderCountComparison.Clear();

        var options = _viewModel.CountOptions;
        for (int i = 0; i < options.Length; i++)
        {
            var option = options[i];
            int count = option.Count;

            var card = new VisualElement();
            card.AddToClassList("wizard__comparison-card");
            card.name = $"fc-card-{count}";
            card.userData = count;

            var title = new Label(option.Label);
            title.AddToClassList("wizard__comparison-title");
            card.Add(title);

            // Pros
            var prosLabel = new Label("Advantages");
            prosLabel.AddToClassList("wizard__comparison-list-label");
            card.Add(prosLabel);

            for (int p = 0; p < option.Pros.Length; p++)
            {
                var item = new Label($"+ {option.Pros[p]}");
                item.AddToClassList("wizard__comparison-item");
                card.Add(item);
            }

            // Cons
            var consLabel = new Label("Trade-offs");
            consLabel.AddToClassList("wizard__comparison-list-label");
            consLabel.AddToClassList("wizard__comparison-list-label--cons");
            card.Add(consLabel);

            for (int c = 0; c < option.Cons.Length; c++)
            {
                var item = new Label($"- {option.Cons[c]}");
                item.AddToClassList("wizard__comparison-item");
                card.Add(item);
            }

            card.RegisterCallback<ClickEvent>(evt =>
            {
                if (_viewModel != null)
                    _viewModel.SelectFounderCount(count);
            });

            _founderCountComparison.Add(card);
        }
    }

    // ── Refresh ──

    private void BuildFounderCards()
    {
        if (_viewModel == null) return;
        for (int fi = 0; fi < 2; fi++)
        {
            BuildArchetypeCards(fi);
        }
    }

    private void OnViewModelChanged()
    {
        RefreshAll();
    }

    private void RefreshAll()
    {
        if (_viewModel == null) return;

        ShowStep(_viewModel.CurrentStep);
        RefreshStepper();
        RefreshFooter();
        RefreshPreviewPanel();
        RefreshBackgroundCardSelection();
        RefreshFounderCountCardSelection();

        int fi = _viewModel.GetFounderIndexForStep(_viewModel.CurrentStep);
        int subStep = _viewModel.GetFounderSubStepForStep(_viewModel.CurrentStep);

        if (fi >= 0)
        {
            switch (subStep)
            {
                case 0:
                    BuildArchetypeCards(fi);
                    RefreshArchetypeSelection(fi);
                    break;
                case 1:
                    SyncFounderNameField(fi);
                    RefreshSalaryButtons(fi);
                    RefreshSalaryPreview(fi);
                    if (_equityExpectations[fi] != null)
                        _equityExpectations[fi].text = _viewModel.GetEquityExpectation(fi);
                    break;
                case 2:
                    BuildPersonalityStyleCards(fi);
                    RefreshPersonalityStyleSelection(fi);
                    BuildWeaknessCards(fi);
                    RefreshWeaknessSelection(fi);
                    RefreshPersonalityWeaknessValidation(fi);
                    break;
            }
        }

        if (_viewModel.IsOnFinalStep)
            BindReviewStep();
    }

    // ── Step Management ──

    private void ShowStep(int stepIndex)
    {
        int panelIndex = GetPanelIndexForStep(stepIndex);

        for (int i = 0; i < _stepPanels.Count; i++)
        {
            if (_stepPanels[i] == null) continue;

            if (i == panelIndex)
                _stepPanels[i].AddToClassList("wizard__step-panel--visible");
            else
                _stepPanels[i].RemoveFromClassList("wizard__step-panel--visible");
        }
    }

    private int GetPanelIndexForStep(int stepIndex)
    {
        if (_viewModel != null && _viewModel.IsOnFinalStep)
            return _stepPanels.Count - 1;
        return stepIndex;
    }

    private void RefreshStepper()
    {
        if (_viewModel == null) return;

        if (_stepCounter != null)
            _stepCounter.text = _viewModel.StepLabel;

        int groupCount = _viewModel.GetStepperGroupCount();
        int activeGroupIndex = _viewModel.GetStepperGroupIndexForStep(_viewModel.CurrentStep);
        string[] groupLabels = _viewModel.StepperGroupLabels;

        for (int i = 0; i < _stepIndicators.Count; i++)
        {
            bool visible = i < groupCount;
            _stepIndicators[i].style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;

            if (visible)
            {
                var label = _stepIndicators[i].Q<Label>(className: "stepper__step-label");
                if (label != null && i < groupLabels.Length) label.text = groupLabels[i];

                var circleLabel = _stepIndicators[i].Q<Label>(className: "stepper__circle-label");
                if (circleLabel != null) circleLabel.text = (i + 1).ToString();
            }

            _stepIndicators[i].RemoveFromClassList("stepper__step--active");
            _stepIndicators[i].RemoveFromClassList("stepper__step--completed");

            if (i == activeGroupIndex)
                _stepIndicators[i].AddToClassList("stepper__step--active");
            else if (i < activeGroupIndex)
                _stepIndicators[i].AddToClassList("stepper__step--completed");
        }

        for (int i = 0; i < _stepConnectors.Count; i++)
        {
            bool visible = i < groupCount - 1;
            _stepConnectors[i].style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;

            _stepConnectors[i].RemoveFromClassList("stepper__connector--completed");
            if (i < activeGroupIndex)
                _stepConnectors[i].AddToClassList("stepper__connector--completed");
        }
    }

    private void RefreshFooter()
    {
        if (_viewModel == null) return;

        bool isReviewStep = _viewModel.IsOnFinalStep;

        // Back button: disabled on step 0 (but still visible — clicking goes to main menu)
        if (_btnBack != null)
            _btnBack.text = _viewModel.CanGoBack ? "Back" : "Cancel";

        // Randomise label
        if (_btnRandomise != null)
            _btnRandomise.text = _viewModel.GetRandomiseLabel();

        // Continue button — hide on final review step (Start Game button in body takes over)
        if (_btnContinue != null)
        {
            _btnContinue.style.display = isReviewStep ? DisplayStyle.None : DisplayStyle.Flex;
            if (!isReviewStep)
            {
                _btnContinue.text = _viewModel.GetContinueLabel();
                _btnContinue.SetEnabled(_viewModel.CanContinue);
            }
        }
    }

    private void RefreshPreviewPanel()
    {
        if (_viewModel == null) return;

        int currentStep = _viewModel.CurrentStep;
        int fi = _viewModel.GetFounderIndexForStep(currentStep);

        bool showBgPreview = currentStep == 1 && _viewModel.SelectedBackgroundIndex >= 0;
        bool showFcPreview = currentStep == 2 && _viewModel.SelectedFounderCount > 0;
        bool showFounderPreview = fi >= 0;

        // Placeholder visibility
        if (_previewPlaceholder != null)
            _previewPlaceholder.style.display = (!showBgPreview && !showFcPreview && !showFounderPreview) ? DisplayStyle.Flex : DisplayStyle.None;

        // Background preview
        if (_previewBackgroundDetails != null)
        {
            if (showBgPreview)
            {
                _previewBackgroundDetails.RemoveFromClassList("wizard__preview-section");
                _previewBackgroundDetails.AddToClassList("wizard__preview-section");
                _previewBackgroundDetails.AddToClassList("wizard__preview-section--visible");
                PopulateBackgroundPreview(_viewModel.BackgroundOptions[_viewModel.SelectedBackgroundIndex]);
            }
            else
            {
                _previewBackgroundDetails.RemoveFromClassList("wizard__preview-section--visible");
            }
        }

        // Founder count preview
        if (_previewFounderCountDetails != null)
        {
            if (showFcPreview)
            {
                _previewFounderCountDetails.RemoveFromClassList("wizard__preview-section");
                _previewFounderCountDetails.AddToClassList("wizard__preview-section");
                _previewFounderCountDetails.AddToClassList("wizard__preview-section--visible");
                PopulateFounderCountPreview();
            }
            else
            {
                _previewFounderCountDetails.RemoveFromClassList("wizard__preview-section--visible");
            }
        }

        // Founder preview panel
        if (_previewFounderDetails != null)
        {
            if (showFounderPreview)
            {
                _previewFounderDetails.RemoveFromClassList("wizard__preview-section");
                _previewFounderDetails.AddToClassList("wizard__preview-section");
                _previewFounderDetails.AddToClassList("wizard__preview-section--visible");
                PopulateFounderPreview(fi);
            }
            else
            {
                _previewFounderDetails.RemoveFromClassList("wizard__preview-section--visible");
            }
        }
    }

    private void PopulateBackgroundPreview(CompanyBackgroundOption option)
    {
        if (_previewBgName != null) _previewBgName.text = option.Name;
        if (_previewBgDescription != null) _previewBgDescription.text = option.Description;

        if (_previewBgRoles != null)
        {
            _previewBgRoles.Clear();
            for (int i = 0; i < option.RecommendedRoleNames.Length; i++)
            {
                var tag = new Label(option.RecommendedRoleNames[i]);
                tag.AddToClassList("wizard__preview-tag");
                _previewBgRoles.Add(tag);
            }
        }

        if (_previewBgStrengths != null)
        {
            _previewBgStrengths.Clear();
            for (int i = 0; i < option.Strengths.Length; i++)
            {
                var item = new Label($"+ {option.Strengths[i]}");
                item.AddToClassList("wizard__preview-list-item");
                item.AddToClassList("wizard__preview-list-item--strength");
                _previewBgStrengths.Add(item);
            }
        }

        if (_previewBgRisks != null)
        {
            _previewBgRisks.Clear();
            for (int i = 0; i < option.Risks.Length; i++)
            {
                var item = new Label($"- {option.Risks[i]}");
                item.AddToClassList("wizard__preview-list-item");
                item.AddToClassList("wizard__preview-list-item--risk");
                _previewBgRisks.Add(item);
            }
        }

        if (_previewBgDifficulty != null) _previewBgDifficulty.text = option.DifficultyLabel;
    }

    private void PopulateFounderCountPreview()
    {
        if (_previewFcSummary == null || _viewModel == null) return;

        if (_viewModel.SelectedFounderCount == 1)
        {
            _previewFcSummary.text = "Starting with 1 founder gives you full control and lower costs, " +
                "but narrower skills. You'll need to hire quickly to cover gaps.";
        }
        else
        {
            _previewFcSummary.text = "Starting with 2 founders provides broader skill coverage and shared workload, " +
                "but increases your monthly salary costs from day one.";
        }
    }

    // ── Card Selection Highlights ──

    private void RefreshBackgroundCardSelection()
    {
        if (_viewModel == null) return;

        for (int i = 0; i < _backgroundCards.Count; i++)
        {
            if (i == _viewModel.SelectedBackgroundIndex)
                _backgroundCards[i].AddToClassList("wizard__card--selected");
            else
                _backgroundCards[i].RemoveFromClassList("wizard__card--selected");
        }
    }

    private void RefreshFounderCountCardSelection()
    {
        if (_viewModel == null || _founderCountComparison == null) return;

        var card1 = _founderCountComparison.Q<VisualElement>("fc-card-1");
        var card2 = _founderCountComparison.Q<VisualElement>("fc-card-2");

        if (card1 != null)
        {
            if (_viewModel.SelectedFounderCount == 1)
                card1.AddToClassList("wizard__comparison-card--selected");
            else
                card1.RemoveFromClassList("wizard__comparison-card--selected");
        }

        if (card2 != null)
        {
            if (_viewModel.SelectedFounderCount == 2)
                card2.AddToClassList("wizard__comparison-card--selected");
            else
                card2.RemoveFromClassList("wizard__comparison-card--selected");
        }
    }

    // ── Sync helpers ──

    private void SyncStepOneFieldsFromViewModel()
    {
        if (_viewModel == null) return;

        if (_companyNameInput != null && _companyNameInput.value != _viewModel.CompanyName)
            _companyNameInput.SetValueWithoutNotify(_viewModel.CompanyName);
    }

    // ── Founder Step Caching ──

    private void CacheFounderStepElements()
    {
        if (_root == null) return;

        string[] suffixes = { "1", "2" };

        for (int fi = 0; fi < 2; fi++)
        {
            string s = suffixes[fi];

            _archetypeCardContainers[fi] = _root.Q<VisualElement>($"archetype-cards-container-{s}");
            _founderNameInputs[fi] = _root.Q<TextField>($"founder-name-input-{s}");
            _randomiseNameBtns[fi] = _root.Q<Button>($"btn-randomise-name-{s}");
            _personalityStyleContainers[fi] = _root.Q<VisualElement>($"personality-style-cards-{s}");
            _weaknessContainers[fi] = _root.Q<VisualElement>($"weakness-cards-{s}");

            for (int si = 0; si < 4; si++)
                _salaryButtons[fi][si] = _root.Q<Button>($"salary-btn-{s}-{si}");

            _salaryRecommendations[fi] = _root.Q<Label>($"salary-recommendation-{s}");
            _equityExpectations[fi] = _root.Q<Label>($"equity-expectation-{s}");
            _salaryMonthlyCosts[fi] = _root.Q<Label>($"salary-monthly-cost-{s}");
            _salaryRunwayImpacts[fi] = _root.Q<Label>($"salary-runway-impact-{s}");
            _salaryPressures[fi] = _root.Q<Label>($"salary-pressure-{s}");
            _personalityValidations[fi] = _root.Q<Label>($"personality-validation-{s}");
            _weaknessValidations[fi] = _root.Q<Label>($"weakness-validation-{s}");
        }

        // Founder preview elements
        _previewFounderDetails = _root.Q<VisualElement>("preview-founder-details");
        _founderPreviewName = _root.Q<Label>("founder-preview-name");
        _founderPreviewArchetypeBadge = _root.Q<Label>("founder-preview-archetype-badge");
        _founderPreviewRole = _root.Q<Label>("founder-preview-role");
        _founderPreviewLocation = _root.Q<Label>("founder-preview-location");
        _founderPreviewTopSkills = _root.Q<VisualElement>("founder-preview-top-skills");
        _founderPreviewCaStars = _root.Q<VisualElement>("founder-preview-ca-stars");
        _founderPreviewPaStars = _root.Q<VisualElement>("founder-preview-pa-stars");
        _founderPreviewStrengths = _root.Q<VisualElement>("founder-preview-strengths");
        _founderPreviewRisks = _root.Q<VisualElement>("founder-preview-risks");
        _founderPreviewDeptBars = _root.Q<VisualElement>("founder-preview-dept-bars");
        _founderPreviewTeamTotal = _root.Q<Label>("founder-preview-team-total");
        _founderPreviewCompanyDetails = _root.Q<VisualElement>("founder-preview-company-details");
    }

    // ── Founder Step Handler Wiring ──

    private void WireFounderStepHandlers()
    {
        for (int fi = 0; fi < 2; fi++)
        {
            if (_randomiseNameBtns[fi] != null)
                _randomiseNameBtns[fi].clicked += fi == 0 ? OnRandomiseNameF1 : OnRandomiseNameF2;

            if (_founderNameInputs[fi] != null)
                _founderNameInputs[fi].RegisterCallback<ChangeEvent<string>>(fi == 0 ? OnFounderNameChangedF1 : OnFounderNameChangedF2);
        }
    }

    private void UnwireFounderStepHandlers()
    {
        for (int fi = 0; fi < 2; fi++)
        {
            if (_randomiseNameBtns[fi] != null)
                _randomiseNameBtns[fi].clicked -= fi == 0 ? OnRandomiseNameF1 : OnRandomiseNameF2;

            if (_founderNameInputs[fi] != null)
                _founderNameInputs[fi].UnregisterCallback<ChangeEvent<string>>(fi == 0 ? OnFounderNameChangedF1 : OnFounderNameChangedF2);
        }
    }

    // ── Named Handler Methods (Founder 1) ──

    private void OnRandomiseNameF1() => RandomiseFounderName(0);
    private void OnFounderNameChangedF1(ChangeEvent<string> evt) => OnFounderNameChanged(evt, 0);

    // ── Named Handler Methods (Founder 2) ──

    private void OnRandomiseNameF2() => RandomiseFounderName(1);
    private void OnFounderNameChangedF2(ChangeEvent<string> evt) => OnFounderNameChanged(evt, 1);

    // ── Handler Implementations ──

    private void RandomiseFounderName(int fi)
    {
        if (_viewModel == null) return;
        string name = _viewModel.GetRandomFounderName();
        _viewModel.SetFounderName(fi, name);
        if (_founderNameInputs[fi] != null)
            _founderNameInputs[fi].SetValueWithoutNotify(name);
    }

    private void OnFounderNameChanged(ChangeEvent<string> evt, int fi)
    {
        if (_viewModel != null) _viewModel.SetFounderName(fi, evt.newValue);
    }

    private void BuildArchetypeCards(int fi)
    {
        var container = _archetypeCardContainers[fi];
        if (container == null || _viewModel == null) return;

        container.Clear();
        _archetypeCards[fi].Clear();

        var options = _viewModel.ArchetypeOptions;
        for (int i = 0; i < options.Length; i++)
        {
            var option = options[i];
            int archetypeIndex = i;
            int founderIndex = fi;

            var card = new VisualElement();
            card.AddToClassList("founder__archetype-card");
            card.userData = archetypeIndex;
            card.focusable = true;

            if (option.IsGated)
                card.AddToClassList("founder__archetype-card--gated");

            var nameLabel = new Label(option.DisplayName);
            nameLabel.AddToClassList("founder__archetype-card-name");
            nameLabel.AddToClassList("text--primary");
            card.Add(nameLabel);

            var roleLabel = new Label(RoleIdHelper.GetName(option.Role));
            roleLabel.AddToClassList("founder__archetype-card-role");
            roleLabel.AddToClassList("text--secondary");
            card.Add(roleLabel);

            if (!string.IsNullOrEmpty(option.BestEarlyUse))
            {
                var earlyUse = new Label(option.BestEarlyUse);
                earlyUse.AddToClassList("founder__archetype-card-early-use");
                earlyUse.AddToClassList("text--secondary");
                card.Add(earlyUse);
            }

            if (option.TopSkills != null && option.TopSkills.Length > 0)
            {
                var skillsRow = new VisualElement();
                skillsRow.AddToClassList("founder__archetype-card-skills");
                for (int t = 0; t < option.TopSkills.Length && t < 3; t++)
                {
                    var chip = new Label(SkillIdHelper.GetName(option.TopSkills[t]));
                    chip.AddToClassList("status-chip");
                    skillsRow.Add(chip);
                }
                card.Add(skillsRow);
            }

            if (option.Risks != null && option.Risks.Length > 0)
            {
                var riskChips = new VisualElement();
                riskChips.AddToClassList("founder__archetype-card-risks");
                for (int r = 0; r < option.Risks.Length && r < 2; r++)
                {
                    var chip = new Label(option.Risks[r]);
                    chip.AddToClassList("status-chip");
                    chip.AddToClassList("status-chip--warning");
                    riskChips.Add(chip);
                }
                card.Add(riskChips);
            }

            if (option.IsGated && !string.IsNullOrEmpty(option.GateCondition))
            {
                var gateLabel = new Label(option.GateCondition);
                gateLabel.AddToClassList("founder__archetype-card-gate");
                gateLabel.AddToClassList("text--muted");
                card.Add(gateLabel);
            }

            card.RegisterCallback<ClickEvent>(evt =>
            {
                if (!options[archetypeIndex].IsGated)
                    OnArchetypeCardClicked(founderIndex, archetypeIndex);
            });
            card.RegisterCallback<KeyDownEvent>(evt =>
            {
                if ((evt.keyCode == UnityEngine.KeyCode.Return || evt.keyCode == UnityEngine.KeyCode.Space)
                    && !options[archetypeIndex].IsGated)
                    OnArchetypeCardClicked(founderIndex, archetypeIndex);
            });

            _archetypeCards[fi].Add(card);
            container.Add(card);
        }
    }

    private void OnArchetypeCardClicked(int founderIndex, int archetypeIndex)
    {
        if (_viewModel == null) return;
        _viewModel.SelectArchetype(founderIndex, archetypeIndex);
    }

    private void RefreshArchetypeSelection(int fi)
    {
        if (_viewModel == null) return;
        int selected = _viewModel.GetSelectedArchetypeIndex(fi);
        var cards = _archetypeCards[fi];
        for (int i = 0; i < cards.Count; i++)
        {
            if (i == selected)
                cards[i].AddToClassList("founder__archetype-card--selected");
            else
                cards[i].RemoveFromClassList("founder__archetype-card--selected");
        }
    }

    private void SyncFounderNameField(int fi)
    {
        if (_viewModel == null || _founderNameInputs[fi] == null) return;
        string name = _viewModel.GetFounderName(fi);
        if (_founderNameInputs[fi].value != name)
            _founderNameInputs[fi].SetValueWithoutNotify(name);
    }

    // ── Personality Style Cards ──

    private void BuildPersonalityStyleCards(int fi)
    {
        var container = _personalityStyleContainers[fi];
        if (container == null) return;

        container.Clear();
        _personalityStyleCards[fi].Clear();

        var options = NewGameFounderCreationViewModel.PersonalityStyleOptions;
        for (int i = 0; i < options.Length; i++)
        {
            var option = options[i];
            int styleIndex = i;
            int founderIndex = fi;

            var card = new VisualElement();
            card.AddToClassList("wizard__card");
            card.AddToClassList("wizard__card--style");
            card.userData = styleIndex;

            var title = new Label(option.DisplayName);
            title.AddToClassList("wizard__card-title");
            title.AddToClassList("text--primary");
            card.Add(title);

            var desc = new Label(option.Description);
            desc.AddToClassList("wizard__card-description");
            desc.AddToClassList("text--secondary");
            card.Add(desc);

            var modifiers = new Label(option.AttributeModifiers);
            modifiers.AddToClassList("wizard__card-modifiers");
            modifiers.AddToClassList("text--muted");
            card.Add(modifiers);

            card.focusable = true;
            card.RegisterCallback<ClickEvent>(evt => OnPersonalityStyleCardClicked(founderIndex, styleIndex));
            card.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode == UnityEngine.KeyCode.Return || evt.keyCode == UnityEngine.KeyCode.Space)
                    OnPersonalityStyleCardClicked(founderIndex, styleIndex);
            });

            _personalityStyleCards[fi].Add(card);
            container.Add(card);
        }
    }

    private void OnPersonalityStyleCardClicked(int founderIndex, int styleIndex)
    {
        if (_viewModel == null) return;
        _viewModel.SelectPersonalityStyle(founderIndex, styleIndex);
        RefreshPersonalityStyleSelection(founderIndex);
    }

    private void RefreshPersonalityStyleSelection(int fi)
    {
        if (_viewModel == null) return;
        int selected = _viewModel.GetSelectedPersonalityStyleIndex(fi);
        var cards = _personalityStyleCards[fi];
        for (int i = 0; i < cards.Count; i++)
        {
            if (i == selected)
                cards[i].AddToClassList("wizard__card--selected");
            else
                cards[i].RemoveFromClassList("wizard__card--selected");
        }
    }

    // ── Weakness Cards ──

    private void BuildWeaknessCards(int fi)
    {
        var container = _weaknessContainers[fi];
        if (container == null) return;

        container.Clear();
        _weaknessCards[fi].Clear();

        var options = NewGameFounderCreationViewModel.WeaknessOptions;
        for (int i = 0; i < options.Length; i++)
        {
            var option = options[i];
            int weaknessIndex = i;
            int founderIndex = fi;

            var card = new VisualElement();
            card.AddToClassList("wizard__card");
            card.AddToClassList("wizard__card--weakness");
            card.userData = weaknessIndex;

            var title = new Label(option.DisplayName);
            title.AddToClassList("wizard__card-title");
            title.AddToClassList("text--primary");
            card.Add(title);

            var desc = new Label(option.Description);
            desc.AddToClassList("wizard__card-description");
            desc.AddToClassList("text--secondary");
            card.Add(desc);

            var risk = new Label($"Risk: {option.Risk}");
            risk.AddToClassList("wizard__card-risk");
            risk.AddToClassList("text--warning");
            card.Add(risk);

            card.focusable = true;
            card.RegisterCallback<ClickEvent>(evt => OnWeaknessCardClicked(founderIndex, weaknessIndex));
            card.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode == UnityEngine.KeyCode.Return || evt.keyCode == UnityEngine.KeyCode.Space)
                    OnWeaknessCardClicked(founderIndex, weaknessIndex);
            });

            _weaknessCards[fi].Add(card);
            container.Add(card);
        }
    }

    private void OnWeaknessCardClicked(int founderIndex, int weaknessIndex)
    {
        if (_viewModel == null) return;
        _viewModel.SelectWeakness(founderIndex, weaknessIndex);
        RefreshWeaknessSelection(founderIndex);
    }

    private void RefreshWeaknessSelection(int fi)
    {
        if (_viewModel == null) return;
        int selected = _viewModel.GetSelectedWeaknessIndex(fi);
        var cards = _weaknessCards[fi];
        for (int i = 0; i < cards.Count; i++)
        {
            if (i == selected)
                cards[i].AddToClassList("wizard__card--selected");
            else
                cards[i].RemoveFromClassList("wizard__card--selected");
        }
    }

    private void RefreshSalaryButtons(int fi)
    {
        if (_viewModel == null) return;
        int selected = _viewModel.GetSelectedSalaryIndex(fi);
        for (int i = 0; i < _salaryButtons[fi].Length; i++)
        {
            var btn = _salaryButtons[fi][i];
            if (btn == null) continue;
            int idx = i;
            if (i == selected)
                btn.AddToClassList("founder__salary-btn--selected");
            else
                btn.RemoveFromClassList("founder__salary-btn--selected");

            btn.clicked -= GetSalaryClickHandler(fi, i);
            btn.clicked += GetSalaryClickHandler(fi, i);
        }
    }

    private Action _salaryHandlers00, _salaryHandlers01, _salaryHandlers02, _salaryHandlers03;
    private Action _salaryHandlers10, _salaryHandlers11, _salaryHandlers12, _salaryHandlers13;

    private Action GetSalaryClickHandler(int fi, int idx)
    {
        if (fi == 0)
        {
            switch (idx)
            {
                case 0: return _salaryHandlers00 ?? (_salaryHandlers00 = () => OnSalaryClicked(0, 0));
                case 1: return _salaryHandlers01 ?? (_salaryHandlers01 = () => OnSalaryClicked(0, 1));
                case 2: return _salaryHandlers02 ?? (_salaryHandlers02 = () => OnSalaryClicked(0, 2));
                default: return _salaryHandlers03 ?? (_salaryHandlers03 = () => OnSalaryClicked(0, 3));
            }
        }
        else
        {
            switch (idx)
            {
                case 0: return _salaryHandlers10 ?? (_salaryHandlers10 = () => OnSalaryClicked(1, 0));
                case 1: return _salaryHandlers11 ?? (_salaryHandlers11 = () => OnSalaryClicked(1, 1));
                case 2: return _salaryHandlers12 ?? (_salaryHandlers12 = () => OnSalaryClicked(1, 2));
                default: return _salaryHandlers13 ?? (_salaryHandlers13 = () => OnSalaryClicked(1, 3));
            }
        }
    }

    private void OnSalaryClicked(int fi, int idx)
    {
        if (_viewModel == null) return;
        _viewModel.SelectSalary(fi, idx);
    }

    private void RefreshSalaryPreview(int fi)
    {
        if (_viewModel == null) return;
        int idx = _viewModel.GetSelectedSalaryIndex(fi);
        var opts = NewGameFounderCreationViewModel.SalaryOptions;
        if (idx < 0 || idx >= opts.Length) return;
        var opt = opts[idx];
        if (_salaryMonthlyCosts[fi] != null)
            _salaryMonthlyCosts[fi].text = $"Monthly: ${opt.MonthlyAmount:N0}";
        if (_salaryRunwayImpacts[fi] != null)
            _salaryRunwayImpacts[fi].text = $"Runway: {opt.RunwayImpact}";
        if (_salaryPressures[fi] != null)
            _salaryPressures[fi].text = $"Pressure: {opt.FuturePressure}";
    }

    private void RefreshPersonalityWeaknessValidation(int fi)
    {
        if (_viewModel == null) return;
        var setup = _viewModel.SetupState.FounderSetups;
        if (setup == null || fi >= setup.Length) return;
        var s = setup[fi];
        if (_personalityValidations[fi] != null)
        {
            bool hasPersonality = s.PersonalityStyleId >= 0;
            _personalityValidations[fi].text = hasPersonality ? "" : "No personality style selected — choose how this founder operates.";
            _personalityValidations[fi].style.display = hasPersonality ? UnityEngine.UIElements.DisplayStyle.None : UnityEngine.UIElements.DisplayStyle.Flex;
        }
        if (_weaknessValidations[fi] != null)
        {
            bool hasWeakness = s.WeaknessId >= 0;
            _weaknessValidations[fi].text = hasWeakness ? "" : "No weakness selected — choose one founder weakness.";
            _weaknessValidations[fi].style.display = hasWeakness ? UnityEngine.UIElements.DisplayStyle.None : UnityEngine.UIElements.DisplayStyle.Flex;
        }
    }

    // ── Founder Preview Panel Population ──

    private void PopulateFounderPreview(int fi)
    {
        if (_viewModel == null) return;

        var preview = _viewModel.GetFounderPreview(fi);

        if (_founderPreviewName != null) _founderPreviewName.text = preview.Name;
        if (_founderPreviewArchetypeBadge != null) _founderPreviewArchetypeBadge.text = preview.ArchetypeName;
        if (_founderPreviewRole != null) _founderPreviewRole.text = preview.RoleName;
        if (_founderPreviewLocation != null) _founderPreviewLocation.text = preview.Location;

        // Top skills chips
        if (_founderPreviewTopSkills != null)
        {
            _founderPreviewTopSkills.Clear();
            if (preview.TopSkills != null)
            {
                for (int i = 0; i < preview.TopSkills.Length; i++)
                {
                    var chip = new Label($"{preview.TopSkills[i].SkillName} {preview.TopSkills[i].Value}");
                    chip.AddToClassList("founder-preview__skill-chip");
                    _founderPreviewTopSkills.Add(chip);
                }
            }
        }

        // CA stars
        if (_founderPreviewCaStars != null)
        {
            _founderPreviewCaStars.Clear();
            for (int i = 0; i < 5; i++)
            {
                var star = new Label("★");
                star.AddToClassList("founder-preview__star");
                if (i >= preview.CAStars) star.AddToClassList("founder-preview__star--empty");
                _founderPreviewCaStars.Add(star);
            }
        }

        // PA stars
        if (_founderPreviewPaStars != null)
        {
            _founderPreviewPaStars.Clear();
            for (int i = 0; i < 5; i++)
            {
                var star = new Label("★");
                star.AddToClassList("founder-preview__star");
                if (i >= preview.PAStars) star.AddToClassList("founder-preview__star--empty");
                _founderPreviewPaStars.Add(star);
            }
        }

        // Strengths
        if (_founderPreviewStrengths != null)
        {
            _founderPreviewStrengths.Clear();
            if (preview.Strengths != null)
            {
                for (int i = 0; i < preview.Strengths.Length; i++)
                {
                    var item = new Label($"+ {preview.Strengths[i]}");
                    item.AddToClassList("founder-preview__list-item");
                    item.AddToClassList("founder-preview__list-item--strength");
                    _founderPreviewStrengths.Add(item);
                }
            }
        }

        // Risks
        if (_founderPreviewRisks != null)
        {
            _founderPreviewRisks.Clear();
            if (preview.Risks != null)
            {
                for (int i = 0; i < preview.Risks.Length; i++)
                {
                    var item = new Label($"- {preview.Risks[i]}");
                    item.AddToClassList("founder-preview__list-item");
                    item.AddToClassList("founder-preview__list-item--risk");
                    _founderPreviewRisks.Add(item);
                }
            }
        }

        // Department bars
        if (_founderPreviewDeptBars != null)
        {
            _founderPreviewDeptBars.Clear();
            if (preview.DepartmentBars != null)
            {
                for (int i = 0; i < preview.DepartmentBars.Length; i++)
                {
                    var entry = preview.DepartmentBars[i];
                    var row = new VisualElement();
                    row.AddToClassList("founder-preview__dept-row");

                    var lbl = new Label(entry.Department);
                    lbl.AddToClassList("founder-preview__dept-label");
                    row.Add(lbl);

                    var track = new VisualElement();
                    track.AddToClassList("founder-preview__dept-bar-track");
                    var fill = new VisualElement();
                    fill.AddToClassList("founder-preview__dept-bar-fill");
                    float fillPct = entry.Max > 0 ? (float)entry.Current / entry.Max : 0f;
                    fill.style.width = new StyleLength(new Length(fillPct * 100f, LengthUnit.Percent));
                    track.Add(fill);
                    row.Add(track);

                    var count = new Label($"{entry.Current}/{entry.Max}");
                    count.AddToClassList("founder-preview__dept-count");
                    row.Add(count);

                    _founderPreviewDeptBars.Add(row);
                }
            }
        }

        if (_founderPreviewTeamTotal != null)
            _founderPreviewTeamTotal.text = preview.TotalTeamSize.ToString();

        // Company details
        if (_founderPreviewCompanyDetails != null)
        {
            _founderPreviewCompanyDetails.Clear();
            var company = preview.Company;
            AddCompanyDetailRow("Company", company.CompanyName);
            AddCompanyDetailRow("Industry", company.Industry);
            AddCompanyDetailRow("Model", company.BusinessModel);
            AddCompanyDetailRow("HQ", company.Headquarters);
            AddCompanyDetailRow("Budget", company.StartingBudget);
            AddCompanyDetailRow("Runway", company.Runway);
        }
    }

    private void AddCompanyDetailRow(string key, string value)
    {
        if (_founderPreviewCompanyDetails == null) return;
        var row = new VisualElement();
        row.AddToClassList("founder-preview__company-row");
        var keyLbl = new Label(key);
        keyLbl.AddToClassList("founder-preview__company-key");
        var valLbl = new Label(value);
        valLbl.AddToClassList("founder-preview__company-value");
        row.Add(keyLbl);
        row.Add(valLbl);
        _founderPreviewCompanyDetails.Add(row);
    }

    // ── Final Step Caching ──

    private void CacheFinalStepElements()
    {
        if (_root == null) return;

        // Review step
        _reviewCompanyName = _root.Q<Label>("review-company-name");
        _reviewIndustry = _root.Q<Label>("review-industry");
        _reviewBackground = _root.Q<Label>("review-background");
        _reviewFoundersSummary = _root.Q<Label>("review-founders-summary");
        _reviewSalaryCost = _root.Q<Label>("review-salary-cost");
        _reviewStartingCash = _root.Q<Label>("review-starting-cash");
        _reviewRunway = _root.Q<Label>("review-runway");
        _reviewTeamStrength = _root.Q<Label>("review-team-strength");
        _reviewErrorsContainer = _root.Q<VisualElement>("review-errors-container");
        _reviewWarningsContainer = _root.Q<VisualElement>("review-warnings-container");
        _btnStartGame = _root.Q<Button>("btn-start-game");
    }

    private void WireFinalStepHandlers()
    {
        if (_btnStartGame != null)
            _btnStartGame.clicked += OnStartGameClicked;
    }

    private void UnwireFinalStepHandlers()
    {
        if (_btnStartGame != null)
            _btnStartGame.clicked -= OnStartGameClicked;
    }

    // ── Final Step Bind Methods ──

    private void BindReviewStep()
    {
        if (_viewModel == null) return;

        var data = _viewModel.GetReviewData();

        if (_reviewCompanyName != null) _reviewCompanyName.text = data.CompanyName;
        if (_reviewIndustry != null) _reviewIndustry.text = data.Industry;
        if (_reviewBackground != null) _reviewBackground.text = data.BackgroundName;
        if (_reviewFoundersSummary != null) _reviewFoundersSummary.text = data.FoundersSummary;
        if (_reviewSalaryCost != null) _reviewSalaryCost.text = data.MonthlySalaryCost;
        if (_reviewStartingCash != null) _reviewStartingCash.text = data.StartingCash;
        if (_reviewRunway != null) _reviewRunway.text = data.RunwayEstimate;
        if (_reviewTeamStrength != null) _reviewTeamStrength.text = data.TeamStrengthLabel;

        // Errors
        if (_reviewErrorsContainer != null)
        {
            _reviewErrorsContainer.Clear();
            for (int i = 0; i < data.BlockingErrors.Length; i++)
            {
                var banner = new VisualElement();
                banner.AddToClassList("review__error-banner");
                var icon = new Label("✕");
                icon.AddToClassList("review__error-icon");
                var text = new Label(data.BlockingErrors[i]);
                text.AddToClassList("review__error-text");
                banner.Add(icon);
                banner.Add(text);
                _reviewErrorsContainer.Add(banner);
            }
        }

        // Warnings
        if (_reviewWarningsContainer != null)
        {
            _reviewWarningsContainer.Clear();
            for (int i = 0; i < data.Warnings.Length; i++)
            {
                var banner = new VisualElement();
                banner.AddToClassList("review__warning-banner");
                var icon = new Label("!");
                icon.AddToClassList("review__warning-icon");
                var text = new Label(data.Warnings[i]);
                text.AddToClassList("review__warning-text");
                banner.Add(icon);
                banner.Add(text);
                _reviewWarningsContainer.Add(banner);
            }
        }

        // Enable/disable Start Game
        if (_btnStartGame != null)
            _btnStartGame.SetEnabled(data.CanStartGame);
    }

    // ── Start Game Handler ──

    private void OnStartGameClicked()
    {
        if (_viewModel == null) return;

        var reviewData = _viewModel.GetReviewData();
        if (!reviewData.CanStartGame)
        {
            BindReviewStep();
            return;
        }

        var founders = _viewModel.GenerateFounderData();
        OnStartGameConfirmed?.Invoke(_viewModel.SetupState, founders);
    }
}
