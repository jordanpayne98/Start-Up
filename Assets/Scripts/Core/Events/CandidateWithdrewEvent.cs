public class CandidateWithdrewEvent : GameEvent
{
    public int CandidateId;
    public string CandidateName;

    public CandidateWithdrewEvent(int tick, int candidateId, string candidateName)
        : base(tick)
    {
        CandidateId = candidateId;
        CandidateName = candidateName;
    }
}
