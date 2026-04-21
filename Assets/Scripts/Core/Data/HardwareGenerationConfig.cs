using UnityEngine;

[CreateAssetMenu(menuName = "StartUp/Hardware Generation Config")]
public class HardwareGenerationConfig : ScriptableObject {
    public int generation;
    public HardwareTierConfig[] processingTiers;
    public HardwareTierConfig[] graphicsTiers;
    public HardwareTierConfig[] memoryTiers;
    public HardwareTierConfig[] storageTiers;
    public FormFactorConfig[] formFactors;

    [Header("Market Expectations")]
    public HardwareTier expectedProcessingTier = HardwareTier.MidRange;
    public HardwareTier expectedGraphicsTier   = HardwareTier.MidRange;
    public HardwareTier expectedMemoryTier     = HardwareTier.Budget;
    public HardwareTier expectedStorageTier    = HardwareTier.Budget;

    public HardwareTierConfig GetTierConfig(HardwareComponent component, HardwareTier tier) {
        HardwareTierConfig[] tiers = GetTierArray(component);
        if (tiers == null) return default;
        for (int i = 0; i < tiers.Length; i++) {
            if (tiers[i].tier == tier) return tiers[i];
        }
        return tiers.Length > 0 ? tiers[0] : default;
    }

    public FormFactorConfig GetFormFactorConfig(ConsoleFormFactor formFactor) {
        if (formFactors == null) return default;
        for (int i = 0; i < formFactors.Length; i++) {
            if (formFactors[i].formFactor == formFactor) return formFactors[i];
        }
        return formFactors.Length > 0 ? formFactors[0] : default;
    }

    public int CalculateManufactureCost(HardwareConfiguration config) {
        FormFactorConfig ff = GetFormFactorConfig(config.formFactor);
        float mult = ff.manufactureCostMultiplier > 0f ? ff.manufactureCostMultiplier : 1f;
        int sum = GetTierConfig(HardwareComponent.Processing, config.processingTier).manufactureCostPerUnit
                + GetTierConfig(HardwareComponent.Graphics, config.graphicsTier).manufactureCostPerUnit
                + GetTierConfig(HardwareComponent.Memory, config.memoryTier).manufactureCostPerUnit
                + GetTierConfig(HardwareComponent.Storage, config.storageTier).manufactureCostPerUnit;
        return (int)(sum * mult);
    }

    public int CalculateDevCostAdd(HardwareConfiguration config) {
        FormFactorConfig ff = GetFormFactorConfig(config.formFactor);
        float mult = ff.devCostMultiplier > 0f ? ff.devCostMultiplier : 1f;
        int sum = GetTierConfig(HardwareComponent.Processing, config.processingTier).devCostAdd
                + GetTierConfig(HardwareComponent.Graphics, config.graphicsTier).devCostAdd
                + GetTierConfig(HardwareComponent.Memory, config.memoryTier).devCostAdd
                + GetTierConfig(HardwareComponent.Storage, config.storageTier).devCostAdd;
        return (int)(sum * mult);
    }

    public float GetHardwareCeiling(HardwareComponent component, HardwareTier tier, ConsoleFormFactor formFactor) {
        HardwareTierConfig tc = GetTierConfig(component, tier);
        FormFactorConfig ff = GetFormFactorConfig(formFactor);
        float ffMult = ff.ceilingMultiplier > 0f ? ff.ceilingMultiplier : 1f;
        return tc.qualityCeiling * ffMult;
    }

    private HardwareTierConfig[] GetTierArray(HardwareComponent component) {
        switch (component) {
            case HardwareComponent.Processing: return processingTiers;
            case HardwareComponent.Graphics:   return graphicsTiers;
            case HardwareComponent.Memory:     return memoryTiers;
            case HardwareComponent.Storage:    return storageTiers;
            default:                           return null;
        }
    }
}
