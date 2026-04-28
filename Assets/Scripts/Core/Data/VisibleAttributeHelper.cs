public static class VisibleAttributeHelper
{
    public const int AttributeCount = 8;

    private static readonly string[] _names =
    {
        "Leadership",
        "Creativity",
        "Focus",
        "Communication",
        "Adaptability",
        "Work Ethic",
        "Composure",
        "Initiative"
    };

    private static readonly string[] _stableIds =
    {
        "attr.leadership",
        "attr.creativity",
        "attr.focus",
        "attr.communication",
        "attr.adaptability",
        "attr.work_ethic",
        "attr.composure",
        "attr.initiative"
    };

    public static string GetName(VisibleAttributeId id)
    {
        int idx = (int)id;
        if (idx >= 0 && idx < _names.Length) return _names[idx];
        return "Unknown";
    }

    public static string GetStableId(VisibleAttributeId id)
    {
        int idx = (int)id;
        if (idx >= 0 && idx < _stableIds.Length) return _stableIds[idx];
        return "attr.unknown";
    }
}
