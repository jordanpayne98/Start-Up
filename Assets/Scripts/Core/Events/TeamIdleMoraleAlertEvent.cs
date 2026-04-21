public class TeamIdleMoraleAlertEvent : GameEvent
{
    public TeamId TeamId;
    public string TeamName;

    public TeamIdleMoraleAlertEvent(int tick, TeamId teamId, string teamName) : base(tick)
    {
        TeamId = teamId;
        TeamName = teamName;
    }
}
