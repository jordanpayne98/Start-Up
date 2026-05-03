// TeamWorkEngine Version: Clean v2
using System.Collections.Generic;

// Pure-static utility for all shared work/quality formulas.
// No state, no allocations, no Unity references.
public struct TeamWorkResult
{
    public float TotalSpeedSkill;
    public float AvgQualitySkill;
    public int Contributors;
    public int ActiveCount;
    public float EffectiveCapacity;
    public float Overhead;
    public float AvgMorale;
    public float CoverageQualityMod;
    public float CoverageSpeedMod;
}

public static class TeamWorkEngine
{
    private const float MinContributorThresholdDefault = 1.5f;

    // ─── Effective Skill Computation ──────────────────────────────────────────

    public static void ComputeEffectiveSkills(
        float visibleSkill,
        int ca,
        float morale,
        float energy,
        float workEthic,
        float creative,
        float adaptability,
        bool isRoleFit,
        Personality personality,
        float avgTeamMorale,
        out float speedSkill,
        out float qualitySkill)
    {
        float caMod = 0.70f + (ca / 200f) * 0.50f;
        float moraleMod = MoraleSystem.MoraleMultiplier(morale);
        float energyMod = FatigueSystem.EnergyMultiplier(energy);
        float workEthicMod = 0.90f + (workEthic / 20f) * 0.20f;
        float creativeMod = 0.90f + (creative / 20f) * 0.20f;
        float offRoleSpeedMod = isRoleFit ? 1.0f : 0.25f + (adaptability / 20f) * 0.25f;
        float offRoleQualityMod = isRoleFit ? 1.0f : 0.30f + (adaptability / 20f) * 0.30f;

        float personalitySpeedMod = ComputePersonalitySpeedMod(personality, avgTeamMorale);
        float personalityQualityMod = ComputePersonalityQualityMod(personality);

        // Soft-cap curve: inflection at skill 6 (average new hire baseline).
        const float SkillDiminishingReturnsPivot = 6f;
        float scaledSkill = SkillDiminishingReturnsPivot * (float)System.Math.Sqrt(visibleSkill / SkillDiminishingReturnsPivot);

        speedSkill = scaledSkill * caMod * moraleMod * energyMod * workEthicMod * personalitySpeedMod * offRoleSpeedMod;
        qualitySkill = scaledSkill * caMod * moraleMod * energyMod * creativeMod * personalityQualityMod * offRoleQualityMod;
    }

    private static float ComputePersonalitySpeedMod(Personality personality, float avgTeamMorale)
    {
        switch (personality)
        {
            case Personality.Intense:       return 1.05f;
            case Personality.Abrasive:      return 1.03f;
            case Personality.Competitive:   return avgTeamMorale >= 65f ? 1.03f : 1.00f;
            case Personality.Perfectionist: return 0.97f;
            case Personality.Easygoing:     return 0.98f;
            default:                        return 1.00f;
        }
    }

    private static float ComputePersonalityQualityMod(Personality personality)
    {
        switch (personality)
        {
            case Personality.Perfectionist: return 1.05f;
            default:                        return 1.00f;
        }
    }

    // ─── Real Team Aggregation ─────────────────────────────────────────────────

    public static float ComputeEffectiveCapacity(List<EmployeeId> members, EmployeeSystem empSystem)
    {
        float total = 0f;
        int count = members.Count;
        for (int i = 0; i < count; i++)
        {
            Employee emp = empSystem.GetEmployee(members[i]);
            if (emp == null || !emp.isActive) continue;
            total += emp.EffectiveOutput;
        }
        return total;
    }

