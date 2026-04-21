public class StockPurchasedEvent : GameEvent
{
    public CompetitorId Buyer { get; }
    public CompetitorId Target { get; }
    public float Percentage { get; }
    public long Price { get; }

    public StockPurchasedEvent(int tick, CompetitorId buyer, CompetitorId target, float percentage, long price) : base(tick)
    {
        Buyer = buyer;
        Target = target;
        Percentage = percentage;
        Price = price;
    }
}

public class StockSoldEvent : GameEvent
{
    public CompetitorId Seller { get; }
    public CompetitorId Target { get; }
    public float Percentage { get; }
    public long Proceeds { get; }

    public StockSoldEvent(int tick, CompetitorId seller, CompetitorId target, float percentage, long proceeds) : base(tick)
    {
        Seller = seller;
        Target = target;
        Percentage = percentage;
        Proceeds = proceeds;
    }
}

public class CompanyAcquiredEvent : GameEvent
{
    public CompetitorId Acquirer { get; }
    public CompetitorId Target { get; }

    public CompanyAcquiredEvent(int tick, CompetitorId acquirer, CompetitorId target) : base(tick)
    {
        Acquirer = acquirer;
        Target = target;
    }
}

public class DividendPaidEvent : GameEvent
{
    public CompetitorId Owner { get; }
    public long Amount { get; }

    public DividendPaidEvent(int tick, CompetitorId owner, long amount) : base(tick)
    {
        Owner = owner;
        Amount = amount;
    }
}

public class PlayerAcquiredEvent : GameEvent
{
    public CompetitorId Acquirer { get; }

    public PlayerAcquiredEvent(int tick, CompetitorId acquirer) : base(tick)
    {
        Acquirer = acquirer;
    }
}

public class ProductSoldToCompetitorEvent : GameEvent
{
    public ProductId ProductId { get; }
    public CompetitorId Buyer { get; }
    public long Price { get; }

    public ProductSoldToCompetitorEvent(int tick, ProductId productId, CompetitorId buyer, long price) : base(tick)
    {
        ProductId = productId;
        Buyer = buyer;
        Price = price;
    }
}
