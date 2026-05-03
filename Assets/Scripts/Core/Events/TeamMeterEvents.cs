// TeamMeterEvents — Wave 3C
// Domain events emitted by TeamMeterSystem on meaningful state changes.
// Emitted only on: membership changes, assignment changes, morale threshold crosses,
// skill/attribute increases, chemistry changes — not every tick.

public struct TeamMetersChangedEvent
{
    public TeamId TeamId;
    public TeamMeterId[] ChangedMeters;
    public int Tick;

    public TeamMetersChangedEvent(TeamId teamId, TeamMeterId[] changedMeters, int tick)
    {
        TeamId       = teamId;
        ChangedMeters = changedMeters;
        Tick         = tick;
    }
}

public struct TeamRiskChangedEvent
{
    public TeamId TeamId;
    public string RiskType;   // "ScopeCreep", "LowReliability", etc.
    public string Severity;   // "Warning", "Critical"
    public int Tick;

    public TeamRiskChangedEvent(TeamId teamId, string riskType, string severity, int tick)
    {
        TeamId   = teamId;
        RiskType = riskType;
        Severity = severity;
        Tick     = tick;
    }
}
