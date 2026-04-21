public class MarketDemandUpdatedEvent : GameEvent
{
    public bool IsSubCategory { get; }
    public int CategoryOrNicheValue { get; }
    public float NewDemand { get; }
    public MarketTrend Trend { get; }

    public MarketDemandUpdatedEvent(int tick, bool isSubCategory, int categoryOrNicheValue, float newDemand, MarketTrend trend)
        : base(tick)
    {
        IsSubCategory = isSubCategory;
        CategoryOrNicheValue = categoryOrNicheValue;
        NewDemand = newDemand;
        Trend = trend;
    }
}
