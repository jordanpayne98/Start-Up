public struct SetProductMaintenanceCommand : ICommand
{
    public int Tick { get; set; }
    public ProductId ProductId;
    public bool IsMaintained;
}
