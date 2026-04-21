using System.Collections.Generic;
using UnityEngine.UIElements;

public class IndustryOverviewView : IGameView
{
    private VisualElement _root;
    private IndustryOverviewViewModel _viewModel;

    private Label _marketSizeLabel;
    private Label _activeCompaniesLabel;
    private Label _totalProductsLabel;

    private VisualElement _revenueContainer;
    private ElementPool _revenuePool;

    private VisualElement _shareContainer;
    private ElementPool _sharePool;

    private VisualElement _productCountContainer;
    private ElementPool _productCountPool;

    private VisualElement _reputationContainer;
    private ElementPool _reputationPool;

    private VisualElement _salaryContainer;
    private ElementPool _salaryPool;

    private VisualElement _disruptionContainer;
    private ElementPool _disruptionPool;

    public void Initialize(VisualElement root) {
        _root = root;

        var title = new Label("Industry Overview");
        title.AddToClassList("section-header");
        _root.Add(title);

        var scroll = new ScrollView();
        scroll.style.flexGrow = 1;
        var content = scroll.contentContainer;

        var statsCard = new VisualElement();
        statsCard.AddToClassList("card");
        statsCard.style.marginBottom = 12;
        var statsRow = new VisualElement();
        statsRow.AddToClassList("flex-row");
        statsRow.AddToClassList("justify-between");
        statsCard.Add(statsRow);

        _marketSizeLabel = CreateStatCell(statsRow, "Total Market Revenue");
        _activeCompaniesLabel = CreateStatCell(statsRow, "Active Companies");
        _totalProductsLabel = CreateStatCell(statsRow, "Products on Market");
        content.Add(statsCard);

        var leaderboardRow = new VisualElement();
        leaderboardRow.AddToClassList("flex-row");
        leaderboardRow.style.marginBottom = 12;

        _revenueContainer = CreateRankingColumn(leaderboardRow, "Top Revenue", out _revenuePool);
        _shareContainer = CreateRankingColumn(leaderboardRow, "Top Market Share", out _sharePool);
        _productCountContainer = CreateRankingColumn(leaderboardRow, "Most Products", out _productCountPool);
        _reputationContainer = CreateRankingColumn(leaderboardRow, "Top Reputation", out _reputationPool);
        content.Add(leaderboardRow);

        var salaryCard = new VisualElement();
        salaryCard.AddToClassList("card");
        salaryCard.style.marginBottom = 12;
        var salaryTitle = new Label("Salary Benchmarks");
        salaryTitle.AddToClassList("text-bold");
        salaryTitle.style.marginBottom = 6;
        salaryCard.Add(salaryTitle);
        var salaryHeader = BuildSalaryHeader();
        salaryCard.Add(salaryHeader);
        _salaryContainer = new VisualElement();
        _salaryPool = new ElementPool(CreateSalaryRow, _salaryContainer);
        salaryCard.Add(_salaryContainer);
        content.Add(salaryCard);

        var disruptionCard = new VisualElement();
        disruptionCard.AddToClassList("card");
        var disruptionTitle = new Label("Active Disruptions");
        disruptionTitle.AddToClassList("text-bold");
        disruptionTitle.style.marginBottom = 6;
        disruptionCard.Add(disruptionTitle);
        _disruptionContainer = new VisualElement();
        _disruptionPool = new ElementPool(CreateDisruptionRow, _disruptionContainer);
        disruptionCard.Add(_disruptionContainer);
        content.Add(disruptionCard);

        _root.Add(scroll);
    }

    public void Bind(IViewModel viewModel) {
        _viewModel = viewModel as IndustryOverviewViewModel;
        if (_viewModel == null) return;

        _marketSizeLabel.text = _viewModel.TotalMarketSize;
        _activeCompaniesLabel.text = _viewModel.ActiveCompanyCount.ToString();
        _totalProductsLabel.text = _viewModel.TotalProductsOnMarket.ToString();

        _revenuePool.UpdateList(_viewModel.RevenueRanking, BindRankingRow);
        _sharePool.UpdateList(_viewModel.MarketShareRanking, BindRankingRow);
        _productCountPool.UpdateList(_viewModel.ProductCountRanking, BindRankingRow);
        _reputationPool.UpdateList(_viewModel.ReputationRanking, BindRankingRow);

        _salaryPool.UpdateList(_viewModel.SalaryBenchmarks, BindSalaryRow);
        _disruptionPool.UpdateList(_viewModel.ActiveDisruptions, BindDisruptionRow);
    }

    public void Dispose() {
        _viewModel = null;
        _revenuePool = null;
        _sharePool = null;
        _productCountPool = null;
        _reputationPool = null;
        _salaryPool = null;
        _disruptionPool = null;
    }

    private static Label CreateStatCell(VisualElement parent, string labelText) {
        var cell = new VisualElement();
        cell.AddToClassList("card");
        cell.style.flexGrow = 1;
        cell.style.marginRight = 8;

        var lbl = new Label(labelText);
        lbl.AddToClassList("text-sm");
        lbl.AddToClassList("text-muted");
        cell.Add(lbl);

        var val = new Label("--");
        val.AddToClassList("text-bold");
        cell.Add(val);

        parent.Add(cell);
        return val;
    }

