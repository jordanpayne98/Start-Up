// SkillGrowthSystem Version: Clean v3 (SkillId/RoleProfileTable migration)
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

    // Called once per contract completion. Mutates employee.Stats.Skills in-place.
    public static void AwardSkillXP(
        Contract contract,
        Team team,
        EmployeeSystem employeeSystem,
        IRng rng,
        RoleProfileTable roleProfileTable,
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

        float totalEffective = 0f;
        for (int i = 0; i < memberCount; i++)
        {
            var emp = employeeSystem.GetEmployee(team.members[i]);
            if (emp == null || !emp.isActive) continue;
            totalEffective += emp.EffectiveOutput;
        }
        if (totalEffective <= 0f) totalEffective = memberCount;
        float perUnitXP = rawBaseXP / totalEffective;

        float[] weights = contract.Requirements.Weights;

        for (int i = 0; i < memberCount; i++)
        {
            var memberId = team.members[i];
            var employee = employeeSystem.GetEmployee(memberId);
            if (employee == null || !employee.isActive) continue;

            float variance = varianceMin + (rng.NextFloat01() * varianceRange);
            float ageDecay = GetAgeDecayMultiplier(employee.age, tuning);
            float baseXP = perUnitXP * employee.EffectiveOutput * variance * ageDecay;
            if (employee.isFounder) baseXP *= 1.5f;
            if (baseXP < 0f) baseXP = 0f;

            int[] tiers = GetTiersForRole(employee.role, roleProfileTable);

            SkillId nativeSkill = GetNativeSkillForRole(employee.role);
            bool nativeSkillAwarded = false;
            bool anyFit = false;
            int currentCA = AbilityCalculator.ComputeAbility(employee.Stats.Skills, tiers);

            int skillCount = SkillIdHelper.SkillCount;
            for (int s = 0; s < skillCount; s++)
            {
                float w = (weights != null && s < weights.Length) ? weights[s] : 0f;
                if (w <= 0f) continue;

                float weightedXP = baseXP * w;
                bool fit = TeamWorkEngine.IsRoleFitForSkill(employee.role, (SkillId)s);
                if (fit) anyFit = true;
                float finalXP = fit ? weightedXP : weightedXP * misfitRate;

                if (currentCA < employee.Stats.PotentialAbility)
                {
                    AccumulateXP(employee, s, finalXP, tiers, employee.Stats.PotentialAbility);
                    currentCA = AbilityCalculator.ComputeAbility(employee.Stats.Skills, tiers);
                }

                if ((SkillId)s == nativeSkill) nativeSkillAwarded = true;
            }

            if (!nativeSkillAwarded && !anyFit)
            {
                if (currentCA < employee.Stats.PotentialAbility)
                {
                    AccumulateXP(employee, (int)nativeSkill, baseXP * nativeRate, tiers, employee.Stats.PotentialAbility);
                    currentCA = AbilityCalculator.ComputeAbility(employee.Stats.Skills, tiers);
                }
            }

            float adaptability  = employee.Stats.GetVisibleAttribute(VisibleAttributeId.Adaptability);
            float spilloverRate  = spilloverBase + (adaptability / 20f) * spilloverSpread;
            float spilloverAward = baseXP * spilloverRate;
            if (spilloverAward > 0f)
            {
                for (int s = 0; s < skillCount; s++)
                {
                    float w = (weights != null && s < weights.Length) ? weights[s] : 0f;
                    if (w > 0f) continue;
                    if (currentCA >= employee.Stats.PotentialAbility) break;
                    AccumulateXP(employee, s, spilloverAward, tiers, employee.Stats.PotentialAbility);
                    currentCA = AbilityCalculator.ComputeAbility(employee.Stats.Skills, tiers);
                }
            }

            abilitySystem?.InvalidateCA(memberId);
        }
    }

    // Called once per day during TickPhaseWork for active product development phases.
    public static void AwardProductPhaseXP(
        Team team,
        SkillId phaseSkill,
        EmployeeSystem employeeSystem,
        RoleProfileTable roleProfileTable,
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

        float totalEffective = 0f;
        for (int i = 0; i < memberCount; i++)
        {
            var emp = employeeSystem.GetEmployee(team.members[i]);
            if (emp == null || !emp.isActive) continue;
            totalEffective += emp.EffectiveOutput;
        }
        if (totalEffective <= 0f) totalEffective = memberCount;
        float perUnitXP = xpPerDay / totalEffective;

        int skillCount = SkillIdHelper.SkillCount;
        for (int i = 0; i < memberCount; i++)
        {
            var memberId = team.members[i];
            var employee = employeeSystem.GetEmployee(memberId);
            if (employee == null || !employee.isActive) continue;

            float ageDecay = GetAgeDecayMultiplier(employee.age, tuning);
            float baseXP = perUnitXP * employee.EffectiveOutput * ageDecay;
            if (baseXP < 0f) baseXP = 0f;

            int[] tiers = GetTiersForRole(employee.role, roleProfileTable);

            int currentCA = AbilityCalculator.ComputeAbility(employee.Stats.Skills, tiers);
            bool fit = TeamWorkEngine.IsRoleFitForSkill(employee.role, phaseSkill);

            if (fit)
            {
                if (currentCA < employee.Stats.PotentialAbility)
                {
                    AccumulateXP(employee, (int)phaseSkill, baseXP, tiers, employee.Stats.PotentialAbility);
                    currentCA = AbilityCalculator.ComputeAbility(employee.Stats.Skills, tiers);
                }
            }
            else
            {
                if (currentCA < employee.Stats.PotentialAbility)
                {
                    AccumulateXP(employee, (int)phaseSkill, baseXP * misfitRate, tiers, employee.Stats.PotentialAbility);
                    currentCA = AbilityCalculator.ComputeAbility(employee.Stats.Skills, tiers);
                }
                SkillId nativeSkill = GetNativeSkillForRole(employee.role);
                if (nativeSkill != phaseSkill && currentCA < employee.Stats.PotentialAbility)
                {
                    AccumulateXP(employee, (int)nativeSkill, baseXP * nativeRate, tiers, employee.Stats.PotentialAbility);
                    currentCA = AbilityCalculator.ComputeAbility(employee.Stats.Skills, tiers);
                }
            }

            float adaptability   = employee.Stats.GetVisibleAttribute(VisibleAttributeId.Adaptability);
            float spilloverRate  = spilloverBase + (adaptability / 20f) * spilloverSpread;
            float spilloverAward = baseXP * spilloverRate;
            if (spilloverAward > 0f)
            {
                for (int s = 0; s < skillCount; s++)
                {
                    if ((SkillId)s == phaseSkill) continue;
                    if (currentCA >= employee.Stats.PotentialAbility) break;
                    AccumulateXP(employee, s, spilloverAward, tiers, employee.Stats.PotentialAbility);
                    currentCA = AbilityCalculator.ComputeAbility(employee.Stats.Skills, tiers);
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
        RoleProfileTable roleProfileTable,
        AbilitySystem abilitySystem,
        TuningConfig tuning = null)
    {
        if (team == null) return;
        if (team.teamType != TeamType.Marketing) return;
        int memberCount = team.members.Count;
        if (memberCount == 0) return;

        float totalEffective = 0f;
        for (int i = 0; i < memberCount; i++)
        {
            var emp = employeeSystem.GetEmployee(team.members[i]);
            if (emp == null || !emp.isActive) continue;
            totalEffective += emp.EffectiveOutput;
        }
        if (totalEffective <= 0f) totalEffective = memberCount;
        float perUnitXP = xpAmount / totalEffective;

        for (int i = 0; i < memberCount; i++)
        {
            var memberId = team.members[i];
            var employee = employeeSystem.GetEmployee(memberId);
            if (employee == null || !employee.isActive) continue;

            float varianceMin = tuning != null ? tuning.XPVarianceMin : DefaultVarianceMin;
            float varianceRange = tuning != null ? tuning.XPVarianceRange : DefaultVarianceRange;
            float variance = varianceMin + rng.NextFloat01() * varianceRange;
            float ageDecay = GetAgeDecayMultiplier(employee.age, tuning);
            float finalXP = perUnitXP * employee.EffectiveOutput * variance * ageDecay;

            int[] tiers = GetTiersForRole(employee.role, roleProfileTable);

            int currentCA = AbilityCalculator.ComputeAbility(employee.Stats.Skills, tiers);
            if (currentCA < employee.Stats.PotentialAbility)
                AccumulateXP(employee, (int)SkillId.Marketing, finalXP, tiers, employee.Stats.PotentialAbility);
            abilitySystem?.InvalidateCA(memberId);
        }
    }

    private static SkillId GetNativeSkillForRole(RoleId role)
    {
        switch (role)
        {
            case RoleId.SoftwareEngineer:     return SkillId.Programming;
            case RoleId.SystemsEngineer:      return SkillId.SystemsArchitecture;
            case RoleId.SecurityEngineer:     return SkillId.Security;
            case RoleId.PerformanceEngineer:  return SkillId.PerformanceOptimisation;
            case RoleId.HardwareEngineer:     return SkillId.HardwareIntegration;
            case RoleId.ManufacturingEngineer:return SkillId.Manufacturing;
            case RoleId.ProductDesigner:      return SkillId.ProductDesign;
            case RoleId.GameDesigner:         return SkillId.GameDesign;
            case RoleId.TechnicalArtist:      return SkillId.Vfx;
            case RoleId.AudioDesigner:        return SkillId.AudioDesign;
            case RoleId.QaEngineer:           return SkillId.QaTesting;
            case RoleId.TechnicalSupportSpecialist: return SkillId.TechnicalSupport;
            case RoleId.Marketer:             return SkillId.Marketing;
            case RoleId.SalesExecutive:       return SkillId.Sales;
            case RoleId.Accountant:           return SkillId.Accountancy;
            case RoleId.HrSpecialist:         return SkillId.HrRecruitment;
            default:                          return SkillId.Programming;
        }
    }

    private static int[] GetTiersForRole(RoleId role, RoleProfileTable roleProfileTable)
    {
        if (roleProfileTable != null)
        {
            var profile = roleProfileTable.Get(role);
            if (profile != null) return RoleSuitabilityCalculator.BuildTierArray(profile);
        }
        return UniformTiers;
    }

    private static int[] BuildUniformTiers()
    {
        int count = SkillIdHelper.SkillCount;
        var tiers = new int[count];
        for (int i = 0; i < count; i++) tiers[i] = 3;
        return tiers;
    }

    public static SkillId? TeamTypeToSkillId(TeamType type)
    {
        switch (type)
        {
            case TeamType.Development: return null;
            case TeamType.Design:      return SkillId.ProductDesign;
            case TeamType.QA:          return SkillId.QaTesting;
            case TeamType.Marketing:   return SkillId.Marketing;
            case TeamType.HR:          return SkillId.HrRecruitment;
            default: return null;
        }
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
        if (skillIndex < 0 || skillIndex >= SkillIdHelper.SkillCount) return;
        if (employee.Stats.Skills[skillIndex] >= 20) return;

        int oldLevel = employee.Stats.Skills[skillIndex];
        employee.Stats.SkillXp[skillIndex] += amount;

        while (employee.Stats.SkillXp[skillIndex] >= 1.0f && employee.Stats.Skills[skillIndex] < 20)
        {
            employee.Stats.Skills[skillIndex]++;
            int newCA = AbilityCalculator.ComputeAbility(employee.Stats.Skills, tiers);
            if (newCA > potentialAbility)
            {
                employee.Stats.Skills[skillIndex]--;
                employee.Stats.SkillXp[skillIndex] = 0f;
                return;
            }
            employee.Stats.SkillXp[skillIndex] -= 1.0f;
        }

        if (employee.Stats.Skills[skillIndex] >= 20)
            employee.Stats.SkillXp[skillIndex] = 0f;
        employee.Stats.SkillDeltaDirection[skillIndex] = (sbyte)(employee.Stats.Skills[skillIndex] > oldLevel ? 1 : 0);
    }
}
