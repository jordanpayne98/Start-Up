public struct SetCrunchModeCommand : ICommand
{
    public int Tick { get; set; }
    public TeamId TeamId;
    public bool Enable;
}
