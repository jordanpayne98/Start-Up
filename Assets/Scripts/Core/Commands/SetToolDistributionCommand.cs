public struct SetToolDistributionCommand : ICommand
{
    public int Tick { get; set; }
    public ProductId ProductId;
    public ToolDistributionModel Model;
    public float LicensingRate;
    public float MonthlySubscriptionPrice;
}
