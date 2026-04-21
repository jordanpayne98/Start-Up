public class BuyStockCommand : ICommand
{
    public int Tick { get; private set; }
    public CompetitorId TargetCompanyId { get; private set; }
    public float Percentage { get; private set; }
    public CompetitorId? FromInvestorId { get; private set; }

    public BuyStockCommand(int tick, CompetitorId targetCompanyId, float percentage, CompetitorId? fromInvestorId = null)
    {
        Tick = tick;
        TargetCompanyId = targetCompanyId;
        Percentage = percentage;
        FromInvestorId = fromInvestorId;
    }
}

public class SellStockCommand : ICommand
{
    public int Tick { get; private set; }
    public CompetitorId TargetCompanyId { get; private set; }
    public float Percentage { get; private set; }

    public SellStockCommand(int tick, CompetitorId targetCompanyId, float percentage)
    {
        Tick = tick;
        TargetCompanyId = targetCompanyId;
        Percentage = percentage;
    }
}

public class SellProductToCompetitorCommand : ICommand
{
    public int Tick { get; private set; }
    public ProductId ProductId { get; private set; }
    public CompetitorId BuyerCompetitorId { get; private set; }

    public SellProductToCompetitorCommand(int tick, ProductId productId, CompetitorId buyerCompetitorId)
    {
        Tick = tick;
        ProductId = productId;
        BuyerCompetitorId = buyerCompetitorId;
    }
}
