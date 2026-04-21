// SkillGrowthSystem Version: Clean v2 (role-fit XP)
using System;

public static class SkillGrowthSystem
{
    private const int MaxXPPerContract = 2;
    private const float DefaultVarianceMin         = 0.8f;
    private const float DefaultVarianceRange       = 0.4f;
    private const float DefaultSpilloverBase       = 0.35f;
    private const float DefaultSpilloverSpread     = 0.30f;
    private const float DefaultMisfitXPRate        = 0.25f;
    private const float DefaultNativeXPRate        = 0.15f;
    private const float DefaultProductPhaseXPPerDay = 0.15f;

    private static readonly int[] UniformTiers = BuildUniformTiers();

    // Called once per contract completion. Mutates employee.skills in-place.
    public static void AwardSkillXP(
        Contract contract,
        Team team,
        EmployeeSystem employeeSystem,
        IRng rng,
        RoleTierTable roleTierTable,
        AbilitySystem abilitySystem,
        TuningConfig tuning = null)
    {
        if (team == null) return;
        int memberCount = team.members.Count;
        if (memberCount == 0) return;

        int   maxXP           = tuning != null ? tuning.MaxXPPerContract           : MaxXPPerContract;
        float varianceMin     = tuning != null ? tuning.XPVarianceMin              : DefaultVarianceMin;
        float varianceRange   = tuning != null ? tuning.XPVarianceRange            : DefaultVarianceRange;
        float spilloverBase   = tuning != null ? tuning.SkillSpilloverRateBase     : DefaultSpilloverBase;
        float spilloverSpread = tuning != null ? tuning.SkillSpilloverRateSpread   : DefaultSpilloverSpread;
        float misfitRate      = tuning != null ? tuning.ContractMisfitXPRate       : DefaultMisfitXPRate;
        float nativeRate      = tuning != null ? tuning.ContractNativeXPRate       : DefaultNativeXPRate;

        float overallQuality = ComputeOverallQuality(contract);
        float rawBaseXP = (contract.Difficulty / 10f) * (overallQuality / 100f) * maxXP;
        rawBaseXP /= memberCount;

        float[] weights = contract.Requirements.Weights;

        for (int i = 0; i < memberCount; i++)
        {
            var memberId = team.members[i];
            var employee = employeeSystem.GetEmployee(memberId);
            if (employee == null || !employee.isActive) continue;

            float variance = varianceMin + (rng.NextFloat01() * varianceRange);
            float ageDecay = GetAgeDecayMultiplier(employee.age, tuning);
            float baseXP = rawBaseXP * variance * ageDecay;
            if (employee.isFounder) baseXP *= 1.5f;
            if (baseXP < 0f) baseXP = 0f;

            int[] tiers = roleTierTable != null
                ? roleTierTable.GetTiers(employee.role)
                : UniformTiers;

            SkillType nativeSkill = GetNativeSkillForRole(employee.role);
            bool nativeSkillAwarded = false;
            bool anyFit = false;
            int currentCA = AbilityCalculator.ComputeAbility(employee.skills, tiers);

            for (int s = 0; s < SkillTypeHelper.SkillTypeCount; s++)
            {
                float w = weights[s];
                if (w <= 0f) continue;

                float weightedXP = baseXP * w;
                bool fit = TeamWorkEngine.IsRoleFitForSkill(employee.role, (SkillType)s);
                if (fit) anyFit = true;
                float finalXP = fit ? weightedXP : weightedXP * misfitRate;

                if (currentCA < employee.potentialAbility)
                {
                    AccumulateXP(employee, s, finalXP, tiers, employee.potentialAbility);
                    currentCA = AbilityCalculator.ComputeAbility(employee.skills, tiers);
                }

                if ((SkillType)s == nativeSkill) nativeSkillAwarded = true;
            }

            if (!nativeSkillAwarded && !anyFit)
            {
                if (currentCA < employee.potentialAbility)
                {
                    AccumulateXP(employee, (int)nativeSkill, baseXP * nativeRate, tiers, employee.potentialAbility);
                    currentCA = AbilityCalculator.ComputeAbility(employee.skills, tiers);
                }
            }

            float spilloverRate  = spilloverBase + (employee.hiddenAttributes.Adaptability / 20f) * spilloverSpread;
            float spilloverAward = baseXP * spilloverRate;
            if (spilloverAward > 0f)
            {
                for (int s = 0; s < SkillTypeHelper.SkillTypeCount; s++)
                {
                    if (weights[s] > 0f) continue;
                    if (currentCA >= employee.potentialAbility) break;
                    AccumulateXP(employee, s, spilloverAward, tiers, employee.potentialAbility);
                    currentCA = AbilityCalculator.ComputeAbility(employee.skills, tiers);
                }
            }

            abilitySystem?.InvalidateCA(memberId);
        }
    }

