using System.Collections.Generic;
using UnityEngine.UIElements;

public class EmployeesView : IGameView
{
    private readonly ICommandDispatcher _dispatcher;
    private readonly IModalPresenter _modal;
    private readonly INavigationService _nav;

    private VisualElement _root;
    private EmployeesViewModel _vm;

    // Header
    private Button _backBtn;
    private Label _titleLabel;
    private Label _countLabel;
    private Label _capacityLabel;

    // Sort
    private Button _sortName;
    private Button _sortRole;
    private Button _sortSalary;
    private Button _sortExpiry;
    private Button _sortMorale;

    // List
    private ScrollView _scroll;
    private VisualElement _listContainer;
    private ElementPool _rowPool;

    private EmployeeSortMode _currentSort = EmployeeSortMode.Name;

    public EmployeesView(ICommandDispatcher dispatcher, IModalPresenter modal, INavigationService nav) {
        _dispatcher = dispatcher;
        _modal = modal;
        _nav = nav;
    }

    public void Initialize(VisualElement root) {
        _root = root;
        _root.AddToClassList("employees-screen");

        // Header
        var header = new VisualElement();
        header.AddToClassList("screen-header");

        _backBtn = new Button(OnBackClicked) { text = "← HR Portal" };
        _backBtn.AddToClassList("btn-ghost");
        _backBtn.AddToClassList("btn-sm");
        header.Add(_backBtn);

        var titleRow = new VisualElement();
        titleRow.AddToClassList("flex-row");
        titleRow.AddToClassList("align-center");
        titleRow.style.flexGrow = 1;
        titleRow.style.marginLeft = 12;

        _titleLabel = new Label("Employees");
        _titleLabel.AddToClassList("metric-primary");
        _titleLabel.AddToClassList("text-accent");
        titleRow.Add(_titleLabel);

        _countLabel = new Label();
        _countLabel.AddToClassList("badge");
        _countLabel.AddToClassList("badge--neutral");
        _countLabel.style.marginLeft = 8;
        titleRow.Add(_countLabel);

        var capacityRow = new VisualElement();
        capacityRow.AddToClassList("flex-row");
        capacityRow.AddToClassList("align-center");
        var capacityLbl = new Label("Avg. Capacity:");
        capacityLbl.AddToClassList("metric-tertiary");
        capacityLbl.style.marginRight = 4;
        _capacityLabel = new Label("--");
        _capacityLabel.AddToClassList("metric-secondary");
        capacityRow.Add(capacityLbl);
        capacityRow.Add(_capacityLabel);

        header.Add(titleRow);
        header.Add(capacityRow);
        _root.Add(header);

        // Sort bar
        var sortBar = new VisualElement();
        sortBar.AddToClassList("employees-sort-bar");

        var sortLabel = new Label("Sort:");
        sortLabel.AddToClassList("metric-tertiary");
        sortLabel.style.alignSelf = Align.Center;
        sortLabel.style.marginRight = 8;
        sortBar.Add(sortLabel);

        _sortName   = BuildSortBtn(sortBar, "Name",    EmployeeSortMode.Name);
        _sortRole   = BuildSortBtn(sortBar, "Role",    EmployeeSortMode.Role);
        _sortSalary = BuildSortBtn(sortBar, "Salary",  EmployeeSortMode.Salary);
        _sortExpiry = BuildSortBtn(sortBar, "Expiry",  EmployeeSortMode.ContractExpiry);
        _sortMorale = BuildSortBtn(sortBar, "Morale",  EmployeeSortMode.Morale);

        _root.Add(sortBar);

        // Column headers
        var colHeader = new VisualElement();
        colHeader.AddToClassList("employees-col-header");
        AddColLabel(colHeader, "Employee",  2, false);
        AddColLabel(colHeader, "Type",      1);
        AddColLabel(colHeader, "Team",      1);
        AddColLabel(colHeader, "Morale",    1);
        AddColLabel(colHeader, "Energy",    1);
        AddColLabel(colHeader, "Salary",    1);
        AddColLabel(colHeader, "Expiry",    1);
        _root.Add(colHeader);

        // Scroll list
        _scroll = new ScrollView(ScrollViewMode.Vertical);
        _scroll.AddToClassList("employees-scroll");
        _listContainer = new VisualElement();
        _listContainer.AddToClassList("employees-list");
        _scroll.Add(_listContainer);
        _root.Add(_scroll);

        _rowPool = new ElementPool(CreateEmployeeRow, _listContainer);

        UpdateSortHighlight();
    }

    public void Bind(IViewModel viewModel) {
        _vm = viewModel as EmployeesViewModel;
        if (_vm == null) return;

        if (_countLabel != null) _countLabel.text = _vm.EmployeeCount.ToString();
        if (_capacityLabel != null) _capacityLabel.text = _vm.EffectiveCapacityText;

        _rowPool.UpdateList(_vm.Employees, BindEmployeeRow);
    }

