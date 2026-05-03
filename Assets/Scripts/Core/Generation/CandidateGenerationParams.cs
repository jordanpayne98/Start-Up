public struct CandidateGenerationParams
{
    public CandidateSource Source;
    public RoleId? ForceRole;
    public RoleFamily? ForceFamily;
    public float QualityMultiplier;
    public CandidateArchetype? ForceArchetype;
    public CareerStage? ForceCareerStage;
    // Optional per-role weight overrides (length RoleIdHelper.RoleCount = 16); null = use profile defaults
    public float[] RoleWeightOverrides;

    // Convenience factories

    public static CandidateGenerationParams OpenMarket(float quality = 1f)
    {
        return new CandidateGenerationParams
        {
            Source           = CandidateSource.OpenMarket,
            QualityMultiplier = quality
        };
    }

    public static CandidateGenerationParams HRSearch(RoleId role, RoleFamily family, float quality)
    {
        return new CandidateGenerationParams
        {
            Source            = CandidateSource.HRSearch,
            ForceRole         = role,
            ForceFamily       = family,
            QualityMultiplier = quality
        };
    }

    public static CandidateGenerationParams StartingPool(float quality, RoleFamily? biasFamily)
    {
        return new CandidateGenerationParams
        {
            Source            = CandidateSource.StartingPool,
            ForceFamily       = biasFamily,
            QualityMultiplier = quality
        };
    }
}
