// Pure static utility — no state, no Unity refs, no allocations.
// Computes role suitability using the weighted average CA formula (Wave 3A).
public static class RoleSuitabilityCalculator
{
    private const int NaturalThreshold      = 140;
    private const int AccomplishedThreshold = 100;
    private const int CompetentThreshold    = 60;
    private const int AwkwardThreshold      = 30;

    private static readonly RoleId[] _allRoles = (RoleId[])System.Enum.GetValues(typeof(RoleId));
    public static RoleId[] AllRoles => _allRoles;

    // Computes CA for a role using the weighted average formula.
    // Accepts RoleWeightBand[] directly — no tier array needed.
    public static int ComputeAbilityForRole(int[] skills, RoleWeightBand[] skillBands)
    {
        return AbilityCalculator.ComputeRoleCA(skills, skillBands);
    }

    // Legacy overload — accepts old integer tier arrays. Retained for callers not yet migrated.
    // NOTE: This path bypasses the new weighted average formula; prefer the RoleWeightBand[] overload.
    [System.Obsolete("Use ComputeAbilityForRole(int[] skills, RoleWeightBand[] skillBands) instead.")]
    public static int ComputeAbilityForRole(int[] skills, int[] tierMultipliers)
    {
        // Delegate to new formula via profile lookup is not possible here without a profile.
        // Convert integer tiers back to approximate RoleWeightBand for the formula.
        if (skills == null || tierMultipliers == null) return 0;
        int len = skills.Length < tierMultipliers.Length ? skills.Length : tierMultipliers.Length;
        var bands = new RoleWeightBand[len];
        for (int i = 0; i < len; i++)
        {
            switch (tierMultipliers[i])
            {
                case 2:  bands[i] = RoleWeightBand.Primary;   break;
                case 3:  bands[i] = RoleWeightBand.Secondary; break;
                default: bands[i] = RoleWeightBand.Tertiary;  break;
            }
        }
        return AbilityCalculator.ComputeRoleCA(skills, bands);
    }

    // Maps a raw ability score to a RoleSuitability tier using absolute thresholds.
    public static RoleSuitability GetSuitability(int abilityForRole)
    {
        if (abilityForRole >= NaturalThreshold)      return RoleSuitability.Natural;
        if (abilityForRole >= AccomplishedThreshold) return RoleSuitability.Accomplished;
        if (abilityForRole >= CompetentThreshold)    return RoleSuitability.Competent;
        if (abilityForRole >= AwkwardThreshold)      return RoleSuitability.Awkward;
        return RoleSuitability.Unsuitable;
    }

    // Convenience: fetches skill bands from profile, computes ability, returns suitability.
    public static RoleSuitability GetSuitabilityForRole(int[] skills, RoleProfileTable profileTable, RoleId role)
    {
        if (skills == null || profileTable == null) return RoleSuitability.Unsuitable;
        var profile = profileTable.Get(role);
        if (profile == null) return RoleSuitability.Unsuitable;
        int ability = AbilityCalculator.ComputeRoleCA(skills, profile.SkillBands);
        return GetSuitability(ability);
    }

    // Builds an integer tier array from a RoleProfileDefinition.SkillBands.
    // Primary=2, Secondary=3, Tertiary/Ignored=4.
    // Retained for non-CA callers (skill allocation, decay weighting, founder generation).
    public static int[] BuildTierArray(RoleProfileDefinition profile)
    {
        int count = SkillIdHelper.SkillCount;
        var tiers = new int[count];
        for (int i = 0; i < count; i++)
        {
            if (profile.SkillBands == null || i >= profile.SkillBands.Length)
            {
                tiers[i] = 3;
                continue;
            }
            switch (profile.SkillBands[i])
            {
                case RoleWeightBand.Primary:   tiers[i] = 2; break;
                case RoleWeightBand.Secondary: tiers[i] = 3; break;
                default:                       tiers[i] = 4; break;
            }
        }
        return tiers;
    }
}
