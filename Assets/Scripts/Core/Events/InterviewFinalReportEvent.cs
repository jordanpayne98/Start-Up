public class InterviewFinalReportEvent : GameEvent
{
    public int CandidateId { get; }
    public string CandidateName { get; }

    public InterviewFinalReportEvent(int tick, int candidateId, string candidateName)
        : base(tick)
    {
        CandidateId = candidateId;
        CandidateName = candidateName;
    }
}
