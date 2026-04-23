using System;

public static class MarketReadResolver
{
#if UNITY_EDITOR
    public struct CandidateReadDebug
    {
        public MarketReadType Type;
        public int AbsStrength;
        public byte StrengthTier;
        public byte Confidence;
        public bool Suppressed;
        public bool Visible;
    }

    public struct MarketReadDebugData
    {
        public CandidateReadDebug Candidate0;
        public CandidateReadDebug Candidate1;
        public CandidateReadDebug Candidate2;
        public CandidateReadDebug Candidate3;
        public CandidateReadDebug Candidate4;
        public CandidateReadDebug Candidate5;
        public CandidateReadDebug Candidate6;
        public CandidateReadDebug Candidate7;
        public CandidateReadDebug Candidate8;
        public CandidateReadDebug Candidate9;
        public MarketReadPanelDisplay FinalDisplay;
        public MarketReadDelta LastDelta;
    }
#endif

    public struct MarketReadContext
    {
        public float PriceVsNormPct;
        public float CoverageRatio;
        public int PlatformCount;
        public int TotalAvailablePlatforms;
        public float AvgToolQuality;
        public int SelectedFeatureCount;
        public int TrendingFeatureCount;
        public int ExpectedFeaturesSkipped;
        public float SchedulePressure;
        public int PivotCount;
        public int DelayCount;
        public bool HasPrice;
        public bool HasFeatures;
        public bool HasPlatforms;
        public bool HasReleaseDate;
    }

    private struct CandidateRead
    {
        public MarketReadType Type;
        public int AbsStrength;
        public byte StrengthTier;
        public byte Confidence;
        public bool Suppressed;
    }

    private const int CandidateCount = 10;
    private const int ThresholdMild = 25;
    private const int ThresholdModerate = 50;
    private const int ThresholdStrong = 70;

    public static void Resolve(in ProductIdentitySnapshot snapshot, in MarketReadContext context, ref MarketReadPanelDisplay display)
    {
        Span<CandidateRead> candidates = stackalloc CandidateRead[CandidateCount];
        ScoreCandidates(in snapshot, in context, candidates);
        SuppressOppositePairs(candidates);
        ApplyConfidenceGating(candidates);
        SelectTopThree(candidates, ref display);

        if (!display.HasAnyReads)
            display.EmptyStateText = "Market read will become clearer once price, features, and release timing are set.";
        else
            display.EmptyStateText = "";

        if (display.CardCount > 0) BuildCardDisplay(in candidates[FindSlot(candidates, display.Card0.Type)], in context, ref display.Card0);
        if (display.CardCount > 1) BuildCardDisplay(in candidates[FindSlot(candidates, display.Card1.Type)], in context, ref display.Card1);
        if (display.CardCount > 2) BuildCardDisplay(in candidates[FindSlot(candidates, display.Card2.Type)], in context, ref display.Card2);
    }

#if UNITY_EDITOR
    public static void Resolve(in ProductIdentitySnapshot snapshot, in MarketReadContext context, ref MarketReadPanelDisplay display, out MarketReadDebugData debugData)
    {
        Span<CandidateRead> candidates = stackalloc CandidateRead[CandidateCount];
        ScoreCandidates(in snapshot, in context, candidates);
        SuppressOppositePairs(candidates);
        ApplyConfidenceGating(candidates);
        SelectTopThree(candidates, ref display);

        if (!display.HasAnyReads)
            display.EmptyStateText = "Market read will become clearer once price, features, and release timing are set.";
        else
            display.EmptyStateText = "";

        if (display.CardCount > 0) BuildCardDisplay(in candidates[FindSlot(candidates, display.Card0.Type)], in context, ref display.Card0);
        if (display.CardCount > 1) BuildCardDisplay(in candidates[FindSlot(candidates, display.Card1.Type)], in context, ref display.Card1);
        if (display.CardCount > 2) BuildCardDisplay(in candidates[FindSlot(candidates, display.Card2.Type)], in context, ref display.Card2);

        debugData = new MarketReadDebugData { FinalDisplay = display };
        debugData.Candidate0 = BuildCandidateDebug(candidates[0], ref display);
        debugData.Candidate1 = BuildCandidateDebug(candidates[1], ref display);
        debugData.Candidate2 = BuildCandidateDebug(candidates[2], ref display);
        debugData.Candidate3 = BuildCandidateDebug(candidates[3], ref display);
        debugData.Candidate4 = BuildCandidateDebug(candidates[4], ref display);
        debugData.Candidate5 = BuildCandidateDebug(candidates[5], ref display);
        debugData.Candidate6 = BuildCandidateDebug(candidates[6], ref display);
        debugData.Candidate7 = BuildCandidateDebug(candidates[7], ref display);
        debugData.Candidate8 = BuildCandidateDebug(candidates[8], ref display);
        debugData.Candidate9 = BuildCandidateDebug(candidates[9], ref display);
    }

