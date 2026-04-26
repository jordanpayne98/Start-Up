using System.Collections.Generic;
using UnityEngine.UIElements;

public class HRAssignmentsView : IGameView
{
    private readonly ICommandDispatcher _dispatcher;
    private readonly IModalPresenter _modal;
    private readonly INavigationService _nav;
    private readonly ITooltipProvider _tooltip;

    private VisualElement _root;
    private HRAssignmentsViewModel _vm;

    // Header
    private Button _backButton;

    // Tab buttons
    private Button _tabAssignments;
    private Button _tabSourced;

    // Search tab
    private Label _searchCountLabel;
    private Button _newSearchButton;
    private ScrollView _assignmentsScroll;
    private VisualElement _assignmentListContainer;
    private ElementPool _assignmentPool;
    private VisualElement _assignmentEmptyState;
    private Label _noHRMessageLabel;

    // Sourced tab
    private Label _sourcedCountLabel;
    private ScrollView _sourcedScroll;
    private VisualElement _sourcedListContainer;
    private ElementPool _sourcedPool;
    private VisualElement _sourcedEmptyState;

    // Active tab index: 0 = Assignments, 1 = Sourced
    private int _activeTab;

    public HRAssignmentsView(ICommandDispatcher dispatcher, IModalPresenter modal, INavigationService nav, ITooltipProvider tooltip) {
        _dispatcher = dispatcher;
        _modal = modal;
        _nav = nav;
        _tooltip = tooltip;
    }

