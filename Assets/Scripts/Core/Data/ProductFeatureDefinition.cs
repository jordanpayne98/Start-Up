using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(menuName = "StartUp/Product Feature")]
public class ProductFeatureDefinition : ScriptableObject
{
    public string featureId;
    public string displayName;
    public string description;
    public FeatureCategory featureCategory;

    [Header("Generation Gating")]
    public int availableFromGeneration = 1;
    public bool nativeOnly = false;

    [Header("Paradigm Affinities")]
    public ParadigmAffinity[] paradigmAffinities;

    [Header("Cross-Product Dependencies")]
    public string requiresPlatformFeature;
    public string requiresToolFeature;

    [Header("Feature Interactions")]
    [FormerlySerializedAs("synergyFeatureIds")]
    public string[] synergyFeatureIds;
    [FormerlySerializedAs("conflictFeatureIds")]
    public string[] conflictFeatureIds;
    public string[] prerequisiteFeatureIds;
    public float synergyBonusPercent;
    public float conflictPenaltyPercent;

    [Header("Hardware Constraints (Console Only)")]
    public int constrainedByHardware = -1;    // -1 = no constraint; cast to HardwareComponent when >= 0
    public int minimumHardwareTier = -1;      // -1 = no minimum; cast to HardwareTier when >= 0
    public int formFactorRequired = -1;       // -1 = any form factor; cast to ConsoleFormFactor when >= 0

    [Header("Skill Requirements")]
    public int requiredTotalSkillPoints = 0;
    public SkillId requiredSkillType = SkillId.Programming;

    [Header("Cost & Quality")]
    public int baseCost = 0;
    public float devCostMultiplier = 1f;
    public float qualityWeight = 1f;

    [Header("Demand Lifecycle")]
    public int demandIntroductionGen = 1;
    public int demandMaturitySpeed = 2;
    public bool isFoundational = false;
}
