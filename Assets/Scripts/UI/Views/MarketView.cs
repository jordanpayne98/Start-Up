using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class MarketView : IGameView
{
    private readonly ICommandDispatcher _dispatcher;
    private readonly ITooltipProvider _tooltipProvider;
    private VisualElement _root;
    private MarketViewModel _viewModel;

    private Button _backButton;
    private Label _breadcrumbLabel;

    private VisualElement _categoryListContainer;
    private readonly List<VisualElement> _categoryRows = new List<VisualElement>(14);

    private VisualElement _nicheChartContainer;
    private VisualElement _nicheToggleBar;
    private readonly List<Button> _nicheTogglePills = new List<Button>(12);
    private DemandLineChart _chart;
    private VisualElement _legendContainer;

    private VisualElement _saleSection;
    private VisualElement _saleListContainer;
    private ElementPool _saleRowPool;
    private readonly List<ProductSaleStatusVM> _saleScratch = new List<ProductSaleStatusVM>(16);

    private readonly List<DemandLineChart.ChartLine> _chartLineScratch = new List<DemandLineChart.ChartLine>(12);

    private const int MaxNichePills = 13;

    public MarketView(ICommandDispatcher dispatcher, ITooltipProvider tooltipProvider)
    {
        _dispatcher = dispatcher;
        _tooltipProvider = tooltipProvider;
    }

    public void Initialize(VisualElement root)
    {
        _root = root;
        root.AddToClassList("screen-container");

        var scroll = new ScrollView();
        scroll.style.flexGrow = 1;
        scroll.contentContainer.style.flexGrow = 1;

        // Breadcrumb bar
        var breadcrumbBar = new VisualElement();
        breadcrumbBar.AddToClassList("market-breadcrumb-bar");
        breadcrumbBar.style.flexDirection = FlexDirection.Row;
        breadcrumbBar.style.alignItems = Align.Center;
        breadcrumbBar.style.marginBottom = 8;

        _backButton = new Button { text = "\u2190" };
        _backButton.AddToClassList("btn-secondary");
        _backButton.AddToClassList("btn-sm");
        _backButton.style.marginRight = 8;
        _backButton.style.display = DisplayStyle.None;
        _backButton.clicked += OnBackClicked;
        breadcrumbBar.Add(_backButton);

        _breadcrumbLabel = new Label("Market Overview");
        _breadcrumbLabel.AddToClassList("section-header");
        breadcrumbBar.Add(_breadcrumbLabel);

        scroll.contentContainer.Add(breadcrumbBar);

        // Level 1 — Category list
        _categoryListContainer = new VisualElement();
        _categoryListContainer.AddToClassList("market-category-list");

        var categories = MarketViewModel.OrderedMarketCategories;
        for (int c = 0; c < categories.Length; c++)
        {
            var row = CreateListRow(UIFormatting.FormatCategory(categories[c]));
            row.userData = (int)categories[c];
            row.RegisterCallback<ClickEvent>(OnCategoryRowClicked);
            _categoryRows.Add(row);
            _categoryListContainer.Add(row);
        }

        scroll.contentContainer.Add(_categoryListContainer);

        // Level 2 — Niche chart
        _nicheChartContainer = new VisualElement();
        _nicheChartContainer.AddToClassList("market-niche-chart");
        _nicheChartContainer.style.display = DisplayStyle.None;
        _nicheChartContainer.style.flexGrow = 1;

        _nicheToggleBar = new VisualElement();
        _nicheToggleBar.AddToClassList("market-toggle-bar");
        _nicheToggleBar.style.flexDirection = FlexDirection.Row;
        _nicheToggleBar.style.flexWrap = Wrap.Wrap;
        _nicheToggleBar.style.marginBottom = 8;

        for (int i = 0; i < MaxNichePills; i++)
        {
            var pill = new Button();
            pill.AddToClassList("filter-pill");
            pill.style.display = DisplayStyle.None;
            pill.RegisterCallback<ClickEvent>(OnNicheToggleClicked);
            _nicheTogglePills.Add(pill);
            _nicheToggleBar.Add(pill);
        }

        _nicheChartContainer.Add(_nicheToggleBar);

        _chart = new DemandLineChart();
        _chart.AddToClassList("demand-line-chart");
        _chart.style.flexGrow = 1;
        _chart.style.minHeight = 200;
        _nicheChartContainer.Add(_chart);

        _legendContainer = new VisualElement();
        _legendContainer.AddToClassList("market-legend");
        _legendContainer.style.flexDirection = FlexDirection.Row;
        _legendContainer.style.flexWrap = Wrap.Wrap;
        _legendContainer.style.marginTop = 8;
        _nicheChartContainer.Add(_legendContainer);

        scroll.contentContainer.Add(_nicheChartContainer);

        // Sale Events section
        _saleSection = new VisualElement();

        var divider = new VisualElement();
        divider.AddToClassList("section-divider");
        divider.style.marginTop = 8;
        divider.style.marginBottom = 8;
        _saleSection.Add(divider);

        var saleHeader = new Label("Sale Events");
        saleHeader.AddToClassList("section-header");
        saleHeader.style.marginBottom = 8;
        _saleSection.Add(saleHeader);

        _saleListContainer = new VisualElement();
        _saleRowPool = new ElementPool(CreateSaleRow, _saleListContainer);
        _saleSection.Add(_saleListContainer);

        scroll.contentContainer.Add(_saleSection);

        _root.Add(scroll);
    }

    public void Bind(IViewModel viewModel)
    {
        if (viewModel is not MarketViewModel vm) return;
        _viewModel = vm;

        _breadcrumbLabel.text = vm.Breadcrumb;
        _backButton.style.display = vm.CurrentLevel != MarketViewLevel.CategoryList
            ? DisplayStyle.Flex
            : DisplayStyle.None;

        switch (vm.CurrentLevel)
        {
            case MarketViewLevel.CategoryList:
                _categoryListContainer.style.display = DisplayStyle.Flex;
                _nicheChartContainer.style.display = DisplayStyle.None;
                _saleSection.style.display = vm.SaleStatusCount > 0 ? DisplayStyle.Flex : DisplayStyle.None;
                BindCategoryRows(vm);
                break;

            case MarketViewLevel.NicheChart:
                _categoryListContainer.style.display = DisplayStyle.None;
                _nicheChartContainer.style.display = DisplayStyle.Flex;
                _saleSection.style.display = DisplayStyle.None;
                BindNicheChart(vm);
                break;
        }

        _saleScratch.Clear();
        for (int i = 0; i < vm.SaleStatusCount; i++)
            _saleScratch.Add(vm.SaleStatuses[i]);
        _saleRowPool.UpdateList(_saleScratch, BindSaleRow);
    }

    public void Dispose()
    {
        _backButton.clicked -= OnBackClicked;

        int catCount = _categoryRows.Count;
        for (int i = 0; i < catCount; i++)
            _categoryRows[i].UnregisterCallback<ClickEvent>(OnCategoryRowClicked);

        int pillCount = _nicheTogglePills.Count;
        for (int i = 0; i < pillCount; i++)
            _nicheTogglePills[i].UnregisterCallback<ClickEvent>(OnNicheToggleClicked);

        _categoryRows.Clear();
        _nicheTogglePills.Clear();
        _viewModel = null;
        _saleRowPool = null;
        _chart = null;
        _legendContainer = null;
        _nicheToggleBar = null;
        _backButton = null;
        _breadcrumbLabel = null;
        _categoryListContainer = null;
        _nicheChartContainer = null;
        _saleSection = null;
        _saleListContainer = null;
    }

    // ─── Named Handlers (wired in Initialize) ────────────────────────────────

    private void OnBackClicked()
    {
        if (_viewModel == null) return;
        _viewModel.GoBack();
        Bind(_viewModel);
    }

    private void OnCategoryRowClicked(ClickEvent evt)
    {
        if (_viewModel == null) return;
        if (evt.currentTarget is VisualElement row && row.userData is int catInt)
        {
            var category = (ProductCategory)catInt;
            _viewModel.SelectCategory(category);
            Bind(_viewModel);
        }
    }

    private void OnNicheToggleClicked(ClickEvent evt)
    {
        if (_viewModel == null) return;
        if (evt.currentTarget is Button pill && pill.userData is int nicheInt)
        {
            var niche = (ProductNiche)nicheInt;
            bool isCurrentlyActive = pill.ClassListContains("filter-pill--active");
            bool newState = !isCurrentlyActive;
            _viewModel.ToggleNiche(niche, newState);
            pill.EnableInClassList("filter-pill--active", newState);
            RebuildChartData();
        }
    }

    // ─── Bind Helpers ─────────────────────────────────────────────────────────

    private void BindCategoryRows(MarketViewModel vm)
    {
        var categories = vm.Categories;
        int count = categories.Count;
        for (int i = 0; i < count && i < _categoryRows.Count; i++)
        {
            var row = _categoryRows[i];
            var data = categories[i];
            var nameLabel = row.Q<Label>("name-label");
            var trendLabel = row.Q<Label>("trend-label");
            var demandFill = row.Q<VisualElement>("demand-fill");
            var demandPctLabel = row.Q<Label>("demand-pct");

            if (nameLabel != null) nameLabel.text = data.Name;
            if (trendLabel != null) SetTrendIndicator(trendLabel, data.Trend);

            if (demandFill != null)
            {
                demandFill.style.width = Length.Percent(data.DemandPercent);
                demandFill.EnableInClassList("market-row__demand-fill--high", data.DemandPercent >= 70);
                demandFill.EnableInClassList("market-row__demand-fill--low",  data.DemandPercent <= 30);
            }

            if (demandPctLabel != null)
                demandPctLabel.text = data.DemandPercent + "%";
        }
    }

    private void BindNicheChart(MarketViewModel vm)
    {
        var projections = vm.Projections;
        int projCount = projections.Count;

        // Update toggle pills
        for (int i = 0; i < MaxNichePills; i++)
        {
            var pill = _nicheTogglePills[i];
            if (i < projCount)
            {
                var proj = projections[i];
                pill.style.display = DisplayStyle.Flex;
                pill.text = proj.Name;
                pill.userData = (int)proj.Niche;
                pill.EnableInClassList("filter-pill--active", proj.IsToggled);
            }
            else
            {
                pill.style.display = DisplayStyle.None;
            }
        }

        RebuildChartData();
    }

    private void RebuildChartData()
    {
        if (_viewModel == null) return;
        var projections = _viewModel.Projections;
        int projCount = projections.Count;

        _chartLineScratch.Clear();
        _legendContainer.Clear();

        for (int i = 0; i < projCount; i++)
        {
            var proj = projections[i];
            if (!proj.IsToggled) continue;

            _chartLineScratch.Add(new DemandLineChart.ChartLine
            {
                DataPoints = proj.ProjectedDemand,
                LineColor = proj.LineColor,
                Label = proj.Name
            });

            var legendEntry = new VisualElement();
            legendEntry.style.flexDirection = FlexDirection.Row;
            legendEntry.style.alignItems = Align.Center;
            legendEntry.style.marginRight = 12;
            legendEntry.style.marginBottom = 4;

            var colorSquare = new VisualElement();
            colorSquare.style.width = 10;
            colorSquare.style.height = 10;
            colorSquare.style.marginRight = 4;
            colorSquare.style.backgroundColor = new StyleColor(
                new Color(proj.LineColor.r / 255f, proj.LineColor.g / 255f, proj.LineColor.b / 255f, 1f));
            legendEntry.Add(colorSquare);

            var legendLabel = new Label(proj.Name + " (" + proj.CurrentDemand + "%)");
            legendLabel.AddToClassList("text-sm");
            legendEntry.Add(legendLabel);

            _legendContainer.Add(legendEntry);
        }

        if (_chartLineScratch.Count > 0)
            _chart.SetData(_chartLineScratch, 0f, 100f);
        else
            _chart.ClearData();
    }

    // ─── Row Factory ──────────────────────────────────────────────────────────

    private static VisualElement CreateListRow(string defaultName)
    {
        var row = new VisualElement();
        row.AddToClassList("market-row");
        row.AddToClassList("market-group__header");
        row.style.flexDirection = FlexDirection.Row;
        row.style.alignItems = Align.Center;
        row.style.paddingTop = 6;
        row.style.paddingBottom = 6;
        row.style.paddingLeft = 8;
        row.style.paddingRight = 8;
        row.style.marginBottom = 2;

        var nameLabel = new Label(defaultName);
        nameLabel.name = "name-label";
        nameLabel.AddToClassList("market-row__name");
        row.Add(nameLabel);

        var demandBar = new VisualElement();
        demandBar.AddToClassList("market-row__demand-bar");

        var demandFill = new VisualElement();
        demandFill.name = "demand-fill";
        demandFill.AddToClassList("market-row__demand-fill");
        demandBar.Add(demandFill);
        row.Add(demandBar);

        var demandPct = new Label("0%");
        demandPct.name = "demand-pct";
        demandPct.style.width = 40;
        demandPct.style.fontSize = 10;
        demandPct.style.unityTextAlign = TextAnchor.MiddleRight;
        row.Add(demandPct);

        var trendLabel = new Label("\u2014");
        trendLabel.name = "trend-label";
        trendLabel.AddToClassList("market-row__trend");
        trendLabel.style.marginLeft = 8;
        row.Add(trendLabel);

        var chevron = new Label("\u25b6");
        chevron.AddToClassList("market-row__chevron");
        chevron.style.marginLeft = 8;
        row.Add(chevron);

        return row;
    }

    // ─── Sale rows ────────────────────────────────────────────────────────────

    private VisualElement CreateSaleRow()
    {
        var row = new VisualElement();
        row.AddToClassList("sale-row");

        var nameLabel = new Label();
        nameLabel.name = "sale-name";
        nameLabel.AddToClassList("sale-row__name");
        row.Add(nameLabel);

        var statusLabel = new Label();
        statusLabel.name = "sale-status";
        statusLabel.AddToClassList("sale-row__status");
        row.Add(statusLabel);

        var triggerBtn = new Button { text = "Run Sale" };
        triggerBtn.name = "trigger-btn";
        triggerBtn.AddToClassList("btn-secondary");
        triggerBtn.AddToClassList("btn-sm");
        triggerBtn.RegisterCallback<ClickEvent>(OnTriggerSaleClicked);
        row.Add(triggerBtn);

        return row;
    }

    private void BindSaleRow(VisualElement row, ProductSaleStatusVM data)
    {
        var nameLabel   = row.Q<Label>("sale-name");
        var statusLabel = row.Q<Label>("sale-status");
        var triggerBtn  = row.Q<Button>("trigger-btn");

        if (nameLabel != null) nameLabel.text = data.ProductName;

        if (statusLabel != null)
        {
            statusLabel.EnableInClassList("sale-row__status--active",   data.IsOnSale);
            statusLabel.EnableInClassList("sale-row__status--cooldown", !data.IsOnSale && data.CooldownRemainingDays > 0);
            statusLabel.EnableInClassList("sale-row__status--ready",    data.CanTriggerSale);

            if (data.IsOnSale)
                statusLabel.text = "On Sale";
            else if (data.CooldownRemainingDays > 0)
                statusLabel.text = "Cooldown: " + data.CooldownRemainingDays + "d";
            else
                statusLabel.text = "Ready";
        }

        if (triggerBtn != null)
        {
            triggerBtn.SetEnabled(data.CanTriggerSale);
            triggerBtn.userData = data.ProductId;
        }
    }

    private void OnTriggerSaleClicked(ClickEvent evt)
    {
        var btn = evt.currentTarget as Button;
        if (btn?.userData is ProductId pid)
            OnTriggerSaleForProduct(pid);
    }

    private void OnTriggerSaleForProduct(ProductId productId)
    {
        _dispatcher.Dispatch(new TriggerSaleEventCommand
        {
            Tick = _dispatcher.CurrentTick,
            ProductId = productId
        });
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static void SetTrendIndicator(Label label, MarketTrend trend)
    {
        label.text = TrendIcon(trend);
        label.EnableInClassList("trend-rising",  trend == MarketTrend.Rising);
        label.EnableInClassList("trend-stable",  trend == MarketTrend.Stable);
        label.EnableInClassList("trend-falling", trend == MarketTrend.Falling);
    }

    private static string TrendIcon(MarketTrend trend)
    {
        switch (trend)
        {
            case MarketTrend.Rising:  return "\u25b2";
            case MarketTrend.Falling: return "\u25bc";
            default:                  return "\u2014";
        }
    }

}

