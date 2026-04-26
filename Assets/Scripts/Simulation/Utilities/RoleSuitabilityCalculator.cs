// Pure static utility — no state, no Unity refs, no allocations.
// Computes role suitability tier from raw skill arrays using AbilityCalculator and RoleTierTable.
public static class RoleSuitabilityCalculator
{
    private const int NaturalThreshold     = 140;
    private const int AccomplishedThreshold = 100;
    private const int CompetentThreshold   = 60;
    private const int AwkwardThreshold     = 30;

    // All valid EmployeeRole values in stable order (gap at index 3 intentional).
    public static readonly EmployeeRole[] AllRoles =
    {
        EmployeeRole.Developer,
        EmployeeRole.Designer,
        EmployeeRole.QAEngineer,
        EmployeeRole.HR,
        EmployeeRole.SoundEngineer,
        EmployeeRole.VFXArtist,
        EmployeeRole.Accountant,
        EmployeeRole.Marketer
    };

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

    // Convenience: fetches tiers from table, computes ability, returns suitability.
    public static RoleSuitability GetSuitabilityForRole(int[] skills, RoleTierTable tierTable, EmployeeRole role)
    {
        if (skills == null || tierTable == null) return RoleSuitability.Unsuitable;
        int[] tiers = tierTable.GetTiers(role);
        int ability = AbilityCalculator.ComputeAbility(skills, tiers);
        return GetSuitability(ability);
    }
}
