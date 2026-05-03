// TeamImpactResult — Wave 3C
// Per-meter deltas for employee contribution or candidate projected impact.
// For candidate projections, the Min/Max fields give the confidence-widened range.

public struct TeamImpactResult
{
    public int CreativityDelta;
    public int CoordinationDelta;
    public int ReliabilityDelta;
    public int TechnicalStrengthDelta;
    public int CommercialAwarenessDelta;

    // Confidence-range fields (populated only for candidate projections)
    public int CreativityDeltaMin;
    public int CreativityDeltaMax;
    public int CoordinationDeltaMin;
    public int CoordinationDeltaMax;
    public int ReliabilityDeltaMin;
    public int ReliabilityDeltaMax;
    public int TechnicalStrengthDeltaMin;
    public int TechnicalStrengthDeltaMax;
    public int CommercialAwarenessDeltaMin;
    public int CommercialAwarenessDeltaMax;

    public TeamMeterConfidence Confidence;

    public static TeamImpactResult Zero()
    {
        return new TeamImpactResult
        {
            Confidence = TeamMeterConfidence.Confirmed
        };
    }
}

/// <summary>Maps a delta value to a human-readable impact label.</summary>
public enum ImpactLabel
{
    MajorNegative,
    Negative,
    SlightNegative,
    Neutral,
    SlightPositive,
    Positive,
    MajorPositive
}

public static class ImpactLabelHelper
{
    public static ImpactLabel FromDelta(int delta)
    {
        if (delta <= -20) return ImpactLabel.MajorNegative;
        if (delta <= -10) return ImpactLabel.Negative;
        if (delta <= -1)  return ImpactLabel.SlightNegative;
        if (delta == 0)   return ImpactLabel.Neutral;
        if (delta <= 9)   return ImpactLabel.SlightPositive;
        if (delta <= 19)  return ImpactLabel.Positive;
        return ImpactLabel.MajorPositive;
    }

    public static string ToDisplayString(ImpactLabel label)
    {
        switch (label)
        {
            case ImpactLabel.MajorNegative:  return "Major Negative";
            case ImpactLabel.Negative:       return "Negative";
            case ImpactLabel.SlightNegative: return "Slight Negative";
            case ImpactLabel.Neutral:        return "Neutral";
            case ImpactLabel.SlightPositive: return "Slight Positive";
            case ImpactLabel.Positive:       return "Positive";
            case ImpactLabel.MajorPositive:  return "Major Positive";
            default:                         return "Neutral";
        }
    }
}