    public static TeamWorkResult AggregateTeam(
        List<EmployeeId> members,
        EmployeeSystem empSystem,
        FatigueSystem fatigueSystem,
        SkillId requiredSkill,
        RoleProfileTable roleProfileTable,
        float overheadPerMember,
        float minContributorThreshold = MinContributorThresholdDefault,
        int optimalTeamSize = 5)
    {
        float totalSpeedSkill = 0f;
        float qualityWeightedSum = 0f;
        float qualityWeightSum = 0f;
        float moraleSum = 0f;
        int contributors = 0;
        int activeCount = 0;
        float effectiveCapacity = 0f;

        // Pre-scan average morale for Competitive personality threshold check
        int memberCount = members.Count;
        float preMoraleSum = 0f;
        int preMoraleCount = 0;
        for (int i = 0; i < memberCount; i++)
        {
            Employee emp = empSystem.GetEmployee(members[i]);
            if (emp == null || !emp.isActive) continue;
            preMoraleSum += emp.morale;
            preMoraleCount++;
        }
        float avgTeamMorale = preMoraleCount > 0 ? preMoraleSum / preMoraleCount : 50f;

        for (int i = 0; i < memberCount; i++)
        {
            Employee emp = empSystem.GetEmployee(members[i]);
            if (emp == null || !emp.isActive) continue;

            activeCount++;
            effectiveCapacity += emp.EffectiveOutput;

            int ca;
            if (roleProfileTable != null)
            {
                var profile = roleProfileTable.Get(emp.role);
                if (profile != null && profile.SkillBands != null)
                    ca = AbilityCalculator.ComputeRoleCA(emp.Stats.Skills, profile.SkillBands);
                else
                    ca = ComputeFallbackCA(emp.Stats.Skills);
            }
            else
            {
                ca = ComputeFallbackCA(emp.Stats.Skills);
            }

            bool isRoleFit = IsRoleFitForSkill(emp.role, requiredSkill);
            float energy = fatigueSystem != null ? fatigueSystem.GetEnergy(emp.id) : 100f;

            float workEthic     = emp.Stats.GetVisibleAttribute(VisibleAttributeId.WorkEthic);
            float creative      = emp.Stats.GetVisibleAttribute(VisibleAttributeId.Creativity);
            float adaptability  = emp.Stats.GetVisibleAttribute(VisibleAttributeId.Adaptability);

            ComputeEffectiveSkills(
                emp.GetSkill(requiredSkill),
                ca,
                emp.morale,
                energy,
                workEthic,
                creative,
                adaptability,
                isRoleFit,
                emp.personality,
                avgTeamMorale,
                out float speedSkill,
                out float qualitySkill);

            totalSpeedSkill += speedSkill;
            moraleSum += emp.morale;

            if (speedSkill >= minContributorThreshold)
                contributors++;

            float qw = System.Math.Max(qualitySkill, 0.1f);
            qualityWeightedSum += qualitySkill * qw;
            qualityWeightSum += qw;
        }

        float avgQualitySkill = qualityWeightSum > 0f ? qualityWeightedSum / qualityWeightSum : 0f;
        float overhead = effectiveCapacity > 0f ? System.Math.Max(0.70f, 1f - overheadPerMember * (effectiveCapacity - 1f)) : 1f;
        float avgMorale = activeCount > 0 ? moraleSum / activeCount : 50f;
        float coverageQualityMod = ComputeCoverageQualityMod(effectiveCapacity, optimalTeamSize);
        float coverageSpeedMod = ComputeCoverageSpeedMod(effectiveCapacity, optimalTeamSize);

        return new TeamWorkResult
        {
            TotalSpeedSkill = totalSpeedSkill,
            AvgQualitySkill = avgQualitySkill,
            Contributors = contributors,
            ActiveCount = activeCount,
            EffectiveCapacity = effectiveCapacity,
            Overhead = overhead,
            AvgMorale = avgMorale,
            CoverageQualityMod = coverageQualityMod,
            CoverageSpeedMod = coverageSpeedMod
        };
    }

    // ─── Synthetic Team Aggregation (for Competitors) ─────────────────────────

    public static TeamWorkResult AggregateSynthetic(
        float syntheticVisibleSkill,
        float syntheticCA,
        float syntheticMorale,
        float syntheticWorkEthic,
        float syntheticCreative,
        int virtualTeamSize = 4,
        float syntheticAdaptability = 10f,
        float roleFitRatio = 0.35f)
    {
        const float syntheticEnergy = 75f;
        const Personality syntheticPersonality = Personality.Professional;

        ComputeEffectiveSkills(
            syntheticVisibleSkill,
            (int)syntheticCA,
            syntheticMorale,
            syntheticEnergy,
            syntheticWorkEthic,
            syntheticCreative,
            syntheticAdaptability,
            true,
            syntheticPersonality,
            syntheticMorale,
            out float roleFitSpeed,
            out float roleFitQuality);

        float offRoleMod = 0.10f + (syntheticAdaptability / 20f) * 0.15f;
        float blendedSpeedMod = roleFitRatio + (1f - roleFitRatio) * offRoleMod;
        float blendedQualityMod = roleFitRatio + (1f - roleFitRatio) * offRoleMod;

        float overhead = System.Math.Max(0.70f, 1f - 0.04f * (virtualTeamSize - 1));

        return new TeamWorkResult
        {
            TotalSpeedSkill = roleFitSpeed * virtualTeamSize * blendedSpeedMod,
            AvgQualitySkill = roleFitQuality * blendedQualityMod,
            Contributors = virtualTeamSize,
            ActiveCount = virtualTeamSize,
            EffectiveCapacity = virtualTeamSize,
            Overhead = overhead,
            AvgMorale = syntheticMorale,
            CoverageQualityMod = ComputeCoverageQualityMod(virtualTeamSize, 5),
            CoverageSpeedMod = ComputeCoverageSpeedMod(virtualTeamSize, 5)
        };
    }

