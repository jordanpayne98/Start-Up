using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

public class AddMemberModalView : IGameView
{
    private readonly IModalPresenter _modal;
    private readonly ICommandDispatcher _dispatcher;

    private VisualElement _root;
    private Label _titleLabel;
    private VisualElement _listContainer;
    private ElementPool _listPool;
    private VisualElement _emptyState;
    private Button _doneBtn;

    private VisualElement _filterRow;
    private Button[] _filterButtons;
    private readonly HashSet<RoleId> _activeFilters = new HashSet<RoleId>();
    private readonly HashSet<EmployeeId> _selectedIds = new HashSet<EmployeeId>();
    private readonly List<AvailableEmployeeDisplay> _filteredList = new List<AvailableEmployeeDisplay>(16);
    private Button _addSelectedBtn;
    private Label _selectionCountLabel;

    private AddMemberModalViewModel _vm;

    public AddMemberModalView(ICommandDispatcher dispatcher, IModalPresenter modal) {
        _dispatcher = dispatcher;
        _modal = modal;
    }

    public void Initialize(VisualElement root) {
        _root = root;
        _root.AddToClassList("add-member-modal");

        _titleLabel = new Label();
        _titleLabel.AddToClassList("modal-title");
        _titleLabel.AddToClassList("text-bold");
        _titleLabel.style.marginBottom = 4;
        _root.Add(_titleLabel);

        var subtitle = new Label("Showing unassigned employees");
        subtitle.AddToClassList("metric-tertiary");
        subtitle.style.marginBottom = 8;
        _root.Add(subtitle);

        _filterRow = new VisualElement();
        _filterRow.AddToClassList("flex-row");
        _filterRow.style.flexWrap = Wrap.Wrap;
        _filterRow.style.marginBottom = 8;
        _root.Add(_filterRow);

        var roleValues = (RoleId[])Enum.GetValues(typeof(RoleId));
        _filterButtons = new Button[roleValues.Length];
        for (int i = 0; i < roleValues.Length; i++) {
            var role = roleValues[i];
            var btn = new Button();
            btn.text = RoleIdHelper.GetName(role);
            btn.userData = role;
            btn.AddToClassList("filter-pill");
            btn.AddToClassList(UIFormatting.RolePillClass(role));
            btn.AddToClassList("filter-pill--active");
            btn.style.marginRight = 4;
            btn.style.marginBottom = 4;
            btn.RegisterCallback<ClickEvent>(OnFilterClicked);
            _filterRow.Add(btn);
            _filterButtons[i] = btn;
            _activeFilters.Add(role);
        }

        var scroll = new ScrollView();
        scroll.style.flexGrow = 1;
        scroll.style.minHeight = 180;
        _root.Add(scroll);

        _listContainer = new VisualElement();
        scroll.Add(_listContainer);
        _listPool = new ElementPool(CreateEmployeeRow, _listContainer);

        _emptyState = new VisualElement();
        _emptyState.AddToClassList("empty-state");
        var emptyIcon = new Label("👤");
        emptyIcon.AddToClassList("empty-state__icon");
        _emptyState.Add(emptyIcon);
        var emptyLabel = new Label("No unassigned employees available.");
        emptyLabel.AddToClassList("metric-tertiary");
        _emptyState.Add(emptyLabel);
        _emptyState.style.display = DisplayStyle.None;
        scroll.Add(_emptyState);

        var footer = new VisualElement();
        footer.AddToClassList("modal-footer");
        footer.AddToClassList("flex-row");
        footer.AddToClassList("justify-between");
        footer.style.marginTop = 8;
        footer.style.alignItems = Align.Center;
        _root.Add(footer);

        _selectionCountLabel = new Label("0 selected");
        _selectionCountLabel.AddToClassList("metric-tertiary");
        footer.Add(_selectionCountLabel);

        var footerRight = new VisualElement();
        footerRight.AddToClassList("flex-row");
        footer.Add(footerRight);

        _doneBtn = new Button { text = "Done" };
        _doneBtn.AddToClassList("btn-ghost");
        _doneBtn.style.marginRight = 6;
        footerRight.Add(_doneBtn);

        _addSelectedBtn = new Button { text = "Add Selected" };
        _addSelectedBtn.AddToClassList("btn-primary");
        _addSelectedBtn.SetEnabled(false);
        footerRight.Add(_addSelectedBtn);

        _doneBtn.clicked += OnDoneClicked;
        _addSelectedBtn.clicked += OnAddSelectedClicked;
    }

    public void Bind(IViewModel viewModel) {
        _vm = viewModel as AddMemberModalViewModel;
        if (_vm == null) return;
        if (_titleLabel != null) _titleLabel.text = "Add Member to " + _vm.TeamName;
        _selectedIds.Clear();
        UpdateSelectionCount();
        ApplyFilters();
    }

