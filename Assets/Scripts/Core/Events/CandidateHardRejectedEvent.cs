public class CandidateHardRejectedEvent : GameEvent
{
    public int CandidateId;

    public CandidateHardRejectedEvent(int tick, int candidateId)
        : base(tick)
    {
        CandidateId = candidateId;
    }
}
