using System.Collections.Generic;
using UnityEngine.UIElements;

public class MyInvestmentsView : IGameView
{
    private readonly ICommandDispatcher _dispatcher;
    private VisualElement _root;
    private MyInvestmentsViewModel _viewModel;

    private Label _portfolioValueLabel;
    private Label _dividendsLabel;
    private Label _companiesCountLabel;
    private Label _closestBuyoutLabel;

    private VisualElement _holdingsContainer;
    private ElementPool _holdingsPool;

    private VisualElement _watchlistContainer;
    private ElementPool _watchlistPool;

    public MyInvestmentsView(ICommandDispatcher dispatcher) {
        _dispatcher = dispatcher;
    }

    public void Initialize(VisualElement root) {
        _root = root;

        var title = new Label("My Investments");
        title.AddToClassList("section-header");
        _root.Add(title);

        var scroll = new ScrollView();
        scroll.style.flexGrow = 1;
        var content = scroll.contentContainer;

        var summaryCard = new VisualElement();
        summaryCard.AddToClassList("card");
        summaryCard.style.marginBottom = 12;

        var summaryRow = new VisualElement();
        summaryRow.AddToClassList("flex-row");
        summaryRow.AddToClassList("justify-between");
        summaryCard.Add(summaryRow);

        _portfolioValueLabel = CreateSummaryCard(summaryRow, "Portfolio Value");
        _dividendsLabel = CreateSummaryCard(summaryRow, "Annual Dividends");
        _companiesCountLabel = CreateSummaryCard(summaryRow, "Companies");
        _closestBuyoutLabel = CreateSummaryCard(summaryRow, "Closest Buyout");

        content.Add(summaryCard);

        var holdingsTitle = new Label("Holdings");
        holdingsTitle.AddToClassList("text-bold");
        holdingsTitle.style.marginBottom = 6;
        content.Add(holdingsTitle);

        var holdingsHeader = BuildHoldingsHeader();
        content.Add(holdingsHeader);

        _holdingsContainer = new VisualElement();
        _holdingsPool = new ElementPool(CreateHoldingRow, _holdingsContainer);
        content.Add(_holdingsContainer);

        var watchlistTitle = new Label("Watchlist");
        watchlistTitle.AddToClassList("text-bold");
        watchlistTitle.style.marginTop = 16;
        watchlistTitle.style.marginBottom = 6;
        content.Add(watchlistTitle);

        var watchlistHeader = BuildWatchlistHeader();
        content.Add(watchlistHeader);

        _watchlistContainer = new VisualElement();
        _watchlistPool = new ElementPool(CreateWatchlistRow, _watchlistContainer);
        content.Add(_watchlistContainer);

        _root.Add(scroll);
    }

    public void Bind(IViewModel viewModel) {
        _viewModel = viewModel as MyInvestmentsViewModel;
        if (_viewModel == null) return;

        _portfolioValueLabel.text = _viewModel.TotalPortfolioValue;
        _dividendsLabel.text = _viewModel.TotalAnnualDividends;
        _companiesCountLabel.text = _viewModel.CompaniesInvested.ToString();
        _closestBuyoutLabel.text = _viewModel.ClosestToBuyout;

        _holdingsPool.UpdateList(_viewModel.Holdings, BindHoldingRow);
        _watchlistPool.UpdateList(_viewModel.Watchlist, BindWatchlistRow);
    }

    public void Dispose() {
        _viewModel = null;
        _holdingsPool = null;
        _watchlistPool = null;
    }

    private static Label CreateSummaryCard(VisualElement parent, string labelText) {
        var card = new VisualElement();
        card.AddToClassList("card");
        card.style.flexGrow = 1;
        card.style.marginRight = 8;

        var lbl = new Label(labelText);
        lbl.AddToClassList("text-muted");
        lbl.AddToClassList("text-sm");
        card.Add(lbl);

        var value = new Label("--");
        value.AddToClassList("text-bold");
        card.Add(value);

        parent.Add(card);
        return value;
    }

    private static VisualElement BuildHoldingsHeader() {
        var row = new VisualElement();
        row.AddToClassList("column-header");

        string[] cols = { "Company", "Owned", "Value", "Dividend", "P/L", "", "" };
        int[] flexes = { 3, 1, 2, 2, 2, 1, 1 };

        for (int i = 0; i < cols.Length; i++) {
            var lbl = new Label(cols[i]);
            lbl.AddToClassList("column-header__cell");
            lbl.style.flexGrow = flexes[i];
            lbl.style.flexBasis = 0;
            row.Add(lbl);
        }
        return row;
    }

    private static VisualElement BuildWatchlistHeader() {
        var row = new VisualElement();
        row.AddToClassList("column-header");

        string[] cols = { "Company", "Price", "Revenue", "Market Share", "" };
        int[] flexes = { 3, 2, 2, 2, 1 };

        for (int i = 0; i < cols.Length; i++) {
            var lbl = new Label(cols[i]);
            lbl.AddToClassList("column-header__cell");
            lbl.style.flexGrow = flexes[i];
            lbl.style.flexBasis = 0;
            row.Add(lbl);
        }
        return row;
    }

