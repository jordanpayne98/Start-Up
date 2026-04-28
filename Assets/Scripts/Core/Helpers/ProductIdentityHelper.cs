using System;
using System.Collections.Generic;

public static class ProductIdentityHelper
{
    internal struct ProductIdentityInputs
    {
        public float PriceNorm;
        public float CoverageRatio;
        public float ExpectedSelectedRatio;
        public float MissingExpectedRatio;
        public float EmergingSelectedRatio;
        public float FoundationalSelectedRatio;

        public float PlatformBreadthRatio;
        public float AvgPlatformCeilingNorm;
        public float AvgToolCeilingNorm;

        public float ScheduleRatio;
        public float DelayRatio;
        public float PivotRatio;
        public float CutRatio;

        public float BugPressure;
        public float BugControl;

        public float LowPrice;
        public float PremiumPrice;
        public float ScopeLoad;
        public float LowScopeLoad;
        public float RushFactor;
        public float SchedulePolish;
    }

    public static ProductIdentitySnapshot ComputeAtShip(
        Product product,
        ProductTemplateDefinition template,
        GenerationSystem generationSystem,
        PlatformSystem platformSystem,
        ProductState productState,
        TuningConfig tuning)
    {
        if (product == null || template == null)
            return default;

        var inputs = BuildInputsFromProduct(product, template, generationSystem, platformSystem, productState, tuning, isPreview: false);
        var snapshot = ComputeSnapshot(in inputs, tuning);
        snapshot.LastComputedTick = product.ShipTick;
        snapshot.IsValid = true;
        return snapshot;
    }

    public static ProductIdentitySnapshot ComputeCurrent(
        Product product,
        ProductTemplateDefinition template,
        GenerationSystem generationSystem,
        PlatformSystem platformSystem,
        ProductState productState,
        TuningConfig tuning)
    {
        if (product == null || template == null)
            return default;

        var inputs = BuildInputsFromProduct(product, template, generationSystem, platformSystem, productState, tuning, isPreview: false);
        var snapshot = ComputeSnapshot(in inputs, tuning);
        snapshot.LastComputedTick = product.ShipTick + product.TicksSinceShip;
        snapshot.IsValid = true;
        return snapshot;
    }

    public static ProductIdentityPreview ComputePreview(
        ProductCreationPlanningViewModel draft,
        ProductTemplateDefinition template,
        GenerationSystem generationSystem,
        PlatformSystem platformSystem,
        ProductState productState,
        TuningConfig tuning,
        int currentTick,
        int targetReleaseTick)
    {
        if (draft == null || template == null)
            return default;

        var inputs = BuildInputsFromDraft(draft, template, generationSystem, platformSystem, productState, tuning, currentTick, targetReleaseTick);
        var snapshot = ComputeSnapshot(in inputs, tuning);
        snapshot.LastComputedTick = currentTick;
        snapshot.IsValid = true;

        return new ProductIdentityPreview { Snapshot = snapshot };
    }

    public static string BuildReviewerCommentary(ProductIdentitySnapshot snapshot)
    {
        if (!snapshot.IsValid) return null;

        ProductIdentityTag t1 = snapshot.PrimaryTag;
        ProductIdentityTag t2 = snapshot.SecondaryTag;

        if ((t1 == ProductIdentityTag.Premium && t2 == ProductIdentityTag.Disciplined) ||
            (t1 == ProductIdentityTag.Disciplined && t2 == ProductIdentityTag.Premium))
            return "Reviewers see this as a premium release with clear polish expectations.";

        if ((t1 == ProductIdentityTag.Premium && t2 == ProductIdentityTag.Chaotic) ||
            (t1 == ProductIdentityTag.Chaotic && t2 == ProductIdentityTag.Premium))
            return "Reviewers see this as a premium release that may have launched rough.";

        if ((t1 == ProductIdentityTag.Experimental && t2 == ProductIdentityTag.FeatureHeavy) ||
            (t1 == ProductIdentityTag.FeatureHeavy && t2 == ProductIdentityTag.Experimental))
            return "Reviewers see this as an ambitious, high-risk product.";

        if ((t1 == ProductIdentityTag.Broad && t2 == ProductIdentityTag.Accessible) ||
            (t1 == ProductIdentityTag.Accessible && t2 == ProductIdentityTag.Broad))
            return "Reviewers see this as a broad-market product aimed at a wide audience.";

        if ((t1 == ProductIdentityTag.Specialist && t2 == ProductIdentityTag.Refined) ||
            (t1 == ProductIdentityTag.Refined && t2 == ProductIdentityTag.Specialist))
            return "Reviewers see this as a focused specialist product rather than a mass-market release.";

        if (t1 == ProductIdentityTag.Chaotic || t2 == ProductIdentityTag.Chaotic)
            return "Reviewers see signs of production instability in the final release.";

        return null;
    }

