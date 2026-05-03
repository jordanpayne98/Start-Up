using System.Collections.Generic;
using UnityEngine.UIElements;

public class HRPortalLandingView : IGameView
{
    private readonly ICommandDispatcher _dispatcher;
    private readonly INavigationService _nav;

    private VisualElement _root;

    // Candidates card
    private Label _candidateCountLabel;
    private Label _shortlistedLabel;
    private Label _daysToRefreshLabel;
    private Label _candidateBadge;
    private VisualElement _candidateMiniList;
    private ElementPool _candidatePool;
    private Button _candidatesBtn;

    // Assignments card
    private Label _activeSearchesLabel;
    private Label _sourcedCountLabel;
    private Label _assignmentBadge;
    private VisualElement _assignmentMiniList;
    private ElementPool _assignmentPool;
    private Button _assignmentsBtn;

    // Employees card
    private Label _employeeCountLabel;
    private Label _capacityLabel;
    private Label _pendingRenewalsLabel;
    private Label _employeeBadge;
    private VisualElement _employeeMiniList;
    private ElementPool _employeePool;
    private Button _employeesBtn;

    // Teams card
    private Label _teamCountLabel;
    private Label _avgChemistryLabel;
    private Label _teamBadge;
    private VisualElement _teamMiniList;
    private ElementPool _teamPool;
    private Button _teamsBtn;

    public HRPortalLandingView(ICommandDispatcher dispatcher, INavigationService nav) {
        _dispatcher = dispatcher;
        _nav = nav;
    }

    public void Initialize(VisualElement root, UIServices services) {
        _root = root;

        var header = new VisualElement();
        header.AddToClassList("portal-header");
        var titleLabel = new Label("HR Portal");
        titleLabel.AddToClassList("metric-primary");
        titleLabel.AddToClassList("text-accent");
        header.Add(titleLabel);
        var subtitleLabel = new Label("People, Teams & Talent");
        subtitleLabel.AddToClassList("metric-tertiary");
        header.Add(subtitleLabel);
        _root.Add(header);

        var grid = new VisualElement();
        grid.AddToClassList("portal-card-grid");
        _root.Add(grid);

        // --- Candidates card ---
        var candidatesCard = BuildPortalCard(grid, "Candidates", "border--accent-info", out _candidateBadge);
        (_candidateCountLabel, _) = AddStatRow(candidatesCard, "Available");
        (_shortlistedLabel, _)   = AddStatRow(candidatesCard, "Shortlisted");
        (_daysToRefreshLabel, _) = AddStatRow(candidatesCard, "Pool refresh");
        AddDivider(candidatesCard);
        _candidateMiniList = new VisualElement();
        _candidateMiniList.name = "candidate-mini-list";
        _candidateMiniList.AddToClassList("portal-card__mini-list");
        candidatesCard.Add(_candidateMiniList);
        _candidatePool = new ElementPool(CreateCandidateItem, _candidateMiniList);
        _candidatesBtn = new Button(OnCandidatesClicked) { text = "Show Candidates →" };
        _candidatesBtn.AddToClassList("btn-ghost");
        _candidatesBtn.AddToClassList("btn-sm");
        candidatesCard.Add(_candidatesBtn);

        // --- Assignments card ---
        var assignmentsCard = BuildPortalCard(grid, "HR Assignments", "border--accent-warning", out _assignmentBadge);
        (_activeSearchesLabel, _) = AddStatRow(assignmentsCard, "Active searches");
        (_sourcedCountLabel, _)   = AddStatRow(assignmentsCard, "Sourced candidates");
        AddDivider(assignmentsCard);
        _assignmentMiniList = new VisualElement();
        _assignmentMiniList.name = "assignment-mini-list";
        _assignmentMiniList.AddToClassList("portal-card__mini-list");
        assignmentsCard.Add(_assignmentMiniList);
        _assignmentPool = new ElementPool(CreateSearchItem, _assignmentMiniList);
        _assignmentsBtn = new Button(OnAssignmentsClicked) { text = "Show Assignments →" };
        _assignmentsBtn.AddToClassList("btn-ghost");
        _assignmentsBtn.AddToClassList("btn-sm");
        assignmentsCard.Add(_assignmentsBtn);

        // --- Employees card ---
        var employeesCard = BuildPortalCard(grid, "Employees", "border--accent-success", out _employeeBadge);
        (_employeeCountLabel, _)   = AddStatRow(employeesCard, "Total");
        (_capacityLabel, _)        = AddStatRow(employeesCard, "Avg. capacity");
        (_pendingRenewalsLabel, _) = AddStatRow(employeesCard, "Renewals due");
        AddDivider(employeesCard);
        _employeeMiniList = new VisualElement();
        _employeeMiniList.name = "employee-mini-list";
        _employeeMiniList.AddToClassList("portal-card__mini-list");
        employeesCard.Add(_employeeMiniList);
        _employeePool = new ElementPool(CreateEmployeeItem, _employeeMiniList);
        _employeesBtn = new Button(OnEmployeesClicked) { text = "Show Employees →" };
        _employeesBtn.AddToClassList("btn-ghost");
        _employeesBtn.AddToClassList("btn-sm");
        employeesCard.Add(_employeesBtn);

        // --- Teams card ---
        var teamsCard = BuildPortalCard(grid, "Teams", "border--accent-special", out _teamBadge);
        (_teamCountLabel, _)    = AddStatRow(teamsCard, "Active teams");
        (_avgChemistryLabel, _) = AddStatRow(teamsCard, "Avg. chemistry");
        AddDivider(teamsCard);
        _teamMiniList = new VisualElement();
        _teamMiniList.name = "team-mini-list";
        _teamMiniList.AddToClassList("portal-card__mini-list");
        teamsCard.Add(_teamMiniList);
        _teamPool = new ElementPool(CreateTeamItem, _teamMiniList);
        _teamsBtn = new Button(OnTeamsClicked) { text = "Show Teams →" };
        _teamsBtn.AddToClassList("btn-ghost");
        _teamsBtn.AddToClassList("btn-sm");
        teamsCard.Add(_teamsBtn);
    }

