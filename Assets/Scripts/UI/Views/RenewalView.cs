using System.Collections.Generic;
using UnityEngine.UIElements;

public class RenewalView : IGameView
{
    private readonly ICommandDispatcher _dispatcher;
    private readonly IModalPresenter _modal;

    private VisualElement _root;
    private RenewalViewModel _vm;

    // Header
    private Label _countLabel;
    private Button _btnClose;

    // Scroll + list
    private ScrollView _scroll;
    private VisualElement _listContainer;
    private ElementPool _rowPool;

    // Footer
    private Button _btnDone;

    // Per-expanded-row offer card references (cached on first expand)
    private VisualElement _expandedOfferCard;

    // Offer card sub-elements
    private Label _escalationText;
    private VisualElement _escalationIndicator;
    private Label _currentSalaryCell;
    private Label _currentTypeCell;
    private Label _currentLengthCell;
    private Label _hiredDateCell;
    private VisualElement _requestCard;
    private Label _requestTypeLabel;
    private Label _requestLengthLabel;
    private Button _btnAcceptRequest;
    private Button _tabTypeFT;
    private Button _tabTypePT;
    private Button _tabLenShort;
    private Button _tabLenStandard;
    private Button _tabLenLong;
    private Label _offerSalary;
    private Label _offerEffOutput;
    private Label _offerValueEff;
    private Label _offerMarketPos;
    private Button _btnRenew;

    private bool _initialized;

    public RenewalView(ICommandDispatcher dispatcher, IModalPresenter modal) {
        _dispatcher = dispatcher;
        _modal      = modal;
    }

    public void Initialize(VisualElement root) {
        if (_initialized) return;
        _initialized = true;
        _root = root;

        _countLabel    = _root.Q<Label>("renewal-count");
        _btnClose      = _root.Q<Button>("btn-close");
        _scroll        = _root.Q<ScrollView>("renewal-scroll");
        _listContainer = _root.Q<VisualElement>("renewal-list");
        _btnDone       = _root.Q<Button>("btn-done");

        _rowPool = new ElementPool(CreateRenewalRow, _listContainer);

        if (_btnClose != null) _btnClose.clicked += OnCloseClicked;
        if (_btnDone  != null) _btnDone.clicked  += OnCloseClicked;
    }

    public void Bind(IViewModel viewModel) {
        _vm = viewModel as RenewalViewModel;
        if (_vm == null) return;

        if (_countLabel != null) {
            int pending = _vm.PendingCount;
            _countLabel.text = pending + (pending == 1 ? " pending" : " pending");
            SetExclusiveClass(_countLabel, pending > 0 ? "badge--warning" : "badge--neutral",
                "badge--warning", "badge--neutral", "badge--success");
        }

        _rowPool.UpdateList(_vm.Rows, BindRenewalRow);

        BindExpandedOfferCard();
    }

