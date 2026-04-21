// TeamWorkEngine Version: Clean v1
using System.Collections.Generic;

// Pure-static utility for all shared work/quality formulas.
// No state, no allocations, no Unity references.
public struct TeamWorkResult
{
    public float TotalSpeedSkill;
    public float AvgQualitySkill;
    public int Contributors;
    public int ActiveCount;
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
        float workEthic,
        float creative,
        float adaptability,
        bool isRoleFit,
        out float speedSkill,
        out float qualitySkill)
    {
        float caMod = 0.70f + (ca / 200f) * 0.50f;
        float moraleMod = MoraleSystem.MoraleMultiplier(morale);
        float workEthicMod = 0.90f + (workEthic / 20f) * 0.20f;
        float creativeMod = 0.90f + (creative / 20f) * 0.20f;
        float offRoleSpeedMod = isRoleFit ? 1.0f : 0.25f + (adaptability / 20f) * 0.25f;
        float offRoleQualityMod = isRoleFit ? 1.0f : 0.30f + (adaptability / 20f) * 0.30f;

        // Soft-cap curve: inflection at skill 6 (average new hire baseline).
        // Below 6 the curve provides a slight boost; above 6 it compresses.
        // Skill 6→6.0, 10→7.75, 15→9.49, 20→10.95 (1.82x at 20 vs 3.33x linear).
        const float SkillDiminishingReturnsPivot = 6f;
        float scaledSkill = SkillDiminishingReturnsPivot * (float)System.Math.Sqrt(visibleSkill / SkillDiminishingReturnsPivot);

        speedSkill = scaledSkill * caMod * moraleMod * workEthicMod * offRoleSpeedMod;
        qualitySkill = scaledSkill * caMod * moraleMod * creativeMod * offRoleQualityMod;
    }

    // ─── Real Team Aggregation ─────────────────────────────────────────────────

    public static TeamWorkResult AggregateTeam(
        List<EmployeeId> members,
        EmployeeSystem empSystem,
        SkillType requiredSkill,
        RoleTierTable roleTierTable,
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

        int memberCount = members.Count;
        for (int i = 0; i < memberCount; i++)
        {
            Employee emp = empSystem.GetEmployee(members[i]);
            if (emp == null || !emp.isActive) continue;

            activeCount++;

            int ca;
            if (roleTierTable != null)
            {
                int[] tiers = roleTierTable.GetTiers(emp.role);
                ca = tiers != null ? AbilityCalculator.ComputeAbility(emp.skills, tiers) : ComputeFallbackCA(emp.skills);
            }
            else
            {
                ca = ComputeFallbackCA(emp.skills);
            }

            bool isRoleFit = IsRoleFitForSkill(emp.role, requiredSkill);

            ComputeEffectiveSkills(
                emp.GetSkill(requiredSkill),
                ca,
                emp.morale,
                emp.hiddenAttributes.WorkEthic,
                emp.hiddenAttributes.Creative,
                emp.hiddenAttributes.Adaptability,
                isRoleFit,
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
        float overhead = activeCount > 0 ? System.Math.Max(0.70f, 1f - overheadPerMember * (activeCount - 1)) : 1f;
        float avgMorale = activeCount > 0 ? moraleSum / activeCount : 50f;
        float coverageQualityMod = ComputeCoverageQualityMod(activeCount, optimalTeamSize);
        float coverageSpeedMod = ComputeCoverageSpeedMod(activeCount, optimalTeamSize);

        return new TeamWorkResult
        {
            TotalSpeedSkill = totalSpeedSkill,
            AvgQualitySkill = avgQualitySkill,
            Contributors = contributors,
            ActiveCount = activeCount,
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
        ComputeEffectiveSkills(
            syntheticVisibleSkill,
            (int)syntheticCA,
            syntheticMorale,
            syntheticWorkEthic,
            syntheticCreative,
            syntheticAdaptability,
            true,
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
        float extraMult = 1f)
    {
        return team.TotalSpeedSkill * workRatePerSkillPoint * team.Overhead * coverageMod * varianceMod * extraMult;
    }

    // ─── Quality Curve ─────────────────────────────────────────────────────────

    public static float ComputeQuality(
        float avgQualitySkill,
        float minThreshold,
        float targetThreshold,
        float excellenceThreshold,
        float coverageQualityMod,
        float avgMorale = 50f)
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

        float quality = baseQuality * coverageQualityMod;
        if (quality < 0f) quality = 0f;
        if (quality > 100f) quality = 100f;
        return quality;
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

    public static float ComputeCoverageQualityMod(int activeCount, int optimalTeamSize)
    {
        if (optimalTeamSize <= 0) optimalTeamSize = 1;
        float ratio = (float)activeCount / optimalTeamSize;

        if (ratio <= 0f) return 0.4f;
        if (ratio < 1.0f) return 0.5f + 0.5f * (float)System.Math.Sqrt(ratio);

        float bonus = 0.25f * (float)System.Math.Log(ratio + 1.0, 2.0) / (float)System.Math.Log(4.0, 2.0);
        float result = 1.0f + bonus;
        if (result > 1.25f) result = 1.25f;
        return result;
    }

    // ─── Coverage Speed Modifier ───────────────────────────────────────────────

    public static float ComputeCoverageSpeedMod(int activeCount, int optimalTeamSize)
    {
        if (optimalTeamSize <= 0) optimalTeamSize = 1;
        float ratio = (float)activeCount / optimalTeamSize;

        if (ratio <= 0f) return 0.1f;
        if (ratio < 1.0f) return 0.1f + 0.9f * ratio * ratio;

        float bonus = 0.2f * (float)System.Math.Log(ratio, 2.0);
        float result = 1.0f + bonus;
        if (result > 1.20f) result = 1.20f;
        return result;
    }

    // ─── Utility ──────────────────────────────────────────────────────────────

    public static bool IsRoleFitForSkill(EmployeeRole role, SkillType skill)
    {
        switch (role)
        {
            case EmployeeRole.Developer:     return skill == SkillType.Programming;
            case EmployeeRole.Designer:      return skill == SkillType.Design;
            case EmployeeRole.QAEngineer:    return skill == SkillType.QA;
            case EmployeeRole.SoundEngineer: return skill == SkillType.SFX;
            case EmployeeRole.VFXArtist:     return skill == SkillType.VFX;
            default:                         return false;
        }
    }

    public static SkillType MapPhaseToSkill(ProductPhaseType phaseType)
    {
        switch (phaseType)
        {
            case ProductPhaseType.Design:      return SkillType.Design;
            case ProductPhaseType.Programming: return SkillType.Programming;
            case ProductPhaseType.SFX:         return SkillType.SFX;
            case ProductPhaseType.VFX:         return SkillType.VFX;
            case ProductPhaseType.QA:          return SkillType.QA;
            default:                           return SkillType.Programming;
        }
    }

    // Fallback CA when no RoleTierTable is available: avg(skills) / 20 * 0.5 + 0.7 → CA int equivalent.
    // Mirrors ContractPredictionHelper.ComputeCA approach.
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
