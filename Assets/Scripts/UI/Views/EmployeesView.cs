using System.Collections.Generic;
using UnityEngine.UIElements;

public class EmployeesView : IGameView
{
    private readonly ICommandDispatcher _dispatcher;
    private readonly IModalPresenter _modal;
    private readonly ITooltipProvider _tooltipProvider;
    private VisualElement _root;
    private VisualElement _listContainer;
    private ElementPool _employeePool;
    private EmployeesViewModel _viewModel;
    private readonly List<Button> _headerButtons = new List<Button>();

    // Empty state
    private VisualElement _emptyState;

    // Stagger scratch list — reused, never allocated in Bind
    private readonly List<VisualElement> _staggerScratch = new List<VisualElement>();

    // Stagger guard
    private bool _hasAnimatedIn;

    public EmployeesView(ICommandDispatcher dispatcher, IModalPresenter modal, ITooltipProvider tooltipProvider) {
        _dispatcher = dispatcher;
        _modal = modal;
        _tooltipProvider = tooltipProvider;
    }

    public void Initialize(VisualElement root) {
        _root = root;

        // Title
        var title = new Label("Employees");
        title.AddToClassList("section-header");
        _root.Add(title);

        // Column headers
        var headerRow = new VisualElement();
        headerRow.AddToClassList("column-header");

        var columns = new[] {
            ("Name",   EmployeeSortColumn.Name,   3, ""),
            ("Role",   EmployeeSortColumn.Role,   2, ""),
            ("Age",    EmployeeSortColumn.Role,   1, ""),
            ("Salary", EmployeeSortColumn.Salary, 1, "Monthly pay. Deducted automatically each in-game month."),
            ("Morale", EmployeeSortColumn.Morale, 1, "Current wellbeing. Low morale reduces work quality and speed."),
            ("Ability", EmployeeSortColumn.Ability, 1, "Overall skill star rating from 1 to 5."),
            ("Team",   EmployeeSortColumn.Team,   2, "")
        };

        for (int i = 0; i < columns.Length; i++) {
            var (label, sortCol, flex, tip) = columns[i];
            var btn = new Button { text = label };
            btn.AddToClassList("column-header__cell");
            btn.style.flexGrow = flex;
            btn.style.flexBasis = 0;
            btn.style.backgroundColor = new StyleColor(UnityEngine.Color.clear);
            btn.style.borderTopWidth = 0;
            btn.style.borderBottomWidth = 0;
            btn.style.borderLeftWidth = 0;
            btn.style.borderRightWidth = 0;

            if (!string.IsNullOrEmpty(tip))
                btn.SetSimpleTooltip(tip, _tooltipProvider.TooltipService);

            var capturedCol = sortCol;
            btn.clicked += () => {
                _viewModel?.Sort(capturedCol);
                Bind(_viewModel);
            };

            headerRow.Add(btn);
            _headerButtons.Add(btn);
        }
        _root.Add(headerRow);

        // Empty state
        _emptyState = UICardHelper.CreateEmptyState("👤", "No employees hired yet.");
        _emptyState.AddToClassList("empty-state--hidden");
        _root.Add(_emptyState);

        // Scrollable list
        var scrollView = new ScrollView();
        scrollView.style.flexGrow = 1;
        _listContainer = scrollView.contentContainer;
        _employeePool = new ElementPool(CreateEmployeeRow, _listContainer);
        _root.Add(scrollView);
    }

    public void Bind(IViewModel viewModel) {
        _viewModel = viewModel as EmployeesViewModel;
        if (_viewModel == null) return;

        _employeePool.UpdateList(_viewModel.Employees, BindEmployeeRow);

        bool hasEmployees = _viewModel.Employees != null && _viewModel.Employees.Count > 0;
        if (_emptyState != null) {
            if (hasEmployees) _emptyState.AddToClassList("empty-state--hidden");
            else _emptyState.RemoveFromClassList("empty-state--hidden");
        }

        // Stagger on first bind
        if (!_hasAnimatedIn && hasEmployees) {
            _hasAnimatedIn = true;
            _staggerScratch.Clear();
            int childCount = _listContainer.childCount;
            for (int i = 0; i < childCount; i++) {
                var el = _listContainer[i];
                if (el.style.display != DisplayStyle.None) _staggerScratch.Add(el);
            }
            UIAnimator.StaggerIn(_staggerScratch);
        }
    }

    public void Dispose() {
        var ts = _tooltipProvider.TooltipService;
        int btnCount = _headerButtons.Count;
        for (int i = 0; i < btnCount; i++)
            _headerButtons[i].ClearTooltip(ts);
        _hasAnimatedIn = false;
        _staggerScratch.Clear();
        _viewModel = null;
        _employeePool = null;
    }