    private void BindRenewalRow(VisualElement el, RenewalRowDisplay data) {
        el.userData = data.Id;

        var collapsed = el.Q<VisualElement>("row-collapsed");
        if (collapsed != null) {
            collapsed.EnableInClassList("renewal-row--resolved", data.IsResolved);
        }

        if (data.IsResolved) el.AddToClassList("renewal-row--resolved");
        else el.RemoveFromClassList("renewal-row--resolved");

        var nameLabel = el.Q<Label>("row-name");
        if (nameLabel != null) nameLabel.text = data.Name;

        var roleLabel = el.Q<Label>("row-role");
        if (roleLabel != null) roleLabel.text = data.RoleDisplay;

        var arrangeBadge = el.Q<Label>("row-arrange");
        if (arrangeBadge != null) {
            arrangeBadge.text = data.ArrangementBadge;
            SetBadgeClass(arrangeBadge, data.ArrangementClass);
        }

        var urgencyChip = el.Q<Label>("row-urgency");
        if (urgencyChip != null) {
            urgencyChip.text = data.UrgencyDisplay;
            SetBadgeClass(urgencyChip, data.UrgencyClass);
        }

        var requestChip = el.Q<Label>("row-request");
        if (requestChip != null) {
            requestChip.style.display = data.HasRequest ? DisplayStyle.Flex : DisplayStyle.None;
            if (data.HasRequest) {
                requestChip.text = data.RequestChipText;
                SetBadgeClass(requestChip, data.RequestChipClass);
            }
        }

        var salaryLabel = el.Q<Label>("row-salary");
        if (salaryLabel != null) salaryLabel.text = data.SalaryDisplay;

        var expiryLabel = el.Q<Label>("row-expiry");
        if (expiryLabel != null) {
            expiryLabel.text = data.ExpiryDisplay;
            SetExclusiveClass(expiryLabel, data.ExpiryClass, "text-danger", "text-warning", "text-muted");
        }

        var resolvedRow = el.Q<VisualElement>("row-resolved-row");
        if (resolvedRow != null) {
            resolvedRow.style.display = data.IsResolved ? DisplayStyle.Flex : DisplayStyle.None;
            if (data.IsResolved) {
                var resolvedBadge = resolvedRow.Q<Label>("row-resolved-badge");
                if (resolvedBadge != null) {
                    resolvedBadge.text = data.ResolvedBadge;
                    SetBadgeClass(resolvedBadge, data.ResolvedClass);
                }
            }
        }

        bool isExpanded = _vm != null && _vm.ExpandedIndex >= 0
            && _vm.ExpandedIndex < _vm.Rows.Count
            && _vm.Rows[_vm.ExpandedIndex].Id.Equals(data.Id);

        var chevron = el.Q<Label>("row-chevron");
        if (chevron != null) {
            chevron.text = isExpanded ? "▲" : "▼";
            chevron.style.display = (data.IsResolved || data.StrikeCount >= 3) ? DisplayStyle.None : DisplayStyle.Flex;
        }

        var offerCard = el.Q<VisualElement>("row-offer-card");
        if (offerCard != null) {
            bool showCard = isExpanded && !data.IsResolved && data.StrikeCount < 3;
            offerCard.EnableInClassList("renewal-offer-card--visible", showCard);
            if (showCard) {
                _expandedOfferCard = offerCard;
                CacheOfferCardRefs(offerCard);
            }
        }
    }

    private void BindExpandedOfferCard() {
        if (_vm == null || _vm.ExpandedIndex < 0 || _vm.ExpandedIndex >= _vm.Rows.Count) return;
        if (_expandedOfferCard == null) return;

        var offer = _vm.ExpandedOffer;

        if (_escalationIndicator != null) {
            _escalationIndicator.EnableInClassList("escalation-indicator--hidden", !offer.ShowEscalation);
            if (offer.ShowEscalation && _escalationText != null) {
                _escalationText.text = offer.EscalationDisplay;
                SetBadgeClass(_escalationText, offer.EscalationClass);
            }
        }

        if (_currentSalaryCell != null) _currentSalaryCell.text = offer.CurrentSalaryDisplay;
        if (_currentTypeCell   != null) _currentTypeCell.text   = offer.CurrentTypeDisplay;
        if (_currentLengthCell != null) _currentLengthCell.text = offer.CurrentLengthDisplay;
        if (_hiredDateCell     != null) _hiredDateCell.text     = offer.HiredDateDisplay;

        if (_requestCard != null) {
            _requestCard.EnableInClassList("request-card--hidden", !offer.HasRequest);
            if (offer.HasRequest) {
                if (_requestTypeLabel  != null) _requestTypeLabel.text  = offer.RequestTypeDisplay;
                if (_requestLengthLabel != null) _requestLengthLabel.text = offer.RequestLengthDisplay;
            }
        }
        if (_btnAcceptRequest != null) {
            _btnAcceptRequest.style.display = offer.CanAcceptRequest ? DisplayStyle.Flex : DisplayStyle.None;
        }

        bool isFT = offer.SelectedType == EmploymentType.FullTime;
        SetTabActive(_tabTypeFT, isFT);
        SetTabActive(_tabTypePT, !isFT);

        SetTabActive(_tabLenShort,    offer.SelectedLength == ContractLengthOption.Short);
        SetTabActive(_tabLenStandard, offer.SelectedLength == ContractLengthOption.Standard);
        SetTabActive(_tabLenLong,     offer.SelectedLength == ContractLengthOption.Long);

        if (_offerSalary    != null) _offerSalary.text    = offer.OfferSalaryDisplay;
        if (_offerEffOutput != null) _offerEffOutput.text = offer.EffOutputDisplay;
        if (_offerValueEff  != null) {
            _offerValueEff.text = offer.ValueEffDisplay;
            SetExclusiveClass(_offerValueEff, "", "text-success", "text-warning", "text-danger");
        }
        if (_offerMarketPos != null) {
            SetBadgeClass(_offerMarketPos, offer.MarketLabelClass, offer.MarketLabelDisplay);
        }

        if (_btnRenew != null) {
            _btnRenew.SetEnabled(offer.CanRenew);
        }
    }

