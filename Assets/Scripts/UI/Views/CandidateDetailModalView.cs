using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// UXML-backed candidate details modal.
/// Initialize loads the CandidateDetailModal.uxml template into the provided root,
/// queries all named elements, and wires tab switching + action button handlers.
/// Bind performs data-only updates from the ViewModel with no structural changes.
/// Adapts the shared profile shell from EmployeeDetailModalView with confidence overlays.
/// </summary>
public class CandidateDetailModalView : IGameView
{
    private ICommandDispatcher _dispatcher;
    private IModalPresenter    _modal;

    private VisualTreeAsset _asset;
    private CandidateDetailModalViewModel _vm;
    private VisualElement _root;

    // ── Header ───────────────────────────────────────────────────────────────

    private Label         _nameLabel;
    private Label         _ageLabel;
    private Label         _rolePill;
    private Label         _sourceBadge;
    private Label         _pipelineStateLabel;
    private Label         _salaryEstimateLabel;
    private Label         _salaryConfidenceLabel;
    private Label         _caEstimateLabel;
    private Label         _paEstimateLabel;
    private Label         _overallConfidenceLabel;
    private Label         _expiryLabel;
    private VisualElement _badgesContainer;

    // ── Tabs ─────────────────────────────────────────────────────────────────

    private readonly List<Button>        _tabButtons  = new List<Button>(5);
    private readonly List<VisualElement> _tabContents = new List<VisualElement>(5);

    // ── Overview panels ──────────────────────────────────────────────────────

    private VisualElement _roleFitRows;
    private VisualElement _teamSelector;
    private Label         _teamProjectionFit;
    private Label         _teamProjectionDetail;
    private VisualElement _coreSkillsRows;
    private VisualElement _supportingSkillsRows;
    private VisualElement _attributesRows;
    private Label         _reportSummaryLabel;
    private VisualElement _reportStrengths;
    private VisualElement _reportConcerns;
    private Label         _reportConfidenceLabel;
    private Label         _recommendationLabel;

    // ── Interview panel ──────────────────────────────────────────────────────

    private Label         _interviewStageLabel;
    private Label         _interviewHRTeamLabel;
    private Label         _interviewTimeLabel;
    private Label         _interviewKnowledgeLabel;
    private Label         _firstReportLabel;
    private Label         _finalReportLabel;
    private VisualElement _revealedStrengths;
    private VisualElement _revealedConcerns;

    // ── Personality panel ────────────────────────────────────────────────────

    private Label         _personalityTypeLabel;
    private Label         _personalityConfidenceLabel;
    private VisualElement _signalRows;
    private VisualElement _riskFlagRows;
    private Label         _retentionRiskLabel;
    private Label         _salaryPressureLabel;

    // ── Comparison panel ─────────────────────────────────────────────
    private VisualElement _comparisonTargetSelector;
    private Label         _comparisonTargetColHeader;
    private VisualElement _comparisonCandidateMetrics;
    private VisualElement _comparisonLabelMetrics;
    private VisualElement _comparisonTargetMetrics;
    private VisualElement _comparisonDeltaMetrics;
    private Label         _comparisonRecommendationText;
    private DropdownField _comparisonDropdown;
    private DropdownField _teamDropdown;

    // ── Offer panel ──────────────────────────────────────────────────────────

    private Label         _salaryDemandStateLabel;
    private Label         _salaryEstimateDisplay;
    private Label         _monthlyCostLabel;
    private Label         _runwayImpactLabel;
    private Label         _acceptanceChanceLabel;
    private VisualElement _acceptanceFactorsList;
    private Label         _offerStatusLabel;
    private VisualElement _offerStatusSection;
    private Button        _btnMakeOffer;
    private Button        _btnHire;
    private Button        _btnWithdrawOffer;
    private Button        _btnAcceptCounter;
    private Button        _btnRejectCounter;

    // ── Bottom status cards ──────────────────────────────────────────────────

    private Label         _statusInterestValue;
    private Label         _statusSalaryValue;
    private Label         _statusPotentialValue;
    private Label         _statusRiskValue;
    private Label         _statusAvailabilityValue;
    private Label         _statusInterviewValue;
    private Label         _statusTeamImpactValue;
    private Label         _statusComparisonValue;

    private VisualElement _statusCardInterest;
    private VisualElement _statusCardSalary;
    private VisualElement _statusCardPotential;
    private VisualElement _statusCardRisk;
    private VisualElement _statusCardAvailability;
    private VisualElement _statusCardInterview;
    private VisualElement _statusCardTeamImpact;
    private VisualElement _statusCardComparison;

    // ── Footer action buttons ────────────────────────────────────────────────

    private Button _btnClose;
    private Button _btnCloseFooter;
    private Button _btnInterview;
    private Button _btnShortlist;
    private Button _btnOffer;
    private Button _btnReject;
    private Button _btnHireFooter;
    private Button _btnWithdrawFooter;
    private Button _btnAcceptHR;
    private Button _btnDeclineHR;

    private VisualElement _footerActions;

    // ── Constructor ───────────────────────────────────────────────────────────

    public CandidateDetailModalView() { }

    /// <summary>Called by WindowManager before Initialize to inject the UXML template asset.</summary>
    public void SetAsset(VisualTreeAsset asset)
    {
        _asset = asset;
    }

    // ── IGameView ─────────────────────────────────────────────────────────────

