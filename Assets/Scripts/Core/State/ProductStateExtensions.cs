using System.Collections.Generic;

public static class ProductStateExtensions
{
    public static int CountOnMarketInCategory(this ProductState state, ProductCategory category)
    {
        int count = 0;
        foreach (KeyValuePair<ProductId, Product> kvp in state.shippedProducts)
        {
            if (kvp.Value.IsOnMarket && kvp.Value.Category == category)
                count++;
        }
        return count;
    }

    public static int CountOnMarketInCategory(this ProductState state, ProductCategory category, ProductId excludeId)
    {
        int count = 0;
        foreach (KeyValuePair<ProductId, Product> kvp in state.shippedProducts)
        {
            if (kvp.Key == excludeId) continue;
            if (kvp.Value.IsOnMarket && kvp.Value.Category == category)
                count++;
        }
        return count;
    }

    public static bool IsLastOnMarketInCategory(this ProductState state, ProductId productId)
    {
        if (!state.shippedProducts.TryGetValue(productId, out var product)) return false;
        if (!product.IsOnMarket) return false;
        return state.CountOnMarketInCategory(product.Category, productId) == 0;
    }

    public static bool IsCriticalCategory(this ProductCategory category)
    {
        return category.IsPlatform() || category.IsTool();
    }
}
