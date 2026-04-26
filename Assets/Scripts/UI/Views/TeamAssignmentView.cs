using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

public class TeamAssignmentView : IGameView
{
    private readonly ICommandDispatcher _dispatcher;
    private readonly IModalPresenter _modal;
    private readonly INavigationService _nav;

    private VisualElement _root;
    private VisualElement _listContainer;
    private ElementPool _teamCardPool;
    private VisualElement _emptyState;
    private Button _createTeamBtn;
    private Button _backBtn;

    private TeamAssignmentViewModel _vm;
    private readonly HashSet<TeamId> _expandedTeams = new HashSet<TeamId>();
    private readonly List<ElementPool> _memberPools = new List<ElementPool>(8);

    private readonly AddMemberModalViewModel _addMemberVm = new AddMemberModalViewModel();
    private readonly AddMemberModalView _addMemberView;
    private readonly CreateTeamModalView _createTeamView;

    // Inline confirmation
    private Action _pendingAction;
    private VisualElement _confirmBanner;
    private Label _confirmLabel;
    private Button _confirmYesBtn;
    private Button _confirmNoBtn;

    public TeamAssignmentView(ICommandDispatcher dispatcher, IModalPresenter modal, INavigationService nav) {
        _dispatcher = dispatcher;
        _modal = modal;
        _nav = nav;
        _addMemberView = new AddMemberModalView(dispatcher, modal);
        _createTeamView = new CreateTeamModalView(dispatcher, modal);
    }

    public void Initialize(VisualElement root) {
        _root = root;
        _root.AddToClassList("team-assignment-screen");

        var header = new VisualElement();
        header.AddToClassList("screen-header");
        header.AddToClassList("flex-row");
        header.AddToClassList("justify-between");
        header.style.marginBottom = 12;
        _root.Add(header);

        var headerLeft = new VisualElement();
        headerLeft.AddToClassList("flex-row");
        header.Add(headerLeft);

        _backBtn = new Button { text = "← Back" };
        _backBtn.AddToClassList("btn-ghost");
        _backBtn.AddToClassList("btn-sm");
        _backBtn.style.marginRight = 8;
        headerLeft.Add(_backBtn);

        var titleLabel = new Label("Teams");
        titleLabel.AddToClassList("metric-primary");
        titleLabel.AddToClassList("text-accent");
        headerLeft.Add(titleLabel);

        _createTeamBtn = new Button { text = "+ Create Team" };
        _createTeamBtn.AddToClassList("btn-primary");
        _createTeamBtn.AddToClassList("btn-sm");
        header.Add(_createTeamBtn);

        _backBtn.clicked += OnBackClicked;
        _createTeamBtn.clicked += OnCreateTeamClicked;

        // Inline confirmation banner
        _confirmBanner = new VisualElement();
        _confirmBanner.AddToClassList("card");
        _confirmBanner.AddToClassList("flex-row");
        _confirmBanner.AddToClassList("justify-between");
        _confirmBanner.style.display = DisplayStyle.None;
        _confirmBanner.style.marginBottom = 8;
        _confirmBanner.style.paddingTop = 8;
        _confirmBanner.style.paddingBottom = 8;
        _confirmBanner.style.paddingLeft = 8;
        _confirmBanner.style.paddingRight = 8;
        _root.Add(_confirmBanner);

        _confirmLabel = new Label();
        _confirmLabel.AddToClassList("metric-secondary");
        _confirmLabel.style.flexGrow = 1;
        _confirmLabel.style.whiteSpace = WhiteSpace.Normal;
        _confirmBanner.Add(_confirmLabel);

        var confirmBtns = new VisualElement();
        confirmBtns.AddToClassList("flex-row");
        _confirmBanner.Add(confirmBtns);

        _confirmYesBtn = new Button { text = "Confirm" };
        _confirmYesBtn.AddToClassList("btn-sm");
        _confirmYesBtn.AddToClassList("btn-danger");
        _confirmYesBtn.style.marginRight = 4;
        confirmBtns.Add(_confirmYesBtn);

        _confirmNoBtn = new Button { text = "Cancel" };
        _confirmNoBtn.AddToClassList("btn-sm");
        _confirmNoBtn.AddToClassList("btn-ghost");
        confirmBtns.Add(_confirmNoBtn);

        _confirmYesBtn.clicked += OnConfirmYes;
        _confirmNoBtn.clicked += OnConfirmNo;

        var scroll = new ScrollView();
        scroll.style.flexGrow = 1;
        _root.Add(scroll);

        _listContainer = new VisualElement();
        _listContainer.AddToClassList("team-card-list");
        scroll.Add(_listContainer);

        _teamCardPool = new ElementPool(CreateTeamCard, _listContainer);

        _emptyState = UICardHelper.CreateEmptyState("◉", "No teams yet. Create one to get started.");
        _emptyState.style.display = DisplayStyle.None;
        scroll.Add(_emptyState);
    }

