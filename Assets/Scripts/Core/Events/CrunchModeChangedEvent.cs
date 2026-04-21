public class CrunchModeChangedEvent : GameEvent
{
    public TeamId TeamId { get; }
    public bool IsCrunching { get; }

    public CrunchModeChangedEvent(int tick, TeamId teamId, bool isCrunching) : base(tick)
    {
        TeamId = teamId;
        IsCrunching = isCrunching;
    }
}