    private static CandidateReadDebug BuildCandidateDebug(in CandidateRead c, ref MarketReadPanelDisplay display)
    {
        bool visible = !c.Suppressed && AlreadySelected(ref display, c.Type);
        return new CandidateReadDebug
        {
            Type = c.Type,
            AbsStrength = c.AbsStrength,
            StrengthTier = c.StrengthTier,
            Confidence = c.Confidence,
            Suppressed = c.Suppressed,
            Visible = visible
        };
    }
#endif

    public static MarketReadDelta ComparePanels(in MarketReadPanelDisplay previous, in MarketReadPanelDisplay current)
    {
        if (!previous.HasAnyReads)
            return new MarketReadDelta { HasDelta = false };

        int bestAbsChange = 0;
        string bestMessage = null;

        CheckCardShift(in previous, in current, previous.Card0, ref bestAbsChange, ref bestMessage);
        CheckCardShift(in previous, in current, previous.Card1, ref bestAbsChange, ref bestMessage);
        CheckCardShift(in previous, in current, previous.Card2, ref bestAbsChange, ref bestMessage);
        CheckCardShift(in previous, in current, current.Card0, ref bestAbsChange, ref bestMessage);
        CheckCardShift(in previous, in current, current.Card1, ref bestAbsChange, ref bestMessage);
        CheckCardShift(in previous, in current, current.Card2, ref bestAbsChange, ref bestMessage);

        if (bestMessage == null)
            return new MarketReadDelta { HasDelta = false };

        return new MarketReadDelta { HasDelta = true, Message = bestMessage };
    }

    private static void CheckCardShift(in MarketReadPanelDisplay previous, in MarketReadPanelDisplay current,
        MarketReadCardDisplay card, ref int bestAbsChange, ref string bestMessage)
    {
        if (!card.IsVisible) return;
        MarketReadType type = card.Type;

        byte prevTier = FindTierInDisplay(in previous, type);
        byte currTier = FindTierInDisplay(in current, type);

        int change = currTier - prevTier;
        if (change == 0) return;

        int absChange = change < 0 ? -change : change;
        if (absChange <= bestAbsChange) return;

        string msg = BuildDeltaMessage(type, change);
        if (msg == null) return;

        bestAbsChange = absChange;
        bestMessage = msg;
    }

    private static byte FindTierInDisplay(in MarketReadPanelDisplay display, MarketReadType type)
    {
        if (display.CardCount > 0 && display.Card0.Type == type && display.Card0.IsVisible) return display.Card0.StrengthTier;
        if (display.CardCount > 1 && display.Card1.Type == type && display.Card1.IsVisible) return display.Card1.StrengthTier;
        if (display.CardCount > 2 && display.Card2.Type == type && display.Card2.IsVisible) return display.Card2.StrengthTier;
        return 0;
    }

    private static string BuildDeltaMessage(MarketReadType type, int tierDelta)
    {
        bool up = tierDelta > 0;
        switch (type)
        {
            case MarketReadType.PremiumPricing:            return up ? "More premium" : "Less premium";
            case MarketReadType.ValuePricing:              return up ? "More value-oriented" : "Less value-oriented";
            case MarketReadType.BroadAudienceAppeal:       return up ? "More broad" : "Less broad";
            case MarketReadType.SpecialistPositioning:     return up ? "More focused" : "Less focused";
            case MarketReadType.SafeFeatureStrategy:       return up ? "More stable" : "Less stable";
            case MarketReadType.ExperimentalFeatureStrategy: return up ? "More risky" : "Less risky";
            case MarketReadType.FocusedFeatureSet:         return up ? "More focused" : "Less focused";
            case MarketReadType.HeavyFeatureScope:         return up ? "More feature-heavy" : "Less feature-heavy";
            case MarketReadType.DisciplinedBuild:          return up ? "More stable" : "Less stable";
            case MarketReadType.UnstableProduction:        return up ? "More risky" : "Less risky";
            default:                                       return null;
        }
    }

