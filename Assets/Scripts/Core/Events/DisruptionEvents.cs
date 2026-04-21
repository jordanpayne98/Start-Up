public class MinorDisruptionStartedEvent : GameEvent
{
    public string Description;
    public DisruptionEventType EventType;

    public MinorDisruptionStartedEvent(int tick, ActiveDisruption disruption) : base(tick)
    {
        Description = disruption?.Description ?? string.Empty;
        EventType = disruption?.EventType ?? DisruptionEventType.NicheDemandShift;
    }
}

public class MajorDisruptionStartedEvent : GameEvent
{
    public string Description;
    public DisruptionEventType EventType;

    public MajorDisruptionStartedEvent(int tick, ActiveDisruption disruption) : base(tick)
    {
        Description = disruption?.Description ?? string.Empty;
        EventType = disruption?.EventType ?? DisruptionEventType.Recession;
    }
}

public class ShowdownResolvedEvent : GameEvent
{
    public ShowdownResult Result;
    public ProductNiche Niche;

    public ShowdownResolvedEvent(int tick, ProductNiche niche, ShowdownResult result) : base(tick)
    {
        Niche = niche;
        Result = result;
    }
}