    public void Initialize(VisualElement root) {
        _root = root;
        _root.AddToClassList("hr-assignments");

        // --- Screen Header ---
        var header = new VisualElement();
        header.AddToClassList("screen-header");
        header.AddToClassList("flex-row");
        header.AddToClassList("justify-between");
        header.style.alignItems = Align.Center;

        var titleRow = new VisualElement();
        titleRow.AddToClassList("flex-row");
        titleRow.style.alignItems = Align.Center;

        _backButton = new Button();
        _backButton.text = "\u2190 HR Portal";
        _backButton.AddToClassList("btn-ghost");
        _backButton.AddToClassList("btn-sm");
        _backButton.style.marginRight = 12;
        titleRow.Add(_backButton);

        var titleLabel = new Label("HR Assignments");
        titleLabel.AddToClassList("metric-primary");
        titleLabel.AddToClassList("text-accent");
        titleRow.Add(titleLabel);

        header.Add(titleRow);
        _root.Add(header);

        // --- Tab Bar ---
        var tabBar = new VisualElement();
        tabBar.AddToClassList("tab-bar");
        tabBar.AddToClassList("flex-row");

        _tabAssignments = new Button();
        _tabAssignments.text = "Search Assignments";
        _tabAssignments.AddToClassList("tab-btn");
        _tabAssignments.AddToClassList("tab--active");
        tabBar.Add(_tabAssignments);

        _tabSourced = new Button();
        _tabSourced.text = "Sourced Pool";
        _tabSourced.AddToClassList("tab-btn");
        tabBar.Add(_tabSourced);

        _root.Add(tabBar);

        // ── ASSIGNMENTS TAB CONTENT ──────────────────────────────────────────────
        var assignmentsSection = new VisualElement();
        assignmentsSection.name = "section-assignments";
        assignmentsSection.style.flexGrow = 1;

        // Assignments toolbar
        var assignmentsToolbar = new VisualElement();
        assignmentsToolbar.AddToClassList("flex-row");
        assignmentsToolbar.AddToClassList("justify-between");
        assignmentsToolbar.style.alignItems = Align.Center;
        assignmentsToolbar.style.marginBottom = 8;

        _searchCountLabel = new Label("0 active searches");
        _searchCountLabel.AddToClassList("metric-tertiary");
        assignmentsToolbar.Add(_searchCountLabel);

        _newSearchButton = new Button { text = "+ New Search" };
        _newSearchButton.AddToClassList("btn-primary");
        _newSearchButton.AddToClassList("btn-sm");
        assignmentsToolbar.Add(_newSearchButton);
        assignmentsSection.Add(assignmentsToolbar);

        // No HR message
        _noHRMessageLabel = new Label();
        _noHRMessageLabel.AddToClassList("badge");
        _noHRMessageLabel.AddToClassList("badge--warning");
        _noHRMessageLabel.style.marginBottom = 8;
        _noHRMessageLabel.style.display = DisplayStyle.None;
        assignmentsSection.Add(_noHRMessageLabel);

        // Assignment list
        var assignmentsScroll = new ScrollView();
        assignmentsScroll.style.flexGrow = 1;
        _assignmentsScroll = assignmentsScroll;

        _assignmentListContainer = new VisualElement();
        _assignmentListContainer.name = "assignment-list";
        _assignmentListContainer.AddToClassList("assignment-list");
        assignmentsScroll.Add(_assignmentListContainer);
        assignmentsSection.Add(assignmentsScroll);

        _assignmentPool = new ElementPool(CreateAssignmentCard, _assignmentListContainer);

        // Assignment empty state
        _assignmentEmptyState = new VisualElement();
        _assignmentEmptyState.AddToClassList("empty-state");
        _assignmentEmptyState.style.display = DisplayStyle.None;

        var emptyAssignIcon = new Label("\u2315");
        emptyAssignIcon.AddToClassList("empty-state__icon");
        _assignmentEmptyState.Add(emptyAssignIcon);

        var emptyAssignLabel = new Label("No active searches");
        emptyAssignLabel.AddToClassList("empty-state__title");
        _assignmentEmptyState.Add(emptyAssignLabel);

        var emptyAssignSub = new Label("Click \u201c+ New Search\u201d to start sourcing candidates.");
        emptyAssignSub.AddToClassList("empty-state__subtitle");
        _assignmentEmptyState.Add(emptyAssignSub);

        assignmentsSection.Add(_assignmentEmptyState);
        _root.Add(assignmentsSection);

        // ── SOURCED TAB CONTENT ──────────────────────────────────────────────────
        var sourcedSection = new VisualElement();
        sourcedSection.name = "section-sourced";
        sourcedSection.style.flexGrow = 1;
        sourcedSection.style.display = DisplayStyle.None;

        // Sourced toolbar
        var sourcedToolbar = new VisualElement();
        sourcedToolbar.AddToClassList("flex-row");
        sourcedToolbar.AddToClassList("justify-between");
        sourcedToolbar.style.alignItems = Align.Center;
        sourcedToolbar.style.marginBottom = 8;

        _sourcedCountLabel = new Label("0 sourced candidates");
        _sourcedCountLabel.AddToClassList("metric-tertiary");
        sourcedToolbar.Add(_sourcedCountLabel);
        sourcedSection.Add(sourcedToolbar);

        // Sourced list
        var sourcedScroll = new ScrollView();
        sourcedScroll.style.flexGrow = 1;
        _sourcedScroll = sourcedScroll;

        _sourcedListContainer = new VisualElement();
        _sourcedListContainer.name = "sourced-list";
        _sourcedListContainer.AddToClassList("sourced-list");
        sourcedScroll.Add(_sourcedListContainer);
        sourcedSection.Add(sourcedScroll);

        _sourcedPool = new ElementPool(CreateSourcedRow, _sourcedListContainer);

        // Sourced empty state
        _sourcedEmptyState = new VisualElement();
        _sourcedEmptyState.AddToClassList("empty-state");
        _sourcedEmptyState.style.display = DisplayStyle.None;

        var emptySourcedIcon = new Label("\u25a1");
        emptySourcedIcon.AddToClassList("empty-state__icon");
        _sourcedEmptyState.Add(emptySourcedIcon);

        var emptySourcedLabel = new Label("No sourced candidates");
        emptySourcedLabel.AddToClassList("empty-state__title");
        _sourcedEmptyState.Add(emptySourcedLabel);

        var emptySourcedSub = new Label("Completed HR searches will deliver candidates here.");
        emptySourcedSub.AddToClassList("empty-state__subtitle");
        _sourcedEmptyState.Add(emptySourcedSub);

        sourcedSection.Add(_sourcedEmptyState);
        _root.Add(sourcedSection);

        // Store section refs via name lookup for tab switching
        _root.Q<VisualElement>("section-assignments").userData = 0;
        _root.Q<VisualElement>("section-sourced").userData = 1;

        // Wire handlers
        _backButton.clicked += OnBackClicked;
        _tabAssignments.clicked += OnTabAssignmentsClicked;
        _tabSourced.clicked += OnTabSourcedClicked;
        _newSearchButton.clicked += OnNewSearchClicked;
    }

