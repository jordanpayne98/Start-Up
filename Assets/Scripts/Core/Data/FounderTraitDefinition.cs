using UnityEngine;

/// <summary>
/// Lightweight optional founder trait — applies skill modifiers and event weight tags.
/// Page 04 section 21. Traits are modifiers and event weights, not massive stat bonuses.
/// </summary>
[CreateAssetMenu(menuName = "StartUp/FounderTraitDefinition")]
public class FounderTraitDefinition : ScriptableObject
{
    public int TraitId;
    public string DisplayName;
    [TextArea] public string Description;

    /// <summary>Skills affected by this trait modifier.</summary>
    public SkillId[] StatModifierSkills;

    /// <summary>Modifier values parallel to StatModifierSkills. Positive = bonus, negative = penalty.</summary>
    public int[] StatModifierValues;

    /// <summary>Tags used by the event system for weighted event selection.</summary>
    public string[] EventWeightTags;

    public int SortOrder;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(DisplayName))
            Debug.LogWarning($"[FounderTraitDefinition] '{name}' missing DisplayName.");
        if (StatModifierSkills != null && StatModifierValues != null &&
            StatModifierSkills.Length != StatModifierValues.Length)
            Debug.LogWarning($"[FounderTraitDefinition] '{name}' StatModifierSkills and StatModifierValues length mismatch.");
    }
#endif
}