    private static void ScoreCandidates(in ProductIdentitySnapshot snapshot, in MarketReadContext context, Span<CandidateRead> candidates)
    {
        int idx = 0;

        int priceAbs = Math.Abs(snapshot.PricePositioning);
        candidates[idx++] = MakeCandidate(MarketReadType.PremiumPricing, snapshot.PricePositioning > 0 ? priceAbs : 0, in context);
        candidates[idx++] = MakeCandidate(MarketReadType.ValuePricing, snapshot.PricePositioning < 0 ? priceAbs : 0, in context);

        int audienceAbs = Math.Abs(snapshot.AudienceBreadth);
        candidates[idx++] = MakeCandidate(MarketReadType.BroadAudienceAppeal, snapshot.AudienceBreadth > 0 ? audienceAbs : 0, in context);
        candidates[idx++] = MakeCandidate(MarketReadType.SpecialistPositioning, snapshot.AudienceBreadth < 0 ? audienceAbs : 0, in context);

        int riskAbs = Math.Abs(snapshot.InnovationRisk);
        candidates[idx++] = MakeCandidate(MarketReadType.SafeFeatureStrategy, snapshot.InnovationRisk < 0 ? riskAbs : 0, in context);
        candidates[idx++] = MakeCandidate(MarketReadType.ExperimentalFeatureStrategy, snapshot.InnovationRisk > 0 ? riskAbs : 0, in context);

        int scopeAbs = Math.Abs(snapshot.FeatureScope);
        candidates[idx++] = MakeCandidate(MarketReadType.FocusedFeatureSet, snapshot.FeatureScope < 0 ? scopeAbs : 0, in context);
        candidates[idx++] = MakeCandidate(MarketReadType.HeavyFeatureScope, snapshot.FeatureScope > 0 ? scopeAbs : 0, in context);

        int prodAbs = Math.Abs(snapshot.ProductionDiscipline);
        candidates[idx++] = MakeCandidate(MarketReadType.DisciplinedBuild, snapshot.ProductionDiscipline > 0 ? prodAbs : 0, in context);
        candidates[idx++] = MakeCandidate(MarketReadType.UnstableProduction, snapshot.ProductionDiscipline < 0 ? prodAbs : 0, in context);
    }

    private static CandidateRead MakeCandidate(MarketReadType type, int absStrength, in MarketReadContext context)
    {
        byte tier = 0;
        if (absStrength >= ThresholdStrong)   tier = 3;
        else if (absStrength >= ThresholdModerate) tier = 2;
        else if (absStrength >= ThresholdMild)     tier = 1;

        return new CandidateRead
        {
            Type = type,
            AbsStrength = absStrength,
            StrengthTier = tier,
            Confidence = ComputeConfidence(type, in context),
            Suppressed = tier == 0
        };
    }

    private static void SuppressOppositePairs(Span<CandidateRead> candidates)
    {
        SuppressPair(candidates, 0, 1);
        SuppressPair(candidates, 2, 3);
        SuppressPair(candidates, 4, 5);
        SuppressPair(candidates, 6, 7);
        SuppressPair(candidates, 8, 9);
    }

    private static void SuppressPair(Span<CandidateRead> candidates, int a, int b)
    {
        if (candidates[a].Suppressed || candidates[b].Suppressed) return;
        if (candidates[a].AbsStrength > candidates[b].AbsStrength)
            candidates[b] = Suppressed(candidates[b]);
        else if (candidates[b].AbsStrength > candidates[a].AbsStrength)
            candidates[a] = Suppressed(candidates[a]);
        else
        {
            candidates[a] = Suppressed(candidates[a]);
            candidates[b] = Suppressed(candidates[b]);
        }
    }