    public void Dispose() {
        if (_btnClose != null) _btnClose.clicked -= OnCloseClicked;
        if (_btnDone  != null) _btnDone.clicked  -= OnCloseClicked;

        UnwireOfferCardHandlers();

        _vm                  = null;
        _rowPool             = null;
        _expandedOfferCard   = null;
        _root                = null;
        _listContainer       = null;
        _initialized         = false;
    }

    // ── Factory ──────────────────────────────────────────────────────────────────

    private VisualElement CreateRenewalRow() {
        var row = new VisualElement();
        row.AddToClassList("renewal-row");

        // Collapsed summary
        var collapsed = new VisualElement();
        collapsed.name = "row-collapsed";
        collapsed.AddToClassList("renewal-row__collapsed");

        var nameCol = new VisualElement();
        nameCol.AddToClassList("renewal-row__name-col");
        var nameLabel = new Label();
        nameLabel.name = "row-name";
        nameLabel.AddToClassList("renewal-row__name");
        var roleLabel = new Label();
        roleLabel.name = "row-role";
        roleLabel.AddToClassList("renewal-row__role");
        nameCol.Add(nameLabel);
        nameCol.Add(roleLabel);

        var badges = new VisualElement();
        badges.AddToClassList("renewal-row__badges");

        var arrangeBadge = new Label();
        arrangeBadge.name = "row-arrange";
        arrangeBadge.AddToClassList("badge");
        arrangeBadge.AddToClassList("renewal-row__badge-gap");

        var urgencyChip = new Label();
        urgencyChip.name = "row-urgency";
        urgencyChip.AddToClassList("badge");
        urgencyChip.AddToClassList("renewal-row__badge-gap");

        var requestChip = new Label();
        requestChip.name = "row-request";
        requestChip.AddToClassList("badge");
        requestChip.AddToClassList("renewal-row__badge-gap");
        requestChip.style.display = DisplayStyle.None;

        badges.Add(arrangeBadge);
        badges.Add(urgencyChip);
        badges.Add(requestChip);

        var salaryCol = new VisualElement();
        salaryCol.AddToClassList("renewal-row__salary-col");
        var salaryLabel = new Label();
        salaryLabel.name = "row-salary";
        salaryLabel.AddToClassList("renewal-row__salary");
        var expiryLabel = new Label();
        expiryLabel.name = "row-expiry";
        expiryLabel.AddToClassList("font-xs");
        salaryCol.Add(salaryLabel);
        salaryCol.Add(expiryLabel);

        var chevron = new Label("▼");
        chevron.name = "row-chevron";
        chevron.AddToClassList("renewal-row__chevron");

        collapsed.Add(nameCol);
        collapsed.Add(badges);
        collapsed.Add(salaryCol);
        collapsed.Add(chevron);

        // Resolved badge row
        var resolvedRow = new VisualElement();
        resolvedRow.name = "row-resolved-row";
        resolvedRow.AddToClassList("resolved-badge-row");
        resolvedRow.style.display = DisplayStyle.None;
        var resolvedBadge = new Label();
        resolvedBadge.name = "row-resolved-badge";
        resolvedBadge.AddToClassList("badge");
        resolvedRow.Add(resolvedBadge);

        // Offer card (hidden by default)
        var offerCard = CreateOfferCard();
        offerCard.name = "row-offer-card";

        row.Add(collapsed);
        row.Add(resolvedRow);
        row.Add(offerCard);

        collapsed.RegisterCallback<ClickEvent>(OnRowCollapsedClicked);

        return row;
    }

