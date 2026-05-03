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

    /// <summary>
    /// Per-hidden-attribute modifier array (length 7, indexed by HiddenAttributeId).
    /// Applied during founder hidden attribute generation.
    /// Positive = boost, Negative = penalty.
    /// </summary>
    public int[] HiddenAttributeModifiers;

    /// <summary>Visible attributes explicitly boosted by this personality style (may overlap StrengthAttributes).</summary>
    public VisibleAttributeId[] BoostAttributes;

    /// <summary>How much to add to BoostAttributes / StrengthAttributes during generation.</summary>
    public int BoostAmount;

    /// <summary>How much to subtract from WeakAttributes during generation.</summary>
    public int PenaltyAmount;

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
