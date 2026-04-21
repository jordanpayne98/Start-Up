using System;
using System.Collections.Generic;

[Serializable]
public class StockHolding
{
    public StockHoldingId Id;
    public CompetitorId TargetCompanyId;
    public CompetitorId OwnerCompanyId;
    public bool IsPlayerOwned;
    public float PercentageOwned;
    public long PurchasePrice;
    public int PurchaseTick;
}

[Serializable]
public class StockListing
{
    public CompetitorId CompanyId;
    public long StockPrice;
    public float UnownedPercentage;
    public Dictionary<CompetitorId, float> OwnershipBreakdown;
    public long LastDividendPayout;
    public int LastDividendTick;
}

[Serializable]
public class StockState
{
    public Dictionary<StockHoldingId, StockHolding> holdings;
    public Dictionary<CompetitorId, StockListing> listings;
    public int nextHoldingId;

    public static StockState CreateNew()
    {
        return new StockState
        {
            holdings = new Dictionary<StockHoldingId, StockHolding>(),
            listings = new Dictionary<CompetitorId, StockListing>(),
            nextHoldingId = 1
        };
    }
}
