using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class HRView : IGameView
{
    private readonly ICommandDispatcher _dispatcher;
    private readonly IModalPresenter _modal;
    private HRViewModel _viewModel;

    // Left: candidate list
    private VisualElement _candidateList;
    private ElementPool _candidatePool;
    private Button _rerollButton;

    // Right: header
    private Label _candidateNameLabel;
    private Label _candidateRoleLabel;
    private Label _candidateIdLabel;

    // Hiring mode toggle
    private Button _btnModeManual;
    private Button _btnModeHR;
    private Label _modeContextBanner;

    // Interview panel
    private VisualElement _interviewProgressRow;
    private Label _interviewTeamLabel;
    private ProgressBar _interviewProgressBar;
    private Label _interviewProgressPct;
    private VisualElement _firstReportRow;
    private VisualElement _finalReportSection;
    private Label _recommendationLabel;
    private Label _skillTierLabel;
    private Label _roleFitLabel;
    private Label _hardRejectBadge;
    private Button _startInterviewBtn;
    private Label _noTeamWarning;

    // HR team management
    private VisualElement _hrTeamsSection;
    private VisualElement _hrTeamCards;
    private Button _createHRTeamBtn;
    private VisualElement _createHRTeamForm;
    private TextField _createHRTeamInput;
    private Button _confirmCreateHRTeamBtn;
    private bool _createHRTeamFormOpen;

    // Stats panel
    private VisualElement _statsPanel;
    private Label _statsAge;
    private Label _statsSalary;
    private Label _statsCA;
    private Label _statsPA;
    private Label _statsExpiry;

    // Offer panel
    private VisualElement _offerPanel;
    private Label _salaryDemandLabel;
    private Label _hiringPathLabel;
    private Button _acceptOfferBtn;
    private Button _rejectOfferBtn;
    private VisualElement _counterRow;
    private Label _counterSalaryLabel;
    private Button _acceptCounterBtn;
    private Button _rejectCounterBtn;

    // State
    private int _selectedCandidateId = -1;
    private CandidateDisplay _selectedCandidate;
    private bool _hasCandidateSelected;

    public HRView(ICommandDispatcher dispatcher, IModalPresenter modal)
    {
        _dispatcher = dispatcher;
        _modal = modal;
    }

    public void Initialize(VisualElement root)
    {
#if UNITY_EDITOR
        var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/UXML/Screens/StaffHR.uxml");
        if (visualTree != null)
            visualTree.CloneTree(root);
        else
            Debug.LogError("[HRView] StaffHR.uxml not found at expected path");
#endif

        // Candidate list
        _candidateList = root.Q<VisualElement>("hr-candidates-list");
        if (_candidateList == null) UnityEngine.Debug.LogError("[HRView] hr-candidates-list not found in UXML");
        _candidatePool = new ElementPool(CreateCandidateRow, _candidateList);

        _rerollButton = root.Q<Button>("hr-reroll-btn");
        if (_rerollButton != null)
            _rerollButton.clicked += OnRerollClicked;

        // Candidate header
        _candidateNameLabel = root.Q<Label>("hr-candidate-name");
        _candidateRoleLabel = root.Q<Label>("hr-candidate-role");
        _candidateIdLabel   = root.Q<Label>("hr-candidate-id");

        // Hiring mode toggle
        _btnModeManual     = root.Q<Button>("btn-mode-manual");
        _btnModeHR         = root.Q<Button>("btn-mode-hr");
        _modeContextBanner = root.Q<Label>("hr-mode-context-banner");

        if (_btnModeManual != null)
            _btnModeManual.clicked += OnModeManualClicked;
        if (_btnModeHR != null)
            _btnModeHR.clicked += OnModeHRClicked;

        // Stats panel
        _statsPanel   = root.Q<VisualElement>("hr-stats-panel");
        _statsAge     = root.Q<Label>("hr-stats-age");
        _statsSalary  = root.Q<Label>("hr-stats-salary");
        _statsCA      = root.Q<Label>("hr-stats-ca");
        _statsPA      = root.Q<Label>("hr-stats-pa");
        _statsExpiry  = root.Q<Label>("hr-stats-expiry");

        // Interview panel
        _interviewProgressRow = root.Q<VisualElement>("hr-interview-progress-row");        _interviewTeamLabel   = root.Q<Label>("hr-interview-team-label");
        _interviewProgressBar = root.Q<ProgressBar>("hr-interview-progress-bar");
        _interviewProgressPct = root.Q<Label>("hr-interview-progress-pct");
        _firstReportRow       = root.Q<VisualElement>("hr-first-report-row");
        _finalReportSection   = root.Q<VisualElement>("hr-final-report-section");
        _recommendationLabel  = root.Q<Label>("hr-recommendation-label");
        _skillTierLabel       = root.Q<Label>("hr-skill-tier");
        _roleFitLabel         = root.Q<Label>("hr-role-fit");
        _hardRejectBadge      = root.Q<Label>("hr-hard-reject-badge");
        _startInterviewBtn    = root.Q<Button>("hr-start-interview-btn");
        _noTeamWarning        = root.Q<Label>("hr-no-team-warning");

        if (_startInterviewBtn != null)
            _startInterviewBtn.clicked += OnStartInterviewClicked;

        // HR team management
        _hrTeamsSection    = root.Q<VisualElement>("hr-team-status-list");
        _hrTeamCards       = new VisualElement();
        _hrTeamCards.name  = "hr-team-cards";
        if (_hrTeamsSection != null) _hrTeamsSection.Add(_hrTeamCards);

        // Create HR Team button + inline form
        _createHRTeamBtn = new Button { text = "+ Create HR Team" };
        _createHRTeamBtn.AddToClassList("btn-secondary");
        _createHRTeamBtn.AddToClassList("btn-sm");
        _createHRTeamBtn.style.marginTop = 8;
        _createHRTeamBtn.clicked += OnCreateHRTeamToggled;

        _createHRTeamForm = new VisualElement();
        _createHRTeamForm.style.flexDirection = FlexDirection.Row;
        _createHRTeamForm.style.marginTop = 6;
        _createHRTeamForm.style.display = DisplayStyle.None;

        _createHRTeamInput = new TextField { value = "HR Department" };
        _createHRTeamInput.style.flexGrow = 1;
        _createHRTeamForm.Add(_createHRTeamInput);

        _confirmCreateHRTeamBtn = new Button { text = "Create" };
        _confirmCreateHRTeamBtn.AddToClassList("btn-primary");
        _confirmCreateHRTeamBtn.AddToClassList("btn-sm");
        _confirmCreateHRTeamBtn.style.marginLeft = 6;
        _confirmCreateHRTeamBtn.clicked += OnConfirmCreateHRTeam;
        _createHRTeamForm.Add(_confirmCreateHRTeamBtn);

        if (_hrTeamsSection != null) {
            _hrTeamsSection.Add(_createHRTeamBtn);
            _hrTeamsSection.Add(_createHRTeamForm);
        }

        // Offer panel — built programmatically (not from UXML)
        _offerPanel = root.Q<VisualElement>("hr-offer-panel");

        if (_offerPanel != null)
        {
            _offerPanel.Clear();

            // Salary demand row
            var demandRow = new VisualElement();
            demandRow.style.flexDirection = FlexDirection.Row;
            demandRow.style.justifyContent = Justify.SpaceBetween;
            demandRow.style.alignItems = Align.Center;
            demandRow.style.marginBottom = 8;

            var demandTitleLabel = new Label("Contract Demand");
            demandTitleLabel.AddToClassList("metric-secondary");
            demandRow.Add(demandTitleLabel);

            _salaryDemandLabel = new Label("—");
            _salaryDemandLabel.AddToClassList("metric-value");
            demandRow.Add(_salaryDemandLabel);

            _offerPanel.Add(demandRow);

            // Hiring path label
            _hiringPathLabel = new Label();
            _hiringPathLabel.AddToClassList("metric-tertiary");
            _hiringPathLabel.style.marginBottom = 12;
            _offerPanel.Add(_hiringPathLabel);

            // Accept / Reject buttons
            var btnRow = new VisualElement();
            btnRow.style.flexDirection = FlexDirection.Row;
            btnRow.style.justifyContent = Justify.FlexEnd;

            _rejectOfferBtn = new Button { text = "Reject" };
            _rejectOfferBtn.AddToClassList("btn-danger");
            _rejectOfferBtn.AddToClassList("btn-sm");
            _rejectOfferBtn.style.marginRight = 8;
            _rejectOfferBtn.clicked += OnRejectOfferClicked;
            btnRow.Add(_rejectOfferBtn);

            _acceptOfferBtn = new Button { text = "Accept" };
            _acceptOfferBtn.AddToClassList("btn-primary");
            _acceptOfferBtn.AddToClassList("btn-sm");
            _acceptOfferBtn.clicked += OnAcceptOfferClicked;
            btnRow.Add(_acceptOfferBtn);

            _offerPanel.Add(btnRow);
        }

        _counterRow           = root.Q<VisualElement>("hr-counter-row");
        _counterSalaryLabel   = root.Q<Label>("hr-counter-salary-label");
        _acceptCounterBtn     = root.Q<Button>("hr-accept-counter-btn");
        _rejectCounterBtn     = root.Q<Button>("hr-reject-counter-btn");

        if (_acceptCounterBtn != null)
            _acceptCounterBtn.clicked += OnAcceptCounterClicked;
        if (_rejectCounterBtn != null)
            _rejectCounterBtn.clicked += OnRejectCounterClicked;

        // Initial state: hide detail panels
        SetVisible(_offerPanel, false);
    }

    public void Bind(IViewModel viewModel)
    {
        _viewModel = viewModel as HRViewModel;
        if (_viewModel == null) return;

        _candidatePool.UpdateList(_viewModel.Candidates, BindCandidateRow);

        RebuildHRTeamCards();

        if (_hasCandidateSelected)
        {
            var candidates = _viewModel.Candidates;
            int count = candidates.Count;
            for (int i = 0; i < count; i++)
            {
                if (candidates[i].CandidateId == _selectedCandidateId)
                {
                    ShowCandidateDetail(candidates[i]);
                    return;
                }
            }
            ClearCandidateDetail();
        }
    }

    public void Dispose()
    {
        if (_rerollButton != null) _rerollButton.clicked -= OnRerollClicked;
        if (_btnModeManual != null) _btnModeManual.clicked -= OnModeManualClicked;
        if (_btnModeHR != null) _btnModeHR.clicked -= OnModeHRClicked;
        if (_startInterviewBtn != null) _startInterviewBtn.clicked -= OnStartInterviewClicked;
        if (_createHRTeamBtn != null) _createHRTeamBtn.clicked -= OnCreateHRTeamToggled;
        if (_confirmCreateHRTeamBtn != null) _confirmCreateHRTeamBtn.clicked -= OnConfirmCreateHRTeam;
        if (_acceptOfferBtn != null) _acceptOfferBtn.clicked -= OnAcceptOfferClicked;
        if (_rejectOfferBtn != null) _rejectOfferBtn.clicked -= OnRejectOfferClicked;
        if (_acceptCounterBtn != null) _acceptCounterBtn.clicked -= OnAcceptCounterClicked;
        if (_rejectCounterBtn != null) _rejectCounterBtn.clicked -= OnRejectCounterClicked;
        _candidatePool = null;
        _viewModel = null;
    }

    // ─── Detail panel ─────────────────────────────────────────────────────────

    private void ShowCandidateDetail(CandidateDisplay data)
    {
        _selectedCandidateId = data.CandidateId;
        _selectedCandidate   = data;
        _hasCandidateSelected = true;

        if (_candidateNameLabel != null) _candidateNameLabel.text = data.Name;
        if (_candidateRoleLabel != null) _candidateRoleLabel.text = data.Role;
        if (_candidateIdLabel   != null) _candidateIdLabel.text   = data.CandidateId.ToString();

        SetVisible(_statsPanel, true);
        if (_statsAge    != null) _statsAge.text    = data.Age > 0 ? data.Age.ToString() : "—";
        if (_statsExpiry != null) _statsExpiry.text = data.ExpiryDisplay;

        bool statsRevealed = data.StatsRevealed;
        if (_statsSalary != null)
        {
            _statsSalary.text = statsRevealed ? data.SalaryDisplay : "—";
        }
        if (_statsCA != null)
        {
            _statsCA.text = statsRevealed ? data.CADisplay : "—";
        }
        if (_statsPA != null)
        {
            _statsPA.text = statsRevealed ? data.PADisplay : "—";
        }

        // Hiring mode toggle buttons
        UpdateModeToggle(data);

        // Interview progress row
        bool showProgress = data.InterviewInProgress;
        SetVisible(_interviewProgressRow, showProgress);
        if (showProgress)
        {
            if (_interviewTeamLabel  != null) _interviewTeamLabel.text  = "HR Team: " + data.InterviewTeamLabel;
            if (_interviewProgressBar != null) _interviewProgressBar.value = data.InterviewProgressPercent * 100f;
            if (_interviewProgressPct != null) _interviewProgressPct.text  = (int)(data.InterviewProgressPercent * 100f) + "%";
        }

        // First report badge
        SetVisible(_firstReportRow, data.FirstReportReady && !data.FinalReportReady);

        // Final report section
        bool showFinal = data.FinalReportReady;
        SetVisible(_finalReportSection, showFinal);
        if (showFinal)
        {
            if (_recommendationLabel != null) _recommendationLabel.text = data.RecommendationLabel ?? "";
            if (_skillTierLabel      != null) _skillTierLabel.text      = data.SkillTierLabel ?? "—";
            if (_roleFitLabel        != null) _roleFitLabel.text        = "—";
        }

        // Hard reject badge
        SetVisible(_hardRejectBadge, data.IsHardRejected);

        // Start interview button — available in both Manual and HR mode
        bool isManualMode = data.SelectedMode == HiringMode.Manual;
        bool canStart = !data.IsHardRejected && !data.InterviewInProgress && !data.FinalReportReady
            && (isManualMode || data.CanStartInterview);
        if (_startInterviewBtn != null)
        {
            _startInterviewBtn.SetEnabled(canStart);
            bool showStartBtn = !data.InterviewInProgress && !data.FinalReportReady && !data.IsHardRejected;
            SetVisible(_startInterviewBtn, showStartBtn);
        }

        // No-team warning — only shown in HR mode with no available team
        bool showNoTeamWarning = !isManualMode && !data.CanStartInterview && !data.InterviewInProgress && !data.FinalReportReady && !data.IsHardRejected;
        SetVisible(_noTeamWarning, showNoTeamWarning);

        // Offer panel: show after final report OR when Manual mode allows direct offer
        bool showOffer = (data.FinalReportReady || data.CanMakeOfferManually) && !data.IsHardRejected;
        SetVisible(_offerPanel, showOffer);

        if (showOffer)
        {
            // Salary demand
            if (_salaryDemandLabel != null) _salaryDemandLabel.text = data.SalaryDemandDisplay;

            // Hiring path label
            if (_hiringPathLabel != null)
            {
                if (data.IsTargeted)
                    _hiringPathLabel.text = "HR Sourced";
                else if (data.FinalReportReady)
                    _hiringPathLabel.text = "Interviewed";
                else
                    _hiringPathLabel.text = "Direct Hire — salary demand is highest tier";
            }

            // Accept enabled whenever we have a computed demand
            if (_acceptOfferBtn != null) _acceptOfferBtn.SetEnabled(data.SalaryDemandRaw > 0);

            // Counter row — hidden (counter-offer system removed)
            SetVisible(_counterRow, false);
        }
    }

    private void UpdateModeToggle(CandidateDisplay data)
    {
        if (_viewModel == null) return;

        bool isManual = data.SelectedMode == HiringMode.Manual;

        if (_btnModeManual != null)
        {
            _btnModeManual.RemoveFromClassList("mode-active");
            if (isManual) _btnModeManual.AddToClassList("mode-active");
        }

        if (_btnModeHR != null)
        {
            _btnModeHR.RemoveFromClassList("mode-active");
            if (!isManual) _btnModeHR.AddToClassList("mode-active");
        }

        if (_modeContextBanner != null)
        {
            _modeContextBanner.RemoveFromClassList("accent-warning");
            _modeContextBanner.RemoveFromClassList("accent-success");

            if (isManual)
            {
                _modeContextBanner.text = "No HR team — slower, less accurate, harder to close";
                _modeContextBanner.AddToClassList("accent-warning");
            }
            else
            {
                _modeContextBanner.text = "HR team will be assigned automatically";
                _modeContextBanner.AddToClassList("accent-success");
            }
        }
    }

    private void ClearCandidateDetail()
    {
        _hasCandidateSelected = false;
        _selectedCandidateId = -1;
        if (_candidateNameLabel != null) _candidateNameLabel.text = "—";
        if (_candidateRoleLabel != null) _candidateRoleLabel.text = "";
        SetVisible(_statsPanel, false);
        SetVisible(_interviewProgressRow, false);
        SetVisible(_firstReportRow, false);
        SetVisible(_finalReportSection, false);
        SetVisible(_hardRejectBadge, false);
        SetVisible(_startInterviewBtn, false);
        SetVisible(_noTeamWarning, false);
        SetVisible(_offerPanel, false);
    }

    private void UpdatePatiencePips(int filled) { }

    // ─── Candidate list ───────────────────────────────────────────────────────

    private VisualElement CreateCandidateRow()
    {
        var row = new VisualElement();
        row.AddToClassList("candidate-row");
        row.style.paddingTop    = 8;
        row.style.paddingBottom = 8;
        row.style.borderBottomWidth = 1;
        row.style.borderBottomColor = new UnityEngine.Color(0.86f, 0.82f, 0.76f);
        row.style.cursor = StyleKeyword.None;

        var nameLabel = new Label();
        nameLabel.name = "cand-name";
        nameLabel.style.unityFontStyleAndWeight = UnityEngine.FontStyle.Bold;
        nameLabel.style.fontSize = 13;
        row.Add(nameLabel);

        var sub = new VisualElement();
        sub.style.flexDirection = FlexDirection.Row;
        sub.style.justifyContent = Justify.SpaceBetween;
        sub.style.marginTop = 3;

        var roleLabel = new Label();
        roleLabel.name = "cand-role";
        roleLabel.AddToClassList("role-pill");
        roleLabel.style.alignSelf = Align.FlexStart;
        roleLabel.style.marginTop = 3;
        sub.Add(roleLabel);

        var statusLabel = new Label();
        statusLabel.name = "cand-status";
        statusLabel.style.fontSize = 11;
        sub.Add(statusLabel);

        row.Add(sub);
        return row;
    }

    private void BindCandidateRow(VisualElement el, CandidateDisplay data)
    {
        el.Q<Label>("cand-name").text = data.Name;

        var candRole = el.Q<Label>("cand-role");
        candRole.text = data.Role;
        UIFormatting.ClearRolePillClasses(candRole);
        candRole.AddToClassList(UIFormatting.RolePillClass(data.Role));

        var status = el.Q<Label>("cand-status");
        if (data.IsHardRejected)
        {
            status.text = "Rejected";
            status.style.color = new UnityEngine.Color(0.65f, 0.31f, 0.31f);
        }
        else if (data.FinalReportReady)
        {
            status.text = "Offer Ready";
            status.style.color = new UnityEngine.Color(0.39f, 0.58f, 0.34f);
        }
        else if (data.FirstReportReady)
        {
            status.text = "1st Report";
            status.style.color = new UnityEngine.Color(0.35f, 0.48f, 0.62f);
        }
        else if (data.InterviewInProgress)
        {
            status.text = $"{(int)(data.InterviewProgressPercent * 100f)}%";
            status.style.color = new UnityEngine.Color(0.42f, 0.42f, 0.37f);
        }
        else if (data.IsDeclined)
        {
            status.text = "Declined";
            status.style.color = new UnityEngine.Color(0.77f, 0.60f, 0.25f);
        }
        else if (data.SelectedMode == HiringMode.Manual)
        {
            // Manual mode badge — no active interview, not rejected/declined
            status.text = "Manual";
            status.style.color = new UnityEngine.Color(0.77f, 0.60f, 0.25f);
        }
        else
        {
            status.text = "";
        }

        // Click to select
        el.userData = data;
        el.UnregisterCallback<ClickEvent>(OnCandidateRowClicked);
        el.RegisterCallback<ClickEvent>(OnCandidateRowClicked);
    }

    private void OnCandidateRowClicked(ClickEvent evt)
    {
        var el = evt.currentTarget as VisualElement;
        if (el?.userData is CandidateDisplay data)
        {
            ShowCandidateDetail(data);
        }
    }

    // ─── HR team management ───────────────────────────────────────────────────

    private void RebuildHRTeamCards()
    {
        if (_hrTeamCards == null || _viewModel == null) return;
        _hrTeamCards.Clear();

        var teams = _viewModel.HRTeamStatuses;
        int count = teams.Count;
        for (int i = 0; i < count; i++)
        {
            _hrTeamCards.Add(BuildHRTeamCard(teams[i]));
        }
    }

    private VisualElement BuildHRTeamCard(HRTeamStatusDisplay data)
    {
        var card = new VisualElement();
        card.AddToClassList("card");
        card.style.marginBottom = 8;
        card.style.paddingTop    = 8;
        card.style.paddingBottom = 8;
        card.style.paddingLeft   = 10;
        card.style.paddingRight  = 10;

        // Header row: team name + status badge
        var header = new VisualElement();
        header.style.flexDirection = FlexDirection.Row;
        header.style.justifyContent = Justify.SpaceBetween;
        header.style.alignItems = Align.Center;
        header.style.marginBottom = 6;

        var nameLabel = new Label(data.TeamName);
        nameLabel.AddToClassList("metric-secondary");
        header.Add(nameLabel);

        var statusLabel = new Label(data.StatusLabel);
        statusLabel.AddToClassList("badge");
        switch (data.Status)
        {
            case HRTeamStatus.Idle:         statusLabel.AddToClassList("badge--success"); break;
            case HRTeamStatus.Searching:    statusLabel.AddToClassList("badge--warning"); break;
            case HRTeamStatus.Interviewing: statusLabel.AddToClassList("badge--info");    break;
        }
        header.Add(statusLabel);
        card.Add(header);

        // Member list
        var members = data.Members;
        int mc = members != null ? members.Count : 0;
        if (mc == 0)
        {
            var emptyLabel = new Label("No members — add an HR Specialist");
            emptyLabel.AddToClassList("metric-tertiary");
            emptyLabel.style.marginBottom = 6;
            card.Add(emptyLabel);
        }
        else
        {
            for (int m = 0; m < mc; m++)
            {
                var member = members[m];
                var row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;
                row.style.justifyContent = Justify.SpaceBetween;
                row.style.alignItems = Align.Center;
                row.style.marginBottom = 4;

                var mName = new Label(member.Name);
                mName.AddToClassList("metric-tertiary");
                mName.style.flexGrow = 1;
                row.Add(mName);

                var mRole = new Label(member.Role);
                mRole.AddToClassList("metric-tertiary");
                mRole.style.marginRight = 8;
                row.Add(mRole);

                var removeBtn = new Button { text = "Remove" };
                removeBtn.AddToClassList("btn-sm");
                removeBtn.AddToClassList("btn-danger");
                var capturedId = member.Id;
                removeBtn.clicked += () => {
                    _dispatcher.Dispatch(new RemoveEmployeeFromTeamCommand {
                        Tick = _dispatcher.CurrentTick,
                        EmployeeId = capturedId
                    });
                };
                row.Add(removeBtn);
                card.Add(row);
            }
        }

        // Add Member flyout toggle
        var addMemberBtn = new Button { text = "+ Add Member" };
        addMemberBtn.AddToClassList("btn-secondary");
        addMemberBtn.AddToClassList("btn-sm");
        addMemberBtn.style.marginTop = 4;
        card.Add(addMemberBtn);

        // Flyout: list unassigned HR employees
        var flyout = new VisualElement();
        flyout.style.display = DisplayStyle.None;
        flyout.style.marginTop = 6;
        flyout.AddToClassList("card");
        card.Add(flyout);

        var capturedTeamId = data.Id;
        addMemberBtn.clicked += () => {
            bool open = flyout.style.display == DisplayStyle.Flex;
            flyout.style.display = open ? DisplayStyle.None : DisplayStyle.Flex;
            if (!open) RebuildAddMemberFlyout(flyout, capturedTeamId);
        };

        // ── Assign Search Task button ──────────────────────────────────────────
        var actionRow = new VisualElement();
        actionRow.style.flexDirection = FlexDirection.Row;
        actionRow.style.marginTop = 8;
        actionRow.style.flexWrap = Wrap.Wrap;

        var assignSearchBtn = new Button { text = "Assign Search Task" };
        assignSearchBtn.AddToClassList("btn-sm");
        assignSearchBtn.AddToClassList("btn-primary");
        assignSearchBtn.style.marginRight = 6;
        assignSearchBtn.SetEnabled(data.CanSearch);

        assignSearchBtn.clicked += () => {
            _modal.OpenHRSearchConfigurator(capturedTeamId);
        };
        actionRow.Add(assignSearchBtn);

        // Cancel search button (visible only when searching)
        if (data.Status == HRTeamStatus.Searching && data.ActiveSearchId.HasValue)
        {
            var cancelSearchBtn = new Button { text = "Cancel Search" };
            cancelSearchBtn.AddToClassList("btn-sm");
            cancelSearchBtn.AddToClassList("btn-danger");
            var capturedSearchId = data.ActiveSearchId.Value;
            cancelSearchBtn.clicked += () => {
                _dispatcher.Dispatch(new CancelHRSearchCommand {
                    Tick = _dispatcher.CurrentTick,
                    SearchId = capturedSearchId
                });
            };
            actionRow.Add(cancelSearchBtn);
        }

        card.Add(actionRow);

        return card;
    }

    private void RebuildAddMemberFlyout(VisualElement flyout, TeamId teamId)
    {
        flyout.Clear();
        if (_viewModel == null) return;

        var available = _viewModel.AvailableHREmployees;
        int count = available.Count;

        if (count == 0)
        {
            var emptyLabel = new Label("No unassigned HR Specialists");
            emptyLabel.AddToClassList("metric-tertiary");
            flyout.Add(emptyLabel);
            return;
        }

        for (int i = 0; i < count; i++)
        {
            var emp = available[i];
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.justifyContent = Justify.SpaceBetween;
            row.style.alignItems = Align.Center;
            row.style.marginBottom = 4;

            var nameLabel = new Label(emp.Name);
            nameLabel.AddToClassList("metric-tertiary");
            nameLabel.style.flexGrow = 1;
            row.Add(nameLabel);

            var addBtn = new Button { text = "Add" };
            addBtn.AddToClassList("btn-sm");
            addBtn.AddToClassList("btn-primary");
            var capturedEmpId = emp.EmployeeId;
            addBtn.clicked += () => {
                _dispatcher.Dispatch(new AssignEmployeeToTeamCommand {
                    Tick = _dispatcher.CurrentTick,
                    EmployeeId = capturedEmpId,
                    TeamId = teamId
                });
                flyout.style.display = DisplayStyle.None;
            };
            row.Add(addBtn);
            flyout.Add(row);
        }
    }

    private void OnCreateHRTeamToggled()
    {
        _createHRTeamFormOpen = !_createHRTeamFormOpen;
        if (_createHRTeamForm != null)
        {
            _createHRTeamForm.style.display = _createHRTeamFormOpen ? DisplayStyle.Flex : DisplayStyle.None;
            if (_createHRTeamFormOpen && _createHRTeamInput != null)
            {
                _createHRTeamInput.value = "HR Department";
                _createHRTeamInput.Focus();
            }
        }
    }

    private void OnConfirmCreateHRTeam()
    {
        _dispatcher.Dispatch(new CreateTeamCommand {
            Tick = _dispatcher.CurrentTick,
            TeamType = TeamType.HR
        });
        _createHRTeamFormOpen = false;
        if (_createHRTeamForm != null) _createHRTeamForm.style.display = DisplayStyle.None;
        if (_createHRTeamInput != null) _createHRTeamInput.value = "";
    }

    // ─── Callbacks ────────────────────────────────────────────────────────────

    private void OnRerollClicked()
    {
        _dispatcher.Dispatch(new RerollCandidatePoolCommand());
    }

    private void OnModeManualClicked()
    {
        if (_selectedCandidateId < 0 || _viewModel == null) return;
        _viewModel.SetHiringMode(_selectedCandidateId, HiringMode.Manual);
        var candidates = _viewModel.Candidates;
        int count = candidates.Count;
        for (int i = 0; i < count; i++)
        {
            if (candidates[i].CandidateId == _selectedCandidateId)
            {
                var updated = candidates[i];
                updated.SelectedMode = HiringMode.Manual;
                updated.HiringModeTag = "Manual";
                ShowCandidateDetail(updated);
                return;
            }
        }
    }

    private void OnModeHRClicked()
    {
        if (_selectedCandidateId < 0 || _viewModel == null) return;
        _viewModel.SetHiringMode(_selectedCandidateId, HiringMode.HR);
        var candidates = _viewModel.Candidates;
        int count = candidates.Count;
        for (int i = 0; i < count; i++)
        {
            if (candidates[i].CandidateId == _selectedCandidateId)
            {
                var updated = candidates[i];
                updated.SelectedMode = HiringMode.HR;
                updated.HiringModeTag = "HR";
                ShowCandidateDetail(updated);
                return;
            }
        }
    }

    private void OnStartInterviewClicked()
    {
        if (_selectedCandidateId < 0 || _viewModel == null) return;
        HiringMode mode = _viewModel.GetHiringMode(_selectedCandidateId);
        _dispatcher.Dispatch(new StartInterviewCommand { CandidateId = _selectedCandidateId, Mode = mode });
    }

    private void OnAcceptOfferClicked()
    {
        if (_selectedCandidateId < 0 || _viewModel == null) return;
        // Demand is the exact salary — offer it directly
        int demand = _viewModel.GetSalaryDemand(_selectedCandidateId);
        HiringMode mode = _viewModel.GetHiringMode(_selectedCandidateId);
        _dispatcher.Dispatch(new MakeOfferCommand { Tick = _dispatcher.CurrentTick, CandidateId = _selectedCandidateId, OfferedSalary = demand, Mode = mode });
    }

    private void OnRejectOfferClicked()
    {
        if (_selectedCandidateId < 0 || _viewModel == null) return;
        _dispatcher.Dispatch(new DismissCandidateCommand { Tick = _dispatcher.CurrentTick, CandidateId = _selectedCandidateId });
        ClearCandidateDetail();
    }

    private void OnSliderChanged(ChangeEvent<float> evt) { }

    private void OnMakeOfferClicked()
    {
        if (_selectedCandidateId < 0 || _viewModel == null) return;
        HiringMode mode = _viewModel.GetHiringMode(_selectedCandidateId);
        _dispatcher.Dispatch(new MakeOfferCommand { Tick = _dispatcher.CurrentTick, CandidateId = _selectedCandidateId, OfferedSalary = 0, Mode = mode });
    }

    private void OnAcceptCounterClicked()
    {
        // counter-offer system removed; button kept for layout compatibility but no-op
    }

    private void OnRejectCounterClicked()
    {
        // counter-offer system removed; button kept for layout compatibility but no-op
    }

    // ─── Utility ─────────────────────────────────────────────────────────────

    private static void SetVisible(VisualElement el, bool visible)
    {
        if (el == null) return;
        if (visible) el.RemoveFromClassList("hidden");
        else         el.AddToClassList("hidden");
    }
}