    private static void ApplyConfidenceGating(Span<CandidateRead> candidates)
    {
        for (int i = 0; i < CandidateCount; i++)
        {
            if (candidates[i].Suppressed) continue;
            var type = candidates[i].Type;
            byte conf = candidates[i].Confidence;
            bool isProductionRead = type == MarketReadType.DisciplinedBuild || type == MarketReadType.UnstableProduction;
            if (isProductionRead && conf == 0)
                candidates[i] = Suppressed(candidates[i]);
        }
    }

    private static void SelectTopThree(Span<CandidateRead> candidates, ref MarketReadPanelDisplay display)
    {
        display.CardCount = 0;
        display.HasAnyReads = false;
        display.Card0 = default;
        display.Card1 = default;
        display.Card2 = default;

        for (int pass = 0; pass < 3; pass++)
        {
            int bestIdx = -1;
            int bestStr = 0;
            for (int i = 0; i < CandidateCount; i++)
            {
                if (candidates[i].Suppressed) continue;
                if (AlreadySelected(ref display, candidates[i].Type)) continue;
                if (candidates[i].AbsStrength >= ThresholdMild && candidates[i].AbsStrength > bestStr)
                {
                    bestStr = candidates[i].AbsStrength;
                    bestIdx = i;
                }
            }
            if (bestIdx < 0) break;

            ref var slot = ref GetSlot(ref display, display.CardCount);
            slot.IsVisible = true;
            slot.Type = candidates[bestIdx].Type;
            slot.StrengthTier = candidates[bestIdx].StrengthTier;
            slot.Confidence = candidates[bestIdx].Confidence;
            display.CardCount++;
            display.HasAnyReads = true;
        }
    }

    private static bool AlreadySelected(ref MarketReadPanelDisplay display, MarketReadType type)
    {
        if (display.CardCount > 0 && display.Card0.Type == type) return true;
        if (display.CardCount > 1 && display.Card1.Type == type) return true;
        if (display.CardCount > 2 && display.Card2.Type == type) return true;
        return false;
    }

    private static ref MarketReadCardDisplay GetSlot(ref MarketReadPanelDisplay display, int index)
    {
        if (index == 0) return ref display.Card0;
        if (index == 1) return ref display.Card1;
        return ref display.Card2;
    }

    private static int FindSlot(Span<CandidateRead> candidates, MarketReadType type)
    {
        for (int i = 0; i < CandidateCount; i++)
            if (candidates[i].Type == type) return i;
        return 0;
    }

    private static CandidateRead Suppressed(CandidateRead c)
    {
        c.Suppressed = true;
        return c;
    }

    private static void BuildCardDisplay(in CandidateRead candidate, in MarketReadContext context, ref MarketReadCardDisplay card)
    {
        card.Title = GetTitle(candidate.Type);
        card.StrengthText = GetStrengthText(candidate.StrengthTier);
        card.Because = BuildBecauseLine(candidate.Type, in context);
        card.Implication = GetImplication(candidate.Type);
        card.Tooltip = BuildTooltip(candidate.Type, in context);
    }

