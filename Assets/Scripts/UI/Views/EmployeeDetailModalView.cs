using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class EmployeeDetailModalView : IGameView
{
    private readonly ICommandDispatcher _dispatcher;
    private readonly IModalPresenter _modal;

    private VisualElement _root;
    private EmployeeDetailModalViewModel _vm;

    // Header
    private Label _nameLabel;
    private Label _ageLabel;
    private Label _roleLabel;
    private Label _founderBadge;
    private Label _personalityLabel;

    // Left column — role suitability rows
    private readonly List<VisualElement> _roleSuitRows = new List<VisualElement>(8);

    // Left column — contract
    private Label _contractTypeLabel;
    private Label _contractLengthLabel;
    private Label _salaryLabel;
    private Label _remainingLabel;
    private Label _hiredDateLabel;
    private Label _marketRateLabel;
    private Label _marketPositionLabel;
    private Label _valueEffLabel;

    // Left column — escalation
    private VisualElement _escalationIndicator;
    private Label _escalationText;

    // Left column — departure risk
    private VisualElement _departureRiskCard;
    private Label _departureRiskText;

    // Right column — skill table rows
    private readonly List<Label> _skillNameLabels  = new List<Label>(9);
    private readonly List<Label> _skillValueLabels = new List<Label>(9);

    // Right column — renewal section
    private VisualElement _renewalCard;
    private Label _renewalCurrentSalaryLabel;
    private Label _renewalDemandLabel;
    private VisualElement _patienceDotsContainer;
    private readonly List<VisualElement> _renewalPatienceDots = new List<VisualElement>(6);
    private Label _renewalCooldownLabel;
    private VisualElement _renewalCounterSummary;
    private Label _counterSalaryLabel;
    private Label _counterRoleLabel;
    private Label _counterTypeLabel;
    private Label _counterLengthLabel;

    // Right column — off-role indicator
    private VisualElement _offRoleCard;
    private Label _offRoleLabel;

    // Status bar
    private Label _statusTeamLabel;
    private Label _statusMoraleLabel;
    private Label _statusEnergyLabel;

    // Footer buttons
    private Button _btnClose;
    private Button _btnOfferContract;

    // Content panels (for swap between detail and renewal)
    private VisualElement _detailPanel;
    private VisualElement _renewalPanel;

    // Renewal sub-view
    private ContractRenewalModalView _renewalView;
    private ContractRenewalModalViewModel _renewalVm;

    public EmployeeDetailModalView(ICommandDispatcher dispatcher, IModalPresenter modal) {
        _dispatcher = dispatcher;
        _modal = modal;
    }

    public void Initialize(VisualElement root) {
        _root = root;
        _root.AddToClassList("employee-detail-modal");

        // Modal header
        var header = new VisualElement();
        header.AddToClassList("modal-header");

        var headerLeft = new VisualElement();
        headerLeft.AddToClassList("flex-row");
        headerLeft.AddToClassList("align-center");
        headerLeft.style.flexGrow = 1;

        _nameLabel = new Label();
        _nameLabel.AddToClassList("modal-title");
        headerLeft.Add(_nameLabel);

        _roleLabel = new Label();
        _roleLabel.AddToClassList("role-pill");
        _roleLabel.style.marginLeft = 10;
        headerLeft.Add(_roleLabel);

        _founderBadge = new Label("Founder");
        _founderBadge.AddToClassList("role-pill");
        _founderBadge.AddToClassList("role-pill--founder");
        _founderBadge.style.marginLeft = 8;
        _founderBadge.style.display = DisplayStyle.None;
        headerLeft.Add(_founderBadge);

        _ageLabel = new Label();
        _ageLabel.AddToClassList("metric-tertiary");
        _ageLabel.style.marginLeft = 10;
        headerLeft.Add(_ageLabel);

        _personalityLabel = new Label();
        _personalityLabel.AddToClassList("badge");
        _personalityLabel.style.marginLeft = 10;
        headerLeft.Add(_personalityLabel);

        header.Add(headerLeft);
        _root.Add(header);

        // Detail panel (swappable)
        _detailPanel = new VisualElement();
        _detailPanel.AddToClassList("employee-detail-modal__body");
        _detailPanel.style.flexGrow = 1;
        _detailPanel.style.flexDirection = FlexDirection.Column;
        _root.Add(_detailPanel);

        // Renewal panel (hidden by default)
        _renewalPanel = new VisualElement();
        _renewalPanel.AddToClassList("modal-body");
        _renewalPanel.AddToClassList("employee-detail-modal__body");
        _renewalPanel.style.display = DisplayStyle.None;
        _root.Add(_renewalPanel);

        BuildDetailPanel(_detailPanel);

        // Modal footer
        var footer = new VisualElement();
        footer.AddToClassList("modal-footer");

        _btnClose = new Button();
        _btnClose.text = "Close";
        _btnClose.AddToClassList("btn-ghost");
        footer.Add(_btnClose);
        _btnClose.clicked += OnCloseClicked;

        var footerRight = new VisualElement();
        footerRight.AddToClassList("flex-row");
        footerRight.style.flexGrow = 1;
        footerRight.style.justifyContent = Justify.FlexEnd;

        _btnOfferContract = new Button();
        _btnOfferContract.text = "Offer New Contract";
        _btnOfferContract.AddToClassList("btn-primary");
        footerRight.Add(_btnOfferContract);
        _btnOfferContract.clicked += OnOfferContractClicked;

        footer.Add(footerRight);
        _root.Add(footer);
    }

    public void Bind(IViewModel viewModel) {
        _vm = viewModel as EmployeeDetailModalViewModel;
        if (_vm == null) return;

        // Header
        if (_nameLabel != null)    _nameLabel.text = _vm.Name;
        if (_ageLabel != null)     _ageLabel.text  = "Age " + _vm.Age;
        if (_founderBadge != null) _founderBadge.style.display = _vm.IsFounder ? DisplayStyle.Flex : DisplayStyle.None;

        if (_roleLabel != null) {
            _roleLabel.text = _vm.RoleName;
            UIFormatting.ClearRolePillClasses(_roleLabel);
            _roleLabel.AddToClassList(_vm.RolePillClass);
        }

        if (_personalityLabel != null) {
            _personalityLabel.text = _vm.PersonalityText;
            _personalityLabel.RemoveFromClassList("personality--collaborative");
            _personalityLabel.RemoveFromClassList("personality--professional");
            _personalityLabel.RemoveFromClassList("personality--easygoing");
            _personalityLabel.RemoveFromClassList("personality--independent");
            _personalityLabel.RemoveFromClassList("personality--competitive");
            _personalityLabel.RemoveFromClassList("personality--perfectionist");
            _personalityLabel.RemoveFromClassList("personality--intense");
            _personalityLabel.RemoveFromClassList("personality--abrasive");
            _personalityLabel.RemoveFromClassList("personality--volatile");
            _personalityLabel.RemoveFromClassList("personality--unknown");
            _personalityLabel.AddToClassList(_vm.PersonalityClass);
        }

        // Role suitability rows
        var entries = _vm.RoleSuitabilities;
        int rowCount = _roleSuitRows.Count;
        for (int i = 0; i < rowCount && i < entries.Length; i++) {
            var row = _roleSuitRows[i];
            var entry = entries[i];

            var dot = row.Q<VisualElement>("suit-dot");
            if (dot != null) {
                dot.RemoveFromClassList("role-suit-dot--natural");
                dot.RemoveFromClassList("role-suit-dot--accomplished");
                dot.RemoveFromClassList("role-suit-dot--competent");
                dot.RemoveFromClassList("role-suit-dot--awkward");
                dot.RemoveFromClassList("role-suit-dot--unsuitable");
                dot.AddToClassList(MapSuitabilityToRoleSuitDot(entry.SuitabilityClass));
            }

            if (entry.RoleName == _vm.AssignedRoleName) {
                row.AddToClassList("role-suit-row--assigned");
            } else {
                row.RemoveFromClassList("role-suit-row--assigned");
            }

            var preferredEl = row.Q<Label>("suit-preferred");
            if (preferredEl != null)
                preferredEl.style.display = entry.IsPreferred ? DisplayStyle.Flex : DisplayStyle.None;
        }

        // Skills table
        var skillTable = _vm.SkillTable;
        int skillCount = _skillNameLabels.Count;
        for (int i = 0; i < skillCount && i < skillTable.Length; i++) {
            var nameLabel  = _skillNameLabels[i];
            var valueLabel = _skillValueLabels[i];
            var entry = skillTable[i];

            if (nameLabel != null)  nameLabel.text  = entry.Name;
            if (nameLabel != null)  nameLabel.style.color = new StyleColor(entry.NameColor);
            if (valueLabel != null) {
                valueLabel.text = entry.ValueText;
                valueLabel.RemoveFromClassList("skill-row__value--up");
                valueLabel.RemoveFromClassList("skill-row__value--down");
                if (!string.IsNullOrEmpty(entry.ValueClass))
                    valueLabel.AddToClassList(entry.ValueClass);
            }
        }

        // Contract
        if (_contractTypeLabel != null)   _contractTypeLabel.text   = _vm.ContractType;
        if (_contractLengthLabel != null) _contractLengthLabel.text = _vm.ContractLength;
        if (_salaryLabel != null)         _salaryLabel.text         = _vm.SalaryText;
        if (_remainingLabel != null)      _remainingLabel.text      = _vm.RemainingText;
        if (_hiredDateLabel != null)      _hiredDateLabel.text      = _vm.HiredDateText;
        if (_marketRateLabel != null)     _marketRateLabel.text     = _vm.MarketRateText;
        if (_valueEffLabel != null)       _valueEffLabel.text       = _vm.ValueEfficiencyText;

        if (_marketPositionLabel != null) {
            _marketPositionLabel.text = _vm.MarketPositionText;
            _marketPositionLabel.RemoveFromClassList("badge--danger");
            _marketPositionLabel.RemoveFromClassList("badge--warning");
            _marketPositionLabel.RemoveFromClassList("badge--neutral");
            _marketPositionLabel.RemoveFromClassList("badge--success");
            _marketPositionLabel.RemoveFromClassList("badge--accent");
            _marketPositionLabel.AddToClassList(_vm.MarketPositionClass);
        }

        // Escalation
        if (_escalationIndicator != null) {
            _escalationIndicator.EnableInClassList("escalation-indicator--hidden", !_vm.ShowEscalation);
            if (_vm.ShowEscalation && _escalationText != null) {
                _escalationText.text = _vm.EscalationText;
                _escalationText.RemoveFromClassList("badge--warning");
                _escalationText.RemoveFromClassList("badge--danger");
                _escalationText.RemoveFromClassList("badge--neutral");
                _escalationText.AddToClassList(_vm.EscalationClass);
            }
        }

        // Departure risk
        if (_departureRiskCard != null) {
            _departureRiskCard.style.display = _vm.ShowDepartureRisk ? DisplayStyle.Flex : DisplayStyle.None;
            if (_vm.ShowDepartureRisk && _departureRiskText != null)
                _departureRiskText.text = _vm.DepartureRiskText;
        }

        // Renewal section
        if (_renewalCard != null) {
            _renewalCard.style.display = _vm.ShowRenewalSection ? DisplayStyle.Flex : DisplayStyle.None;

            if (_vm.ShowRenewalSection) {
                if (_renewalCurrentSalaryLabel != null) _renewalCurrentSalaryLabel.text = _vm.CurrentSalaryText;
                if (_renewalDemandLabel != null)        _renewalDemandLabel.text        = _vm.RenewalDemandText;

                BindRenewalPatienceDots();

                if (_renewalCooldownLabel != null) {
                    _renewalCooldownLabel.style.display = _vm.IsOnCooldown ? DisplayStyle.Flex : DisplayStyle.None;
                    if (_vm.IsOnCooldown) _renewalCooldownLabel.text = "Frustrated — " + _vm.CooldownText;
                }

                if (_renewalCounterSummary != null) {
                    _renewalCounterSummary.style.display = _vm.HasPendingRenewalCounter ? DisplayStyle.Flex : DisplayStyle.None;
                    if (_vm.HasPendingRenewalCounter) {
                        if (_counterSalaryLabel != null) _counterSalaryLabel.text = _vm.CounterSalaryText;
                        if (_counterRoleLabel != null)   _counterRoleLabel.text   = _vm.CounterRoleName;
                        if (_counterTypeLabel != null)   _counterTypeLabel.text   = _vm.CounterTypeName;
                        if (_counterLengthLabel != null) _counterLengthLabel.text = _vm.CounterLengthText;
                    }
                }
            }
        }

        // Off-role indicator
        if (_offRoleCard != null) {
            _offRoleCard.style.display = _vm.IsWorkingOffRole ? DisplayStyle.Flex : DisplayStyle.None;
            if (_vm.IsWorkingOffRole && _offRoleLabel != null)
                _offRoleLabel.text = "Preferred: " + _vm.PreferredRoleName + " — assigned as " + _vm.AssignedRoleName;
        }

        // Status bar
        if (_statusTeamLabel != null) _statusTeamLabel.text = _vm.TeamName;
        if (_statusMoraleLabel != null) {
            _statusMoraleLabel.text = _vm.MoraleText;
            _statusMoraleLabel.RemoveFromClassList("text-success");
            _statusMoraleLabel.RemoveFromClassList("text-warning");
            _statusMoraleLabel.RemoveFromClassList("text-danger");
            _statusMoraleLabel.AddToClassList(_vm.MoraleClass);
        }
        if (_statusEnergyLabel != null) {
            _statusEnergyLabel.text = _vm.EnergyText;
            _statusEnergyLabel.RemoveFromClassList("energy-band--fresh");
            _statusEnergyLabel.RemoveFromClassList("energy-band--fit");
            _statusEnergyLabel.RemoveFromClassList("energy-band--tiring");
            _statusEnergyLabel.RemoveFromClassList("energy-band--drained");
            _statusEnergyLabel.RemoveFromClassList("energy-band--exhausted");
            _statusEnergyLabel.AddToClassList(_vm.EnergyClass);
        }

        // Offer contract button
        if (_btnOfferContract != null)
            _btnOfferContract.style.display = _vm.ShowOfferNewContract ? DisplayStyle.Flex : DisplayStyle.None;
    }

    public void Dispose() {
        if (_btnClose != null)         _btnClose.clicked         -= OnCloseClicked;
        if (_btnOfferContract != null) _btnOfferContract.clicked -= OnOfferContractClicked;

        _renewalView?.Dispose();
        _renewalView = null;
        _renewalVm = null;
        _vm = null;
        _root = null;
        _roleSuitRows.Clear();
        _skillNameLabels.Clear();
        _skillValueLabels.Clear();
        _renewalPatienceDots.Clear();
    }

    // ── Handlers ──────────────────────────────────────────────────────────────

    private void OnCloseClicked() {
        if (_detailPanel != null && _detailPanel.style.display == DisplayStyle.None) {
            ShowDetailPanel();
        } else {
            _modal.DismissModal();
        }
    }

    private void OnOfferContractClicked() {
        if (_vm == null) return;
        ShowRenewalPanel(_vm.CurrentEmployeeId);
    }

    private void OnRenewalCancelClicked() {
        ShowDetailPanel();
    }

    private void ShowDetailPanel() {
        if (_detailPanel != null) _detailPanel.style.display = DisplayStyle.Flex;
        if (_renewalPanel != null) _renewalPanel.style.display = DisplayStyle.None;
        if (_btnClose != null) _btnClose.text = "Close";
        if (_btnOfferContract != null && _vm != null)
            _btnOfferContract.style.display = _vm.ShowOfferNewContract ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private void ShowRenewalPanel(EmployeeId id) {
        if (_detailPanel != null) _detailPanel.style.display = DisplayStyle.None;
        if (_renewalPanel != null) {
            _renewalPanel.style.display = DisplayStyle.Flex;
            _renewalPanel.Clear();

            _renewalVm = new ContractRenewalModalViewModel();
            _renewalVm.SetEmployeeId(id);

            if (_vm?.LastState != null)
                _renewalVm.Refresh(_vm.LastState);

            _renewalView = new ContractRenewalModalView(_dispatcher, OnRenewalCancelClicked, OnRenewalConfirmed);
            _renewalView.Initialize(_renewalPanel);
            _renewalView.Bind(_renewalVm);
        }
        if (_btnClose != null) _btnClose.text = "\u2190 Back";
        if (_btnOfferContract != null) _btnOfferContract.style.display = DisplayStyle.None;
    }

    private void OnRenewalConfirmed() {
        _modal.DismissModal();
    }

    // ── Build ──────────────────────────────────────────────────────────────────

    private void BuildDetailPanel(VisualElement panel) {
        // Two-column body
        var body = new VisualElement();
        body.AddToClassList("modal-body--two-col");
        body.style.flexGrow = 1;
        panel.Add(body);

        // Left column
        var leftCol = new VisualElement();
        leftCol.AddToClassList("modal-col--left");
        body.Add(leftCol);

        var leftScroll = new ScrollView(ScrollViewMode.Vertical);
        leftScroll.style.flexGrow = 1;
        leftCol.Add(leftScroll);

        var leftContent = new VisualElement();
        leftContent.style.flexDirection = FlexDirection.Column;
        leftScroll.Add(leftContent);

        BuildRoleSuitabilityCard(leftContent);
        BuildContractCard(leftContent);

        // Right column
        var rightCol = new VisualElement();
        rightCol.AddToClassList("modal-col--right");
        body.Add(rightCol);

        var rightScroll = new ScrollView(ScrollViewMode.Vertical);
        rightScroll.style.flexGrow = 1;
        rightCol.Add(rightScroll);

        var rightContent = new VisualElement();
        rightContent.style.flexDirection = FlexDirection.Column;
        rightScroll.Add(rightContent);

        BuildSkillsTableCard(rightContent);
        BuildRenewalCard(rightContent);
        BuildOffRoleCard(rightContent);

        // Status bar
        BuildStatusBar(panel);
    }

    private void BuildRoleSuitabilityCard(VisualElement parent) {
        var card = BuildColCard(parent, "Role Suitability");
        var allRoles = RoleSuitabilityCalculator.AllRoles;
        int count = allRoles.Length;
        for (int i = 0; i < count; i++) {
            var row = new VisualElement();
            row.AddToClassList("role-suit-row");

            var dot = new VisualElement();
            dot.AddToClassList("role-suit-dot");
            dot.AddToClassList("role-suit-dot--unsuitable");
            dot.name = "suit-dot";
            row.Add(dot);

            var nameLabel = new Label(UIFormatting.FormatRole(allRoles[i]));
            nameLabel.AddToClassList("metric-secondary");
            row.Add(nameLabel);

            var preferredLabel = new Label("(preferred)");
            preferredLabel.AddToClassList("metric-tertiary");
            preferredLabel.style.marginLeft = 6;
            preferredLabel.style.display = DisplayStyle.None;
            preferredLabel.name = "suit-preferred";
            row.Add(preferredLabel);

            _roleSuitRows.Add(row);
            card.Add(row);
        }
    }

    private void BuildContractCard(VisualElement parent) {
        var card = BuildColCard(parent, "Contract");

        _hiredDateLabel = new Label("--");
        _hiredDateLabel.AddToClassList("metric-tertiary");
        _hiredDateLabel.style.marginBottom = 6;
        card.Add(_hiredDateLabel);

        _escalationIndicator = new VisualElement();
        _escalationIndicator.AddToClassList("escalation-indicator");
        _escalationIndicator.AddToClassList("escalation-indicator--hidden");
        _escalationText = new Label();
        _escalationText.AddToClassList("escalation-text");
        _escalationIndicator.Add(_escalationText);
        card.Add(_escalationIndicator);

        _departureRiskCard = new VisualElement();
        _departureRiskCard.AddToClassList("card--warning-left");
        _departureRiskCard.style.display = DisplayStyle.None;
        _departureRiskText = new Label();
        _departureRiskText.AddToClassList("metric-secondary");
        _departureRiskCard.Add(_departureRiskText);
        card.Add(_departureRiskCard);

        AddContractRow(card, "Type",             out _contractTypeLabel);
        AddContractRow(card, "Salary",           out _salaryLabel);
        AddContractRow(card, "Remaining",        out _remainingLabel);
        AddContractRow(card, "Length",           out _contractLengthLabel);
        AddContractRow(card, "Market Rate",      out _marketRateLabel);
        AddContractRow(card, "Value Efficiency", out _valueEffLabel);

        var posRow = new VisualElement();
        posRow.AddToClassList("flex-row");
        posRow.AddToClassList("justify-between");
        posRow.style.marginBottom = 4;
        var posLbl = new Label("Position");
        posLbl.AddToClassList("metric-tertiary");
        _marketPositionLabel = new Label();
        _marketPositionLabel.AddToClassList("badge");
        posRow.Add(posLbl);
        posRow.Add(_marketPositionLabel);
        card.Add(posRow);
    }

    private void BuildSkillsTableCard(VisualElement parent) {
        var card = BuildColCard(parent, "Skills");
        int skillCount = SkillTypeHelper.SkillTypeCount;
        for (int i = 0; i < skillCount; i++) {
            var row = new VisualElement();
            row.AddToClassList("skill-table-row");

            var nameLabel = new Label("--");
            nameLabel.AddToClassList("skill-table-row__name");
            row.Add(nameLabel);
            _skillNameLabels.Add(nameLabel);

            var valueLabel = new Label("--");
            valueLabel.AddToClassList("skill-table-row__value");
            row.Add(valueLabel);
            _skillValueLabels.Add(valueLabel);

            card.Add(row);
        }
    }

    private void BuildRenewalCard(VisualElement parent) {
        _renewalCard = new VisualElement();
        _renewalCard.AddToClassList("renewal-card");
        parent.Add(_renewalCard);

        var titleLabel = new Label("Renewal Negotiation");
        titleLabel.AddToClassList("renewal-card__title");
        _renewalCard.Add(titleLabel);

        // Demand vs current salary row
        var salaryCompareRow = new VisualElement();
        salaryCompareRow.AddToClassList("flex-row");
        salaryCompareRow.AddToClassList("justify-between");
        salaryCompareRow.style.marginBottom = 6;

        var currentSalaryGroup = new VisualElement();
        currentSalaryGroup.style.flexDirection = FlexDirection.Column;
        var currentSalaryLbl = new Label("Current");
        currentSalaryLbl.AddToClassList("metric-tertiary");
        currentSalaryGroup.Add(currentSalaryLbl);
        _renewalCurrentSalaryLabel = new Label("--");
        _renewalCurrentSalaryLabel.AddToClassList("metric-secondary");
        currentSalaryGroup.Add(_renewalCurrentSalaryLabel);
        salaryCompareRow.Add(currentSalaryGroup);

        var demandGroup = new VisualElement();
        demandGroup.style.flexDirection = FlexDirection.Column;
        demandGroup.style.alignItems = Align.FlexEnd;
        var demandLbl = new Label("Demand");
        demandLbl.AddToClassList("metric-tertiary");
        demandGroup.Add(demandLbl);
        _renewalDemandLabel = new Label("--");
        _renewalDemandLabel.AddToClassList("metric-secondary");
        demandGroup.Add(_renewalDemandLabel);
        salaryCompareRow.Add(demandGroup);

        _renewalCard.Add(salaryCompareRow);

        // Patience dots row
        var patienceRow = new VisualElement();
        patienceRow.AddToClassList("flex-row");
        patienceRow.style.alignItems = Align.Center;
        patienceRow.style.marginBottom = 6;
        var patienceLbl = new Label("Patience: ");
        patienceLbl.AddToClassList("metric-tertiary");
        patienceLbl.style.marginRight = 4;
        patienceRow.Add(patienceLbl);
        _patienceDotsContainer = new VisualElement();
        _patienceDotsContainer.AddToClassList("flex-row");
        patienceRow.Add(_patienceDotsContainer);
        _renewalCard.Add(patienceRow);

        // Cooldown label
        _renewalCooldownLabel = new Label();
        _renewalCooldownLabel.AddToClassList("renewal-cooldown-label");
        _renewalCooldownLabel.style.display = DisplayStyle.None;
        _renewalCard.Add(_renewalCooldownLabel);

        // Counter summary
        _renewalCounterSummary = new VisualElement();
        _renewalCounterSummary.AddToClassList("card--warning-left");
        _renewalCounterSummary.style.display = DisplayStyle.None;
        _renewalCounterSummary.style.marginTop = 6;

        var counterTitle = new Label("Counter-Offer Pending");
        counterTitle.AddToClassList("metric-tertiary");
        counterTitle.style.marginBottom = 4;
        _renewalCounterSummary.Add(counterTitle);

        AddCounterRow(_renewalCounterSummary, "Salary",   out _counterSalaryLabel);
        AddCounterRow(_renewalCounterSummary, "Role",     out _counterRoleLabel);
        AddCounterRow(_renewalCounterSummary, "Type",     out _counterTypeLabel);
        AddCounterRow(_renewalCounterSummary, "Length",   out _counterLengthLabel);

        _renewalCard.Add(_renewalCounterSummary);
    }

    private void BuildOffRoleCard(VisualElement parent) {
        _offRoleCard = new VisualElement();
        _offRoleCard.AddToClassList("off-role-card");
        _offRoleCard.style.display = DisplayStyle.None;

        _offRoleLabel = new Label();
        _offRoleLabel.AddToClassList("metric-secondary");
        _offRoleLabel.style.whiteSpace = WhiteSpace.Normal;
        _offRoleCard.Add(_offRoleLabel);

        parent.Add(_offRoleCard);
    }

    private void BuildStatusBar(VisualElement panel) {
        var bar = new VisualElement();
        bar.AddToClassList("employee-status-bar");
        panel.Add(bar);

        AddStatusBarItem(bar, "Team", out _statusTeamLabel);
        AddStatusBarItem(bar, "Morale", out _statusMoraleLabel);
        AddStatusBarItem(bar, "Energy", out _statusEnergyLabel);
    }

    // ── Bind helpers ──────────────────────────────────────────────────────────

    private void BindRenewalPatienceDots() {
        if (_patienceDotsContainer == null) return;
        int max     = _vm.RenewalMaxPatience;
        int current = _vm.RenewalCurrentPatience;

        while (_renewalPatienceDots.Count < max) {
            var dot = new VisualElement();
            dot.AddToClassList("renewal-patience-dot");
            _patienceDotsContainer.Add(dot);
            _renewalPatienceDots.Add(dot);
        }

        int dotCount = _renewalPatienceDots.Count;
        for (int i = 0; i < dotCount; i++)
            _renewalPatienceDots[i].style.display = i < max ? DisplayStyle.Flex : DisplayStyle.None;

        for (int i = 0; i < max; i++) {
            var dot = _renewalPatienceDots[i];
            dot.RemoveFromClassList("renewal-patience-dot--safe");
            dot.RemoveFromClassList("renewal-patience-dot--warning");
            dot.RemoveFromClassList("renewal-patience-dot--critical");
            dot.RemoveFromClassList("renewal-patience-dot--empty");

            if (i >= current) {
                dot.AddToClassList("renewal-patience-dot--empty");
            } else if (current >= 3) {
                dot.AddToClassList("renewal-patience-dot--safe");
            } else if (current == 2) {
                dot.AddToClassList("renewal-patience-dot--warning");
            } else {
                dot.AddToClassList("renewal-patience-dot--critical");
            }
        }
    }

    // ── Static build helpers ──────────────────────────────────────────────────

    private static VisualElement BuildColCard(VisualElement parent, string title) {
        var card = new VisualElement();
        card.AddToClassList("col-card");

        var titleLabel = new Label(title);
        titleLabel.AddToClassList("col-card__title");
        card.Add(titleLabel);

        parent.Add(card);
        return card;
    }

    private static void AddContractRow(VisualElement parent, string label, out Label valueLabel) {
        var row = new VisualElement();
        row.AddToClassList("flex-row");
        row.AddToClassList("justify-between");
        row.style.marginBottom = 4;

        var lbl = new Label(label);
        lbl.AddToClassList("metric-tertiary");
        row.Add(lbl);

        valueLabel = new Label("--");
        valueLabel.AddToClassList("metric-secondary");
        row.Add(valueLabel);

        parent.Add(row);
    }

    private static void AddCounterRow(VisualElement parent, string label, out Label valueLabel) {
        var row = new VisualElement();
        row.AddToClassList("flex-row");
        row.AddToClassList("justify-between");
        row.style.marginBottom = 3;

        var lbl = new Label(label);
        lbl.AddToClassList("metric-tertiary");
        row.Add(lbl);

        valueLabel = new Label("--");
        valueLabel.AddToClassList("metric-secondary");
        row.Add(valueLabel);

        parent.Add(row);
    }

    private static void AddStatusBarItem(VisualElement bar, string label, out Label valueLabel) {
        var item = new VisualElement();
        item.AddToClassList("status-bar__item");

        var lbl = new Label(label + ":");
        lbl.AddToClassList("status-bar__label");
        item.Add(lbl);

        valueLabel = new Label("--");
        valueLabel.AddToClassList("status-bar__value");
        item.Add(valueLabel);

        bar.Add(item);
    }

    // ── Utility ───────────────────────────────────────────────────────────────

    private static string MapSuitabilityToRoleSuitDot(string suitabilityClass) {
        if (suitabilityClass == null) return "role-suit-dot--unsuitable";
        if (suitabilityClass.Contains("natural"))      return "role-suit-dot--natural";
        if (suitabilityClass.Contains("accomplished")) return "role-suit-dot--accomplished";
        if (suitabilityClass.Contains("competent"))    return "role-suit-dot--competent";
        if (suitabilityClass.Contains("awkward"))      return "role-suit-dot--awkward";
        return "role-suit-dot--unsuitable";
    }
}
