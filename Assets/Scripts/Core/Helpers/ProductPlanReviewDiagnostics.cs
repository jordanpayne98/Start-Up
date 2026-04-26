using System;

public static class ProductPlanReviewDiagnostics
{
    private const float SeverityThreshold = 0.15f;
    private const int CandidateCount = 5;

    private struct CandidateCard
    {
        public ProductPlanReviewCardType Type;
        public ProductPlanReviewStatus Status;
        public float Severity;
        public bool Suppressed;
    }

    public static void Evaluate(in ProductPlanReviewContext context, ProductPlanReviewDisplay display)
    {
        display.Reset();

        Span<CandidateCard> candidates = stackalloc CandidateCard[CandidateCount];

        candidates[0] = new CandidateCard {
            Type = ProductPlanReviewCardType.ScopePressure,
            Status = ProductPlanReviewStatus.Risk,
            Severity = ScoreScopePressure(in context),
            Suppressed = !context.HasFeatures || context.ScheduleConfidenceLow && ScoreScopePressure(in context) < SeverityThreshold
        };

        candidates[1] = new CandidateCard {
            Type = ProductPlanReviewCardType.MissingExpectedFeatures,
            Status = ProductPlanReviewStatus.Risk,
            Severity = ScoreMissingExpected(in context),
            Suppressed = !context.HasFeatures
        };

        candidates[2] = new CandidateCard {
            Type = ProductPlanReviewCardType.PriceExpectationMismatch,
            Status = ProductPlanReviewStatus.Risk,
            Severity = ScorePriceMismatch(in context),
            Suppressed = !context.HasPrice
        };

        candidates[3] = new CandidateCard {
            Type = ProductPlanReviewCardType.PlatformBottleneck,
            Status = ProductPlanReviewStatus.Tradeoff,
            Severity = ScorePlatformBottleneck(in context),
            Suppressed = !context.HasPlatforms
        };

        candidates[4] = new CandidateCard {
            Type = ProductPlanReviewCardType.TightReleasePlan,
            Status = ProductPlanReviewStatus.Risk,
            Severity = ScoreTightRelease(in context),
            Suppressed = context.ScheduleConfidenceLow
        };

        if (context.ScheduleConfidenceLow)
        {
            candidates[0].Severity *= 0.5f;
            candidates[0].Suppressed = !context.HasFeatures || candidates[0].Severity < SeverityThreshold;
        }

        for (int i = 0; i < CandidateCount; i++)
        {
            if (candidates[i].Severity < SeverityThreshold)
                candidates[i].Suppressed = true;
        }

        SortCandidates(ref candidates);

        int filled = 0;
        for (int i = 0; i < CandidateCount && filled < 3; i++)
        {
            if (candidates[i].Suppressed) continue;
            var card = display.Cards[filled];
            card.IsVisible = true;
            card.Type = candidates[i].Type;
            card.Status = candidates[i].Status;
            PopulateCardCopy(candidates[i].Type, card);
            card.Tooltip = default;
            filled++;
        }

        display.CardCount = filled;
        display.HasCards = filled > 0;

        if (!display.HasCards)
            display.EmptyText = "No major planning issues detected. This product plan looks coherent.";
        else
            display.EmptyText = "";
    }

    private static void SortCandidates(ref Span<CandidateCard> candidates)
    {
        for (int i = 0; i < candidates.Length - 1; i++)
        {
            for (int j = i + 1; j < candidates.Length; j++)
            {
                if (ShouldSwap(in candidates[i], in candidates[j]))
                {
                    var tmp = candidates[i];
                    candidates[i] = candidates[j];
                    candidates[j] = tmp;
                }
            }
        }
    }

    private static bool ShouldSwap(in CandidateCard a, in CandidateCard b)
    {
        if (a.Suppressed && !b.Suppressed) return false;
        if (!a.Suppressed && b.Suppressed) return false;

        bool aIsRisk = a.Status == ProductPlanReviewStatus.Risk;
        bool bIsRisk = b.Status == ProductPlanReviewStatus.Risk;

        if (aIsRisk && !bIsRisk && b.Severity <= 0.8f) return false;
        if (!aIsRisk && bIsRisk && a.Severity <= 0.8f) return true;

        if (Math.Abs(a.Severity - b.Severity) > 0.01f)
            return b.Severity > a.Severity;

        return GetPriority(b.Type) < GetPriority(a.Type);
    }

    private static int GetPriority(ProductPlanReviewCardType type)
    {
        switch (type)
        {
            case ProductPlanReviewCardType.PriceExpectationMismatch:  return 0;
            case ProductPlanReviewCardType.ScopePressure:             return 1;
            case ProductPlanReviewCardType.MissingExpectedFeatures:   return 2;
            case ProductPlanReviewCardType.TightReleasePlan:          return 3;
            case ProductPlanReviewCardType.PlatformBottleneck:        return 4;
            default:                                                   return 5;
        }
    }