    public void Bind(IViewModel viewModel) {
        _vm = viewModel as HRAssignmentsViewModel;
        if (_vm == null) return;

        // Assignments tab
        int searchCount = _vm.ActiveSearchCount;
        _searchCountLabel.text = searchCount == 1 ? "1 active search" : searchCount + " active searches";

        bool hasHRTeam = _vm.HasHRTeam;
        string noHRMsg = _vm.NoHRMessage;
        bool showNoHRMsg = !string.IsNullOrEmpty(noHRMsg);
        _noHRMessageLabel.text = noHRMsg;
        _noHRMessageLabel.style.display = showNoHRMsg ? DisplayStyle.Flex : DisplayStyle.None;
        _newSearchButton.SetEnabled(hasHRTeam);

        bool hasSearches = searchCount > 0;
        _assignmentEmptyState.style.display = hasSearches ? DisplayStyle.None : DisplayStyle.Flex;
        _assignmentsScroll.style.display = hasSearches ? DisplayStyle.Flex : DisplayStyle.None;
        _assignmentPool.UpdateList(_vm.ActiveSearches, BindAssignmentCard);

        // Sourced tab
        int sourcedCount = _vm.SourcedCount;
        _sourcedCountLabel.text = sourcedCount == 1 ? "1 sourced candidate" : sourcedCount + " sourced candidates";

        bool hasSourced = sourcedCount > 0;
        _sourcedEmptyState.style.display = hasSourced ? DisplayStyle.None : DisplayStyle.Flex;
        _sourcedScroll.style.display = hasSourced ? DisplayStyle.Flex : DisplayStyle.None;
        _sourcedPool.UpdateList(_vm.SourcedCandidates, BindSourcedRow);
    }

    public void Dispose() {
        if (_backButton != null)        _backButton.clicked        -= OnBackClicked;
        if (_tabAssignments != null)    _tabAssignments.clicked    -= OnTabAssignmentsClicked;
        if (_tabSourced != null)        _tabSourced.clicked        -= OnTabSourcedClicked;
        if (_newSearchButton != null)   _newSearchButton.clicked   -= OnNewSearchClicked;

        _assignmentPool = null;
        _sourcedPool = null;
        _assignmentsScroll = null;
        _sourcedScroll = null;
        _vm = null;
        _root = null;
    }

    // --- Tab logic ---

    private void OnBackClicked()           => _nav.NavigateTo(ScreenId.HRPortalLanding);
    private void OnTabAssignmentsClicked() => SetActiveTab(0);
    private void OnTabSourcedClicked()     => SetActiveTab(1);

    private void SetActiveTab(int index) {
        _activeTab = index;

        bool showAssignments = index == 0;
        SetTabActive(_tabAssignments, showAssignments);
        SetTabActive(_tabSourced, !showAssignments);

        var sectAssign = _root.Q<VisualElement>("section-assignments");
        var sectSourced = _root.Q<VisualElement>("section-sourced");

        if (sectAssign != null) sectAssign.style.display = showAssignments ? DisplayStyle.Flex : DisplayStyle.None;
        if (sectSourced != null) sectSourced.style.display = showAssignments ? DisplayStyle.None : DisplayStyle.Flex;
    }

