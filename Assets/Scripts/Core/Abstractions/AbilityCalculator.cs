// Pure-static utility for computing Role CA and related ability values.
// No state, no Unity references, no allocations.
// Formula: Weighted average (Page 03 spec) — replaces old progressive cost formula.
public static class AbilityCalculator
{
    private const int GlobalMaxAbility  = 200;
    private const int GlobalMaxPotential = 200;

    // -------------------------------------------------------------------------
    // Core Role CA formula (spec section 3)
    // -------------------------------------------------------------------------
    // BaseRoleCA = Sum(skills[i] * 10 * BandWeight(skillBands[i])) / Sum(BandWeight)
    // Ignored skills (weight 0) are excluded from both numerator and denominator.
    // Then applies Primary Skill Floor cap and Missing Primary Penalty.
    // Returns CA clamped [0, 200].
    // -------------------------------------------------------------------------
    public static int ComputeRoleCA(int[] skills, RoleWeightBand[] skillBands)
    {
        if (skills == null || skillBands == null) return 0;

        int len = skills.Length < skillBands.Length ? skills.Length : skillBands.Length;
        float totalContribution = 0f;
        float weightSum = 0f;

        // Track primary skills for cap and penalty
        int primaryCount = 0;
        int primarySum = 0;
        int lowestPrimary = int.MaxValue;
        int highestPrimary = int.MinValue;

        for (int i = 0; i < len; i++)
        {
            var band = skillBands[i];
            if (band == RoleWeightBand.Ignored) continue;

            float w = RoleWeightBandHelper.ToWeight(band);
            int level = skills[i];
            if (level < 0) level = 0;
            if (level > 20) level = 20;

            totalContribution += level * 10f * w;
            weightSum += w;

            if (band == RoleWeightBand.Primary)
            {
                primaryCount++;
                primarySum += level;
                if (level < lowestPrimary)  lowestPrimary  = level;
                if (level > highestPrimary) highestPrimary = level;
            }
        }

        if (weightSum <= 0f) return 0;

        int baseCA = (int)(totalContribution / weightSum);

        // Primary Skill Floor cap (spec section 7)
        if (primaryCount > 0)
        {
            int avgPrimary = primarySum / primaryCount;
            int primaryFloorCap = GetPrimarySkillFloorCap(avgPrimary);
            if (baseCA > primaryFloorCap) baseCA = primaryFloorCap;
        }

        // Missing Primary Penalty (spec section 8)
        if (primaryCount >= 2)
        {
            int penalty = GetMissingPrimaryPenalty(lowestPrimary, highestPrimary);
            baseCA -= penalty;
        }

        // Clamp and return
        if (baseCA < 0) baseCA = 0;
        if (baseCA > GlobalMaxAbility) baseCA = GlobalMaxAbility;
        return baseCA;
    }

    // -------------------------------------------------------------------------
    // Primary Skill Floor cap (spec section 7)
    // Prevents specialists with weak primaries from appearing elite.
    // avgPrimary: average of all Primary-band skill values.
    // Returns the maximum CA allowed at that primary average.
    // -------------------------------------------------------------------------
    public static int GetPrimarySkillFloorCap(int avgPrimarySkillValue)
    {
        if (avgPrimarySkillValue >= 11) return 200;
        if (avgPrimarySkillValue >= 8)  return 140;
        if (avgPrimarySkillValue >= 5)  return 100;
        return 70;
    }

    // -------------------------------------------------------------------------
    // Missing Primary Penalty (spec section 8)
    // Penalises when primary skills are highly uneven.
    // lowestPrimary/highestPrimary: lowest and highest Primary-band skill values.
    // Returns penalty to subtract from base CA.
    // -------------------------------------------------------------------------
    public static int GetMissingPrimaryPenalty(int lowestPrimary, int highestPrimary)
    {
        int gap = highestPrimary - lowestPrimary;
        if (gap >= 12) return 15;
        if (gap >= 9)  return 10;
        if (gap >= 6)  return 5;
        return 0;
    }

    // -------------------------------------------------------------------------
    // PA Distance XP Multiplier (spec sections 12 / Page 06 section 7.2)
    // Consumed by SkillGrowthSystem (Plan 3B).
    // bestRoleCA: employee's best role CA; potentialAbility: their PA.
    // -------------------------------------------------------------------------
    public static float GetPADistanceXPMultiplier(int bestRoleCA, int potentialAbility)
    {
        int delta = bestRoleCA - potentialAbility; // negative = below PA, positive = above PA
        if (delta <= -40) return 1.20f;
        if (delta <= -20) return 1.00f;
        if (delta <= -1)  return 0.70f;
        if (delta == 0)   return 0.40f;
        if (delta <= 10)  return 0.20f;
        return 0.05f;
    }

    // -------------------------------------------------------------------------
    // Star ratings (spec section 5.1 — updated thresholds)
    // 0–39=1, 40–79=2, 80–119=3, 120–159=4, 160–200=5
    // -------------------------------------------------------------------------
    public static int AbilityToStars(int ability)
    {
        if (ability >= 160) return 5;
        if (ability >= 120) return 4;
        if (ability >= 80)  return 3;
        if (ability >= 40)  return 2;
        return 1;
    }

    // Potential star thresholds (spec section 10.2)
    public static int PotentialToStars(int potential)
    {
        if (potential >= 160) return 5;
        if (potential >= 120) return 4;
        if (potential >= 80)  return 3;
        if (potential >= 40)  return 2;
        return 1;
    }

    // -------------------------------------------------------------------------
    // Labels and display — unchanged
    // -------------------------------------------------------------------------
    public static string StarsLabel(int stars)
    {
        switch (stars)
        {
            case 5:  return "Legendary";
            case 4:  return "Elite";
            case 3:  return "High";
            case 2:  return "Average";
            default: return "Low";
        }
    }

    public static string PotentialStarsDisplay(int stars)
    {
        switch (stars)
        {
            case 5:  return "\u2605\u2605\u2605\u2605\u2605";
            case 4:  return "\u2605\u2605\u2605\u2605";
            case 3:  return "\u2605\u2605\u2605";
            case 2:  return "\u2605\u2605";
            default: return "\u2605";
        }
    }

    public static int MaxAbility(TuningConfig tuning = null)
    {
        return tuning != null ? tuning.AbilityGlobalMax : GlobalMaxAbility;
    }

    public static int MaxPotential(TuningConfig tuning = null)
    {
        return tuning != null ? tuning.PotentialGlobalMax : GlobalMaxPotential;
    }
}
