public class HRCandidatesReadyForReviewEvent : GameEvent
{
    public TeamId TeamId { get; }
    public string TeamName { get; }
    public int[] CandidateIds { get; }
    public int CandidateCount { get; }
    public string CriteriaLabel { get; }

    public HRCandidatesReadyForReviewEvent(int tick, TeamId teamId, string teamName, int[] candidateIds, string criteriaLabel)
        : base(tick)
    {
        TeamId = teamId;
        TeamName = teamName;
        CandidateIds = candidateIds;
        CandidateCount = candidateIds != null ? candidateIds.Length : 0;
        CriteriaLabel = criteriaLabel;
    }
}