    private static void SetTabActive(Button btn, bool active) {
        if (btn == null) return;
        if (active) btn.AddToClassList("tab--active");
        else btn.RemoveFromClassList("tab--active");
    }

    // --- New Search ---

    private void OnNewSearchClicked() {
        var vm = new AssignmentModalViewModel();
        vm.SetEditMode(false);
        _modal.ShowModal(new AssignmentModalView(_modal, _dispatcher), vm);
    }

    // --- Assignment card factory/bind ---

    private VisualElement CreateAssignmentCard() {
        var card = new VisualElement();
        card.AddToClassList("assignment-card");
        card.AddToClassList("card");

        // Header row: role pill + status badge
        var cardHeader = new VisualElement();
        cardHeader.AddToClassList("flex-row");
        cardHeader.AddToClassList("justify-between");
        cardHeader.style.alignItems = Align.Center;
        cardHeader.style.marginBottom = 8;

        var rolePill = new Label();
        rolePill.name = "card-role";
        rolePill.AddToClassList("role-pill");
        cardHeader.Add(rolePill);

        var statusBadge = new Label();
        statusBadge.name = "card-status";
        statusBadge.AddToClassList("badge");
        cardHeader.Add(statusBadge);

        card.Add(cardHeader);

        // Filter row
        var filterRow = new VisualElement();
        filterRow.AddToClassList("flex-row");
        filterRow.AddToClassList("assignment-filter-row");
        filterRow.style.marginBottom = 8;

        var abilityChip = new Label();
        abilityChip.name = "card-ability";
        abilityChip.AddToClassList("chip");
        abilityChip.AddToClassList("chip--muted");
        abilityChip.style.marginRight = 6;
        filterRow.Add(abilityChip);

        var potentialChip = new Label();
        potentialChip.name = "card-potential";
        potentialChip.AddToClassList("chip");
        potentialChip.AddToClassList("chip--muted");
        potentialChip.style.marginRight = 6;
        filterRow.Add(potentialChip);

        var batchChip = new Label();
        batchChip.name = "card-batch";
        batchChip.AddToClassList("chip");
        batchChip.AddToClassList("chip--muted");
        filterRow.Add(batchChip);

        card.Add(filterRow);

        // Progress bar
        var progressBar = new VisualElement();
        progressBar.AddToClassList("progress-bar");
        progressBar.style.marginBottom = 8;

        var progressFill = new VisualElement();
        progressFill.name = "card-progress-fill";
        progressFill.AddToClassList("progress-bar__fill");
        progressBar.Add(progressFill);
        card.Add(progressBar);

        // Action row
        var actionRow = new VisualElement();
        actionRow.AddToClassList("flex-row");
        actionRow.AddToClassList("justify-end");

        var cancelBtn = new Button { text = "Cancel" };
        cancelBtn.name = "card-cancel";
        cancelBtn.AddToClassList("btn-ghost");
        cancelBtn.AddToClassList("btn-sm");
        cancelBtn.AddToClassList("text-danger");
        cancelBtn.RegisterCallback<ClickEvent>(OnCancelSearchClicked);
        actionRow.Add(cancelBtn);

        card.Add(actionRow);
        return card;
    }

