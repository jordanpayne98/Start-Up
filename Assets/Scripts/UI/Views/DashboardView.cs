using System.Collections.Generic;
using UnityEngine.UIElements;

/// <summary>
/// Visual prototype for the Dashboard screen.
/// Builds its hierarchy programmatically to match DashboardViewModel static mock data.
/// Initialize() wires the element tree once.  Bind() updates text and meter widths only.
/// </summary>
public class DashboardView : IGameView
{
    // ── Stat card value labels ────────────────────────────────────────────────
    private Label _statCash;
    private Label _statRunway;
    private Label _statEmployees;
    private Label _statCandidates;

    // ── Product rows (name label, progress label, meter fill) ─────────────────
    private readonly List<(Label name, Label pct, VisualElement fill)> _productRows =
        new List<(Label, Label, VisualElement)>();

    // ── Contract rows (name label, chip label) ────────────────────────────────
    private readonly List<(Label name, Label chip)> _contractRows =
        new List<(Label, Label)>();

    // ── Activity rows (title label, subtitle label, severity bar) ─────────────
    private readonly List<(Label title, Label subtitle, VisualElement severityBar)> _activityRows =
        new List<(Label, Label, VisualElement)>();

    private DashboardViewModel _viewModel;

    // ═════════════════════════════════════════════════════════════════════════
    // IGameView.Initialize
    // ═════════════════════════════════════════════════════════════════════════

    public void Initialize(VisualElement root, UIServices services)
    {
        if (root == null) return;

        // ── Dashboard root container ──────────────────────────────────────────
        var dashboardRoot = new VisualElement();
        dashboardRoot.AddToClassList("dashboard-root");

        var scrollView = new ScrollView();
        scrollView.style.flexGrow = 1;
        scrollView.Add(dashboardRoot);
        root.Add(scrollView);

        // ── Screen header ─────────────────────────────────────────────────────
        var header = new VisualElement();
        header.AddToClassList("dashboard-header");

        var headerIcon = new Label("▦");
        headerIcon.AddToClassList("dashboard-header__icon");
        header.Add(headerIcon);

        var headerLabels = new VisualElement();
        headerLabels.AddToClassList("dashboard-header__labels");

        var headerTitle = new Label("Dashboard");
        headerTitle.AddToClassList("dashboard-header__title");
        headerLabels.Add(headerTitle);

        var headerSub = new Label("Company overview — static prototype");
        headerSub.AddToClassList("dashboard-header__subtitle");
        headerLabels.Add(headerSub);

        header.Add(headerLabels);
        dashboardRoot.Add(header);

        // ── Metric strip ──────────────────────────────────────────────────────
        var metricStrip = new VisualElement();
        metricStrip.AddToClassList("metric-strip");

        _statCash       = AddStatCard(metricStrip, "CASH",       "£0",        "Available Funds");
        _statRunway     = AddStatCard(metricStrip, "RUNWAY",     "—",         "Est. Months");
        _statEmployees  = AddStatCard(metricStrip, "EMPLOYEES",  "0",         "Headcount");
        _statCandidates = AddStatCard(metricStrip, "CANDIDATES", "0",         "In Pipeline");

        dashboardRoot.Add(metricStrip);

        // ── Dashboard 3-column grid ───────────────────────────────────────────
        var grid = new VisualElement();
        grid.AddToClassList("dashboard-grid");

        BuildProductsCard(grid);
        BuildContractsCard(grid);
        BuildActivityCard(grid);

        dashboardRoot.Add(grid);
    }

    // ═════════════════════════════════════════════════════════════════════════
    // IGameView.Bind
    // ═════════════════════════════════════════════════════════════════════════

    public void Bind(IViewModel viewModel)
    {
        _viewModel = viewModel as DashboardViewModel;
        if (_viewModel == null) return;

        // Stat cards
        if (_statCash       != null) _statCash.text       = _viewModel.CashDisplay;
        if (_statRunway     != null) _statRunway.text      = _viewModel.RunwayDisplay;
        if (_statEmployees  != null) _statEmployees.text   = _viewModel.EmployeeCount;
        if (_statCandidates != null) _statCandidates.text  = _viewModel.CandidateCount;

        // Products
        var products = _viewModel.Products;
        int productCount = products.Count < _productRows.Count ? products.Count : _productRows.Count;
        for (int i = 0; i < productCount; i++)
        {
            var entry = products[i];
            var row   = _productRows[i];

            row.name.text = entry.Name;
            row.pct.text  = entry.ProgressLabel;

            // Meter fill: set width as percentage string
            row.fill.style.width = new StyleLength(new Length(entry.ProgressPercent, LengthUnit.Percent));

            // Semantic color class based on progress
            row.fill.RemoveFromClassList("meter-fill--danger");
            row.fill.RemoveFromClassList("meter-fill--warning");
            row.fill.RemoveFromClassList("meter-fill--success");

            if (entry.ProgressPercent >= 80)
                row.fill.AddToClassList("meter-fill--success");
            else if (entry.ProgressPercent >= 40)
                row.fill.AddToClassList("meter-fill--warning");
            else
                row.fill.AddToClassList("meter-fill--danger");
        }

        // Contracts
        var contracts = _viewModel.Contracts;
        int contractCount = contracts.Count < _contractRows.Count ? contracts.Count : _contractRows.Count;
        for (int i = 0; i < contractCount; i++)
        {
            var entry = contracts[i];
            var row   = _contractRows[i];

            row.name.text = entry.Name;
            row.chip.text = entry.RiskLabel;

            row.chip.RemoveFromClassList("chip--warning");
            row.chip.RemoveFromClassList("chip--success");
            row.chip.RemoveFromClassList("chip--info");
            row.chip.RemoveFromClassList("chip--danger");

            row.chip.AddToClassList("chip--" + entry.RiskLevel);
        }

        // Activity
        var activity = _viewModel.RecentActivity;
        int activityCount = activity.Count < _activityRows.Count ? activity.Count : _activityRows.Count;
        for (int i = 0; i < activityCount; i++)
        {
            var entry = activity[i];
            var row   = _activityRows[i];

            row.title.text    = entry.Title;
            row.subtitle.text = entry.Subtitle;

            row.severityBar.RemoveFromClassList("activity-severity--info");
            row.severityBar.RemoveFromClassList("activity-severity--warning");
            row.severityBar.RemoveFromClassList("activity-severity--danger");
            row.severityBar.RemoveFromClassList("activity-severity--success");

            row.severityBar.AddToClassList("activity-severity--" + entry.Severity);
        }
    }