    public void Bind(IViewModel viewModel) {
        _vm = viewModel as TeamAssignmentViewModel;
        if (_vm == null) return;
        bool hasTeams = _vm.HasTeams;
        if (_emptyState != null) _emptyState.style.display = hasTeams ? DisplayStyle.None : DisplayStyle.Flex;
        _teamCardPool.UpdateList(_vm.Teams, BindTeamCard);
    }

    public void Dispose() {
        if (_backBtn != null)       _backBtn.clicked       -= OnBackClicked;
        if (_createTeamBtn != null) _createTeamBtn.clicked -= OnCreateTeamClicked;
        if (_confirmYesBtn != null) _confirmYesBtn.clicked -= OnConfirmYes;
        if (_confirmNoBtn != null)  _confirmNoBtn.clicked  -= OnConfirmNo;

        _teamCardPool = null;
        _memberPools.Clear();
        _expandedTeams.Clear();
        _pendingAction = null;
        _vm = null;
        _root = null;
    }

    private void OnBackClicked() => _nav.NavigateTo(ScreenId.HRPortalLanding);
    private void OnCreateTeamClicked() => _modal.ShowModal(_createTeamView, null);

    private void ShowConfirmation(string message, Action onConfirm) {
        _pendingAction = onConfirm;
        if (_confirmLabel != null) _confirmLabel.text = message;
        if (_confirmBanner != null) _confirmBanner.style.display = DisplayStyle.Flex;
    }

    private void OnConfirmYes() {
        if (_confirmBanner != null) _confirmBanner.style.display = DisplayStyle.None;
        var action = _pendingAction;
        _pendingAction = null;
        action?.Invoke();
    }

    private void OnConfirmNo() {
        _pendingAction = null;
        if (_confirmBanner != null) _confirmBanner.style.display = DisplayStyle.None;
    }

    // --- Team card factory ---

