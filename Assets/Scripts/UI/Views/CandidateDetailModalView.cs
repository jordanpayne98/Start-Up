using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class CandidateDetailModalView : IGameView
{
    private readonly IModalPresenter _modal;
    private readonly ICommandDispatcher _dispatcher;

    private VisualElement _root;
    private CandidateDetailModalViewModel _vm;

    // Header
    private Label _nameLabel;
    private Label _ageLabel;
    private Label _rolePill;
    private Label _sourceLabel;
    private Label _expiryLabel;
    private Button _closeButton;
    private Button _offerBackButton;

    // Detail view container
    private VisualElement _detailView;

    // Detail — personality / preferences (right col Preferences card)
    private Label _personalityLabel;
    private Label _ftPrefLabel;
    private Label _lengthPrefLabel;

    // Detail — role suitability rows (8 rows, left col)
    private readonly List<VisualElement> _suitabilityRows = new List<VisualElement>(8);

    // Detail — skill table rows (9 rows, right col)
    private readonly List<Label> _skillNameLabels  = new List<Label>(9);
    private readonly List<Label> _skillValueLabels = new List<Label>(9);

    // Detail — capability
    private Label _abilityLabel;
    private Label _potentialLabel;

    // Detail — salary
    private Label _salaryAskingLabel;
    private Label _salaryMarketLabel;

    // Detail — status bar
    private Label _statusSourceLabel;
    private Label _statusReliabilityLabel;
    private Label _statusExpiryLabel;
    private VisualElement _statusPatienceContainer;
    private readonly List<VisualElement> _statusPatienceDots = new List<VisualElement>(6);

    // Interview progress bar
    private VisualElement _interviewProgressBar;
    private VisualElement _interviewProgressFill;

    // Footer buttons (detail)
    private Button _interviewButton;
    private Button _shortlistButton;
    private Button _rejectButton;
    private Button _makeOfferButton;

    // Offer view container
    private VisualElement _offerView;

    // Offer — role selector rows (one per role)
    private readonly List<VisualElement> _roleRows = new List<VisualElement>(8);

    // Offer arrangement toggle
    private Button _offerFTButton;
    private Button _offerPTButton;
    private EmploymentType _selectedEmploymentType = EmploymentType.FullTime;

    // Offer length selector
    private Button _offerShortBtn;
    private Button _offerStdBtn;
    private Button _offerLongBtn;
    private ContractLengthOption _selectedLength = ContractLengthOption.Standard;

    // Offer salary slider
    private Slider _salarySlider;
    private Label _salarySliderValueLabel;
    private Label _salaryDemandSubLabel;
    private Label _salaryMarketSubLabel;

    // Offer acceptance bar
    private VisualElement _acceptanceBarFill;
    private Label _acceptanceChanceLabel;

    // Patience dots
    private VisualElement _patienceDotsContainer;
    private readonly List<VisualElement> _patienceDots = new List<VisualElement>(6);

    // Mismatch section
    private VisualElement _mismatchSection;
    private Label _mismatchHintLabel;
    private VisualElement _mismatchListContainer;
    private ElementPool _mismatchPool;

    // Confirm offer button
    private Button _confirmOfferButton;

    // Reject confirm overlay
    private VisualElement _rejectOverlay;
    private Button _rejectConfirmButton;
    private Button _rejectCancelButton;

    // Shortlist duration overlay
    private VisualElement _shortlistOverlay;
    private Button _shortlist1m;
    private Button _shortlist3m;
    private Button _shortlist6m;
    private Button _shortlistIndef;
    private Button _shortlistCancel;

    private bool _inOfferView;

    public CandidateDetailModalView(IModalPresenter modal, ICommandDispatcher dispatcher) {
        _modal = modal;
        _dispatcher = dispatcher;
    }

    public void Initialize(VisualElement root) {
        _root = root;
        _root.AddToClassList("candidate-detail-modal");

        // --- Header ---
        var header = new VisualElement();
        header.AddToClassList("modal-header");
        header.AddToClassList("flex-row");
        header.AddToClassList("justify-between");

        var headerLeft = new VisualElement();
        headerLeft.AddToClassList("flex-row");
        headerLeft.style.alignItems = Align.Center;

        _offerBackButton = new Button();
        _offerBackButton.text = "\u2190 Back";
        _offerBackButton.AddToClassList("btn-ghost");
        _offerBackButton.AddToClassList("btn-sm");
        _offerBackButton.style.display = DisplayStyle.None;
        _offerBackButton.style.marginRight = 8;
        headerLeft.Add(_offerBackButton);

        var nameAgeRow = new VisualElement();
        nameAgeRow.AddToClassList("flex-col");
        _nameLabel = new Label("Name");
        _nameLabel.AddToClassList("text-2xl");
        _nameLabel.AddToClassList("text-bold");
        nameAgeRow.Add(_nameLabel);

        var roleMeta = new VisualElement();
        roleMeta.AddToClassList("flex-row");
        roleMeta.style.alignItems = Align.Center;
        _rolePill = new Label("Role");
        _rolePill.AddToClassList("role-pill");
        _rolePill.style.marginRight = 6;
        roleMeta.Add(_rolePill);
        _ageLabel = new Label("Age");
        _ageLabel.AddToClassList("metric-tertiary");
        roleMeta.Add(_ageLabel);
        nameAgeRow.Add(roleMeta);

        headerLeft.Add(nameAgeRow);
        header.Add(headerLeft);

        _closeButton = new Button();
        _closeButton.text = "\u2715";
        _closeButton.AddToClassList("btn-ghost");
        _closeButton.AddToClassList("btn-icon");
        header.Add(_closeButton);

        _root.Add(header);

        // --- Source / Expiry meta row ---
        var metaRow = new VisualElement();
        metaRow.AddToClassList("flex-row");
        metaRow.style.marginBottom = 8;
        _sourceLabel = new Label("Market");
        _sourceLabel.AddToClassList("badge");
        _sourceLabel.AddToClassList("badge--muted");
        _sourceLabel.style.marginRight = 8;
        metaRow.Add(_sourceLabel);
        _expiryLabel = new Label("--d remaining");
        _expiryLabel.AddToClassList("metric-tertiary");
        metaRow.Add(_expiryLabel);
        _root.Add(metaRow);

        // --- Scroll body ---
        var scroll = new ScrollView();
        scroll.style.flexGrow = 1;
        scroll.style.flexShrink = 1;
        _root.Add(scroll);

        // ---- Detail View ----
        _detailView = new VisualElement();
        _detailView.AddToClassList("detail-view");
        scroll.Add(_detailView);

        // Two-column body
        var detailBody = new VisualElement();
        detailBody.AddToClassList("modal-body--two-col");
        _detailView.Add(detailBody);

        // Left column (40%)
        var leftCol = new VisualElement();
        leftCol.AddToClassList("modal-col--left");
        detailBody.Add(leftCol);

        // Right column (60%)
        var rightCol = new VisualElement();
        rightCol.AddToClassList("modal-col--right");
        detailBody.Add(rightCol);

        // --- Left: Role Suitability card ---
        var suitabilityCard = BuildSectionCard("Role Suitability", leftCol);
        var allRoles = RoleSuitabilityCalculator.AllRoles;
        int roleCount = allRoles.Length;
        for (int i = 0; i < roleCount; i++) {
            var row = new VisualElement();
            row.AddToClassList("role-suitability-row");

            var dot = new VisualElement();
            dot.AddToClassList("suitability-dot");
            dot.AddToClassList("suitability-dot--unsuitable");
            dot.name = "suitability-dot";
            row.Add(dot);

            var nameLabel = new Label(UIFormatting.FormatRole(allRoles[i]));
            nameLabel.AddToClassList("metric-secondary");
            nameLabel.name = "suitability-name";
            row.Add(nameLabel);

            var preferredLabel = new Label("(preferred)");
            preferredLabel.AddToClassList("metric-tertiary");
            preferredLabel.style.marginLeft = 6;
            preferredLabel.style.display = DisplayStyle.None;
            preferredLabel.name = "suitability-preferred";
            row.Add(preferredLabel);

            suitabilityCard.Add(row);
            _suitabilityRows.Add(row);
        }

        // --- Left: Capability card ---
        var capCard = BuildSectionCard("Capability", leftCol);
        (_abilityLabel, _)   = AddDetailRow(capCard, "Ability");
        (_potentialLabel, _) = AddDetailRow(capCard, "Potential");

        // --- Left: Salary card ---
        var salaryCard = BuildSectionCard("Salary", leftCol);
        (_salaryAskingLabel, _) = AddDetailRow(salaryCard, "Asking");
        (_salaryMarketLabel, _) = AddDetailRow(salaryCard, "Market Rate");

        // --- Right: Skills Table card ---
        var skillsCard = BuildSectionCard("Skills", rightCol);
        int skillCount = SkillTypeHelper.SkillTypeCount;
        for (int i = 0; i < skillCount; i++) {
            var row = new VisualElement();
            row.AddToClassList("skill-row");

            var nameLabel = new Label("--");
            nameLabel.AddToClassList("skill-row__name");
            row.Add(nameLabel);

            var valueLabel = new Label("--");
            valueLabel.AddToClassList("skill-row__value");
            row.Add(valueLabel);

            skillsCard.Add(row);
            _skillNameLabels.Add(nameLabel);
            _skillValueLabels.Add(valueLabel);
        }

        // --- Right: Preferences card ---
        var prefCard = BuildSectionCard("Preferences", rightCol);
        (_ftPrefLabel, _)     = AddDetailRow(prefCard, "FT / PT");
        (_lengthPrefLabel, _) = AddDetailRow(prefCard, "Contract Length");

        var personalityRow = new VisualElement();
        personalityRow.AddToClassList("flex-row");
        personalityRow.AddToClassList("justify-between");
        personalityRow.AddToClassList("detail-row");
        personalityRow.style.marginBottom = 4;
        var personalityKeyLabel = new Label("Personality");
        personalityKeyLabel.AddToClassList("metric-tertiary");
        personalityRow.Add(personalityKeyLabel);
        _personalityLabel = new Label("Interview required");
        _personalityLabel.AddToClassList("metric-secondary");
        _personalityLabel.style.whiteSpace = WhiteSpace.Normal;
        _personalityLabel.style.maxWidth = 160;
        _personalityLabel.style.unityTextAlign = TextAnchor.MiddleRight;
        personalityRow.Add(_personalityLabel);
        prefCard.Add(personalityRow);

        // Interview progress bar (hidden unless in progress)
        _interviewProgressBar = new VisualElement();
        _interviewProgressBar.AddToClassList("progress-bar");
        _interviewProgressBar.style.marginTop = 8;
        _interviewProgressBar.style.display = DisplayStyle.None;
        _interviewProgressFill = new VisualElement();
        _interviewProgressFill.AddToClassList("progress-bar__fill");
        _interviewProgressBar.Add(_interviewProgressFill);
        _detailView.Add(_interviewProgressBar);

        // --- Status Bar ---
        var statusBar = new VisualElement();
        statusBar.AddToClassList("status-bar");
        _root.Add(statusBar);

        var sourceItem = new VisualElement();
        sourceItem.AddToClassList("status-bar__item");
        var sourceKeyLabel = new Label("Source");
        sourceKeyLabel.AddToClassList("metric-tertiary");
        sourceItem.Add(sourceKeyLabel);
        _statusSourceLabel = new Label("--");
        _statusSourceLabel.AddToClassList("metric-secondary");
        sourceItem.Add(_statusSourceLabel);
        statusBar.Add(sourceItem);

        var confidenceItem = new VisualElement();
        confidenceItem.AddToClassList("status-bar__item");
        var confidenceKeyLabel = new Label("Reliability");
        confidenceKeyLabel.AddToClassList("metric-tertiary");
        confidenceItem.Add(confidenceKeyLabel);
        _statusReliabilityLabel = new Label("--");
        _statusReliabilityLabel.AddToClassList("metric-secondary");
        confidenceItem.Add(_statusReliabilityLabel);
        statusBar.Add(confidenceItem);

        var expiryItem = new VisualElement();
        expiryItem.AddToClassList("status-bar__item");
        var expiryKeyLabel = new Label("Expires");
        expiryKeyLabel.AddToClassList("metric-tertiary");
        expiryItem.Add(expiryKeyLabel);
        _statusExpiryLabel = new Label("--");
        _statusExpiryLabel.AddToClassList("metric-secondary");
        expiryItem.Add(_statusExpiryLabel);
        statusBar.Add(expiryItem);

        var patienceItem = new VisualElement();
        patienceItem.AddToClassList("status-bar__item");
        var patienceKeyLabel = new Label("Patience");
        patienceKeyLabel.AddToClassList("metric-tertiary");
        patienceItem.Add(patienceKeyLabel);
        _statusPatienceContainer = new VisualElement();
        _statusPatienceContainer.AddToClassList("flex-row");
        patienceItem.Add(_statusPatienceContainer);
        statusBar.Add(patienceItem);

        // ---- Offer View ----
        _offerView = new VisualElement();
        _offerView.AddToClassList("offer-view");
        _offerView.style.display = DisplayStyle.None;
        scroll.Add(_offerView);

        BuildOfferView();

        // --- Footer ---
        var footer = new VisualElement();
        footer.AddToClassList("modal-footer");
        footer.AddToClassList("flex-row");
        footer.style.justifyContent = Justify.SpaceBetween;
        _root.Add(footer);

        var footerLeft = new VisualElement();
        footerLeft.AddToClassList("flex-row");

        _rejectButton = new Button();
        _rejectButton.text = "Reject";
        _rejectButton.AddToClassList("btn-danger");
        footerLeft.Add(_rejectButton);

        footer.Add(footerLeft);

        var footerRight = new VisualElement();
        footerRight.AddToClassList("flex-row");

        _shortlistButton = new Button();
        _shortlistButton.text = "Shortlist";
        _shortlistButton.AddToClassList("btn-ghost");
        _shortlistButton.style.marginRight = 8;
        footerRight.Add(_shortlistButton);

        _interviewButton = new Button();
        _interviewButton.text = "Start Interview";
        _interviewButton.AddToClassList("btn-secondary");
        _interviewButton.style.marginRight = 8;
        footerRight.Add(_interviewButton);

        _makeOfferButton = new Button();
        _makeOfferButton.text = "Make Offer \u2192";
        _makeOfferButton.AddToClassList("btn-primary");
        footerRight.Add(_makeOfferButton);

        _confirmOfferButton = new Button();
        _confirmOfferButton.text = "Submit Offer";
        _confirmOfferButton.AddToClassList("btn-primary");
        _confirmOfferButton.style.display = DisplayStyle.None;
        footerRight.Add(_confirmOfferButton);

        footer.Add(footerRight);

        // --- Reject Confirm Overlay ---
        _rejectOverlay = new VisualElement();
        _rejectOverlay.AddToClassList("reject-overlay");
        _rejectOverlay.style.position = Position.Absolute;
        _rejectOverlay.style.left = 0; _rejectOverlay.style.right = 0;
        _rejectOverlay.style.top = 0; _rejectOverlay.style.bottom = 0;
        _rejectOverlay.style.alignItems = Align.Center;
        _rejectOverlay.style.justifyContent = Justify.Center;
        _rejectOverlay.style.backgroundColor = new StyleColor(new Color(0.1f, 0.11f, 0.13f, 0.92f));
        _rejectOverlay.style.display = DisplayStyle.None;

        var rejectConfirmCard = new VisualElement();
        rejectConfirmCard.AddToClassList("card");
        rejectConfirmCard.style.width = 280;

        var rejectMsg = new Label("Remove this candidate from your pool?");
        rejectMsg.AddToClassList("metric-secondary");
        rejectMsg.style.whiteSpace = WhiteSpace.Normal;
        rejectMsg.style.marginBottom = 12;
        rejectConfirmCard.Add(rejectMsg);

        var rejectBtnRow = new VisualElement();
        rejectBtnRow.AddToClassList("flex-row");
        rejectBtnRow.style.justifyContent = Justify.SpaceBetween;

        _rejectCancelButton = new Button();
        _rejectCancelButton.text = "Cancel";
        _rejectCancelButton.AddToClassList("btn-ghost");
        rejectBtnRow.Add(_rejectCancelButton);

        _rejectConfirmButton = new Button();
        _rejectConfirmButton.text = "Confirm Reject";
        _rejectConfirmButton.AddToClassList("btn-danger");
        rejectBtnRow.Add(_rejectConfirmButton);

        rejectConfirmCard.Add(rejectBtnRow);
        _rejectOverlay.Add(rejectConfirmCard);
        _root.Add(_rejectOverlay);

        // --- Shortlist Duration Overlay ---
        _shortlistOverlay = new VisualElement();
        _shortlistOverlay.AddToClassList("reject-overlay");
        _shortlistOverlay.style.position = Position.Absolute;
        _shortlistOverlay.style.left = 0; _shortlistOverlay.style.right = 0;
        _shortlistOverlay.style.top = 0; _shortlistOverlay.style.bottom = 0;
        _shortlistOverlay.style.alignItems = Align.Center;
        _shortlistOverlay.style.justifyContent = Justify.Center;
        _shortlistOverlay.style.backgroundColor = new StyleColor(new Color(0f, 0f, 0f, 0.7f));
        _shortlistOverlay.style.display = DisplayStyle.None;

        var shortlistCard = new VisualElement();
        shortlistCard.AddToClassList("card");
        shortlistCard.style.width = 420;

        var shortlistMsg = new Label("How long should this candidate be shortlisted?");
        shortlistMsg.AddToClassList("metric-secondary");
        shortlistMsg.style.whiteSpace = WhiteSpace.Normal;
        shortlistMsg.style.marginBottom = 12;
        shortlistCard.Add(shortlistMsg);

        var shortlistDurationRow = new VisualElement();
        shortlistDurationRow.AddToClassList("flex-row");
        shortlistDurationRow.style.justifyContent = Justify.SpaceBetween;
        shortlistDurationRow.style.marginBottom = 8;

        _shortlist1m = new Button();
        _shortlist1m.text = "1 Month";
        _shortlist1m.AddToClassList("btn-ghost");
        _shortlist1m.userData = 30;
        shortlistDurationRow.Add(_shortlist1m);

        _shortlist3m = new Button();
        _shortlist3m.text = "3 Months";
        _shortlist3m.AddToClassList("btn-ghost");
        _shortlist3m.userData = 90;
        shortlistDurationRow.Add(_shortlist3m);

        _shortlist6m = new Button();
        _shortlist6m.text = "6 Months";
        _shortlist6m.AddToClassList("btn-ghost");
        _shortlist6m.userData = 180;
        shortlistDurationRow.Add(_shortlist6m);

        _shortlistIndef = new Button();
        _shortlistIndef.text = "Indefinitely";
        _shortlistIndef.AddToClassList("btn-secondary");
        _shortlistIndef.userData = -1;
        shortlistDurationRow.Add(_shortlistIndef);

        shortlistCard.Add(shortlistDurationRow);

        _shortlistCancel = new Button();
        _shortlistCancel.text = "Cancel";
        _shortlistCancel.AddToClassList("btn-ghost");
        _shortlistCancel.style.alignSelf = Align.Center;
        shortlistCard.Add(_shortlistCancel);

        _shortlistOverlay.Add(shortlistCard);
        _root.Add(_shortlistOverlay);

        // Wire handlers
        _closeButton.clicked              += OnCloseClicked;
        _offerBackButton.clicked          += OnOfferBackClicked;
        _interviewButton.clicked          += OnInterviewClicked;
        _shortlistButton.clicked          += OnShortlistClicked;
        _rejectButton.clicked             += OnRejectClicked;
        _makeOfferButton.clicked          += OnMakeOfferClicked;
        _confirmOfferButton.clicked       += OnConfirmOfferClicked;
        _rejectConfirmButton.clicked      += OnRejectConfirmClicked;
        _rejectCancelButton.clicked       += OnRejectCancelClicked;
        _offerFTButton.clicked            += OnOfferFTClicked;
        _offerPTButton.clicked            += OnOfferPTClicked;
        _offerShortBtn.clicked            += OnLengthShortClicked;
        _offerStdBtn.clicked              += OnLengthStdClicked;
        _offerLongBtn.clicked             += OnLengthLongClicked;
        _salarySlider.RegisterValueChangedCallback(OnSalarySliderChanged);
        _shortlist1m.RegisterCallback<ClickEvent>(OnShortlistDurationClicked);
        _shortlist3m.RegisterCallback<ClickEvent>(OnShortlistDurationClicked);
        _shortlist6m.RegisterCallback<ClickEvent>(OnShortlistDurationClicked);
        _shortlistIndef.RegisterCallback<ClickEvent>(OnShortlistDurationClicked);
        _shortlistCancel.clicked          += OnShortlistCancelClicked;
    }

    private void BuildOfferView() {
        // Role selector card
        var roleCard = BuildSectionCard("Role", _offerView);
        var allRoles = RoleSuitabilityCalculator.AllRoles;
        int roleCount = allRoles.Length;
        for (int i = 0; i < roleCount; i++) {
            var row = new VisualElement();
            row.AddToClassList("role-selector-row");
            row.AddToClassList("flex-row");
            row.userData = allRoles[i];

            var dot = new VisualElement();
            dot.AddToClassList("suitability-dot");
            dot.AddToClassList("suitability-dot--unsuitable");
            dot.name = "role-dot";
            row.Add(dot);

            var nameLabel = new Label(UIFormatting.FormatRole(allRoles[i]));
            nameLabel.AddToClassList("metric-secondary");
            nameLabel.name = "role-name";
            row.Add(nameLabel);

            var preferredLabel = new Label("(preferred)");
            preferredLabel.AddToClassList("metric-tertiary");
            preferredLabel.style.marginLeft = 6;
            preferredLabel.style.display = DisplayStyle.None;
            preferredLabel.name = "role-preferred";
            row.Add(preferredLabel);

            row.RegisterCallback<ClickEvent>(OnRoleRowClicked);
            _roleRows.Add(row);
            roleCard.Add(row);
        }

        // Arrangement toggle card
        var arrangementCard = BuildSectionCard("Arrangement", _offerView);
        var arrangementRow = new VisualElement();
        arrangementRow.AddToClassList("flex-row");
        _offerFTButton = new Button();
        _offerFTButton.text = "Full-Time";
        _offerFTButton.AddToClassList("offer-toggle-btn");
        _offerFTButton.AddToClassList("offer-toggle-btn--active");
        arrangementRow.Add(_offerFTButton);
        _offerPTButton = new Button();
        _offerPTButton.text = "Part-Time";
        _offerPTButton.AddToClassList("offer-toggle-btn");
        arrangementRow.Add(_offerPTButton);
        arrangementCard.Add(arrangementRow);

        // Length selector card
        var lengthCard = BuildSectionCard("Contract Length", _offerView);
        var lengthRow = new VisualElement();
        lengthRow.AddToClassList("flex-row");

        _offerShortBtn = CreateLengthCard("Short", "3\u20136 mo", ContractLengthOption.Short);
        _offerStdBtn   = CreateLengthCard("Standard", "6\u201312 mo", ContractLengthOption.Standard);
        _offerLongBtn  = CreateLengthCard("Long", "12\u201318 mo", ContractLengthOption.Long);
        _offerStdBtn.AddToClassList("length-card--selected");
        lengthRow.Add(_offerShortBtn);
        lengthRow.Add(_offerStdBtn);
        lengthRow.Add(_offerLongBtn);
        lengthCard.Add(lengthRow);

        // Salary slider card
        var salarySliderCard = BuildSectionCard("Offer Salary", _offerView);

        _salarySlider = new Slider(0, 1);
        _salarySlider.AddToClassList("salary-slider");
        _salarySlider.style.marginBottom = 4;
        salarySliderCard.Add(_salarySlider);

        _salarySliderValueLabel = new Label("$0/mo");
        _salarySliderValueLabel.AddToClassList("metric-secondary");
        _salarySliderValueLabel.style.marginBottom = 4;
        salarySliderCard.Add(_salarySliderValueLabel);

        var salarySubRow = new VisualElement();
        salarySubRow.AddToClassList("flex-row");
        salarySubRow.AddToClassList("justify-between");
        salarySubRow.style.marginTop = 2;

        _salaryDemandSubLabel = new Label("Their demand: ???");
        _salaryDemandSubLabel.AddToClassList("metric-tertiary");
        salarySubRow.Add(_salaryDemandSubLabel);

        _salaryMarketSubLabel = new Label("Market: ???");
        _salaryMarketSubLabel.AddToClassList("metric-tertiary");
        salarySubRow.Add(_salaryMarketSubLabel);

        salarySliderCard.Add(salarySubRow);

        // Offer Assessment card
        var assessCard = BuildSectionCard("Offer Assessment", _offerView);

        var acceptanceRow = new VisualElement();
        acceptanceRow.AddToClassList("flex-row");
        acceptanceRow.AddToClassList("justify-between");
        acceptanceRow.style.alignItems = Align.Center;
        acceptanceRow.style.marginBottom = 6;

        var acceptanceBarOuter = new VisualElement();
        acceptanceBarOuter.AddToClassList("acceptance-bar");
        acceptanceBarOuter.style.flexGrow = 1;
        acceptanceBarOuter.style.marginRight = 8;

        _acceptanceBarFill = new VisualElement();
        _acceptanceBarFill.AddToClassList("acceptance-bar__fill");
        _acceptanceBarFill.AddToClassList("acceptance-bar--medium");
        _acceptanceBarFill.style.width = Length.Percent(0f);
        acceptanceBarOuter.Add(_acceptanceBarFill);
        acceptanceRow.Add(acceptanceBarOuter);

        _acceptanceChanceLabel = new Label("???");
        _acceptanceChanceLabel.AddToClassList("metric-secondary");
        acceptanceRow.Add(_acceptanceChanceLabel);

        assessCard.Add(acceptanceRow);

        // Patience dots row
        var patienceRow = new VisualElement();
        patienceRow.AddToClassList("flex-row");
        patienceRow.style.alignItems = Align.Center;
        patienceRow.style.marginBottom = 6;

        var patienceLabel = new Label("Patience: ");
        patienceLabel.AddToClassList("metric-tertiary");
        patienceLabel.style.marginRight = 4;
        patienceRow.Add(patienceLabel);

        _patienceDotsContainer = new VisualElement();
        _patienceDotsContainer.AddToClassList("flex-row");
        patienceRow.Add(_patienceDotsContainer);
        assessCard.Add(patienceRow);

        // Mismatch section
        _mismatchSection = new VisualElement();
        _mismatchSection.AddToClassList("mismatch-section");
        _mismatchSection.style.display = DisplayStyle.None;
        _offerView.Add(_mismatchSection);

        _mismatchHintLabel = new Label("");
        _mismatchHintLabel.AddToClassList("metric-tertiary");
        _mismatchHintLabel.style.whiteSpace = WhiteSpace.Normal;
        _mismatchHintLabel.style.marginBottom = 6;
        _mismatchSection.Add(_mismatchHintLabel);

        _mismatchListContainer = new VisualElement();
        _mismatchSection.Add(_mismatchListContainer);
        _mismatchPool = new ElementPool(CreateMismatchWarning, _mismatchListContainer);
    }

    public void Bind(IViewModel viewModel) {
        _vm = viewModel as CandidateDetailModalViewModel;
        if (_vm == null) return;

        // Header
        _nameLabel.text = _vm.Name ?? "--";
        _ageLabel.text  = _vm.Age  ?? "--";
        if (_rolePill != null) {
            _rolePill.text = _vm.RoleName ?? "--";
            UIFormatting.ClearRolePillClasses(_rolePill);
            if (!string.IsNullOrEmpty(_vm.RolePillClass))
                _rolePill.AddToClassList(_vm.RolePillClass);
        }
        _sourceLabel.text = _vm.Source  ?? "--";
        _expiryLabel.text = _vm.ExpiryText ?? "--";

        // Detail sections
        if (_personalityLabel != null) _personalityLabel.text = _vm.PersonalityText ?? "--";

        if (_ftPrefLabel != null) {
            _ftPrefLabel.text = _vm.FTPrefText ?? "--";
            _ftPrefLabel.RemoveFromClassList("detail-row__value--hint");
            _ftPrefLabel.RemoveFromClassList("text-muted");
            if (!string.IsNullOrEmpty(_vm.FTPrefClass))
                _ftPrefLabel.AddToClassList(_vm.FTPrefClass);
        }
        if (_lengthPrefLabel != null) _lengthPrefLabel.text = _vm.LengthPrefText ?? "--";

        // Role suitability rows
        BindSuitabilityRows();

        // Skills table
        BindSkillTable();

        if (_abilityLabel != null)   _abilityLabel.text   = _vm.AbilityEstimate   ?? "--";
        if (_potentialLabel != null) _potentialLabel.text = _vm.PotentialEstimate ?? "--";
        if (_salaryAskingLabel != null) _salaryAskingLabel.text = _vm.SalaryAsking ?? "--";
        if (_salaryMarketLabel != null) _salaryMarketLabel.text = _vm.SalaryMarket ?? "--";

        // Status bar
        BindStatusBar();

        // Interview progress
        if (_interviewProgressBar != null) {
            bool showProgress = _vm.IsInterviewInProgress || (_vm.KnowledgePercent > 0f && !_vm.IsInterviewed);
            _interviewProgressBar.style.display = showProgress ? DisplayStyle.Flex : DisplayStyle.None;
            if (showProgress && _interviewProgressFill != null)
                _interviewProgressFill.style.width = Length.Percent(_vm.KnowledgePercent);
        }

        // Interview button
        if (_interviewButton != null) {
            _interviewButton.text = _vm.InterviewButtonText;
            _interviewButton.RemoveFromClassList("btn-secondary");
            _interviewButton.RemoveFromClassList("btn-ghost");
            _interviewButton.AddToClassList(_vm.InterviewButtonClass);
            _interviewButton.SetEnabled(_vm.InterviewButtonEnabled);
        }

        // Shortlist button
        if (_shortlistButton != null) {
            _shortlistButton.text = _vm.IsShortlisted ? "Shortlisted \u2713" : "Shortlist";
            _shortlistButton.SetEnabled(!_vm.IsShortlisted);
        }

        // Make Offer button — disabled during cooldown
        if (_makeOfferButton != null) {
            if (_vm.IsOfferOnCooldown) {
                _makeOfferButton.text = "Candidate is reviewing options";
                _makeOfferButton.SetEnabled(false);
                _makeOfferButton.RemoveFromClassList("btn-primary");
                _makeOfferButton.AddToClassList("btn-ghost");
            } else {
                _makeOfferButton.text = _vm.HasPendingCounter ? "Counter Offer \u2192" : "Make Offer \u2192";
                _makeOfferButton.SetEnabled(true);
                _makeOfferButton.RemoveFromClassList("btn-ghost");
                _makeOfferButton.AddToClassList("btn-primary");
            }
        }

        if (_confirmOfferButton != null) {
            _confirmOfferButton.text = _vm.HasPendingCounter ? "Submit Counter" : "Submit Offer";
        }

        // Offer view elements (bind regardless of visibility so they're ready when shown)
        BindOfferElements();
    }

    private void BindSuitabilityRows() {
        var entries = _vm.RoleSuitabilities;
        int rowCount = _suitabilityRows.Count;
        for (int i = 0; i < rowCount && i < entries.Length; i++) {
            var row = _suitabilityRows[i];
            var entry = entries[i];

            var dot = row.Q<VisualElement>("suitability-dot");
            if (dot != null) {
                dot.RemoveFromClassList("suitability-dot--natural");
                dot.RemoveFromClassList("suitability-dot--accomplished");
                dot.RemoveFromClassList("suitability-dot--competent");
                dot.RemoveFromClassList("suitability-dot--awkward");
                dot.RemoveFromClassList("suitability-dot--unsuitable");
                dot.RemoveFromClassList("suitability-dot--unknown");
                string dotClass = _vm.IsSkillsRevealed ? entry.SuitabilityClass : "suitability-dot--unknown";
                dot.AddToClassList(dotClass);
            }

            var preferredEl = row.Q<Label>("suitability-preferred");
            if (preferredEl != null)
                preferredEl.style.display = entry.IsPreferred ? DisplayStyle.Flex : DisplayStyle.None;

            if (entry.IsPreferred) row.AddToClassList("role-suitability-row--preferred");
            else row.RemoveFromClassList("role-suitability-row--preferred");
        }
    }

    private void BindSkillTable() {
        var table = _vm.SkillTable;
        int count = _skillNameLabels.Count;
        for (int i = 0; i < count && i < table.Length; i++) {
            var entry = table[i];
            var nameLabel = _skillNameLabels[i];
            var valueLabel = _skillValueLabels[i];

            nameLabel.text = entry.Name;
            nameLabel.style.color = new StyleColor(entry.NameColor);

            valueLabel.text = entry.ValueText;
            valueLabel.RemoveFromClassList("skill-row__value--up");
            valueLabel.RemoveFromClassList("skill-row__value--down");
            valueLabel.RemoveFromClassList("skill-row__value--unknown");
            if (!string.IsNullOrEmpty(entry.ValueClass))
                valueLabel.AddToClassList(entry.ValueClass);
        }
    }

    private void BindStatusBar() {
        if (_statusSourceLabel != null)      _statusSourceLabel.text      = _vm.Source ?? "--";
        if (_statusReliabilityLabel != null) _statusReliabilityLabel.text = string.IsNullOrEmpty(_vm.ReliabilityText) ? "--" : _vm.ReliabilityText;
        if (_statusExpiryLabel != null)      _statusExpiryLabel.text      = _vm.ExpiryText ?? "--";
        BindStatusPatienceDots();
    }

    private void BindStatusPatienceDots() {
        if (_statusPatienceContainer == null) return;
        int max     = _vm.MaxPatience;
        int current = _vm.CurrentPatience;

        while (_statusPatienceDots.Count < max) {
            var dot = new VisualElement();
            dot.AddToClassList("patience-dot");
            _statusPatienceContainer.Add(dot);
            _statusPatienceDots.Add(dot);
        }

        int dotCount = _statusPatienceDots.Count;
        for (int i = 0; i < dotCount; i++) {
            _statusPatienceDots[i].style.display = i < max ? DisplayStyle.Flex : DisplayStyle.None;
        }

        for (int i = 0; i < max; i++) {
            var dot = _statusPatienceDots[i];
            dot.RemoveFromClassList("patience-dot--safe");
            dot.RemoveFromClassList("patience-dot--warning");
            dot.RemoveFromClassList("patience-dot--critical");
            dot.RemoveFromClassList("patience-dot--empty");

            if (i >= current) {
                dot.AddToClassList("patience-dot--empty");
            } else if (current >= 3) {
                dot.AddToClassList("patience-dot--safe");
            } else if (current == 2) {
                dot.AddToClassList("patience-dot--warning");
            } else {
                dot.AddToClassList("patience-dot--critical");
            }
        }
    }

    private void BindOfferElements() {
        if (_vm == null) return;

        // Role selector rows
        var entries = _vm.RoleSuitabilities;
        int rowCount = _roleRows.Count;
        for (int i = 0; i < rowCount && i < entries.Length; i++) {
            var row = _roleRows[i];
            var entry = entries[i];

            var dot = row.Q<VisualElement>("role-dot");
            if (dot != null) {
                dot.RemoveFromClassList("suitability-dot--natural");
                dot.RemoveFromClassList("suitability-dot--accomplished");
                dot.RemoveFromClassList("suitability-dot--competent");
                dot.RemoveFromClassList("suitability-dot--awkward");
                dot.RemoveFromClassList("suitability-dot--unsuitable");
                string dotClass = _vm.IsSkillsRevealed ? entry.SuitabilityClass : "suitability-dot--unsuitable";
                dot.AddToClassList(dotClass);
            }

            var preferredEl = row.Q<Label>("role-preferred");
            if (preferredEl != null)
                preferredEl.style.display = entry.IsPreferred ? DisplayStyle.Flex : DisplayStyle.None;

            bool isSelected = entry.Role == _vm.SelectedRole;
            if (isSelected) row.AddToClassList("role-selector-row--selected");
            else            row.RemoveFromClassList("role-selector-row--selected");

            if (entry.IsPreferred) row.AddToClassList("role-selector-row--preferred");
            else                   row.RemoveFromClassList("role-selector-row--preferred");
        }

        // Salary slider
        if (_salarySlider != null) {
            _salarySlider.lowValue  = _vm.SalarySliderMin;
            _salarySlider.highValue = _vm.SalarySliderMax;
            _salarySlider.SetValueWithoutNotify(_vm.CurrentOfferSalary);
            _salarySliderValueLabel.text = UIFormatting.FormatMoney(_vm.CurrentOfferSalary) + "/mo";
        }

        // Salary sub-labels
        if (_salaryDemandSubLabel != null) {
            string demandText = _vm.IsSalaryDemandVisible
                ? "Their demand: " + UIFormatting.FormatMoney(_vm.SalarySliderAnchor) + "/mo"
                : "Their demand: ???";
            _salaryDemandSubLabel.text = demandText;
        }
        if (_salaryMarketSubLabel != null) {
            _salaryMarketSubLabel.text = "Market: " + _vm.SalaryMarket;
        }

        // Acceptance bar
        if (_acceptanceBarFill != null) {
            _acceptanceBarFill.RemoveFromClassList("acceptance-bar--high");
            _acceptanceBarFill.RemoveFromClassList("acceptance-bar--medium");
            _acceptanceBarFill.RemoveFromClassList("acceptance-bar--low");
            _acceptanceBarFill.AddToClassList(_vm.AcceptanceChanceClass);
            _acceptanceBarFill.style.width = Length.Percent(_vm.AcceptanceChance);
        }
        if (_acceptanceChanceLabel != null)
            _acceptanceChanceLabel.text = _vm.AcceptanceChanceText ?? "???";

        // Patience dots
        BindPatienceDots();

        // Mismatch
        if (_mismatchSection != null) {
            _mismatchSection.style.display = _vm.ShowMismatchSection ? DisplayStyle.Flex : DisplayStyle.None;
            if (_mismatchHintLabel != null) _mismatchHintLabel.text = _vm.MismatchHintText ?? "";
            _mismatchPool.UpdateList(_vm.MismatchWarnings, BindMismatchWarning);
        }
    }

    private void BindPatienceDots() {
        if (_patienceDotsContainer == null) return;
        int max = _vm.MaxPatience;
        int current = _vm.CurrentPatience;

        // Grow pool if needed
        while (_patienceDots.Count < max) {
            var dot = new VisualElement();
            dot.AddToClassList("patience-dot");
            _patienceDotsContainer.Add(dot);
            _patienceDots.Add(dot);
        }
        // Hide excess dots
        int dotCount = _patienceDots.Count;
        for (int i = 0; i < dotCount; i++) {
            _patienceDots[i].style.display = i < max ? DisplayStyle.Flex : DisplayStyle.None;
        }

        // Assign USS classes based on position and current patience
        for (int i = 0; i < max; i++) {
            var dot = _patienceDots[i];
            dot.RemoveFromClassList("patience-dot--safe");
            dot.RemoveFromClassList("patience-dot--warning");
            dot.RemoveFromClassList("patience-dot--critical");
            dot.RemoveFromClassList("patience-dot--empty");

            if (i >= current) {
                dot.AddToClassList("patience-dot--empty");
            } else if (current >= 3) {
                dot.AddToClassList("patience-dot--safe");
            } else if (current == 2) {
                dot.AddToClassList("patience-dot--warning");
            } else {
                dot.AddToClassList("patience-dot--critical");
            }
        }
    }

    public void Dispose() {
        if (_closeButton != null)       _closeButton.clicked       -= OnCloseClicked;
        if (_offerBackButton != null)   _offerBackButton.clicked   -= OnOfferBackClicked;
        if (_interviewButton != null)   _interviewButton.clicked   -= OnInterviewClicked;
        if (_shortlistButton != null)   _shortlistButton.clicked   -= OnShortlistClicked;
        if (_rejectButton != null)      _rejectButton.clicked      -= OnRejectClicked;
        if (_makeOfferButton != null)   _makeOfferButton.clicked   -= OnMakeOfferClicked;
        if (_confirmOfferButton != null) _confirmOfferButton.clicked -= OnConfirmOfferClicked;
        if (_rejectConfirmButton != null) _rejectConfirmButton.clicked -= OnRejectConfirmClicked;
        if (_rejectCancelButton != null)  _rejectCancelButton.clicked  -= OnRejectCancelClicked;
        if (_offerFTButton != null)     _offerFTButton.clicked     -= OnOfferFTClicked;
        if (_offerPTButton != null)     _offerPTButton.clicked     -= OnOfferPTClicked;
        if (_offerShortBtn != null)     _offerShortBtn.clicked     -= OnLengthShortClicked;
        if (_offerStdBtn != null)       _offerStdBtn.clicked       -= OnLengthStdClicked;
        if (_offerLongBtn != null)      _offerLongBtn.clicked      -= OnLengthLongClicked;
        if (_salarySlider != null)      _salarySlider.UnregisterValueChangedCallback(OnSalarySliderChanged);

        int rowCount = _roleRows.Count;
        for (int i = 0; i < rowCount; i++)
            _roleRows[i].UnregisterCallback<ClickEvent>(OnRoleRowClicked);
        _roleRows.Clear();
        _patienceDots.Clear();
        _suitabilityRows.Clear();
        _skillNameLabels.Clear();
        _skillValueLabels.Clear();
        _statusPatienceDots.Clear();

        if (_shortlist1m != null)    _shortlist1m.UnregisterCallback<ClickEvent>(OnShortlistDurationClicked);
        if (_shortlist3m != null)    _shortlist3m.UnregisterCallback<ClickEvent>(OnShortlistDurationClicked);
        if (_shortlist6m != null)    _shortlist6m.UnregisterCallback<ClickEvent>(OnShortlistDurationClicked);
        if (_shortlistIndef != null) _shortlistIndef.UnregisterCallback<ClickEvent>(OnShortlistDurationClicked);
        if (_shortlistCancel != null) _shortlistCancel.clicked -= OnShortlistCancelClicked;
        _shortlistOverlay = null;
        _shortlist1m = null;
        _shortlist3m = null;
        _shortlist6m = null;
        _shortlistIndef = null;
        _shortlistCancel = null;

        _mismatchPool = null;
        _vm = null;
        _root = null;
    }

    // --- Handlers ---

    private void OnCloseClicked() => _modal?.DismissModal();

    private void OnOfferBackClicked() => ShowDetailView();

    private void OnInterviewClicked() {
        if (_vm == null) return;
        if (!_vm.IsShortlisted) {
            _dispatcher.Dispatch(new ShortlistCandidateCommand {
                Tick = _dispatcher.CurrentTick,
                CandidateId = GetCandidateId(),
                DurationDays = -1
            });
        }
        _dispatcher.Dispatch(new StartInterviewCommand {
            CandidateId = GetCandidateId(),
            Mode = GetHiringMode()
        });
        _modal?.DismissModal();
    }

    private void OnShortlistClicked() {
        if (_vm == null) return;
        if (_shortlistOverlay != null)
            _shortlistOverlay.style.display = DisplayStyle.Flex;
    }

    private void OnShortlistDurationClicked(ClickEvent evt) {
        if (_vm == null) return;
        int durationDays = (int)((VisualElement)evt.currentTarget).userData;
        _dispatcher.Dispatch(new ShortlistCandidateCommand {
            Tick = _dispatcher.CurrentTick,
            CandidateId = GetCandidateId(),
            DurationDays = durationDays
        });
        if (_shortlistOverlay != null)
            _shortlistOverlay.style.display = DisplayStyle.None;
        if (_shortlistButton != null) {
            _shortlistButton.text = "Shortlisted \u2713";
            _shortlistButton.SetEnabled(false);
        }
    }

    private void OnShortlistCancelClicked() {
        if (_shortlistOverlay != null)
            _shortlistOverlay.style.display = DisplayStyle.None;
    }

    private void OnRejectClicked() {
        if (_rejectOverlay != null)
            _rejectOverlay.style.display = DisplayStyle.Flex;
    }

    private void OnRejectCancelClicked() {
        if (_rejectOverlay != null)
            _rejectOverlay.style.display = DisplayStyle.None;
    }

    private void OnRejectConfirmClicked() {
        if (_vm == null) return;
        _dispatcher.Dispatch(new DismissCandidateCommand {
            CandidateId = GetCandidateId()
        });
        _modal?.DismissModal();
    }

    private void OnMakeOfferClicked() => ShowOfferView();

    private void OnConfirmOfferClicked() {
        if (_vm == null) return;
        _dispatcher.Dispatch(new MakeOfferCommand {
            CandidateId    = GetCandidateId(),
            OfferedSalary  = _vm.CurrentOfferSalary,
            OfferedRole    = _vm.SelectedRole,
            Mode           = GetHiringMode(),
            EmploymentType = _selectedEmploymentType,
            Length         = _selectedLength
        });
        _modal?.DismissModal();
    }

    private void OnOfferFTClicked() {
        _selectedEmploymentType = EmploymentType.FullTime;
        SetArrangementToggle(fullTime: true);
        if (_vm != null) {
            _vm.RefreshOfferData(_selectedEmploymentType, _selectedLength);
            SyncSliderFromVm();
            BindOfferElements();
        }
    }

    private void OnOfferPTClicked() {
        _selectedEmploymentType = EmploymentType.PartTime;
        SetArrangementToggle(fullTime: false);
        if (_vm != null) {
            _vm.RefreshOfferData(_selectedEmploymentType, _selectedLength);
            SyncSliderFromVm();
            BindOfferElements();
        }
    }

    private void OnLengthShortClicked() {
        _selectedLength = ContractLengthOption.Short;
        SetLengthSelected(_offerShortBtn);
        if (_vm != null) {
            _vm.RefreshOfferData(_selectedEmploymentType, _selectedLength);
            SyncSliderFromVm();
            BindOfferElements();
        }
    }

    private void OnLengthStdClicked() {
        _selectedLength = ContractLengthOption.Standard;
        SetLengthSelected(_offerStdBtn);
        if (_vm != null) {
            _vm.RefreshOfferData(_selectedEmploymentType, _selectedLength);
            SyncSliderFromVm();
            BindOfferElements();
        }
    }

    private void OnLengthLongClicked() {
        _selectedLength = ContractLengthOption.Long;
        SetLengthSelected(_offerLongBtn);
        if (_vm != null) {
            _vm.RefreshOfferData(_selectedEmploymentType, _selectedLength);
            SyncSliderFromVm();
            BindOfferElements();
        }
    }

    private void OnRoleRowClicked(ClickEvent evt) {
        if (_vm == null) return;
        var target = evt.currentTarget as VisualElement;
        if (target == null || !(target.userData is EmployeeRole role)) return;
        _vm.SetSelectedRole(role);
        SyncSliderFromVm();
        BindOfferElements();
    }

    private void OnSalarySliderChanged(ChangeEvent<float> evt) {
        if (_vm == null) return;
        int rounded = SalaryDemandCalculator.Round50(evt.newValue);
        _salarySlider.SetValueWithoutNotify(rounded);
        _vm.SetOfferSalary(rounded);
        if (_salarySliderValueLabel != null)
            _salarySliderValueLabel.text = UIFormatting.FormatMoney(rounded) + "/mo";
        // Refresh only acceptance display
        if (_acceptanceBarFill != null) {
            _acceptanceBarFill.RemoveFromClassList("acceptance-bar--high");
            _acceptanceBarFill.RemoveFromClassList("acceptance-bar--medium");
            _acceptanceBarFill.RemoveFromClassList("acceptance-bar--low");
            _acceptanceBarFill.AddToClassList(_vm.AcceptanceChanceClass);
            _acceptanceBarFill.style.width = Length.Percent(_vm.AcceptanceChance);
        }
        if (_acceptanceChanceLabel != null)
            _acceptanceChanceLabel.text = _vm.AcceptanceChanceText ?? "???";
    }

    private void SyncSliderFromVm() {
        if (_vm == null || _salarySlider == null) return;
        _salarySlider.lowValue  = _vm.SalarySliderMin;
        _salarySlider.highValue = _vm.SalarySliderMax;
        _salarySlider.SetValueWithoutNotify(_vm.CurrentOfferSalary);
        if (_salarySliderValueLabel != null)
            _salarySliderValueLabel.text = UIFormatting.FormatMoney(_vm.CurrentOfferSalary) + "/mo";
    }

    // --- View swap ---

    private void ShowDetailView() {
        _inOfferView = false;
        if (_detailView != null) _detailView.style.display = DisplayStyle.Flex;
        if (_offerView != null) _offerView.style.display   = DisplayStyle.None;
        if (_offerBackButton != null) _offerBackButton.style.display = DisplayStyle.None;
        if (_makeOfferButton != null) _makeOfferButton.style.display = DisplayStyle.Flex;
        if (_confirmOfferButton != null) _confirmOfferButton.style.display = DisplayStyle.None;
        if (_interviewButton != null) _interviewButton.style.display = DisplayStyle.Flex;
        if (_shortlistButton != null) _shortlistButton.style.display = DisplayStyle.Flex;
    }

    private void ShowOfferView() {
        if (_vm != null && _vm.IsOfferOnCooldown) return;
        _inOfferView = true;
        if (_detailView != null) _detailView.style.display = DisplayStyle.None;
        if (_offerView != null) _offerView.style.display   = DisplayStyle.Flex;
        if (_offerBackButton != null) _offerBackButton.style.display = DisplayStyle.Flex;
        if (_makeOfferButton != null) _makeOfferButton.style.display = DisplayStyle.None;
        if (_confirmOfferButton != null) _confirmOfferButton.style.display = DisplayStyle.Flex;
        if (_interviewButton != null) _interviewButton.style.display = DisplayStyle.None;
        if (_shortlistButton != null) _shortlistButton.style.display = DisplayStyle.None;

        if (_vm != null) {
            _vm.RefreshOfferData(_selectedEmploymentType, _selectedLength);
            SyncSliderFromVm();
            BindOfferElements();
            SetArrangementToggle(_selectedEmploymentType == EmploymentType.FullTime);
            Button lengthBtn = _selectedLength == ContractLengthOption.Short ? _offerShortBtn
                : _selectedLength == ContractLengthOption.Long ? _offerLongBtn
                : _offerStdBtn;
            SetLengthSelected(lengthBtn);
        }
    }

    // --- Helpers ---

    // Called by WindowManager when a counter-offer is pending for this candidate.
    // Counter-offer view rendering is handled in Plan 6.
    public void ShowCounterOfferView() {
        // Placeholder: reserved for Plan 6 counter-offer UI integration.
        ShowOfferView();
    }

    private int GetCandidateId() => _vm?.CandidateId ?? 0;

    private HiringMode GetHiringMode() {
        if (_vm == null) return HiringMode.Manual;
        return _vm.Source == "HR Sourced" || _vm.Source == "Shortlisted" ? HiringMode.HR : HiringMode.Manual;
    }

    private static VisualElement BuildSectionCard(string title, VisualElement parent) {
        var card = new VisualElement();
        card.AddToClassList("card");
        card.style.marginBottom = 8;
        var titleLabel = new Label(title);
        titleLabel.AddToClassList("card__title");
        card.Add(titleLabel);
        parent.Add(card);
        return card;
    }

    private static (Label value, VisualElement row) AddDetailRow(VisualElement parent, string label) {
        var row = new VisualElement();
        row.AddToClassList("flex-row");
        row.AddToClassList("justify-between");
        row.AddToClassList("detail-row");
        row.style.marginBottom = 4;

        var labelEl = new Label(label);
        labelEl.AddToClassList("metric-tertiary");
        row.Add(labelEl);

        var valueEl = new Label("--");
        valueEl.AddToClassList("metric-secondary");
        valueEl.name = "detail-row__value";
        row.Add(valueEl);

        parent.Add(row);
        return (valueEl, row);
    }

    private void SetArrangementToggle(bool fullTime) {
        if (_offerFTButton != null) {
            if (fullTime) _offerFTButton.AddToClassList("offer-toggle-btn--active");
            else _offerFTButton.RemoveFromClassList("offer-toggle-btn--active");
        }
        if (_offerPTButton != null) {
            if (!fullTime) _offerPTButton.AddToClassList("offer-toggle-btn--active");
            else _offerPTButton.RemoveFromClassList("offer-toggle-btn--active");
        }
    }

    private void SetLengthSelected(Button selected) {
        if (_offerShortBtn != null) _offerShortBtn.RemoveFromClassList("length-card--selected");
        if (_offerStdBtn != null)   _offerStdBtn.RemoveFromClassList("length-card--selected");
        if (_offerLongBtn != null)  _offerLongBtn.RemoveFromClassList("length-card--selected");
        selected?.AddToClassList("length-card--selected");
    }

    private Button CreateLengthCard(string label, string sub, ContractLengthOption option) {
        var btn = new Button();
        btn.AddToClassList("length-card");
        var labelEl = new Label(label);
        labelEl.AddToClassList("text-bold");
        btn.Add(labelEl);
        var subEl = new Label(sub);
        subEl.AddToClassList("metric-tertiary");
        btn.Add(subEl);
        return btn;
    }

    private VisualElement CreateMismatchWarning() {
        var row = new VisualElement();
        row.AddToClassList("mismatch-warning");
        row.AddToClassList("flex-row");
        var icon = new Label("\u26A0");
        icon.AddToClassList("text-warning");
        icon.style.marginRight = 6;
        row.Add(icon);
        var text = new Label();
        text.name = "mismatch-text";
        text.AddToClassList("metric-tertiary");
        text.style.whiteSpace = WhiteSpace.Normal;
        row.Add(text);
        return row;
    }

    private void BindMismatchWarning(VisualElement el, string warning) {
        var text = el.Q<Label>("mismatch-text");
        if (text != null) text.text = warning;
    }
}