    public void Dispose() {
        if (_doneBtn != null) _doneBtn.clicked -= OnDoneClicked;
        if (_addSelectedBtn != null) _addSelectedBtn.clicked -= OnAddSelectedClicked;
        if (_filterButtons != null) {
            int count = _filterButtons.Length;
            for (int i = 0; i < count; i++) {
                if (_filterButtons[i] != null)
                    _filterButtons[i].UnregisterCallback<ClickEvent>(OnFilterClicked);
            }
        }
        _selectedIds.Clear();
        _filteredList.Clear();
        _activeFilters.Clear();
        _filterButtons = null;
        _addSelectedBtn = null;
        _selectionCountLabel = null;
        _filterRow = null;
        _listPool = null;
        _vm = null;
        _root = null;
    }

    private void OnDoneClicked() {
        _modal?.DismissModal();
    }

    private void OnFilterClicked(ClickEvent evt) {
        var btn = evt.currentTarget as Button;
        if (btn == null || !(btn.userData is RoleId role)) return;
        if (_activeFilters.Contains(role)) {
            _activeFilters.Remove(role);
            btn.RemoveFromClassList("filter-pill--active");
        } else {
            _activeFilters.Add(role);
            btn.AddToClassList("filter-pill--active");
        }
        ApplyFilters();
    }

    private void ApplyFilters() {
        if (_vm == null) return;
        _filteredList.Clear();
        var available = _vm.AvailableEmployees;
        int count = available.Count;
        for (int i = 0; i < count; i++) {
            if (_activeFilters.Contains(available[i].Role))
                _filteredList.Add(available[i]);
        }
        _listPool.UpdateList(_filteredList, BindEmployeeRow);
        bool hasItems = _filteredList.Count > 0;
        if (_emptyState != null) _emptyState.style.display = hasItems ? DisplayStyle.None : DisplayStyle.Flex;
    }

    private void OnRowClicked(ClickEvent evt) {
        var el = evt.currentTarget as VisualElement;
        if (el == null || !(el.userData is EmployeeId empId)) return;
        if (_selectedIds.Contains(empId)) {
            _selectedIds.Remove(empId);
            el.RemoveFromClassList("list-item--selected");
        } else {
            _selectedIds.Add(empId);
            el.AddToClassList("list-item--selected");
        }
        UpdateSelectionCount();
    }

    private void UpdateSelectionCount() {
        int count = _selectedIds.Count;
        if (_selectionCountLabel != null) _selectionCountLabel.text = count + " selected";
        if (_addSelectedBtn != null) _addSelectedBtn.SetEnabled(count > 0);
    }

    private void OnAddSelectedClicked() {
        if (_vm == null) return;
        var available = _vm.AvailableEmployees;
        int count = available.Count;
        for (int i = count - 1; i >= 0; i--) {
            if (_selectedIds.Contains(available[i].Id)) {
                _dispatcher.Dispatch(new AssignEmployeeToTeamCommand {
                    EmployeeId = available[i].Id,
                    TeamId = _vm.TeamId
                });
                available.RemoveAt(i);
            }
        }
        _selectedIds.Clear();
        UpdateSelectionCount();
        ApplyFilters();
    }

    private VisualElement CreateEmployeeRow() {
        var row = new VisualElement();
        row.AddToClassList("list-item");
        row.AddToClassList("list-item--selectable");
        row.AddToClassList("flex-row");
        row.AddToClassList("justify-between");
        row.style.marginBottom = 4;
        row.RegisterCallback<ClickEvent>(OnRowClicked);

        var nameGroup = new VisualElement();
        nameGroup.style.flexGrow = 1;

        var nameLabel = new Label();
        nameLabel.name = "emp-name";
        nameLabel.AddToClassList("metric-secondary");
        nameGroup.Add(nameLabel);

        var roleLabel = new Label();
        roleLabel.name = "emp-role";
        roleLabel.AddToClassList("badge");
        nameGroup.Add(roleLabel);

        row.Add(nameGroup);

        var typeLabel = new Label();
        typeLabel.name = "emp-type";
        typeLabel.AddToClassList("metric-tertiary");
        typeLabel.style.marginRight = 8;
        row.Add(typeLabel);

        return row;
    }

    private void BindEmployeeRow(VisualElement el, AvailableEmployeeDisplay data) {
        el.userData = data.Id;
        el.RemoveFromClassList("list-item--selected");

        var nameLabel = el.Q<Label>("emp-name");
        var roleLabel = el.Q<Label>("emp-role");
        var typeLabel = el.Q<Label>("emp-type");

        if (nameLabel != null) nameLabel.text = data.Name;
        if (typeLabel != null) typeLabel.text = data.TypeText;

        if (roleLabel != null) {
            UIFormatting.ClearRolePillClasses(roleLabel);
            roleLabel.text = data.RoleName;
            roleLabel.AddToClassList(data.RolePillClass);
        }
    }
}
