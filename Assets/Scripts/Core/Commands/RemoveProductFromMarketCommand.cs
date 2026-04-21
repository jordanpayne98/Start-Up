public struct RemoveProductFromMarketCommand : ICommand
{
    public int Tick { get; set; }
    public ProductId ProductId;
}
