public struct SpawnShippedProductCheatCommand : ICommand
{
    public int Tick { get; set; }
    public string TemplateId;
    public string ProductName;
    public string[] SelectedFeatureIds;
    public bool IsSubscriptionBased;
    public float Price;
    public float OverallQuality;
}