    private static float ScoreScopePressure(in ProductPlanReviewContext ctx)
    {
        if (!ctx.HasFeatures) return 0f;
        bool highScope = ctx.SelectedFeatureRatio >= 0.75f;
        bool tightSchedule = ctx.ScheduleRatio < 1.10f;
        if (!highScope || !tightSchedule) return 0f;
        float scopeExcess = (ctx.SelectedFeatureRatio - 0.75f) / 0.25f;
        float scheduleGap = (1.10f - ctx.ScheduleRatio) / 1.10f;
        return Math.Min(1f, (scopeExcess + scheduleGap) * 0.5f);
    }

    private static float ScoreMissingExpected(in ProductPlanReviewContext ctx)
    {
        if (!ctx.HasFeatures || ctx.ExpectedFeaturesTotal <= 0) return 0f;
        if (ctx.ExpectedFeatureCoverage >= 0.75f) return 0f;
        return 1f - ctx.ExpectedFeatureCoverage;
    }

    private static float ScorePriceMismatch(in ProductPlanReviewContext ctx)
    {
        if (!ctx.HasPrice) return 0f;
        if (ctx.PriceNorm < 1.20f) return 0f;
        bool qualityLow = ctx.PolishForecast < 0.65f || ctx.StabilityForecast < 0.65f;
        if (!qualityLow) return 0f;
        float priceDeviation = (ctx.PriceNorm - 1.20f) / 0.80f;
        return Math.Min(1f, priceDeviation);
    }

    private static float ScorePlatformBottleneck(in ProductPlanReviewContext ctx)
    {
        if (!ctx.HasPlatforms) return 0f;
        if (ctx.PlatformReach >= 0.40f) return 0f;
        return 1f - ctx.PlatformReach;
    }

    private static float ScoreTightRelease(in ProductPlanReviewContext ctx)
    {
        if (ctx.ScheduleConfidenceLow) return 0f;
        if (ctx.ScheduleRatio >= 0.85f) return 0f;
        return 1f - ctx.ScheduleRatio;
    }

    private static void PopulateCardCopy(ProductPlanReviewCardType type, ProductPlanReviewCardDisplay card)
    {
        switch (type)
        {
            case ProductPlanReviewCardType.ScopePressure:
                card.Title = "Scope Pressure";
                card.StatusText = "Risk";
                card.WhyText = "You're targeting a large feature set without enough schedule headroom to execute it well.";
                card.EffectText = "Rushed features lower quality scores and increase the chance of a delayed or rough launch.";
                card.ChangeText = "Cut lower-priority features, extend your release date, or assign a larger team.";
                break;
            case ProductPlanReviewCardType.MissingExpectedFeatures:
                card.Title = "Missing Expected Features";
                card.StatusText = "Risk";
                card.WhyText = "Players expect certain features for this product type, and you're skipping a significant portion of them.";
                card.EffectText = "Missing expected features reduces review scores and audience satisfaction.";
                card.ChangeText = "Add the foundational and standard-demand features for this product category.";
                break;
            case ProductPlanReviewCardType.PriceExpectationMismatch:
                card.Title = "Price Expectation Mismatch";
                card.StatusText = "Risk";
                card.WhyText = "Your price is well above the norm, but the current plan doesn't support premium quality signals.";
                card.EffectText = "Players will feel overcharged, leading to lower review scores and weaker sales.";
                card.ChangeText = "Lower the price, improve polish and stability output, or invest in better tooling.";
                break;
            case ProductPlanReviewCardType.PlatformBottleneck:
                card.Title = "Platform Bottleneck";
                card.StatusText = "Tradeoff";
                card.WhyText = "You're only targeting a small portion of compatible platforms.";
                card.EffectText = "Limits your addressable market, which constrains peak revenue potential.";
                card.ChangeText = "Add more platforms if your budget and team capacity allows — or accept the niche focus.";
                break;
            case ProductPlanReviewCardType.TightReleasePlan:
                card.Title = "Tight Release Plan";
                card.StatusText = "Risk";
                card.WhyText = "Your planned development window is significantly shorter than what this product typically requires.";
                card.EffectText = "Tight timelines increase crunch risk and can result in a lower-quality release.";
                card.ChangeText = "Push the release date out or reduce feature scope to give your team more breathing room.";
                break;
            default:
                card.Title = "";
                card.StatusText = "";
                card.WhyText = "";
                card.EffectText = "";
                card.ChangeText = "";
                break;
        }
    }
}