    public void Bind(IViewModel viewModel) {
        var vm = viewModel as HRPortalLandingViewModel;
        if (vm == null) return;

        // Candidates card
        if (_candidateCountLabel != null)   _candidateCountLabel.text = vm.TotalCandidates.ToString();
        if (_shortlistedLabel != null)      _shortlistedLabel.text    = vm.ShortlistedCount.ToString();
        if (_daysToRefreshLabel != null)    _daysToRefreshLabel.text  = vm.DaysToRefresh + "d";
        SetBadge(_candidateBadge, vm.CandidateAlertCount);
        _candidatePool.UpdateList(vm.TopCandidates, BindCandidateItem);

        // Assignments card
        if (_activeSearchesLabel != null)   _activeSearchesLabel.text = vm.ActiveSearches.ToString();
        if (_sourcedCountLabel != null)     _sourcedCountLabel.text   = vm.SourcedCount.ToString();
        SetBadge(_assignmentBadge, vm.AssignmentAlertCount);
        _assignmentPool.UpdateList(vm.TopSearches, BindSearchItem);

        // Employees card
        if (_employeeCountLabel != null)    _employeeCountLabel.text   = vm.EmployeeCount.ToString();
        if (_capacityLabel != null)         _capacityLabel.text        = vm.EffectiveCapacity;
        if (_pendingRenewalsLabel != null)  _pendingRenewalsLabel.text = vm.PendingRenewals.ToString();
        SetBadge(_employeeBadge, vm.EmployeeAlertCount);
        _employeePool.UpdateList(vm.TopRenewals, BindEmployeeItem);

        // Teams card
        if (_teamCountLabel != null)        _teamCountLabel.text    = vm.TeamCount.ToString();
        if (_avgChemistryLabel != null)     _avgChemistryLabel.text = vm.AverageChemistry;
        SetBadge(_teamBadge, vm.TeamAlertCount);
        _teamPool.UpdateList(vm.TeamsNeedingAttention, BindTeamItem);
    }