    // ═════════════════════════════════════════════════════════════════════════
    // IGameView.Dispose
    // ═════════════════════════════════════════════════════════════════════════

    public void Dispose()
    {
        _statCash = null;
        _statRunway = null;
        _statEmployees = null;
        _statCandidates = null;
        _productRows.Clear();
        _contractRows.Clear();
        _activityRows.Clear();
        _viewModel = null;
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Private helpers
    // ═════════════════════════════════════════════════════════════════════════

    private static Label AddStatCard(VisualElement parent, string headerText, string initialValue, string sublabel)
    {
        var card = new VisualElement();
        card.AddToClassList("card");
        card.AddToClassList("stat-card");

        var headerLabel = new Label(headerText);
        headerLabel.AddToClassList("stat-card__label");
        card.Add(headerLabel);

        var valueLabel = new Label(initialValue);
        valueLabel.AddToClassList("stat-card__value");
        card.Add(valueLabel);

        var subLabel = new Label(sublabel);
        subLabel.AddToClassList("stat-card__sublabel");
        card.Add(subLabel);

        parent.Add(card);
        return valueLabel;
    }

    private void BuildProductsCard(VisualElement parent)
    {
        var card = new VisualElement();
        card.AddToClassList("card");

        // Header
        var cardHeader = new VisualElement();
        cardHeader.AddToClassList("card-header");

        var title = new Label("Active Products");
        title.AddToClassList("card-header__title");
        cardHeader.Add(title);

        var viewAll = new Button { text = "View All" };
        viewAll.AddToClassList("btn");
        viewAll.AddToClassList("btn-ghost");
        viewAll.AddToClassList("btn-sm");
        cardHeader.Add(viewAll);

        card.Add(cardHeader);

        // 3 product rows
        for (int i = 0; i < 3; i++)
        {
            var row = new VisualElement();
            row.AddToClassList("product-row");

            var nameLabel = new Label("—");
            nameLabel.AddToClassList("product-row__name");
            row.Add(nameLabel);

            var meterWrap = new VisualElement();
            meterWrap.AddToClassList("meter-bar");
            meterWrap.AddToClassList("product-row__meter");

            var meterFill = new VisualElement();
            meterFill.AddToClassList("meter-fill");
            meterWrap.Add(meterFill);
            row.Add(meterWrap);

            var pctLabel = new Label("0%");
            pctLabel.AddToClassList("product-row__pct");
            row.Add(pctLabel);

            card.Add(row);
            _productRows.Add((nameLabel, pctLabel, meterFill));
        }

        parent.Add(card);
    }

    private void BuildContractsCard(VisualElement parent)
    {
        var card = new VisualElement();
        card.AddToClassList("card");

        var cardHeader = new VisualElement();
        cardHeader.AddToClassList("card-header");

        var title = new Label("Active Contracts");
        title.AddToClassList("card-header__title");
        cardHeader.Add(title);

        var viewAll = new Button { text = "View All" };
        viewAll.AddToClassList("btn");
        viewAll.AddToClassList("btn-ghost");
        viewAll.AddToClassList("btn-sm");
        cardHeader.Add(viewAll);

        card.Add(cardHeader);

        for (int i = 0; i < 3; i++)
        {
            var row = new VisualElement();
            row.AddToClassList("contract-row");

            var nameLabel = new Label("—");
            nameLabel.AddToClassList("contract-row__name");
            row.Add(nameLabel);

            var chipLabel = new Label("—");
            chipLabel.AddToClassList("chip");
            row.Add(chipLabel);

            card.Add(row);
            _contractRows.Add((nameLabel, chipLabel));
        }

        parent.Add(card);
    }

    private void BuildActivityCard(VisualElement parent)
    {
        var card = new VisualElement();
        card.AddToClassList("card");

        var cardHeader = new VisualElement();
        cardHeader.AddToClassList("card-header");

        var title = new Label("Recent Activity");
        title.AddToClassList("card-header__title");
        cardHeader.Add(title);

        card.Add(cardHeader);

        for (int i = 0; i < 4; i++)
        {
            var row = new VisualElement();
            row.AddToClassList("activity-row");

            var severityBar = new VisualElement();
            severityBar.AddToClassList("activity-severity");
            row.Add(severityBar);

            var labels = new VisualElement();
            labels.AddToClassList("activity-row__labels");

            var titleLabel = new Label("—");
            titleLabel.AddToClassList("activity-row__title");
            labels.Add(titleLabel);

            var subtitleLabel = new Label("—");
            subtitleLabel.AddToClassList("activity-row__subtitle");
            labels.Add(subtitleLabel);

            row.Add(labels);
            card.Add(row);
            _activityRows.Add((titleLabel, subtitleLabel, severityBar));
        }

        parent.Add(card);
    }
}
