using UnityEngine;

[CreateAssetMenu(menuName = "StartUp/FounderExpertiseDefinition")]
public class FounderExpertiseDefinition : ScriptableObject
{
    public int ExpertiseId;
    public string DisplayName;

    /// <summary>Skills that receive +2-3 bonus from this expertise area.</summary>
    public SkillId[] BoostedSkills;

    /// <summary>Attributes that receive +1 bonus from this expertise area.</summary>
    public VisibleAttributeId[] BoostedAttributes;

    /// <summary>Display text for card tooltip — roles this expertise is most relevant for.</summary>
    public string[] RelevantRoles;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(DisplayName))
            Debug.LogWarning($"[FounderExpertiseDefinition] '{name}' missing DisplayName.");
        if (BoostedSkills == null || BoostedSkills.Length == 0)
            Debug.LogWarning($"[FounderExpertiseDefinition] '{name}' has no BoostedSkills.");
    }
#endif
}
