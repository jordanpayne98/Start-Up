// SalaryDemandCalculator Version: Clean v3
// Pure static helper — no Unity refs, no state, no allocations.
public static class SalaryDemandCalculator
{
    private const int MinimumWage = 500;

    // ─── Unified demand formula ───────────────────────────────────────────────

    public static int ComputeDemand(RoleId role, int[] skills, int ability, int potential,
        int age, int[] hiddenAttributes)
    {
        int baseSalary = SalaryBand.GetBase(role);

        float caFactor = 1f + (float)System.Math.Max(0, ability - 40) / 100f;
        if (caFactor > 1.8f) caFactor = 1.8f;

        float paFactor = 1f + (System.Math.Max(0, potential - ability) / 200f) * 0.15f;
        if (paFactor > 1.15f) paFactor = 1.15f;

        float ageFactor;
        if      (age < 22) ageFactor = 0.90f;
        else if (age <= 28) ageFactor = 0.95f;
        else if (age <= 35) ageFactor = 1.00f;
        else if (age <= 42) ageFactor = 1.05f;
        else               ageFactor = 1.10f;

        int ambition     = hiddenAttributes != null && hiddenAttributes.Length > (int)HiddenAttributeId.Ambition
            ? hiddenAttributes[(int)HiddenAttributeId.Ambition] : 10;
        int workEthicVis = 10; // WorkEthic is in visible attributes; use default for salary
        int creative     = 10;
        int adaptability = 10;

        float ambitionContrib    = (ambition     - 1) * (0.25f / 19f);
        float workEthicContrib   = (workEthicVis - 1) * (0.08f / 19f);
        float creativeContrib    = (creative     - 1) * (0.05f / 19f);
        float adaptDiscount      = (adaptability - 1) * (0.08f / 19f);
        float personalityFactor  = 1f + ambitionContrib + workEthicContrib + creativeContrib - adaptDiscount;

        int demand = Round50(baseSalary * caFactor * paFactor * ageFactor * personalityFactor);
        return demand < MinimumWage ? MinimumWage : demand;
    }

    // Full overload using EmployeeStatBlock for attribute access.
    public static int ComputeDemand(RoleId role, EmployeeStatBlock stats, int ability, int age)
    {
        int baseSalary = SalaryBand.GetBase(role);

        float caFactor = 1f + (float)System.Math.Max(0, ability - 40) / 100f;
        if (caFactor > 1.8f) caFactor = 1.8f;

        float paFactor = 1f + (System.Math.Max(0, stats.PotentialAbility - ability) / 200f) * 0.15f;
        if (paFactor > 1.15f) paFactor = 1.15f;

        float ageFactor;
        if      (age < 22) ageFactor = 0.90f;
        else if (age <= 28) ageFactor = 0.95f;
        else if (age <= 35) ageFactor = 1.00f;
        else if (age <= 42) ageFactor = 1.05f;
        else               ageFactor = 1.10f;

        int ambition     = stats.GetHiddenAttribute(HiddenAttributeId.Ambition);
        int workEthicVis = stats.GetVisibleAttribute(VisibleAttributeId.WorkEthic);
        int creative     = stats.GetVisibleAttribute(VisibleAttributeId.Creativity);
        int adaptability = stats.GetVisibleAttribute(VisibleAttributeId.Adaptability);

        float ambitionContrib    = (ambition     - 1) * (0.25f / 19f);
        float workEthicContrib   = (workEthicVis - 1) * (0.08f / 19f);
        float creativeContrib    = (creative     - 1) * (0.05f / 19f);
        float adaptDiscount      = (adaptability - 1) * (0.08f / 19f);
        float personalityFactor  = 1f + ambitionContrib + workEthicContrib + creativeContrib - adaptDiscount;

        int demand = Round50(baseSalary * caFactor * paFactor * ageFactor * personalityFactor);
        return demand < MinimumWage ? MinimumWage : demand;
    }

    // Convenience overload for CandidateData.
    public static int ComputeDemand(CandidateData candidate)
    {
        if (candidate == null) return 0;
        int ability = candidate.CurrentAbility > 0 ? candidate.CurrentAbility : 0;
        return ComputeDemand(candidate.Role, candidate.Stats, ability, candidate.Age);
    }

    // ─── Unchanged utilities ──────────────────────────────────────────────────

    public static bool IsSalaryRevealed(CandidateData candidate)
    {
        if (candidate == null) return false;
        if (candidate.IsTargeted) return true;
        if (candidate.InterviewStage >= 3) return true;
        return false;
    }

    public static int Round50(float value)
    {
        return (int)System.Math.Round(value / 50f) * 50;
    }
}