    // Called once per day during TickPhaseWork for active product development phases.
    public static void AwardProductPhaseXP(
        Team team,
        SkillType phaseSkill,
        EmployeeSystem employeeSystem,
        RoleTierTable roleTierTable,
        AbilitySystem abilitySystem,
        TuningConfig tuning = null)
    {
        if (team == null) return;
        int memberCount = team.members.Count;
        if (memberCount == 0) return;

        float xpPerDay        = tuning != null ? tuning.ProductPhaseXPPerDay      : DefaultProductPhaseXPPerDay;
        float misfitRate      = tuning != null ? tuning.ProductPhaseMisfitXPRate   : DefaultMisfitXPRate;
        float nativeRate      = tuning != null ? tuning.ProductPhaseNativeXPRate   : DefaultNativeXPRate;
        float spilloverBase   = tuning != null ? tuning.SkillSpilloverRateBase     : DefaultSpilloverBase;
        float spilloverSpread = tuning != null ? tuning.SkillSpilloverRateSpread   : DefaultSpilloverSpread;

        float perMember = xpPerDay / memberCount;

        for (int i = 0; i < memberCount; i++)
        {
            var memberId = team.members[i];
            var employee = employeeSystem.GetEmployee(memberId);
            if (employee == null || !employee.isActive) continue;

            float ageDecay = GetAgeDecayMultiplier(employee.age, tuning);
            float baseXP = perMember * ageDecay;
            if (baseXP < 0f) baseXP = 0f;

            int[] tiers = roleTierTable != null
                ? roleTierTable.GetTiers(employee.role)
                : UniformTiers;

            int currentCA = AbilityCalculator.ComputeAbility(employee.skills, tiers);
            bool fit = TeamWorkEngine.IsRoleFitForSkill(employee.role, phaseSkill);

            if (fit)
            {
                if (currentCA < employee.potentialAbility)
                {
                    AccumulateXP(employee, (int)phaseSkill, baseXP, tiers, employee.potentialAbility);
                    currentCA = AbilityCalculator.ComputeAbility(employee.skills, tiers);
                }
            }
            else
            {
                if (currentCA < employee.potentialAbility)
                {
                    AccumulateXP(employee, (int)phaseSkill, baseXP * misfitRate, tiers, employee.potentialAbility);
                    currentCA = AbilityCalculator.ComputeAbility(employee.skills, tiers);
                }
                SkillType nativeSkill = GetNativeSkillForRole(employee.role);
                if (nativeSkill != phaseSkill && currentCA < employee.potentialAbility)
                {
                    AccumulateXP(employee, (int)nativeSkill, baseXP * nativeRate, tiers, employee.potentialAbility);
                    currentCA = AbilityCalculator.ComputeAbility(employee.skills, tiers);
                }
            }

            float spilloverRate  = spilloverBase + (employee.hiddenAttributes.Adaptability / 20f) * spilloverSpread;
            float spilloverAward = baseXP * spilloverRate;
            if (spilloverAward > 0f)
            {
                for (int s = 0; s < SkillTypeHelper.SkillTypeCount; s++)
                {
                    if ((SkillType)s == phaseSkill) continue;
                    if (currentCA >= employee.potentialAbility) break;
                    AccumulateXP(employee, s, spilloverAward, tiers, employee.potentialAbility);
                    currentCA = AbilityCalculator.ComputeAbility(employee.skills, tiers);
                }
            }

            abilitySystem?.InvalidateCA(memberId);
        }
    }

    public static void AwardMarketingXP(
        Team team,
        EmployeeSystem employeeSystem,
        float xpAmount,
        IRng rng,
        RoleTierTable roleTierTable,
        AbilitySystem abilitySystem,
        TuningConfig tuning = null)
    {
        if (team == null) return;
        if (team.teamType != TeamType.Marketing) return;
        int memberCount = team.members.Count;
        if (memberCount == 0) return;
        xpAmount /= memberCount;

        for (int i = 0; i < memberCount; i++)
        {
            var memberId = team.members[i];
            var employee = employeeSystem.GetEmployee(memberId);
            if (employee == null || !employee.isActive) continue;

            float varianceMin = tuning != null ? tuning.XPVarianceMin : DefaultVarianceMin;
            float varianceRange = tuning != null ? tuning.XPVarianceRange : DefaultVarianceRange;
            float variance = varianceMin + rng.NextFloat01() * varianceRange;
            float ageDecay = GetAgeDecayMultiplier(employee.age, tuning);
            float finalXP = xpAmount * variance * ageDecay;

            int[] tiers = roleTierTable != null
                ? roleTierTable.GetTiers(employee.role)
                : UniformTiers;

            int currentCA = AbilityCalculator.ComputeAbility(employee.skills, tiers);
            if (currentCA < employee.potentialAbility)
                AccumulateXP(employee, (int)SkillType.Marketing, finalXP, tiers, employee.potentialAbility);
            abilitySystem?.InvalidateCA(memberId);
        }
    }

