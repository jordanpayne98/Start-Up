// TeamMeterSnapshot — Wave 3C
// Cached result of a team's 5 derived meters (0-100 each).
// TeamMeterId, TeamMeterConfidence, and TeamMeterLabel are co-located here for cohesion.

public struct TeamMeterSnapshot
{
    public TeamId TeamId;
    public int Creativity;           // 0-100
    public int Coordination;         // 0-100
    public int Reliability;          // 0-100
    public int TechnicalStrength;    // 0-100
    public int CommercialAwareness;  // 0-100
    public TeamMeterConfidence Confidence;
    public int LastCalculatedTick;

    public static TeamMeterSnapshot Empty(TeamId teamId)
    {
        return new TeamMeterSnapshot
        {
            TeamId             = teamId,
            Creativity         = 0,
            Coordination       = 0,
            Reliability        = 0,
            TechnicalStrength  = 0,
            CommercialAwareness = 0,
            Confidence         = TeamMeterConfidence.Low,
            LastCalculatedTick = 0
        };
    }
}

public enum TeamMeterConfidence
{
    Low,
    Medium,
    High,
    Confirmed
}

public enum TeamMeterId
{
    Creativity,
    Coordination,
    Reliability,
    TechnicalStrength,
    CommercialAwareness
}

public enum TeamMeterLabel
{
    // Standard labels (Coordination, Reliability, TechnicalStrength, CommercialAwareness)
    VeryWeak,
    Weak,
    Functional,
    Strong,
    Excellent,
    Elite,
    // Creativity flavour labels
    Rigid,
    Conventional,
    Capable,
    Inventive,
    Visionary,
    Breakthrough
}
