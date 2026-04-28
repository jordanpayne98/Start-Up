using System.Collections.Generic;

public enum BenchmarkConfidence : byte
{
    Estimated,
    Limited,
    Moderate,
    High
}

public enum BenchmarkVisibility : byte
{
    Basic,
    RoleBreakdown,
    FullDetail
}

public struct RoleBenchmark
{
    public int MarketRate;
    public float AvgCostPerOutput;
    public int DataPointCount;
    public BenchmarkConfidence Confidence;
    public float FtRatio;
    public int FtAvgSalary;
    public int PtAvgSalary;
}

public struct CompanyBenchmark
{
    public float OverallCostPerOutput;
    public int TotalEmployees;
    public float OverallFtRatio;
    public BenchmarkConfidence Confidence;
}

public static class CompetitorBenchmarkCalculator
{
    public static RoleBenchmark ComputeRoleBenchmark(RoleId role, IReadOnlyList<Competitor> competitors, EmployeeState employeeState)
    {
        if (competitors == null || competitors.Count == 0 || employeeState == null)
            return FallbackRoleBenchmark(role);

        float totalWeightedCost = 0f;
        float totalOutput       = 0f;
        long  ftSalarySum       = 0L;
        int   ftCount           = 0;
        long  ptSalarySum       = 0L;
        int   ptCount           = 0;
        int   dataPoints        = 0;

        int compCount = competitors.Count;
        for (int c = 0; c < compCount; c++)
        {
            Competitor comp = competitors[c];
            if (comp == null || comp.IsBankrupt || comp.IsAbsorbed) continue;
            if (comp.EmployeeIds == null || comp.EmployeeIds.Count == 0) continue;

            bool hasRoleEmployee = false;
            int empCount = comp.EmployeeIds.Count;
            for (int e = 0; e < empCount; e++)
            {
                EmployeeId eid = comp.EmployeeIds[e];
                if (!employeeState.employees.TryGetValue(eid, out Employee emp)) continue;
                if (!emp.isActive || emp.role != role) continue;

                float output = emp.EffectiveOutput > 0f ? emp.EffectiveOutput : 1f;
                float costPerOutput = emp.salary / output;
                totalWeightedCost += costPerOutput * output;
                totalOutput       += output;

                if (emp.ArrangementType == EmploymentType.FullTime)
                {
                    ftSalarySum += emp.salary;
                    ftCount++;
                }
                else
                {
                    ptSalarySum += emp.salary;
                    ptCount++;
                }

                hasRoleEmployee = true;
            }

            if (hasRoleEmployee) dataPoints++;
        }

        if (dataPoints == 0)
            return FallbackRoleBenchmark(role);

        float avgCostPerOutput = totalOutput > 0f ? totalWeightedCost / totalOutput : 0f;
        int totalSalaries = ftCount + ptCount;
        float ftRatio = totalSalaries > 0 ? (float)ftCount / totalSalaries : 1f;
        int ftAvgSalary = ftCount > 0 ? (int)(ftSalarySum / ftCount) : 0;
        int ptAvgSalary = ptCount > 0 ? (int)(ptSalarySum / ptCount) : 0;

        return new RoleBenchmark
        {
            MarketRate       = SalaryBand.GetBase(role),
            AvgCostPerOutput = avgCostPerOutput,
            DataPointCount   = dataPoints,
            Confidence       = ToConfidence(dataPoints),
            FtRatio          = ftRatio,
            FtAvgSalary      = ftAvgSalary,
            PtAvgSalary      = ptAvgSalary
        };
    }

    public static CompanyBenchmark ComputeCompanyBenchmark(IReadOnlyList<Competitor> competitors, EmployeeState employeeState)
    {
        if (competitors == null || competitors.Count == 0 || employeeState == null)
            return new CompanyBenchmark { Confidence = BenchmarkConfidence.Estimated };

        float totalWeightedCost = 0f;
        float totalOutput       = 0f;
        int   ftCount           = 0;
        int   totalCount        = 0;
        int   dataPoints        = 0;

        int compCount = competitors.Count;
        for (int c = 0; c < compCount; c++)
        {
            Competitor comp = competitors[c];
            if (comp == null || comp.IsBankrupt || comp.IsAbsorbed) continue;
            if (comp.EmployeeIds == null || comp.EmployeeIds.Count == 0) continue;

            bool hasEmployee = false;
            int empCount = comp.EmployeeIds.Count;
            for (int e = 0; e < empCount; e++)
            {
                EmployeeId eid = comp.EmployeeIds[e];
                if (!employeeState.employees.TryGetValue(eid, out Employee emp)) continue;
                if (!emp.isActive) continue;

                float output = emp.EffectiveOutput > 0f ? emp.EffectiveOutput : 1f;
                totalWeightedCost += (emp.salary / output) * output;
                totalOutput       += output;
                totalCount++;

                if (emp.ArrangementType == EmploymentType.FullTime) ftCount++;

                hasEmployee = true;
            }

            if (hasEmployee) dataPoints++;
        }

        float overallCostPerOutput = totalOutput > 0f ? totalWeightedCost / totalOutput : 0f;
        float overallFtRatio       = totalCount > 0 ? (float)ftCount / totalCount : 1f;

        return new CompanyBenchmark
        {
            OverallCostPerOutput = overallCostPerOutput,
            TotalEmployees       = totalCount,
            OverallFtRatio       = overallFtRatio,
            Confidence           = ToConfidence(dataPoints)
        };
    }

    public static BenchmarkVisibility GetVisibility(int playerReputation)
    {
        if (playerReputation >= 60) return BenchmarkVisibility.FullDetail;
        if (playerReputation >= 30) return BenchmarkVisibility.RoleBreakdown;
        return BenchmarkVisibility.Basic;
    }

    private static RoleBenchmark FallbackRoleBenchmark(RoleId role)
    {
        return new RoleBenchmark
        {
            MarketRate       = SalaryBand.GetBase(role),
            AvgCostPerOutput = SalaryBand.GetBase(role),
            DataPointCount   = 0,
            Confidence       = BenchmarkConfidence.Estimated,
            FtRatio          = 1f,
            FtAvgSalary      = SalaryBand.GetBase(role),
            PtAvgSalary      = SalaryDemandCalculator.Round50(SalaryBand.GetBase(role) * 0.60f)
        };
    }

    private static BenchmarkConfidence ToConfidence(int dataPoints)
    {
        if (dataPoints == 0) return BenchmarkConfidence.Estimated;
        if (dataPoints == 1) return BenchmarkConfidence.Limited;
        if (dataPoints <= 4) return BenchmarkConfidence.Moderate;
        return BenchmarkConfidence.High;
    }
}
