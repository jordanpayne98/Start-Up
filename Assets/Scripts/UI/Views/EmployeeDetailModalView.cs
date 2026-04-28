using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// UXML-backed employee details modal.
/// Initialize loads the EmployeeDetailModal.uxml template into the provided root,
/// queries all named elements, and wires tab switching + action button handlers.
/// Bind performs data-only updates from the ViewModel with no structural changes.
/// </summary>
public class EmployeeDetailModalView : IGameView
{
    private readonly ICommandDispatcher _dispatcher;
    private readonly IModalPresenter    _modal;

    private VisualTreeAsset _asset;
    private EmployeeDetailModalViewModel _vm;
    private VisualElement _root;

    // ── Header ───────────────────────────────────────────────────────────────

    private Label         _nameLabel;
    private Label         _ageLabel;
    private Label         _seniorityLabel;
    private Label         _rolePill;
    private Label         _roleFamilyLabel;
    private Label         _founderBadge;
    private Label         _teamLabel;
    private Label         _contractTypeLabel;
    private Label         _salaryLabel;
    private Label         _benchmarkLabel;
    private Label         _contractExpiryLabel;
    private Label         _moraleLabel;
    private Label         _caStarsLabel;
    private Label         _paStarsLabel;
    private VisualElement _badgesContainer;

    // ── Tabs ─────────────────────────────────────────────────────────────────

    private readonly List<Button>       _tabButtons  = new List<Button>(6);
    private readonly List<VisualElement> _tabContents = new List<VisualElement>(6);

    // ── Overview panel elements ─────────────────────────────────────────────

    // Left panel
    private VisualElement _roleFitRows;
    private Label         _summaryCurrentRole;
    private Label         _summaryRoleFit;
    private Label         _summaryCurrentCA;
    private Label         _summaryBestRole;
    private Label         _summaryBestCA;
    private Label         _summarySuggestion;
    private Label         _teamFitName;
    private Label         _teamFitLabel;
    private Label         _teamFitAssignment;
    private VisualElement _teamFitNotes;

    // Center panel
    private VisualElement _coreSkillsRows;
    private VisualElement _supportingSkillsRows;
    private VisualElement _attributesRows;
    private Button        _viewAllSkillsBtn;

    // Right panel
    private Label         _personalityLabel;
    private VisualElement _hiddenSignalRows;
    private Label         _salaryBenchmarkNote;
    private Label         _contractRiskNote;
    private Label         _recommendationText;

    // Team impact
    private VisualElement _teamImpactSection;
    private VisualElement _teamImpactRows;

    // ── Personal tab elements ─────────────────────────────────────────────────

    private Label         _personalPersonalityLabel;
    private Label         _personalPersonalityDescription;
    private VisualElement _personalSignalRows;
    private Label         _retentionRiskLabel;
    private VisualElement _retentionReasonsList;
    private Label         _personalMoraleStatus;
    private Label         _personalStressStatus;
    private Label         _burnoutRiskLabel;
    private VisualElement _founderTraitsSection;
    private Label         _founderArchetypeLabel;
    private Label         _founderTraitLabel;
    private Label         _founderWeaknessLabel;

    // ── Performance tab elements ──────────────────────────────────────────────

    private Label         _formScoreLabel;
    private Label         _avgOutputLabel;
    private Label         _avgQualityLabel;
    private Label         _bugContributionLabel;
    private VisualElement _workHistoryList;
    private Label         _workHistoryEmpty;

    // ── Growth tab elements ───────────────────────────────────────────────────

    private Label         _growthCurrentCA;
    private Label         _growthBestCA;
    private Label         _growthPA;
    private Label         _growthPADistance;
    private Label         _growthOutlookLabel;
    private Label         _growthAgeEffect;
    private Label         _growthMentoringInfluence;
    private Label         _growthPlateauWarning;
    private VisualElement _attributeTrendRows;
    private VisualElement _skillGrowthList;

    // ── Bottom status cards (value labels only) ───────────────────────────────

    private Label         _statusMoraleValue;
    private Label         _statusStressValue;
    private Label         _statusFormValue;
    private Label         _statusGrowthValue;
    private Label         _statusSalaryValue;
    private Label         _statusContractValue;
    private Label         _statusTeamImpactValue;
    private Label         _statusRecentWorkValue;

    private VisualElement _statusCardMorale;
    private VisualElement _statusCardStress;
    private VisualElement _statusCardForm;
    private VisualElement _statusCardGrowth;
    private VisualElement _statusCardSalary;
    private VisualElement _statusCardContract;
    private VisualElement _statusCardTeamImpact;
    private VisualElement _statusCardRecentWork;

    // ── Career tab elements ───────────────────────────────────────────────────

    private Label         _careerDaysEmployed;
    private Label         _careerProductsShipped;
    private Label         _careerContractsCompleted;
    private Label         _careerAvgQuality;
    private Label         _careerTotalSalary;
    private Label         _careerSkillIncreases;
    private VisualElement _careerTimeline;
    private Label         _careerEmpty;

    // ── Comparison tab elements ────────────────────────────────────────────────

    private VisualElement _comparisonTargetSelector;
    private VisualElement _comparisonMetricsList;
    private readonly List<Button> _comparisonTargetButtons = new List<Button>(4);

    // ── Footer action buttons ────────────────────────────────────────────────

    private Button _btnClose;
    private Button _btnCloseFooter;
    private Button _btnAssignTeam;
    private Button _btnRenewContract;
    private Button _btnCompare;
    private Button _btnFireEmployee;

    // ── Constructor ───────────────────────────────────────────────────────────

    public EmployeeDetailModalView(ICommandDispatcher dispatcher, IModalPresenter modal)
    {
        _dispatcher = dispatcher;
        _modal      = modal;
    }

    /// <summary>Called by WindowManager before Initialize to inject the UXML template asset.</summary>
    public void SetAsset(VisualTreeAsset asset)
    {
        _asset = asset;
    }

    // ── IGameView ─────────────────────────────────────────────────────────────