    private static string BuildBecauseLine(MarketReadType type, in MarketReadContext context)
    {
        switch (type)
        {
            case MarketReadType.PremiumPricing:
            {
                float pct = context.PriceVsNormPct;
                string priceStr = pct >= 0f ? "+" + pct.ToString("F0") + "% above norm" : pct.ToString("F0") + "% below norm";
                if (context.PlatformCount <= 1)
                    return "price is " + priceStr + " and platform reach is narrow";
                return "price is " + priceStr + " and tool quality is elevated";
            }
            case MarketReadType.ValuePricing:
            {
                float pct = context.PriceVsNormPct;
                string priceStr = "price is " + Math.Abs(pct).ToString("F0") + "% below norm";
                if (context.CoverageRatio >= 0.6f)
                    return priceStr + " and expected features are mostly covered";
                return priceStr + " and platform reach is broad";
            }
            case MarketReadType.BroadAudienceAppeal:
            {
                string reason = context.PlatformCount > 2 ? context.PlatformCount + " target platforms selected" : "expected features are mostly covered";
                return reason + " and price is near or below norm";
            }
            case MarketReadType.SpecialistPositioning:
            {
                string reason = context.PlatformCount <= 1 ? "platform target is narrow" : "price is above norm";
                if (context.TrendingFeatureCount > 0)
                    return reason + " and feature mix leans toward standout choices";
                return reason + " and positioning signals a focused audience";
            }
            case MarketReadType.SafeFeatureStrategy:
            {
                string trending = context.TrendingFeatureCount == 0 ? "no trending features selected" : "low reliance on trending features";
                return "mostly expected features selected and " + trending;
            }
            case MarketReadType.ExperimentalFeatureStrategy:
            {
                if (context.TrendingFeatureCount > 0 && context.ExpectedFeaturesSkipped > 0)
                    return context.TrendingFeatureCount + " trending features selected, " + context.ExpectedFeaturesSkipped + " expected skipped";
                if (context.TrendingFeatureCount > 0)
                    return context.TrendingFeatureCount + " trending features selected with high scope";
                return "expected features are being skipped and scope is ambitious";
            }
            case MarketReadType.FocusedFeatureSet:
            {
                int feats = context.SelectedFeatureCount;
                return feats == 1 ? "1 feature selected with room for polish" : feats + " features selected with schedule slack";
            }
            case MarketReadType.HeavyFeatureScope:
            {
                string pressure = context.SchedulePressure >= 0.6f ? " and release plan is tight" : "";
                return context.SelectedFeatureCount + " features selected" + pressure;
            }
            case MarketReadType.DisciplinedBuild:
            {
                string slack = context.SchedulePressure < 0.4f ? "release plan leaves room for polish" : "expected features mostly covered";
                return slack + " and setup looks stable";
            }
            case MarketReadType.UnstableProduction:
            {
                if (context.PivotCount > 0 || context.DelayCount > 0)
                    return "release plan is tight and pivots/delays are accumulating";
                return "scope is high and release plan is tight for current ambition";
            }
            default:
                return "";
        }
    }

    private static TooltipData BuildTooltip(MarketReadType type, in MarketReadContext context)
    {
        string title = GetTitle(type);
        string body = GetTooltipBody(type);
        string shiftHint = GetShiftHint(type);
        var stats = BuildStatRows(type, in context, shiftHint);
        return new TooltipData { Title = title, Body = body, Stats = stats };
    }

