using System.Collections.Generic;

public class CandidateReport
{
    public string SummaryLabel;
    public List<string> Strengths = new List<string>();
    public List<string> Concerns = new List<string>();
    public List<string> RiskFlags = new List<string>();
    public List<RoleId> RecommendedRoles = new List<RoleId>();
    public List<string> SalaryNotes = new List<string>();
    public List<string> TeamFitNotes = new List<string>();
    public ConfidenceLevel OverallConfidence;
}
