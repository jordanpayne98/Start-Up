using System;

[Serializable]
public struct HardwareTierConfig {
    public HardwareTier tier;
    public int manufactureCostPerUnit;
    public float qualityCeiling;
    public int devCostAdd;
}

[Serializable]
public struct FormFactorConfig {
    public ConsoleFormFactor formFactor;
    public float manufactureCostMultiplier;
    public float ceilingMultiplier;
    public float devCostMultiplier;
}
