using System;

[Serializable]
public struct HardwareConfiguration {
    public HardwareTier processingTier;
    public HardwareTier graphicsTier;
    public HardwareTier memoryTier;
    public HardwareTier storageTier;
    public ConsoleFormFactor formFactor;
    public int manufactureCostPerUnit;
}
