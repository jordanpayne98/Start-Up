using System;
using UnityEngine.UIElements;

public class ContractRenewalModalView : IGameView
{
    private readonly ICommandDispatcher _dispatcher;
    private readonly Action _onCancel;
    private readonly Action _onConfirmed;

    private VisualElement _root;
    private ContractRenewalModalViewModel _vm;

    // Current terms
    private Label _currentTypeLabel;
    private Label _currentLengthLabel;
    private Label _currentSalaryLabel;
    private Label _currentRemainingLabel;

    // Request card
    private VisualElement _requestCard;
    private Label _requestText;
    private Label _requestConsequenceLabel;
    private Button _btnAcceptRequest;

    // Escalation warning
    private VisualElement _escalationCard;
    private Label _escalationWarningText;

    // Type tabs
    private Button _tabFT;
    private Button _tabPT;

    // Length tabs
    private Button _tabShort;
    private Button _tabStandard;
    private Button _tabLong;

    // Offer summary
    private Label _offerSalaryLabel;
    private Label _offerDeltaLabel;
    private Label _offerEffOutputLabel;
    private Label _offerValueEffLabel;
    private Label _offerMarketPosLabel;

    // Footer
    private Button _btnLetExpire;
    private Button _btnCancel;
    private Button _btnConfirm;

    public ContractRenewalModalView(ICommandDispatcher dispatcher, Action onCancel, Action onConfirmed) {
        _dispatcher  = dispatcher;
        _onCancel    = onCancel;
        _onConfirmed = onConfirmed;
    }

