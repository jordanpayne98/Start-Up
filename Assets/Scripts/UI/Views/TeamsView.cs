using System.Collections.Generic;
using UnityEngine.UIElements;

public class TeamsView : IGameView
{
    private readonly ICommandDispatcher _dispatcher;
    private readonly IModalPresenter _modal;
    private readonly ITooltipProvider _tooltipProvider;
    private VisualElement _root;
    private VisualElement _teamListContainer;
    private VisualElement _teamDetailPanel;
    private ElementPool _teamPool;
    private ElementPool _memberPool;
    private ElementPool _unassignedPool;
    private TeamsViewModel _viewModel;

    // Detail panel elements
    private Label _detailName;
    private TextField _renameField;
    private Label _detailType;
    private Label _detailContract;
    private Label _assignLabel;
    private Button _assignContractBtn;
    private Button _unassignContractBtn;
    private VisualElement _assignContractFlyout;
    private VisualElement _memberListContainer;
    private Button _deleteTeamButton;

    // Transfer flyout
    private VisualElement _transferFlyout;
    private Label _transferFlyoutTitle;
    private VisualElement _transferTeamListContainer;
    private EmployeeId _transferTargetEmployeeId;

    // Create team grid
    private VisualElement _createButtonGrid;
    private bool _gridVisible;

    private static readonly string[] _teamBadgeClasses = {
        "badge--neutral", "badge--role-programming", "badge--role-design",
        "badge--role-sfx", "badge--role-vfx", "badge--role-negotiation",
        "badge--role-accountancy", "badge--role-hr", "badge--role-qa"
    };

    private static readonly string[] _rolePillClasses = {
        "role-pill--developer", "role-pill--designer", "role-pill--qa-engineer",
        "role-pill--hr-specialist", "role-pill--sfx-artist", "role-pill--vfx-artist",
        "role-pill--accountant", "role-pill--marketing-specialist", "role-pill--unknown"
    };

    private static readonly string[] _moraleBandClasses = {
        "morale-band--inspired", "morale-band--motivated", "morale-band--stable",
        "morale-band--unhappy", "morale-band--miserable", "morale-band--critical"
    };

    // Crunch toggle button
    private Button _crunchToggleBtn;

    // Add member flyout elements
    private VisualElement _addMemberFlyout;
    private VisualElement _unassignedListContainer;
    private TextField _memberFilterField;
    private bool _flyoutOpen;
    private string _roleFilter = "";

    // Empty state
    private VisualElement _teamsEmptyState;

    // Stagger scratch list — reused, never allocated in Bind
    private readonly List<VisualElement> _staggerScratch = new List<VisualElement>();

    // Stagger guard
    private bool _hasAnimatedIn;

    // Filtered unassigned scratch list — reused, never allocated in RefreshUnassignedPool
    private readonly List<EmployeeRowDisplay> _filteredScratch = new List<EmployeeRowDisplay>();

    // Track last selected team to detect selection change
    private TeamId? _lastSelectedTeamId;

    public TeamsView(ICommandDispatcher dispatcher, IModalPresenter modal, ITooltipProvider tooltipProvider) {
        _dispatcher = dispatcher;
        _modal = modal;
        _tooltipProvider = tooltipProvider;
    }

