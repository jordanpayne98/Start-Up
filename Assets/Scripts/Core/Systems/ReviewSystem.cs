using System;
using System.Collections.Generic;
using UnityEngine;

public class ReviewSystem {
    private const int DimensionCount = 6;

    private static readonly ReviewDimension[] _allDimensions = {
        ReviewDimension.Quality,
        ReviewDimension.Functionality,
        ReviewDimension.Innovation,
        ReviewDimension.Stability,
        ReviewDimension.Value,
        ReviewDimension.Polish
    };

    private readonly ReviewOutletDefinition[] _outlets;
    private readonly IRng _rng;
    private readonly TuningConfig _tuning;

    public ReviewSystem(ReviewOutletDefinition[] outlets, IRng rng, TuningConfig tuning = null) {
        _outlets = outlets ?? new ReviewOutletDefinition[0];
        _rng = rng;
        _tuning = tuning;
    }

    public ProductReviewResult GenerateReviews(Product product, ProductTemplateDefinition template, float featureRelevance, ProductIdentitySnapshot? identity = null) {
        float[] rawDimensions = ComputeRawDimensions(product, template, featureRelevance);
        ApplyAntiInflation(rawDimensions, product);

        int outletCount = _outlets.Length;
        var outletReviews = new List<OutletReview>(outletCount);
        float totalScore = 0f;

        float[] aggregateDimensionTotals = new float[DimensionCount];

        for (int o = 0; o < outletCount; o++) {
            var outlet = _outlets[o];
            if (outlet == null) continue;

            float[] outletDimScores = new float[DimensionCount];
            float weightedScore = 0f;

            float[] weights = GetOutletWeights(outlet);

            for (int d = 0; d < DimensionCount; d++) {
                float clamped = rawDimensions[d] < 0f ? 0f : rawDimensions[d] > 100f ? 100f : rawDimensions[d];
                float dimScore = clamped * weights[d];
                outletDimScores[d] = dimScore;
                weightedScore += dimScore;
                aggregateDimensionTotals[d] += clamped;
            }

            float effectiveHarshness = outlet.harshness * (_tuning != null ? _tuning.ReviewHarshnessMultiplier : 1f);
            float harshnessPenalty = (effectiveHarshness - 1f) * 30f;
            float outletScore = weightedScore - harshnessPenalty;
            float volatilityRange = outlet.volatility;
            float noise = (_rng.NextFloat01() * 2f - 1f) * volatilityRange;
            outletScore += noise;

            if (identity.HasValue && identity.Value.IsValid) {
                var snap = identity.Value;
                float polishScore = rawDimensions[(int)ReviewDimension.Polish];
                float stabilityScore = rawDimensions[(int)ReviewDimension.Stability];
                float innovationScore = rawDimensions[(int)ReviewDimension.Innovation];
                float valueScore = rawDimensions[(int)ReviewDimension.Value];
                float functionalityScore = rawDimensions[(int)ReviewDimension.Functionality];

                int profileAdjustment = 0;

                if (snap.PricePositioning >= 40 && polishScore < 55f)
                    profileAdjustment -= 4;
                if (snap.PricePositioning >= 40 && polishScore >= 75f && valueScore >= 50f)
                    profileAdjustment += 2;

                if (snap.InnovationRisk >= 40 && innovationScore >= 70f)
                    profileAdjustment += 3;
                if (snap.InnovationRisk >= 40 && stabilityScore < 50f)
                    profileAdjustment -= 3;

                if (snap.AudienceBreadth >= 40 && functionalityScore < 55f)
                    profileAdjustment -= 3;

                if (snap.FeatureScope >= 40 && (polishScore < 55f || stabilityScore < 55f))
                    profileAdjustment -= 3;

                if (snap.ProductionDiscipline >= 40 && stabilityScore >= 65f)
                    profileAdjustment += 2;

                if (snap.ProductionDiscipline <= -40 && (stabilityScore < 55f || product.DateShiftCount >= 2))
                    profileAdjustment -= 3;

                profileAdjustment = Mathf.Clamp(profileAdjustment, -6, 6);
                outletScore += profileAdjustment;
            }

            if (outletScore > 90f) {
                float excess = outletScore - 90f;
                outletScore = 90f + excess * (10f / (10f + excess));
            }
            outletScore = outletScore < 0f ? 0f : outletScore > 100f ? 100f : outletScore;

            var review = new OutletReview {
                OutletId = outlet.outletId,
                OutletName = outlet.displayName,
                OutletStyle = outlet.outletStyle,
                Score = outletScore,
                DimensionKeys = new ReviewDimension[DimensionCount],
                DimensionValues = new float[DimensionCount]
            };

            for (int d = 0; d < DimensionCount; d++) {
                review.DimensionKeys[d] = _allDimensions[d];
                review.DimensionValues[d] = outletDimScores[d];
            }

            outletReviews.Add(review);
            totalScore += outletScore;
        }

        float aggregateScore = outletCount > 0 ? totalScore / outletCount : 0f;
        aggregateScore = aggregateScore < 0f ? 0f : aggregateScore > 100f ? 100f : aggregateScore;

        var result = new ProductReviewResult {
            AggregateScore = aggregateScore,
            OutletReviews = outletReviews,
            DimensionKeys = new ReviewDimension[DimensionCount],
            DimensionValues = new float[DimensionCount]
        };

        for (int d = 0; d < DimensionCount; d++) {
            result.DimensionKeys[d] = _allDimensions[d];
            result.DimensionValues[d] = outletCount > 0 ? aggregateDimensionTotals[d] / outletCount : 0f;
        }

        return result;
    }