    private static SkillType GetNativeSkillForRole(EmployeeRole role)
    {
        switch (role)
        {
            case EmployeeRole.Developer:     return SkillType.Programming;
            case EmployeeRole.Designer:      return SkillType.Design;
            case EmployeeRole.QAEngineer:    return SkillType.QA;
            case EmployeeRole.SoundEngineer: return SkillType.SFX;
            case EmployeeRole.VFXArtist:     return SkillType.VFX;
            default:                         return SkillType.Programming;
        }
    }

    private static int[] BuildUniformTiers()
    {
        var tiers = new int[SkillTypeHelper.SkillTypeCount];
        for (int i = 0; i < SkillTypeHelper.SkillTypeCount; i++) tiers[i] = 3;
        return tiers;
    }

    public static SkillType? TeamTypeToSkillType(TeamType type)
    {
        switch (type)
        {
            case TeamType.Programming: return SkillType.Programming;
            case TeamType.Design:      return SkillType.Design;
            case TeamType.QA:          return SkillType.QA;
            case TeamType.SFX:         return SkillType.SFX;
            case TeamType.VFX:         return SkillType.VFX;
            case TeamType.Accounting:  return SkillType.Accountancy;
            case TeamType.Marketing:   return SkillType.Marketing;
            default: return null;
        }
    }

    private static SkillType GetHighestSkill(Employee employee)
    {
        int bestIdx = 0;
        int bestVal = employee.skills[0];
        for (int i = 1; i < SkillTypeHelper.SkillTypeCount; i++)
        {
            if (employee.skills[i] > bestVal)
            {
                bestVal = employee.skills[i];
                bestIdx = i;
            }
        }
        return (SkillType)bestIdx;
    }

    private static float ComputeOverallQuality(Contract contract)
    {
        return contract.QualityScore > 0f ? contract.QualityScore : 50f;
    }

    private static float GetAgeDecayMultiplier(int age, TuningConfig tuning = null)
    {
        if (tuning != null)
        {
            var brackets = tuning.SkillAgeDecayBrackets;
            var multipliers = tuning.SkillAgeDecayMultipliers;
            for (int i = 0; i < brackets.Length; i++)
            {
                if (age <= brackets[i])
                    return multipliers[i];
            }
            return multipliers[multipliers.Length - 1];
        }
        if (age <= 24) return 1.00f;
        if (age <= 29) return 0.90f;
        if (age <= 34) return 0.78f;
        if (age <= 39) return 0.65f;
        if (age <= 44) return 0.52f;
        if (age <= 49) return 0.40f;
        return 0.30f;
    }

    private static void AccumulateXP(Employee employee, int skillIndex, float amount, int[] tiers, int potentialAbility)
    {
        if (amount <= 0f) return;
        if (employee.skills[skillIndex] >= 20) return;

        if (employee.skillXp == null)
            employee.skillXp = new float[SkillTypeHelper.SkillTypeCount];
        if (employee.skillDeltaDirection == null)
            employee.skillDeltaDirection = new sbyte[SkillTypeHelper.SkillTypeCount];

        int oldLevel = employee.skills[skillIndex];
        employee.skillXp[skillIndex] += amount;

        while (employee.skillXp[skillIndex] >= 1.0f && employee.skills[skillIndex] < 20)
        {
            employee.skills[skillIndex]++;
            int newCA = AbilityCalculator.ComputeAbility(employee.skills, tiers);
            if (newCA > potentialAbility)
            {
                employee.skills[skillIndex]--;
                employee.skillXp[skillIndex] = 0f;
                return;
            }
            employee.skillXp[skillIndex] -= 1.0f;
        }

        if (employee.skills[skillIndex] >= 20)
            employee.skillXp[skillIndex] = 0f;
        employee.skillDeltaDirection[skillIndex] = (sbyte)(employee.skills[skillIndex] > oldLevel ? 1 : 0);
    }
}