    private void BindAssignmentCard(VisualElement el, SearchAssignmentDisplay data) {
        el.userData = data.SearchId;

        var rolePill       = el.Q<Label>("card-role");
        var statusBadge    = el.Q<Label>("card-status");
        var abilityChip    = el.Q<Label>("card-ability");
        var potentialChip  = el.Q<Label>("card-potential");
        var batchChip      = el.Q<Label>("card-batch");
        var progressFill   = el.Q<VisualElement>("card-progress-fill");

        if (rolePill != null) {
            rolePill.text = data.RoleName;
            UIFormatting.ClearRolePillClasses(rolePill);
            rolePill.AddToClassList(UIFormatting.RolePillClass(data.RoleName));
        }

        if (statusBadge != null) {
            statusBadge.text = data.Status;
            statusBadge.RemoveFromClassList("badge--accent");
            statusBadge.RemoveFromClassList("badge--success");
            statusBadge.AddToClassList(data.StatusClass);
        }

        if (abilityChip != null)   abilityChip.text   = data.AbilityFilter;
        if (potentialChip != null) potentialChip.text  = data.PotentialFilter;
        if (batchChip != null)     batchChip.text      = "Batch: " + data.BatchSize;

        if (progressFill != null) {
            float clampedProgress = data.Progress < 0f ? 0f : (data.Progress > 1f ? 1f : data.Progress);
            progressFill.style.width = Length.Percent(clampedProgress * 100f);
        }

        // Store the search id on the cancel button via parent card's userData
        var cancelBtn = el.Q<Button>("card-cancel");
        if (cancelBtn != null) cancelBtn.userData = data.SearchId;
    }

    private void OnCancelSearchClicked(ClickEvent evt) {
        var btn = evt.currentTarget as Button;
        if (btn == null) return;
        if (!(btn.userData is HRSearchId searchId)) return;
        _dispatcher.Dispatch(new CancelHRSearchCommand { SearchId = searchId });
    }

    // --- Sourced row factory/bind ---

    private VisualElement CreateSourcedRow() {
        var row = new VisualElement();
        row.AddToClassList("sourced-row");
        row.AddToClassList("list-item");
        row.AddToClassList("flex-row");

        var leftCol = new VisualElement();
        leftCol.style.flexGrow = 1;

        var nameLabel = new Label();
        nameLabel.name = "row-name";
        nameLabel.AddToClassList("metric-secondary");
        nameLabel.style.marginBottom = 2;
        leftCol.Add(nameLabel);

        var rolePill = new Label();
        rolePill.name = "row-role";
        rolePill.AddToClassList("role-pill");
        leftCol.Add(rolePill);

        row.Add(leftCol);

        var rightCol = new VisualElement();
        rightCol.AddToClassList("flex-col");
        rightCol.style.alignItems = UnityEngine.UIElements.Align.FlexEnd;

        var abilityLabel = new Label();
        abilityLabel.name = "row-ability";
        abilityLabel.AddToClassList("metric-secondary");
        abilityLabel.style.marginBottom = 2;
        rightCol.Add(abilityLabel);

        var potentialLabel = new Label();
        potentialLabel.name = "row-potential";
        potentialLabel.AddToClassList("metric-tertiary");
        rightCol.Add(potentialLabel);

        row.Add(rightCol);

        row.RegisterCallback<ClickEvent>(OnSourcedRowClicked);
        return row;
    }

    private void BindSourcedRow(VisualElement el, SourcedCandidateDisplay data) {
        el.userData = data.CandidateId;

        var nameLabel      = el.Q<Label>("row-name");
        var rolePill       = el.Q<Label>("row-role");
        var abilityLabel   = el.Q<Label>("row-ability");
        var potentialLabel = el.Q<Label>("row-potential");

        if (nameLabel != null)    nameLabel.text = data.Name;

        if (rolePill != null) {
            rolePill.text = data.Role;
            UIFormatting.ClearRolePillClasses(rolePill);
            rolePill.AddToClassList(UIFormatting.RolePillClass(data.Role));
        }

        if (abilityLabel != null)   abilityLabel.text   = "CA: " + data.AbilityEstimate;
        if (potentialLabel != null) potentialLabel.text  = data.PotentialEstimate;
    }

    private void OnSourcedRowClicked(ClickEvent evt) {
        var el = evt.currentTarget as VisualElement;
        if (el == null || !(el.userData is int candidateId)) return;

        var vm = new CandidateDetailModalViewModel();
        vm.SetCandidateId(candidateId);
        _modal.ShowModal(new CandidateDetailModalView(_modal, _dispatcher), vm);
    }
}
