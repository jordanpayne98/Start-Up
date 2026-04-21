public class CompetitorSpawnedEvent : GameEvent
{
    public CompetitorId Id;

    public CompetitorSpawnedEvent(int tick, CompetitorId id) : base(tick)
    {
        Id = id;
    }
}

public class CompetitorBankruptEvent : GameEvent
{
    public CompetitorId Id;

    public CompetitorBankruptEvent(int tick, CompetitorId id) : base(tick)
    {
        Id = id;
    }
}

public class CompetitorAbsorbedEvent : GameEvent
{
    public CompetitorId AbsorbedId;
    public CompetitorId AbsorberId;

    public CompetitorAbsorbedEvent(int tick, CompetitorId absorbedId, CompetitorId absorberId) : base(tick)
    {
        AbsorbedId = absorbedId;
        AbsorberId = absorberId;
    }
}

public class CompetitorProductLaunchedEvent : GameEvent
{
    public CompetitorId CompetitorId;
    public ProductId ProductId;
    public ProductNiche Niche;

    public CompetitorProductLaunchedEvent(int tick, CompetitorId competitorId, ProductId productId, ProductNiche niche) : base(tick)
    {
        CompetitorId = competitorId;
        ProductId = productId;
        Niche = niche;
    }
}

public class CompetitorDevStartedEvent : GameEvent
{
    public CompetitorId CompetitorId;
    public ProductId ProductId;
    public ProductNiche Niche;

    public CompetitorDevStartedEvent(int tick, CompetitorId competitorId, ProductId productId, ProductNiche niche) : base(tick)
    {
        CompetitorId = competitorId;
        ProductId = productId;
        Niche = niche;
    }
}

public class CompetitorProductSunsetEvent : GameEvent
{
    public CompetitorId CompetitorId;
    public ProductId ProductId;

    public CompetitorProductSunsetEvent(int tick, CompetitorId competitorId, ProductId productId) : base(tick)
    {
        CompetitorId = competitorId;
        ProductId = productId;
    }
}

public class CompetitorProductUpdatedEvent : GameEvent
{
    public CompetitorId CompetitorId;
    public ProductId ProductId;
    public bool IsMajorExpansion;

    public CompetitorProductUpdatedEvent(int tick, CompetitorId competitorId, ProductId productId, bool isMajorExpansion) : base(tick)
    {
        CompetitorId = competitorId;
        ProductId = productId;
        IsMajorExpansion = isMajorExpansion;
    }
}