    private VisualElement CreateOfferCard() {
        var card = new VisualElement();
        card.AddToClassList("renewal-offer-card");

        // Escalation
        var escalation = new VisualElement();
        escalation.name = "offer-escalation";
        escalation.AddToClassList("escalation-indicator");
        escalation.AddToClassList("escalation-indicator--hidden");
        var escalationText = new Label();
        escalationText.name = "offer-escalation-text";
        escalationText.AddToClassList("escalation-text");
        escalation.Add(escalationText);

        // Current terms label
        var currentLabel = new Label("Current Contract");
        currentLabel.AddToClassList("terms-section-label");

        // Terms grid
        var termsGrid = new VisualElement();
        termsGrid.AddToClassList("terms-grid");
        termsGrid.Add(MakeTermsCell("offer-current-salary", "Current Salary"));
        termsGrid.Add(MakeTermsCell("offer-current-type", "Type"));
        termsGrid.Add(MakeTermsCell("offer-current-length", "Length"));
        termsGrid.Add(MakeTermsCell("offer-hired-date", "Hired"));

        // Request card
        var requestCard = new VisualElement();
        requestCard.name = "offer-request-card";
        requestCard.AddToClassList("request-card");
        requestCard.AddToClassList("request-card--hidden");
        var requestHeader = new VisualElement();
        requestHeader.AddToClassList("request-card__header");
        var requestTitle = new Label("Employee Request");
        requestTitle.AddToClassList("request-card__title");
        requestHeader.Add(requestTitle);
        var requestDetails = new VisualElement();
        requestDetails.AddToClassList("flex-row");
        var requestTypeLabel = new Label();
        requestTypeLabel.name = "offer-request-type";
        requestTypeLabel.AddToClassList("badge");
        requestTypeLabel.AddToClassList("badge--info");
        requestTypeLabel.AddToClassList("renewal-row__badge-gap");
        var requestLengthLabel = new Label();
        requestLengthLabel.name = "offer-request-length";
        requestLengthLabel.AddToClassList("badge");
        requestLengthLabel.AddToClassList("badge--info");
        requestDetails.Add(requestTypeLabel);
        requestDetails.Add(requestLengthLabel);
        var btnAccept = new Button();
        btnAccept.name = "offer-btn-accept-request";
        btnAccept.text = "Accept Request";
        btnAccept.AddToClassList("btn-secondary");
        btnAccept.AddToClassList("btn-sm");
        btnAccept.AddToClassList("request-card__btn-accept");
        requestCard.Add(requestHeader);
        requestCard.Add(requestDetails);
        requestCard.Add(btnAccept);

        // Offer builder label
        var offerLabel = new Label("Renewal Offer");
        offerLabel.AddToClassList("terms-section-label");

        // Type tabs
        var tabsRow = new VisualElement();
        tabsRow.AddToClassList("offer-tabs-row");

        var typeTabGroup = new VisualElement();
        typeTabGroup.AddToClassList("offer-tab-group");
        var typeLabel = new Label("Type:");
        typeLabel.AddToClassList("offer-tab-group__label");
        var tabFT = new Button { text = "Full-Time" };
        tabFT.name = "offer-tab-ft";
        tabFT.AddToClassList("tab-bar__item");
        var tabPT = new Button { text = "Part-Time" };
        tabPT.name = "offer-tab-pt";
        tabPT.AddToClassList("tab-bar__item");
        typeTabGroup.Add(typeLabel);
        typeTabGroup.Add(tabFT);
        typeTabGroup.Add(tabPT);

        var lenTabGroup = new VisualElement();
        lenTabGroup.AddToClassList("offer-tab-group");
        var lenLabel = new Label("Length:");
        lenLabel.AddToClassList("offer-tab-group__label");
        var tabShort = new Button { text = "Short" };
        tabShort.name = "offer-tab-short";
        tabShort.AddToClassList("tab-bar__item");
        var tabStandard = new Button { text = "Standard" };
        tabStandard.name = "offer-tab-standard";
        tabStandard.AddToClassList("tab-bar__item");
        var tabLong = new Button { text = "Long" };
        tabLong.name = "offer-tab-long";
        tabLong.AddToClassList("tab-bar__item");
        lenTabGroup.Add(lenLabel);
        lenTabGroup.Add(tabShort);
        lenTabGroup.Add(tabStandard);
        lenTabGroup.Add(tabLong);

        tabsRow.Add(typeTabGroup);
        tabsRow.Add(lenTabGroup);

        // Offer summary
        var summary = new VisualElement();
        summary.AddToClassList("offer-summary");
        var offerSalary = new Label();
        offerSalary.name = "offer-salary";
        offerSalary.AddToClassList("offer-summary__salary");
        var summaryMeta = new VisualElement();
        summaryMeta.AddToClassList("offer-summary__meta");
        var offerEff = new Label();
        offerEff.name = "offer-eff-output";
        offerEff.AddToClassList("offer-summary__meta-row");
        offerEff.AddToClassList("text-muted");
        var offerValueEff = new Label();
        offerValueEff.name = "offer-value-eff";
        offerValueEff.AddToClassList("offer-summary__meta-row");
        var offerMarket = new Label();
        offerMarket.name = "offer-market-pos";
        offerMarket.AddToClassList("badge");
        summaryMeta.Add(offerEff);
        summaryMeta.Add(offerValueEff);
        summaryMeta.Add(offerMarket);
        summary.Add(offerSalary);
        summary.Add(summaryMeta);

        // Actions
        var actions = new VisualElement();
        actions.AddToClassList("offer-actions");
        var btnRenew = new Button { text = "Renew Contract" };
        btnRenew.name = "offer-btn-renew";
        btnRenew.AddToClassList("btn-primary");
        btnRenew.AddToClassList("offer-actions__btn");
        actions.Add(btnRenew);

        card.Add(escalation);
        card.Add(currentLabel);
        card.Add(termsGrid);
        card.Add(requestCard);
        card.Add(offerLabel);
        card.Add(tabsRow);
        card.Add(summary);
        card.Add(actions);

        tabFT.clicked      += OnTabFTClicked;
        tabPT.clicked      += OnTabPTClicked;
        tabShort.clicked   += OnTabShortClicked;
        tabStandard.clicked += OnTabStandardClicked;
        tabLong.clicked    += OnTabLongClicked;
        btnAccept.clicked  += OnAcceptRequestClicked;
        btnRenew.clicked   += OnRenewClicked;

        return card;
    }

