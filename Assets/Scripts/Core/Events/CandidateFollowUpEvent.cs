public class CandidateFollowUpEvent : GameEvent
{
    public int CandidateId;
    public string CandidateName;
    public int WithdrawalDeadlineTick;

    public CandidateFollowUpEvent(int tick, int candidateId, string candidateName, int withdrawalDeadlineTick)
        : base(tick)
    {
        CandidateId = candidateId;
        CandidateName = candidateName;
        WithdrawalDeadlineTick = withdrawalDeadlineTick;
    }
}
