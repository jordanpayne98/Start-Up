using System.Collections.Generic;
using UnityEngine.UIElements;

public class CandidateMarketView : IGameView
{
    private readonly ICommandDispatcher _dispatcher;
    private readonly IModalPresenter _modal;
    private readonly INavigationService _nav;
    private readonly ITooltipProvider _tooltip;

    private VisualElement _root;
    private CandidateMarketViewModel _vm;

    // Header
    private Button _backButton;

    // Tab buttons
    private Button _tabMarket;
    private Button _tabSourced;
    private Button _tabShortlist;

    // Pool-info-bar
    private Label _capacityLabel;
    private Label _capacityWarning;
    private Label _refreshLabel;

    // Candidate list
    private VisualElement _candidateListContainer;
    private ElementPool _candidatePool;

    // Active tab index: 0 = Market, 1 = Sourced, 2 = Shortlist
    private int _activeTab;

    private static readonly string[] TabActiveClass = { "tab--active" };

    public CandidateMarketView(ICommandDispatcher dispatcher, IModalPresenter modal, INavigationService nav, ITooltipProvider tooltip) {
        _dispatcher = dispatcher;
        _modal = modal;
        _nav = nav;
        _tooltip = tooltip;
    }

    public void Initialize(VisualElement root, UIServices services) {
        _root = root;
        _root.AddToClassList("candidate-market");

        // --- Screen Header ---
        var header = new VisualElement();
        header.AddToClassList("screen-header");
        header.AddToClassList("flex-row");
        header.AddToClassList("justify-between");

        var titleRow = new VisualElement();
        titleRow.AddToClassList("flex-row");
        titleRow.style.alignItems = Align.Center;

        _backButton = new Button();
        _backButton.text = "← HR Portal";
        _backButton.AddToClassList("btn-ghost");
        _backButton.AddToClassList("btn-sm");
        _backButton.style.marginRight = 12;
        titleRow.Add(_backButton);

        var titleLabel = new Label("Candidates");
        titleLabel.AddToClassList("metric-primary");
        titleLabel.AddToClassList("text-accent");
        titleRow.Add(titleLabel);

        header.Add(titleRow);
        _root.Add(header);

        // --- Tab Bar ---
        var tabBar = new VisualElement();
        tabBar.AddToClassList("tab-bar");
        tabBar.AddToClassList("flex-row");

        _tabMarket = new Button();
        _tabMarket.text = "Market Pool";
        _tabMarket.AddToClassList("tab-btn");
        _tabMarket.AddToClassList("tab--active");
        tabBar.Add(_tabMarket);

        _tabSourced = new Button();
        _tabSourced.text = "Sourced Pool";
        _tabSourced.AddToClassList("tab-btn");
        tabBar.Add(_tabSourced);

        _tabShortlist = new Button();
        _tabShortlist.text = "Shortlist";
        _tabShortlist.AddToClassList("tab-btn");
        tabBar.Add(_tabShortlist);

        _root.Add(tabBar);

        // --- Pool Info Bar ---
        var infoBar = new VisualElement();
        infoBar.AddToClassList("pool-info-bar");
        infoBar.AddToClassList("flex-row");
        infoBar.AddToClassList("justify-between");

        _capacityLabel = new Label("0 / 20 candidates");
        _capacityLabel.AddToClassList("metric-tertiary");
        infoBar.Add(_capacityLabel);

        _capacityWarning = new Label("Pool near full");
        _capacityWarning.AddToClassList("badge");
        _capacityWarning.AddToClassList("badge--warning");
        _capacityWarning.style.display = DisplayStyle.None;
        infoBar.Add(_capacityWarning);

        _refreshLabel = new Label("Refreshes in --d");
        _refreshLabel.AddToClassList("metric-tertiary");
        infoBar.Add(_refreshLabel);

        _root.Add(infoBar);

        // --- Candidate List ---
        var scroll = new ScrollView();
        scroll.style.flexGrow = 1;

        _candidateListContainer = new VisualElement();
        _candidateListContainer.name = "candidate-list";
        _candidateListContainer.AddToClassList("candidate-list");
        scroll.Add(_candidateListContainer);

        _root.Add(scroll);

        _candidatePool = new ElementPool(CreateCandidateRow, _candidateListContainer);

        // Wire handlers
        _backButton.clicked += OnBackClicked;
        _tabMarket.clicked += OnTabMarketClicked;
        _tabSourced.clicked += OnTabSourcedClicked;
        _tabShortlist.clicked += OnTabShortlistClicked;
    }

    public void Bind(IViewModel viewModel) {
        _vm = viewModel as CandidateMarketViewModel;
        if (_vm == null) return;

        UpdateActiveTabList();
    }

