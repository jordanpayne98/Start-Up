using System.Collections.Generic;
using UnityEngine.UIElements;

public class StockInvestmentsView : IGameView
{
    private readonly ICommandDispatcher _dispatcher;
    private VisualElement _root;
    private StockInvestmentsViewModel _viewModel;

    private Label _portfolioValueLabel;
    private Label _dividendLabel;

    private VisualElement _listContainer;
    private ElementPool _listPool;

    public StockInvestmentsView(ICommandDispatcher dispatcher) {
        _dispatcher = dispatcher;
    }

    public void Initialize(VisualElement root) {
        _root = root;

        var title = new Label("Stock / Investments");
        title.AddToClassList("section-header");
        _root.Add(title);

        var summaryCard = new VisualElement();
        summaryCard.AddToClassList("card");
        summaryCard.style.marginBottom = 12;
        var summaryRow = new VisualElement();
        summaryRow.AddToClassList("flex-row");
        summaryCard.Add(summaryRow);

        _portfolioValueLabel = CreateSummaryCell(summaryRow, "Portfolio Value");
        _dividendLabel = CreateSummaryCell(summaryRow, "Dividend Income / mo");
        _root.Add(summaryCard);

        var scroll = new ScrollView();
        scroll.style.flexGrow = 1;
        var content = scroll.contentContainer;

        var header = BuildHeader();
        content.Add(header);

        _listContainer = new VisualElement();
        _listPool = new ElementPool(CreateListingRow, _listContainer);
        content.Add(_listContainer);

        _root.Add(scroll);
    }

    public void Bind(IViewModel viewModel) {
        _viewModel = viewModel as StockInvestmentsViewModel;
        if (_viewModel == null) return;

        _portfolioValueLabel.text = _viewModel.TotalPortfolioValue;
        _dividendLabel.text = _viewModel.TotalDividendIncome;

        _listPool.UpdateList(_viewModel.AllListings, BindListingRow);
    }

    public void Dispose() {
        _viewModel = null;
        _listPool = null;
    }

    private static Label CreateSummaryCell(VisualElement parent, string labelText) {
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

    private static VisualElement BuildHeader() {
        var row = new VisualElement();
        row.AddToClassList("column-header");
        string[] cols = { "Company", "Price", "Available", "You Own", "Others Own", "", "" };
        int[] flexes = { 3, 2, 2, 2, 2, 1, 1 };
        for (int i = 0; i < cols.Length; i++) {
            var lbl = new Label(cols[i]);
            lbl.AddToClassList("column-header__cell");
            lbl.style.flexGrow = flexes[i];
            lbl.style.flexBasis = 0;
            row.Add(lbl);
        }
        return row;
    }

    private VisualElement CreateListingRow() {
        var row = new VisualElement();
        row.AddToClassList("list-item");

        var nameLabel = new Label();
        nameLabel.name = "stock-name";
        nameLabel.style.flexGrow = 3;
        nameLabel.style.flexBasis = 0;
        row.Add(nameLabel);

        var priceLabel = new Label();
        priceLabel.name = "stock-price";
        priceLabel.AddToClassList("metric-secondary");
        priceLabel.style.flexGrow = 2;
        priceLabel.style.flexBasis = 0;
        row.Add(priceLabel);

        var availLabel = new Label();
        availLabel.name = "stock-avail";
        availLabel.AddToClassList("metric-secondary");
        availLabel.style.flexGrow = 2;
        availLabel.style.flexBasis = 0;
        row.Add(availLabel);

        var playerLabel = new Label();
        playerLabel.name = "stock-player";
        playerLabel.AddToClassList("metric-secondary");
        playerLabel.style.flexGrow = 2;
        playerLabel.style.flexBasis = 0;
        row.Add(playerLabel);

        var othersLabel = new Label();
        othersLabel.name = "stock-others";
        othersLabel.AddToClassList("metric-tertiary");
        othersLabel.style.flexGrow = 2;
        othersLabel.style.flexBasis = 0;
        row.Add(othersLabel);

        var buyBtn = new Button { text = "Buy 5%" };
        buyBtn.name = "stock-buy-btn";
        buyBtn.AddToClassList("btn-primary");
        buyBtn.AddToClassList("btn-sm");
        buyBtn.style.flexGrow = 1;
        buyBtn.style.flexBasis = 0;
        buyBtn.RegisterCallback<ClickEvent>(OnBuyClicked);
        row.Add(buyBtn);

        var sellBtn = new Button { text = "Sell 5%" };
        sellBtn.name = "stock-sell-btn";
        sellBtn.AddToClassList("btn-secondary");
        sellBtn.AddToClassList("btn-sm");
        sellBtn.style.flexGrow = 1;
        sellBtn.style.flexBasis = 0;
        sellBtn.RegisterCallback<ClickEvent>(OnSellClicked);
        row.Add(sellBtn);

        return row;
    }

    private void BindListingRow(VisualElement el, StockListingVM data) {
        el.Q<Label>("stock-name").text = data.CompanyName;
        el.Q<Label>("stock-price").text = data.StockPrice;
        el.Q<Label>("stock-avail").text = data.UnownedPercent;
        el.Q<Label>("stock-player").text = data.PlayerOwned;
        el.Q<Label>("stock-others").text = data.TotalInvestorOwned;
        el.Q<Button>("stock-buy-btn").SetEnabled(data.CanBuy);
        el.Q<Button>("stock-sell-btn").SetEnabled(data.PlayerOwned != "--");
        el.userData = data.CompanyId;
    }

    private void OnBuyClicked(ClickEvent evt) {
        var btn = evt.currentTarget as Button;
        if (btn?.parent?.userData is CompetitorId id)
            _dispatcher.Dispatch(new BuyStockCommand(_dispatcher.CurrentTick, id, 0.05f));
    }

    private void OnSellClicked(ClickEvent evt) {
        var btn = evt.currentTarget as Button;
        if (btn?.parent?.userData is CompetitorId id)
            _dispatcher.Dispatch(new SellStockCommand(_dispatcher.CurrentTick, id, 0.05f));
    }
}
