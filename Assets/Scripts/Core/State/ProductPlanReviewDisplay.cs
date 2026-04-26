public enum ProductPlanReviewCardType
{
    None,
    ScopePressure,
    MissingExpectedFeatures,
    PriceExpectationMismatch,
    PlatformBottleneck,
    TightReleasePlan
}

public enum ProductPlanReviewStatus
{
    Risk = 0,
    Opportunity = 1,
    Tradeoff = 2
}

public sealed class ProductPlanReviewCardDisplay
{
    public bool IsVisible;
    public ProductPlanReviewCardType Type;
    public ProductPlanReviewStatus Status;
    public string Title;
    public string StatusText;
    public string WhyText;
    public string EffectText;
    public string ChangeText;
    public TooltipData Tooltip;
}

public sealed class ProductPlanReviewDisplay
{
    public bool HasCards;
    public string EmptyText;
    public int CardCount;
    public readonly ProductPlanReviewCardDisplay[] Cards;

    public ProductPlanReviewDisplay()
    {
        Cards = new ProductPlanReviewCardDisplay[3];
        for (int i = 0; i < 3; i++)
            Cards[i] = new ProductPlanReviewCardDisplay();
    }

    public void Reset()
    {
        HasCards = false;
        EmptyText = "";
        CardCount = 0;
        for (int i = 0; i < 3; i++)
            Cards[i].IsVisible = false;
    }
}