    // ─── Work Per Tick ─────────────────────────────────────────────────────────

    public static float ComputeWorkPerTick(
        in TeamWorkResult team,
        float workRatePerSkillPoint,
        float coverageMod,
        float varianceMod,
        float extraMult = 1f,
        float chemistrySpeedMod = 1f,
        float conflictSpeedMult = 1f)
    {
        return team.TotalSpeedSkill * workRatePerSkillPoint * team.Overhead * coverageMod * varianceMod * extraMult * chemistrySpeedMod * conflictSpeedMult;
    }

    // ─── Quality Curve ─────────────────────────────────────────────────────────

    public static float ComputeQuality(
        float avgQualitySkill,
        float minThreshold,
        float targetThreshold,
        float excellenceThreshold,
        float coverageQualityMod,
        float avgMorale = 50f,
        float chemistryQualityMod = 1f,
        float conflictQualityMult = 1f)
    {
        float s = avgQualitySkill;
        float baseQuality;

        if (minThreshold <= 0f)
        {
            baseQuality = 60f;
        }
        else if (s < minThreshold)
        {
            float t = s / minThreshold;
            baseQuality = 40f + t * 10f;
        }
        else if (targetThreshold > minThreshold && s < targetThreshold)
        {
            float t = (s - minThreshold) / (targetThreshold - minThreshold);
            baseQuality = 50f + t * 10f;
        }
        else if (excellenceThreshold > targetThreshold && s < excellenceThreshold)
        {
            float t = (s - targetThreshold) / (excellenceThreshold - targetThreshold);
            baseQuality = 60f + t * 10f;
        }
        else
        {
            float excess = s - excellenceThreshold;
            baseQuality = 70f + 25f * (1f - (float)System.Math.Exp(-excess * 0.15f));
        }

        float quality = baseQuality * coverageQualityMod * chemistryQualityMod * conflictQualityMult;
        if (quality < 0f) quality = 0f;
        if (quality > 100f) quality = 100f;
        return quality;
    }

    // ─── Chemistry Modifiers ───────────────────────────────────────────────────

    public static float GetChemistrySpeedMod(ChemistryBand band)
    {
        switch (band)
        {
            case ChemistryBand.Excellent: return 1.04f;
            case ChemistryBand.Good:      return 1.02f;
            case ChemistryBand.Neutral:   return 1.00f;
            case ChemistryBand.Poor:      return 0.96f;
            case ChemistryBand.Toxic:     return 0.92f;
            default:                      return 1.00f;
        }
    }

    public static float GetChemistryQualityMod(ChemistryBand band)
    {
        switch (band)
        {
            case ChemistryBand.Excellent: return 1.06f;
            case ChemistryBand.Good:      return 1.03f;
            case ChemistryBand.Neutral:   return 1.00f;
            case ChemistryBand.Poor:      return 0.95f;
            case ChemistryBand.Toxic:     return 0.90f;
            default:                      return 1.00f;
        }
    }

    // ─── Speed Range Multiplier ────────────────────────────────────────────────

    public static float ComputeSpeedRangeMultiplier(
        float avgQualitySkill,
        float minThreshold,
        float targetThreshold,
        float excellenceThreshold)
    {
        float s = avgQualitySkill;

        if (minThreshold <= 0f)
            return 1.0f;

        if (s < minThreshold)
        {
            float t = s / minThreshold;
            return 0.60f + t * 0.30f;
        }

        if (targetThreshold > minThreshold && s < targetThreshold)
        {
            float t = (s - minThreshold) / (targetThreshold - minThreshold);
            return 0.90f + t * 0.05f;
        }

        if (excellenceThreshold > targetThreshold && s < excellenceThreshold)
        {
            float t = (s - targetThreshold) / (excellenceThreshold - targetThreshold);
            return 0.95f + t * 0.10f;
        }

        float excess = s - excellenceThreshold;
        return 1.05f + 0.15f * (1f - (float)System.Math.Exp(-excess * 0.20f));
    }

