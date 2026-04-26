public class CandidateLostPatienceEvent : GameEvent
{
    public int CandidateId;
    public string CandidateName;

    public CandidateLostPatienceEvent(int tick, int candidateId, string candidateName)
        : base(tick)
    {
        CandidateId = candidateId;
        CandidateName = candidateName;
    }
}
