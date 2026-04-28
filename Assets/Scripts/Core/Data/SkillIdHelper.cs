public static class SkillIdHelper
{
    public const int SkillCount = 26;

    private static readonly string[] _names =
    {
        "Programming",
        "Systems Architecture",
        "Performance Optimisation",
        "Security",
        "CPU Engineering",
        "GPU Engineering",
        "Hardware Integration",
        "Manufacturing",
        "Product Design",
        "UX/UI Design",
        "Game Design",
        "User Research",
        "Visual Art",
        "VFX",
        "Audio Design",
        "Writing & Content",
        "QA Testing",
        "Bug Fixing",
        "Release Management",
        "Technical Support",
        "Marketing",
        "Brand Management",
        "Sales",
        "Negotiation",
        "HR & Recruitment",
        "Accountancy"
    };

    private static readonly string[] _stableIds =
    {
        "skill.programming",
        "skill.systems_architecture",
        "skill.performance_optimisation",
        "skill.security",
        "skill.cpu_engineering",
        "skill.gpu_engineering",
        "skill.hardware_integration",
        "skill.manufacturing",
        "skill.product_design",
        "skill.ux_ui_design",
        "skill.game_design",
        "skill.user_research",
        "skill.visual_art",
        "skill.vfx",
        "skill.audio_design",
        "skill.writing_content",
        "skill.qa_testing",
        "skill.bug_fixing",
        "skill.release_management",
        "skill.technical_support",
        "skill.marketing",
        "skill.brand_management",
        "skill.sales",
        "skill.negotiation",
        "skill.hr_recruitment",
        "skill.accountancy"
    };

    private static readonly SkillCategory[] _categories =
    {
        SkillCategory.SoftwareEngineering,
        SkillCategory.SoftwareEngineering,
        SkillCategory.SoftwareEngineering,
        SkillCategory.SoftwareEngineering,
        SkillCategory.HardwareEngineering,
        SkillCategory.HardwareEngineering,
        SkillCategory.HardwareEngineering,
        SkillCategory.HardwareEngineering,
        SkillCategory.ProductAndUx,
        SkillCategory.ProductAndUx,
        SkillCategory.ProductAndUx,
        SkillCategory.ProductAndUx,
        SkillCategory.CreativeProduction,
        SkillCategory.CreativeProduction,
        SkillCategory.CreativeProduction,
        SkillCategory.CreativeProduction,
        SkillCategory.QualityAndDelivery,
        SkillCategory.QualityAndDelivery,
        SkillCategory.QualityAndDelivery,
        SkillCategory.QualityAndDelivery,
        SkillCategory.Commercial,
        SkillCategory.Commercial,
        SkillCategory.Commercial,
        SkillCategory.Commercial,
        SkillCategory.CompanyOperations,
        SkillCategory.CompanyOperations
    };

    private static readonly int[] _categoryStartIndex =
    {
        0,  // SoftwareEngineering
        4,  // HardwareEngineering
        8,  // ProductAndUx
        12, // CreativeProduction
        16, // QualityAndDelivery
        20, // Commercial
        24  // CompanyOperations
    };

    private static readonly int[] _categoryCounts =
    {
        4, // SoftwareEngineering
        4, // HardwareEngineering
        4, // ProductAndUx
        4, // CreativeProduction
        4, // QualityAndDelivery
        4, // Commercial
        2  // CompanyOperations
    };

    public static string GetName(SkillId id)
    {
        int idx = (int)id;
        if (idx >= 0 && idx < _names.Length) return _names[idx];
        return "Unknown";
    }

    public static string GetStableId(SkillId id)
    {
        int idx = (int)id;
        if (idx >= 0 && idx < _stableIds.Length) return _stableIds[idx];
        return "skill.unknown";
    }

    public static SkillCategory GetCategory(SkillId id)
    {
        int idx = (int)id;
        if (idx >= 0 && idx < _categories.Length) return _categories[idx];
        return SkillCategory.SoftwareEngineering;
    }

    public static int GetCategoryStartIndex(SkillCategory cat)
    {
        int idx = (int)cat;
        if (idx >= 0 && idx < _categoryStartIndex.Length) return _categoryStartIndex[idx];
        return 0;
    }

    public static int GetCategoryCount(SkillCategory cat)
    {
        int idx = (int)cat;
        if (idx >= 0 && idx < _categoryCounts.Length) return _categoryCounts[idx];
        return 0;
    }
}