    public void Initialize(VisualElement root) {
        _root = root;

        var layout = new VisualElement();
        layout.AddToClassList("flex-row");
        layout.style.flexGrow = 1;

        // Left: Team list
        var leftPanel = new VisualElement();
        leftPanel.style.flexGrow = 1;
        leftPanel.style.flexBasis = 0;
        leftPanel.style.marginRight = 16;

        // Left header row: "Teams" label + "Create Team" button
        var leftHeader = new VisualElement();
        leftHeader.AddToClassList("flex-row");
        leftHeader.AddToClassList("justify-between");
        leftHeader.AddToClassList("align-center");
        leftHeader.style.marginBottom = 8;

        var title = new Label("Teams");
        title.AddToClassList("section-header");
        title.style.marginBottom = 0;
        leftHeader.Add(title);

        var createTeamButton = new Button { text = "+" };
        createTeamButton.AddToClassList("btn-primary");
        createTeamButton.clicked += OnCreateTeamClicked;
        leftHeader.Add(createTeamButton);

        leftPanel.Add(leftHeader);

        // Create button grid (hidden by default)
        _createButtonGrid = new VisualElement();
        _createButtonGrid.AddToClassList("team-create-grid");
        _createButtonGrid.style.display = DisplayStyle.None;

        var teamTypes = (TeamType[])System.Enum.GetValues(typeof(TeamType));
        for (int t = 0; t < teamTypes.Length; t++) {
            var type = teamTypes[t];
            var typeBtn = new Button { text = UIFormatting.FormatTeamType(type) };
            typeBtn.AddToClassList("btn-sm");
            typeBtn.AddToClassList("btn--team-type-" + type.ToString().ToLower());
            typeBtn.userData = type;
            typeBtn.RegisterCallback<ClickEvent>(OnCreateTypeClicked);
            _createButtonGrid.Add(typeBtn);
        }
        leftPanel.Add(_createButtonGrid);

        _teamsEmptyState = UICardHelper.CreateEmptyState("👥", "No teams created yet.");
        _teamsEmptyState.AddToClassList("empty-state--hidden");
        leftPanel.Add(_teamsEmptyState);

        var scrollView = new ScrollView();
        scrollView.style.flexGrow = 1;
        _teamListContainer = scrollView.contentContainer;
        _teamPool = new ElementPool(CreateTeamItem, _teamListContainer);
        leftPanel.Add(scrollView);

        layout.Add(leftPanel);

        // Right: Team details
        _teamDetailPanel = new VisualElement();
        _teamDetailPanel.style.flexGrow = 2;
        _teamDetailPanel.style.flexBasis = 0;
        _teamDetailPanel.AddToClassList("card");
        UICardHelper.ApplyBevel(_teamDetailPanel);
        UICardHelper.AddGradient(_teamDetailPanel);

        // Detail header row: name + delete button
        var detailHeader = new VisualElement();
        detailHeader.AddToClassList("flex-row");
        detailHeader.AddToClassList("justify-between");
        detailHeader.AddToClassList("align-center");
        detailHeader.style.marginBottom = 4;

        _detailName = new Label("Select a team");
        _detailName.AddToClassList("metric-primary");
        _detailName.RegisterCallback<ClickEvent>(OnDetailNameClicked);
        detailHeader.Add(_detailName);

        _renameField = new TextField();
        _renameField.AddToClassList("team-rename-input");
        _renameField.style.display = DisplayStyle.None;
        _renameField.RegisterCallback<FocusOutEvent>(OnRenameConfirm);
        _renameField.RegisterCallback<KeyDownEvent>(OnRenameKeyDown);
        detailHeader.Add(_renameField);

        _deleteTeamButton = new Button { text = "Delete Team" };
        _deleteTeamButton.AddToClassList("btn-danger");
        _deleteTeamButton.style.display = DisplayStyle.None;
        _deleteTeamButton.clicked += OnDeleteTeamClicked;
        detailHeader.Add(_deleteTeamButton);

        _teamDetailPanel.Add(detailHeader);

        var infoRow = new VisualElement();
        infoRow.AddToClassList("flex-row");
        infoRow.style.marginTop = 8;
        infoRow.style.marginBottom = 16;

        _detailType = new Label();
        _detailType.AddToClassList("badge");
        _detailType.style.marginRight = 8;
        infoRow.Add(_detailType);

        _teamDetailPanel.Add(infoRow);

        _detailContract = new Label();
        _detailContract.AddToClassList("metric-tertiary");
        _detailContract.style.marginBottom = 4;
        _detailContract.SetRichTooltip("team.assignment", _tooltipProvider.TooltipService);
        _teamDetailPanel.Add(_detailContract);

        // Contract/product assign row
        var assignRow = new VisualElement();
        assignRow.AddToClassList("flex-row");
        assignRow.AddToClassList("align-center");
        assignRow.style.marginBottom = 12;

        _assignLabel = new Label();
        _assignLabel.AddToClassList("metric-tertiary");
        _assignLabel.style.flexGrow = 1;
        assignRow.Add(_assignLabel);

        _assignContractBtn = new Button { text = "Assign Work" };
        _assignContractBtn.AddToClassList("btn-sm");
        _assignContractBtn.AddToClassList("btn-secondary");
        _assignContractBtn.style.marginRight = 4;
        _assignContractBtn.RegisterCallback<ClickEvent>(OnAssignContractClicked);
        assignRow.Add(_assignContractBtn);

        _unassignContractBtn = new Button { text = "Unassign" };
        _unassignContractBtn.AddToClassList("btn-sm");
        _unassignContractBtn.AddToClassList("btn-danger");
        _unassignContractBtn.RegisterCallback<ClickEvent>(OnUnassignContractClicked);
        assignRow.Add(_unassignContractBtn);

        _crunchToggleBtn = new Button { text = "Enable Crunch" };
        _crunchToggleBtn.AddToClassList("btn-sm");
        _crunchToggleBtn.AddToClassList("btn-secondary");
        _crunchToggleBtn.style.marginLeft = 4;
        _crunchToggleBtn.clicked += OnCrunchToggleClicked;
        assignRow.Add(_crunchToggleBtn);

        _teamDetailPanel.Add(assignRow);

        // Assign work flyout (hidden by default)
        _assignContractFlyout = new VisualElement();
        _assignContractFlyout.AddToClassList("card");
        _assignContractFlyout.style.marginBottom = 8;
        _assignContractFlyout.style.display = DisplayStyle.None;
        _teamDetailPanel.Add(_assignContractFlyout);

        // Transfer flyout (hidden, shared)
        _transferFlyout = new VisualElement();
        _transferFlyout.AddToClassList("card");
        _transferFlyout.style.marginBottom = 8;
        _transferFlyout.style.display = DisplayStyle.None;

        _transferFlyoutTitle = new Label("Transfer to:");
        _transferFlyoutTitle.AddToClassList("metric-tertiary");
        _transferFlyoutTitle.style.marginBottom = 4;
        _transferFlyout.Add(_transferFlyoutTitle);

        _transferTeamListContainer = new VisualElement();
        _transferFlyout.Add(_transferTeamListContainer);

        _teamDetailPanel.Add(_transferFlyout);

        // Members header row: "Members" + "Add Member" button
        var membersHeader = new VisualElement();
        membersHeader.AddToClassList("flex-row");
        membersHeader.AddToClassList("justify-between");
        membersHeader.AddToClassList("align-center");
        membersHeader.style.marginBottom = 8;

        var membersTitle = new Label("Members");
        membersTitle.AddToClassList("metric-secondary");
        membersTitle.style.marginBottom = 0;
        membersHeader.Add(membersTitle);

        var addMemberButton = new Button { text = "Add Member" };
        addMemberButton.AddToClassList("btn-secondary");
        addMemberButton.clicked += OnAddMemberClicked;
        membersHeader.Add(addMemberButton);

        var removeAllButton = new Button { text = "Remove All" };
        removeAllButton.AddToClassList("btn-sm");
        removeAllButton.AddToClassList("btn-danger");
        removeAllButton.clicked += OnRemoveAllClicked;
        membersHeader.Add(removeAllButton);

        _teamDetailPanel.Add(membersHeader);

        // Add member flyout (hidden by default)
        _addMemberFlyout = new VisualElement();
        _addMemberFlyout.AddToClassList("card");
        _addMemberFlyout.style.marginBottom = 8;
        _addMemberFlyout.style.display = DisplayStyle.None;

        _memberFilterField = new TextField();
        _memberFilterField.style.marginBottom = 4;
        _memberFilterField.RegisterValueChangedCallback(OnMemberFilterChanged);
        _addMemberFlyout.Add(_memberFilterField);

        // Role filter buttons
        var roleFilterRow = new VisualElement();
        roleFilterRow.AddToClassList("flex-row");
        roleFilterRow.style.marginBottom = 4;
        roleFilterRow.style.flexWrap = Wrap.Wrap;

        var roleFilters = new[] { "All", "Dev", "Des", "QA", "HR", "SFX", "VFX", "Acc", "Mkt" };
        for (int r = 0; r < roleFilters.Length; r++) {
            string filter = roleFilters[r];
            var filterBtn = new Button { text = filter };
            filterBtn.AddToClassList("btn-sm");
            filterBtn.AddToClassList("btn-secondary");
            filterBtn.style.marginRight = 2;
            filterBtn.style.marginBottom = 2;
            filterBtn.clicked += () => OnRoleFilterClicked(filter);
            roleFilterRow.Add(filterBtn);
        }
        _addMemberFlyout.Add(roleFilterRow);

        // Add All button
        var addAllRow = new VisualElement();
        addAllRow.AddToClassList("flex-row");
        addAllRow.AddToClassList("justify-end");
        addAllRow.style.marginBottom = 4;

        var addAllBtn = new Button { text = "Add All Visible" };
        addAllBtn.AddToClassList("btn-sm");
        addAllBtn.AddToClassList("btn-primary");
        addAllBtn.clicked += OnAddAllClicked;
        addAllRow.Add(addAllBtn);
        _addMemberFlyout.Add(addAllRow);

        var flyoutScroll = new ScrollView();
        flyoutScroll.style.maxHeight = 160;
        _unassignedListContainer = flyoutScroll.contentContainer;
        _unassignedPool = new ElementPool(CreateUnassignedRow, _unassignedListContainer);
        _addMemberFlyout.Add(flyoutScroll);

        _teamDetailPanel.Add(_addMemberFlyout);

        _memberListContainer = new VisualElement();
        _memberPool = new ElementPool(CreateMemberRow, _memberListContainer);
        _teamDetailPanel.Add(_memberListContainer);

        layout.Add(_teamDetailPanel);
        _root.Add(layout);
    }

