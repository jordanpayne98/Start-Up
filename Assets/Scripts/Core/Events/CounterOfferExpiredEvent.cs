public class CounterOfferExpiredEvent : GameEvent
{
    public int CandidateId;
    public string CandidateName;

    public CounterOfferExpiredEvent(int tick, int candidateId, string candidateName)
        : base(tick)
    {
        CandidateId = candidateId;
        CandidateName = candidateName;
    }
}
