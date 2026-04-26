// ContractPredictionHelper Version: Clean v1
using System.Collections.Generic;

public enum TeamFitTier
{
    Critical,   // < 30   — will struggle to make meaningful progress
    Low,        // 30–49  — below minimum thresholds
    Moderate,   // 50–69  — meets minimums, misses targets
    Good,       // 70–84  — near or at target skill
    Excellent   // 85–100 — at or above excellence threshold
}

public struct TeamFitResult
{
    public int Score;          // 0–100
    public TeamFitTier Tier;
    public string Label;       // "Excellent", "Good", etc.
    public string CssClass;    // USS class for badge colour
}

/// <summary>
/// Deterministic (no RNG) prediction of how well a team will perform on a contract.
/// Uses the same effective-skill formula as ProcessWork but fixes variance at 1.0.
/// </summary>
public static class ContractPredictionHelper
{
    private const float WorkRatePerSkillPoint = 0.016f;
    private const float TicksPerDay = 4800f;

    public static TeamFitResult Predict(Contract contract, Team team, IReadOnlyList<Employee> employees)
    {
        if (contract == null || team == null || employees == null || team.members == null || team.members.Count == 0)
            return MakeResult(0);

        int memberCount = team.members.Count;
        int contributors = 0;
        float totalSpeedSkill = 0f;
        float qualityWeightedSum = 0f;
        float qualityWeightSum = 0f;
        float moraleSum = 0f;
        int activeCount = 0;

        for (int i = 0; i < memberCount; i++)
        {
            Employee emp = FindEmployee(employees, team.members[i]);
            if (emp == null || !emp.isActive) continue;

            float visibleSkill = emp.GetSkill(contract.RequiredSkill);
            int ca = ComputeSimpleCA(emp);
            bool isRoleFit = TeamWorkEngine.IsRoleFitForSkill(emp.role, contract.RequiredSkill);

            TeamWorkEngine.ComputeEffectiveSkills(
                visibleSkill, ca, emp.morale,
                100f,
                emp.hiddenAttributes.WorkEthic,
                emp.hiddenAttributes.Creative,
                emp.hiddenAttributes.Adaptability,
                isRoleFit,
                emp.personality,
                50f,
                out float speedSkill, out float qualitySkill);

            totalSpeedSkill += speedSkill;
            moraleSum += emp.morale;
            activeCount++;

            if (speedSkill >= 1.5f)
                contributors++;

            float qw = System.Math.Max(qualitySkill, 0.1f);
            qualityWeightedSum += qualitySkill * qw;
            qualityWeightSum += qw;
        }

        if (activeCount == 0)
            return MakeResult(0);

        float teamQualitySkill = qualityWeightSum > 0f ? qualityWeightedSum / qualityWeightSum : 0f;
        float avgMorale = activeCount > 0 ? moraleSum / activeCount : 50f;
        float teamOverhead = System.Math.Max(0.70f, 1f - 0.04f * (activeCount - 1));

        int resolvedMin     = contract.MinContributors > 0 ? contract.MinContributors : 1;
        int resolvedOptimal = contract.OptimalContributors > 0 ? contract.OptimalContributors : resolvedMin + 1;

        float coverageScore;
        if (contributors < resolvedOptimal)
            coverageScore = 0.60f + 0.40f * (contributors / (float)resolvedOptimal);
        else
            coverageScore = 1.0f;

        float skillScore = TeamWorkEngine.ComputeQuality(
            teamQualitySkill,
            contract.MinSkillRequired,
            contract.TargetSkill,
            contract.ExcellenceSkill,
            1f,
            avgMorale) / 100f;

        float deadlineScore = 1.0f;
        if (contract.DeadlineDurationTicks > 0 && contract.TotalWorkRequired > 0)
        {
            float workPerTick = totalSpeedSkill * WorkRatePerSkillPoint * teamOverhead;
            if (workPerTick > 0f)
            {
                float ticksNeeded = contract.TotalWorkRequired / workPerTick;
                deadlineScore = System.Math.Min(1.0f, contract.DeadlineDurationTicks / ticksNeeded);
            }
        }

        float combined = skillScore * 0.50f + coverageScore * 0.30f + deadlineScore * 0.20f;
        int score = (int)(combined * 100f);
        if (score < 0) score = 0;
        if (score > 100) score = 100;
        return MakeResult(score);
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private static TeamFitResult MakeResult(int score)
    {
        TeamFitTier tier;
        string label;
        string css;

        if (score < 30)
        {
            tier  = TeamFitTier.Critical;
            label = "Critical";
            css   = "badge--danger";
        }
        else if (score < 50)
        {
            tier  = TeamFitTier.Low;
            label = "Low";
            css   = "badge--warning";
        }
        else if (score < 70)
        {
            tier  = TeamFitTier.Moderate;
            label = "Moderate";
            css   = "badge--accent";
        }
        else if (score < 85)
        {
            tier  = TeamFitTier.Good;
            label = "Good";
            css   = "badge--success";
        }
        else
        {
            tier  = TeamFitTier.Excellent;
            label = "Excellent";
            css   = "badge--success";
        }

        return new TeamFitResult { Score = score, Tier = tier, Label = label, CssClass = css };
    }

    private static Employee FindEmployee(IReadOnlyList<Employee> employees, EmployeeId id)
    {
        int count = employees.Count;
        for (int i = 0; i < count; i++)
        {
            if (employees[i].id == id) return employees[i];
        }
        return null;
    }

    private static int ComputeSimpleCA(Employee emp)
    {
        int sum = 0;
        int count = emp.skills != null ? emp.skills.Length : 0;
        for (int i = 0; i < count; i++) sum += emp.skills[i];
        float avg = count > 0 ? (float)sum / count : 5f;
        return (int)(System.Math.Min(avg / 20f, 1f) * 200f);
    }
}