    public void Dispose() {
        if (_backButton != null)   _backButton.clicked   -= OnBackClicked;
        if (_tabMarket != null)    _tabMarket.clicked    -= OnTabMarketClicked;
        if (_tabSourced != null)   _tabSourced.clicked   -= OnTabSourcedClicked;
        if (_tabShortlist != null) _tabShortlist.clicked -= OnTabShortlistClicked;

        _candidatePool = null;
        _vm = null;
        _root = null;
    }

    // --- Tab logic ---

    private void OnBackClicked()      => _nav.NavigateTo(ScreenId.HRPortalLanding);
    private void OnTabMarketClicked() => SetActiveTab(0);
    private void OnTabSourcedClicked()  => SetActiveTab(1);
    private void OnTabShortlistClicked() => SetActiveTab(2);

    private void SetActiveTab(int index) {
        _activeTab = index;
        UpdateTabClasses();
        if (_vm != null) UpdateActiveTabList();
    }

    private void UpdateTabClasses() {
        SetTabActive(_tabMarket,   _activeTab == 0);
        SetTabActive(_tabSourced,  _activeTab == 1);
        SetTabActive(_tabShortlist, _activeTab == 2);
    }

    private static void SetTabActive(Button btn, bool active) {
        if (btn == null) return;
        if (active) btn.AddToClassList("tab--active");
        else btn.RemoveFromClassList("tab--active");
    }

    private void UpdateActiveTabList() {
        if (_vm == null) return;
        List<CandidateRowDisplay> pool;
        switch (_activeTab) {
            case 1:
                pool = _vm.SourcedPool;
                _capacityLabel.text = _vm.SourcedCount + " sourced";
                _capacityWarning.style.display = DisplayStyle.None;
                _refreshLabel.style.display = DisplayStyle.None;
                break;
            case 2:
                pool = _vm.Shortlist;
                _capacityLabel.text = _vm.ShortlistCount + " shortlisted";
                _capacityWarning.style.display = DisplayStyle.None;
                _refreshLabel.style.display = DisplayStyle.None;
                break;
            default:
                pool = _vm.MarketPool;
                _capacityLabel.text = _vm.MarketCount + " / " + CandidateMarketViewModel.MaxPoolCapacity + " candidates";
                _refreshLabel.text = "Refreshes in " + _vm.DaysToRefresh + "d";
                _refreshLabel.style.display = DisplayStyle.Flex;
                if (_vm.IsPoolFull) {
                    _capacityWarning.text = "Pool full";
                    _capacityWarning.style.display = DisplayStyle.Flex;
                } else if (_vm.IsPoolNearFull) {
                    _capacityWarning.text = "Near full";
                    _capacityWarning.style.display = DisplayStyle.Flex;
                } else {
                    _capacityWarning.style.display = DisplayStyle.None;
                }
                break;
        }
        _candidatePool.UpdateList(pool, BindCandidateRow);
    }

    // --- Row factory/bind ---

    private VisualElement CreateCandidateRow() {
        var row = new VisualElement();
        row.AddToClassList("candidate-row");
        row.AddToClassList("list-item");
        row.AddToClassList("flex-row");

        var leftCol = new VisualElement();
        leftCol.style.flexGrow = 1;

        var nameRow = new VisualElement();
        nameRow.AddToClassList("flex-row");
        nameRow.style.alignItems = Align.Center;
        nameRow.style.marginBottom = 2;

        var nameLabel = new Label();
        nameLabel.name = "row-name";
        nameLabel.AddToClassList("metric-secondary");
        nameLabel.style.marginRight = 6;
        nameRow.Add(nameLabel);

        var interviewedBadge = new Label("Interviewed");
        interviewedBadge.name = "row-interviewed";
        interviewedBadge.AddToClassList("badge");
        interviewedBadge.AddToClassList("badge--success");
        interviewedBadge.style.display = DisplayStyle.None;
        nameRow.Add(interviewedBadge);

        var interviewingBadge = new Label("Interviewing");
        interviewingBadge.name = "row-interviewing";
        interviewingBadge.AddToClassList("badge");
        interviewingBadge.AddToClassList("badge--accent");
        interviewingBadge.style.display = DisplayStyle.None;
        nameRow.Add(interviewingBadge);

        var counterBadge = new Label("Counter!");
        counterBadge.name = "row-counter-badge";
        counterBadge.AddToClassList("badge");
        counterBadge.AddToClassList("counter-badge");
        counterBadge.style.display = DisplayStyle.None;
        counterBadge.RegisterCallback<ClickEvent>(OnCounterBadgeClicked);
        nameRow.Add(counterBadge);

        leftCol.Add(nameRow);

        var roleRow = new VisualElement();
        roleRow.AddToClassList("flex-row");
        roleRow.style.alignItems = Align.Center;

        var rolePill = new Label();
        rolePill.name = "row-role";
        rolePill.AddToClassList("role-pill");
        rolePill.style.marginRight = 6;
        roleRow.Add(rolePill);

        var sourceBadge = new Label();
        sourceBadge.name = "row-source";
        sourceBadge.AddToClassList("badge");
        sourceBadge.AddToClassList("badge--muted");
        roleRow.Add(sourceBadge);

        var patienceContainer = new VisualElement();
        patienceContainer.name = "row-patience";
        patienceContainer.AddToClassList("patience-dots");
        patienceContainer.AddToClassList("flex-row");
        patienceContainer.style.display = DisplayStyle.None;
        patienceContainer.style.marginLeft = 6;
        roleRow.Add(patienceContainer);

        leftCol.Add(roleRow);
        row.Add(leftCol);

        var rightCol = new VisualElement();
        rightCol.AddToClassList("flex-col");
        rightCol.style.alignItems = Align.FlexEnd;

        var salaryChip = new Label();
        salaryChip.name = "row-salary";
        salaryChip.AddToClassList("badge");
        salaryChip.style.marginBottom = 4;
        rightCol.Add(salaryChip);

        var expiryLabel = new Label();
        expiryLabel.name = "row-expiry";
        expiryLabel.AddToClassList("metric-tertiary");
        rightCol.Add(expiryLabel);

        row.Add(rightCol);

        row.RegisterCallback<ClickEvent>(OnRowClicked);
        return row;
    }

