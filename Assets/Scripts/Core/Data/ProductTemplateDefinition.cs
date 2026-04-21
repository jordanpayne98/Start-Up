using System;
using UnityEngine;

[Serializable]
public struct NicheConfig
{
    public ProductNiche niche;
    public int retentionMonths;
    public float volatility;
}

[CreateAssetMenu(menuName = "StartUp/Product Template")]
public class ProductTemplateDefinition : ScriptableObject
{
    public string templateId;
    public string displayName;
    public string description;
    public ProductCategory category;
    public ProductLayer layer;
    public int baseUpfrontCost;

    [Header("Difficulty")]
    [Range(1, 5)]
    public int difficultyTier = 3;

    [Header("Platform & Tool Dependencies")]
    public ProductCategory[] validTargetPlatforms;
    [Tooltip("If set, only OperatingSystem platforms with these niches are shown. GameConsole filtering still uses validTargetPlatforms.")]
    public ProductNiche[] validPlatformNiches;
    public ProductCategory[] requiredToolTypes;
    public float ownToolQualityBonus = 0.15f;
    public float licensedToolQualityBonus = 0.08f;

    [Header("Team Scaling")]
    public int optimalTeamSizePerPhase = 4;

    [Header("Paradigm & Cost")]
    public float paradigmSensitivity = 1f;

    [Header("Niche Configuration")]
    public NicheConfig[] nicheConfigs;
    public string[] revenueModelOptions;

    public bool HasNiches => nicheConfigs != null && nicheConfigs.Length > 0;

    [Header("Phases & Features")]
    public ProductPhaseDefinition[] phases;
    public ProductFeatureDefinition[] availableFeatures;
    public ProductEconomyConfig economyConfig;
}
