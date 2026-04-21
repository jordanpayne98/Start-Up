using UnityEngine;

[CreateAssetMenu(menuName = "StartUp/Review Outlet")]
public class ReviewOutletDefinition : ScriptableObject {
    public string outletId;
    public string displayName;
    public string outletStyle;

    [Header("Dimension Weights (must sum to ~1.0)")]
    [Range(0f, 0.5f)] public float qualityWeight = 0.25f;
    [Range(0f, 0.5f)] public float functionalityWeight = 0.20f;
    [Range(0f, 0.5f)] public float innovationWeight = 0.20f;
    [Range(0f, 0.5f)] public float stabilityWeight = 0.15f;
    [Range(0f, 0.5f)] public float valueWeight = 0.10f;
    [Range(0f, 0.5f)] public float polishWeight = 0.10f;

    [Header("Personality")]
    [Range(0.5f, 1.5f)] public float harshness = 1.0f;
    [Range(0f, 10f)] public float volatility = 3f;
}
