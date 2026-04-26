public class InterviewThresholdEvent : GameEvent
{
    public int CandidateId { get; }
    public string CandidateName { get; }
    public int ThresholdReached { get; } // 20, 40, 60, 80, or 100

    public InterviewThresholdEvent(int tick, int candidateId, string candidateName, int thresholdReached)
        : base(tick)
    {
        CandidateId = candidateId;
        CandidateName = candidateName;
        ThresholdReached = thresholdReached;
    }
}
