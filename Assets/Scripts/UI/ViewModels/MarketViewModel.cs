using System.Collections.Generic;
using UnityEngine;

public enum MarketViewLevel { CategoryList, NicheChart }

public struct CategoryRowVM
{
    public ProductCategory Category;
    public string Name;
    public MarketTrend Trend;
    public int DemandPercent;
}

public struct NicheProjectionVM
{
    public ProductNiche Niche;
    public string Name;
    public Color32 LineColor;
    public bool IsToggled;
    public List<float> ProjectedDemand;
    public int CurrentDemand;
    public MarketTrend CurrentTrend;
}

public struct ProductSaleStatusVM
{
    public ProductId ProductId;
    public string ProductName;
    public bool IsOnSale;
    public int CooldownRemainingDays;
    public bool CanTriggerSale;
}

public class MarketViewModel : IViewModel
{
    public MarketViewLevel CurrentLevel { get; private set; }
    public ProductCategory? SelectedCategory { get; private set; }
    public string Breadcrumb { get; private set; }

    public IReadOnlyList<CategoryRowVM> Categories => _categories;
    public IReadOnlyList<NicheProjectionVM> Projections => _projections;

    public ProductSaleStatusVM[] SaleStatuses { get; private set; }
    public int SaleStatusCount { get; private set; }

    private readonly List<CategoryRowVM> _categories = new List<CategoryRowVM>(14);
    private readonly List<NicheProjectionVM> _projections = new List<NicheProjectionVM>(12);
    private readonly HashSet<ProductNiche> _toggledNiches = new HashSet<ProductNiche>();
    private bool _hasVisitedCurrentNicheChart;

    private IReadOnlyGameState _cachedState;
    private CompetitorState _cachedCompState;

    private static readonly Dictionary<ProductNiche, ProductCategory> _nicheToCategory;

    private static readonly HashSet<ProductCategory> _marketCategories = new HashSet<ProductCategory>
    {
        ProductCategory.OperatingSystem,
        ProductCategory.VideoGame,
        ProductCategory.GameConsole,
        ProductCategory.GameEngine,
        ProductCategory.GraphicsEditor,
        ProductCategory.AudioTool,
        ProductCategory.DevFramework
    };

    public static readonly ProductCategory[] OrderedMarketCategories = new ProductCategory[]
    {
        ProductCategory.OperatingSystem,
        ProductCategory.VideoGame,
        ProductCategory.GameConsole,
        ProductCategory.GameEngine,
        ProductCategory.GraphicsEditor,
        ProductCategory.AudioTool,
        ProductCategory.DevFramework
    };

    static MarketViewModel()
    {
        _nicheToCategory = new Dictionary<ProductNiche, ProductCategory>
        {
            // Operating System niches
            { ProductNiche.DesktopOS,           ProductCategory.OperatingSystem },
            // Video Game genres
            { ProductNiche.RPG,               ProductCategory.VideoGame },
            { ProductNiche.FPS,               ProductCategory.VideoGame },
            { ProductNiche.Strategy,          ProductCategory.VideoGame },
            { ProductNiche.Puzzle,            ProductCategory.VideoGame },
            { ProductNiche.Platformer,        ProductCategory.VideoGame },
            { ProductNiche.Simulation,        ProductCategory.VideoGame },
            { ProductNiche.Racing,            ProductCategory.VideoGame },
            { ProductNiche.Sports,            ProductCategory.VideoGame },
            { ProductNiche.Horror,            ProductCategory.VideoGame },
            { ProductNiche.Adventure,         ProductCategory.VideoGame },
            { ProductNiche.MMORPG,            ProductCategory.VideoGame },
            { ProductNiche.Sandbox,           ProductCategory.VideoGame },
            { ProductNiche.Fighting,          ProductCategory.VideoGame },
        };
    }

    public MarketViewModel()
    {
        CurrentLevel = MarketViewLevel.CategoryList;
        Breadcrumb = "Market Overview";
        SaleStatuses = new ProductSaleStatusVM[64];
    }

    public void SelectCategory(ProductCategory category)
    {
        SelectedCategory = category;
        _hasVisitedCurrentNicheChart = false;

        CurrentLevel = MarketViewLevel.NicheChart;
        Breadcrumb = "Market Overview > " + FormatCategoryPlural(category);
        RefreshProjections();
    }

    public void GoBack()
    {
        if (CurrentLevel == MarketViewLevel.NicheChart)
        {
            SelectedCategory = null;
            CurrentLevel = MarketViewLevel.CategoryList;
            Breadcrumb = "Market Overview";
        }
    }

    public void ToggleNiche(ProductNiche niche, bool visible)
    {
        if (visible)
            _toggledNiches.Add(niche);
        else
            _toggledNiches.Remove(niche);

        int count = _projections.Count;
        for (int i = 0; i < count; i++)
        {
            var p = _projections[i];
            if (p.Niche == niche)
            {
                p.IsToggled = visible;
                _projections[i] = p;
                break;
            }
        }
    }

    public void Refresh(IReadOnlyGameState state)
    {
        if (state == null) return;
        var snapshot = state as GameStateSnapshot;
        Refresh(state, snapshot?.CompetitorState, snapshot?.MarketStateRef);
    }

