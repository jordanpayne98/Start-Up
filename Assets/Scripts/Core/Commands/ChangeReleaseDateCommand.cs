public struct ChangeReleaseDateCommand : ICommand
{
    public int Tick { get; set; }
    public ProductId ProductId;
    public int NewTargetDay;
}
