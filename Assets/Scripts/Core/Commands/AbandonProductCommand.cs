public struct AbandonProductCommand : ICommand
{
    public int Tick { get; set; }
    public ProductId ProductId;
}
