// SalaryDemandCalculator Version: Clean v2
// Pure static helper — no Unity refs, no state, no allocations.
public static class SalaryDemandCalculator
{
    private const int MinimumWage = 500;

    // ─── Unified demand formula ───────────────────────────────────────────────

    public static int ComputeDemand(EmployeeRole role, int[] skills, int ability, int potential,
        int age, HiddenAttributes hidden)
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

        float ambitionContrib    = (hidden.Ambition     - 1) * (0.25f / 19f);
        float workEthicContrib   = (hidden.WorkEthic    - 1) * (0.08f / 19f);
        float creativeContrib    = (hidden.Creative     - 1) * (0.05f / 19f);
        float adaptDiscount      = (hidden.Adaptability - 1) * (0.08f / 19f);
        float personalityFactor  = 1f + ambitionContrib + workEthicContrib + creativeContrib - adaptDiscount;

        int demand = Round50(baseSalary * caFactor * paFactor * ageFactor * personalityFactor);
        return demand < MinimumWage ? MinimumWage : demand;
    }

    // Convenience overload for CandidateData — ability derived from skills via AbilityCalculator.
    public static int ComputeDemand(CandidateData candidate)
    {
        if (candidate == null) return 0;
        int[] tiers = candidate.GetRoleTiersForSalary();
        int ability = AbilityCalculator.ComputeAbility(candidate.Skills, tiers);
        return ComputeDemand(candidate.Role, candidate.Skills, ability,
            candidate.PotentialAbility, candidate.Age, candidate.HiddenAttributes);
    }

    // Convenience overload for Employee — ability derived from role-weighted skills.
    public static int ComputeDemand(Employee employee)
    {
        if (employee == null) return 0;
        int[] tiers = employee.GetRoleTiersForSalary();
        int ability = AbilityCalculator.ComputeAbility(employee.skills, tiers);
        return ComputeDemand(employee.role, employee.skills, ability,
            employee.potentialAbility, employee.age, employee.hiddenAttributes);
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
