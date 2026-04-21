public struct ReleaseDateChangedEvent
{
    public ProductId ProductId;
    public int OldTick;
    public int NewTick;
    public bool IsRush;
    public int ShiftCount;
}