    public void Initialize(VisualElement root)
    {
        _root = root;

        // Clone the UXML template into the provided root
        if (_asset != null)
        {
            _asset.CloneTree(root);
        }
        else
        {
            root.AddToClassList("employee-detail-modal");
            Debug.LogWarning("[EmployeeDetailModalView] No VisualTreeAsset assigned. " +
                             "Assign employeeDetailAsset in WindowManager inspector.");
        }

        // ── Query header ──────────────────────────────────────────────────────
        _nameLabel           = root.Q<Label>("name-label");
        _ageLabel            = root.Q<Label>("age-label");
        _seniorityLabel      = root.Q<Label>("seniority-label");
        _rolePill            = root.Q<Label>("role-pill");
        _roleFamilyLabel     = root.Q<Label>("role-family-label");
        _founderBadge        = root.Q<Label>("founder-badge");
        _teamLabel           = root.Q<Label>("team-label");
        _contractTypeLabel   = root.Q<Label>("contract-type-label");
        _salaryLabel         = root.Q<Label>("salary-label");
        _benchmarkLabel      = root.Q<Label>("benchmark-label");
        _contractExpiryLabel = root.Q<Label>("contract-expiry-label");
        _moraleLabel         = root.Q<Label>("morale-label");
        _caStarsLabel        = root.Q<Label>("ca-stars-label");
        _paStarsLabel        = root.Q<Label>("pa-stars-label");
        _badgesContainer     = root.Q<VisualElement>("badges-container");

        // ── Query tabs ────────────────────────────────────────────────────────
        _tabButtons.Clear();
        _tabContents.Clear();

        _tabButtons.Add(root.Q<Button>("tab-overview"));
        _tabButtons.Add(root.Q<Button>("tab-personal"));
        _tabButtons.Add(root.Q<Button>("tab-performance"));
        _tabButtons.Add(root.Q<Button>("tab-growth"));
        _tabButtons.Add(root.Q<Button>("tab-career"));
        _tabButtons.Add(root.Q<Button>("tab-comparison"));

        _tabContents.Add(root.Q<VisualElement>("overview-content"));
        _tabContents.Add(root.Q<VisualElement>("personal-content"));
        _tabContents.Add(root.Q<VisualElement>("performance-content"));
        _tabContents.Add(root.Q<VisualElement>("growth-content"));
        _tabContents.Add(root.Q<VisualElement>("career-content"));
        _tabContents.Add(root.Q<VisualElement>("comparison-content"));

        // Wire tab buttons — store index in userData, single named callback
        for (int i = 0; i < _tabButtons.Count; i++)
        {
            var btn = _tabButtons[i];
            if (btn == null) continue;
            btn.userData = i;
            btn.RegisterCallback<ClickEvent>(OnTabButtonClicked);
        }

        // Show default tab
        ShowTab(0);

        // ── Query overview panel elements ──────────────────────────────────────
        _roleFitRows        = root.Q<VisualElement>("role-fit-rows");
        _summaryCurrentRole = root.Q<Label>("summary-current-role");
        _summaryRoleFit     = root.Q<Label>("summary-role-fit");
        _summaryCurrentCA   = root.Q<Label>("summary-current-ca");
        _summaryBestRole    = root.Q<Label>("summary-best-role");
        _summaryBestCA      = root.Q<Label>("summary-best-ca");
        _summarySuggestion  = root.Q<Label>("summary-suggestion");
        _teamFitName        = root.Q<Label>("team-fit-name");
        _teamFitLabel       = root.Q<Label>("team-fit-label");
        _teamFitAssignment  = root.Q<Label>("team-fit-assignment");
        _teamFitNotes       = root.Q<VisualElement>("team-fit-notes");

        _coreSkillsRows       = root.Q<VisualElement>("core-skills-rows");
        _supportingSkillsRows = root.Q<VisualElement>("supporting-skills-rows");
        _attributesRows       = root.Q<VisualElement>("attributes-rows");
        _viewAllSkillsBtn     = root.Q<Button>("view-all-skills-btn");

        _personalityLabel     = root.Q<Label>("personality-label");
        _hiddenSignalRows     = root.Q<VisualElement>("hidden-signal-rows");
        _salaryBenchmarkNote  = root.Q<Label>("salary-benchmark-note");
        _contractRiskNote     = root.Q<Label>("contract-risk-note");
        _recommendationText   = root.Q<Label>("recommendation-text");

        _teamImpactSection    = root.Q<VisualElement>("team-impact-section");
        _teamImpactRows       = root.Q<VisualElement>("team-impact-rows");

        // Wire "View All Skills" to switch to Growth tab (index 3)
        if (_viewAllSkillsBtn != null)
            _viewAllSkillsBtn.clicked += OnViewAllSkillsClicked;

        // ── Query Personal tab ────────────────────────────────────────────────
        _personalPersonalityLabel       = root.Q<Label>("personal-personality-label");
        _personalPersonalityDescription = root.Q<Label>("personal-personality-description");
        _personalSignalRows             = root.Q<VisualElement>("personal-signal-rows");
        _retentionRiskLabel             = root.Q<Label>("retention-risk-label");
        _retentionReasonsList           = root.Q<VisualElement>("retention-reasons-list");
        _personalMoraleStatus           = root.Q<Label>("personal-morale-status");
        _personalStressStatus           = root.Q<Label>("personal-stress-status");
        _burnoutRiskLabel               = root.Q<Label>("burnout-risk-label");
        _founderTraitsSection           = root.Q<VisualElement>("founder-traits-section");
        _founderArchetypeLabel          = root.Q<Label>("founder-archetype-label");
        _founderTraitLabel              = root.Q<Label>("founder-trait-label");
        _founderWeaknessLabel           = root.Q<Label>("founder-weakness-label");

        // ── Query Performance tab ─────────────────────────────────────────────
        _formScoreLabel        = root.Q<Label>("form-score-label");
        _avgOutputLabel        = root.Q<Label>("avg-output-label");
        _avgQualityLabel       = root.Q<Label>("avg-quality-label");
        _bugContributionLabel  = root.Q<Label>("bug-contribution-label");
        _workHistoryList       = root.Q<VisualElement>("work-history-list");
        _workHistoryEmpty      = root.Q<Label>("work-history-empty");

        // ── Query Growth tab ──────────────────────────────────────────────────
        _growthCurrentCA          = root.Q<Label>("growth-current-ca");
        _growthBestCA             = root.Q<Label>("growth-best-ca");
        _growthPA                 = root.Q<Label>("growth-pa");
        _growthPADistance         = root.Q<Label>("growth-pa-distance");
        _growthOutlookLabel       = root.Q<Label>("growth-outlook-label");
        _growthAgeEffect          = root.Q<Label>("growth-age-effect");
        _growthMentoringInfluence = root.Q<Label>("growth-mentoring-influence");
        _growthPlateauWarning     = root.Q<Label>("growth-plateau-warning");
        _attributeTrendRows       = root.Q<VisualElement>("attribute-trend-rows");
        _skillGrowthList          = root.Q<VisualElement>("skill-growth-list");

        // ── Query Career tab ──────────────────────────────────────────────────
        _careerDaysEmployed       = root.Q<Label>("career-days-employed");
        _careerProductsShipped    = root.Q<Label>("career-products-shipped");
        _careerContractsCompleted = root.Q<Label>("career-contracts-completed");
        _careerAvgQuality         = root.Q<Label>("career-avg-quality");
        _careerTotalSalary        = root.Q<Label>("career-total-salary");
        _careerSkillIncreases     = root.Q<Label>("career-skill-increases");
        _careerTimeline           = root.Q<VisualElement>("career-timeline");
        _careerEmpty              = root.Q<Label>("career-empty");

        // ── Query Comparison tab ──────────────────────────────────────────────
        _comparisonTargetSelector = root.Q<VisualElement>("comparison-target-selector");
        _comparisonMetricsList    = root.Q<VisualElement>("comparison-metrics-list");

        // ── Query bottom status cards ──────────────────────────────────────────
        _statusCardMorale     = root.Q<VisualElement>("status-card-morale");
        _statusCardStress     = root.Q<VisualElement>("status-card-stress");
        _statusCardForm       = root.Q<VisualElement>("status-card-form");
        _statusCardGrowth     = root.Q<VisualElement>("status-card-growth");
        _statusCardSalary     = root.Q<VisualElement>("status-card-salary");
        _statusCardContract   = root.Q<VisualElement>("status-card-contract");
        _statusCardTeamImpact = root.Q<VisualElement>("status-card-team-impact");
        _statusCardRecentWork = root.Q<VisualElement>("status-card-recent-work");

        _statusMoraleValue     = root.Q<Label>("status-morale-value");
        _statusStressValue     = root.Q<Label>("status-stress-value");
        _statusFormValue       = root.Q<Label>("status-form-value");
        _statusGrowthValue     = root.Q<Label>("status-growth-value");
        _statusSalaryValue     = root.Q<Label>("status-salary-value");
        _statusContractValue   = root.Q<Label>("status-contract-value");
        _statusTeamImpactValue = root.Q<Label>("status-team-impact-value");
        _statusRecentWorkValue = root.Q<Label>("status-recent-work-value");

        // ── Query footer ──────────────────────────────────────────────────────
        _btnClose        = root.Q<Button>("btn-close");
        _btnCloseFooter  = root.Q<Button>("btn-close-footer");
        _btnAssignTeam   = root.Q<Button>("btn-assign-team");
        _btnRenewContract = root.Q<Button>("btn-renew-contract");
        _btnCompare      = root.Q<Button>("btn-compare");
        _btnFireEmployee = root.Q<Button>("btn-fire-employee");

        // Wire action buttons
        if (_btnClose        != null) _btnClose.clicked        += OnCloseClicked;
        if (_btnCloseFooter  != null) _btnCloseFooter.clicked  += OnCloseClicked;
        if (_btnAssignTeam   != null) _btnAssignTeam.clicked   += OnAssignTeamClicked;
        if (_btnRenewContract != null) _btnRenewContract.clicked += OnRenewContractClicked;
        if (_btnCompare      != null) _btnCompare.clicked      += OnCompareClicked;
        if (_btnFireEmployee != null) _btnFireEmployee.clicked  += OnFireEmployeeClicked;
    }

