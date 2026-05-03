public enum SortDirection
{
    Ascending,
    Descending
}

public enum SeverityState
{
    Normal,
    Warning,
    Danger
}

public enum HRTab
{
    Employees = 0,
    Candidates = 1
}

public struct TeamSummaryDisplay
{
    public TeamId Id;
    public string Name;
    public int MemberCount;
    public string ContractName;
    public string TeamType;
    public TeamType TeamTypeEnum;
    public float AvgMorale;
}
