public class NewsArticleAddedEvent : GameEvent
{
    public HypeEventType EventType { get; }
    public ProductId ProductId { get; }
    public string ProductName { get; }
    public string Headline { get; }
    public bool WasMitigated { get; }

    public NewsArticleAddedEvent(int tick, HypeEventType eventType, ProductId productId,
        string productName, string headline, bool wasMitigated) : base(tick)
    {
        EventType = eventType;
        ProductId = productId;
        ProductName = productName;
        Headline = headline;
        WasMitigated = wasMitigated;
    }
}