    public void Bind(IViewModel viewModel)
    {
        _vm = viewModel as EmployeeDetailModalViewModel;
        if (_vm == null) return;

        if (_vm.IsInactiveEmployee)
        {
            BindInactiveState();
            return;
        }

        BindHeader();
        BindBadges();
        BindOverviewTab();
        BindBottomStatusCards();
        BindActionVisibility();
        BindPersonalTab();
        BindPerformanceTab();
        BindGrowthTab();
        BindCareerTab();
        BindComparisonTab();
    }

    public void Dispose()
    {
        // Unregister tab handlers
        for (int i = 0; i < _tabButtons.Count; i++)
        {
            var btn = _tabButtons[i];
            if (btn == null) continue;
            btn.UnregisterCallback<ClickEvent>(OnTabButtonClicked);
        }
        _tabButtons.Clear();
        _tabContents.Clear();

        if (_viewAllSkillsBtn != null) _viewAllSkillsBtn.clicked -= OnViewAllSkillsClicked;
        if (_btnClose        != null) _btnClose.clicked        -= OnCloseClicked;
        if (_btnCloseFooter  != null) _btnCloseFooter.clicked  -= OnCloseClicked;
        if (_btnAssignTeam   != null) _btnAssignTeam.clicked   -= OnAssignTeamClicked;
        if (_btnRenewContract != null) _btnRenewContract.clicked -= OnRenewContractClicked;
        if (_btnCompare      != null) _btnCompare.clicked      -= OnCompareClicked;
        if (_btnFireEmployee != null) _btnFireEmployee.clicked  -= OnFireEmployeeClicked;

        // Null Personal tab
        _personalPersonalityLabel       = null;
        _personalPersonalityDescription = null;
        _personalSignalRows             = null;
        _retentionRiskLabel             = null;
        _retentionReasonsList           = null;
        _personalMoraleStatus           = null;
        _personalStressStatus           = null;
        _burnoutRiskLabel               = null;
        _founderTraitsSection           = null;
        _founderArchetypeLabel          = null;
        _founderTraitLabel              = null;
        _founderWeaknessLabel           = null;

        // Null Performance tab
        _formScoreLabel       = null;
        _avgOutputLabel       = null;
        _avgQualityLabel      = null;
        _bugContributionLabel = null;
        _workHistoryList      = null;
        _workHistoryEmpty     = null;

        // Null Growth tab
        _growthCurrentCA          = null;
        _growthBestCA             = null;
        _growthPA                 = null;
        _growthPADistance         = null;
        _growthOutlookLabel       = null;
        _growthAgeEffect          = null;
        _growthMentoringInfluence = null;
        _growthPlateauWarning     = null;
        _attributeTrendRows       = null;
        _skillGrowthList          = null;

        // Null Career tab
        _careerDaysEmployed       = null;
        _careerProductsShipped    = null;
        _careerContractsCompleted = null;
        _careerAvgQuality         = null;
        _careerTotalSalary        = null;
        _careerSkillIncreases     = null;
        _careerTimeline           = null;
        _careerEmpty              = null;

        // Null Comparison tab
        for (int i = 0; i < _comparisonTargetButtons.Count; i++)
        {
            var btn = _comparisonTargetButtons[i];
            if (btn != null)
                btn.UnregisterCallback<ClickEvent>(OnComparisonTargetClicked);
        }
        _comparisonTargetButtons.Clear();
        _comparisonTargetSelector = null;
        _comparisonMetricsList    = null;

        _vm   = null;
        _root = null;
    }

    // ── Tab switching ─────────────────────────────────────────────────────────

    private void OnTabButtonClicked(ClickEvent evt)
    {
        if (evt.target is VisualElement el && el.userData is int index)
            ShowTab(index);
    }

    private void ShowTab(int index)
    {
        if (_vm != null) _vm.ActiveTabIndex = index;

        for (int i = 0; i < _tabButtons.Count; i++)
        {
            var btn = _tabButtons[i];
            if (btn != null)
                btn.EnableInClassList("tab--active", i == index);
        }

        for (int i = 0; i < _tabContents.Count; i++)
        {
            var content = _tabContents[i];
            if (content != null)
                content.EnableInClassList("edm-tab-content--hidden", i != index);
        }
    }

    // ── Bind helpers ──────────────────────────────────────────────────────────

