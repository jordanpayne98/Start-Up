public class TeamCreatedEvent : GameEvent
{
    public TeamId TeamId { get; }
    public string TeamName { get; }
    
    public TeamCreatedEvent(int tick, TeamId teamId, string teamName) : base(tick)
    {
        TeamId = teamId;
        TeamName = teamName;
    }
}
