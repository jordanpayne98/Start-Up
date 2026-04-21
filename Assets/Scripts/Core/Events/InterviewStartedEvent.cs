public class InterviewStartedEvent : GameEvent
{
    public int CandidateId { get; }

    public InterviewStartedEvent(int tick, int candidateId)
        : base(tick)
    {
        CandidateId = candidateId;
    }
}