    private static ProductIdentityInputs BuildInputsFromProduct(
        Product product,
        ProductTemplateDefinition template,
        GenerationSystem generationSystem,
        PlatformSystem platformSystem,
        ProductState productState,
        TuningConfig tuning,
        bool isPreview)
    {
        int selectedCount = product.SelectedFeatureIds != null ? product.SelectedFeatureIds.Length : 0;
        int availableCount = template.availableFeatures != null ? template.availableFeatures.Length : 0;
        int droppedCount = product.DroppedFeatureIds != null ? product.DroppedFeatureIds.Count : 0;

        float priceCenter = template.economyConfig != null && template.economyConfig.pricePerUnit > 0f
            ? template.economyConfig.pricePerUnit : 1f;
        float price = product.PriceOverride > 0f ? product.PriceOverride : priceCenter;

        int currentGen = generationSystem != null ? generationSystem.GetCurrentGeneration() : 1;

        CountFeatureBuckets(
            product.SelectedFeatureIds, template.availableFeatures, currentGen,
            out int expectedCount, out int selectedExpectedCount, out int selectedEmergingCount, out int selectedFoundationalCount);

        int compatiblePlatformCount = CountCompatibleOnMarketPlatforms(template, productState);
        int targetPlatformCount = product.TargetPlatformIds != null ? product.TargetPlatformIds.Length : 0;

        float avgPlatformCeiling = ComputeAvgPlatformCeiling(product.TargetPlatformIds, platformSystem, product.OwnerCompanyId.IsPlayer);
        float avgToolCeiling = ComputeAvgToolCeiling(product.RequiredToolIds, productState);

        float expectedTicks = ReviewSystem.ComputeExpectedTicks(template, tuning);
        float scheduleTicks = product.TotalDevelopmentTicks;

        int dateShiftCount = product.DateShiftCount;
        int pivotsUsed = product.PivotsUsed;
        int maxPivots = product.MaxPivots > 0 ? product.MaxPivots : 1;

        float bugPressure = Clamp01(product.BugsRemaining / 30f);

        return BuildInputsCore(
            priceCenter, price,
            selectedCount, availableCount, droppedCount,
            expectedCount, selectedExpectedCount, selectedEmergingCount, selectedFoundationalCount,
            compatiblePlatformCount, targetPlatformCount,
            avgPlatformCeiling, avgToolCeiling,
            expectedTicks, scheduleTicks,
            dateShiftCount, pivotsUsed, maxPivots,
            bugPressure, false, 0f, 0f, 0f);
    }

