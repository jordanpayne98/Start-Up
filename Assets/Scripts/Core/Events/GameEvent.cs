public abstract class GameEvent
{
    public int Tick { get; protected set; }
    
    protected GameEvent(int tick)
    {
        Tick = tick;
    }
}
