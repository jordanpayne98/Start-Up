public struct StartingPoolParams
{
    public CompanyBackgroundDefinition Background;
    public RoleId[] FounderRoles;           // roles already covered by founders
    public SkillId[] FounderWeakSkills;     // skills where founders are weak (below threshold)
    public RoleFamily[] FounderWeakFamilies; // role families not covered by any founder
    public int PoolSize;                    // default 10
    public float QualityMultiplier;         // default 1.0, can be adjusted by difficulty

    public static StartingPoolParams Default(CompanyBackgroundDefinition background)
    {
        return new StartingPoolParams
        {
            Background           = background,
            FounderRoles         = new RoleId[0],
            FounderWeakSkills    = new SkillId[0],
            FounderWeakFamilies  = new RoleFamily[0],
            PoolSize             = 10,
            QualityMultiplier    = 1.0f
        };
    }
}