    private static ProductIdentityInputs BuildInputsFromDraft(
        ProductCreationPlanningViewModel draft,
        ProductTemplateDefinition template,
        GenerationSystem generationSystem,
        PlatformSystem platformSystem,
        ProductState productState,
        TuningConfig tuning,
        int currentTick,
        int targetReleaseTick)
    {
        var allFeatures = draft.AllFeatures;
        var selectedIndices = draft.Draft.SelectedFeatureIds;
        string[] selectedFeatureIds = new string[selectedIndices.Count];
        for (int i = 0; i < selectedIndices.Count; i++)
        {
            int idx = selectedIndices[i];
            if (idx >= 0 && idx < allFeatures.Count)
                selectedFeatureIds[i] = allFeatures[idx].FeatureId;
        }

        var platIndices = draft.SelectedPlatformIndices;
        var platOptions = draft.PlatformOptions;
        List<ProductId> selectedPlatformIds = new List<ProductId>();
        for (int i = 0; i < platIndices.Count; i++)
        {
            int idx = platIndices[i];
            if (idx >= 0 && idx < platOptions.Count)
                selectedPlatformIds.Add(new ProductId(platOptions[idx].PlatformIdValue));
        }

        int selectedCount = selectedFeatureIds != null ? selectedFeatureIds.Length : 0;
        int availableCount = template.availableFeatures != null ? template.availableFeatures.Length : 0;

        float priceCenter = template.economyConfig != null && template.economyConfig.pricePerUnit > 0f
            ? template.economyConfig.pricePerUnit : 1f;
        float price = draft.TargetPrice > 0 ? draft.TargetPrice : priceCenter;

        int currentGen = generationSystem != null ? generationSystem.GetCurrentGeneration() : 1;

        CountFeatureBuckets(
            selectedFeatureIds, template.availableFeatures, currentGen,
            out int expectedCount, out int selectedExpectedCount, out int selectedEmergingCount, out int selectedFoundationalCount);

        int compatiblePlatformCount = CountCompatibleOnMarketPlatforms(template, productState);
        int targetPlatformCount = selectedPlatformIds != null ? selectedPlatformIds.Count : 0;

        ProductId[] platformIdArr = null;
        if (selectedPlatformIds != null && selectedPlatformIds.Count > 0) {
            platformIdArr = new ProductId[selectedPlatformIds.Count];
            for (int i = 0; i < selectedPlatformIds.Count; i++)
                platformIdArr[i] = selectedPlatformIds[i];
        }

        float avgPlatformCeiling = ComputeAvgPlatformCeilingFromIds(platformIdArr, platformSystem, isPlayerOwned: true);
        float avgToolCeiling = ComputeAvgToolCeiling(new ProductId[0], productState);

        float expectedTicks = ReviewSystem.ComputeExpectedTicks(template, tuning);
        float plannedTicks = targetReleaseTick > currentTick
            ? (float)(targetReleaseTick - currentTick)
            : expectedTicks;

        float emergingSelectedRatio = selectedCount > 0 ? (float)selectedEmergingCount / selectedCount : 0f;
        float rushFactor = Remap01(1f - (expectedTicks > 0f ? plannedTicks / expectedTicks : 1f), 0f, 0.35f);
        float coverageRatioPreview = availableCount > 0 ? Clamp01((float)selectedCount / availableCount) : 0f;
        float bugPressure = Clamp01(
            coverageRatioPreview * 0.45f +
            emergingSelectedRatio * 0.25f +
            rushFactor * 0.30f);

        return BuildInputsCore(
            priceCenter, price,
            selectedCount, availableCount, 0,
            expectedCount, selectedExpectedCount, selectedEmergingCount, selectedFoundationalCount,
            compatiblePlatformCount, targetPlatformCount,
            avgPlatformCeiling, avgToolCeiling,
            expectedTicks, plannedTicks,
            0, 0, 1,
            bugPressure, true, emergingSelectedRatio, rushFactor, coverageRatioPreview);
    }

