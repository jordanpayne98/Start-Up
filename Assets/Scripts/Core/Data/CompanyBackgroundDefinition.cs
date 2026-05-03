using UnityEngine;

[CreateAssetMenu(menuName = "StartUp/CompanyBackgroundDefinition")]
public class CompanyBackgroundDefinition : ScriptableObject
{
    public int BackgroundId;
    public string DisplayName;
    [TextArea] public string Description;
    public RoleId[] RecommendedFounderRoles;
    public string[] StartingStrengths;
    public string[] StartingRisks;
    public string[] SuggestedFirstActions;
    public string DifficultyLabel;

    // Legacy string-based bias tags (retained for backward compatibility)
    public string[] CandidatePoolBiasTags;
    public string[] ContractPoolBiasTags;

    // Typed candidate pool bias — used by StartingPoolGenerator directly
    public RoleFamily[] CandidatePoolBiasFamilies;
    public float[] CandidatePoolBiasWeights;

    // Typed contract pool bias — used by StartingContractGenerator
    public string[] ContractPoolBiasCategories;

    // Startup inbox messages
    [TextArea] public string WelcomeMessage;
    [TextArea] public string HiringHint;
}
