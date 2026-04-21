public class ProductLaunchedToMarketEvent : GameEvent
{
    public ProductId ProductId;
    public int LaunchRevenue;

    public ProductLaunchedToMarketEvent(int tick, ProductId productId, int launchRevenue) : base(tick)
    {
        ProductId = productId;
        LaunchRevenue = launchRevenue;
    }
}
