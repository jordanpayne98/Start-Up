public enum MoraleBand
{
    Critical,
    Miserable,
    Unhappy,
    Stable,
    Motivated,
    Inspired
}

public static class MoraleBandHelper
{
    // 0-19 Critical, 20-34 Miserable, 35-54 Unhappy, 55-74 Stable, 75-89 Motivated, 90+ Inspired
    public static MoraleBand GetMoraleBand(float morale)
    {
        if (morale >= 90f) return MoraleBand.Inspired;
        if (morale >= 75f) return MoraleBand.Motivated;
        if (morale >= 55f) return MoraleBand.Stable;
        if (morale >= 35f) return MoraleBand.Unhappy;
        if (morale >= 20f) return MoraleBand.Miserable;
        return MoraleBand.Critical;
    }

    public static string GetMoraleBandLabel(MoraleBand band)
    {
        switch (band)
        {
            case MoraleBand.Inspired:  return "Inspired";
            case MoraleBand.Motivated: return "Motivated";
            case MoraleBand.Stable:    return "Stable";
            case MoraleBand.Unhappy:   return "Unhappy";
            case MoraleBand.Miserable: return "Miserable";
            default:                   return "Critical";
        }
    }
}
