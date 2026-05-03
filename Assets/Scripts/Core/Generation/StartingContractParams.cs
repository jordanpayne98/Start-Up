public struct StartingContractParams
{
    public CompanyBackgroundDefinition Background;
    public RoleId[] FounderRoles;       // to ensure at least one founder-suitable contract
    public float DifficultyMultiplier;  // default 1.0

    public static StartingContractParams Default(CompanyBackgroundDefinition background)
    {
        return new StartingContractParams
        {
            Background            = background,
            FounderRoles          = new RoleId[0],
            DifficultyMultiplier  = 1.0f
        };
    }
}
