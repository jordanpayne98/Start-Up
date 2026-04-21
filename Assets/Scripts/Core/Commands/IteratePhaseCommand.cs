public struct IteratePhaseCommand : ICommand
{
    public int Tick { get; set; }
    public ProductId ProductId;
    public ProductPhaseType PhaseType;
}
