using System.Collections.Generic;

public static class CandidateReportGenerator
{
    // Builds a CandidateReport from current candidate data and role profile.
    // Call at generation time and again after each interview stage completes.
    public static CandidateReport Generate(CandidateData candidate, RoleProfileTable roleProfileTable)
    {
        var report = new CandidateReport();

        if (candidate == null)
        {
            report.SummaryLabel = "No data available";
            report.OverallConfidence = ConfidenceLevel.Unknown;
            return report;
        }

        RoleProfileDefinition profile = roleProfileTable?.Get(candidate.Role);

        // ── Strengths: top skills relative to role profile ──────────────────
        int skillCount = SkillIdHelper.SkillCount;
        int strengthsAdded = 0;
        // Find top 3 primary/secondary skills
        for (int pass = 0; pass < 2 && strengthsAdded < 3; pass++)
        {
            RoleWeightBand targetBand = pass == 0 ? RoleWeightBand.Primary : RoleWeightBand.Secondary;
            for (int i = 0; i < skillCount && strengthsAdded < 3; i++)
            {
                if (profile == null) break;
                if (profile.SkillBands[i] != targetBand) continue;
                int val = candidate.Stats.GetSkill((SkillId)i);
                if (val >= 10)
                {
                    string label = val >= 15 ? "Exceptional" : val >= 12 ? "Strong" : "Good";
                    report.Strengths.Add($"{label} {SkillIdHelper.GetName((SkillId)i)} estimate");
                    strengthsAdded++;
                }
            }
        }

        // Strengths: visible attributes above 14
        int attrCount = VisibleAttributeHelper.AttributeCount;
        for (int i = 0; i < attrCount; i++)
        {
            int val = candidate.Stats.GetVisibleAttribute((VisibleAttributeId)i);
            if (val >= 14)
                report.Strengths.Add($"High {VisibleAttributeHelper.GetName((VisibleAttributeId)i)}");
        }

        // ── Concerns: hidden attribute signals ──────────────────────────────
        int ambition     = candidate.Stats.GetHiddenAttribute(HiddenAttributeId.Ambition);
        int loyalty      = candidate.Stats.GetHiddenAttribute(HiddenAttributeId.Loyalty);
        int ego          = candidate.Stats.GetHiddenAttribute(HiddenAttributeId.Ego);
        int consistency  = candidate.Stats.GetHiddenAttribute(HiddenAttributeId.Consistency);

        if (ambition >= 14)  report.Concerns.Add("Ambition appears High");
        if (ego >= 14)       report.Concerns.Add("Ego may be a concern");
        if (consistency <= 5) report.Concerns.Add("Consistency appears Low");
        if (loyalty <= 5)    report.Concerns.Add("Loyalty signals are weak");

        // ── Risk flags ───────────────────────────────────────────────────────
        if (ambition >= 14 && loyalty <= 7)
            report.RiskFlags.Add("High ambition combined with low loyalty — flight risk");
        if (ego >= 16)
            report.RiskFlags.Add("Very high ego — potential team friction");
        if (consistency <= 4)
            report.RiskFlags.Add("Low consistency — unreliable performance");
        if (candidate.CompetitorInterest)
            report.RiskFlags.Add("Competitor interest detected — act quickly");

        // Expiry urgency flag (if expiry is set and soon)
        if (candidate.ExpiryTick > 0 && candidate.ExpiryTick != int.MaxValue)
            report.RiskFlags.Add("Limited availability window");

        // ── Recommended roles ────────────────────────────────────────────────
        if (candidate.ProjectedRoleFits != null)
        {
            int fitCount = candidate.ProjectedRoleFits.Count;
            for (int i = 0; i < fitCount; i++)
                report.RecommendedRoles.Add(candidate.ProjectedRoleFits[i]);
        }
        if (!report.RecommendedRoles.Contains(candidate.Role))
            report.RecommendedRoles.Insert(0, candidate.Role);

        // ── Salary notes ─────────────────────────────────────────────────────
        var confidence = candidate.Confidence;
        if (confidence != null)
        {
            switch (confidence.SalaryConfidence)
            {
                case ConfidenceLevel.Unknown:
                    report.SalaryNotes.Add("Salary demand unknown — interview required");
                    break;
                case ConfidenceLevel.Low:
                    report.SalaryNotes.Add($"Estimated salary ~{candidate.SalaryEstimateMin:N0}–{candidate.SalaryEstimateMax:N0} (wide range)");
                    break;
                case ConfidenceLevel.Medium:
                    report.SalaryNotes.Add($"Estimated salary ~{candidate.SalaryEstimateMin:N0}–{candidate.SalaryEstimateMax:N0}");
                    break;
                case ConfidenceLevel.High:
                case ConfidenceLevel.Confirmed:
                    report.SalaryNotes.Add($"Salary demand confirmed: {candidate.SalaryDemandActual:N0}");
                    break;
            }
        }

        // ── Team fit notes ───────────────────────────────────────────────────
        int mentoring    = candidate.Stats.GetHiddenAttribute(HiddenAttributeId.Mentoring);
        int pressureTol  = candidate.Stats.GetHiddenAttribute(HiddenAttributeId.PressureTolerance);
        int learningRate = candidate.Stats.GetHiddenAttribute(HiddenAttributeId.LearningRate);

        if (mentoring >= 14) report.TeamFitNotes.Add("Strong mentor potential");
        if (pressureTol >= 14) report.TeamFitNotes.Add("Handles pressure well");
        if (learningRate >= 14) report.TeamFitNotes.Add("Fast learner");
        if (candidate.Archetype == CandidateArchetype.DifficultStar)
            report.TeamFitNotes.Add("May require careful team placement");

        // ── Summary label ────────────────────────────────────────────────────
        report.SummaryLabel = BuildSummaryLabel(candidate);

        // ── Overall confidence ───────────────────────────────────────────────
        report.OverallConfidence = confidence != null ? confidence.SkillConfidence : ConfidenceLevel.Unknown;

        return report;
    }

    private static string BuildSummaryLabel(CandidateData candidate)
    {
        int ca = candidate.CurrentAbility;
        string tier = ca >= 160 ? "Elite"
            : ca >= 120 ? "Strong"
            : ca >= 80  ? "Promising"
            : ca >= 40  ? "Average"
            : "Junior";

        string roleLabel = RoleIdHelper.GetName(candidate.Role);
        string archLabel = GetArchetypeLabel(candidate.Archetype);
        return $"{tier} {roleLabel} ({archLabel})";
    }

    private static string GetArchetypeLabel(CandidateArchetype archetype)
    {
        switch (archetype)
        {
            case CandidateArchetype.Specialist:        return "Specialist";
            case CandidateArchetype.Generalist:        return "Generalist";
            case CandidateArchetype.RawTalent:         return "Raw Talent";
            case CandidateArchetype.DifficultStar:     return "Difficult Star";
            case CandidateArchetype.ReliableWorker:    return "Reliable";
            case CandidateArchetype.CreativeRisk:      return "Creative Risk";
            case CandidateArchetype.Mentor:            return "Mentor";
            case CandidateArchetype.PressurePlayer:    return "Pressure Player";
            case CandidateArchetype.CommercialClimber: return "Commercial";
            case CandidateArchetype.StableOperator:    return "Stable";
            default:                                   return "Unknown";
        }
    }
}
