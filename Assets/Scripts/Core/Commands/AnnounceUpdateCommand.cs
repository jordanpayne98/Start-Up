public struct AnnounceUpdateCommand : ICommand
{
    public int Tick { get; set; }
    public ProductId ProductId;
}
