public struct TriggerSaleEventCommand : ICommand
{
    public int Tick { get; set; }
    public ProductId ProductId;
}
