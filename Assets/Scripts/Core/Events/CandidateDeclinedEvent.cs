public enum DeclineReason
{
    SalaryTooLow,
    ArrangementMismatch,
    Generic,
    RoleMismatch,
    PatienceExhausted
}

public class CandidateDeclinedEvent : GameEvent
{
    public int CandidateId;
    public string CandidateName;
    public string ConditionText;
    public int DeclineExpiryTick;
    public DeclineReason Reason;

    public CandidateDeclinedEvent(int tick, int candidateId, string candidateName, string conditionText, int declineExpiryTick, DeclineReason reason = DeclineReason.Generic)
        : base(tick)
    {
        CandidateId = candidateId;
        CandidateName = candidateName;
        ConditionText = conditionText;
        DeclineExpiryTick = declineExpiryTick;
        Reason = reason;
    }
}