    public void Initialize(VisualElement root, UIServices services) {
        _root = root;
        _root.AddToClassList("contract-renewal-modal");

        // Header
        var header = new VisualElement();
        header.AddToClassList("renewal-header");
        var titleLabel = new Label("Offer New Contract");
        titleLabel.AddToClassList("renewal-title");
        header.Add(titleLabel);
        _root.Add(header);

        // Scrollable body
        var scroll = new ScrollView(ScrollViewMode.Vertical);
        scroll.style.flexGrow = 1;
        _root.Add(scroll);

        var body = new VisualElement();
        body.AddToClassList("contract-renewal-modal__body");
        scroll.Add(body);

        // Current terms card
        var currentCard = new VisualElement();
        currentCard.AddToClassList("card--info-left");
        currentCard.AddToClassList("contract-renewal-modal__card");
        var currentTitle = new Label("Current Terms");
        currentTitle.AddToClassList("terms-section-label");
        currentCard.Add(currentTitle);

        var currentGrid = new VisualElement();
        currentGrid.AddToClassList("terms-grid");
        AddTermsCell(currentGrid, "Type",      out _currentTypeLabel);
        AddTermsCell(currentGrid, "Length",    out _currentLengthLabel);
        AddTermsCell(currentGrid, "Salary",    out _currentSalaryLabel);
        AddTermsCell(currentGrid, "Remaining", out _currentRemainingLabel);
        currentCard.Add(currentGrid);
        body.Add(currentCard);

        // Employee request card (conditional)
        _requestCard = new VisualElement();
        _requestCard.AddToClassList("request-card");
        _requestCard.AddToClassList("contract-renewal-modal__card");
        _requestCard.style.display = DisplayStyle.None;

        var requestHeader = new VisualElement();
        requestHeader.AddToClassList("request-card__header");
        var requestTitle = new Label("Employee Request");
        requestTitle.AddToClassList("request-card__title");
        requestHeader.Add(requestTitle);
        _requestCard.Add(requestHeader);

        _requestText = new Label();
        _requestText.AddToClassList("metric-secondary");
        _requestCard.Add(_requestText);

        _requestConsequenceLabel = new Label();
        _requestConsequenceLabel.AddToClassList("metric-tertiary");
        _requestConsequenceLabel.style.marginTop = 4;
        _requestCard.Add(_requestConsequenceLabel);

        _btnAcceptRequest = new Button(OnAcceptRequestClicked) { text = "Accept Request" };
        _btnAcceptRequest.AddToClassList("btn-secondary");
        _btnAcceptRequest.AddToClassList("btn-sm");
        _btnAcceptRequest.AddToClassList("request-card__btn-accept");
        _requestCard.Add(_btnAcceptRequest);

        body.Add(_requestCard);

        // Escalation warning (conditional)
        _escalationCard = new VisualElement();
        _escalationCard.AddToClassList("escalation-indicator");
        _escalationCard.AddToClassList("escalation-indicator--hidden");
        _escalationCard.AddToClassList("contract-renewal-modal__card");
        _escalationWarningText = new Label();
        _escalationWarningText.AddToClassList("escalation-text");
        _escalationCard.Add(_escalationWarningText);
        body.Add(_escalationCard);

        // Offer builder section
        var offerLabel = new Label("Renewal Offer");
        offerLabel.AddToClassList("terms-section-label");
        offerLabel.style.marginTop = 12;
        body.Add(offerLabel);

        // Type selector
        var typeRow = new VisualElement();
        typeRow.AddToClassList("offer-tabs-row");

        var typeGroup = new VisualElement();
        typeGroup.AddToClassList("offer-tab-group");
        var typeGroupLabel = new Label("Type:");
        typeGroupLabel.AddToClassList("offer-tab-group__label");

        _tabFT = new Button(OnTabFTClicked) { text = "Full-Time" };
        _tabFT.AddToClassList("tab-bar__item");

        _tabPT = new Button(OnTabPTClicked) { text = "Part-Time" };
        _tabPT.AddToClassList("tab-bar__item");

        typeGroup.Add(typeGroupLabel);
        typeGroup.Add(_tabFT);
        typeGroup.Add(_tabPT);
        typeRow.Add(typeGroup);
        body.Add(typeRow);

        // Length selector
        var lenRow = new VisualElement();
        lenRow.AddToClassList("offer-tabs-row");

        var lenGroup = new VisualElement();
        lenGroup.AddToClassList("offer-tab-group");
        var lenGroupLabel = new Label("Length:");
        lenGroupLabel.AddToClassList("offer-tab-group__label");

        _tabShort    = new Button(OnTabShortClicked)    { text = "Short" };
        _tabShort.AddToClassList("tab-bar__item");
        _tabStandard = new Button(OnTabStandardClicked) { text = "Standard" };
        _tabStandard.AddToClassList("tab-bar__item");
        _tabLong     = new Button(OnTabLongClicked)     { text = "Long" };
        _tabLong.AddToClassList("tab-bar__item");

        lenGroup.Add(lenGroupLabel);
        lenGroup.Add(_tabShort);
        lenGroup.Add(_tabStandard);
        lenGroup.Add(_tabLong);
        lenRow.Add(lenGroup);
        body.Add(lenRow);

        // Offer summary card
        var summaryCard = new VisualElement();
        summaryCard.AddToClassList("card--accent-left");
        summaryCard.AddToClassList("contract-renewal-modal__card");
        summaryCard.AddToClassList("offer-summary");

        var salaryCol = new VisualElement();
        _offerSalaryLabel = new Label();
        _offerSalaryLabel.AddToClassList("offer-summary__salary");
        _offerDeltaLabel = new Label();
        _offerDeltaLabel.AddToClassList("metric-secondary");
        salaryCol.Add(_offerSalaryLabel);
        salaryCol.Add(_offerDeltaLabel);
        summaryCard.Add(salaryCol);

        var metaCol = new VisualElement();
        metaCol.AddToClassList("offer-summary__meta");
        _offerEffOutputLabel = new Label();
        _offerEffOutputLabel.AddToClassList("offer-summary__meta-row");
        _offerValueEffLabel = new Label();
        _offerValueEffLabel.AddToClassList("offer-summary__meta-row");
        _offerMarketPosLabel = new Label();
        _offerMarketPosLabel.AddToClassList("badge");
        metaCol.Add(_offerEffOutputLabel);
        metaCol.Add(_offerValueEffLabel);
        metaCol.Add(_offerMarketPosLabel);
        summaryCard.Add(metaCol);

        body.Add(summaryCard);

        // Footer
        var footer = new VisualElement();
        footer.AddToClassList("contract-renewal-modal__footer");

        _btnLetExpire = new Button(OnLetExpireClicked) { text = "Let Contract Expire" };
        _btnLetExpire.AddToClassList("btn-danger");
        _btnLetExpire.AddToClassList("btn-sm");
        footer.Add(_btnLetExpire);

        var footerRight = new VisualElement();
        footerRight.AddToClassList("flex-row");
        footerRight.style.flexGrow = 1;
        footerRight.style.justifyContent = Justify.FlexEnd;

        _btnCancel = new Button(OnCancelClicked) { text = "Cancel" };
        _btnCancel.AddToClassList("btn-ghost");
        _btnCancel.style.marginRight = 8;
        footerRight.Add(_btnCancel);

        _btnConfirm = new Button(OnConfirmClicked) { text = "Confirm Renewal" };
        _btnConfirm.AddToClassList("btn-primary");
        footerRight.Add(_btnConfirm);

        footer.Add(footerRight);
        _root.Add(footer);
    }