    private static ProductIdentityInputs BuildInputsCore(
        float priceCenter, float price,
        int selectedCount, int availableCount, int droppedCount,
        int expectedCount, int selectedExpectedCount, int selectedEmergingCount, int selectedFoundationalCount,
        int compatiblePlatformCount, int targetPlatformCount,
        float avgPlatformCeiling, float avgToolCeiling,
        float expectedTicks, float scheduleTicks,
        int dateShiftCount, int pivotsUsed, int maxPivots,
        float bugPressure, bool overrideEmergingRush, float emergingOverride, float rushOverride, float coverageOverride)
    {
        var inputs = new ProductIdentityInputs();

        inputs.PriceNorm = priceCenter > 0f ? price / priceCenter : 1f;
        inputs.PremiumPrice = Remap01(inputs.PriceNorm, 1.05f, 1.45f);
        inputs.LowPrice = Remap01(1.05f - inputs.PriceNorm, 0f, 0.35f);

        inputs.CoverageRatio = overrideEmergingRush
            ? coverageOverride
            : (availableCount > 0 ? Clamp01((float)selectedCount / availableCount) : 0f);

        inputs.ExpectedSelectedRatio = expectedCount > 0 ? (float)selectedExpectedCount / expectedCount : 1f;
        inputs.MissingExpectedRatio = 1f - inputs.ExpectedSelectedRatio;

        inputs.EmergingSelectedRatio = overrideEmergingRush
            ? emergingOverride
            : (selectedCount > 0 ? (float)selectedEmergingCount / selectedCount : 0f);

        inputs.FoundationalSelectedRatio = selectedCount > 0 ? (float)selectedFoundationalCount / selectedCount : 0f;

        inputs.PlatformBreadthRatio = compatiblePlatformCount > 0
            ? Clamp01((float)targetPlatformCount / compatiblePlatformCount)
            : 0f;

        inputs.AvgPlatformCeilingNorm = avgPlatformCeiling / 100f;
        inputs.AvgToolCeilingNorm = avgToolCeiling / 100f;

        inputs.ScheduleRatio = expectedTicks > 0f ? scheduleTicks / expectedTicks : 1f;
        inputs.SchedulePolish = Remap01(inputs.ScheduleRatio, 0.95f, 1.40f);
        inputs.RushFactor = overrideEmergingRush
            ? rushOverride
            : Remap01(1f - inputs.ScheduleRatio, 0f, 0.35f);

        inputs.DelayRatio = Remap01(dateShiftCount, 0f, 3f);
        inputs.PivotRatio = maxPivots > 0 ? Clamp01((float)pivotsUsed / maxPivots) : 0f;
        inputs.CutRatio = (selectedCount + droppedCount) > 0
            ? (float)droppedCount / (selectedCount + droppedCount)
            : 0f;

        inputs.ScopeLoad = Remap01(inputs.CoverageRatio, 0.45f, 0.90f);
        inputs.LowScopeLoad = 1f - inputs.ScopeLoad;

        inputs.BugPressure = bugPressure;
        inputs.BugControl = 1f - inputs.BugPressure;

        return inputs;
    }

    private static ProductIdentitySnapshot ComputeSnapshot(in ProductIdentityInputs i, TuningConfig tuning)
    {
        float premium =
            0.55f * i.PremiumPrice +
            0.20f * i.AvgToolCeilingNorm +
            0.10f * i.AvgPlatformCeilingNorm +
            0.15f * i.SchedulePolish;

        float accessible =
            0.60f * i.LowPrice +
            0.25f * i.PlatformBreadthRatio +
            0.15f * i.ExpectedSelectedRatio;

        float experimental =
            0.40f * i.EmergingSelectedRatio +
            0.20f * i.MissingExpectedRatio +
            0.20f * i.ScopeLoad +
            0.10f * i.PivotRatio +
            0.10f * i.DelayRatio;

        float safe =
            0.45f * i.FoundationalSelectedRatio +
            0.25f * i.ExpectedSelectedRatio +
            0.20f * i.LowScopeLoad +
            0.10f * (1f - i.PivotRatio);

        float broad =
            0.45f * i.PlatformBreadthRatio +
            0.30f * i.ExpectedSelectedRatio +
            0.25f * i.LowPrice;

        float specialist =
            0.45f * (1f - i.PlatformBreadthRatio) +
            0.30f * i.PremiumPrice +
            0.25f * i.EmergingSelectedRatio;

        float featureHeavy =
            0.55f * i.CoverageRatio +
            0.20f * i.EmergingSelectedRatio +
            0.15f * Remap01(i.ScheduleRatio, 1.10f, 1.60f) +
            0.10f * (1f - i.CutRatio);

        float refined =
            0.45f * Remap01(0.55f - i.CoverageRatio, 0f, 0.35f) +
            0.30f * i.CutRatio +
            0.25f * i.SchedulePolish;

        float disciplined =
            0.35f * i.SchedulePolish +
            0.30f * i.ExpectedSelectedRatio +
            0.20f * i.CutRatio +
            0.15f * i.BugControl;

        float chaotic =
            0.30f * i.DelayRatio +
            0.25f * i.PivotRatio +
            0.25f * i.RushFactor +
            0.20f * i.BugPressure;

        var snapshot = new ProductIdentitySnapshot
        {
            Version = 1,
            PricePositioning = ComputeAxis(premium, accessible),
            InnovationRisk = ComputeAxis(experimental, safe),
            AudienceBreadth = ComputeAxis(broad, specialist),
            FeatureScope = ComputeAxis(featureHeavy, refined),
            ProductionDiscipline = ComputeAxis(disciplined, chaotic)
        };

        DeriveTags(ref snapshot);
        return snapshot;
    }