    private VisualElement CreateEmployeeRow() {
        var row = new VisualElement();
        row.AddToClassList("list-item");

        var nameLabel = new Label();
        nameLabel.name = "emp-name";
        nameLabel.style.flexGrow = 3;
        nameLabel.style.flexBasis = 0;
        row.Add(nameLabel);

        var roleLabel = new Label();
        roleLabel.name = "emp-role";
        roleLabel.AddToClassList("role-pill");
        roleLabel.style.flexGrow = 2;
        roleLabel.style.flexBasis = 0;
        roleLabel.style.alignSelf = Align.Center;
        roleLabel.style.overflow = Overflow.Hidden;
        row.Add(roleLabel);

        var ageLabel = new Label();
        ageLabel.name = "emp-age";
        ageLabel.AddToClassList("metric-tertiary");
        ageLabel.style.flexGrow = 1;
        ageLabel.style.flexBasis = 0;
        ageLabel.style.paddingLeft = 8;
        row.Add(ageLabel);

        var salaryLabel = new Label();
        salaryLabel.name = "emp-salary";
        salaryLabel.AddToClassList("metric-secondary");
        salaryLabel.style.flexGrow = 1;
        salaryLabel.style.flexBasis = 0;
        row.Add(salaryLabel);

        var moraleLabel = new Label();
        moraleLabel.name = "emp-morale-label";
        moraleLabel.AddToClassList("morale-band-label");
        moraleLabel.style.flexGrow = 1;
        moraleLabel.style.flexBasis = 0;
        row.Add(moraleLabel);

        var starContainer = new VisualElement();
        starContainer.name = "emp-stars";
        starContainer.AddToClassList("star-rating");
        starContainer.style.flexGrow = 1;
        starContainer.style.flexBasis = 0;
        for (int s = 0; s < 5; s++) {
            var star = new VisualElement();
            star.AddToClassList("star");
            star.AddToClassList("star-empty");
            starContainer.Add(star);
        }
        row.Add(starContainer);

        var teamLabel = new Label();
        teamLabel.name = "emp-team";
        teamLabel.AddToClassList("metric-tertiary");
        teamLabel.style.flexGrow = 2;
        teamLabel.style.flexBasis = 0;
        row.Add(teamLabel);

        // Wire click handler once at creation
        row.RegisterCallback<ClickEvent>(OnEmployeeRowClicked);

        return row;
    }

    private void BindEmployeeRow(VisualElement el, EmployeeRowDisplay data) {
        var nameLabel = el.Q<Label>("emp-name");
        nameLabel.text = data.Name;
        if (data.IsFounder) nameLabel.AddToClassList("text-founder");
        else nameLabel.RemoveFromClassList("text-founder");

        el.Q<Label>("emp-age").text = data.Age > 0 ? data.Age.ToString() : "";

        var roleLabel = el.Q<Label>("emp-role");
        roleLabel.text = data.Role;
        UIFormatting.ClearRolePillClasses(roleLabel);
        roleLabel.AddToClassList(UIFormatting.RolePillClass(data.Role));

        el.Q<Label>("emp-salary").text = data.SalaryDisplay;
        el.Q<Label>("emp-team").text = data.TeamName;

        // Morale band label
        var moraleLabel = el.Q<Label>("emp-morale-label");
        if (moraleLabel != null) {
            moraleLabel.text = data.MoraleBandLabel;
            moraleLabel.RemoveFromClassList("morale-band--inspired");
            moraleLabel.RemoveFromClassList("morale-band--motivated");
            moraleLabel.RemoveFromClassList("morale-band--stable");
            moraleLabel.RemoveFromClassList("morale-band--unhappy");
            moraleLabel.RemoveFromClassList("morale-band--miserable");
            moraleLabel.RemoveFromClassList("morale-band--critical");
            moraleLabel.AddToClassList("morale-band--" + data.MoraleBandLabel.ToLowerInvariant());
        }

        // Star rating (Ability)
        var starContainer = el.Q<VisualElement>("emp-stars");
        if (starContainer != null) {
            int childCount = starContainer.childCount;
            for (int s = 0; s < childCount && s < 5; s++) {
                var star = starContainer[s];
                star.RemoveFromClassList("star-filled");
                star.RemoveFromClassList("star-empty");
                star.AddToClassList(s < data.AbilityStars ? "star-filled" : "star-empty");
            }
        }

        // Selection state
        if (_viewModel != null && _viewModel.SelectedEmployeeId.HasValue
            && _viewModel.SelectedEmployeeId.Value == data.Id) {
            el.AddToClassList("list-item--selected");
        } else {
            el.RemoveFromClassList("list-item--selected");
        }

        el.userData = data.Id;
    }

    private void OnEmployeeRowClicked(ClickEvent evt) {
        var el = evt.currentTarget as VisualElement;
        if (el == null || !(el.userData is EmployeeId id)) return;
        _viewModel?.SelectEmployee(id);
        var profileVM = new EmployeeProfileViewModel();
        profileVM.SetEmployee(id);
        _modal.ShowModal(new EmployeeProfileView(_dispatcher, _modal), profileVM);
        Bind(_viewModel);
    }
}