    public void Initialize(VisualElement root, UIServices services)
    {
        _dispatcher = services?.Commands;
        _modal      = services?.Modals;
        _root       = root;

        if (_asset != null)
        {
            _asset.CloneTree(root);
        }
        else
        {
            root.AddToClassList("cdm");
            Debug.LogWarning("[CandidateDetailModalView] No VisualTreeAsset assigned. " +
                             "Assign candidateDetailAsset in WindowManager inspector.");
        }

        // ── Query header ──────────────────────────────────────────────────────
        _nameLabel              = root.Q<Label>("name-label");
        _ageLabel               = root.Q<Label>("age-label");
        _rolePill               = root.Q<Label>("role-pill");
        _sourceBadge            = root.Q<Label>("source-badge");
        _pipelineStateLabel     = root.Q<Label>("pipeline-state-label");
        _salaryEstimateLabel    = root.Q<Label>("salary-estimate-label");
        _salaryConfidenceLabel  = root.Q<Label>("salary-confidence-label");
        _caEstimateLabel        = root.Q<Label>("ca-estimate-label");
        _paEstimateLabel        = root.Q<Label>("pa-estimate-label");
        _overallConfidenceLabel = root.Q<Label>("overall-confidence-label");
        _expiryLabel            = root.Q<Label>("expiry-label");
        _badgesContainer        = root.Q<VisualElement>("badges-container");

        // ── Query tabs ────────────────────────────────────────────────────────
        _tabButtons.Clear();
        _tabContents.Clear();

        _tabButtons.Add(root.Q<Button>("tab-overview"));
        _tabButtons.Add(root.Q<Button>("tab-interview"));
        _tabButtons.Add(root.Q<Button>("tab-personality"));
        _tabButtons.Add(root.Q<Button>("tab-comparison"));
        _tabButtons.Add(root.Q<Button>("tab-offer"));

        _tabContents.Add(root.Q<VisualElement>("overview-content"));
        _tabContents.Add(root.Q<VisualElement>("interview-content"));
        _tabContents.Add(root.Q<VisualElement>("personality-content"));
        _tabContents.Add(root.Q<VisualElement>("comparison-content"));
        _tabContents.Add(root.Q<VisualElement>("offer-content"));

        if (_tabButtons[0] != null) _tabButtons[0].clicked += OnTab0Clicked;
        if (_tabButtons[1] != null) _tabButtons[1].clicked += OnTab1Clicked;
        if (_tabButtons[2] != null) _tabButtons[2].clicked += OnTab2Clicked;
        if (_tabButtons[3] != null) _tabButtons[3].clicked += OnTab3Clicked;
        if (_tabButtons[4] != null) _tabButtons[4].clicked += OnTab4Clicked;

        ShowTab(0);

        // ── Query overview panels ─────────────────────────────────────────────
        _roleFitRows          = root.Q<VisualElement>("role-fit-rows");
        _teamSelector         = root.Q<VisualElement>("team-selector");
        _teamProjectionFit    = root.Q<Label>("team-projection-fit");
        _teamProjectionDetail = root.Q<Label>("team-projection-detail");
        _coreSkillsRows       = root.Q<VisualElement>("core-skills-rows");
        _supportingSkillsRows = root.Q<VisualElement>("supporting-skills-rows");
        _attributesRows       = root.Q<VisualElement>("attributes-rows");
        _reportSummaryLabel   = root.Q<Label>("report-summary-label");
        _reportStrengths      = root.Q<VisualElement>("report-strengths");
        _reportConcerns       = root.Q<VisualElement>("report-concerns");
        _reportConfidenceLabel = root.Q<Label>("report-confidence-label");
        _recommendationLabel  = root.Q<Label>("recommendation-label");

        // Create and wire team dropdown once
        if (_teamSelector != null)
        {
            _teamDropdown = new DropdownField("Team", new List<string> { "Select a team..." }, 0);
            _teamDropdown.RegisterValueChangedCallback(OnTeamDropdownChanged);
            _teamSelector.Add(_teamDropdown);
        }

        // ── Query interview panel ─────────────────────────────────────────────
        _interviewStageLabel     = root.Q<Label>("interview-stage-label");
        _interviewHRTeamLabel    = root.Q<Label>("interview-hr-team-label");
        _interviewTimeLabel      = root.Q<Label>("interview-time-label");
        _interviewKnowledgeLabel = root.Q<Label>("interview-knowledge-label");
        _firstReportLabel        = root.Q<Label>("first-report-label");
        _finalReportLabel        = root.Q<Label>("final-report-label");
        _revealedStrengths       = root.Q<VisualElement>("revealed-strengths");
        _revealedConcerns        = root.Q<VisualElement>("revealed-concerns");

        // ── Query personality panel ───────────────────────────────────────────
        _personalityTypeLabel       = root.Q<Label>("personality-type-label");
        _personalityConfidenceLabel = root.Q<Label>("personality-confidence-label");
        _signalRows                 = root.Q<VisualElement>("signal-rows");
        _riskFlagRows               = root.Q<VisualElement>("risk-flag-rows");
        _retentionRiskLabel         = root.Q<Label>("retention-risk-label");
        _salaryPressureLabel        = root.Q<Label>("salary-pressure-label");

        // ── Query comparison panel ────────────────────────────────────────────
        _comparisonTargetSelector   = root.Q<VisualElement>("comparison-target-selector");
        _comparisonTargetColHeader  = root.Q<Label>("comparison-target-col-header");
        _comparisonCandidateMetrics = root.Q<VisualElement>("comparison-candidate-metrics");
        _comparisonLabelMetrics     = root.Q<VisualElement>("comparison-metric-labels");
        _comparisonTargetMetrics    = root.Q<VisualElement>("comparison-target-metrics");
        _comparisonDeltaMetrics     = root.Q<VisualElement>("comparison-delta-metrics");
        _comparisonRecommendationText = root.Q<Label>("comparison-recommendation-text");

        // Create and wire comparison dropdown once
        if (_comparisonTargetSelector != null)
        {
            _comparisonDropdown = new DropdownField(new List<string> { "Same Role Employees", "Team Average", "Market Average" }, 0);
            _comparisonDropdown.RegisterValueChangedCallback(OnComparisonDropdownChanged);
            _comparisonTargetSelector.Add(_comparisonDropdown);
        }

        // ── Query offer panel ─────────────────────────────────────────────────
        _salaryDemandStateLabel  = root.Q<Label>("salary-demand-state-label");
        _salaryEstimateDisplay   = root.Q<Label>("salary-estimate-display");
        _monthlyCostLabel        = root.Q<Label>("monthly-cost-label");
        _runwayImpactLabel       = root.Q<Label>("runway-impact-label");
        _acceptanceChanceLabel   = root.Q<Label>("acceptance-chance-label");
        _acceptanceFactorsList   = root.Q<VisualElement>("acceptance-factors-list");
        _offerStatusLabel        = root.Q<Label>("offer-status-label");
        _offerStatusSection      = root.Q<VisualElement>("offer-status-section");
        _btnMakeOffer            = root.Q<Button>("btn-make-offer");
        _btnHire                 = root.Q<Button>("btn-hire");
        _btnWithdrawOffer        = root.Q<Button>("btn-withdraw-offer");
        _btnAcceptCounter        = root.Q<Button>("btn-accept-counter");
        _btnRejectCounter        = root.Q<Button>("btn-reject-counter");

        // ── Query bottom status cards ──────────────────────────────────────────
        _statusCardInterest     = root.Q<VisualElement>("status-card-interest");
        _statusCardSalary       = root.Q<VisualElement>("status-card-salary");
        _statusCardPotential    = root.Q<VisualElement>("status-card-potential");
        _statusCardRisk         = root.Q<VisualElement>("status-card-risk");
        _statusCardAvailability = root.Q<VisualElement>("status-card-availability");
        _statusCardInterview    = root.Q<VisualElement>("status-card-interview");
        _statusCardTeamImpact   = root.Q<VisualElement>("status-card-team-impact");
        _statusCardComparison   = root.Q<VisualElement>("status-card-comparison");

        _statusInterestValue     = root.Q<Label>("status-interest-value");
        _statusSalaryValue       = root.Q<Label>("status-salary-value");
        _statusPotentialValue    = root.Q<Label>("status-potential-value");
        _statusRiskValue         = root.Q<Label>("status-risk-value");
        _statusAvailabilityValue = root.Q<Label>("status-availability-value");
        _statusInterviewValue    = root.Q<Label>("status-interview-value");
        _statusTeamImpactValue   = root.Q<Label>("status-team-impact-value");
        _statusComparisonValue   = root.Q<Label>("status-comparison-value");

        // ── Query footer ──────────────────────────────────────────────────────
        _footerActions    = root.Q<VisualElement>("footer-actions");
        _btnClose         = root.Q<Button>("btn-close");
        _btnCloseFooter   = root.Q<Button>("btn-close-footer");
        _btnInterview     = root.Q<Button>("btn-interview");
        _btnShortlist     = root.Q<Button>("btn-shortlist");
        _btnOffer         = root.Q<Button>("btn-offer");
        _btnReject        = root.Q<Button>("btn-reject");
        _btnHireFooter    = root.Q<Button>("btn-hire-footer");
        _btnWithdrawFooter = root.Q<Button>("btn-withdraw-footer");
        _btnAcceptHR      = root.Q<Button>("btn-accept-hr");
        _btnDeclineHR     = root.Q<Button>("btn-decline-hr");

        // ── Wire action buttons ───────────────────────────────────────────────
        if (_btnClose           != null) _btnClose.clicked           += OnCloseClicked;
        if (_btnCloseFooter     != null) _btnCloseFooter.clicked     += OnCloseClicked;
        if (_btnInterview       != null) _btnInterview.clicked       += OnInterviewClicked;
        if (_btnShortlist       != null) _btnShortlist.clicked       += OnShortlistClicked;
        if (_btnOffer           != null) _btnOffer.clicked           += OnOfferFooterClicked;
        if (_btnReject          != null) _btnReject.clicked          += OnRejectClicked;
        if (_btnHireFooter      != null) _btnHireFooter.clicked      += OnHireClicked;
        if (_btnWithdrawFooter  != null) _btnWithdrawFooter.clicked  += OnWithdrawOfferClicked;
        if (_btnAcceptHR        != null) _btnAcceptHR.clicked        += OnAcceptHRClicked;
        if (_btnDeclineHR       != null) _btnDeclineHR.clicked       += OnDeclineHRClicked;

        // ── Wire offer panel buttons ──────────────────────────────────────────
        if (_btnMakeOffer     != null) _btnMakeOffer.clicked     += OnOfferClicked;
        if (_btnHire          != null) _btnHire.clicked          += OnHireClicked;
        if (_btnWithdrawOffer != null) _btnWithdrawOffer.clicked += OnWithdrawOfferClicked;
        if (_btnAcceptCounter != null) _btnAcceptCounter.clicked += OnAcceptCounterClicked;
        if (_btnRejectCounter != null) _btnRejectCounter.clicked += OnRejectCounterClicked;
    }