    private static void CountFeatureBuckets(
        string[] selectedFeatureIds,
        ProductFeatureDefinition[] availableFeatures,
        int currentGen,
        out int expectedCount,
        out int selectedExpectedCount,
        out int selectedEmergingCount,
        out int selectedFoundationalCount)
    {
        expectedCount = 0;
        selectedExpectedCount = 0;
        selectedEmergingCount = 0;
        selectedFoundationalCount = 0;

        if (availableFeatures == null) return;

        int availCount = availableFeatures.Length;
        for (int i = 0; i < availCount; i++) {
            var feat = availableFeatures[i];
            if (feat == null) continue;

            var stage = FeatureDemandHelper.GetDemandStage(
                currentGen, feat.demandIntroductionGen, feat.demandMaturitySpeed, feat.isFoundational, 0f);

            bool isExpected = feat.isFoundational || stage == FeatureDemandStage.Standard;
            if (isExpected) expectedCount++;

            bool isSelected = IsSelected(selectedFeatureIds, feat.featureId);
            if (!isSelected) continue;

            if (isExpected) selectedExpectedCount++;
            if (stage == FeatureDemandStage.Emerging || stage == FeatureDemandStage.Growing)
                selectedEmergingCount++;
            if (feat.isFoundational) selectedFoundationalCount++;
        }
    }

    private static bool IsSelected(string[] selectedFeatureIds, string featureId)
    {
        if (selectedFeatureIds == null || featureId == null) return false;
        int count = selectedFeatureIds.Length;
        for (int i = 0; i < count; i++)
            if (selectedFeatureIds[i] == featureId) return true;
        return false;
    }

    private static int CountCompatibleOnMarketPlatforms(ProductTemplateDefinition template, ProductState productState)
    {
        if (template.validTargetPlatforms == null || template.validTargetPlatforms.Length == 0)
            return 0;
        if (productState?.shippedProducts == null)
            return 0;

        int count = 0;
        foreach (var kvp in productState.shippedProducts) {
            var p = kvp.Value;
            if (!p.IsOnMarket) continue;
            bool valid = false;
            for (int i = 0; i < template.validTargetPlatforms.Length; i++) {
                if (p.Category == template.validTargetPlatforms[i]) { valid = true; break; }
            }
            if (valid) count++;
        }
        return count;
    }

    private static float ComputeAvgPlatformCeiling(ProductId[] platformIds, PlatformSystem platformSystem, bool isPlayer)
    {
        if (platformSystem == null || platformIds == null || platformIds.Length == 0)
            return 65f;
        return ComputeAvgPlatformCeilingFromIds(platformIds, platformSystem, isPlayer);
    }

    private static float ComputeAvgPlatformCeilingFromIds(ProductId[] platformIds, PlatformSystem platformSystem, bool isPlayerOwned)
    {
        if (platformSystem == null || platformIds == null || platformIds.Length == 0)
            return 65f;
        float total = 0f;
        int count = platformIds.Length;
        for (int i = 0; i < count; i++)
            total += platformSystem.GetCeiling(platformIds[i], isPlayerOwned);
        return total / count;
    }

