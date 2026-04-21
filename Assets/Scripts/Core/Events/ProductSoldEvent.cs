public class ProductSoldEvent : GameEvent
{
    public ProductId ProductId;
    public CompetitorId BuyerId;
    public long SalePrice;

    public ProductSoldEvent(int tick, ProductId productId, CompetitorId buyerId, long salePrice) : base(tick)
    {
        ProductId = productId;
        BuyerId = buyerId;
        SalePrice = salePrice;
    }
}