    private static TooltipStatRow[] BuildStatRows(MarketReadType type, in MarketReadContext context, string shiftHint)
    {
        var rows = new TooltipStatRow[4];
        int idx = 0;

        switch (type)
        {
            case MarketReadType.PremiumPricing:
                rows[idx++] = StatRow("Price vs norm", FormatPct(context.PriceVsNormPct));
                rows[idx++] = StatRow("Tool quality", FormatQuality(context.AvgToolQuality));
                rows[idx++] = StatRow("Platform reach", FormatPlatforms(context.PlatformCount, context.TotalAvailablePlatforms));
                break;
            case MarketReadType.ValuePricing:
                rows[idx++] = StatRow("Price vs norm", FormatPct(context.PriceVsNormPct));
                rows[idx++] = StatRow("Expected features", FormatCoverage(context.CoverageRatio));
                rows[idx++] = StatRow("Platform reach", FormatPlatforms(context.PlatformCount, context.TotalAvailablePlatforms));
                break;
            case MarketReadType.BroadAudienceAppeal:
                rows[idx++] = StatRow("Expected features", FormatCoverage(context.CoverageRatio));
                rows[idx++] = StatRow("Platform targets", context.PlatformCount.ToString());
                rows[idx++] = StatRow("Price position", FormatPct(context.PriceVsNormPct));
                break;
            case MarketReadType.SpecialistPositioning:
                rows[idx++] = StatRow("Platform reach", FormatPlatforms(context.PlatformCount, context.TotalAvailablePlatforms));
                rows[idx++] = StatRow("Price position", FormatPct(context.PriceVsNormPct));
                rows[idx++] = StatRow("Trending features", context.TrendingFeatureCount.ToString());
                break;
            case MarketReadType.SafeFeatureStrategy:
                rows[idx++] = StatRow("Expected features", FormatCoverage(context.CoverageRatio));
                rows[idx++] = StatRow("Trending features", context.TrendingFeatureCount.ToString());
                rows[idx++] = StatRow("Scope pressure", FormatPressure(context.SchedulePressure));
                break;
            case MarketReadType.ExperimentalFeatureStrategy:
                rows[idx++] = StatRow("Trending features", context.TrendingFeatureCount.ToString());
                rows[idx++] = StatRow("Expected features skipped", context.ExpectedFeaturesSkipped.ToString());
                rows[idx++] = StatRow("Scope pressure", FormatPressure(context.SchedulePressure));
                break;
            case MarketReadType.FocusedFeatureSet:
                rows[idx++] = StatRow("Selected features", context.SelectedFeatureCount.ToString());
                rows[idx++] = StatRow("Coverage", FormatCoverage(context.CoverageRatio));
                rows[idx++] = StatRow("Schedule slack", FormatSlack(context.SchedulePressure));
                break;
            case MarketReadType.HeavyFeatureScope:
                rows[idx++] = StatRow("Selected features", context.SelectedFeatureCount.ToString());
                rows[idx++] = StatRow("Coverage", FormatCoverage(context.CoverageRatio));
                rows[idx++] = StatRow("Schedule pressure", FormatPressure(context.SchedulePressure));
                break;
            case MarketReadType.DisciplinedBuild:
                rows[idx++] = StatRow("Schedule slack", FormatSlack(context.SchedulePressure));
                rows[idx++] = StatRow("Expected features", FormatCoverage(context.CoverageRatio));
                rows[idx++] = StatRow("Scope stability", context.SelectedFeatureCount <= 4 ? "Controlled" : "Moderate");
                break;
            case MarketReadType.UnstableProduction:
                rows[idx++] = StatRow("Release pressure", FormatPressure(context.SchedulePressure));
                rows[idx++] = StatRow("Pivots / delays", (context.PivotCount + context.DelayCount).ToString());
                rows[idx++] = StatRow("Scope pressure", context.SelectedFeatureCount > 6 ? "High" : "Moderate");
                break;
        }

        rows[idx++] = StatRow("To shift this", shiftHint);

        var result = new TooltipStatRow[idx];
        for (int i = 0; i < idx; i++) result[i] = rows[i];
        return result;
    }

    private static string GetTooltipBody(MarketReadType type)
    {
        switch (type)
        {
            case MarketReadType.PremiumPricing:            return "The market sees this as a higher-priced product with stronger polish expectations.";
            case MarketReadType.ValuePricing:              return "The market sees this as a value-priced product with less premium upside but lower value risk.";
            case MarketReadType.BroadAudienceAppeal:       return "The current setup reads as something intended for a wider audience.";
            case MarketReadType.SpecialistPositioning:     return "The current setup reads as aimed at a narrower audience with higher upside if it lands.";
            case MarketReadType.SafeFeatureStrategy:       return "The feature plan reads as controlled and reliable rather than risky.";
            case MarketReadType.ExperimentalFeatureStrategy: return "The feature plan reads as more ambitious and novelty-led, which raises launch risk.";
            case MarketReadType.FocusedFeatureSet:         return "The product reads as concentrated and polish-friendly rather than broad and feature-packed.";
            case MarketReadType.HeavyFeatureScope:         return "The product reads as broad in scope, which increases delivery pressure.";
            case MarketReadType.DisciplinedBuild:          return "The production plan reads as controlled and more likely to support stable reviews.";
            case MarketReadType.UnstableProduction:        return "The production currently reads as under pressure from scope, timing, or change churn.";
            default:                                       return "";
        }
    }

    private static string GetShiftHint(MarketReadType type)
    {
        switch (type)
        {
            case MarketReadType.PremiumPricing:            return "lower price or improve tools";
            case MarketReadType.ValuePricing:              return "raise price or narrow target";
            case MarketReadType.BroadAudienceAppeal:       return "cover expected features, broaden platforms";
            case MarketReadType.SpecialistPositioning:     return "narrow platforms, add standout features";
            case MarketReadType.SafeFeatureStrategy:       return "add trending features";
            case MarketReadType.ExperimentalFeatureStrategy: return "use expected features, cut scope";
            case MarketReadType.FocusedFeatureSet:         return "add more features";
            case MarketReadType.HeavyFeatureScope:         return "cut features or extend schedule";
            case MarketReadType.DisciplinedBuild:          return "keep scope tight, avoid pivots";
            case MarketReadType.UnstableProduction:        return "cut scope or push release date";
            default:                                       return "";
        }
    }

