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
    public string[] CandidatePoolBiasTags;
    public string[] ContractPoolBiasTags;
}