    public void Bind(IViewModel viewModel) {
        _vm = viewModel as ContractRenewalModalViewModel;
        if (_vm == null) return;

        // Current terms
        if (_currentTypeLabel != null)      _currentTypeLabel.text      = _vm.CurrentType;
        if (_currentLengthLabel != null)    _currentLengthLabel.text    = _vm.CurrentLength;
        if (_currentSalaryLabel != null)    _currentSalaryLabel.text    = _vm.CurrentSalary;
        if (_currentRemainingLabel != null) _currentRemainingLabel.text = _vm.CurrentRemaining;

        // Request card
        if (_requestCard != null) {
            _requestCard.style.display = _vm.HasRequest ? DisplayStyle.Flex : DisplayStyle.None;
            if (_vm.HasRequest) {
                if (_requestText != null) _requestText.text = _vm.RequestText;
                if (_requestConsequenceLabel != null) _requestConsequenceLabel.text = _vm.RequestConsequence;
            }
        }

        // Escalation
        if (_escalationCard != null) {
            _escalationCard.EnableInClassList("escalation-indicator--hidden", !_vm.ShowEscalationWarning);
            if (_vm.ShowEscalationWarning && _escalationWarningText != null) {
                _escalationWarningText.text = _vm.EscalationWarningText;
                _escalationWarningText.RemoveFromClassList("badge--warning");
                _escalationWarningText.RemoveFromClassList("badge--danger");
                _escalationWarningText.RemoveFromClassList("badge--neutral");
                _escalationWarningText.AddToClassList(_vm.EscalationWarningClass);
            }
        }

        // Type tabs
        SetTabActive(_tabFT, _vm.SelectedType == EmploymentType.FullTime);
        SetTabActive(_tabPT, _vm.SelectedType == EmploymentType.PartTime);

        // Length tabs
        SetTabActive(_tabShort,    _vm.SelectedLength == ContractLengthOption.Short);
        SetTabActive(_tabStandard, _vm.SelectedLength == ContractLengthOption.Standard);
        SetTabActive(_tabLong,     _vm.SelectedLength == ContractLengthOption.Long);

        // Offer summary
        if (_offerSalaryLabel != null) _offerSalaryLabel.text = _vm.OfferSalaryText;
        if (_offerDeltaLabel != null) {
            _offerDeltaLabel.text = _vm.SalaryDeltaText;
            _offerDeltaLabel.RemoveFromClassList("text-success");
            _offerDeltaLabel.RemoveFromClassList("text-danger");
            _offerDeltaLabel.RemoveFromClassList("text-muted");
            _offerDeltaLabel.AddToClassList(_vm.SalaryDeltaClass);
        }
        if (_offerEffOutputLabel != null) _offerEffOutputLabel.text = "Output: " + _vm.OfferEffOutputText;
        if (_offerValueEffLabel != null)  _offerValueEffLabel.text  = "Value: " + _vm.OfferValueEffText;
        if (_offerMarketPosLabel != null) {
            _offerMarketPosLabel.text = _vm.MarketPositionText;
            _offerMarketPosLabel.RemoveFromClassList("badge--danger");
            _offerMarketPosLabel.RemoveFromClassList("badge--warning");
            _offerMarketPosLabel.RemoveFromClassList("badge--neutral");
            _offerMarketPosLabel.RemoveFromClassList("badge--success");
            _offerMarketPosLabel.RemoveFromClassList("badge--accent");
            _offerMarketPosLabel.AddToClassList(_vm.MarketPositionClass);
        }

        // Buttons
        if (_btnLetExpire != null) _btnLetExpire.style.display = _vm.IsFounder ? DisplayStyle.None : DisplayStyle.Flex;
        if (_btnConfirm != null) _btnConfirm.SetEnabled(_vm.CanConfirm);
    }

