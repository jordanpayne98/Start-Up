using UnityEngine;

[CreateAssetMenu(menuName = "StartUp/FounderArchetypeDefinition")]
public class FounderArchetypeDefinition : ScriptableObject
{
    public int ArchetypeId;
    public string DisplayName;
    [TextArea] public string Description;
    public RoleId Role;

    /// <summary>3-4 primary skills highlighted for this archetype.</summary>
    public SkillId[] TopSkills;

    public string BestEarlyUse;
    public string[] Strengths;
    public string[] Risks;
    public string RecommendedPairing;

    public int CAMin;
    public int CAMax;
    public int PAMin;
    public int PAMax;

    /// <summary>Skills that receive bonus points during stat generation for this archetype.</summary>
    public SkillId[] SkillBiasProfile;

    /// <summary>
    /// Visible attribute biases for this archetype (e.g., Focus, WorkEthic, Initiative).
    /// Used during stat generation to weight attribute rolls.
    /// </summary>
    public VisibleAttributeId[] AttributeBias;

    /// <summary>
    /// If true, this archetype requires a specific company background to unlock.
    /// </summary>
    public bool IsGatedArchetype;

    /// <summary>
    /// Human-readable gate condition shown to player when locked.
    /// Example: "Security Founder requires Enterprise Consultancy background."
    /// </summary>
    public string GateCondition;

    public int SortOrder;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(DisplayName))
            Debug.LogWarning($"[FounderArchetypeDefinition] '{name}' missing DisplayName.");
        if (TopSkills == null || TopSkills.Length < 1)
            Debug.LogWarning($"[FounderArchetypeDefinition] '{name}' must have at least 1 TopSkill.");
        if (CAMin > CAMax)
            Debug.LogWarning($"[FounderArchetypeDefinition] '{name}' CAMin > CAMax.");
        if (PAMin > PAMax)
            Debug.LogWarning($"[FounderArchetypeDefinition] '{name}' PAMin > PAMax.");
        if (IsGatedArchetype && string.IsNullOrWhiteSpace(GateCondition))
            Debug.LogWarning($"[FounderArchetypeDefinition] '{name}' IsGatedArchetype is true but GateCondition is empty.");
    }
#endif
}
