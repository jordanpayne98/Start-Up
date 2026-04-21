using System.Collections.Generic;
using UnityEngine.UIElements;

public class CompetitorListView : IGameView
{
    private readonly IModalPresenter _modal;
    private VisualElement _root;
    private VisualElement _listContainer;
    private ElementPool _rowPool;
    private CompetitorListViewModel _viewModel;
    private VisualElement _emptyState;
    private readonly List<Button> _headerButtons = new List<Button>();

    public CompetitorListView(IModalPresenter modal) {
        _modal = modal;
    }

    public void Initialize(VisualElement root) {
        _root = root;

        var title = new Label("Competitors");
        title.AddToClassList("section-header");
        _root.Add(title);

        var headerRow = new VisualElement();
        headerRow.AddToClassList("column-header");

        var columns = new[] {
            ("Name",        CompetitorSortColumn.Name,        3),
            ("Reputation",  CompetitorSortColumn.Reputation,  2),
            ("Employees",   CompetitorSortColumn.Employees,   1),
            ("Products",    CompetitorSortColumn.Products,    1),
            ("Monthly Revenue", CompetitorSortColumn.Revenue, 2),
            ("Cash",        CompetitorSortColumn.Cash,        2),
            ("Your Stake",  CompetitorSortColumn.PlayerStake, 2)
        };

        for (int i = 0; i < columns.Length; i++) {
            var (label, sortCol, flex) = columns[i];
            var btn = new Button { text = label };
            btn.AddToClassList("column-header__cell");
            btn.style.flexGrow = flex;
            btn.style.flexBasis = 0;
            btn.style.backgroundColor = new StyleColor(UnityEngine.Color.clear);
            btn.style.borderTopWidth = 0;
            btn.style.borderBottomWidth = 0;
            btn.style.borderLeftWidth = 0;
            btn.style.borderRightWidth = 0;
            var capturedCol = sortCol;
            btn.clicked += () => {
                _viewModel?.Sort(capturedCol);
                Bind(_viewModel);
            };
            headerRow.Add(btn);
            _headerButtons.Add(btn);
        }
        _root.Add(headerRow);

        _emptyState = UICardHelper.CreateEmptyState("🏢", "No competitors active yet.");
        _emptyState.AddToClassList("empty-state--hidden");
        _root.Add(_emptyState);

        var scrollView = new ScrollView();
        scrollView.style.flexGrow = 1;
        _listContainer = scrollView.contentContainer;
        _rowPool = new ElementPool(CreateRow, _listContainer);
        _root.Add(scrollView);
    }

    public void Bind(IViewModel viewModel) {
        _viewModel = viewModel as CompetitorListViewModel;
        if (_viewModel == null) return;

        _rowPool.UpdateList(_viewModel.CompetitorRows, BindRow);

        bool hasRows = _viewModel.CompetitorRows != null && _viewModel.CompetitorRows.Count > 0;
        if (_emptyState != null) {
            if (hasRows) _emptyState.AddToClassList("empty-state--hidden");
            else _emptyState.RemoveFromClassList("empty-state--hidden");
        }
    }

    public void Dispose() {
        _viewModel = null;
        _rowPool = null;
        _headerButtons.Clear();
    }

    private VisualElement CreateRow() {
        var row = new VisualElement();
        row.AddToClassList("list-item");

        var nameLabel = new Label();
        nameLabel.name = "comp-name";
        nameLabel.style.flexGrow = 3;
        nameLabel.style.flexBasis = 0;
        row.Add(nameLabel);

        var archetypeLabel = new Label();
        archetypeLabel.name = "comp-archetype";
        archetypeLabel.AddToClassList("role-pill");
        archetypeLabel.style.flexGrow = 2;
        archetypeLabel.style.flexBasis = 0;
        archetypeLabel.style.alignSelf = Align.Center;
        row.Add(archetypeLabel);

        var employeesLabel = new Label();
        employeesLabel.name = "comp-employees";
        employeesLabel.AddToClassList("metric-secondary");
        employeesLabel.style.flexGrow = 1;
        employeesLabel.style.flexBasis = 0;
        row.Add(employeesLabel);

        var productsLabel = new Label();
        productsLabel.name = "comp-products";
        productsLabel.AddToClassList("metric-secondary");
        productsLabel.style.flexGrow = 1;
        productsLabel.style.flexBasis = 0;
        row.Add(productsLabel);

        var revenueLabel = new Label();
        revenueLabel.name = "comp-revenue";
        revenueLabel.AddToClassList("metric-secondary");
        revenueLabel.style.flexGrow = 2;
        revenueLabel.style.flexBasis = 0;
        row.Add(revenueLabel);

        var cashLabel = new Label();
        cashLabel.name = "comp-cash";
        cashLabel.AddToClassList("metric-secondary");
        cashLabel.style.flexGrow = 2;
        cashLabel.style.flexBasis = 0;
        row.Add(cashLabel);

        var stakeLabel = new Label();
        stakeLabel.name = "comp-stake";
        stakeLabel.AddToClassList("metric-tertiary");
        stakeLabel.style.flexGrow = 2;
        stakeLabel.style.flexBasis = 0;
        row.Add(stakeLabel);

        row.RegisterCallback<ClickEvent>(OnRowClicked);
        return row;
    }

    private void BindRow(VisualElement el, CompetitorRowVM data) {
        el.Q<Label>("comp-name").text = data.CompanyName;
        el.Q<Label>("comp-archetype").text = data.ReputationLabel;
        el.Q<Label>("comp-employees").text = data.EmployeeCount.ToString();
        el.Q<Label>("comp-products").text = data.ProductCount.ToString();
        el.Q<Label>("comp-revenue").text = data.Revenue;
        el.Q<Label>("comp-cash").text = data.Cash;

        string stakeText = data.PlayerOwnershipPercent > 0f
            ? UIFormatting.FormatPercent(data.PlayerOwnershipPercent)
            : "--";
        el.Q<Label>("comp-stake").text = stakeText;

        el.EnableInClassList("list-item--warning", data.IsNearBankruptcy);
        el.userData = data.Id;
    }

    private void OnRowClicked(ClickEvent evt) {
        var el = evt.currentTarget as VisualElement;
        if (el == null || !(el.userData is CompetitorId id)) return;
        _modal?.OpenCompetitorProfile(id);
    }
}