    // ─── Coverage Quality Modifier ─────────────────────────────────────────────

    public static float ComputeCoverageQualityMod(float effectiveCapacity, int optimalTeamSize)
    {
        if (optimalTeamSize <= 0) optimalTeamSize = 1;
        float ratio = effectiveCapacity / optimalTeamSize;

        if (ratio <= 0f) return 0.4f;
        if (ratio < 1.0f) return 0.5f + 0.5f * (float)System.Math.Sqrt(ratio);

        float bonus = 0.25f * (float)System.Math.Log(ratio + 1.0, 2.0) / (float)System.Math.Log(4.0, 2.0);
        float result = 1.0f + bonus;
        if (result > 1.25f) result = 1.25f;
        return result;
    }

    public static float ComputeCoverageQualityMod(int activeCount, int optimalTeamSize)
    {
        return ComputeCoverageQualityMod((float)activeCount, optimalTeamSize);
    }

    // ─── Coverage Speed Modifier ───────────────────────────────────────────────

    public static float ComputeCoverageSpeedMod(float effectiveCapacity, int optimalTeamSize)
    {
        if (optimalTeamSize <= 0) optimalTeamSize = 1;
        float ratio = effectiveCapacity / optimalTeamSize;

        if (ratio <= 0f) return 0.1f;
        if (ratio < 1.0f) return 0.1f + 0.9f * ratio * ratio;

        float bonus = 0.2f * (float)System.Math.Log(ratio, 2.0);
        float result = 1.0f + bonus;
        if (result > 1.20f) result = 1.20f;
        return result;
    }

    public static float ComputeCoverageSpeedMod(int activeCount, int optimalTeamSize)
    {
        return ComputeCoverageSpeedMod((float)activeCount, optimalTeamSize);
    }

    // ─── Utility ──────────────────────────────────────────────────────────────

    public static bool IsRoleFitForSkill(RoleId role, SkillId skill)
    {
        switch (role)
        {
            case RoleId.SoftwareEngineer:     return skill == SkillId.Programming;
            case RoleId.SystemsEngineer:      return skill == SkillId.SystemsArchitecture;
            case RoleId.SecurityEngineer:     return skill == SkillId.Security;
            case RoleId.PerformanceEngineer:  return skill == SkillId.PerformanceOptimisation;
            case RoleId.HardwareEngineer:     return skill == SkillId.HardwareIntegration;
            case RoleId.ManufacturingEngineer:return skill == SkillId.Manufacturing;
            case RoleId.ProductDesigner:      return skill == SkillId.ProductDesign;
            case RoleId.GameDesigner:         return skill == SkillId.GameDesign;
            case RoleId.TechnicalArtist:      return skill == SkillId.Vfx;
            case RoleId.AudioDesigner:        return skill == SkillId.AudioDesign;
            case RoleId.QaEngineer:           return skill == SkillId.QaTesting;
            case RoleId.TechnicalSupportSpecialist: return skill == SkillId.TechnicalSupport;
            case RoleId.Marketer:             return skill == SkillId.Marketing;
            case RoleId.SalesExecutive:       return skill == SkillId.Sales;
            case RoleId.Accountant:           return skill == SkillId.Accountancy;
            case RoleId.HrSpecialist:         return skill == SkillId.HrRecruitment;
            default:                          return false;
        }
    }

    public static SkillId MapPhaseToSkill(ProductPhaseType phaseType)
    {
        switch (phaseType)
        {
            case ProductPhaseType.Design:      return SkillId.ProductDesign;
            case ProductPhaseType.Programming: return SkillId.Programming;
            case ProductPhaseType.SFX:         return SkillId.AudioDesign;
            case ProductPhaseType.VFX:         return SkillId.Vfx;
            case ProductPhaseType.QA:          return SkillId.QaTesting;
            default:                           return SkillId.Programming;
        }
    }

    // Fallback CA when no RoleTierTable is available: avg(skills) / 20 * 0.5 + 0.7 → CA int equivalent.
    private static int ComputeFallbackCA(int[] skills)
    {
        if (skills == null || skills.Length == 0) return 0;
        int sum = 0;
        int len = skills.Length;
        for (int i = 0; i < len; i++)
            sum += skills[i];
        float avg = (float)sum / len;
        float mod = 0.70f + (avg / 20f) * 0.50f;
        return (int)System.Math.Round((mod - 0.70f) / 0.50f * 200f);
    }
}
