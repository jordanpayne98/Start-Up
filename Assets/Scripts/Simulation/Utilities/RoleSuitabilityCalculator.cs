// Pure static utility — no state, no Unity refs, no allocations.
// Computes role suitability tier from raw skill arrays using AbilityCalculator and RoleProfileTable.
public static class RoleSuitabilityCalculator
{
    private const int NaturalThreshold     = 140;
    private const int AccomplishedThreshold = 100;
    private const int CompetentThreshold   = 60;
    private const int AwkwardThreshold     = 30;

    private static readonly RoleId[] _allRoles = (RoleId[])System.Enum.GetValues(typeof(RoleId));
    public static RoleId[] AllRoles => _allRoles;

    // Delegates directly to AbilityCalculator — no allocation, no state.
    public static int ComputeAbilityForRole(int[] skills, int[] tierMultipliers)
    {
        return AbilityCalculator.ComputeAbility(skills, tierMultipliers);
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

    // Convenience: fetches tier multipliers from profile bands, computes ability, returns suitability.
    public static RoleSuitability GetSuitabilityForRole(int[] skills, RoleProfileTable profileTable, RoleId role)
    {
        if (skills == null || profileTable == null) return RoleSuitability.Unsuitable;
        var profile = profileTable.Get(role);
        if (profile == null) return RoleSuitability.Unsuitable;
        int[] tiers = BuildTierArray(profile);
        int ability = AbilityCalculator.ComputeAbility(skills, tiers);
        return GetSuitability(ability);
    }

    // Builds a tier integer array from a RoleProfileDefinition.SkillBands.
    // Primary=2, Secondary=3, Tertiary/None=4.
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
