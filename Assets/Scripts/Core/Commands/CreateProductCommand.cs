public struct TeamAssignment
{
    public ProductTeamRole Role;
    public TeamId TeamId;
}

public struct CreateProductCommand : ICommand
{
    public int Tick { get; set; }
    public string TemplateId;
    public string ProductName;
    public string[] SelectedFeatureIds;
    public bool IsSubscriptionBased;
    public float Price;
    public ProductId[] TargetPlatformIds;
    public ProductId[] RequiredToolIds;
    public GenerationStance Stance;
    public ProductId? PredecessorProductId;
    public TeamAssignment[] InitialTeamAssignments;
    public ProductId? SequelOfId;
    public bool HasHardwareConfig;
    public HardwareConfiguration HardwareConfig;
    public int TargetDay;
    public ToolDistributionModel DistributionModel;
    public float LicensingRate;
    public float MonthlySubscriptionPrice;
    public ProductNiche SelectedNiche;
}
