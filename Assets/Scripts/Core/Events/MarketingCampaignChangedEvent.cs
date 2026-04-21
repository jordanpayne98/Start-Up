public class MarketingCampaignChangedEvent : GameEvent
{
    public ProductId ProductId;
    public bool IsActive;

    public MarketingCampaignChangedEvent(int tick, ProductId productId, bool isActive) : base(tick)
    {
        ProductId = productId;
        IsActive = isActive;
    }
}
