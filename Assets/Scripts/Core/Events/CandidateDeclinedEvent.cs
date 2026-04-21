public class CandidateDeclinedEvent : GameEvent
{
    public int CandidateId;
    public string CandidateName;
    public string ConditionText;
    public int DeclineExpiryTick;

    public CandidateDeclinedEvent(int tick, int candidateId, string candidateName, string conditionText, int declineExpiryTick)
        : base(tick)
    {
        CandidateId = candidateId;
        CandidateName = candidateName;
        ConditionText = conditionText;
        DeclineExpiryTick = declineExpiryTick;
    }
}