    public void Dispose() {
        if (_backBtn != null)   _backBtn.clicked   -= OnBackClicked;
        if (_sortName != null)  _sortName.clicked  -= OnSortNameClicked;
        if (_sortRole != null)  _sortRole.clicked  -= OnSortRoleClicked;
        if (_sortSalary != null) _sortSalary.clicked -= OnSortSalaryClicked;
        if (_sortExpiry != null) _sortExpiry.clicked -= OnSortExpiryClicked;
        if (_sortMorale != null) _sortMorale.clicked -= OnSortMoraleClicked;

        _rowPool = null;
        _vm = null;
        _root = null;
        _listContainer = null;
    }

    // ── Sort handlers ──────────────────────────────────────────────────────────

    private void OnBackClicked()        => _nav.NavigateTo(ScreenId.HRPortalLanding);
    private void OnSortNameClicked()    => SetSort(EmployeeSortMode.Name);
    private void OnSortRoleClicked()    => SetSort(EmployeeSortMode.Role);
    private void OnSortSalaryClicked()  => SetSort(EmployeeSortMode.Salary);
    private void OnSortExpiryClicked()  => SetSort(EmployeeSortMode.ContractExpiry);
    private void OnSortMoraleClicked()  => SetSort(EmployeeSortMode.Morale);

    private void SetSort(EmployeeSortMode mode) {
        _currentSort = mode;
        if (_vm != null) {
            _vm.SortMode = mode;
            _vm.ResortAndNotify();
        }
        UpdateSortHighlight();
        if (_vm != null) {
            _rowPool.UpdateList(_vm.Employees, BindEmployeeRow);
        }
    }

    private void UpdateSortHighlight() {
        SetSortActive(_sortName,   _currentSort == EmployeeSortMode.Name);
        SetSortActive(_sortRole,   _currentSort == EmployeeSortMode.Role);
        SetSortActive(_sortSalary, _currentSort == EmployeeSortMode.Salary);
        SetSortActive(_sortExpiry, _currentSort == EmployeeSortMode.ContractExpiry);
        SetSortActive(_sortMorale, _currentSort == EmployeeSortMode.Morale);
    }

    private static void SetSortActive(Button btn, bool active) {
        if (btn == null) return;
        btn.EnableInClassList("tab-bar__item--active", active);
    }

    // ── Row factory ───────────────────────────────────────────────────────────

    private VisualElement CreateEmployeeRow() {
        var row = new VisualElement();
        row.AddToClassList("employees-row");

        // Employee col
        var empCol = new VisualElement();
        empCol.AddToClassList("employees-row__emp-col");
        empCol.style.flexGrow = 2;

        var nameRow = new VisualElement();
        nameRow.AddToClassList("flex-row");
        nameRow.AddToClassList("align-center");

        var nameLabel = new Label();
        nameLabel.name = "row-name";
        nameLabel.AddToClassList("metric-secondary");

        var founderBadge = new Label("Founder");
        founderBadge.name = "row-founder";
        founderBadge.AddToClassList("role-pill");
        founderBadge.AddToClassList("role-pill--founder");
        founderBadge.style.marginLeft = 6;
        founderBadge.style.display = DisplayStyle.None;

        var renewalBadge = new Label("Renewal");
        renewalBadge.name = "row-renewal";
        renewalBadge.AddToClassList("badge");
        renewalBadge.AddToClassList("badge--warning");
        renewalBadge.style.marginLeft = 6;
        renewalBadge.style.display = DisplayStyle.None;

        nameRow.Add(nameLabel);
        nameRow.Add(founderBadge);
        nameRow.Add(renewalBadge);

        var roleLabel = new Label();
        roleLabel.name = "row-role";
        roleLabel.AddToClassList("role-pill");
        roleLabel.style.marginTop = 2;

        empCol.Add(nameRow);
        empCol.Add(roleLabel);
        row.Add(empCol);

        // Type col
        var typeLabel = new Label();
        typeLabel.name = "row-type";
        typeLabel.AddToClassList("badge");
        typeLabel.AddToClassList("employees-row__cell");
        row.Add(typeLabel);

        // Team col
        var teamLabel = new Label();
        teamLabel.name = "row-team";
        teamLabel.AddToClassList("metric-secondary");
        teamLabel.AddToClassList("employees-row__cell");
        row.Add(teamLabel);

        // Morale col
        var moraleLabel = new Label();
        moraleLabel.name = "row-morale";
        moraleLabel.AddToClassList("metric-secondary");
        moraleLabel.AddToClassList("employees-row__cell");
        row.Add(moraleLabel);

        // Energy col
        var energyLabel = new Label();
        energyLabel.name = "row-energy";
        energyLabel.AddToClassList("metric-secondary");
        energyLabel.AddToClassList("employees-row__cell");
        row.Add(energyLabel);

        // Salary col
        var salaryLabel = new Label();
        salaryLabel.name = "row-salary";
        salaryLabel.AddToClassList("metric-secondary");
        salaryLabel.AddToClassList("employees-row__cell");
        row.Add(salaryLabel);

        // Expiry col
        var expiryLabel = new Label();
        expiryLabel.name = "row-expiry";
        expiryLabel.AddToClassList("metric-secondary");
        expiryLabel.AddToClassList("employees-row__cell");
        row.Add(expiryLabel);

        row.RegisterCallback<ClickEvent>(OnRowClicked);

        return row;
    }