    private float[] ComputeRawDimensions(Product product, ProductTemplateDefinition template, float featureRelevance) {
        float[] dims = new float[DimensionCount];

        // Quality: OverallQuality directly mapped 0-100
        dims[(int)ReviewDimension.Quality] = product.OverallQuality < 0f ? 0f : product.OverallQuality > 100f ? 100f : product.OverallQuality;

        // Functionality: feature count ratio vs pool size
        int poolSize = template.availableFeatures != null ? template.availableFeatures.Length : 1;
        int selectedCount = product.SelectedFeatureIds != null ? product.SelectedFeatureIds.Length : 0;
        float ratio;
        if (poolSize <= 8) {
            ratio = poolSize > 0 ? (float)selectedCount / poolSize : 0f;
        } else if (poolSize <= 20) {
            ratio = (float)selectedCount / (poolSize * 0.6f);
        } else {
            ratio = (float)selectedCount / (poolSize * 0.4f);
        }
        ratio = ratio < 0f ? 0f : ratio > 1f ? 1f : ratio;
        dims[(int)ReviewDimension.Functionality] = ratio * 100f;

        // Innovation: featureRelevance (0-1 mapped to 0-100)
        float innovationRaw = featureRelevance * 100f;
        dims[(int)ReviewDimension.Innovation] = innovationRaw < 0f ? 0f : innovationRaw > 100f ? 100f : innovationRaw;

        // Stability: inverse of bugs — fewer bugs = higher score, linear with floor (gentler than squared)
        float bugRatio = product.BugsRemaining / 30f;
        bugRatio = bugRatio < 0f ? 0f : bugRatio > 1f ? 1f : bugRatio;
        float stabilityCurve = 1f - bugRatio * 0.7f;
        dims[(int)ReviewDimension.Stability] = stabilityCurve * 100f;

        // Value: feature coverage ratio vs price
        float priceCenter = template.economyConfig?.pricePerUnit > 0f ? template.economyConfig.pricePerUnit : 1f;
        float price = product.PriceOverride > 0f ? product.PriceOverride : priceCenter;
        float coverageRatio = poolSize > 0 ? (float)selectedCount / poolSize : 0f;
        coverageRatio = coverageRatio < 0f ? 0f : coverageRatio > 1f ? 1f : coverageRatio;
        float featureValue = coverageRatio * 80f + 10f;
        float priceNorm = price / priceCenter;
        float valueScore = featureValue * (1.5f - priceNorm * 0.5f);
        dims[(int)ReviewDimension.Value] = valueScore < 0f ? 0f : valueScore > 100f ? 100f : valueScore;

        // Polish: dev time adequacy
        float expectedTicks = ComputeExpectedTicks(template, _tuning);
        float actualTicks = product.TotalDevelopmentTicks;
        float timeRatio = expectedTicks > 0f ? actualTicks / expectedTicks : 1f;
        float polishScore;
        if (timeRatio >= 1.5f) {
            float extra = (timeRatio - 1.5f) * 30f;
            polishScore = 85f + (extra < 15f ? extra : 15f);
        } else if (timeRatio >= 1f) {
            polishScore = 55f + (timeRatio - 1f) * 60f;
        } else {
            polishScore = timeRatio * 55f;
        }
        dims[(int)ReviewDimension.Polish] = polishScore < 0f ? 0f : polishScore > 100f ? 100f : polishScore;

        return dims;
    }