    private void BindHeader()
    {
        if (_nameLabel      != null) _nameLabel.text      = _vm.Name;
        if (_ageLabel       != null) _ageLabel.text       = _vm.Age + " yrs";
        if (_seniorityLabel != null) _seniorityLabel.text = _vm.SeniorityLabel;
        if (_teamLabel      != null) _teamLabel.text      = _vm.TeamName;
        if (_salaryLabel    != null) _salaryLabel.text    = _vm.SalaryText;
        if (_contractTypeLabel  != null) _contractTypeLabel.text  = _vm.EmploymentType;
        if (_contractExpiryLabel != null) _contractExpiryLabel.text = _vm.ContractExpiryText;
        if (_caStarsLabel   != null) _caStarsLabel.text   = _vm.CAStarsText;
        if (_paStarsLabel   != null) _paStarsLabel.text   = _vm.PAStarsText;

        // Role pill
        if (_rolePill != null)
        {
            _rolePill.text = _vm.RoleName;
            UIFormatting.ClearRolePillClasses(_rolePill);
            _rolePill.AddToClassList(_vm.RolePillClass);
        }

        // Role family
        if (_roleFamilyLabel != null)
        {
            _roleFamilyLabel.text = string.IsNullOrEmpty(_vm.RoleFamilyName)
                ? ""
                : "· " + _vm.RoleFamilyName;
        }

        // Founder badge
        if (_founderBadge != null)
            _founderBadge.style.display = _vm.IsFounder ? DisplayStyle.Flex : DisplayStyle.None;

        // Benchmark badge
        if (_benchmarkLabel != null)
        {
            _benchmarkLabel.text = _vm.BenchmarkLabel;
            SetSingleBadgeClass(_benchmarkLabel, _vm.BenchmarkClass,
                "badge--danger", "badge--warning", "badge--neutral", "badge--success", "badge--accent");
        }

        // Morale
        if (_moraleLabel != null)
        {
            _moraleLabel.text = _vm.MoraleLabel;
            SetSingleBadgeClass(_moraleLabel, _vm.MoraleClass,
                "text-success", "text-warning", "text-danger");
        }
    }

    private void BindBadges()
    {
        if (_badgesContainer == null) return;
        _badgesContainer.Clear();

        var badges = _vm.BadgeList;
        if (badges == null) return;

        int count = badges.Count;
        for (int i = 0; i < count; i++)
        {
            var data  = badges[i];
            var chip  = new Label(data.Label);
            chip.AddToClassList("badge");
            chip.AddToClassList("edm-badge-chip");
            if (!string.IsNullOrEmpty(data.UssClass))
                chip.AddToClassList(data.UssClass);
            _badgesContainer.Add(chip);
        }
    }

    private void BindBottomStatusCards()
    {
        SetStatusCard(_statusCardMorale,     _statusMoraleValue,     _vm.MoraleCardText,      _vm.MoraleCardClass);
        SetStatusCard(_statusCardStress,     _statusStressValue,     _vm.StressCardText,      _vm.StressCardClass);
        SetStatusCard(_statusCardForm,       _statusFormValue,       _vm.FormCardText,        _vm.FormCardClass);
        SetStatusCard(_statusCardGrowth,     _statusGrowthValue,     _vm.GrowthCardText,      _vm.GrowthCardClass);
        SetStatusCard(_statusCardSalary,     _statusSalaryValue,     _vm.SalaryCardText,      _vm.SalaryCardClass);
        SetStatusCard(_statusCardContract,   _statusContractValue,   _vm.ContractCardText,    _vm.ContractCardClass);
        SetStatusCard(_statusCardTeamImpact, _statusTeamImpactValue, _vm.TeamImpactCardText,  _vm.TeamImpactCardClass);
        SetStatusCard(_statusCardRecentWork, _statusRecentWorkValue, _vm.RecentWorkCardText,  _vm.RecentWorkCardClass);
    }

    private void BindActionVisibility()
    {
        if (_btnAssignTeam   != null)
            _btnAssignTeam.style.display   = _vm.ShowAssignTeam   ? DisplayStyle.Flex : DisplayStyle.None;
        if (_btnRenewContract != null)
            _btnRenewContract.style.display = _vm.ShowRenewContract ? DisplayStyle.Flex : DisplayStyle.None;
        if (_btnFireEmployee != null)
            _btnFireEmployee.style.display = _vm.ShowFireEmployee  ? DisplayStyle.Flex : DisplayStyle.None;
        if (_btnCompare != null)
            _btnCompare.style.display      = _vm.ShowCompare       ? DisplayStyle.Flex : DisplayStyle.None;
    }

    // ── Overview tab binding ─────────────────────────────────────────────────

    private void BindOverviewTab()
    {
        BindRoleFitPanel();
        BindSkillsPanel();
        BindAttributesPanel();
        BindReportPanel();
        BindTeamImpactPanel();
    }

    private void BindRoleFitPanel()
    {
        var data = _vm.OverviewRoleFit;

        // Role fit rows
        if (_roleFitRows != null)
        {
            _roleFitRows.Clear();
            if (data.TopRoleFits != null)
            {
                int count = data.TopRoleFits.Length;
                for (int i = 0; i < count; i++)
                {
                    var entry = data.TopRoleFits[i];
                    var row = new VisualElement();
                    row.AddToClassList("role-suit-row");
                    if (entry.IsCurrentRole) row.AddToClassList("role-suit-row--assigned");

                    var dot = new VisualElement();
                    dot.AddToClassList("role-suit-dot");
                    dot.AddToClassList(entry.SuitabilityClass);
                    row.Add(dot);

                    var nameLabel = new Label(entry.RoleName);
                    nameLabel.AddToClassList("edm-stat-row__name");
                    row.Add(nameLabel);

                    var suitLabel = new Label(entry.Suitability.ToString());
                    suitLabel.AddToClassList("edm-stat-row__value");
                    row.Add(suitLabel);

                    _roleFitRows.Add(row);
                }
            }
        }

        // Current role summary
        if (_summaryCurrentRole != null) _summaryCurrentRole.text = _vm.RoleName;
        if (_summaryRoleFit     != null) _summaryRoleFit.text     = data.CurrentRoleFitLabel;
        if (_summaryCurrentCA   != null) _summaryCurrentCA.text   = data.CurrentCA.ToString();
        if (_summaryBestRole    != null) _summaryBestRole.text    = data.BestRoleName;
        if (_summaryBestCA      != null) _summaryBestCA.text      = data.BestCA.ToString();
        if (_summarySuggestion  != null)
        {
            _summarySuggestion.text = data.SuggestionText ?? "";
            _summarySuggestion.style.display = string.IsNullOrEmpty(data.SuggestionText)
                ? DisplayStyle.None : DisplayStyle.Flex;
        }

        // Team fit
        if (_teamFitName       != null) _teamFitName.text       = data.TeamName;
        if (_teamFitLabel      != null) _teamFitLabel.text      = data.TeamFitLabel;
        if (_teamFitAssignment != null) _teamFitAssignment.text = data.AssignmentText ?? "";
        if (_teamFitNotes != null)
        {
            _teamFitNotes.Clear();
            if (data.TeamFitNotes != null)
            {
                for (int i = 0; i < data.TeamFitNotes.Length; i++)
                {
                    var note = new Label(data.TeamFitNotes[i]);
                    note.AddToClassList("edm-team-fit-note");
                    _teamFitNotes.Add(note);
                }
            }
        }
    }

    private void BindSkillsPanel()
    {
        var data = _vm.OverviewSkills;
        PopulateSkillRows(_coreSkillsRows, data.CoreSkills);
        PopulateSkillRows(_supportingSkillsRows, data.SupportingSkills);
    }

