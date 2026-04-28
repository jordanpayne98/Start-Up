using UnityEngine;

[CreateAssetMenu(menuName = "StartUp/FounderWeaknessDefinition")]
public class FounderWeaknessDefinition : ScriptableObject
{
    public int WeaknessId;
    public string DisplayName;
    [TextArea] public string Description;
    [TextArea] public string Upside;
    public VisibleAttributeId[] AffectedAttributes;
    public int[] AttributeModifiers;
    public string[] RiskTags;
    public int SortOrder;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(DisplayName))
            Debug.LogWarning($"[FounderWeaknessDefinition] '{name}' missing DisplayName.");
        if (AffectedAttributes != null && AttributeModifiers != null && AffectedAttributes.Length != AttributeModifiers.Length)
            Debug.LogWarning($"[FounderWeaknessDefinition] '{name}' AffectedAttributes and AttributeModifiers length mismatch.");
    }
#endif
}
