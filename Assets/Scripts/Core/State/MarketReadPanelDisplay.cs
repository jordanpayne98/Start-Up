public struct MarketReadPanelDisplay
{
    public bool HasAnyReads;
    public string EmptyStateText;
    public int CardCount;
    public MarketReadCardDisplay Card0;
    public MarketReadCardDisplay Card1;
    public MarketReadCardDisplay Card2;
}

public struct MarketReadDelta
{
    public bool HasDelta;
    public string Message;
}