    public static float ComputeExpectedTicks(ProductTemplateDefinition template, TuningConfig tuning) {
        if (template.phases == null) return 1f;
        float workMultiplier = tuning?.ProductBaseWorkMultiplier ?? 100f;
        int featureCount = template.availableFeatures != null ? template.availableFeatures.Length : 0;
        float difficultyScale = 1.0f + (template.difficultyTier - 1) * 0.75f;
        float featureScale = 1.0f + featureCount * 0.12f + (float)Math.Pow(featureCount, 1.5) * 0.02f;
        float total = 0f;
        int count = template.phases.Length;
        for (int i = 0; i < count; i++) {
            var phase = template.phases[i];
            if (phase != null) total += phase.baseWorkUnits * workMultiplier * difficultyScale * featureScale;
        }
        return total > 0f ? total : 1f;
    }

    private void ApplyAntiInflation(float[] dims, Product product) {
        // Sequel fatigue: each sequel iteration reduces innovation score
        if (product.SequelNumber > 0) {
            float fatigue = product.SequelNumber * 0.12f;
            fatigue = fatigue < 0f ? 0f : fatigue > 1f ? 1f : fatigue;
            dims[(int)ReviewDimension.Innovation] *= (1f - fatigue);
        }

        // Hype delivery check: if hype was very high but quality is low, dock quality perception
        if (product.HypeAtShip > 60f && product.OverallQuality < product.HypeAtShip * 0.6f) {
            float raw = (product.HypeAtShip - product.OverallQuality) / 100f;
            float hypePenalty = (raw < 0f ? 0f : raw > 1f ? 1f : raw) * 15f;
            dims[(int)ReviewDimension.Quality] = dims[(int)ReviewDimension.Quality] - hypePenalty;
            if (dims[(int)ReviewDimension.Quality] < 0f) dims[(int)ReviewDimension.Quality] = 0f;
        }

        // Niche saturation: if this product is late to a very established niche, dock innovation
        if (product.BugsRemaining > 10f) {
            float raw = (product.BugsRemaining - 10f) / 20f;
            float bugOverrun = raw < 0f ? 0f : raw > 1f ? 1f : raw;
            dims[(int)ReviewDimension.Stability] = dims[(int)ReviewDimension.Stability] - bugOverrun * 15f;
            if (dims[(int)ReviewDimension.Stability] < 0f) dims[(int)ReviewDimension.Stability] = 0f;
        }

        if (product.BugsRemaining > 20f) {
            float raw = (product.BugsRemaining - 20f) / 30f;
            float qualityBleed = raw < 0f ? 0f : raw > 1f ? 1f : raw;
            dims[(int)ReviewDimension.Quality] = dims[(int)ReviewDimension.Quality] - qualityBleed * 8f;
            if (dims[(int)ReviewDimension.Quality] < 0f) dims[(int)ReviewDimension.Quality] = 0f;
        }

        if (product.BugsRemaining > 15f) {
            float raw = (product.BugsRemaining - 15f) / 25f;
            float polishBleed = raw < 0f ? 0f : raw > 1f ? 1f : raw;
            dims[(int)ReviewDimension.Polish] = dims[(int)ReviewDimension.Polish] - polishBleed * 10f;
            if (dims[(int)ReviewDimension.Polish] < 0f) dims[(int)ReviewDimension.Polish] = 0f;
        }
    }

    private float[] GetOutletWeights(ReviewOutletDefinition outlet) {
        var weights = new float[DimensionCount];
        weights[(int)ReviewDimension.Quality] = outlet.qualityWeight;
        weights[(int)ReviewDimension.Functionality] = outlet.functionalityWeight;
        weights[(int)ReviewDimension.Innovation] = outlet.innovationWeight;
        weights[(int)ReviewDimension.Stability] = outlet.stabilityWeight;
        weights[(int)ReviewDimension.Value] = outlet.valueWeight;
        weights[(int)ReviewDimension.Polish] = outlet.polishWeight;
        return weights;
    }
}