    public void Bind(IViewModel viewModel) {
        _viewModel = viewModel as TeamsViewModel;
        if (_viewModel == null) return;

        _teamPool.UpdateList(_viewModel.Teams, BindTeamItem);

        bool hasTeams = _viewModel.Teams != null && _viewModel.Teams.Count > 0;
        if (_teamsEmptyState != null) {
            if (hasTeams) _teamsEmptyState.AddToClassList("empty-state--hidden");
            else _teamsEmptyState.RemoveFromClassList("empty-state--hidden");
        }

        // Stagger on first bind
        if (!_hasAnimatedIn && hasTeams) {
            _hasAnimatedIn = true;
            _staggerScratch.Clear();
            int childCount = _teamListContainer.childCount;
            for (int i = 0; i < childCount; i++) {
                var el = _teamListContainer[i];
                if (el.style.display != DisplayStyle.None) _staggerScratch.Add(el);
            }
            UIAnimator.StaggerIn(_staggerScratch);
        }

        // Update detail panel
        if (_viewModel.SelectedTeamId.HasValue) {
            // Detect selection change → slide panel in
            bool selectionChanged = !_lastSelectedTeamId.HasValue
                || _lastSelectedTeamId.Value.Value != _viewModel.SelectedTeamId.Value.Value;
            if (selectionChanged) {
                UIAnimator.DetailPanelSlide(_teamDetailPanel, true);
                _lastSelectedTeamId = _viewModel.SelectedTeamId;
                // Close flyouts on team change
                if (_assignContractFlyout != null) _assignContractFlyout.style.display = DisplayStyle.None;
                if (_transferFlyout != null) _transferFlyout.style.display = DisplayStyle.None;
            }

            var detail = _viewModel.SelectedTeam;
            _detailName.text = detail.Name;
            _detailName.style.display = DisplayStyle.Flex;
            if (_renameField != null) _renameField.style.display = DisplayStyle.None;

            _detailType.text = detail.TeamType;
            for (int b = 0; b < _teamBadgeClasses.Length; b++)
                _detailType.RemoveFromClassList(_teamBadgeClasses[b]);
            _detailType.AddToClassList(UIFormatting.TeamTypeBadgeClass(detail.TeamTypeEnum));
            _detailContract.text = "Contract: " + detail.ContractName;

            bool hasContract = detail.AssignedContractId.HasValue;
            bool hasProduct = _viewModel.AssignedProductId.HasValue;
            bool isAssigned = hasContract || hasProduct;

            if (_assignLabel != null) {
                if (hasProduct && _viewModel.AssignedProductId.HasValue) {
                    var products = _viewModel.AvailableProducts;
                    string productName = "Product";
                    int pc = products.Count;
                    for (int p = 0; p < pc; p++) {
                        if (products[p].Id == _viewModel.AssignedProductId.Value) {
                            productName = products[p].Name;
                            break;
                        }
                    }
                    _assignLabel.text = "Product: " + productName;
                } else {
                    _assignLabel.text = "";
                }
            }
            if (_assignContractBtn != null) _assignContractBtn.style.display = isAssigned ? DisplayStyle.None : DisplayStyle.Flex;
            if (_unassignContractBtn != null) _unassignContractBtn.style.display = isAssigned ? DisplayStyle.Flex : DisplayStyle.None;

            if (_crunchToggleBtn != null) {
                _crunchToggleBtn.style.display = isAssigned ? DisplayStyle.Flex : DisplayStyle.None;
                if (detail.IsCrunching) {
                    _crunchToggleBtn.text = "Disable Crunch";
                    _crunchToggleBtn.AddToClassList("btn--active");
                } else {
                    _crunchToggleBtn.text = "Enable Crunch";
                    _crunchToggleBtn.RemoveFromClassList("btn--active");
                }
            }

            _deleteTeamButton.style.display = DisplayStyle.Flex;

            if (detail.Members != null) {
                _memberPool.UpdateList(detail.Members, BindMemberRow);
            }

            // Refresh flyout if open
            if (_flyoutOpen) {
                RefreshUnassignedPool();
            }
        } else {
            _deleteTeamButton.style.display = DisplayStyle.None;
            if (_assignContractBtn != null) _assignContractBtn.style.display = DisplayStyle.None;
            if (_unassignContractBtn != null) _unassignContractBtn.style.display = DisplayStyle.None;
            if (_assignLabel != null) _assignLabel.text = "";
        }
    }