    private void BindEmployeeRow(VisualElement el, EmployeeRowDisplay data) {
        el.userData = data.Id;

        var nameLabel    = el.Q<Label>("row-name");
        var founderBadge = el.Q<Label>("row-founder");
        var renewalBadge = el.Q<Label>("row-renewal");
        var roleLabel    = el.Q<Label>("row-role");
        var typeLabel    = el.Q<Label>("row-type");
        var teamLabel    = el.Q<Label>("row-team");
        var moraleLabel  = el.Q<Label>("row-morale");
        var energyLabel  = el.Q<Label>("row-energy");
        var salaryLabel  = el.Q<Label>("row-salary");
        var expiryLabel  = el.Q<Label>("row-expiry");

        if (nameLabel != null)    nameLabel.text = data.Name;
        if (founderBadge != null) founderBadge.style.display = data.IsFounder ? DisplayStyle.Flex : DisplayStyle.None;
        if (renewalBadge != null) renewalBadge.style.display = data.ShowRenewalBadge ? DisplayStyle.Flex : DisplayStyle.None;

        if (roleLabel != null) {
            roleLabel.text = data.RoleName;
            UIFormatting.ClearRolePillClasses(roleLabel);
            roleLabel.AddToClassList(data.RolePillClass);
        }

        if (typeLabel != null) {
            typeLabel.text = data.TypeBadge;
            typeLabel.RemoveFromClassList("badge--accent");
            typeLabel.RemoveFromClassList("badge--info");
            typeLabel.RemoveFromClassList("badge--special");
            typeLabel.AddToClassList(data.TypeBadgeClass);
        }

        if (teamLabel != null) teamLabel.text = data.TeamName;

        if (moraleLabel != null) {
            moraleLabel.text = data.MoraleText;
            moraleLabel.RemoveFromClassList("text-success");
            moraleLabel.RemoveFromClassList("text-warning");
            moraleLabel.RemoveFromClassList("text-danger");
            moraleLabel.AddToClassList(data.MoraleClass);
        }

        if (energyLabel != null) {
            energyLabel.text = data.EnergyText;
            energyLabel.RemoveFromClassList("energy-band--fresh");
            energyLabel.RemoveFromClassList("energy-band--fit");
            energyLabel.RemoveFromClassList("energy-band--tiring");
            energyLabel.RemoveFromClassList("energy-band--drained");
            energyLabel.RemoveFromClassList("energy-band--exhausted");
            energyLabel.AddToClassList(data.EnergyClass);
        }

        if (salaryLabel != null) salaryLabel.text = data.SalaryText;
        if (expiryLabel != null) expiryLabel.text = data.ContractExpiryText;
    }

    // ── Row click ─────────────────────────────────────────────────────────────

    private void OnRowClicked(ClickEvent evt) {
        var row = evt.currentTarget as VisualElement;
        if (row == null || !(row.userData is EmployeeId id)) return;
        OpenDetailModal(id);
    }

    private void OpenDetailModal(EmployeeId id) {
        var vm = new EmployeeDetailModalViewModel();
        vm.SetEmployeeId(id);
        _modal.ShowModal(new EmployeeDetailModalView(_dispatcher, _modal), vm);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private Button BuildSortBtn(VisualElement parent, string label, EmployeeSortMode mode) {
        var btn = new Button { text = label };
        btn.AddToClassList("tab-bar__item");
        btn.AddToClassList("btn-sm");
        switch (mode) {
            case EmployeeSortMode.Name:           btn.clicked += OnSortNameClicked;   break;
            case EmployeeSortMode.Role:           btn.clicked += OnSortRoleClicked;   break;
            case EmployeeSortMode.Salary:         btn.clicked += OnSortSalaryClicked; break;
            case EmployeeSortMode.ContractExpiry: btn.clicked += OnSortExpiryClicked; break;
            case EmployeeSortMode.Morale:         btn.clicked += OnSortMoraleClicked; break;
        }
        parent.Add(btn);
        return btn;
    }

    private static void AddColLabel(VisualElement parent, string text, int flex, bool center = true) {
        var lbl = new Label(text);
        lbl.AddToClassList("metric-tertiary");
        lbl.style.flexGrow = flex;
        if (center) lbl.style.unityTextAlign = UnityEngine.TextAnchor.MiddleCenter;
        parent.Add(lbl);
    }
}