    private void BindCandidateRow(VisualElement el, CandidateRowDisplay data) {
        el.userData = data.CandidateId;

        var nameLabel        = el.Q<Label>("row-name");
        var interviewedBadge = el.Q<Label>("row-interviewed");
        var interviewingBadge = el.Q<Label>("row-interviewing");
        var counterBadge     = el.Q<Label>("row-counter-badge");
        var rolePill         = el.Q<Label>("row-role");
        var sourceBadge      = el.Q<Label>("row-source");
        var salaryChip       = el.Q<Label>("row-salary");
        var expiryLabel      = el.Q<Label>("row-expiry");
        var patienceContainer = el.Q<VisualElement>("row-patience");

        if (nameLabel != null)        nameLabel.text = data.Name;

        if (interviewedBadge != null)
            interviewedBadge.style.display = data.IsInterviewed ? DisplayStyle.Flex : DisplayStyle.None;

        if (interviewingBadge != null)
            interviewingBadge.style.display = (!data.IsInterviewed && data.IsInterviewing) ? DisplayStyle.Flex : DisplayStyle.None;

        if (counterBadge != null) {
            counterBadge.style.display = data.HasPendingCounter ? DisplayStyle.Flex : DisplayStyle.None;
            counterBadge.userData = data.CandidateId;
        }

        if (rolePill != null) {
            rolePill.text = data.RoleName;
            UIFormatting.ClearRolePillClasses(rolePill);
            rolePill.AddToClassList(data.RolePillClass);
        }

        if (sourceBadge != null) sourceBadge.text = data.SourceBadge;

        if (patienceContainer != null) {
            if (data.HasPendingCounter && data.PatienceMax > 0) {
                patienceContainer.style.display = DisplayStyle.Flex;
                patienceContainer.Clear();
                for (int i = 0; i < data.PatienceMax; i++) {
                    var dot = new VisualElement();
                    dot.AddToClassList("patience-dot");
                    if (i < data.PatienceCurrent) dot.AddToClassList("patience-dot--active");
                    patienceContainer.Add(dot);
                }
            } else {
                patienceContainer.style.display = DisplayStyle.None;
            }
        }

        if (salaryChip != null) {
            salaryChip.text = data.SalaryChip;
            salaryChip.RemoveFromClassList("badge--success");
            salaryChip.RemoveFromClassList("badge--danger");
            salaryChip.RemoveFromClassList("badge--accent");
            salaryChip.AddToClassList(data.SalaryChipClass);
        }

        if (expiryLabel != null) {
            expiryLabel.text = data.ExpiryText;
            expiryLabel.RemoveFromClassList("text-danger");
            expiryLabel.RemoveFromClassList("text-muted");
            expiryLabel.AddToClassList(data.ExpiryClass);
        }
    }

    private void OnCounterBadgeClicked(ClickEvent evt) {
        evt.StopPropagation();
        var el = evt.currentTarget as VisualElement;
        if (el == null || !(el.userData is int candidateId)) return;
        _modal.ShowCandidateDetailModal(candidateId, showCounterOffer: true);
    }

    private void OnRowClicked(ClickEvent evt) {
        var el = evt.currentTarget as VisualElement;
        if (el == null || !(el.userData is int candidateId)) return;

        var vm = new CandidateDetailModalViewModel();
        vm.SetCandidateId(candidateId);
        _modal.ShowModal(new CandidateDetailModalView(), vm);
    }
}