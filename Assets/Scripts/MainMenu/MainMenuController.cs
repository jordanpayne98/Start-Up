using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class MainMenuController : MonoBehaviour {
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private VisualTreeAsset wizardTemplate;

    private const int MaxFounders = 3;

    // ── Wizard state ──
    private VisualElement _wizardRoot;
    private NewGameFounderCreationView _wizardView;
    private NewGameFounderCreationViewModel _wizardViewModel;

    private static readonly string[] TierLabels = { "Intern", "Junior", "Mid-Level", "Senior", "Expert" };
    private static readonly string[] TierDescriptions = {
        "Entry-level skills — great for a lean start",
        "Basic competency — modest early output",
        "Solid contributor — balanced foundation",
        "Experienced professional — strong specialisation",
        "Industry expert — peak starting capability"
    };

    private RoleProfileTable _roleProfileTable;

    private int _gameSeed;
    private RoleId[] _roleValues;
    private readonly List<string> _tierChoices = new List<string>(5);
    private readonly List<RadarChartElement.AxisData> _radarCache = new List<RadarChartElement.AxisData>(SkillIdHelper.SkillCount);

    // ── Main menu elements ──
    private VisualElement _mainMenuRoot;
    private Button _btnNewGame;
    private Button _btnContinue;
    private Button _btnLoadGame;
    private Button _btnSettings;
    private Button _btnExit;
    private Label _versionLabel;

    // ── New game elements ──
    private VisualElement _newGameRoot;
    private TextField _companyNameField;
    private VisualElement _foundersContainer;
    private Button _btnAddFounder;
    private Button _btnLaunch;
    private Button _btnBack;

    // ── Load game elements ──
    private VisualElement _loadGameRoot;
    private VisualElement _manualSavesList;
    private VisualElement _autoSavesList;
    private Button _btnLoadBack;

    // ── Founder card state ──
    private readonly List<FounderCardState> _founderCards = new List<FounderCardState>(MaxFounders);
    private readonly List<string> _roleChoices = new List<string>();
    private InputAction _escapeAction;

    // ── Difficulty elements ──
    private DropdownField _difficultyPresetDropdown;
    private VisualElement _difficultyDetails;
    private Toggle _taxToggle;
    private Toggle _salariesToggle;
    private Toggle _quittingToggle;
    private Toggle _competitorsToggle;
    private Toggle _bankruptcyToggle;
    private SliderInt _startingCashSlider;
    private SliderInt _contractRewardSlider;
    private SliderInt _taxRateSlider;
    private SliderInt _salarySlider;
    private SliderInt _skillGrowthSlider;
    private SliderInt _moraleDecaySlider;
    private SliderInt _competitorSlider;
    private SliderInt _marketDifficultySlider;
    private SliderInt _loanInterestSlider;
    private SliderInt _productWorkRateSlider;
    private SliderInt _bugRateSlider;
    private SliderInt _reviewHarshnessSlider;
    private SliderInt _productRevenueSlider;
    private DifficultySettings _currentDifficulty;

    private struct FounderCardState {
        public VisualElement Root;
        public TextField NameField;
        public SliderInt AgeSlider;
        public DropdownField GenderDropdown;
        public DropdownField RoleDropdown;
        public DropdownField TierDropdown;
        public RadarChartElement RadarChart;
    }

    private void Start() {
        _roleValues = (RoleId[])System.Enum.GetValues(typeof(RoleId));
        _gameSeed = System.Environment.TickCount | 1;
        LoadRoleProfileTable();
        BuildTierChoices();
        BuildRoleChoices();
        QueryAndCacheElements();
        WireHandlers();
        ConfigureInitialState();
        AddFounderCard();
        ShowMainMenu();
        SetupEscapeKey();
    }

    private void SetupEscapeKey() {
        _escapeAction = new InputAction(type: InputActionType.Button);
        _escapeAction.AddBinding("<Keyboard>/escape");
        _escapeAction.performed += OnEscapePerformed;
        _escapeAction.Enable();
    }

    private void OnEscapePerformed(InputAction.CallbackContext ctx) {
        if (_wizardRoot != null) {
            OnWizardCancelled();
        } else if (_newGameRoot != null && _newGameRoot.style.display == DisplayStyle.Flex) {
            OnBackClicked();
        } else if (_loadGameRoot != null && _loadGameRoot.style.display == DisplayStyle.Flex) {
            OnLoadBackClicked();
        }
    }

    private void OnDestroy() {
        if (_escapeAction != null) {
            _escapeAction.performed -= OnEscapePerformed;
            _escapeAction.Disable();
            _escapeAction = null;
        }
    }

    private void LoadRoleProfileTable() {
        var profiles = Resources.LoadAll<RoleProfileDefinition>("RoleProfiles");
        _roleProfileTable = new RoleProfileTable();
        for (int i = 0; i < profiles.Length; i++) {
            _roleProfileTable.Register(profiles[i]);
        }
    }

    private void BuildRoleChoices() {
        for (int i = 0; i < _roleValues.Length; i++) {
            _roleChoices.Add(RoleIdHelper.GetName(_roleValues[i]));
        }
    }

    private void BuildTierChoices() {
        for (int i = 0; i < TierLabels.Length; i++) {
            _tierChoices.Add(TierLabels[i] + " — " + TierDescriptions[i]);
        }
    }

    private int[] GenerateFounderSkills(int tier, RoleId role, IRng rng) {
        var profile = _roleProfileTable?.Get(role);
        int[] roleTiers = profile != null ? RoleSuitabilityCalculator.BuildTierArray(profile) : null;
        int skillCount = SkillIdHelper.SkillCount;
        var skills = new int[skillCount];
        for (int i = 0; i < skillCount; i++) {
            int weight = (roleTiers != null && i < roleTiers.Length) ? roleTiers[i] : 3;
            int min, max;
            switch (tier) {
                case 1:
                    if (weight == 2) { min = 3; max = 5; }
                    else if (weight == 3) { min = 1; max = 3; }
                    else { min = 1; max = 1; }
                    break;
                case 2:
                    if (weight == 2) { min = 5; max = 8; }
                    else if (weight == 3) { min = 2; max = 5; }
                    else { min = 1; max = 2; }
                    break;
                case 3:
                    if (weight == 2) { min = 8; max = 12; }
                    else if (weight == 3) { min = 4; max = 7; }
                    else { min = 1; max = 3; }
                    break;
                case 4:
                    if (weight == 2) { min = 12; max = 15; }
                    else if (weight == 3) { min = 6; max = 10; }
                    else { min = 2; max = 5; }
                    break;
                default: // tier 5
                    if (weight == 2) { min = 16; max = 20; }
                    else if (weight == 3) { min = 9; max = 13; }
                    else { min = 4; max = 7; }
                    break;
            }
            skills[i] = rng.Range(min, max + 1);
        }

        // Identity clamp: the Primary skill (tier value 2) must exceed all others by at least 1
        int identityIdx = -1;
        for (int i = 0; i < skillCount; i++) {
            if (roleTiers != null && i < roleTiers.Length && roleTiers[i] == 2) { identityIdx = i; break; }
        }
        if (identityIdx >= 0) {
            int maxOther = 0;
            for (int i = 0; i < skillCount; i++) {
                if (i != identityIdx && skills[i] > maxOther) maxOther = skills[i];
            }
            if (skills[identityIdx] <= maxOther) {
                skills[identityIdx] = maxOther + 1 > 20 ? 20 : maxOther + 1;
            }
        }

        return skills;
    }

    private void RefreshAllRadarCharts() {
        for (int c = 0; c < _founderCards.Count; c++) {
            var card = _founderCards[c];
            int roleIndex = card.RoleDropdown.index;
            RoleId role = (roleIndex >= 0 && roleIndex < _roleValues.Length)
                ? _roleValues[roleIndex]
                : RoleId.SoftwareEngineer;
            int tier = card.TierDropdown.index + 1;
            if (tier < 1) tier = 1;
            int cardSeed = _gameSeed ^ (c * 6271) ^ (tier * 7919) ^ ((int)role * 4999);
            var rng = new RngStream(cardSeed);
            int[] skills = GenerateFounderSkills(tier, role, rng);
            int skillCount = SkillIdHelper.SkillCount;

            _radarCache.Clear();
            for (int i = 0; i < skillCount; i++) {
                _radarCache.Add(new RadarChartElement.AxisData {
                    Name = SkillIdHelper.GetName((SkillId)i),
                    NormalizedValue = skills[i] / 20f,
                    DeltaDirection = 0,
                    RawValue = skills[i],
                    LabelColor = UIFormatting.GetSkillColor((SkillId)i)
                });
            }
            card.RadarChart.SetData(_radarCache);
        }
    }

    private void QueryAndCacheElements() {
        var root = uiDocument.rootVisualElement;

        _mainMenuRoot = root.Q<VisualElement>("main-menu-root");
        _btnNewGame = root.Q<Button>("btn-new-game");
        _btnContinue = root.Q<Button>("btn-continue");
        _btnLoadGame = root.Q<Button>("btn-load-game");
        _btnSettings = root.Q<Button>("btn-settings");
        _btnExit = root.Q<Button>("btn-exit");
        _versionLabel = root.Q<Label>("version-label");

        _newGameRoot = root.Q<VisualElement>("new-game-root");
        _companyNameField = root.Q<TextField>("company-name-field");
        _foundersContainer = root.Q<VisualElement>("founders-list");
        _btnAddFounder = root.Q<Button>("btn-add-founder");
        _btnLaunch = root.Q<Button>("btn-launch");
        _btnBack = root.Q<Button>("btn-back");

        _loadGameRoot = root.Q<VisualElement>("load-game-root");
        _manualSavesList = root.Q<VisualElement>("manual-saves-list");
        _autoSavesList = root.Q<VisualElement>("auto-saves-list");
        _btnLoadBack = root.Q<Button>("btn-load-back");

        _difficultyPresetDropdown = root.Q<DropdownField>("difficulty-preset");
        _difficultyDetails = root.Q<VisualElement>("difficulty-details");
        _taxToggle = root.Q<Toggle>("tax-toggle");
        _salariesToggle = root.Q<Toggle>("salaries-toggle");
        _quittingToggle = root.Q<Toggle>("quitting-toggle");
        _competitorsToggle = root.Q<Toggle>("competitors-toggle");
        _bankruptcyToggle = root.Q<Toggle>("bankruptcy-toggle");
        _startingCashSlider = root.Q<SliderInt>("starting-cash-slider");
        _contractRewardSlider = root.Q<SliderInt>("contract-reward-slider");
        _taxRateSlider = root.Q<SliderInt>("tax-rate-slider");
        _salarySlider = root.Q<SliderInt>("salary-slider");
        _skillGrowthSlider = root.Q<SliderInt>("skill-growth-slider");
        _moraleDecaySlider = root.Q<SliderInt>("morale-decay-slider");
        _competitorSlider = root.Q<SliderInt>("competitor-slider");
        _marketDifficultySlider = root.Q<SliderInt>("market-difficulty-slider");
        _loanInterestSlider = root.Q<SliderInt>("loan-interest-slider");
        _productWorkRateSlider = root.Q<SliderInt>("product-work-rate-slider");
        _bugRateSlider = root.Q<SliderInt>("bug-rate-slider");
        _reviewHarshnessSlider = root.Q<SliderInt>("review-harshness-slider");
        _productRevenueSlider = root.Q<SliderInt>("product-revenue-slider");
    }

    private void WireHandlers() {
        if (_btnNewGame != null) _btnNewGame.clicked += OnNewGameClicked;
        if (_btnContinue != null) _btnContinue.clicked += OnContinueClicked;
        if (_btnLoadGame != null) _btnLoadGame.clicked += OnLoadGameClicked;
        if (_btnSettings != null) _btnSettings.clicked += OnSettingsClicked;
        if (_btnExit != null) _btnExit.clicked += OnExitClicked;
        if (_btnBack != null) _btnBack.clicked += OnBackClicked;
        if (_btnAddFounder != null) _btnAddFounder.clicked += OnAddFounderClicked;
        if (_btnLaunch != null) _btnLaunch.clicked += OnLaunchClicked;
        if (_companyNameField != null) _companyNameField.RegisterCallback<ChangeEvent<string>>(OnCompanyNameChanged);
        if (_btnLoadBack != null) _btnLoadBack.clicked += OnLoadBackClicked;

        if (_difficultyPresetDropdown != null) {
            _difficultyPresetDropdown.choices = new List<string> { "Sandbox", "Easy", "Normal", "Hard", "Custom" };
            _difficultyPresetDropdown.index = 2;
            _difficultyPresetDropdown.RegisterCallback<ChangeEvent<string>>(OnDifficultyPresetChanged);
        }
        if (_startingCashSlider != null) _startingCashSlider.RegisterCallback<ChangeEvent<int>>(OnDifficultySliderChanged);
        if (_contractRewardSlider != null) _contractRewardSlider.RegisterCallback<ChangeEvent<int>>(OnDifficultySliderChanged);
        if (_taxRateSlider != null) _taxRateSlider.RegisterCallback<ChangeEvent<int>>(OnDifficultySliderChanged);
        if (_salarySlider != null) _salarySlider.RegisterCallback<ChangeEvent<int>>(OnDifficultySliderChanged);
        if (_skillGrowthSlider != null) _skillGrowthSlider.RegisterCallback<ChangeEvent<int>>(OnDifficultySliderChanged);
        if (_moraleDecaySlider != null) _moraleDecaySlider.RegisterCallback<ChangeEvent<int>>(OnDifficultySliderChanged);
        if (_competitorSlider != null) _competitorSlider.RegisterCallback<ChangeEvent<int>>(OnDifficultySliderChanged);
        if (_marketDifficultySlider != null) _marketDifficultySlider.RegisterCallback<ChangeEvent<int>>(OnDifficultySliderChanged);
        if (_loanInterestSlider != null) _loanInterestSlider.RegisterCallback<ChangeEvent<int>>(OnDifficultySliderChanged);
        if (_productWorkRateSlider != null) _productWorkRateSlider.RegisterCallback<ChangeEvent<int>>(OnDifficultySliderChanged);
        if (_bugRateSlider != null) _bugRateSlider.RegisterCallback<ChangeEvent<int>>(OnDifficultySliderChanged);
        if (_reviewHarshnessSlider != null) _reviewHarshnessSlider.RegisterCallback<ChangeEvent<int>>(OnDifficultySliderChanged);
        if (_productRevenueSlider != null) _productRevenueSlider.RegisterCallback<ChangeEvent<int>>(OnDifficultySliderChanged);
        if (_taxToggle != null) _taxToggle.RegisterCallback<ChangeEvent<bool>>(OnDifficultyToggleChanged);
        if (_salariesToggle != null) _salariesToggle.RegisterCallback<ChangeEvent<bool>>(OnDifficultyToggleChanged);
        if (_quittingToggle != null) _quittingToggle.RegisterCallback<ChangeEvent<bool>>(OnDifficultyToggleChanged);
        if (_competitorsToggle != null) _competitorsToggle.RegisterCallback<ChangeEvent<bool>>(OnDifficultyToggleChanged);
        if (_bankruptcyToggle != null) _bankruptcyToggle.RegisterCallback<ChangeEvent<bool>>(OnDifficultyToggleChanged);

        _currentDifficulty = DifficultySettings.Default(DifficultyPreset.Normal);
        SyncUIFromDifficulty();
        RefreshDifficultyDetailsVisibility();
    }

    private void ConfigureInitialState() {
        if (_btnSettings != null) _btnSettings.SetEnabled(false);

        var latest = SaveManager.GetLatestSave();
        if (!latest.HasValue) {
            if (_btnContinue != null) _btnContinue.SetEnabled(false);
        }

        int totalSaves = SaveManager.GetManualSaveCount() + SaveManager.GetAutoSaves().Count;
        if (totalSaves == 0) {
            if (_btnLoadGame != null) _btnLoadGame.SetEnabled(false);
        }
    }

    // ── Screen management ──

    private void ShowMainMenu() {
        SetDisplay(_mainMenuRoot, true);
        SetDisplay(_newGameRoot, false);
        SetDisplay(_loadGameRoot, false);
    }

    private void ShowNewGame() {
        SetDisplay(_mainMenuRoot, false);
        SetDisplay(_newGameRoot, true);
        SetDisplay(_loadGameRoot, false);
        ValidateNewGame();
    }

    private void ShowLoadGame() {
        SetDisplay(_mainMenuRoot, false);
        SetDisplay(_newGameRoot, false);
        SetDisplay(_loadGameRoot, true);
        PopulateLoadGameScreen();
    }

    private void SetDisplay(VisualElement element, bool visible) {
        if (element == null) return;
        element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }

    // ── Button handlers ──

    private void OnNewGameClicked() {
        if (wizardTemplate != null) {
            LaunchWizard();
        } else {
            ShowNewGame();
        }
    }

    // ── Wizard Lifecycle ──

    private void LaunchWizard() {
        var rootElement = uiDocument.rootVisualElement;

        // Hide all existing screens
        SetDisplay(_mainMenuRoot, false);
        SetDisplay(_newGameRoot, false);
        SetDisplay(_loadGameRoot, false);

        // Instantiate wizard UXML
        _wizardRoot = wizardTemplate.Instantiate();
        _wizardRoot.style.flexGrow = 1;
        rootElement.Add(_wizardRoot);

        // Create ViewModel and View
        _wizardViewModel = new NewGameFounderCreationViewModel();
        _wizardView = new NewGameFounderCreationView();
        _wizardView.OnCancelRequested += OnWizardCancelled;
        _wizardView.OnStartGameConfirmed += OnNewGameConfirmed;

        var wizardContainer = _wizardRoot.Q<VisualElement>("new-game-wizard");
        if (wizardContainer != null) {
            _wizardView.Initialize(wizardContainer, null);
            _wizardView.Bind(_wizardViewModel);
        } else {
            Debug.LogError("[MainMenuController] Could not find 'new-game-wizard' element in wizard template");
            CleanupWizard();
            ShowMainMenu();
        }
    }

    private void OnWizardCancelled() {
        CleanupWizard();
        ShowMainMenu();
    }

    private void CleanupWizard() {
        if (_wizardView != null) {
            _wizardView.OnCancelRequested -= OnWizardCancelled;
            _wizardView.OnStartGameConfirmed -= OnNewGameConfirmed;
            _wizardView.Dispose();
            _wizardView = null;
        }
        _wizardViewModel = null;

        if (_wizardRoot != null) {
            var rootElement = uiDocument.rootVisualElement;
            rootElement.Remove(_wizardRoot);
            _wizardRoot = null;
        }
    }

    public void OnNewGameConfirmed(NewGameSetupState setup, FoundingEmployeeData[] founders) {
        if (setup == null || founders == null || founders.Length == 0) {
            Debug.LogError("[MainMenuController] OnNewGameConfirmed received invalid data");
            return;
        }

        NewGameData.IsNewGame = true;
        NewGameData.CompanyName = setup.CompanyName;
        NewGameData.Seed = setup.Seed;
        NewGameData.Difficulty = _currentDifficulty;

        // Build FoundingEmployeeData list for backward-compatible game start
        var founderList = new System.Collections.Generic.List<FoundingEmployeeData>(founders.Length);
        for (int i = 0; i < founders.Length; i++) {
            var f = founders[i];
            if (f.Age == 0) f.Age = 28;
            founderList.Add(f);
        }
        NewGameData.Founders = founderList;
        NewGameData.SetupState = setup;

        CleanupWizard();
        SceneManager.LoadScene("MainGame");
    }

    private void OnContinueClicked() {
        var latest = SaveManager.GetLatestSave();
        if (!latest.HasValue) return;
        NewGameData.IsNewGame = false;
        NewGameData.LoadSlotName = latest.Value.SlotName;
        SceneManager.LoadScene("MainGame");
    }

    private void OnLoadGameClicked() {
        ShowLoadGame();
    }

    private void OnSettingsClicked() {
        // Placeholder — settings not yet implemented
    }

    private void OnExitClicked() {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void OnBackClicked() {
        ShowMainMenu();
    }

    private void OnLoadBackClicked() {
        ShowMainMenu();
    }

    private void OnAddFounderClicked() {
        if (_founderCards.Count >= MaxFounders) return;
        AddFounderCard();
        ValidateNewGame();
    }

    private void OnLaunchClicked() {
        if (!IsNewGameValid()) return;

        NewGameData.IsNewGame = true;
        NewGameData.CompanyName = _companyNameField.value.Trim();
        NewGameData.Founders = BuildFoundersData();
        NewGameData.Difficulty = _currentDifficulty;
        NewGameData.Seed = _gameSeed;
        SceneManager.LoadScene("MainGame");
    }

    private void OnCompanyNameChanged(ChangeEvent<string> evt) {
        ValidateNewGame();
    }

    // ── Founder card creation ──

    private void AddFounderCard() {
        int index = _founderCards.Count;
        var card = BuildFounderCard(index);
        _founderCards.Add(card);
        _foundersContainer.Add(card.Root);
        RefreshFounderCardVisibility();
        ValidateNewGame();
        RefreshAllRadarCharts();
    }

    private void RemoveFounderCard(int index) {
        if (index < 0 || index >= _founderCards.Count) return;
        _foundersContainer.Remove(_founderCards[index].Root);
        _founderCards.RemoveAt(index);
        RefreshFounderCardTitles();
        RefreshFounderCardVisibility();
        ValidateNewGame();
        RefreshAllRadarCharts();
    }

    private void RefreshFounderCardVisibility() {
        for (int i = 0; i < _founderCards.Count; i++) {
            var removeBtn = _founderCards[i].Root.Q<Button>("btn-remove");
            if (removeBtn != null) {
                removeBtn.style.display = _founderCards.Count > 1 ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }
        if (_btnAddFounder != null) {
            _btnAddFounder.style.display = _founderCards.Count < MaxFounders ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }

    private void RefreshFounderCardTitles() {
        for (int i = 0; i < _founderCards.Count; i++) {
            var titleLabel = _founderCards[i].Root.Q<Label>("founder-title");
            if (titleLabel != null) {
                titleLabel.text = $"Founder {i + 1}";
            }
        }
    }

    private FounderCardState BuildFounderCard(int index) {
        var state = new FounderCardState();

        var root = new VisualElement();
        root.AddToClassList("founder-card");

        // Header
        var header = new VisualElement();
        header.AddToClassList("founder-card-header");

        var title = new Label($"Founder {index + 1}");
        title.name = "founder-title";
        title.AddToClassList("founder-title");

        var removeBtn = new Button();
        removeBtn.name = "btn-remove";
        removeBtn.text = "Remove";
        removeBtn.AddToClassList("btn-ghost");
        removeBtn.userData = index;
        removeBtn.clicked += () => {
            int cardIndex = _founderCards.IndexOf(state);
            if (cardIndex >= 0) RemoveFounderCard(cardIndex);
        };

        header.Add(title);
        header.Add(removeBtn);
        root.Add(header);

        // Body: two-column layout
        var body = new VisualElement();
        body.AddToClassList("founder-card-body");

        var leftCol = new VisualElement();
        leftCol.AddToClassList("founder-card-left");

        var rightCol = new VisualElement();
        rightCol.AddToClassList("founder-card-right");

        // Name field — own row in left column
        var nameField = new TextField();
        nameField.label = "Name";
        nameField.AddToClassList("founder-name-field");
        nameField.RegisterCallback<ChangeEvent<string>>(OnFounderFieldChanged);
        leftCol.Add(nameField);

        // Row 1: Age + Gender
        var ageGenderRow = new VisualElement();
        ageGenderRow.AddToClassList("founder-fields-row");

        var ageSlider = new SliderInt(18, 55);
        ageSlider.label = "Age";
        ageSlider.value = 25;
        ageSlider.showInputField = true;
        ageSlider.AddToClassList("founder-age-slider");
        ageSlider.RegisterCallback<ChangeEvent<int>>(OnFounderAgeChanged);

        var genderDropdown = new DropdownField("Gender", new List<string> { "Male", "Female" }, 0);
        genderDropdown.AddToClassList("founder-gender-dropdown");

        ageGenderRow.Add(ageSlider);
        ageGenderRow.Add(genderDropdown);
        leftCol.Add(ageGenderRow);

        // Row 2: Role + Tier
        var roleTierRow = new VisualElement();
        roleTierRow.AddToClassList("founder-fields-row");

        var roleDropdown = new DropdownField("Role", _roleChoices, 0);
        roleDropdown.AddToClassList("founder-role-dropdown");
        roleDropdown.RegisterCallback<ChangeEvent<string>>(OnFounderRoleChanged);

        var tierDropdown = new DropdownField("Experience Tier", _tierChoices, 0);
        tierDropdown.AddToClassList("founder-tier-dropdown");
        tierDropdown.RegisterCallback<ChangeEvent<string>>(OnFounderTierChanged);

        roleTierRow.Add(roleDropdown);
        roleTierRow.Add(tierDropdown);
        leftCol.Add(roleTierRow);

        // Footer note
        var founderNote = new Label("PA: 200 | Permanent | No Salary");
        founderNote.AddToClassList("founder-note");
        leftCol.Add(founderNote);

        // Radar chart in right column
        var radarChart = new RadarChartElement();
        radarChart.AddToClassList("founder-radar-chart");
        rightCol.Add(radarChart);

        body.Add(leftCol);
        body.Add(rightCol);
        root.Add(body);

        state.Root = root;
        state.NameField = nameField;
        state.AgeSlider = ageSlider;
        state.GenderDropdown = genderDropdown;
        state.RoleDropdown = roleDropdown;
        state.TierDropdown = tierDropdown;
        state.RadarChart = radarChart;

        return state;
    }

    private void OnFounderFieldChanged(ChangeEvent<string> evt) {
        ValidateNewGame();
    }

    private void OnFounderAgeChanged(ChangeEvent<int> evt) {
        ValidateNewGame();
    }

    private void OnFounderRoleChanged(ChangeEvent<string> evt) {
        RefreshAllRadarCharts();
        ValidateNewGame();
    }

    private void OnFounderTierChanged(ChangeEvent<string> evt) {
        RefreshAllRadarCharts();
        ValidateNewGame();
    }

    // ── Validation ──

    private bool IsNewGameValid() {
        if (_companyNameField == null) return false;
        if (string.IsNullOrWhiteSpace(_companyNameField.value)) return false;
        if (_founderCards.Count == 0) return false;
        for (int i = 0; i < _founderCards.Count; i++) {
            var card = _founderCards[i];
            if (string.IsNullOrWhiteSpace(card.NameField.value)) return false;
            if (card.TierDropdown.index < 0) return false;
        }
        return true;
    }

    private void ValidateNewGame() {
        if (_btnLaunch != null) {
            _btnLaunch.SetEnabled(IsNewGameValid());
        }
    }

    // ── Launch ──

    private List<FoundingEmployeeData> BuildFoundersData() {
        var result = new List<FoundingEmployeeData>(_founderCards.Count);
        for (int i = 0; i < _founderCards.Count; i++) {
            var card = _founderCards[i];
            int roleIndex = card.RoleDropdown.index;
            RoleId role = roleIndex >= 0 && roleIndex < _roleValues.Length ? _roleValues[roleIndex] : RoleId.SoftwareEngineer;
            Gender gender = card.GenderDropdown.index == 0 ? Gender.Male : Gender.Female;
            var data = new FoundingEmployeeData {
                Name = card.NameField.value.Trim(),
                Age = card.AgeSlider.value,
                Gender = gender,
                Role = role,
                SalaryChoice = 2,
                IsFounder = true
            };
            result.Add(data);
        }
        return result;
    }

    // ── Load Game Screen ──

    private void PopulateLoadGameScreen() {
        if (_manualSavesList != null) {
            _manualSavesList.Clear();
            var manuals = SaveManager.GetManualSaves();
            for (int i = 0; i < manuals.Count; i++) {
                var slot = manuals[i];
                _manualSavesList.Add(BuildSaveSlotCard(slot, isManual: true));
            }
            if (manuals.Count == 0) {
                var empty = new Label("No manual saves.");
                empty.AddToClassList("text-muted");
                _manualSavesList.Add(empty);
            }
        }

        if (_autoSavesList != null) {
            _autoSavesList.Clear();
            var autos = SaveManager.GetAutoSaves();
            for (int i = 0; i < autos.Count; i++) {
                var slot = autos[i];
                _autoSavesList.Add(BuildSaveSlotCard(slot, isManual: false));
            }
            if (autos.Count == 0) {
                var empty = new Label("No auto-saves.");
                empty.AddToClassList("text-muted");
                _autoSavesList.Add(empty);
            }
        }
    }

    private VisualElement BuildSaveSlotCard(SaveMetadata slot, bool isManual) {
        var card = new VisualElement();
        card.AddToClassList("save-slot-card");

        var info = new VisualElement();
        info.AddToClassList("save-slot-info");

        var nameLabel = new Label(slot.DisplayName);
        nameLabel.AddToClassList("save-slot-name");

        var metaLabel = new Label($"{slot.CompanyName} | Day {slot.InGameDay}, Month {slot.InGameMonth}, Year {slot.InGameYear} | ${slot.Money:N0} | {slot.EmployeeCount} employees");
        metaLabel.AddToClassList("save-slot-meta");

        info.Add(nameLabel);
        info.Add(metaLabel);
        card.Add(info);

        var actions = new VisualElement();
        actions.AddToClassList("save-slot-actions");

        var slotName = slot.SlotName;

        var loadBtn = new Button();
        loadBtn.text = "Load";
        loadBtn.AddToClassList("btn-secondary");
        loadBtn.clicked += () => {
            NewGameData.IsNewGame = false;
            NewGameData.LoadSlotName = slotName;
            SceneManager.LoadScene("MainGame");
        };
        actions.Add(loadBtn);

        if (isManual) {
            var deleteBtn = new Button();
            deleteBtn.text = "Delete";
            deleteBtn.AddToClassList("btn-danger");
            deleteBtn.AddToClassList("btn-sm");
            deleteBtn.clicked += () => {
                SaveManager.DeleteSave(slotName);
                PopulateLoadGameScreen();
            };
            actions.Add(deleteBtn);
        }

        card.Add(actions);
        return card;
    }

    // ── Difficulty ──

    private void OnDifficultyPresetChanged(ChangeEvent<string> evt) {
        DifficultyPreset preset;
        switch (evt.newValue) {
            case "Sandbox": preset = DifficultyPreset.Sandbox; break;
            case "Easy":    preset = DifficultyPreset.Easy;    break;
            case "Hard":    preset = DifficultyPreset.Hard;    break;
            case "Custom":  preset = DifficultyPreset.Custom;  break;
            default:        preset = DifficultyPreset.Normal;  break;
        }
        if (preset != DifficultyPreset.Custom) {
            _currentDifficulty = DifficultySettings.Default(preset);
            SyncUIFromDifficulty();
        } else {
            _currentDifficulty.Preset = DifficultyPreset.Custom;
        }
        RefreshDifficultyDetailsVisibility();
    }

    private void OnDifficultySliderChanged(ChangeEvent<int> evt) {
        _currentDifficulty.Preset = DifficultyPreset.Custom;
        if (_difficultyPresetDropdown != null)
            _difficultyPresetDropdown.SetValueWithoutNotify("Custom");
        ReadSlidersIntoDifficulty();
    }

    private void OnDifficultyToggleChanged(ChangeEvent<bool> evt) {
        _currentDifficulty.Preset = DifficultyPreset.Custom;
        if (_difficultyPresetDropdown != null)
            _difficultyPresetDropdown.SetValueWithoutNotify("Custom");
        ReadTogglesIntoDifficulty();
        RefreshSliderEnabledStates();
    }

    private void ReadSlidersIntoDifficulty() {
        if (_startingCashSlider != null)      _currentDifficulty.StartingCash                  = _startingCashSlider.value;
        if (_contractRewardSlider != null)    _currentDifficulty.ContractRewardMultiplier        = _contractRewardSlider.value / 100f;
        if (_taxRateSlider != null)           _currentDifficulty.TaxRate                        = _taxRateSlider.value / 100f;
        if (_salarySlider != null)            _currentDifficulty.SalaryMultiplier               = _salarySlider.value / 100f;
        if (_skillGrowthSlider != null)       _currentDifficulty.SkillGrowthMultiplier          = _skillGrowthSlider.value / 100f;
        if (_moraleDecaySlider != null)       _currentDifficulty.MoraleDecayMultiplier          = _moraleDecaySlider.value / 100f;
        if (_competitorSlider != null)        _currentDifficulty.CompetitorAggressionMultiplier = _competitorSlider.value / 100f;
        if (_marketDifficultySlider != null)  _currentDifficulty.MarketDifficultyMultiplier     = _marketDifficultySlider.value / 100f;
        if (_loanInterestSlider != null)      _currentDifficulty.LoanInterestMultiplier         = _loanInterestSlider.value / 100f;
        if (_productWorkRateSlider != null)   _currentDifficulty.ProductWorkRateMultiplier       = _productWorkRateSlider.value / 100f;
        if (_bugRateSlider != null)           _currentDifficulty.BugRateMultiplier               = _bugRateSlider.value / 100f;
        if (_reviewHarshnessSlider != null)   _currentDifficulty.ReviewHarshnessMultiplier       = _reviewHarshnessSlider.value / 100f;
        if (_productRevenueSlider != null)    _currentDifficulty.ProductRevenueMultiplier        = _productRevenueSlider.value / 100f;
    }

    private void ReadTogglesIntoDifficulty() {
        if (_taxToggle != null)         _currentDifficulty.TaxEnabled         = _taxToggle.value;
        if (_salariesToggle != null)    _currentDifficulty.SalariesEnabled    = _salariesToggle.value;
        if (_quittingToggle != null)    _currentDifficulty.QuittingEnabled    = _quittingToggle.value;
        if (_competitorsToggle != null) _currentDifficulty.CompetitorsEnabled = _competitorsToggle.value;
        if (_bankruptcyToggle != null)  _currentDifficulty.BankruptcyEnabled  = _bankruptcyToggle.value;
    }

    private void SyncUIFromDifficulty() {
        if (_startingCashSlider != null)     _startingCashSlider.SetValueWithoutNotify(_currentDifficulty.StartingCash);
        if (_contractRewardSlider != null)   _contractRewardSlider.SetValueWithoutNotify((int)(_currentDifficulty.ContractRewardMultiplier * 100f));
        if (_taxRateSlider != null)          _taxRateSlider.SetValueWithoutNotify((int)(_currentDifficulty.TaxRate * 100f));
        if (_salarySlider != null)           _salarySlider.SetValueWithoutNotify((int)(_currentDifficulty.SalaryMultiplier * 100f));
        if (_skillGrowthSlider != null)      _skillGrowthSlider.SetValueWithoutNotify((int)(_currentDifficulty.SkillGrowthMultiplier * 100f));
        if (_moraleDecaySlider != null)      _moraleDecaySlider.SetValueWithoutNotify((int)(_currentDifficulty.MoraleDecayMultiplier * 100f));
        if (_competitorSlider != null)       _competitorSlider.SetValueWithoutNotify((int)(_currentDifficulty.CompetitorAggressionMultiplier * 100f));
        if (_marketDifficultySlider != null) _marketDifficultySlider.SetValueWithoutNotify((int)(_currentDifficulty.MarketDifficultyMultiplier * 100f));
        if (_loanInterestSlider != null)     _loanInterestSlider.SetValueWithoutNotify((int)(_currentDifficulty.LoanInterestMultiplier * 100f));
        if (_productWorkRateSlider != null)  _productWorkRateSlider.SetValueWithoutNotify((int)(_currentDifficulty.ProductWorkRateMultiplier * 100f));
        if (_bugRateSlider != null)          _bugRateSlider.SetValueWithoutNotify((int)(_currentDifficulty.BugRateMultiplier * 100f));
        if (_reviewHarshnessSlider != null)  _reviewHarshnessSlider.SetValueWithoutNotify((int)(_currentDifficulty.ReviewHarshnessMultiplier * 100f));
        if (_productRevenueSlider != null)   _productRevenueSlider.SetValueWithoutNotify((int)(_currentDifficulty.ProductRevenueMultiplier * 100f));
        if (_taxToggle != null)              _taxToggle.SetValueWithoutNotify(_currentDifficulty.TaxEnabled);
        if (_salariesToggle != null)         _salariesToggle.SetValueWithoutNotify(_currentDifficulty.SalariesEnabled);
        if (_quittingToggle != null)         _quittingToggle.SetValueWithoutNotify(_currentDifficulty.QuittingEnabled);
        if (_competitorsToggle != null)      _competitorsToggle.SetValueWithoutNotify(_currentDifficulty.CompetitorsEnabled);
        if (_bankruptcyToggle != null)       _bankruptcyToggle.SetValueWithoutNotify(_currentDifficulty.BankruptcyEnabled);
        RefreshSliderEnabledStates();
    }

    private void RefreshSliderEnabledStates() {
        if (_taxRateSlider != null)     _taxRateSlider.SetEnabled(_currentDifficulty.TaxEnabled);
        if (_salarySlider != null)      _salarySlider.SetEnabled(_currentDifficulty.SalariesEnabled);
        if (_competitorSlider != null)  _competitorSlider.SetEnabled(_currentDifficulty.CompetitorsEnabled);
        if (_marketDifficultySlider != null) _marketDifficultySlider.SetEnabled(_currentDifficulty.CompetitorsEnabled);
    }

    private void RefreshDifficultyDetailsVisibility() {
        if (_difficultyDetails == null) return;
        _difficultyDetails.style.display = DisplayStyle.Flex;
        bool isCustom = _currentDifficulty.Preset == DifficultyPreset.Custom;
        _startingCashSlider?.SetEnabled(isCustom);
        _contractRewardSlider?.SetEnabled(isCustom);
        _taxToggle?.SetEnabled(isCustom);
        _salariesToggle?.SetEnabled(isCustom);
        _quittingToggle?.SetEnabled(isCustom);
        _competitorsToggle?.SetEnabled(isCustom);
        _bankruptcyToggle?.SetEnabled(isCustom);
        _skillGrowthSlider?.SetEnabled(isCustom);
        _moraleDecaySlider?.SetEnabled(isCustom);
        _loanInterestSlider?.SetEnabled(isCustom);
        _productWorkRateSlider?.SetEnabled(isCustom);
        _bugRateSlider?.SetEnabled(isCustom);
        _reviewHarshnessSlider?.SetEnabled(isCustom);
        _productRevenueSlider?.SetEnabled(isCustom);
        if (isCustom)
            RefreshSliderEnabledStates();
        else {
            _taxRateSlider?.SetEnabled(false);
            _salarySlider?.SetEnabled(false);
            _competitorSlider?.SetEnabled(false);
            _marketDifficultySlider?.SetEnabled(false);
        }
    }
}
