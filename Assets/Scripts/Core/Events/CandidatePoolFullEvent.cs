public class CandidatePoolFullEvent : GameEvent
{
    public int PoolCount;
    public int PoolMax;
    public int RejectedCount;

    public CandidatePoolFullEvent(int tick, int poolCount, int poolMax, int rejectedCount)
        : base(tick)
    {
        PoolCount = poolCount;
        PoolMax = poolMax;
        RejectedCount = rejectedCount;
    }
}
