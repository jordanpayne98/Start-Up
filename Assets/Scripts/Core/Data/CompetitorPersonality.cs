using System;
using UnityEngine;

[Serializable]
public struct CompetitorPersonality {
    [Range(0f, 1f)] public float RiskTolerance;
    [Range(0f, 1f)] public float RdSpeed;
    [Range(0f, 1f)] public float BrandInvestment;
    [Range(0f, 1f)] public float PricingAggression;
    [Range(0f, 1f)] public float InnovationBias;

    public static CompetitorPersonality Roll(IRng rng, CompetitorPersonality min, CompetitorPersonality max) {
        return new CompetitorPersonality {
            RiskTolerance    = min.RiskTolerance    + rng.NextFloat01() * (max.RiskTolerance    - min.RiskTolerance),
            RdSpeed          = min.RdSpeed          + rng.NextFloat01() * (max.RdSpeed          - min.RdSpeed),
            BrandInvestment  = min.BrandInvestment  + rng.NextFloat01() * (max.BrandInvestment  - min.BrandInvestment),
            PricingAggression = min.PricingAggression + rng.NextFloat01() * (max.PricingAggression - min.PricingAggression),
            InnovationBias   = min.InnovationBias   + rng.NextFloat01() * (max.InnovationBias   - min.InnovationBias)
        };
    }

    public void ApplyDrift(CompetitorMomentum momentum, float driftAmount) {
        switch (momentum) {
            case CompetitorMomentum.Rising:
                BrandInvestment = Clamp01(BrandInvestment + driftAmount);
                RiskTolerance = Clamp01(RiskTolerance + driftAmount * 0.5f);
                InnovationBias = Clamp01(InnovationBias + driftAmount * 0.3f);
                break;
            case CompetitorMomentum.Declining:
                PricingAggression = Clamp01(PricingAggression - driftAmount);
                RdSpeed = Clamp01(RdSpeed + driftAmount * 0.5f);
                RiskTolerance = Clamp01(RiskTolerance - driftAmount * 0.3f);
                break;
            case CompetitorMomentum.Crisis:
                RiskTolerance = Clamp01(RiskTolerance - driftAmount * 2f);
                BrandInvestment = Clamp01(BrandInvestment - driftAmount);
                InnovationBias = Clamp01(InnovationBias - driftAmount);
                break;
        }
    }

    private static float Clamp01(float value) {
        if (value < 0f) return 0f;
        if (value > 1f) return 1f;
        return value;
    }
}