    private VisualElement CreateTeamCard() {
        int poolIndex = _memberPools.Count;

        var card = new VisualElement();
        card.AddToClassList("card");
        card.AddToClassList("team-card");
        UICardHelper.ApplyBevel(card);
        UICardHelper.AddGradient(card);
        card.style.marginBottom = 8;

        var cardHeader = new VisualElement();
        cardHeader.name = "card-header";
        cardHeader.AddToClassList("team-card__header");
        cardHeader.AddToClassList("flex-row");
        cardHeader.AddToClassList("justify-between");
        card.Add(cardHeader);

        var headerLeft = new VisualElement();
        headerLeft.AddToClassList("flex-row");
        headerLeft.style.flexGrow = 1;
        cardHeader.Add(headerLeft);

        var teamName = new Label();
        teamName.name = "team-name";
        teamName.AddToClassList("metric-secondary");
        teamName.AddToClassList("text-bold");
        teamName.style.marginRight = 8;
        headerLeft.Add(teamName);

        var memberBadge = new Label();
        memberBadge.name = "member-badge";
        memberBadge.AddToClassList("badge");
        memberBadge.AddToClassList("badge--neutral");
        memberBadge.style.marginRight = 6;
        headerLeft.Add(memberBadge);

        var chemBadge = new Label();
        chemBadge.name = "chem-badge";
        chemBadge.AddToClassList("badge");
        headerLeft.Add(chemBadge);

        var headerRight = new VisualElement();
        headerRight.AddToClassList("flex-row");
        cardHeader.Add(headerRight);

        var assignBadge = new Label();
        assignBadge.name = "assign-badge";
        assignBadge.AddToClassList("badge");
        assignBadge.AddToClassList("badge--info");
        assignBadge.style.marginRight = 8;
        assignBadge.style.display = DisplayStyle.None;
        headerRight.Add(assignBadge);

        var typeBadge = new Label();
        typeBadge.name = "type-badge";
        typeBadge.AddToClassList("badge");
        typeBadge.style.marginRight = 8;
        headerRight.Add(typeBadge);

        var expandIcon = new Label("▶");
        expandIcon.name = "expand-icon";
        expandIcon.AddToClassList("metric-tertiary");
        headerRight.Add(expandIcon);

        cardHeader.RegisterCallback<ClickEvent>(OnCardHeaderClicked);

        // Body
        var cardBody = new VisualElement();
        cardBody.name = "card-body";
        cardBody.AddToClassList("team-card__body");
        cardBody.style.display = DisplayStyle.None;
        card.Add(cardBody);

        var divider = new VisualElement();
        divider.AddToClassList("divider");
        divider.style.marginTop = 8;
        divider.style.marginBottom = 8;
        cardBody.Add(divider);

        var detailsRow = new VisualElement();
        detailsRow.AddToClassList("flex-row");
        detailsRow.AddToClassList("justify-between");
        detailsRow.style.marginBottom = 8;
        cardBody.Add(detailsRow);

        var capacityLabel = new Label();
        capacityLabel.name = "capacity-label";
        capacityLabel.AddToClassList("metric-tertiary");
        detailsRow.Add(capacityLabel);

        var projectLabel = new Label();
        projectLabel.name = "project-label";
        projectLabel.AddToClassList("metric-tertiary");
        projectLabel.style.display = DisplayStyle.None;
        detailsRow.Add(projectLabel);

        var memberListContainer = new VisualElement();
        memberListContainer.name = "member-list";
        memberListContainer.AddToClassList("team-card__member-list");
        cardBody.Add(memberListContainer);

        var memberPool = new ElementPool(CreateMemberRow, memberListContainer);
        _memberPools.Add(memberPool);

        var memberEmpty = new Label("No members assigned.");
        memberEmpty.name = "member-empty";
        memberEmpty.AddToClassList("metric-tertiary");
        memberEmpty.style.display = DisplayStyle.None;
        cardBody.Add(memberEmpty);

        var actionRow = new VisualElement();
        actionRow.AddToClassList("flex-row");
        actionRow.AddToClassList("team-card__actions");
        actionRow.style.marginTop = 8;
        cardBody.Add(actionRow);

        var addMemberBtn = new Button();
        addMemberBtn.name = "add-member-btn";
        addMemberBtn.text = "+ Add Member";
        addMemberBtn.AddToClassList("btn-ghost");
        addMemberBtn.AddToClassList("btn-sm");
        actionRow.Add(addMemberBtn);
        addMemberBtn.RegisterCallback<ClickEvent>(OnAddMemberButtonClicked);

        var renameBtn = new Button();
        renameBtn.name = "rename-btn";
        renameBtn.text = "Rename";
        renameBtn.AddToClassList("btn-ghost");
        renameBtn.AddToClassList("btn-sm");
        renameBtn.style.marginLeft = 6;
        actionRow.Add(renameBtn);
        renameBtn.RegisterCallback<ClickEvent>(OnRenameTeamButtonClicked);

        var deleteBtn = new Button();
        deleteBtn.name = "delete-btn";
        deleteBtn.text = "Delete Team";
        deleteBtn.AddToClassList("btn-danger");
        deleteBtn.AddToClassList("btn-sm");
        deleteBtn.style.marginLeft = 6;
        actionRow.Add(deleteBtn);
        deleteBtn.RegisterCallback<ClickEvent>(OnDeleteTeamButtonClicked);

        // Store pool index on card for lookup during bind
        card.userData = poolIndex;

        return card;
    }

