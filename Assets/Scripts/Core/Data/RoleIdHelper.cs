public static class RoleIdHelper
{
    public const int RoleCount = 16;

    private static readonly string[] _names =
    {
        "Software Engineer",
        "Systems Engineer",
        "Security Engineer",
        "Performance Engineer",
        "Hardware Engineer",
        "Manufacturing Engineer",
        "Product Designer",
        "Game Designer",
        "Technical Artist",
        "Audio Designer",
        "QA Engineer",
        "Technical Support Specialist",
        "Marketer",
        "Sales Executive",
        "Accountant",
        "HR Specialist"
    };

    private static readonly string[] _stableIds =
    {
        "role.software_engineer",
        "role.systems_engineer",
        "role.security_engineer",
        "role.performance_engineer",
        "role.hardware_engineer",
        "role.manufacturing_engineer",
        "role.product_designer",
        "role.game_designer",
        "role.technical_artist",
        "role.audio_designer",
        "role.qa_engineer",
        "role.technical_support_specialist",
        "role.marketer",
        "role.sales_executive",
        "role.accountant",
        "role.hr_specialist"
    };

    private static readonly RoleFamily[] _families =
    {
        RoleFamily.Engineering,
        RoleFamily.Engineering,
        RoleFamily.Engineering,
        RoleFamily.Engineering,
        RoleFamily.Hardware,
        RoleFamily.Hardware,
        RoleFamily.Product,
        RoleFamily.Product,
        RoleFamily.Creative,
        RoleFamily.Creative,
        RoleFamily.QualityAndSupport,
        RoleFamily.QualityAndSupport,
        RoleFamily.Commercial,
        RoleFamily.Commercial,
        RoleFamily.Operations,
        RoleFamily.Operations
    };

    public static string GetName(RoleId id)
    {
        int idx = (int)id;
        if (idx >= 0 && idx < _names.Length) return _names[idx];
        return "Unknown";
    }

    public static string GetStableId(RoleId id)
    {
        int idx = (int)id;
        if (idx >= 0 && idx < _stableIds.Length) return _stableIds[idx];
        return "role.unknown";
    }

    public static RoleFamily GetFamily(RoleId id)
    {
        int idx = (int)id;
        if (idx >= 0 && idx < _families.Length) return _families[idx];
        return RoleFamily.Engineering;
    }

}