    private static void PopulateSkillRows(
        VisualElement container,
        EmployeeDetailModalViewModel.SkillDisplayEntry[] entries)
    {
        if (container == null) return;
        container.Clear();
        if (entries == null) return;

        int count = entries.Length;
        for (int i = 0; i < count; i++)
        {
            var e = entries[i];
            var row = new VisualElement();
            row.AddToClassList("edm-stat-row");

            var nameLabel = new Label(e.Name);
            nameLabel.AddToClassList("edm-stat-row__name");
            row.Add(nameLabel);

            var valueLabel = new Label(e.Value.ToString());
            valueLabel.AddToClassList("edm-stat-row__value");
            row.Add(valueLabel);

            // Bar
            var barBg = new VisualElement();
            barBg.AddToClassList("edm-stat-row__bar-bg");
            var barFill = new VisualElement();
            barFill.AddToClassList("edm-stat-row__bar-fill");
            float pct = Mathf.Clamp01(e.Value / 20f) * 100f;
            barFill.style.width = new StyleLength(new Length(pct, LengthUnit.Percent));
            // Color by tier
            if (e.Value >= 14)      barFill.AddToClassList("edm-stat-row__bar-fill--high");
            else if (e.Value >= 8)  barFill.AddToClassList("edm-stat-row__bar-fill--mid");
            else                    barFill.AddToClassList("edm-stat-row__bar-fill--low");
            barBg.Add(barFill);
            row.Add(barBg);

            // Delta arrow
            var arrow = new Label();
            arrow.AddToClassList("edm-stat-row__arrow");
            if (e.DeltaDirection > 0)
            {
                arrow.text = "\u25B2";
                arrow.AddToClassList("skill-row__value--up");
            }
            else if (e.DeltaDirection < 0)
            {
                arrow.text = "\u25BC";
                arrow.AddToClassList("skill-row__value--down");
            }
            else
            {
                arrow.style.display = DisplayStyle.None;
            }
            row.Add(arrow);

            container.Add(row);
        }
    }

    private void BindAttributesPanel()
    {
        if (_attributesRows == null) return;
        _attributesRows.Clear();

        var data = _vm.OverviewAttributes;
        if (data.Attributes == null) return;

        int count = data.Attributes.Length;
        for (int i = 0; i < count; i++)
        {
            var attr = data.Attributes[i];
            var row = new VisualElement();
            row.AddToClassList("edm-stat-row");

            var nameLabel = new Label(attr.Name);
            nameLabel.AddToClassList("edm-stat-row__name");
            row.Add(nameLabel);

            var valueLabel = new Label(attr.Value.ToString());
            valueLabel.AddToClassList("edm-stat-row__value");
            row.Add(valueLabel);

            var barBg = new VisualElement();
            barBg.AddToClassList("edm-stat-row__bar-bg");
            var barFill = new VisualElement();
            barFill.AddToClassList("edm-stat-row__bar-fill");
            float pct = Mathf.Clamp01(attr.Value / 20f) * 100f;
            barFill.style.width = new StyleLength(new Length(pct, LengthUnit.Percent));
            if (attr.Value >= 14)       barFill.AddToClassList("edm-stat-row__bar-fill--high");
            else if (attr.Value >= 8)   barFill.AddToClassList("edm-stat-row__bar-fill--mid");
            else                        barFill.AddToClassList("edm-stat-row__bar-fill--low");
            barBg.Add(barFill);
            row.Add(barBg);

            _attributesRows.Add(row);
        }
    }

    private void BindReportPanel()
    {
        var data = _vm.OverviewReport;

        // Personality badge
        if (_personalityLabel != null)
        {
            _personalityLabel.text = data.PersonalityLabel ?? "—";
            _personalityLabel.RemoveFromClassList("badge--positive");
            _personalityLabel.RemoveFromClassList("badge--negative");
            _personalityLabel.RemoveFromClassList("badge--neutral");
            if (!string.IsNullOrEmpty(data.PersonalityClass))
                _personalityLabel.AddToClassList(data.PersonalityClass);
        }

        // Hidden signals
        if (_hiddenSignalRows != null)
        {
            _hiddenSignalRows.Clear();
            if (data.Signals != null)
            {
                int count = data.Signals.Length;
                for (int i = 0; i < count; i++)
                {
                    var sig = data.Signals[i];
                    var row = new VisualElement();
                    row.AddToClassList("edm-signal-row");

                    var nameLabel = new Label(sig.Name);
                    nameLabel.AddToClassList("edm-signal-row__name");
                    row.Add(nameLabel);

                    var labelEl = new Label(sig.Label);
                    labelEl.AddToClassList("edm-signal-row__label");
                    if (!string.IsNullOrEmpty(sig.LabelClass))
                        labelEl.AddToClassList(sig.LabelClass);
                    row.Add(labelEl);

                    _hiddenSignalRows.Add(row);
                }
            }
        }

        // Notes
        if (_salaryBenchmarkNote != null)
        {
            _salaryBenchmarkNote.text = data.SalaryBenchmarkNote ?? "";
            _salaryBenchmarkNote.style.display = string.IsNullOrEmpty(data.SalaryBenchmarkNote)
                ? DisplayStyle.None : DisplayStyle.Flex;
        }
        if (_contractRiskNote != null)
        {
            _contractRiskNote.text = data.ContractRiskNote ?? "";
            _contractRiskNote.style.display = string.IsNullOrEmpty(data.ContractRiskNote)
                ? DisplayStyle.None : DisplayStyle.Flex;
        }
        if (_recommendationText != null)
        {
            _recommendationText.text = data.RecommendationText ?? "";
            _recommendationText.style.display = string.IsNullOrEmpty(data.RecommendationText)
                ? DisplayStyle.None : DisplayStyle.Flex;
        }
    }

    private void BindTeamImpactPanel()
    {
        var data = _vm.OverviewTeamImpact;

        if (_teamImpactSection != null)
            _teamImpactSection.style.display = data.HasTeam ? DisplayStyle.Flex : DisplayStyle.None;

        if (_teamImpactRows != null)
        {
            _teamImpactRows.Clear();
            if (data.Deltas != null)
            {
                int count = data.Deltas.Length;
                for (int i = 0; i < count; i++)
                {
                    var d = data.Deltas[i];
                    var row = new VisualElement();
                    row.AddToClassList("edm-team-impact-row");

                    var nameLabel = new Label(d.MeterName);
                    nameLabel.AddToClassList("edm-team-impact-row__name");
                    row.Add(nameLabel);

                    string deltaText = d.Delta > 0 ? $"+{d.Delta}" : d.Delta == 0 ? "0" : d.Delta.ToString();
                    var deltaLabel = new Label(deltaText);
                    deltaLabel.AddToClassList("edm-team-impact-row__delta");
                    if (d.Delta > 0)       deltaLabel.AddToClassList("edm-team-impact-row__delta--positive");
                    else if (d.Delta < 0) deltaLabel.AddToClassList("edm-team-impact-row__delta--negative");
                    else                   deltaLabel.AddToClassList("edm-team-impact-row__delta--neutral");
                    row.Add(deltaLabel);

                    _teamImpactRows.Add(row);
                }
            }
        }
    }