    public void Bind(IViewModel viewModel)
    {
        _vm = viewModel as CandidateDetailModalViewModel;
        if (_vm == null) return;

        BindHeader();
        BindBadges();
        BindOverviewTab();
        BindInterviewTab();
        BindPersonalityTab();
        BindComparisonTab();
        BindOfferTab();
        BindBottomStatusCards();
        BindActionVisibility();
    }

    public void Dispose()
    {
        if (_tabButtons.Count > 0 && _tabButtons[0] != null) _tabButtons[0].clicked -= OnTab0Clicked;
        if (_tabButtons.Count > 1 && _tabButtons[1] != null) _tabButtons[1].clicked -= OnTab1Clicked;
        if (_tabButtons.Count > 2 && _tabButtons[2] != null) _tabButtons[2].clicked -= OnTab2Clicked;
        if (_tabButtons.Count > 3 && _tabButtons[3] != null) _tabButtons[3].clicked -= OnTab3Clicked;
        if (_tabButtons.Count > 4 && _tabButtons[4] != null) _tabButtons[4].clicked -= OnTab4Clicked;

        _tabButtons.Clear();
        _tabContents.Clear();

        if (_btnClose           != null) _btnClose.clicked           -= OnCloseClicked;
        if (_btnCloseFooter     != null) _btnCloseFooter.clicked     -= OnCloseClicked;
        if (_btnInterview       != null) _btnInterview.clicked       -= OnInterviewClicked;
        if (_btnShortlist       != null) _btnShortlist.clicked       -= OnShortlistClicked;
        if (_btnOffer           != null) _btnOffer.clicked           -= OnOfferFooterClicked;
        if (_btnReject          != null) _btnReject.clicked          -= OnRejectClicked;
        if (_btnHireFooter      != null) _btnHireFooter.clicked      -= OnHireClicked;
        if (_btnWithdrawFooter  != null) _btnWithdrawFooter.clicked  -= OnWithdrawOfferClicked;
        if (_btnAcceptHR        != null) _btnAcceptHR.clicked        -= OnAcceptHRClicked;
        if (_btnDeclineHR       != null) _btnDeclineHR.clicked       -= OnDeclineHRClicked;

        if (_btnMakeOffer     != null) _btnMakeOffer.clicked     -= OnOfferClicked;
        if (_btnHire          != null) _btnHire.clicked          -= OnHireClicked;
        if (_btnWithdrawOffer != null) _btnWithdrawOffer.clicked -= OnWithdrawOfferClicked;
        if (_btnAcceptCounter != null) _btnAcceptCounter.clicked -= OnAcceptCounterClicked;
        if (_btnRejectCounter != null) _btnRejectCounter.clicked -= OnRejectCounterClicked;

        if (_teamDropdown       != null) _teamDropdown.UnregisterValueChangedCallback(OnTeamDropdownChanged);
        if (_comparisonDropdown != null) _comparisonDropdown.UnregisterValueChangedCallback(OnComparisonDropdownChanged);
        _teamDropdown       = null;
        _comparisonDropdown = null;

        _vm   = null;
        _root = null;
    }

