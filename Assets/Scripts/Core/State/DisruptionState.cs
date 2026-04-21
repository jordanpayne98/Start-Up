using System;
using System.Collections.Generic;

[Serializable]
public class ActiveDisruption
{
    public int Id;
    public DisruptionEventType EventType;
    public bool IsMajor;
    public int StartTick;
    public int DurationTicks;
    public ProductNiche? AffectedNiche;
    public CompetitorId? AffectedCompetitor;
    public float Magnitude;
    public string Description;
}

[Serializable]
public class DisruptionState
{
    public List<ActiveDisruption> activeDisruptions;
    public int nextDisruptionId;
    public int lastMinorCheckTick;
    public int lastMajorCheckTick;

    public static DisruptionState CreateNew()
    {
        return new DisruptionState
        {
            activeDisruptions = new List<ActiveDisruption>(),
            nextDisruptionId = 1,
            lastMinorCheckTick = 0,
            lastMajorCheckTick = 0
        };
    }
}