    private static VisualElement MakeTermsCell(string valueName, string labelText) {
        var cell = new VisualElement();
        cell.AddToClassList("terms-cell");
        var lbl = new Label(labelText);
        lbl.AddToClassList("terms-cell__label");
        var val = new Label("—");
        val.name = valueName;
        val.AddToClassList("terms-cell__value");
        cell.Add(lbl);
        cell.Add(val);
        return cell;
    }

    // ── Cache offer card refs for the currently expanded card ─────────────────

    private void CacheOfferCardRefs(VisualElement card) {
        _escalationIndicator = card.Q<VisualElement>("offer-escalation");
        _escalationText      = card.Q<Label>("offer-escalation-text");
        _currentSalaryCell   = card.Q<Label>("offer-current-salary");
        _currentTypeCell     = card.Q<Label>("offer-current-type");
        _currentLengthCell   = card.Q<Label>("offer-current-length");
        _hiredDateCell       = card.Q<Label>("offer-hired-date");
        _requestCard         = card.Q<VisualElement>("offer-request-card");
        _requestTypeLabel    = card.Q<Label>("offer-request-type");
        _requestLengthLabel  = card.Q<Label>("offer-request-length");
        _btnAcceptRequest    = card.Q<Button>("offer-btn-accept-request");
        _tabTypeFT           = card.Q<Button>("offer-tab-ft");
        _tabTypePT           = card.Q<Button>("offer-tab-pt");
        _tabLenShort         = card.Q<Button>("offer-tab-short");
        _tabLenStandard      = card.Q<Button>("offer-tab-standard");
        _tabLenLong          = card.Q<Button>("offer-tab-long");
        _offerSalary         = card.Q<Label>("offer-salary");
        _offerEffOutput      = card.Q<Label>("offer-eff-output");
        _offerValueEff       = card.Q<Label>("offer-value-eff");
        _offerMarketPos      = card.Q<Label>("offer-market-pos");
        _btnRenew            = card.Q<Button>("offer-btn-renew");
    }

    private void UnwireOfferCardHandlers() {
        if (_tabTypeFT      != null) _tabTypeFT.clicked      -= OnTabFTClicked;
        if (_tabTypePT      != null) _tabTypePT.clicked      -= OnTabPTClicked;
        if (_tabLenShort    != null) _tabLenShort.clicked    -= OnTabShortClicked;
        if (_tabLenStandard != null) _tabLenStandard.clicked -= OnTabStandardClicked;
        if (_tabLenLong     != null) _tabLenLong.clicked     -= OnTabLongClicked;
        if (_btnAcceptRequest != null) _btnAcceptRequest.clicked -= OnAcceptRequestClicked;
        if (_btnRenew       != null) _btnRenew.clicked       -= OnRenewClicked;
    }

