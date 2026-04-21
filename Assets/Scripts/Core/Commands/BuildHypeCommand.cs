public struct BuildHypeCommand : ICommand
{
    public int Tick { get; set; }
    public ProductId ProductId;
    public int SpendAmount;
}
