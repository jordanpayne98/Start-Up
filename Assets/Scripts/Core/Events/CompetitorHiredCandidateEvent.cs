public class CompetitorHiredCandidateEvent : GameEvent
{
    public CompetitorId CompetitorId;
    public string CandidateName;
    public RoleId Role;
    public string CompanyName;

    public CompetitorHiredCandidateEvent(int tick, CompetitorId competitorId, string candidateName, RoleId role, string companyName)
        : base(tick)
    {
        CompetitorId = competitorId;
        CandidateName = candidateName;
        Role = role;
        CompanyName = companyName;
    }
}