    // ── Personal tab binding ─────────────────────────────────────────────────

    private void BindPersonalTab()
    {
        var data = _vm.Personal;

        // Personality
        if (_personalPersonalityLabel != null)
        {
            _personalPersonalityLabel.text = data.PersonalityLabel ?? "—";
            _personalPersonalityLabel.RemoveFromClassList("badge--positive");
            _personalPersonalityLabel.RemoveFromClassList("badge--negative");
            _personalPersonalityLabel.RemoveFromClassList("badge--neutral");
            if (!string.IsNullOrEmpty(data.PersonalityClass))
                _personalPersonalityLabel.AddToClassList(data.PersonalityClass);
        }
        if (_personalPersonalityDescription != null)
            _personalPersonalityDescription.text = data.PersonalityDescription ?? "";

        // Signals
        if (_personalSignalRows != null)
        {
            _personalSignalRows.Clear();
            if (data.Signals != null)
            {
                int count = data.Signals.Length;
                for (int i = 0; i < count; i++)
                {
                    var sig = data.Signals[i];
                    var row = new VisualElement();
                    row.AddToClassList("edm-signal-row");

                    var nameLabel = new Label(sig.Name);
                    nameLabel.AddToClassList("edm-signal-row__name");
                    row.Add(nameLabel);

                    var labelEl = new Label(sig.Label);
                    labelEl.AddToClassList("edm-signal-row__label");
                    if (!string.IsNullOrEmpty(sig.LabelClass))
                        labelEl.AddToClassList(sig.LabelClass);
                    row.Add(labelEl);

                    var confLabel = new Label(sig.ConfidenceText);
                    confLabel.AddToClassList("edm-signal-row__confidence");
                    row.Add(confLabel);

                    _personalSignalRows.Add(row);
                }
            }
        }

        // Retention risk
        if (_retentionRiskLabel != null)
        {
            _retentionRiskLabel.text = data.RetentionRiskLabel ?? "—";
            _retentionRiskLabel.RemoveFromClassList("edm-retention-risk--low");
            _retentionRiskLabel.RemoveFromClassList("edm-retention-risk--medium");
            _retentionRiskLabel.RemoveFromClassList("edm-retention-risk--high");
            _retentionRiskLabel.RemoveFromClassList("edm-retention-risk--severe");
            if (!string.IsNullOrEmpty(data.RetentionRiskClass))
                _retentionRiskLabel.AddToClassList(data.RetentionRiskClass);
        }
        if (_retentionReasonsList != null)
        {
            _retentionReasonsList.Clear();
            if (data.RetentionReasons != null)
            {
                int count = data.RetentionReasons.Length;
                for (int i = 0; i < count; i++)
                {
                    var reasonLabel = new Label("· " + data.RetentionReasons[i]);
                    reasonLabel.AddToClassList("edm-retention-reason");
                    _retentionReasonsList.Add(reasonLabel);
                }
            }
        }

        // Morale / stress / burnout
        if (_personalMoraleStatus != null)
        {
            _personalMoraleStatus.text = data.MoraleStatus ?? "—";
            SetSingleBadgeClass(_personalMoraleStatus, data.MoraleClass, "text-success", "text-warning", "text-danger");
        }
        if (_personalStressStatus != null)
        {
            _personalStressStatus.text = data.StressStatus ?? "—";
            SetSingleBadgeClass(_personalStressStatus, data.StressClass, "text-success", "text-warning", "text-danger");
        }
        if (_burnoutRiskLabel != null)
        {
            _burnoutRiskLabel.text = data.ShowBurnoutWarning
                ? "High burnout risk — reduce workload or assign to a lighter project."
                : "";
            _burnoutRiskLabel.style.display = data.ShowBurnoutWarning ? DisplayStyle.Flex : DisplayStyle.None;
        }

        // Founder traits
        if (_founderTraitsSection != null)
            _founderTraitsSection.style.display = data.IsFounder ? DisplayStyle.Flex : DisplayStyle.None;
        if (data.IsFounder)
        {
            if (_founderArchetypeLabel != null) _founderArchetypeLabel.text = data.FounderArchetype ?? "—";
            if (_founderTraitLabel     != null) _founderTraitLabel.text     = data.FounderTrait     ?? "—";
            if (_founderWeaknessLabel  != null) _founderWeaknessLabel.text  = data.FounderWeakness  ?? "—";
        }
    }

    // ── Performance tab binding ──────────────────────────────────────────────

    private void BindPerformanceTab()
    {
        var data = _vm.Performance;

        // Form score
        if (_formScoreLabel != null)
        {
            _formScoreLabel.text = data.FormLabel ?? "—";
            _formScoreLabel.RemoveFromClassList("edm-form-score--strong");
            _formScoreLabel.RemoveFromClassList("edm-form-score--good");
            _formScoreLabel.RemoveFromClassList("edm-form-score--average");
            _formScoreLabel.RemoveFromClassList("edm-form-score--poor");
            _formScoreLabel.RemoveFromClassList("edm-form-score--declining");
            if (!string.IsNullOrEmpty(data.FormClass))
                _formScoreLabel.AddToClassList(data.FormClass);
        }

        // Average stats
        if (_avgOutputLabel       != null) _avgOutputLabel.text       = data.AverageOutput   ?? "—";
        if (_avgQualityLabel      != null) _avgQualityLabel.text      = data.AverageQuality  ?? "—";
        if (_bugContributionLabel != null) _bugContributionLabel.text = data.BugContribution ?? "—";

        // Work history list / empty state
        bool hasHistory = data.HasWorkHistory && data.RecentWork != null && data.RecentWork.Length > 0;

        if (_workHistoryEmpty != null)
            _workHistoryEmpty.style.display = hasHistory ? DisplayStyle.None : DisplayStyle.Flex;

        if (_workHistoryList != null)
        {
            _workHistoryList.Clear();
            if (hasHistory)
            {
                int count = data.RecentWork.Length;
                for (int i = 0; i < count; i++)
                {
                    var entry = data.RecentWork[i];
                    var card = BuildWorkHistoryRow(entry);
                    _workHistoryList.Add(card);
                }
            }
        }
    }

