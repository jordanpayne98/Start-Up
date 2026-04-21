public class MarketDemandCrashedEvent : GameEvent
{
    public bool IsSubCategory { get; }
    public int CategoryOrNicheValue { get; }
    public float NewDemand { get; }

    public MarketDemandCrashedEvent(int tick, bool isSubCategory, int categoryOrNicheValue, float newDemand)
        : base(tick)
    {
        IsSubCategory = isSubCategory;
        CategoryOrNicheValue = categoryOrNicheValue;
        NewDemand = newDemand;
    }
}
