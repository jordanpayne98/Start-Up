using UnityEngine;

[CreateAssetMenu(menuName = "StartUp/FounderPersonalityStyleDefinition")]
public class FounderPersonalityStyleDefinition : ScriptableObject
{
    public int StyleId;
    public string DisplayName;
    [TextArea] public string Description;
    public VisibleAttributeId[] StrengthAttributes;
    public VisibleAttributeId[] WeakAttributes;
    public HiddenAttributeId[] HiddenTendencies;
    public string BestPairing;
    [TextArea] public string RiskWarning;
    public int SortOrder;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(DisplayName))
            Debug.LogWarning($"[FounderPersonalityStyleDefinition] '{name}' missing DisplayName.");
        if (StrengthAttributes == null || StrengthAttributes.Length == 0)
            Debug.LogWarning($"[FounderPersonalityStyleDefinition] '{name}' has no StrengthAttributes.");
    }
#endif
}
