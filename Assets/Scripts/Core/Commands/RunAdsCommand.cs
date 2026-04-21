public struct RunAdsCommand : ICommand
{
    public int Tick { get; set; }
    public ProductId ProductId;
    public int SpendAmount;
}
