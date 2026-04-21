using UnityEngine;

[CreateAssetMenu(menuName = "StartUp/Cross-Product Gate Config")]
public class CrossProductGateConfig : ScriptableObject {
    [Header("Quality Tier Thresholds")]
    public float[] tierThresholds = { 0f, 20f, 40f, 60f, 80f };
    public float[] tierCeilings   = { 0f, 30f, 50f, 65f, 80f };

    public float GetTierCeiling(float upstreamQuality) {
        if (upstreamQuality <= 0f) return 0f;
        if (tierThresholds == null || tierThresholds.Length == 0) return float.MaxValue;
        for (int i = tierThresholds.Length - 1; i >= 0; i--) {
            if (upstreamQuality >= tierThresholds[i]) {
                if (i == tierThresholds.Length - 1) return float.MaxValue;
                return tierCeilings[i];
            }
        }
        return 0f;
    }
}
