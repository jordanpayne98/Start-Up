public static class TeamTypeMapping
{
    public static TeamType ToTeamType(ProductTeamRole role)
    {
        switch (role)
        {
            case ProductTeamRole.Development: return TeamType.Development;
            case ProductTeamRole.Design:      return TeamType.Design;
            case ProductTeamRole.QA:          return TeamType.QA;
            case ProductTeamRole.Marketing:   return TeamType.Marketing;
            default:                          return TeamType.Development;
        }
    }

    public static ProductTeamRole? ToProductRole(TeamType type)
    {
        switch (type)
        {
            case TeamType.Development: return ProductTeamRole.Development;
            case TeamType.Design:      return ProductTeamRole.Design;
            case TeamType.QA:          return ProductTeamRole.QA;
            case TeamType.Marketing:   return ProductTeamRole.Marketing;
            default:                   return null;
        }
    }
}