    private static VisualElement BuildWorkHistoryRow(
        EmployeeDetailModalViewModel.WorkHistoryDisplayEntry entry)
    {
        var card = new VisualElement();
        card.AddToClassList("edm-work-history-row");

        var header = new VisualElement();
        header.AddToClassList("edm-work-history-row__header");

        var dateLabel = new Label(entry.DateText);
        dateLabel.AddToClassList("edm-work-history-row__date");
        header.Add(dateLabel);

        var typePill = new Label(entry.WorkTypePillText);
        typePill.AddToClassList("edm-work-history-row__type-pill");
        if (!string.IsNullOrEmpty(entry.WorkTypePillClass))
            typePill.AddToClassList(entry.WorkTypePillClass);
        header.Add(typePill);

        var nameLabel = new Label(entry.WorkName);
        nameLabel.AddToClassList("edm-work-history-row__name");
        header.Add(nameLabel);

        card.Add(header);

        var detail = new VisualElement();
        detail.AddToClassList("edm-work-history-row__detail");

        if (!string.IsNullOrEmpty(entry.ContributionLabel) && entry.ContributionLabel != "—")
        {
            var contrib = new Label(entry.ContributionLabel);
            contrib.AddToClassList("edm-work-history-row__contribution");
            if (!string.IsNullOrEmpty(entry.ContributionClass))
                contrib.AddToClassList(entry.ContributionClass);
            detail.Add(contrib);
        }

        if (!string.IsNullOrEmpty(entry.OutcomeLabel))
        {
            var outcome = new Label(entry.OutcomeLabel);
            outcome.AddToClassList("edm-work-history-row__outcome");
            if (!string.IsNullOrEmpty(entry.OutcomeClass))
                outcome.AddToClassList(entry.OutcomeClass);
            detail.Add(outcome);
        }

        if (!string.IsNullOrEmpty(entry.QualityText) && entry.QualityText != "—")
        {
            var quality = new Label(entry.QualityText);
            quality.AddToClassList("edm-work-history-row__quality");
            detail.Add(quality);
        }

        if (!string.IsNullOrEmpty(entry.XpSummary))
        {
            var xp = new Label(entry.XpSummary);
            xp.AddToClassList("edm-work-history-row__xp");
            detail.Add(xp);
        }

        card.Add(detail);
        return card;
    }

    // ── Growth tab binding ───────────────────────────────────────────────────

    private void BindGrowthTab()
    {
        var data = _vm.Growth;

        if (_growthCurrentCA    != null) _growthCurrentCA.text    = data.CurrentRoleCA.ToString();
        if (_growthBestCA       != null) _growthBestCA.text       = data.BestRoleCA.ToString();
        if (_growthPA           != null) _growthPA.text           = data.PA > 0 ? data.PA.ToString() : "—";
        if (_growthPADistance   != null) _growthPADistance.text   = data.PADistance > 0 ? "+" + data.PADistance : "At ceiling";

        if (_growthOutlookLabel != null)
        {
            _growthOutlookLabel.text = data.GrowthOutlookLabel ?? "";
            _growthOutlookLabel.RemoveFromClassList("growth-outlook--strong");
            _growthOutlookLabel.RemoveFromClassList("growth-outlook--good");
            _growthOutlookLabel.RemoveFromClassList("growth-outlook--plateauing");
            _growthOutlookLabel.RemoveFromClassList("growth-outlook--declining");
            if (!string.IsNullOrEmpty(data.GrowthOutlookClass))
                _growthOutlookLabel.AddToClassList(data.GrowthOutlookClass);
            _growthOutlookLabel.style.display = string.IsNullOrEmpty(data.GrowthOutlookLabel)
                ? DisplayStyle.None : DisplayStyle.Flex;
        }

        if (_growthAgeEffect          != null) _growthAgeEffect.text          = data.AgeEffect          ?? "—";
        if (_growthMentoringInfluence != null) _growthMentoringInfluence.text = data.MentoringInfluence ?? "—";

        if (_growthPlateauWarning != null)
        {
            _growthPlateauWarning.text = data.PlateauWarning ?? "";
            _growthPlateauWarning.style.display = string.IsNullOrEmpty(data.PlateauWarning)
                ? DisplayStyle.None : DisplayStyle.Flex;
        }

        // Attribute trends
        if (_attributeTrendRows != null)
        {
            _attributeTrendRows.Clear();
            if (data.AttributeTrends != null)
            {
                int count = data.AttributeTrends.Length;
                for (int i = 0; i < count; i++)
                {
                    var trend = data.AttributeTrends[i];
                    var row = new VisualElement();
                    row.AddToClassList("edm-signal-row");

                    var nameLabel = new Label(trend.Name);
                    nameLabel.AddToClassList("edm-signal-row__name");
                    row.Add(nameLabel);

                    var trendLabel = new Label(trend.TrendLabel);
                    trendLabel.AddToClassList("edm-attr-trend-label");
                    if (!string.IsNullOrEmpty(trend.TrendClass))
                        trendLabel.AddToClassList(trend.TrendClass);
                    row.Add(trendLabel);

                    _attributeTrendRows.Add(row);
                }
            }
        }

        // Full skill list with XP bars grouped by category
        if (_skillGrowthList != null)
        {
            _skillGrowthList.Clear();
            if (data.AllSkills != null)
            {
                int count = data.AllSkills.Length;
                for (int i = 0; i < count; i++)
                {
                    var entry = data.AllSkills[i];
                    if (entry.IsCategoryHeader)
                    {
                        var header = new Label(entry.CategoryHeaderText);
                        header.AddToClassList("edm-skill-category-header");
                        _skillGrowthList.Add(header);
                    }
                    else
                    {
                        _skillGrowthList.Add(BuildSkillGrowthRow(entry));
                    }
                }
            }
        }
    }

    private static VisualElement BuildSkillGrowthRow(
        EmployeeDetailModalViewModel.SkillGrowthEntry entry)
    {
        var row = new VisualElement();
        row.AddToClassList("edm-skill-growth-row");

        var top = new VisualElement();
        top.AddToClassList("edm-skill-growth-row__top");

        var nameLabel = new Label(entry.Name);
        nameLabel.AddToClassList("edm-skill-growth-row__name");
        top.Add(nameLabel);

        var valueLabel = new Label(entry.Value.ToString());
        valueLabel.AddToClassList("edm-skill-growth-row__value");
        top.Add(valueLabel);

        var arrow = new Label();
        arrow.AddToClassList("edm-skill-growth-row__arrow");
        if (entry.DeltaDirection > 0)
        {
            arrow.text = "\u25B2";
            arrow.AddToClassList("skill-row__value--up");
        }
        else if (entry.DeltaDirection < 0)
        {
            arrow.text = "\u25BC";
            arrow.AddToClassList("skill-row__value--down");
        }
        else
        {
            arrow.style.display = DisplayStyle.None;
        }
        top.Add(arrow);
        row.Add(top);

        var xpRow = new VisualElement();
        xpRow.AddToClassList("edm-skill-growth-row__xp-row");

        var barBg = new VisualElement();
        barBg.AddToClassList("edm-xp-bar-bg");
        var barFill = new VisualElement();
        barFill.AddToClassList("edm-xp-bar-fill");
        barFill.style.width = new StyleLength(new Length(entry.XpPercent, LengthUnit.Percent));
        barBg.Add(barFill);
        xpRow.Add(barBg);

        var pctLabel = new Label(entry.XpPercent.ToString("F0") + "% to next");
        pctLabel.AddToClassList("edm-xp-pct-label");
        xpRow.Add(pctLabel);

        row.Add(xpRow);
        return row;
    }

    // ── Inactive state ────────────────────────────────────────────────────────

    private void BindInactiveState()
    {
        if (_nameLabel != null) _nameLabel.text = _vm.Name + " (Inactive)";
        if (_btnAssignTeam   != null) _btnAssignTeam.style.display   = DisplayStyle.None;
        if (_btnRenewContract != null) _btnRenewContract.style.display = DisplayStyle.None;
        if (_btnFireEmployee != null) _btnFireEmployee.style.display  = DisplayStyle.None;
        if (_btnCompare      != null) _btnCompare.style.display       = DisplayStyle.None;
    }