    public void Dispose() {
        _detailContract?.ClearTooltip(_tooltipProvider.TooltipService);
        if (_crunchToggleBtn != null) {
            _crunchToggleBtn.clicked -= OnCrunchToggleClicked;
            _crunchToggleBtn = null;
        }
        _hasAnimatedIn = false;
        _gridVisible = false;
        _flyoutOpen = false;
        _roleFilter = "";
        _lastSelectedTeamId = null;
        _staggerScratch.Clear();
        _filteredScratch.Clear();
        _viewModel = null;
        _teamPool = null;
        _memberPool = null;
        _unassignedPool = null;
    }

    private void OnCreateTeamClicked() {
        _gridVisible = !_gridVisible;
        if (_createButtonGrid != null) {
            _createButtonGrid.style.display = _gridVisible ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }

    private void OnCreateTypeClicked(ClickEvent evt) {
        var btn = evt.currentTarget as Button;
        if (btn == null) return;
        if (btn.userData is TeamType type) {
            _dispatcher.Dispatch(new CreateTeamCommand {
                Tick = _dispatcher.CurrentTick,
                TeamType = type
            });
        }
        _gridVisible = false;
        if (_createButtonGrid != null) _createButtonGrid.style.display = DisplayStyle.None;
    }

    private void OnDetailNameClicked(ClickEvent evt) {
        if (_viewModel == null || !_viewModel.SelectedTeamId.HasValue) return;
        if (_detailName != null) _detailName.style.display = DisplayStyle.None;
        if (_renameField != null) {
            _renameField.style.display = DisplayStyle.Flex;
            _renameField.value = _viewModel.SelectedTeam.Name;
            _renameField.Focus();
        }
    }

    private void OnRenameConfirm(FocusOutEvent evt) {
        if (_renameField == null || !_viewModel.SelectedTeamId.HasValue) return;
        string newName = _renameField.value;
        if (!string.IsNullOrWhiteSpace(newName) && newName != _viewModel.SelectedTeam.Name) {
            _dispatcher?.Dispatch(new RenameTeamCommand {
                Tick = _dispatcher.CurrentTick,
                TeamId = _viewModel.SelectedTeamId.Value,
                NewName = newName
            });
        }
        if (_renameField != null) _renameField.style.display = DisplayStyle.None;
        if (_detailName != null) _detailName.style.display = DisplayStyle.Flex;
    }

    private void OnRenameKeyDown(KeyDownEvent evt) {
        if (evt.keyCode == UnityEngine.KeyCode.Return || evt.keyCode == UnityEngine.KeyCode.KeypadEnter) {
            _renameField?.Blur();
        } else if (evt.keyCode == UnityEngine.KeyCode.Escape) {
            if (_renameField != null) _renameField.style.display = DisplayStyle.None;
            if (_detailName != null) _detailName.style.display = DisplayStyle.Flex;
        }
    }

    private void OnAssignContractClicked(ClickEvent evt) {
        if (_viewModel == null || !_viewModel.SelectedTeamId.HasValue) return;
        if (_assignContractFlyout == null) return;
        bool isVisible = _assignContractFlyout.style.display == DisplayStyle.Flex;
        if (isVisible) {
            _assignContractFlyout.style.display = DisplayStyle.None;
            return;
        }
        _assignContractFlyout.Clear();
        var teamId = _viewModel.SelectedTeamId.Value;

        var contracts = _viewModel.AvailableContracts;
        int cc = contracts.Count;
        for (int c = 0; c < cc; c++) {
            var contractData = contracts[c];
            var btn = new Button { text = contractData.Name };
            btn.AddToClassList("btn-sm");
            btn.AddToClassList("btn-secondary");
            btn.style.marginBottom = 2;
            var capturedContractId = contractData.Id;
            btn.RegisterCallback<ClickEvent>(ce => {
                _dispatcher?.Dispatch(new AssignTeamToContractCommand {
                    Tick = _dispatcher.CurrentTick,
                    ContractId = capturedContractId,
                    TeamId = teamId
                });
                if (_assignContractFlyout != null) _assignContractFlyout.style.display = DisplayStyle.None;
            });
            _assignContractFlyout.Add(btn);
        }

        var products = _viewModel.AvailableProducts;
        int pc = products.Count;
        for (int p = 0; p < pc; p++) {
            var productData = products[p];
            var btn = new Button { text = productData.Name + " (Product)" };
            btn.AddToClassList("btn-sm");
            btn.AddToClassList("btn-secondary");
            btn.style.marginBottom = 2;
            var capturedProductId = productData.Id;
            btn.RegisterCallback<ClickEvent>(pe => {
                _dispatcher?.Dispatch(new AssignTeamToProductCommand {
                    Tick = _dispatcher.CurrentTick,
                    ProductId = capturedProductId,
                    TeamId = teamId,
                    RoleSlot = ProductTeamRole.Programming
                });
                if (_assignContractFlyout != null) _assignContractFlyout.style.display = DisplayStyle.None;
            });
            _assignContractFlyout.Add(btn);
        }

        if (contracts.Count == 0 && products.Count == 0) {
            var empty = new Label("No work items available");
            empty.AddToClassList("metric-tertiary");
            _assignContractFlyout.Add(empty);
        }

        _assignContractFlyout.style.display = DisplayStyle.Flex;
    }

    private void OnUnassignContractClicked(ClickEvent evt) {
        if (_viewModel == null || !_viewModel.SelectedTeamId.HasValue) return;
        var detail = _viewModel.SelectedTeam;
        if (detail.AssignedContractId.HasValue) {
            _dispatcher?.Dispatch(new UnassignTeamFromContractCommand {
                Tick = _dispatcher.CurrentTick,
                ContractId = detail.AssignedContractId.Value
            });
        } else if (_viewModel.AssignedProductId.HasValue) {
            _dispatcher?.Dispatch(new UnassignTeamFromProductCommand {
                Tick = _dispatcher.CurrentTick,
                ProductId = _viewModel.AssignedProductId.Value,
                TeamId = _viewModel.SelectedTeamId.Value
            });
        }
    }

    private void OnTransferClicked(ClickEvent evt) {
        var btn = evt.currentTarget as Button;
        if (btn == null || _transferFlyout == null) return;
        if (!(btn.userData is EmployeeId employeeId)) return;
        _transferTargetEmployeeId = employeeId;

        bool isVisible = _transferFlyout.style.display == DisplayStyle.Flex
            && _transferTargetEmployeeId == employeeId;

        _transferTeamListContainer.Clear();
        var other = _viewModel.OtherTeams;
        int count = other.Count;
        for (int i = 0; i < count; i++) {
            var teamData = other[i];
            var teamBtn = new Button { text = teamData.Name };
            teamBtn.AddToClassList("btn-sm");
            teamBtn.AddToClassList("btn-secondary");
            teamBtn.style.marginBottom = 2;
            var capturedTeamId = teamData.Id;
            var capturedEmpId = employeeId;
            teamBtn.RegisterCallback<ClickEvent>(te => {
                _dispatcher?.Dispatch(new AssignEmployeeToTeamCommand {
                    Tick = _dispatcher.CurrentTick,
                    EmployeeId = capturedEmpId,
                    TeamId = capturedTeamId
                });
                if (_transferFlyout != null) _transferFlyout.style.display = DisplayStyle.None;
            });
            _transferTeamListContainer.Add(teamBtn);
        }

        if (count == 0) {
            var empty = new Label("No other teams");
            empty.AddToClassList("metric-tertiary");
            _transferTeamListContainer.Add(empty);
        }

        _transferFlyout.style.display = isVisible ? DisplayStyle.None : DisplayStyle.Flex;
    }

    private void OnDeleteTeamClicked() {
        if (_viewModel == null || _dispatcher == null) return;
        if (!_viewModel.SelectedTeamId.HasValue) return;
        _dispatcher.Dispatch(new DeleteTeamCommand {
            Tick = _dispatcher.CurrentTick,
            TeamId = _viewModel.SelectedTeamId.Value
        });
    }

    private void OnCrunchToggleClicked() {
        if (_viewModel == null || _dispatcher == null) return;
        if (!_viewModel.SelectedTeamId.HasValue) return;
        _dispatcher.Dispatch(new SetCrunchModeCommand {
            Tick = _dispatcher.CurrentTick,
            TeamId = _viewModel.SelectedTeamId.Value,
            Enable = !_viewModel.SelectedTeam.IsCrunching
        });
    }

    private void OnAddMemberClicked() {
        _flyoutOpen = !_flyoutOpen;
        if (_addMemberFlyout != null) {
            _addMemberFlyout.style.display = _flyoutOpen ? DisplayStyle.Flex : DisplayStyle.None;
        }
        if (_flyoutOpen) {
            if (_memberFilterField != null) _memberFilterField.value = "";
            if (_viewModel != null) _viewModel.SetMemberFilter("");
            _roleFilter = "";
            RefreshUnassignedPool();
        }
    }

    private void OnMemberFilterChanged(ChangeEvent<string> evt) {
        _viewModel?.SetMemberFilter(evt.newValue);
        RefreshUnassignedPool();
    }

    private void OnRoleFilterClicked(string filter) {
        _roleFilter = filter == "All" ? "" : filter;
        RefreshUnassignedPool();
    }

    private void OnAddAllClicked() {
        if (_viewModel == null || _dispatcher == null) return;
        if (!_viewModel.SelectedTeamId.HasValue) return;
        var teamId = _viewModel.SelectedTeamId.Value;
        var visible = GetFilteredUnassigned();
        int count = visible.Count;
        for (int i = 0; i < count; i++) {
            _dispatcher.Dispatch(new AssignEmployeeToTeamCommand {
                Tick = _dispatcher.CurrentTick,
                EmployeeId = visible[i].Id,
                TeamId = teamId
            });
        }
    }

    private void OnRemoveAllClicked() {
        if (_viewModel == null || _dispatcher == null) return;
        if (!_viewModel.SelectedTeamId.HasValue) return;
        var members = _viewModel.SelectedTeam.Members;
        if (members == null) return;
        int count = members.Count;
        for (int i = 0; i < count; i++) {
            _dispatcher.Dispatch(new RemoveEmployeeFromTeamCommand {
                Tick = _dispatcher.CurrentTick,
                EmployeeId = members[i].EmployeeId
            });
        }
    }

    private List<EmployeeRowDisplay> GetFilteredUnassigned() {
        var unassigned = _viewModel.UnassignedEmployees;
        var filterText = _viewModel.MemberFilterText;
        _filteredScratch.Clear();
        int count = unassigned.Count;
        for (int i = 0; i < count; i++) {
            var emp = unassigned[i];
            if (!string.IsNullOrEmpty(_roleFilter) && !MatchesRoleFilter(emp.Role, _roleFilter))
                continue;
            if (!string.IsNullOrEmpty(filterText)
                && emp.Name.IndexOf(filterText, System.StringComparison.OrdinalIgnoreCase) < 0
                && emp.Role.IndexOf(filterText, System.StringComparison.OrdinalIgnoreCase) < 0)
                continue;
            _filteredScratch.Add(emp);
        }
        return _filteredScratch;
    }

    private static bool MatchesRoleFilter(string role, string filter) {
        switch (filter) {
            case "Dev": return role.IndexOf("Developer", System.StringComparison.OrdinalIgnoreCase) >= 0;
            case "Des": return role.IndexOf("Designer", System.StringComparison.OrdinalIgnoreCase) >= 0;
            case "QA":  return role.IndexOf("QA", System.StringComparison.OrdinalIgnoreCase) >= 0;
            case "HR":  return role.IndexOf("HR", System.StringComparison.OrdinalIgnoreCase) >= 0;
            case "SFX": return role.IndexOf("Sound", System.StringComparison.OrdinalIgnoreCase) >= 0;
            case "VFX": return role.IndexOf("VFX", System.StringComparison.OrdinalIgnoreCase) >= 0;
            case "Acc": return role.IndexOf("Account", System.StringComparison.OrdinalIgnoreCase) >= 0;
            case "Mkt": return role.IndexOf("Market", System.StringComparison.OrdinalIgnoreCase) >= 0;
            default: return true;
        }
    }

    private void RefreshUnassignedPool() {
        if (_viewModel == null || _unassignedPool == null) return;
        var filtered = GetFilteredUnassigned();
        _unassignedPool.UpdateList(filtered, BindUnassignedRow);
    }

    private VisualElement CreateTeamItem() {
        var item = new VisualElement();
        item.AddToClassList("list-item");
        item.AddToClassList("flex-col");
        item.style.paddingTop = 8;
        item.style.paddingBottom = 8;

        var nameRow = new VisualElement();
        nameRow.AddToClassList("flex-row");
        nameRow.AddToClassList("align-center");

        var nameLabel = new Label();
        nameLabel.name = "team-name";
        nameLabel.AddToClassList("metric-secondary");
        nameLabel.style.flexGrow = 1;
        nameRow.Add(nameLabel);

        var crunchBadge = new Label("CRUNCH");
        crunchBadge.name = "team-crunch-badge";
        crunchBadge.AddToClassList("badge");
        crunchBadge.AddToClassList("chip--crunch");
        crunchBadge.style.display = DisplayStyle.None;
        nameRow.Add(crunchBadge);

        item.Add(nameRow);

        var typeLabel = new Label();
        typeLabel.name = "team-type";
        typeLabel.AddToClassList("badge");
        item.Add(typeLabel);

        var infoRow = new VisualElement();
        infoRow.AddToClassList("flex-row");
        infoRow.AddToClassList("justify-between");
        infoRow.style.marginTop = 4;

        var membersLabel = new Label();
        membersLabel.name = "team-members";
        membersLabel.AddToClassList("metric-tertiary");
        infoRow.Add(membersLabel);

        var contractLabel = new Label();
        contractLabel.name = "team-contract";
        contractLabel.AddToClassList("metric-tertiary");
        infoRow.Add(contractLabel);

        item.Add(infoRow);
        return item;
    }

    private void BindTeamItem(VisualElement el, TeamSummaryDisplay data) {
        el.Q<Label>("team-name").text = data.Name;
        el.Q<Label>("team-members").text = data.MemberCount + " members";
        el.Q<Label>("team-contract").text = data.ContractName;

        var typeLabel = el.Q<Label>("team-type");
        typeLabel.text = data.TeamType;
        for (int b = 0; b < _teamBadgeClasses.Length; b++)
            typeLabel.RemoveFromClassList(_teamBadgeClasses[b]);
        typeLabel.AddToClassList(UIFormatting.TeamTypeBadgeClass(data.TeamTypeEnum));

        var crunchBadge = el.Q<Label>("team-crunch-badge");
        if (crunchBadge != null)
            crunchBadge.style.display = data.IsCrunching ? DisplayStyle.Flex : DisplayStyle.None;

        if (_viewModel != null && _viewModel.SelectedTeamId.HasValue
            && _viewModel.SelectedTeamId.Value == data.Id) {
            el.AddToClassList("list-item--selected");
        } else {
            el.RemoveFromClassList("list-item--selected");
        }

        if (el.userData is EventCallback<ClickEvent> prevClick)
            el.UnregisterCallback(prevClick);
        EventCallback<ClickEvent> onClick = evt => {
            _viewModel?.SelectTeam(data.Id);
            Bind(_viewModel);
        };
        el.userData = onClick;
        el.RegisterCallback(onClick);
    }

    private VisualElement CreateMemberRow() {
        var row = new VisualElement();
        row.AddToClassList("list-item");

        var nameLabel = new Label();
        nameLabel.name = "member-name";
        nameLabel.AddToClassList("metric-secondary");
        nameLabel.AddToClassList("member-name--clickable");
        nameLabel.style.flexGrow = 2;
        nameLabel.style.cursor = StyleKeyword.Auto;
        row.Add(nameLabel);

        var moraleBandLabel = new Label();
        moraleBandLabel.name = "member-morale-band";
        moraleBandLabel.AddToClassList("morale-band-label");
        moraleBandLabel.style.flexGrow = 0;
        moraleBandLabel.style.flexShrink = 0;
        moraleBandLabel.style.alignSelf = Align.Center;
        moraleBandLabel.style.marginRight = 6;
        row.Add(moraleBandLabel);

        var roleLabel = new Label();
        roleLabel.name = "member-role";
        roleLabel.AddToClassList("role-pill");
        roleLabel.style.flexGrow = 0;
        roleLabel.style.flexShrink = 0;
        roleLabel.style.alignSelf = Align.Center;
        row.Add(roleLabel);

        var transferBtn = new Button { text = "Transfer" };
        transferBtn.name = "member-transfer";
        transferBtn.AddToClassList("btn-sm");
        transferBtn.AddToClassList("btn-secondary");
        row.Add(transferBtn);

        var removeBtn = new Button { text = "Remove" };
        removeBtn.name = "member-remove";
        removeBtn.AddToClassList("btn-sm");
        removeBtn.AddToClassList("btn-danger");
        row.Add(removeBtn);

        return row;
    }

    private void BindMemberRow(VisualElement el, TeamMemberDisplay data) {
        var nameLabel = el.Q<Label>("member-name");
        nameLabel.text = data.Name;

        var roleLabel = el.Q<Label>("member-role");
        roleLabel.text = data.Role;
        for (int rp = 0; rp < _rolePillClasses.Length; rp++)
            roleLabel.RemoveFromClassList(_rolePillClasses[rp]);
        roleLabel.AddToClassList(UIFormatting.RolePillClass(data.Role));

        var moraleBandLabel = el.Q<Label>("member-morale-band");
        if (moraleBandLabel != null) {
            moraleBandLabel.text = data.Morale + " - " + data.MoraleBand;
            for (int c = 0; c < _moraleBandClasses.Length; c++)
                moraleBandLabel.RemoveFromClassList(_moraleBandClasses[c]);
            if (!string.IsNullOrEmpty(data.MoraleBandClass))
                moraleBandLabel.AddToClassList(data.MoraleBandClass);
        }

        var transferBtn = el.Q<Button>("member-transfer");
        if (transferBtn != null) {
            transferBtn.userData = data.EmployeeId;
            transferBtn.UnregisterCallback<ClickEvent>(OnTransferClicked);
            transferBtn.RegisterCallback<ClickEvent>(OnTransferClicked);
        }

        // Click name to open employee profile modal
        if (nameLabel.userData is EventCallback<ClickEvent> prevNameClick)
            nameLabel.UnregisterCallback(prevNameClick);
        EventCallback<ClickEvent> onNameClick = evt => {
            evt.StopPropagation();
            var profileVM = new EmployeeProfileViewModel();
            profileVM.SetEmployee(data.EmployeeId);
            _modal?.ShowModal(new EmployeeProfileView(_dispatcher, _modal), profileVM);
        };
        nameLabel.userData = onNameClick;
        nameLabel.RegisterCallback(onNameClick);

        var removeBtn = el.Q<Button>("member-remove");
        if (removeBtn != null) {
            if (removeBtn.userData is System.Action prevRemove)
                removeBtn.clicked -= prevRemove;
            var capturedEmployeeId = data.EmployeeId;
            System.Action onRemove = () => {
                _dispatcher?.Dispatch(new RemoveEmployeeFromTeamCommand {
                    Tick = _dispatcher.CurrentTick,
                    EmployeeId = capturedEmployeeId
                });
            };
            removeBtn.userData = onRemove;
            removeBtn.clicked += onRemove;
        }
    }

    private VisualElement CreateUnassignedRow() {
        var row = new VisualElement();
        row.AddToClassList("list-item");

        var nameLabel = new Label();
        nameLabel.name = "unassigned-name";
        nameLabel.AddToClassList("metric-secondary");
        nameLabel.style.flexGrow = 2;
        row.Add(nameLabel);

        var roleLabel = new Label();
        roleLabel.name = "unassigned-role";
        roleLabel.AddToClassList("role-pill");
        roleLabel.style.flexGrow = 0;
        roleLabel.style.flexShrink = 0;
        roleLabel.style.alignSelf = Align.Center;
        roleLabel.style.marginRight = 8;
        row.Add(roleLabel);

        var addBtn = new Button { text = "Add" };
        addBtn.name = "unassigned-add";
        addBtn.AddToClassList("btn-sm");
        addBtn.AddToClassList("btn-primary");
        row.Add(addBtn);

        return row;
    }

    private void BindUnassignedRow(VisualElement el, EmployeeRowDisplay data) {
        el.Q<Label>("unassigned-name").text = data.Name;

        var roleLabel = el.Q<Label>("unassigned-role");
        roleLabel.text = data.Role;
        for (int rp = 0; rp < _rolePillClasses.Length; rp++)
            roleLabel.RemoveFromClassList(_rolePillClasses[rp]);
        roleLabel.AddToClassList(UIFormatting.RolePillClass(data.Role));

        var addBtn = el.Q<Button>("unassigned-add");
        if (addBtn != null && _viewModel != null && _viewModel.SelectedTeamId.HasValue) {
            if (addBtn.userData is System.Action prevAdd)
                addBtn.clicked -= prevAdd;
            var capturedEmployeeId = data.Id;
            var capturedTeamId = _viewModel.SelectedTeamId.Value;
            System.Action onAdd = () => {
                _dispatcher?.Dispatch(new AssignEmployeeToTeamCommand {
                    Tick = _dispatcher.CurrentTick,
                    EmployeeId = capturedEmployeeId,
                    TeamId = capturedTeamId
                });
            };
            addBtn.userData = onAdd;
            addBtn.clicked += onAdd;
        }
    }
}