    public void Dispose() {
        if (_candidatesBtn != null)   _candidatesBtn.clicked   -= OnCandidatesClicked;
        if (_assignmentsBtn != null)  _assignmentsBtn.clicked  -= OnAssignmentsClicked;
        if (_employeesBtn != null)    _employeesBtn.clicked    -= OnEmployeesClicked;
        if (_teamsBtn != null)        _teamsBtn.clicked        -= OnTeamsClicked;

        _candidatePool   = null;
        _assignmentPool  = null;
        _employeePool    = null;
        _teamPool        = null;
        _root            = null;
    }

    // --- Navigation handlers ---

    private void OnCandidatesClicked()  => _nav.NavigateTo(ScreenId.HRCandidates);
    private void OnAssignmentsClicked() => _nav.NavigateTo(ScreenId.HRAssignments);
    private void OnEmployeesClicked()   => _nav.NavigateTo(ScreenId.HREmployees);
    private void OnTeamsClicked()       => _nav.NavigateTo(ScreenId.HRTeams);

    // --- Card builder ---

    private VisualElement BuildPortalCard(VisualElement parent, string title, string accentClass, out Label badge) {
        var card = new VisualElement();
        card.AddToClassList("card");
        card.AddToClassList("portal-card");
        UICardHelper.ApplyBevel(card);
        UICardHelper.AddGradient(card);
        if (!string.IsNullOrEmpty(accentClass)) {
            UICardHelper.ApplyAccentBorder(card, accentClass);
        }

        var headerRow = new VisualElement();
        headerRow.AddToClassList("flex-row");
        headerRow.AddToClassList("justify-between");

        var titleLabel = new Label(title);
        titleLabel.AddToClassList("card__title");
        headerRow.Add(titleLabel);

        badge = new Label("0");
        badge.AddToClassList("badge");
        badge.AddToClassList("badge--danger");
        badge.style.display = DisplayStyle.None;
        headerRow.Add(badge);

        card.Add(headerRow);

        var wrapper = UICardHelper.WrapWithShadow(card);
        parent.Add(wrapper);

        return card;
    }

    private (Label valueLabel, VisualElement row) AddStatRow(VisualElement parent, string label) {
        var row = new VisualElement();
        row.AddToClassList("flex-row");
        row.AddToClassList("justify-between");
        row.style.marginBottom = 4;

        var labelEl = new Label(label);
        labelEl.AddToClassList("metric-tertiary");
        row.Add(labelEl);

        var valueEl = new Label("--");
        valueEl.AddToClassList("metric-secondary");
        row.Add(valueEl);

        parent.Add(row);
        return (valueEl, row);
    }

    private void AddDivider(VisualElement parent) {
        var divider = new VisualElement();
        divider.AddToClassList("divider");
        divider.style.marginTop = 8;
        divider.style.marginBottom = 8;
        parent.Add(divider);
    }

    private static void SetBadge(Label badge, int count) {
        if (badge == null) return;
        if (count > 0) {
            badge.text = count.ToString();
            badge.style.display = DisplayStyle.Flex;
        } else {
            badge.style.display = DisplayStyle.None;
        }
    }

    // --- Candidate mini-list ---

    private VisualElement CreateCandidateItem() {
        var item = new VisualElement();
        item.AddToClassList("list-item");
        item.AddToClassList("flex-row");
        item.AddToClassList("justify-between");

        var nameRole = new VisualElement();
        nameRole.style.flexGrow = 1;

        var nameLabel = new Label();
        nameLabel.name = "item-name";
        nameLabel.AddToClassList("metric-secondary");
        nameRole.Add(nameLabel);

        var roleLabel = new Label();
        roleLabel.name = "item-role";
        roleLabel.AddToClassList("metric-tertiary");
        nameRole.Add(roleLabel);

        item.Add(nameRole);

        var daysLabel = new Label();
        daysLabel.name = "item-days";
        daysLabel.AddToClassList("badge");
        daysLabel.AddToClassList("badge--warning");
        item.Add(daysLabel);

        return item;
    }