    private static byte ComputeConfidence(MarketReadType type, in MarketReadContext context)
    {
        int score = 0;
        if (context.HasPrice) score++;
        if (context.HasFeatures) score++;
        if (context.HasPlatforms) score++;
        if (context.HasReleaseDate) score++;

        if (score >= 4) return 2;
        if (score >= 2) return 1;
        return 0;
    }

    private static string GetTitle(MarketReadType type)
    {
        switch (type)
        {
            case MarketReadType.PremiumPricing:            return "Premium pricing";
            case MarketReadType.ValuePricing:              return "Value pricing";
            case MarketReadType.BroadAudienceAppeal:       return "Broad audience appeal";
            case MarketReadType.SpecialistPositioning:     return "Specialist positioning";
            case MarketReadType.SafeFeatureStrategy:       return "Safe feature strategy";
            case MarketReadType.ExperimentalFeatureStrategy: return "Experimental feature strategy";
            case MarketReadType.FocusedFeatureSet:         return "Focused feature set";
            case MarketReadType.HeavyFeatureScope:         return "Heavy feature scope";
            case MarketReadType.DisciplinedBuild:          return "Disciplined build";
            case MarketReadType.UnstableProduction:        return "Unstable production";
            default:                                       return "";
        }
    }

    private static string GetImplication(MarketReadType type)
    {
        switch (type)
        {
            case MarketReadType.PremiumPricing:            return "Reviewers will expect stronger polish and stability.";
            case MarketReadType.ValuePricing:              return "Lower value risk, but weaker premium upside.";
            case MarketReadType.BroadAudienceAppeal:       return "Stronger launch ceiling if the basics are covered.";
            case MarketReadType.SpecialistPositioning:     return "Higher upside with the right audience, smaller margin for mistakes.";
            case MarketReadType.SafeFeatureStrategy:       return "More reliable launch, lower standout potential.";
            case MarketReadType.ExperimentalFeatureStrategy: return "Higher breakout upside, but rough-launch risk rises.";
            case MarketReadType.FocusedFeatureSet:         return "Better polish efficiency, lower feature \"wow\" factor.";
            case MarketReadType.HeavyFeatureScope:         return "Development and bug pressure rise quickly.";
            case MarketReadType.DisciplinedBuild:          return "Better chance of stable reviews and long-tail retention.";
            case MarketReadType.UnstableProduction:        return "Review risk rises unless scope, timing, or price is corrected.";
            default:                                       return "";
        }
    }

    private static string GetStrengthText(byte tier)
    {
        switch (tier)
        {
            case 1: return "Mild";
            case 2: return "Moderate";
            case 3: return "Strong";
            default: return "";
        }
    }

    private static TooltipStatRow StatRow(string label, string value)
        => new TooltipStatRow { Label = label, Value = value };

    private static string FormatPct(float pct)
    {
        if (pct > 0f)  return "+" + pct.ToString("F0") + "%";
        if (pct < 0f)  return pct.ToString("F0") + "%";
        return "At norm";
    }

    private static string FormatQuality(float quality)
    {
        if (quality >= 0.75f) return "High";
        if (quality >= 0.45f) return "Medium";
        return "Low";
    }

    private static string FormatPlatforms(int count, int total)
    {
        if (count == 0) return "None selected";
        if (total > 0 && count >= total) return "All platforms";
        if (count == 1) return "1 target";
        return count + " targets";
    }

    private static string FormatCoverage(float ratio)
    {
        if (ratio >= 0.80f) return "High";
        if (ratio >= 0.50f) return "Moderate";
        if (ratio > 0f)     return "Low";
        return "None";
    }

    private static string FormatPressure(float pressure)
    {
        if (pressure >= 0.70f) return "High";
        if (pressure >= 0.40f) return "Moderate";
        return "Low";
    }

    private static string FormatSlack(float pressure)
    {
        if (pressure <= 0.30f) return "High";
        if (pressure <= 0.60f) return "Moderate";
        return "Low";
    }
}
