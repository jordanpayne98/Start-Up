public class AutoActionTakenEvent : GameEvent
{
    public string ActionDescription { get; }

    public AutoActionTakenEvent(int tick, string actionDescription) : base(tick) {
        ActionDescription = actionDescription;
    }
}