    private VisualElement CreateHoldingRow() {
        var row = new VisualElement();
        row.AddToClassList("list-item");

        var nameLabel = new Label();
        nameLabel.name = "hold-name";
        nameLabel.style.flexGrow = 3;
        nameLabel.style.flexBasis = 0;
        row.Add(nameLabel);

        var ownedLabel = new Label();
        ownedLabel.name = "hold-owned";
        ownedLabel.AddToClassList("metric-secondary");
        ownedLabel.style.flexGrow = 1;
        ownedLabel.style.flexBasis = 0;
        row.Add(ownedLabel);

        var valueLabel = new Label();
        valueLabel.name = "hold-value";
        valueLabel.AddToClassList("metric-secondary");
        valueLabel.style.flexGrow = 2;
        valueLabel.style.flexBasis = 0;
        row.Add(valueLabel);

        var dividendLabel = new Label();
        dividendLabel.name = "hold-dividend";
        dividendLabel.AddToClassList("metric-secondary");
        dividendLabel.style.flexGrow = 2;
        dividendLabel.style.flexBasis = 0;
        row.Add(dividendLabel);

        var plLabel = new Label();
        plLabel.name = "hold-pl";
        plLabel.style.flexGrow = 2;
        plLabel.style.flexBasis = 0;
        row.Add(plLabel);

        var buyBtn = new Button { text = "Buy More" };
        buyBtn.name = "hold-buy-btn";
        buyBtn.AddToClassList("btn-secondary");
        buyBtn.AddToClassList("btn-sm");
        buyBtn.style.flexGrow = 1;
        buyBtn.style.flexBasis = 0;
        buyBtn.RegisterCallback<ClickEvent>(OnBuyMoreClicked);
        row.Add(buyBtn);

        var sellBtn = new Button { text = "Sell" };
        sellBtn.name = "hold-sell-btn";
        sellBtn.AddToClassList("btn-secondary");
        sellBtn.AddToClassList("btn-sm");
        sellBtn.style.flexGrow = 1;
        sellBtn.style.flexBasis = 0;
        sellBtn.RegisterCallback<ClickEvent>(OnSellClicked);
        row.Add(sellBtn);

        return row;
    }

    private void BindHoldingRow(VisualElement el, HoldingRowVM data) {
        el.Q<Label>("hold-name").text = data.CompanyName;
        el.Q<Label>("hold-owned").text = data.PercentOwned;
        el.Q<Label>("hold-value").text = data.CurrentValue;
        el.Q<Label>("hold-dividend").text = data.LastDividend;

        var plLabel = el.Q<Label>("hold-pl");
        plLabel.text = data.ProfitLoss;
        plLabel.RemoveFromClassList("text-success");
        plLabel.RemoveFromClassList("text-danger");
        plLabel.AddToClassList(data.ProfitLoss.StartsWith("-") ? "text-danger" : "text-success");

        var buyBtn = el.Q<Button>("hold-buy-btn");
        buyBtn.SetEnabled(data.CanBuyMore);
        buyBtn.text = data.CanBuyout ? "Buyout" : "Buy More";

        el.Q<Button>("hold-sell-btn").SetEnabled(true);
        el.userData = data.CompanyId;
    }

    private VisualElement CreateWatchlistRow() {
        var row = new VisualElement();
        row.AddToClassList("list-item");

        var nameLabel = new Label();
        nameLabel.name = "watch-name";
        nameLabel.style.flexGrow = 3;
        nameLabel.style.flexBasis = 0;
        row.Add(nameLabel);

        var priceLabel = new Label();
        priceLabel.name = "watch-price";
        priceLabel.AddToClassList("metric-secondary");
        priceLabel.style.flexGrow = 2;
        priceLabel.style.flexBasis = 0;
        row.Add(priceLabel);

        var revenueLabel = new Label();
        revenueLabel.name = "watch-revenue";
        revenueLabel.AddToClassList("metric-secondary");
        revenueLabel.style.flexGrow = 2;
        revenueLabel.style.flexBasis = 0;
        row.Add(revenueLabel);

        var shareLabel = new Label();
        shareLabel.name = "watch-share";
        shareLabel.AddToClassList("metric-tertiary");
        shareLabel.style.flexGrow = 2;
        shareLabel.style.flexBasis = 0;
        row.Add(shareLabel);

        var buyBtn = new Button { text = "Buy" };
        buyBtn.name = "watch-buy-btn";
        buyBtn.AddToClassList("btn-primary");
        buyBtn.AddToClassList("btn-sm");
        buyBtn.style.flexGrow = 1;
        buyBtn.style.flexBasis = 0;
        buyBtn.RegisterCallback<ClickEvent>(OnWatchlistBuyClicked);
        row.Add(buyBtn);

        return row;
    }

    private void BindWatchlistRow(VisualElement el, WatchlistRowVM data) {
        el.Q<Label>("watch-name").text = data.CompanyName;
        el.Q<Label>("watch-price").text = data.StockPrice;
        el.Q<Label>("watch-revenue").text = data.Revenue;
        el.Q<Label>("watch-share").text = data.MarketShare;
        el.userData = data.CompanyId;
    }

    private void OnBuyMoreClicked(ClickEvent evt) {
        var btn = evt.currentTarget as Button;
        if (btn?.parent?.userData is CompetitorId id)
            _dispatcher.Dispatch(new BuyStockCommand(_dispatcher.CurrentTick, id, 0.05f));
    }

    private void OnSellClicked(ClickEvent evt) {
        var btn = evt.currentTarget as Button;
        if (btn?.parent?.userData is CompetitorId id)
            _dispatcher.Dispatch(new SellStockCommand(_dispatcher.CurrentTick, id, 0.05f));
    }

    private void OnWatchlistBuyClicked(ClickEvent evt) {
        var btn = evt.currentTarget as Button;
        if (btn?.parent?.userData is CompetitorId id)
            _dispatcher.Dispatch(new BuyStockCommand(_dispatcher.CurrentTick, id, 0.05f));
    }
}
