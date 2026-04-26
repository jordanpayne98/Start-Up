public class TeamDeletedEvent : GameEvent
{
    public TeamId TeamId { get; }

    public TeamDeletedEvent(int tick, TeamId teamId) : base(tick)
    {
        TeamId = teamId;
    }
}
