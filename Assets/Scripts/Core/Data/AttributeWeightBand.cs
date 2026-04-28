public enum AttributeWeightBand
{
    Irrelevant = 0,
    Minor = 1,
    Useful = 2,
    Critical = 3
}

public static class AttributeWeightBandHelper
{
    public static float ToWeight(AttributeWeightBand band)
    {
        if (band == AttributeWeightBand.Critical) return 0.35f;
        if (band == AttributeWeightBand.Useful) return 0.20f;
        if (band == AttributeWeightBand.Minor) return 0.10f;
        return 0.0f;
    }
}