    // ── Click handlers ───────────────────────────────────────────────────────────

    private void OnCloseClicked() {
        _modal?.DismissModal();
    }

    private void OnRowCollapsedClicked(ClickEvent evt) {
        if (_vm == null) return;
        var el = evt.currentTarget as VisualElement;
        if (el == null) return;
        var row = el.parent;
        if (row == null || row.userData == null) return;

        EmployeeId id = (EmployeeId)row.userData;
        int rowIndex = FindRowIndex(id);
        if (rowIndex < 0) return;

        if (_vm.ExpandedIndex == rowIndex) {
            _vm.ExpandRow(-1);
            _expandedOfferCard = null;
        } else {
            _expandedOfferCard = null;
            _vm.ExpandRow(rowIndex);
        }

        Bind(_vm);
    }

    private void OnTabFTClicked() {
        if (_vm == null) return;
        _vm.SetOfferType(EmploymentType.FullTime);
        BindExpandedOfferCard();
    }

    private void OnTabPTClicked() {
        if (_vm == null) return;
        _vm.SetOfferType(EmploymentType.PartTime);
        BindExpandedOfferCard();
    }

    private void OnTabShortClicked() {
        if (_vm == null) return;
        _vm.SetOfferLength(ContractLengthOption.Short);
        BindExpandedOfferCard();
    }

    private void OnTabStandardClicked() {
        if (_vm == null) return;
        _vm.SetOfferLength(ContractLengthOption.Standard);
        BindExpandedOfferCard();
    }

    private void OnTabLongClicked() {
        if (_vm == null) return;
        _vm.SetOfferLength(ContractLengthOption.Long);
        BindExpandedOfferCard();
    }

    private void OnAcceptRequestClicked() {
        if (_vm == null) return;
        _vm.AcceptRequest();
        BindExpandedOfferCard();
    }

    private void OnRenewClicked() {
        if (_vm == null || _vm.ExpandedIndex < 0 || _vm.ExpandedIndex >= _vm.Rows.Count) return;
        var row = _vm.Rows[_vm.ExpandedIndex];
        var offer = _vm.ExpandedOffer;
        if (!offer.CanRenew) return;

        _dispatcher.Dispatch(new RenewContractCommand {
            Tick           = _dispatcher.CurrentTick,
            EmployeeId     = row.Id,
            NewType        = offer.SelectedType,
            NewLength      = offer.SelectedLength,
            AcceptsRequest = offer.CanAcceptRequest
        });
    }

    // ── Helpers ──────────────────────────────────────────────────────────────────

    private int FindRowIndex(EmployeeId id) {
        if (_vm == null) return -1;
        int count = _vm.Rows.Count;
        for (int i = 0; i < count; i++) {
            if (_vm.Rows[i].Id.Equals(id)) return i;
        }
        return -1;
    }

    private static void SetTabActive(Button tab, bool active) {
        if (tab == null) return;
        if (active) tab.AddToClassList("tab-bar__item--active");
        else        tab.RemoveFromClassList("tab-bar__item--active");
    }

    private static void SetBadgeClass(Label label, string cssClass, string text = null) {
        if (label == null) return;
        if (text != null) label.text = text;
        string[] badgeClasses = { "badge--success", "badge--warning", "badge--danger", "badge--neutral",
                                  "badge--accent", "badge--info", "badge--primary",
                                  "chip--urgent", "chip--expiring", "chip--new" };
        int len = badgeClasses.Length;
        for (int i = 0; i < len; i++) label.RemoveFromClassList(badgeClasses[i]);
        if (!string.IsNullOrEmpty(cssClass)) label.AddToClassList(cssClass);
    }

    private static void SetExclusiveClass(VisualElement el, string active, params string[] all) {
        if (el == null) return;
        int len = all.Length;
        for (int i = 0; i < len; i++) el.RemoveFromClassList(all[i]);
        if (!string.IsNullOrEmpty(active)) el.AddToClassList(active);
    }
}