    /// <summary>Legacy compat — called by WindowManager when opening with counter-offer visible.</summary>
    public void ShowCounterOfferView()
    {
        ShowTab(4);
    }

    // ── Tab switching ─────────────────────────────────────────────────────────

    private void OnTab0Clicked() { ShowTab(0); }
    private void OnTab1Clicked() { ShowTab(1); }
    private void OnTab2Clicked() { ShowTab(2); }
    private void OnTab3Clicked() { ShowTab(3); }
    private void OnTab4Clicked() { ShowTab(4); }

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
                content.EnableInClassList("cdm-tab-content--hidden", i != index);
        }
    }

    // ── Bind helpers ──────────────────────────────────────────────────────────

    private void BindHeader()
    {
        if (_nameLabel           != null) _nameLabel.text           = _vm.Name;
        if (_ageLabel            != null) _ageLabel.text            = _vm.Age;
        if (_salaryEstimateLabel != null) _salaryEstimateLabel.text = _vm.SalaryEstimateText;
        if (_caEstimateLabel     != null) _caEstimateLabel.text     = _vm.CAEstimateText;
        if (_paEstimateLabel     != null) _paEstimateLabel.text     = _vm.PAEstimateText;

        if (_rolePill != null)
        {
            _rolePill.text = _vm.RoleName;
            UIFormatting.ClearRolePillClasses(_rolePill);
            _rolePill.AddToClassList(_vm.RolePillClass);
        }

        if (_sourceBadge != null)
        {
            _sourceBadge.text = _vm.CandidateSource;
            SetSingleBadgeClass(_sourceBadge, _vm.SourceBadgeClass,
                "pipeline--hr-sourced", "pipeline--shortlisted", "pipeline--interviewing",
                "pipeline--final-report", "pipeline--offer-pending", "pipeline--market");
        }

        if (_pipelineStateLabel != null)
        {
            _pipelineStateLabel.text = _vm.PipelineState;
            SetSingleBadgeClass(_pipelineStateLabel, _vm.PipelineStateClass,
                "pipeline--hr-sourced", "pipeline--shortlisted", "pipeline--interviewing",
                "pipeline--final-report", "pipeline--offer-pending", "pipeline--market");
        }

        if (_salaryConfidenceLabel != null)
            _salaryConfidenceLabel.text = _vm.SalaryConfidenceText;

        if (_overallConfidenceLabel != null)
        {
            _overallConfidenceLabel.text = _vm.OverallConfidenceText;
            SetSingleBadgeClass(_overallConfidenceLabel, _vm.OverallConfidenceClass,
                "confidence--unknown", "confidence--low", "confidence--medium",
                "confidence--high", "confidence--confirmed");
        }

        if (_expiryLabel != null)
        {
            _expiryLabel.text = _vm.ExpiryText;
            SetSingleBadgeClass(_expiryLabel, _vm.ExpiryClass,
                "expiry--urgent", "expiry--warning", "expiry--normal");
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
            var data = badges[i];
            var chip = new Label(data.Label);
            chip.AddToClassList("badge");
            chip.AddToClassList("cdm-badge-chip");
            if (!string.IsNullOrEmpty(data.UssClass))
                chip.AddToClassList(data.UssClass);
            _badgesContainer.Add(chip);
        }
    }

    private void BindOverviewTab()
    {
        BindRoleFitRows();
        BindEstimatedSkills();
        BindEstimatedAttributes();
        BindReport();
        BindTeamProjection();
    }

    private void BindRoleFitRows()
    {
        if (_roleFitRows == null) return;
        _roleFitRows.Clear();

        var fits = _vm.TopProjectedFits;
        if (fits == null) return;

        int count = fits.Length;
        for (int i = 0; i < count; i++)
        {
            var entry = fits[i];
            var row = new VisualElement();
            row.AddToClassList("cdm-role-fit-row");

            var dot = new VisualElement();
            dot.AddToClassList("role-suit-dot");
            if (!string.IsNullOrEmpty(entry.SuitabilityClass))
                dot.AddToClassList(entry.SuitabilityClass);
            row.Add(dot);

            var nameLabel = new Label(entry.RoleName);
            nameLabel.AddToClassList("cdm-role-fit-row__name");
            row.Add(nameLabel);

            var rangeLabel = new Label(entry.FitRangeText);
            rangeLabel.AddToClassList("cdm-role-fit-row__range");
            if (!string.IsNullOrEmpty(entry.ConfidenceClass))
                rangeLabel.AddToClassList(entry.ConfidenceClass);
            row.Add(rangeLabel);

            var confDot = new VisualElement();
            confDot.AddToClassList("cdm-stat-row__confidence");
            confDot.AddToClassList(entry.ConfidenceDotClass);
            row.Add(confDot);

            _roleFitRows.Add(row);
        }
    }

    private void BindEstimatedSkills()
    {
        BindSkillRows(_coreSkillsRows, _vm.CoreSkills);
        BindSkillRows(_supportingSkillsRows, _vm.SupportingSkills);
    }

    private void BindSkillRows(VisualElement container, CandidateDetailModalViewModel.EstimatedSkillEntry[] skills)
    {
        if (container == null) return;
        container.Clear();

        if (skills == null) return;

        int count = skills.Length;
        for (int i = 0; i < count; i++)
        {
            var entry = skills[i];
            var row = new VisualElement();
            row.AddToClassList("cdm-stat-row");

            var nameLabel = new Label(entry.Name);
            nameLabel.AddToClassList("cdm-stat-row__name");
            nameLabel.style.color = entry.NameColor;
            row.Add(nameLabel);

            var rangeLabel = new Label(entry.DisplayText);
            rangeLabel.AddToClassList("cdm-stat-row__range");
            if (!string.IsNullOrEmpty(entry.DisplayClass))
                rangeLabel.AddToClassList(entry.DisplayClass);
            row.Add(rangeLabel);

            var confDot = new VisualElement();
            confDot.AddToClassList("cdm-stat-row__confidence");
            confDot.AddToClassList(entry.ConfidenceDotClass);
            row.Add(confDot);

            container.Add(row);
        }
    }

    private void BindEstimatedAttributes()
    {
        if (_attributesRows == null) return;
        _attributesRows.Clear();

        var attrs = _vm.EstimatedAttributes;
        if (attrs == null) return;

        int count = attrs.Length;
        for (int i = 0; i < count; i++)
        {
            var entry = attrs[i];
            var row = new VisualElement();
            row.AddToClassList("cdm-stat-row");

            var nameLabel = new Label(entry.Name);
            nameLabel.AddToClassList("cdm-stat-row__name");
            row.Add(nameLabel);

            var valueLabel = new Label(entry.DisplayText);
            valueLabel.AddToClassList("cdm-stat-row__range");
            if (!string.IsNullOrEmpty(entry.DisplayClass))
                valueLabel.AddToClassList(entry.DisplayClass);
            row.Add(valueLabel);

            var confDot = new VisualElement();
            confDot.AddToClassList("cdm-stat-row__confidence");
            confDot.AddToClassList(entry.ConfidenceDotClass);
            row.Add(confDot);

            _attributesRows.Add(row);
        }
    }

    private void BindReport()
    {
        var report = _vm.Report;

        if (_reportSummaryLabel != null)
            _reportSummaryLabel.text = report.SummaryLabel;

        BindStringList(_reportStrengths, report.Strengths, "cdm-report-item", "cdm-report-item--strength");
        BindStringList(_reportConcerns, report.Concerns, "cdm-report-item", "cdm-report-item--concern");

        if (_reportConfidenceLabel != null)
            _reportConfidenceLabel.text = report.ReportConfidence;

        if (_recommendationLabel != null)
            _recommendationLabel.text = report.Recommendation;
    }

    private void BindTeamProjection()
    {
        var proj = _vm.TeamProjection;
        if (_teamProjectionFit    != null) _teamProjectionFit.text    = proj.ProjectedFitText;
        if (_teamProjectionDetail != null) _teamProjectionDetail.text = proj.ProjectionDetailText;

        if (_teamDropdown != null)
        {
            var teamNames = _vm.AvailableTeamNames;
            _suppressDropdownEvents = true;
            if (teamNames != null && teamNames.Length > 0)
            {
                var choices = new List<string>(teamNames);
                _teamDropdown.choices = choices;
                if (_teamDropdown.index < 0 || _teamDropdown.index >= choices.Count)
                    _teamDropdown.index = 0;
            }
            else
            {
                _teamDropdown.choices = new List<string> { "No teams available" };
                _teamDropdown.index   = 0;
            }
            _suppressDropdownEvents = false;
        }
    }

    private void OnTeamDropdownChanged(ChangeEvent<string> evt)
    {
        if (_vm == null || _teamDropdown == null || _suppressDropdownEvents) return;
        int idx = _teamDropdown.index;
        if (idx >= 0) _vm.SetTargetTeamIndex(idx);
    }

    private void BindInterviewTab()
    {
        var data = _vm.Interview;

        if (_interviewStageLabel     != null) _interviewStageLabel.text     = data.StageText;
        if (_interviewHRTeamLabel    != null) _interviewHRTeamLabel.text    = data.AssignedHRTeam;
        if (_interviewTimeLabel      != null) _interviewTimeLabel.text      = data.TimeRemaining;
        if (_interviewKnowledgeLabel != null) _interviewKnowledgeLabel.text = data.KnowledgeText;
        if (_firstReportLabel        != null) _firstReportLabel.text        = data.FirstReportText;
        if (_finalReportLabel        != null) _finalReportLabel.text        = data.FinalReportText;

        BindStringList(_revealedStrengths, data.RevealedStrengths, "cdm-report-item", "cdm-report-item--strength");
        BindStringList(_revealedConcerns, data.RevealedConcerns, "cdm-report-item", "cdm-report-item--concern");
    }

    private void BindPersonalityTab()
    {
        var data = _vm.Personality;

        if (_personalityTypeLabel       != null) _personalityTypeLabel.text       = data.PersonalityEstimate;
        if (_personalityConfidenceLabel != null) _personalityConfidenceLabel.text = data.PersonalityConfidence;
        if (_retentionRiskLabel         != null) _retentionRiskLabel.text         = data.RetentionRisk;
        if (_salaryPressureLabel        != null) _salaryPressureLabel.text        = data.SalaryPressure;

        BindStringList(_riskFlagRows, data.RiskFlags, "cdm-report-item", "cdm-report-item--concern");

        if (_signalRows != null)
        {
            _signalRows.Clear();
            var signals = data.Signals;
            if (signals != null)
            {
                int count = signals.Length;
                for (int i = 0; i < count; i++)
                {
                    var sig = signals[i];
                    var row = new VisualElement();
                    row.AddToClassList("cdm-stat-row");

                    var nameLabel = new Label(sig.Name);
                    nameLabel.AddToClassList("cdm-stat-row__name");
                    row.Add(nameLabel);

                    var valueLabel = new Label(sig.DisplayText);
                    valueLabel.AddToClassList("cdm-stat-row__range");
                    if (!string.IsNullOrEmpty(sig.DisplayClass))
                        valueLabel.AddToClassList(sig.DisplayClass);
                    row.Add(valueLabel);

                    var confDot = new VisualElement();
                    confDot.AddToClassList("cdm-stat-row__confidence");
                    confDot.AddToClassList(sig.ConfidenceDotClass);
                    row.Add(confDot);

                    _signalRows.Add(row);
                }
            }
        }
    }

    private void BindComparisonTab()
    {
        var data = _vm.Comparison;

        // Update comparison dropdown choices/selection
        if (_comparisonDropdown != null && data.TargetLabels != null && data.TargetLabels.Length > 0)
        {
            var choices = new List<string>(data.TargetLabels);
            _suppressDropdownEvents = true;
            _comparisonDropdown.choices = choices;
            int idx = data.SelectedTargetIndex;
            if (idx >= 0 && idx < choices.Count)
                _comparisonDropdown.index = idx;
            _suppressDropdownEvents = false;
        }

        if (_comparisonTargetColHeader != null)
            _comparisonTargetColHeader.text = data.TargetColumnHeader ?? "COMPARISON";

        if (_comparisonRecommendationText != null)
            _comparisonRecommendationText.text = data.RecommendationText ?? "\u2014";

        // Populate metric columns
        if (_comparisonCandidateMetrics == null) return;
        _comparisonCandidateMetrics.Clear();
        _comparisonLabelMetrics?.Clear();
        _comparisonTargetMetrics?.Clear();
        _comparisonDeltaMetrics?.Clear();

        var metrics = data.Metrics;
        if (metrics == null) return;

        int count = metrics.Length;
        for (int i = 0; i < count; i++)
        {
            var m = metrics[i];

            var candidateCell = new Label(m.CandidateValueText ?? "\u2014");
            candidateCell.AddToClassList("cdm-comparison-metric-cell");
            if (!string.IsNullOrEmpty(m.ConfidenceClass))
                candidateCell.AddToClassList(m.ConfidenceClass);
            _comparisonCandidateMetrics.Add(candidateCell);

            if (_comparisonLabelMetrics != null)
            {
                var labelCell = new Label(m.MetricName);
                labelCell.AddToClassList("cdm-comparison-label-cell");
                _comparisonLabelMetrics.Add(labelCell);
            }

            if (_comparisonTargetMetrics != null)
            {
                var targetCell = new Label(m.ComparisonValueText ?? "\u2014");
                targetCell.AddToClassList("cdm-comparison-metric-cell");
                _comparisonTargetMetrics.Add(targetCell);
            }

            if (_comparisonDeltaMetrics != null)
            {
                var deltaCell = new Label(m.DeltaText ?? "\u2014");
                deltaCell.AddToClassList("cdm-comparison-metric-cell");
                if (!string.IsNullOrEmpty(m.DeltaClass))
                    deltaCell.AddToClassList(m.DeltaClass);
                _comparisonDeltaMetrics.Add(deltaCell);
            }
        }
    }

    private bool _suppressDropdownEvents;

    private void OnComparisonDropdownChanged(ChangeEvent<string> evt)
    {
        if (_vm == null || _comparisonDropdown == null || _suppressDropdownEvents) return;
        _vm.SetComparisonTarget(_comparisonDropdown.index);
        BindComparisonTab();
    }

    private void BindOfferTab()
    {
        var data = _vm.Offer;

        if (_salaryDemandStateLabel != null)
            _salaryDemandStateLabel.text = data.DemandStateText ?? "\u2014";

        if (_salaryEstimateDisplay != null)
            _salaryEstimateDisplay.text = data.SalaryEstimateText ?? "\u2014";

        if (_monthlyCostLabel != null)
            _monthlyCostLabel.text = data.MonthlyCostText ?? "\u2014";

        if (_runwayImpactLabel != null)
        {
            _runwayImpactLabel.text = data.RunwayImpactText ?? "\u2014";
            SetSingleBadgeClass(_runwayImpactLabel, data.RunwayImpactClass,
                "runway-impact--warning", "runway-impact--danger");
        }

        if (_acceptanceChanceLabel != null)
        {
            _acceptanceChanceLabel.text = data.AcceptanceChanceLabel ?? "\u2014";
            SetSingleBadgeClass(_acceptanceChanceLabel, data.AcceptanceChanceClass,
                "offer-acceptance--very-likely", "offer-acceptance--likely", "offer-acceptance--medium",
                "offer-acceptance--unlikely", "offer-acceptance--very-unlikely");
        }

        // Acceptance factors
        if (_acceptanceFactorsList != null)
        {
            _acceptanceFactorsList.Clear();
            var factors = data.Factors;
            if (factors != null)
            {
                int fc = factors.Length;
                for (int i = 0; i < fc; i++)
                {
                    var f = factors[i];
                    var row = new VisualElement();
                    row.AddToClassList("cdm-offer__factor-row");

                    var nameLabel = new Label(f.Name);
                    nameLabel.AddToClassList("cdm-offer__factor-name");
                    row.Add(nameLabel);

                    var valueLabel = new Label(f.ValueText ?? "\u2014");
                    valueLabel.AddToClassList("cdm-offer__factor-value");
                    if (!string.IsNullOrEmpty(f.ImpactClass))
                        valueLabel.AddToClassList(f.ImpactClass);
                    row.Add(valueLabel);

                    _acceptanceFactorsList.Add(row);
                }
            }
        }

        // Status
        if (_offerStatusLabel != null)
        {
            _offerStatusLabel.text = data.StatusText ?? "";
            SetSingleBadgeClass(_offerStatusLabel, data.StatusClass,
                "cdm-offer__status-label--warning", "cdm-offer__status-label--danger",
                "cdm-offer__status-label--success", "cdm-offer__status-label");
            if (!string.IsNullOrEmpty(data.StatusClass))
                _offerStatusLabel.AddToClassList(data.StatusClass);
        }

        if (_offerStatusSection != null)
            _offerStatusSection.style.display = string.IsNullOrEmpty(data.StatusText)
                ? DisplayStyle.None : DisplayStyle.Flex;

        // Offer action buttons in offer tab
        if (_btnMakeOffer     != null) { _btnMakeOffer.SetEnabled(data.CanMakeOffer);     _btnMakeOffer.style.display     = data.CanMakeOffer     ? DisplayStyle.Flex : DisplayStyle.None; }
        if (_btnHire          != null) { _btnHire.SetEnabled(data.CanHire);               _btnHire.style.display          = data.CanHire          ? DisplayStyle.Flex : DisplayStyle.None; }
        if (_btnWithdrawOffer != null) { _btnWithdrawOffer.SetEnabled(data.CanWithdrawOffer); _btnWithdrawOffer.style.display = data.CanWithdrawOffer ? DisplayStyle.Flex : DisplayStyle.None; }
        if (_btnAcceptCounter != null) { _btnAcceptCounter.SetEnabled(data.CanAcceptCounter); _btnAcceptCounter.style.display = data.CanAcceptCounter ? DisplayStyle.Flex : DisplayStyle.None; }
        if (_btnRejectCounter != null) { _btnRejectCounter.SetEnabled(data.CanRejectCounter); _btnRejectCounter.style.display = data.CanRejectCounter ? DisplayStyle.Flex : DisplayStyle.None; }
    }

    private void BindBottomStatusCards()
    {
        SetStatusCard(_statusCardInterest,     _statusInterestValue,     _vm.InterestCardText,     _vm.InterestCardClass);
        SetStatusCard(_statusCardSalary,       _statusSalaryValue,       _vm.SalaryCardText,       _vm.SalaryCardClass);
        SetStatusCard(_statusCardPotential,    _statusPotentialValue,    _vm.PotentialCardText,    _vm.PotentialCardClass);
        SetStatusCard(_statusCardRisk,         _statusRiskValue,         _vm.RiskCardText,         _vm.RiskCardClass);
        SetStatusCard(_statusCardAvailability, _statusAvailabilityValue, _vm.AvailabilityCardText, _vm.AvailabilityCardClass);
        SetStatusCard(_statusCardInterview,    _statusInterviewValue,    _vm.InterviewCardText,    _vm.InterviewCardClass);
        SetStatusCard(_statusCardTeamImpact,   _statusTeamImpactValue,   _vm.TeamImpactCardText,   _vm.TeamImpactCardClass);
        SetStatusCard(_statusCardComparison,   _statusComparisonValue,   _vm.ComparisonCardText,   _vm.ComparisonCardClass);
    }

    private void BindActionVisibility()
    {
        var offer = _vm.Offer;
        bool hasNeg      = _vm.HasActiveNegotiation;
        bool isShortlisted = _vm.IsShortlisted;
        bool interviewEnabled = _vm.InterviewButtonEnabled;
        bool isPending = _vm.CandidateSource == "HR Sourced";

        // Footer action visibility based on pipeline state
        SetButtonVisible(_btnInterview,      interviewEnabled || !hasNeg);
        SetButtonVisible(_btnShortlist,      !isShortlisted && !hasNeg);
        SetButtonVisible(_btnOffer,          !hasNeg && !offer.CanHire);
        SetButtonVisible(_btnReject,         !hasNeg && !offer.CanHire);
        SetButtonVisible(_btnHireFooter,     offer.CanHire);
        SetButtonVisible(_btnWithdrawFooter, offer.CanWithdrawOffer);
        SetButtonVisible(_btnAcceptHR,       isPending && !hasNeg);
        SetButtonVisible(_btnDeclineHR,      isPending && !hasNeg);

        if (_btnInterview != null)
        {
            _btnInterview.text = _vm.InterviewButtonText;
            _btnInterview.SetEnabled(_vm.InterviewButtonEnabled);
        }

        if (_btnShortlist != null)
        {
            _btnShortlist.text = isShortlisted ? "Shortlisted" : "Shortlist";
            _btnShortlist.SetEnabled(!isShortlisted);
        }

        if (_btnOffer != null)
            _btnOffer.SetEnabled(!_vm.IsOfferOnCooldown && !hasNeg);

        // Footer class based on pipeline state
        if (_footerActions != null)
        {
            _footerActions.RemoveFromClassList("cdm-footer--offer-pending");
            _footerActions.RemoveFromClassList("cdm-footer--accepted");
            _footerActions.RemoveFromClassList("cdm-footer--unavailable");

            if (offer.CanHire)
                _footerActions.AddToClassList("cdm-footer--accepted");
            else if (offer.CanWithdrawOffer)
                _footerActions.AddToClassList("cdm-footer--offer-pending");
        }
    }

    // ── Action handlers ───────────────────────────────────────────────────────

    private void OnCloseClicked()
    {
        _modal.DismissModal();
    }

    private void OnInterviewClicked()
    {
        if (_vm == null || !_vm.InterviewButtonEnabled) return;
        _dispatcher.Dispatch(new StartInterviewCommand
        {
            Tick        = 0,
            CandidateId = _vm.CandidateId,
            Mode        = _vm.IsShortlisted ? HiringMode.HR : HiringMode.Manual
        });
    }

    private void OnShortlistClicked()
    {
        if (_vm == null || _vm.IsShortlisted) return;
        _dispatcher.Dispatch(new ShortlistCandidateCommand
        {
            Tick        = 0,
            CandidateId = _vm.CandidateId,
            DurationDays = 14
        });
    }

    private void OnOfferFooterClicked()
    {
        ShowTab(4);
    }

    private void OnOfferClicked()
    {
        if (_vm == null || !_vm.Offer.CanMakeOffer) return;
        _dispatcher.Dispatch(new MakeOfferCommand
        {
            Tick            = 0,
            CandidateId     = _vm.CandidateId,
            OfferedSalary   = _vm.SalarySliderAnchor,
            Mode            = _vm.IsShortlisted ? HiringMode.HR : HiringMode.Manual,
            EmploymentType  = EmploymentType.FullTime,
            Length          = ContractLengthOption.Standard,
            OfferedRole     = _vm.SelectedRole
        });
    }

    private void OnHireClicked()
    {
        if (_vm == null || !_vm.Offer.CanHire) return;
        _dispatcher.Dispatch(new AcceptCounterOfferCommand
        {
            Tick        = 0,
            CandidateId = _vm.CandidateId
        });
        _modal.DismissModal();
    }

    private void OnWithdrawOfferClicked()
    {
        if (_vm == null || !_vm.Offer.CanWithdrawOffer) return;
        _dispatcher.Dispatch(new RejectCounterOfferCommand
        {
            Tick        = 0,
            CandidateId = _vm.CandidateId
        });
    }

    private void OnAcceptCounterClicked()
    {
        if (_vm == null || !_vm.Offer.CanAcceptCounter) return;
        _dispatcher.Dispatch(new AcceptCounterOfferCommand
        {
            Tick        = 0,
            CandidateId = _vm.CandidateId
        });
    }

    private void OnRejectCounterClicked()
    {
        if (_vm == null || !_vm.Offer.CanRejectCounter) return;
        _dispatcher.Dispatch(new RejectCounterOfferCommand
        {
            Tick        = 0,
            CandidateId = _vm.CandidateId
        });
    }

    private void OnAcceptHRClicked()
    {
        if (_vm == null) return;
        _dispatcher.Dispatch(new AcceptHRCandidateCommand
        {
            Tick        = 0,
            CandidateId = _vm.CandidateId
        });
    }

    private void OnDeclineHRClicked()
    {
        if (_vm == null) return;
        _dispatcher.Dispatch(new DeclineHRCandidateCommand
        {
            Tick        = 0,
            CandidateId = _vm.CandidateId
        });
    }

    private void OnRejectClicked()
    {
        if (_vm == null) return;
        _dispatcher.Dispatch(new DismissCandidateCommand
        {
            Tick        = 0,
            CandidateId = _vm.CandidateId
        });
        _modal.DismissModal();
    }

    // ── Static helpers ────────────────────────────────────────────────────────

    private static void SetButtonVisible(Button btn, bool visible)
    {
        if (btn == null) return;
        btn.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private static void SetStatusCard(
        VisualElement card,
        Label valueLabel,
        string text,
        string stateClass)
    {
        if (valueLabel != null) valueLabel.text = text ?? "\u2014";

        if (card == null) return;
        card.RemoveFromClassList("cdm-status-card--warning");
        card.RemoveFromClassList("cdm-status-card--danger");
        card.RemoveFromClassList("cdm-status-card--success");
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

    private static void BindStringList(VisualElement container, string[] items, string baseClass, string modifierClass)
    {
        if (container == null) return;
        container.Clear();
        if (items == null) return;

        int count = items.Length;
        for (int i = 0; i < count; i++)
        {
            var label = new Label(items[i]);
            label.AddToClassList(baseClass);
            label.AddToClassList(modifierClass);
            container.Add(label);
        }
    }
}