    public void Refresh(IReadOnlyGameState state, CompetitorState compState, MarketState marketState)
    {
        if (state == null) return;
        _cachedState = state;
        _cachedCompState = compState;

        var nicheValues = (ProductNiche[])System.Enum.GetValues(typeof(ProductNiche));

        _categories.Clear();
        for (int c = 0; c < OrderedMarketCategories.Length; c++)
        {
            var cat = OrderedMarketCategories[c];
            bool isConsumerCategory = true;
            int risingCount = 0;
            int fallingCount = 0;
            float maxDemand = 0f;

            for (int n = 0; n < nicheValues.Length; n++)
            {
                var niche = nicheValues[n];
                if (!_nicheToCategory.TryGetValue(niche, out var mapped) || mapped != cat) continue;

                isConsumerCategory = false;
                MarketTrend trend = state.GetNicheTrend(niche);
                if (trend == MarketTrend.Rising) risingCount++;
                else if (trend == MarketTrend.Falling) fallingCount++;

                float nicheDemand = state.GetNicheDemand(niche);
                if (nicheDemand > maxDemand) maxDemand = nicheDemand;
            }

            MarketTrend dominantTrend;
            int demandPercent;
            if (isConsumerCategory)
            {
                dominantTrend = state.GetCategoryTrend(cat);
                demandPercent = (int)state.GetCategoryDemand(cat);
            }
            else
            {
                dominantTrend = risingCount > fallingCount ? MarketTrend.Rising
                    : fallingCount > risingCount ? MarketTrend.Falling
                    : MarketTrend.Stable;
                demandPercent = (int)maxDemand;
            }

            _categories.Add(new CategoryRowVM
            {
                Category = cat,
                Name = FormatCategoryPlural(cat),
                Trend = dominantTrend,
                DemandPercent = demandPercent
            });
        }

        if (CurrentLevel == MarketViewLevel.NicheChart)
            RefreshProjections();

        SaleStatusCount = 0;
        if (state.ShippedProducts != null)
        {
            foreach (var kvp in state.ShippedProducts)
            {
                var p = kvp.Value;
                if (!p.IsOnMarket || p.IsCompetitorProduct) continue;
                if (SaleStatusCount >= SaleStatuses.Length) break;

                int cooldownTicks = state.GetSaleCooldownRemainingTicks(p.Id);
                int cooldownDays = cooldownTicks / TimeState.TicksPerDay;
                bool canTrigger = !p.IsOnSale && cooldownTicks <= 0;

                SaleStatuses[SaleStatusCount++] = new ProductSaleStatusVM
                {
                    ProductId = p.Id,
                    ProductName = p.ProductName,
                    IsOnSale = p.IsOnSale,
                    CooldownRemainingDays = cooldownDays,
                    CanTriggerSale = canTrigger
                };
            }
        }
    }

    private void RefreshProjections()
    {
        if (_cachedState == null || !SelectedCategory.HasValue) return;

        _projections.Clear();
        var nicheValues = (ProductNiche[])System.Enum.GetValues(typeof(ProductNiche));
        var selectedCat = SelectedCategory.Value;
        int colorIndex = 0;

        bool hasNiches = false;
        for (int n = 0; n < nicheValues.Length; n++)
        {
            if (_nicheToCategory.TryGetValue(nicheValues[n], out var mapped) && mapped == selectedCat)
            {
                hasNiches = true;
                break;
            }
        }

        if (!hasNiches)
        {
            var projection = _cachedState.GetCategoryDemandProjection(selectedCat, 360);
            float demand = _cachedState.GetCategoryDemand(selectedCat);
            MarketTrend trend = _cachedState.GetCategoryTrend(selectedCat);

            _projections.Add(new NicheProjectionVM
            {
                Niche = ProductNiche.None,
                Name = FormatCategoryPlural(selectedCat),
                LineColor = DemandLineChart.GetLineColor(0),
                IsToggled = true,
                ProjectedDemand = projection,
                CurrentDemand = (int)demand,
                CurrentTrend = trend
            });

            _hasVisitedCurrentNicheChart = true;
            return;
        }

        int defaultToggleCount = 0;

        for (int n = 0; n < nicheValues.Length; n++)
        {
            var niche = nicheValues[n];
            if (!_nicheToCategory.TryGetValue(niche, out var mapped) || mapped != selectedCat) continue;

            var projection = _cachedState.GetNicheDemandProjection(niche, 360);
            float demand = _cachedState.GetNicheDemand(niche);
            MarketTrend trend = _cachedState.GetNicheTrend(niche);

            bool isToggled;
            if (!_hasVisitedCurrentNicheChart)
            {
                isToggled = defaultToggleCount < 2;
                if (isToggled)
                {
                    _toggledNiches.Add(niche);
                    defaultToggleCount++;
                }
            }
            else
            {
                isToggled = _toggledNiches.Contains(niche);
            }

            _projections.Add(new NicheProjectionVM
            {
                Niche = niche,
                Name = niche.ToString(),
                LineColor = DemandLineChart.GetLineColor(colorIndex),
                IsToggled = isToggled,
                ProjectedDemand = projection,
                CurrentDemand = (int)demand,
                CurrentTrend = trend
            });

            colorIndex++;
        }

        _hasVisitedCurrentNicheChart = true;
    }

    private static string FormatCategoryPlural(ProductCategory? cat)
    {
        if (!cat.HasValue) return "";
        return FormatCategoryPlural(cat.Value);
    }

    private static string FormatCategoryPlural(ProductCategory cat)
    {
        switch (cat)
        {
            case ProductCategory.OperatingSystem:     return "Operating Systems";
            case ProductCategory.VideoGame:           return "Video Games";
            case ProductCategory.GameConsole:         return "Game Consoles";
            case ProductCategory.GameEngine:          return "Game Engines";
            case ProductCategory.GraphicsEditor:      return "Graphics Editors";
            case ProductCategory.AudioTool:           return "Audio Tools";
            case ProductCategory.DevFramework:        return "Dev Frameworks";
            default:                                  return UIFormatting.FormatCategory(cat);
        }
    }
}
