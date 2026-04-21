public struct ShipProductCommand : ICommand
{
    public int Tick { get; set; }
    public ProductId ProductId;
    public ToolDistributionModel DistributionModel;
    public float LicensingRate;
}