    private void BindTeamCard(VisualElement card, TeamCardDisplay data) {
        // poolIndex was stored in userData during Create; store after extracting
        int poolIndex = card.userData is int idx ? idx : 0;
        card.userData = data;

        var cardHeader = card.Q("card-header");
        if (cardHeader != null) cardHeader.userData = data.Id;

        var teamName = card.Q<Label>("team-name");
        if (teamName != null) teamName.text = data.Name;

        var memberBadge = card.Q<Label>("member-badge");
        if (memberBadge != null) memberBadge.text = data.MemberCount + (data.MemberCount == 1 ? " member" : " members");

        var chemBadge = card.Q<Label>("chem-badge");
        if (chemBadge != null) {
            chemBadge.text = data.ChemistryText;
            chemBadge.RemoveFromClassList("badge--success");
            chemBadge.RemoveFromClassList("badge--warning");
            chemBadge.RemoveFromClassList("badge--neutral");
            chemBadge.AddToClassList(data.ChemistryClass);
        }

        var assignBadge = card.Q<Label>("assign-badge");
        if (assignBadge != null) {
            assignBadge.text = data.AssignmentLabel;
            assignBadge.style.display = data.HasActiveAssignment ? DisplayStyle.Flex : DisplayStyle.None;
        }

        var typeBadge = card.Q<Label>("type-badge");
        if (typeBadge != null) {
            typeBadge.text = data.TeamTypeName;
            typeBadge.RemoveFromClassList("badge--role-development");
            typeBadge.RemoveFromClassList("badge--role-design");
            typeBadge.RemoveFromClassList("badge--role-qa");
            typeBadge.RemoveFromClassList("badge--role-negotiation");
            typeBadge.RemoveFromClassList("badge--role-hr");
            typeBadge.RemoveFromClassList("badge--neutral");
            typeBadge.AddToClassList(data.TeamTypeBadgeClass);
        }

        bool expanded = _expandedTeams.Contains(data.Id);
        var expandIcon = card.Q<Label>("expand-icon");
        if (expandIcon != null) expandIcon.text = expanded ? "▼" : "▶";

        var cardBody = card.Q("card-body");
        if (cardBody != null) {
            cardBody.style.display = expanded ? DisplayStyle.Flex : DisplayStyle.None;
            if (expanded) BindCardBody(cardBody, data, poolIndex);
        }

        var addMemberBtn = card.Q<Button>("add-member-btn");
        if (addMemberBtn != null) addMemberBtn.userData = data.Id;

        var renameBtn = card.Q<Button>("rename-btn");
        if (renameBtn != null) renameBtn.userData = data.Id;

        var deleteBtn = card.Q<Button>("delete-btn");
        if (deleteBtn != null) deleteBtn.userData = data;
    }

    private void BindCardBody(VisualElement cardBody, TeamCardDisplay data, int poolIndex) {
        var capacityLabel = cardBody.Q<Label>("capacity-label");
        if (capacityLabel != null) capacityLabel.text = "Capacity: " + data.EffectiveCapacity;

        var projectLabel = cardBody.Q<Label>("project-label");
        if (projectLabel != null) {
            bool hasProject = !string.IsNullOrEmpty(data.ActiveProjectName);
            projectLabel.text = data.ActiveProjectName;
            projectLabel.style.display = hasProject ? DisplayStyle.Flex : DisplayStyle.None;
        }

        bool hasMembers = data.Members != null && data.Members.Count > 0;
        var memberEmpty = cardBody.Q<Label>("member-empty");
        if (memberEmpty != null) memberEmpty.style.display = hasMembers ? DisplayStyle.None : DisplayStyle.Flex;

        if (poolIndex >= 0 && poolIndex < _memberPools.Count && data.Members != null) {
            _memberPools[poolIndex].UpdateList(data.Members, BindMemberRow);
        }
    }

    // --- Card header expand/collapse ---

    private void OnCardHeaderClicked(ClickEvent evt) {
        var header = evt.currentTarget as VisualElement;
        if (header == null || !(header.userData is TeamId teamId)) return;

        if (_expandedTeams.Contains(teamId)) {
            _expandedTeams.Remove(teamId);
        } else {
            _expandedTeams.Add(teamId);
        }

        if (_vm != null) _teamCardPool.UpdateList(_vm.Teams, BindTeamCard);
    }

    // --- Member rows ---

    private VisualElement CreateMemberRow() {
        var row = new VisualElement();
        row.AddToClassList("team-member-row");
        row.AddToClassList("flex-row");
        row.AddToClassList("justify-between");
        row.style.paddingTop = 4;
        row.style.paddingBottom = 4;
        row.style.marginBottom = 2;

        var nameGroup = new VisualElement();
        nameGroup.AddToClassList("flex-row");
        nameGroup.style.flexGrow = 1;

        var nameLabel = new Label();
        nameLabel.name = "member-name";
        nameLabel.AddToClassList("metric-secondary");
        nameLabel.style.marginRight = 8;
        nameGroup.Add(nameLabel);

        var roleLabel = new Label();
        roleLabel.name = "member-role";
        roleLabel.AddToClassList("badge");
        nameGroup.Add(roleLabel);

        row.Add(nameGroup);

        var rightGroup = new VisualElement();
        rightGroup.AddToClassList("flex-row");

        var typeLabel = new Label();
        typeLabel.name = "member-type";
        typeLabel.AddToClassList("metric-tertiary");
        typeLabel.style.marginRight = 8;
        rightGroup.Add(typeLabel);

        var moraleLabel = new Label();
        moraleLabel.name = "member-morale";
        moraleLabel.AddToClassList("metric-tertiary");
        moraleLabel.style.marginRight = 8;
        rightGroup.Add(moraleLabel);

        var removeBtn = new Button();
        removeBtn.name = "remove-btn";
        removeBtn.text = "Remove";
        removeBtn.AddToClassList("btn-sm");
        removeBtn.AddToClassList("btn-ghost");
        removeBtn.RegisterCallback<ClickEvent>(OnRemoveMemberButtonClicked);
        rightGroup.Add(removeBtn);

        row.Add(rightGroup);
        return row;
    }

