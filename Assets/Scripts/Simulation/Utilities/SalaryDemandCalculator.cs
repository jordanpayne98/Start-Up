// SalaryDemandCalculator Version: Clean v1
using System;

/// <summary>
/// Pure static helper. Computes the exact salary demand per hiring path.
/// No Unity refs, no state, no allocations.
/// </summary>
public static class SalaryDemandCalculator
{
    // Fallback const values — used when no TuningConfig is supplied
    public const float HRMultiplier        = 0.80f;
    public const float InterviewMultiplier = 0.90f;
    public const float DirectMultiplier    = 1.15f;
    public const float MaxNegotiationDiscount = 0.15f;
    private const int MinimumWage = 500;

    /// <summary>
    /// Returns the effective monthly salary demand for a candidate given their current hiring path state.
    /// </summary>
    public static int GetEffectiveDemand(CandidateData candidate, float avgNegotiationSkill,
        int resolvedStage = -1, TuningConfig tuning = null)
    {
        if (candidate == null) return 0;

        if (candidate.IsTargeted)
            return ComputeHRDemand(candidate.Salary, avgNegotiationSkill, tuning);

        int stage = resolvedStage >= 0 ? resolvedStage : candidate.InterviewStage;
        if (stage >= 3)
            return ComputeInterviewedDemand(candidate.Salary, tuning);

        return ComputeDirectDemand(candidate.Salary, tuning);
    }

    public static int ComputeHRDemand(int baseSalary, float avgNegotiationSkill,
        TuningConfig tuning = null)
    {
        float hrMult   = tuning != null ? tuning.SalaryHRMultiplier           : HRMultiplier;
        float maxDisc  = tuning != null ? tuning.SalaryMaxNegotiationDiscount  : MaxNegotiationDiscount;
        int   minWage  = tuning != null ? tuning.SalaryMinimumWage             : MinimumWage;
        float globalMult = tuning != null ? tuning.SalaryGlobalMultiplier : 1f;

        float skill = avgNegotiationSkill;
        if (skill < 0f) skill = 0f;
        if (skill > 20f) skill = 20f;

        float negotiationDiscount = (skill / 20f) * maxDisc;
        int result = Round50(baseSalary * hrMult * (1f - negotiationDiscount) * globalMult);
        return result < minWage ? minWage : result;
    }

    public static int ComputeInterviewedDemand(int baseSalary, TuningConfig tuning = null)
    {
        float mult    = tuning != null ? tuning.SalaryInterviewMultiplier : InterviewMultiplier;
        int   minWage = tuning != null ? tuning.SalaryMinimumWage         : MinimumWage;
        float globalMult = tuning != null ? tuning.SalaryGlobalMultiplier : 1f;
        int result = Round50(baseSalary * mult * globalMult);
        return result < minWage ? minWage : result;
    }

    public static int ComputeDirectDemand(int baseSalary, TuningConfig tuning = null)
    {
        float mult    = tuning != null ? tuning.SalaryDirectMultiplier : DirectMultiplier;
        int   minWage = tuning != null ? tuning.SalaryMinimumWage      : MinimumWage;
        float globalMult = tuning != null ? tuning.SalaryGlobalMultiplier : 1f;
        int result = Round50(baseSalary * mult * globalMult);
        return result < minWage ? minWage : result;
    }

    /// <summary>Whether the exact salary demand is visible to the player for this candidate.</summary>
    public static bool IsSalaryRevealed(CandidateData candidate)
    {
        if (candidate == null) return false;
        if (candidate.IsTargeted) return true;
        if (candidate.InterviewStage >= 3) return true;
        return false;
    }

    public static int ComputeRenewalDemand(Employee emp)
    {
        if (emp == null) return 0;
        int baseSalary = SalaryBand.GetBase(emp.role);
        float uniform = 1f / SkillTypeHelper.SkillTypeCount;
        float sum = 0f;
        for (int i = 0; i < SkillTypeHelper.SkillTypeCount; i++)
            sum += emp.skills[i] * uniform;
        int ca = (int)(sum * 10f);
        if (ca < 0) ca = 0;
        if (ca > 200) ca = 200;
        float caFactor = 1f + (float)System.Math.Max(0, ca - 40) / 100f;
        if (caFactor > 1.8f) caFactor = 1.8f;
        float ambFactor = 1f + (emp.hiddenAttributes.Ambition - 1) * (0.30f / 19f);
        int salary = (int)(baseSalary * caFactor * ambFactor);
        salary = ((salary + 50) / 100) * 100;
        if (salary < MinimumWage) salary = MinimumWage;
        return salary;
    }

    public static int Round50(float value)
    {
        return (int)Math.Round(value / 50f) * 50;
    }
}
