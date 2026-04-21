using System;

[Serializable]
public struct MarketShareEntry
{
    public ProductId ProductId;
    public CompetitorId? OwnerId;
    public float Appeal;
    public float MarketSharePercent;
    public float PenetrationRate;
    public int ActiveUsers;
    public int MonthlyRevenue;
    public float GlobalUserSharePercent;
}
