public struct AnnounceReleaseDateCommand : ICommand
{
    public int Tick { get; set; }
    public ProductId ProductId;
    public int TargetDay;
}