    private static VisualElement CreateRankingColumn(VisualElement parent, string title, out ElementPool pool) {
        var card = new VisualElement();
        card.AddToClassList("card");
        card.style.flexGrow = 1;
        card.style.marginRight = 8;

        var titleLabel = new Label(title);
        titleLabel.AddToClassList("text-bold");
        titleLabel.style.marginBottom = 6;
        card.Add(titleLabel);

        var container = new VisualElement();
        pool = new ElementPool(CreateRankingRow, container);
        card.Add(container);
        parent.Add(card);
        return container;
    }

    private static VisualElement CreateRankingRow() {
        var row = new VisualElement();
        row.AddToClassList("flex-row");
        row.AddToClassList("justify-between");
        row.style.marginBottom = 4;

        var nameLabel = new Label();
        nameLabel.name = "rank-name";
        nameLabel.AddToClassList("text-sm");
        nameLabel.style.flexGrow = 1;
        row.Add(nameLabel);

        var metricLabel = new Label();
        metricLabel.name = "rank-metric";
        metricLabel.AddToClassList("text-sm");
        metricLabel.AddToClassList("text-bold");
        row.Add(metricLabel);

        return row;
    }

    private static void BindRankingRow(VisualElement el, CompanyRankVM data) {
        el.Q<Label>("rank-name").text = data.CompanyName;
        el.Q<Label>("rank-metric").text = data.MetricDisplay;
    }

    private static VisualElement BuildSalaryHeader() {
        var row = new VisualElement();
        row.AddToClassList("column-header");
        string[] cols = { "Tier", "Industry Avg", "Your Avg", "Delta" };
        int[] flexes = { 2, 2, 2, 2 };
        for (int i = 0; i < cols.Length; i++) {
            var lbl = new Label(cols[i]);
            lbl.AddToClassList("column-header__cell");
            lbl.style.flexGrow = flexes[i];
            lbl.style.flexBasis = 0;
            row.Add(lbl);
        }
        return row;
    }

    private static VisualElement CreateSalaryRow() {
        var row = new VisualElement();
        row.AddToClassList("flex-row");
        row.style.marginBottom = 4;

        var tier = new Label();
        tier.name = "sal-tier";
        tier.AddToClassList("text-sm");
        tier.style.flexGrow = 2;
        tier.style.flexBasis = 0;
        row.Add(tier);

        var bench = new Label();
        bench.name = "sal-bench";
        bench.AddToClassList("text-sm");
        bench.style.flexGrow = 2;
        bench.style.flexBasis = 0;
        row.Add(bench);

        var player = new Label();
        player.name = "sal-player";
        player.AddToClassList("text-sm");
        player.style.flexGrow = 2;
        player.style.flexBasis = 0;
        row.Add(player);

        var delta = new Label();
        delta.name = "sal-delta";
        delta.AddToClassList("text-sm");
        delta.style.flexGrow = 2;
        delta.style.flexBasis = 0;
        row.Add(delta);

        return row;
    }

    private static void BindSalaryRow(VisualElement el, SalaryBenchmarkVM data) {
        el.Q<Label>("sal-tier").text = data.TierName;
        el.Q<Label>("sal-bench").text = data.BenchmarkSalary;
        el.Q<Label>("sal-player").text = data.PlayerAvgSalary;
        el.Q<Label>("sal-delta").text = data.Delta;
    }

    private static VisualElement CreateDisruptionRow() {
        var row = new VisualElement();
        row.AddToClassList("list-item");

        var nameLabel = new Label();
        nameLabel.name = "dis-name";
        nameLabel.AddToClassList("text-sm");
        nameLabel.AddToClassList("text-bold");
        nameLabel.style.flexGrow = 2;
        nameLabel.style.flexBasis = 0;
        row.Add(nameLabel);

        var descLabel = new Label();
        descLabel.name = "dis-desc";
        descLabel.AddToClassList("text-sm");
        descLabel.AddToClassList("text-muted");
        descLabel.style.flexGrow = 3;
        descLabel.style.flexBasis = 0;
        row.Add(descLabel);

        var timeLabel = new Label();
        timeLabel.name = "dis-time";
        timeLabel.AddToClassList("text-sm");
        timeLabel.style.flexGrow = 1;
        timeLabel.style.flexBasis = 0;
        row.Add(timeLabel);

        return row;
    }

    private static void BindDisruptionRow(VisualElement el, DisruptionVM data) {
        var nameLabel = el.Q<Label>("dis-name");
        nameLabel.text = data.IsMajor ? "⚠ " + data.EventName : data.EventName;
        nameLabel.EnableInClassList("text-danger", data.IsMajor);

        el.Q<Label>("dis-desc").text = data.Description;
        el.Q<Label>("dis-time").text = data.TicksRemaining;
    }
}
