public static class HRSearchConfig
{
    // --- Tuning reference (set at startup by GameController) ---
    private static TuningConfig _tuning;
    public static void SetTuningConfig(TuningConfig tuning) { _tuning = tuning; }

    // --- Const fallback defaults ---
    private const int DefaultBaseSearchCost            = 2500;
    private const int DefaultBaseDurationDays          = 7;
    private const int DefaultMinDurationDays           = 2;
    private const float DefaultBaseSuccessChance       = 0.30f;
    private const float DefaultMaxSuccessChance        = 0.95f;
    private const float DefaultSkillSuccessScaleFactor = 0.006f;
    private const float DefaultTeamSizeSpeedBonusPerMember = 0.08f;
    private const int DefaultMaxTeamSizeForSpeedBonus  = 5;

    // --- Properties — read from TuningConfig when available ---
    public static int   BaseSearchCost              => _tuning != null ? _tuning.HRBaseSearchCost              : DefaultBaseSearchCost;
    public static int   BaseDurationDays            => _tuning != null ? _tuning.HRBaseDurationDays            : DefaultBaseDurationDays;
    public static int   MinDurationDays             => _tuning != null ? _tuning.HRMinDurationDays             : DefaultMinDurationDays;
    public static float BaseSuccessChance           => _tuning != null ? _tuning.HRBaseSuccessChance           : DefaultBaseSuccessChance;
    public static float MaxSuccessChance            => _tuning != null ? _tuning.HRMaxSuccessChance            : DefaultMaxSuccessChance;
    public static float SkillSuccessScaleFactor     => _tuning != null ? _tuning.HRSkillSuccessScaleFactor     : DefaultSkillSuccessScaleFactor;
    public static float TeamSizeSpeedBonusPerMember => _tuning != null ? _tuning.HRTeamSizeSpeedBonusPerMember : DefaultTeamSizeSpeedBonusPerMember;
    public static int   MaxTeamSizeForSpeedBonus    => _tuning != null ? _tuning.HRMaxTeamSizeForSpeedBonus    : DefaultMaxTeamSizeForSpeedBonus;

    // Reserved for future upgrade scaling
    public const string MaxHRTeamsChannel = "hr_max_teams";
}