    private void BindMemberRow(VisualElement el, TeamMemberDisplay data) {
        var nameLabel = el.Q<Label>("member-name");
        var roleLabel = el.Q<Label>("member-role");
        var typeLabel = el.Q<Label>("member-type");
        var moraleLabel = el.Q<Label>("member-morale");
        var removeBtn = el.Q<Button>("remove-btn");

        if (nameLabel != null) { nameLabel.text = data.Name; nameLabel.userData = data.Id; }

        if (roleLabel != null) {
            UIFormatting.ClearRolePillClasses(roleLabel);
            roleLabel.text = data.RoleName;
            roleLabel.AddToClassList(data.RolePillClass);
        }

        if (typeLabel != null) typeLabel.text = data.TypeText;

        if (moraleLabel != null) {
            moraleLabel.text = data.MoraleText;
            moraleLabel.RemoveFromClassList("text-success");
            moraleLabel.RemoveFromClassList("text-muted");
            moraleLabel.RemoveFromClassList("text-warning");
            moraleLabel.AddToClassList(data.MoraleClass);
        }

        if (removeBtn != null) removeBtn.userData = data.Id;
    }

    private void OnRemoveMemberButtonClicked(ClickEvent evt) {
        var btn = evt.currentTarget as Button;
        if (btn == null || !(btn.userData is EmployeeId empId)) return;

        TeamCardDisplay? teamCard = FindTeamForEmployee(empId);
        if (!teamCard.HasValue) return;

        if (teamCard.Value.HasActiveAssignment) {
            ShowConfirmation(
                "Remove from active team assigned to \"" + teamCard.Value.ActiveProjectName + "\"?",
                () => _dispatcher.Dispatch(new RemoveEmployeeFromTeamCommand { EmployeeId = empId })
            );
        } else {
            _dispatcher.Dispatch(new RemoveEmployeeFromTeamCommand { EmployeeId = empId });
        }
    }

    private TeamCardDisplay? FindTeamForEmployee(EmployeeId empId) {
        if (_vm == null) return null;
        var teams = _vm.Teams;
        int teamCount = teams.Count;
        for (int t = 0; t < teamCount; t++) {
            var members = teams[t].Members;
            if (members == null) continue;
            int memberCount = members.Count;
            for (int m = 0; m < memberCount; m++) {
                if (members[m].Id == empId) return teams[t];
            }
        }
        return null;
    }

    // --- Action button handlers ---

    private void OnAddMemberButtonClicked(ClickEvent evt) {
        var btn = evt.currentTarget as Button;
        if (btn == null || !(btn.userData is TeamId teamId)) return;
        _addMemberVm.SetTeamId(teamId);
        _modal.ShowModal(_addMemberView, _addMemberVm);
    }

    private void OnRenameTeamButtonClicked(ClickEvent evt) {
        var btn = evt.currentTarget as Button;
        if (btn == null || !(btn.userData is TeamId teamId)) return;

        string currentName = "";
        if (_vm != null) {
            var teams = _vm.Teams;
            int count = teams.Count;
            for (int i = 0; i < count; i++) {
                if (teams[i].Id == teamId) { currentName = teams[i].Name; break; }
            }
        }

        var renameView = new RenameTeamModalView(_dispatcher, _modal, teamId, currentName);
        _modal.ShowModal(renameView, null);
    }

    private void OnDeleteTeamButtonClicked(ClickEvent evt) {
        var btn = evt.currentTarget as Button;
        if (btn == null || !(btn.userData is TeamCardDisplay cardData)) return;

        string message = cardData.HasActiveAssignment
            ? "Delete team \"" + cardData.Name + "\"? Currently assigned to: " + cardData.ActiveProjectName + "."
            : "Delete team \"" + cardData.Name + "\"? This cannot be undone.";

        ShowConfirmation(message, () => _dispatcher.Dispatch(new DeleteTeamCommand { TeamId = cardData.Id }));
    }
}
