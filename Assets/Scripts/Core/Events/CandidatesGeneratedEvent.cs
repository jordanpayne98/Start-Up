public class CandidatesGeneratedEvent : GameEvent
{
    public int Count { get; }
    
    public CandidatesGeneratedEvent(int tick, int count) : base(tick)
    {
        Count = count;
    }
}
