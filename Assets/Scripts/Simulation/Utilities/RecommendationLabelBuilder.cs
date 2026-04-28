// RecommendationLabelBuilder Version: Clean v2
/// <summary>
/// Pure static class. Maps Ability tier × Potential tier to a role-contextual recommendation label.
/// Always returns an accurate label — no mislabelling noise.
/// No Unity refs, no state.
/// </summary>
public static class RecommendationLabelBuilder
{
    public enum AbilityTier { Low, High }
    public enum PotentialTier { Low, High }

    public const float AbilityHighThresholdPercent = 0.4f;
    public const float PotentialHighThresholdPercent = 0.4f;

    public static string Build(int candidateAbility, int candidatePotential, int abilityMax, int potentialMax,
        RoleId role, TuningConfig tuning = null)
    {
        float abilityThreshold = tuning != null ? tuning.RecommendationAbilityHighThreshold : AbilityHighThresholdPercent;
        float potentialThreshold = tuning != null ? tuning.RecommendationPotentialHighThreshold : PotentialHighThresholdPercent;

        AbilityTier abilityTier = GetAbilityTier(candidateAbility, abilityMax, abilityThreshold);
        PotentialTier potentialTier = GetPotentialTier(candidatePotential, potentialMax, potentialThreshold);

        string roleLabel = RoleLabel(role);

        if (abilityTier == AbilityTier.High && potentialTier == PotentialTier.High)
            return $"Senior-ready {roleLabel} with strong upside";
        if (abilityTier == AbilityTier.High && potentialTier == PotentialTier.Low)
            return $"Experienced {roleLabel}, likely near their ceiling";
        if (abilityTier == AbilityTier.Low && potentialTier == PotentialTier.High)
            return $"Raw {roleLabel} talent — needs time to develop";
        return $"Entry-level {roleLabel} — limited potential";
    }

    public static AbilityTier GetAbilityTier(int ability, int abilityMax, float threshold = AbilityHighThresholdPercent)
    {
        if (abilityMax <= 0) return AbilityTier.Low;
        return (float)ability / abilityMax >= threshold ? AbilityTier.High : AbilityTier.Low;
    }

    public static PotentialTier GetPotentialTier(int potential, int potentialMax, float threshold = PotentialHighThresholdPercent)
    {
        if (potentialMax <= 0) return PotentialTier.Low;
        return (float)potential / potentialMax >= threshold ? PotentialTier.High : PotentialTier.Low;
    }

    public static string RoleLabel(RoleId role)
    {
        return RoleIdHelper.GetName(role);
    }
}
