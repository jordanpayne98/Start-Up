using UnityEngine;

[CreateAssetMenu(menuName = "StartUp/Market Niche")]
public class MarketNicheData : ScriptableObject
{
    public ProductNiche niche;
    public string displayName;
    [Range(0f, 100f)] public float baseDemand = 50f;
    public float volatility = 5f;

    [System.Obsolete("Use recoveryRateMin/recoveryRateMax. Removed in Market Overhaul Part 2.")]
    public float recoveryRate = 0.05f;

    [Header("Recovery")]
    [Range(0.005f, 0.2f)] public float recoveryRateMin = 0.02f;
    [Range(0.005f, 0.2f)] public float recoveryRateMax = 0.10f;

    [Header("Saturation")]
    public int saturationThreshold = 5;
    [Range(0.1f, 2f)] public float saturationPenaltyPerProduct = 0.4f;

    public float demandFloor = 10f;
    public float demandCeiling = 90f;

    [Header("User Pool")]
    public int maxUserPool = 50_000_000;

    [Header("Market Interest")]
    [Range(0.01f, 0.40f)] public float interestRate = 0.15f;

    [Header("Economy")]
    public float basePricePerUnit = 20f;
    public float baseSubscriptionPrice = 10f;
    [Range(0.3f, 5.0f)] public float devTimeMultiplier = 1.0f;

    [Header("Feature Category Affinity")]
    [Range(0.5f, 1.5f)] public float coreAffinity = 1.0f;
    [Range(0.5f, 1.5f)] public float technicalAffinity = 1.0f;
    [Range(0.5f, 1.5f)] public float socialAffinity = 1.0f;
    [Range(0.5f, 1.5f)] public float qualityOfLifeAffinity = 1.0f;
    [Range(0.5f, 1.5f)] public float contentAffinity = 1.0f;

    public float GetAffinityForCategory(FeatureCategory category) {
        switch (category) {
            // Core / fundamental functionality
            case FeatureCategory.Core:
            case FeatureCategory.Frontend:
            case FeatureCategory.CoreAI:
            case FeatureCategory.MobileCore:
            case FeatureCategory.DesktopCore:
            case FeatureCategory.SaaSPlatform:
            case FeatureCategory.Compute:
            case FeatureCategory.Rendering:
            case FeatureCategory.Hardware:
            case FeatureCategory.Production:
            case FeatureCategory.Protection:
            case FeatureCategory.System:
            case FeatureCategory.Gameplay:
                return coreAffinity;

            // Technical / infrastructure / backend
            case FeatureCategory.Backend:
            case FeatureCategory.Infrastructure:
            case FeatureCategory.CloudNetwork:
            case FeatureCategory.CloudManagement:
            case FeatureCategory.Simulation:
            case FeatureCategory.Detection:
            case FeatureCategory.Network:
            case FeatureCategory.Processing:
            case FeatureCategory.Services:
            case FeatureCategory.Operations:
            case FeatureCategory.Platform:
                return technicalAffinity;

            // Social / community / collaboration
            case FeatureCategory.Social:
            case FeatureCategory.Collaboration:
            case FeatureCategory.Distribution:
            case FeatureCategory.Ecosystem:
            case FeatureCategory.Integration:
            case FeatureCategory.DesktopIntegration:
                return socialAffinity;

            // Quality of life / UX / DX
            case FeatureCategory.DeveloperExperience:
            case FeatureCategory.Experience:
            case FeatureCategory.Interface:
            case FeatureCategory.DesktopInterface:
            case FeatureCategory.AIInterface:
            case FeatureCategory.Tooling:
            case FeatureCategory.Management:
                return qualityOfLifeAffinity;

            // Content / intelligence / data
            case FeatureCategory.Data:
            case FeatureCategory.Intelligence:
            case FeatureCategory.Presentation:
            case FeatureCategory.Pipeline:
            case FeatureCategory.Creation:
                return contentAffinity;

            default:
                return 1.0f;
        }
    }
}