    // ── Career tab binding ────────────────────────────────────────────────────

    private void BindCareerTab()
    {
        var data = _vm.Career;

        if (_careerDaysEmployed       != null) _careerDaysEmployed.text       = data.DaysEmployed.ToString();
        if (_careerProductsShipped    != null) _careerProductsShipped.text    = data.ProductsShipped.ToString();
        if (_careerContractsCompleted != null) _careerContractsCompleted.text = data.ContractsCompleted.ToString();
        if (_careerAvgQuality         != null) _careerAvgQuality.text         = data.AverageQuality;
        if (_careerTotalSalary        != null) _careerTotalSalary.text        = data.TotalSalaryPaid;
        if (_careerSkillIncreases     != null) _careerSkillIncreases.text     = data.SkillIncreases.ToString();

        bool hasHistory = data.HasCareerHistory;
        if (_careerEmpty != null)
            _careerEmpty.EnableInClassList("edm-tab-content--hidden", hasHistory);

        if (_careerTimeline != null)
        {
            _careerTimeline.Clear();
            if (data.Timeline != null)
            {
                int count = data.Timeline.Length;
                for (int i = 0; i < count; i++)
                {
                    var entry = data.Timeline[i];
                    var row = new VisualElement();
                    row.AddToClassList("edm-timeline-entry");

                    var dateLabel = new Label(entry.DateText);
                    dateLabel.AddToClassList("edm-timeline-entry__date");
                    row.Add(dateLabel);

                    var typePill = new Label(entry.TypePillText);
                    typePill.AddToClassList("edm-timeline-entry__type-pill");
                    if (!string.IsNullOrEmpty(entry.TypePillClass))
                        typePill.AddToClassList(entry.TypePillClass);
                    row.Add(typePill);

                    var details = new VisualElement();
                    details.AddToClassList("edm-timeline-entry__details");

                    var titleLabel = new Label(entry.Title);
                    titleLabel.AddToClassList("edm-timeline-entry__title");
                    details.Add(titleLabel);

                    if (!string.IsNullOrEmpty(entry.Subtitle))
                    {
                        var subtitleLabel = new Label(entry.Subtitle);
                        subtitleLabel.AddToClassList("edm-timeline-entry__subtitle");
                        details.Add(subtitleLabel);
                    }

                    row.Add(details);
                    _careerTimeline.Add(row);
                }
            }
        }
    }

    // ── Comparison tab binding ────────────────────────────────────────────────

    private void BindComparisonTab()
    {
        var data = _vm.Comparison;

        // Sync target selector button pool to available targets
        if (_comparisonTargetSelector != null && data.AvailableTargets != null)
        {
            int targetCount = data.AvailableTargets.Length;

            // Grow pool if needed (factory wires handler once)
            while (_comparisonTargetButtons.Count < targetCount)
            {
                var btn = new Button();
                btn.AddToClassList("edm-comparison-target-btn");
                btn.RegisterCallback<ClickEvent>(OnComparisonTargetClicked);
                _comparisonTargetButtons.Add(btn);
                _comparisonTargetSelector.Add(btn);
            }

            // Update each button (data-only: text, userData, active class)
            for (int i = 0; i < _comparisonTargetButtons.Count; i++)
            {
                var btn = _comparisonTargetButtons[i];
                if (i < targetCount)
                {
                    var target = data.AvailableTargets[i];
                    btn.text = target.DisplayName;
                    btn.userData = target.Index;
                    btn.EnableInClassList("edm-comparison-target-btn--active",
                        target.Index == data.SelectedTargetIndex);
                    btn.style.display = DisplayStyle.Flex;
                }
                else
                {
                    btn.style.display = DisplayStyle.None;
                }
            }
        }

        // Metric rows
        if (_comparisonMetricsList != null)
        {
            _comparisonMetricsList.Clear();
            if (data.Metrics != null)
            {
                int count = data.Metrics.Length;
                for (int i = 0; i < count; i++)
                {
                    var metric = data.Metrics[i];
                    var row = new VisualElement();
                    row.AddToClassList("edm-comparison-row");

                    var nameLabel = new Label(metric.MetricName);
                    nameLabel.AddToClassList("edm-comparison-row__name");
                    row.Add(nameLabel);

                    var empLabel = new Label(metric.EmployeeValue);
                    empLabel.AddToClassList("edm-comparison-row__employee");
                    row.Add(empLabel);

                    var benchLabel = new Label(metric.ComparisonValue);
                    benchLabel.AddToClassList("edm-comparison-row__benchmark");
                    row.Add(benchLabel);

                    var diffLabel = new Label(metric.DifferenceText);
                    diffLabel.AddToClassList("edm-comparison-row__diff");
                    if (!string.IsNullOrEmpty(metric.DifferenceClass))
                        diffLabel.AddToClassList(metric.DifferenceClass);
                    row.Add(diffLabel);

                    _comparisonMetricsList.Add(row);
                }
            }
        }
    }

    // ── Action handlers ───────────────────────────────────────────────────────

    private void OnViewAllSkillsClicked()
    {
        ShowTab(3);
    }

    private void OnCloseClicked()
    {
        _modal.DismissModal();
    }

    private void OnAssignTeamClicked()
    {
        if (_vm == null) return;
        _modal.DismissModal();
    }

    private void OnRenewContractClicked()
    {
        if (_vm == null) return;
        _modal.OpenRenewalModal(_vm.CurrentEmployeeId);
    }

    private void OnCompareClicked()
    {
        ShowTab(5);
    }

    private void OnFireEmployeeClicked()
    {
        if (_vm == null || _dispatcher == null) return;
        if (_vm.IsFounder) return;
        _dispatcher.Dispatch(new FireEmployeeCommand
        {
            Tick       = _dispatcher.CurrentTick,
            EmployeeId = _vm.CurrentEmployeeId
        });
        _modal.DismissModal();
    }

    private void OnComparisonTargetClicked(ClickEvent evt)
    {
        if (evt.target is VisualElement el && el.userData is int idx && _vm != null)
        {
            _vm.SetComparisonTarget(idx);
            BindComparisonTab();
        }
    }

    // ── Static helpers ────────────────────────────────────────────────────────

    private static void SetStatusCard(
        VisualElement card,
        Label valueLabel,
        string text,
        string stateClass)
    {
        if (valueLabel != null) valueLabel.text = text ?? "—";

        if (card == null) return;
        card.RemoveFromClassList("edm-status-card--warning");
        card.RemoveFromClassList("edm-status-card--danger");
        card.RemoveFromClassList("edm-status-card--success");
        if (!string.IsNullOrEmpty(stateClass))
            card.AddToClassList(stateClass);
    }

    private static void SetSingleBadgeClass(Label lbl, string activeClass, params string[] allClasses)
    {
        foreach (var cls in allClasses)
            lbl.RemoveFromClassList(cls);
        if (!string.IsNullOrEmpty(activeClass))
            lbl.AddToClassList(activeClass);
    }
}
