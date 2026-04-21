public struct TriggerProductUpdateCommand : ICommand
{
    public int Tick { get; set; }
    public ProductId ProductId;
    public ProductUpdateType UpdateType;
    public string[] FeatureIds;
    public TeamAssignment[] TeamAssignments;
}
