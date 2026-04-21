using System;
using System.Collections.Generic;

[Serializable]
public struct PlatformMarketEntry {
    public ProductId PlatformId;
    public CompetitorId? OwnerId;
    public float MarketSharePercent;
    public int InstallBase;
    public int EcosystemProductCount;
    public float LicensingRate;
    public float QualityCeiling;
}

[Serializable]
public class PlatformState {
    public Dictionary<ProductId, PlatformMarketEntry> platformShares;
    public Dictionary<ProductCategory, float> genericPlatformCeiling;
    public int lastPlatformUpdateTick;

    public static PlatformState CreateNew() {
        var state = new PlatformState {
            platformShares = new Dictionary<ProductId, PlatformMarketEntry>(),
            genericPlatformCeiling = new Dictionary<ProductCategory, float>(),
            lastPlatformUpdateTick = 0
        };

        var categories = (ProductCategory[])Enum.GetValues(typeof(ProductCategory));
        for (int i = 0; i < categories.Length; i++)
            state.genericPlatformCeiling[categories[i]] = 65f;

        return state;
    }
}
