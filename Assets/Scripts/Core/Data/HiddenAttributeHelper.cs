public static class HiddenAttributeHelper
{
    public const int AttributeCount = 7;

    private static readonly string[] _names =
    {
        "Learning Rate",
        "Ambition",
        "Loyalty",
        "Pressure Tolerance",
        "Ego",
        "Consistency",
        "Mentoring"
    };

    private static readonly string[] _stableIds =
    {
        "hidden.learning_rate",
        "hidden.ambition",
        "hidden.loyalty",
        "hidden.pressure_tolerance",
        "hidden.ego",
        "hidden.consistency",
        "hidden.mentoring"
    };

    public static string GetName(HiddenAttributeId id)
    {
        int idx = (int)id;
        if (idx >= 0 && idx < _names.Length) return _names[idx];
        return "Unknown";
    }

    public static string GetStableId(HiddenAttributeId id)
    {
        int idx = (int)id;
        if (idx >= 0 && idx < _stableIds.Length) return _stableIds[idx];
        return "hidden.unknown";
    }

    public static string GetLabel(int value)
    {
        if (value >= 17) return "Exceptional";
        if (value >= 13) return "High";
        if (value >= 9) return "Average";
        if (value >= 5) return "Low";
        return "Very Low";
    }
}
