public enum PivotAction { Drop, Add, Swap }

public struct PivotFeatureCommand : ICommand
{
    public int Tick { get; set; }
    public ProductId ProductId;
    public PivotAction Action;
    public string RemoveFeatureId;  // null for Add
    public string AddFeatureId;     // null for Drop
}
