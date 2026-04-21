public static class TeamTypeMapping
{
    public static TeamType ToTeamType(ProductTeamRole role)
    {
        switch (role)
        {
            case ProductTeamRole.Programming: return TeamType.Programming;
            case ProductTeamRole.Design:      return TeamType.Design;
            case ProductTeamRole.QA:          return TeamType.QA;
            case ProductTeamRole.SFX:         return TeamType.SFX;
            case ProductTeamRole.VFX:         return TeamType.VFX;
            case ProductTeamRole.Marketing:   return TeamType.Marketing;
            default:                          return TeamType.Programming;
        }
    }

    public static ProductTeamRole? ToProductRole(TeamType type)
    {
        switch (type)
        {
            case TeamType.Programming: return ProductTeamRole.Programming;
            case TeamType.Design:      return ProductTeamRole.Design;
            case TeamType.QA:          return ProductTeamRole.QA;
            case TeamType.SFX:         return ProductTeamRole.SFX;
            case TeamType.VFX:         return ProductTeamRole.VFX;
            case TeamType.Marketing:   return ProductTeamRole.Marketing;
            default:                   return null;
        }
    }
}
