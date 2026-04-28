public enum RoleWeightBand
{
    Ignored = 0,
    Tertiary = 1,
    Secondary = 2,
    Primary = 3
}

public static class RoleWeightBandHelper
{
    public static float ToWeight(RoleWeightBand band)
    {
        if (band == RoleWeightBand.Primary) return 1.00f;
        if (band == RoleWeightBand.Secondary) return 0.60f;
        if (band == RoleWeightBand.Tertiary) return 0.30f;
        return 0.0f;
    }
}
