// Pure-static utility for computing Ability and per-skill Potential caps.
// No state, no Unity references, no allocations.
public static class AbilityCalculator
{
    // -------------------------------------------------------------------------
    // Progressive cost system
    // -------------------------------------------------------------------------
    // Ability = floor( sum(CumulativeCost[skills[i]] * tierMultipliers[i]) / TierScale )
    // clamped [0, 200].
    //
    // TierMultipliers per skill (integer, scaled by TierScale=2 to avoid floats):
    //   2 = Primary   (1.0x cost)
    //   3 = Secondary (1.5x cost)
    //   4 = Tertiary  (2.0x cost)
    //
    // -------------------------------------------------------------------------

    private const int TierScale = 2;
    private const int GlobalMaxAbility = 200;
    private const int GlobalMaxPotential = 200;

    // Indexed 0-20. CumulativeCost[level] = total Ability budget consumed by that skill at that level.
    // Bands: 1-10 cost 2 each, 11-15 cost 4 each, 16-18 cost 5 each, 19-20 cost 9 each.
    private static readonly int[] CumulativeCost = {
        0,
        2,  4,  6,  8, 10, 12, 14, 16, 18, 20,
        24, 28, 32, 36, 40,
        45, 50, 55,
        64, 73
    };

    // Indexed 0-20. MarginalCost[n] = cost of going from level n-1 to level n. Index 0 unused.
    private static readonly int[] MarginalCost = {
        0,
        2, 2, 2, 2, 2, 2, 2, 2, 2, 2,
        4, 4, 4, 4, 4,
        5, 5, 5,
        9, 9
    };

    // Compute Ability from skills and per-skill tier multipliers (progressive formula).
    // skills:          int[9] skill levels, each 0-20.
    // tierMultipliers: int[9] per skill, values 2/3/4 (Primary/Secondary/Tertiary).
    // Returns Ability clamped [0, 200].
    public static int ComputeAbility(int[] skills, int[] tierMultipliers)
    {
        if (skills == null || tierMultipliers == null) return 0;
        int sum = 0;
        int len = skills.Length < tierMultipliers.Length ? skills.Length : tierMultipliers.Length;
        for (int i = 0; i < len; i++)
        {
            int level = skills[i];
            if (level < 0) level = 0;
            if (level > 20) level = 20;
            sum += CumulativeCost[level] * tierMultipliers[i];
        }
        int ability = sum / TierScale;
        if (ability < 0) ability = 0;
        if (ability > GlobalMaxAbility) ability = GlobalMaxAbility;
        return ability;
    }

    // Returns the Ability budget cost of gaining one level (currentLevel -> currentLevel+1),
    // adjusted by the skill's tier multiplier. Used by UI and Potential overshoot gating.
    // Returns 0 if currentLevel is already at max (20).
    public static int GetMarginalCost(int currentLevel, int tierMultiplier)
    {
        if (currentLevel < 0) currentLevel = 0;
        if (currentLevel >= 20) return 0;
        return MarginalCost[currentLevel + 1] * tierMultiplier / TierScale;
    }

    // Returns the cumulative Ability cost of a skill at the given level (0-20).
    // Useful for candidate generation and reverse-solve allocation.
    public static int GetCumulativeCost(int level)
    {
        if (level <= 0) return 0;
        if (level > 20) level = 20;
        return CumulativeCost[level];
    }

    // -------------------------------------------------------------------------
    // Star ratings, labels, display — unchanged
    // -------------------------------------------------------------------------

    public static int PotentialToStars(int potential)
    {
        if (potential >= 180) return 5;
        if (potential >= 150) return 4;
        if (potential >= 100) return 3;
        if (potential >= 50)  return 2;
        return 1;
    }

    public static int AbilityToStars(int ability)
    {
        if (ability >= 180) return 5;
        if (ability >= 150) return 4;
        if (ability >= 100) return 3;
        if (ability >= 50)  return 2;
        return 1;
    }

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

    public static int MaxAbility(EmployeeRole role, TuningConfig tuning = null)
    {
        return tuning != null ? tuning.AbilityGlobalMax : GlobalMaxAbility;
    }

    public static int MaxPotential(EmployeeRole role, TuningConfig tuning = null)
    {
        return tuning != null ? tuning.PotentialGlobalMax : GlobalMaxPotential;
    }
}