    private void BindCandidateItem(VisualElement el, CandidateMiniItem data) {
        var nameLabel = el.Q<Label>("item-name");
        var roleLabel = el.Q<Label>("item-role");
        var daysLabel = el.Q<Label>("item-days");
        if (nameLabel != null) nameLabel.text = data.Name;
        if (roleLabel != null) roleLabel.text = data.Role;
        if (daysLabel != null) daysLabel.text = data.DaysLeft;
    }

    // --- Search mini-list ---

    private VisualElement CreateSearchItem() {
        var item = new VisualElement();
        item.AddToClassList("list-item");
        item.AddToClassList("flex-row");
        item.AddToClassList("justify-between");

        var roleLabel = new Label();
        roleLabel.name = "item-role";
        roleLabel.AddToClassList("metric-secondary");
        item.Add(roleLabel);

        var statusLabel = new Label();
        statusLabel.name = "item-status";
        statusLabel.AddToClassList("metric-tertiary");
        item.Add(statusLabel);

        return item;
    }

    private void BindSearchItem(VisualElement el, SearchMiniItem data) {
        var roleLabel   = el.Q<Label>("item-role");
        var statusLabel = el.Q<Label>("item-status");
        if (roleLabel != null)   roleLabel.text   = data.Role;
        if (statusLabel != null) statusLabel.text = data.Status;
    }

    // --- Employee mini-list ---

    private VisualElement CreateEmployeeItem() {
        var item = new VisualElement();
        item.AddToClassList("list-item");
        item.AddToClassList("flex-row");
        item.AddToClassList("justify-between");

        var nameRole = new VisualElement();
        nameRole.style.flexGrow = 1;

        var nameLabel = new Label();
        nameLabel.name = "item-name";
        nameLabel.AddToClassList("metric-secondary");
        nameRole.Add(nameLabel);

        var roleLabel = new Label();
        roleLabel.name = "item-role";
        roleLabel.AddToClassList("metric-tertiary");
        nameRole.Add(roleLabel);

        item.Add(nameRole);

        var daysLabel = new Label();
        daysLabel.name = "item-days";
        daysLabel.AddToClassList("badge");
        daysLabel.AddToClassList("badge--danger");
        item.Add(daysLabel);

        return item;
    }

    private void BindEmployeeItem(VisualElement el, EmployeeMiniItem data) {
        var nameLabel = el.Q<Label>("item-name");
        var roleLabel = el.Q<Label>("item-role");
        var daysLabel = el.Q<Label>("item-days");
        if (nameLabel != null) nameLabel.text = data.Name;
        if (roleLabel != null) roleLabel.text = data.Role;
        if (daysLabel != null) daysLabel.text = data.ContractDaysLeft;
    }

    // --- Team mini-list ---

    private VisualElement CreateTeamItem() {
        var item = new VisualElement();
        item.AddToClassList("list-item");
        item.AddToClassList("flex-row");
        item.AddToClassList("justify-between");

        var nameLabel = new Label();
        nameLabel.name = "item-name";
        nameLabel.AddToClassList("metric-secondary");
        item.Add(nameLabel);

        var issueLabel = new Label();
        issueLabel.name = "item-issue";
        issueLabel.AddToClassList("badge");
        issueLabel.AddToClassList("badge--warning");
        item.Add(issueLabel);

        return item;
    }

    private void BindTeamItem(VisualElement el, TeamMiniItem data) {
        var nameLabel  = el.Q<Label>("item-name");
        var issueLabel = el.Q<Label>("item-issue");
        if (nameLabel != null)  nameLabel.text  = data.Name;
        if (issueLabel != null) issueLabel.text = data.Issue;
    }
}