    private static float ComputeAvgToolCeiling(ProductId[] toolIds, ProductState productState)
    {
        if (toolIds == null || toolIds.Length == 0 || productState?.shippedProducts == null)
            return 65f;
        float total = 0f;
        int count = 0;
        for (int i = 0; i < toolIds.Length; i++) {
            if (!productState.shippedProducts.TryGetValue(toolIds[i], out var tool)) continue;
            total += tool.OverallQuality;
            count++;
        }
        return count > 0 ? total / count : 65f;
    }

    private static void DeriveTags(ref ProductIdentitySnapshot snapshot)
    {
        sbyte[] values = new sbyte[5] {
            snapshot.PricePositioning,
            snapshot.InnovationRisk,
            snapshot.AudienceBreadth,
            snapshot.FeatureScope,
            snapshot.ProductionDiscipline
        };

        int first = 0, second = 1, third = 2;
        for (int i = 0; i < 5; i++) {
            int absI = values[i] < 0 ? -values[i] : values[i];
            int absFirst = values[first] < 0 ? -values[first] : values[first];
            int absSecond = values[second] < 0 ? -values[second] : values[second];
            int absThird = values[third] < 0 ? -values[third] : values[third];

            if (absI > absFirst) { third = second; second = first; first = i; }
            else if (i != first && absI > absSecond) { third = second; second = i; }
            else if (i != first && i != second && absI > absThird) { third = i; }
        }

        snapshot.PrimaryTag = AxisToTag(first, values[first]);
        snapshot.SecondaryTag = AxisToTag(second, values[second]);
        snapshot.TertiaryTag = AxisToTag(third, values[third]);
    }

    private static ProductIdentityTag AxisToTag(int axisIndex, sbyte value)
    {
        if (value >= 60) {
            switch (axisIndex) {
                case 0: return ProductIdentityTag.Premium;
                case 1: return ProductIdentityTag.Experimental;
                case 2: return ProductIdentityTag.Broad;
                case 3: return ProductIdentityTag.FeatureHeavy;
                case 4: return ProductIdentityTag.Disciplined;
            }
        } else if (value >= 25) {
            switch (axisIndex) {
                case 0: return ProductIdentityTag.Premium;
                case 1: return ProductIdentityTag.Experimental;
                case 2: return ProductIdentityTag.Broad;
                case 3: return ProductIdentityTag.FeatureHeavy;
                case 4: return ProductIdentityTag.Disciplined;
            }
        } else if (value <= -60) {
            switch (axisIndex) {
                case 0: return ProductIdentityTag.Accessible;
                case 1: return ProductIdentityTag.Safe;
                case 2: return ProductIdentityTag.Specialist;
                case 3: return ProductIdentityTag.Refined;
                case 4: return ProductIdentityTag.Chaotic;
            }
        } else if (value <= -25) {
            switch (axisIndex) {
                case 0: return ProductIdentityTag.Accessible;
                case 1: return ProductIdentityTag.Safe;
                case 2: return ProductIdentityTag.Specialist;
                case 3: return ProductIdentityTag.Refined;
                case 4: return ProductIdentityTag.Chaotic;
            }
        } else {
            switch (axisIndex) {
                case 0: return ProductIdentityTag.Standard;
                case 1: return ProductIdentityTag.Balanced;
                case 2: return ProductIdentityTag.General;
                case 3: return ProductIdentityTag.Balanced;
                case 4: return ProductIdentityTag.Flexible;
            }
        }
        return ProductIdentityTag.None;
    }

    private static sbyte ComputeAxis(float positive, float negative)
    {
        return ClampToSignedByte((int)Math.Round((positive - negative) * 100f));
    }

    private static float Remap01(float value, float low, float high)
    {
        if (high <= low) return 0f;
        return Clamp01((value - low) / (high - low));
    }

    private static float Clamp01(float v)
    {
        return v < 0f ? 0f : v > 1f ? 1f : v;
    }

    private static sbyte ClampToSignedByte(int v)
    {
        return (sbyte)(v < -100 ? -100 : v > 100 ? 100 : v);
    }
}