    public void Dispose() {
        if (_btnAcceptRequest != null) _btnAcceptRequest.clicked -= OnAcceptRequestClicked;
        if (_tabFT != null)       _tabFT.clicked       -= OnTabFTClicked;
        if (_tabPT != null)       _tabPT.clicked       -= OnTabPTClicked;
        if (_tabShort != null)    _tabShort.clicked    -= OnTabShortClicked;
        if (_tabStandard != null) _tabStandard.clicked -= OnTabStandardClicked;
        if (_tabLong != null)     _tabLong.clicked     -= OnTabLongClicked;
        if (_btnLetExpire != null) _btnLetExpire.clicked -= OnLetExpireClicked;
        if (_btnCancel != null)   _btnCancel.clicked   -= OnCancelClicked;
        if (_btnConfirm != null)  _btnConfirm.clicked  -= OnConfirmClicked;

        _vm = null;
        _root = null;
    }

    // ── Handlers ──────────────────────────────────────────────────────────────

    private void OnAcceptRequestClicked() {
        if (_vm == null) return;
        _vm.AcceptRequest();
        Bind(_vm);
    }

    private void OnTabFTClicked() {
        if (_vm == null) return;
        _vm.SetSelectedType(EmploymentType.FullTime);
        Bind(_vm);
    }

    private void OnTabPTClicked() {
        if (_vm == null) return;
        _vm.SetSelectedType(EmploymentType.PartTime);
        Bind(_vm);
    }

    private void OnTabShortClicked() {
        if (_vm == null) return;
        _vm.SetSelectedLength(ContractLengthOption.Short);
        Bind(_vm);
    }

    private void OnTabStandardClicked() {
        if (_vm == null) return;
        _vm.SetSelectedLength(ContractLengthOption.Standard);
        Bind(_vm);
    }

    private void OnTabLongClicked() {
        if (_vm == null) return;
        _vm.SetSelectedLength(ContractLengthOption.Long);
        Bind(_vm);
    }

    private void OnLetExpireClicked() {
        if (_vm == null) return;
        _dispatcher.Dispatch(new DeclineRenewalCommand {
            Tick = _dispatcher.CurrentTick,
            EmployeeId = _vm.EmployeeId
        });
        _onConfirmed?.Invoke();
    }

    private void OnCancelClicked() {
        _onCancel?.Invoke();
    }

    private void OnConfirmClicked() {
        if (_vm == null || !_vm.CanConfirm) return;
        _dispatcher.Dispatch(new RenewContractCommand {
            Tick = _dispatcher.CurrentTick,
            EmployeeId = _vm.EmployeeId,
            NewType = _vm.SelectedType,
            NewLength = _vm.SelectedLength,
            AcceptsRequest = _vm.HasRequest
        });
        _onConfirmed?.Invoke();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static void SetTabActive(Button btn, bool active) {
        if (btn == null) return;
        btn.EnableInClassList("tab-bar__item--active", active);
    }

    private static void AddTermsCell(VisualElement grid, string label, out Label valueLabel) {
        var cell = new VisualElement();
        cell.AddToClassList("terms-cell");

        var lbl = new Label(label);
        lbl.AddToClassList("terms-cell__label");
        cell.Add(lbl);

        valueLabel = new Label("--");
        valueLabel.AddToClassList("terms-cell__value");
        cell.Add(valueLabel);

        grid.Add(cell);
    }
}
