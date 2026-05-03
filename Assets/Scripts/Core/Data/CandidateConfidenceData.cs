public class CandidateConfidenceData
{
    public ConfidenceLevel SkillConfidence;
    public ConfidenceLevel VisibleAttributeConfidence;
    public ConfidenceLevel HiddenAttributeConfidence;
    public ConfidenceLevel CAConfidence;
    public ConfidenceLevel PAConfidence;
    public ConfidenceLevel SalaryConfidence;
    public ConfidenceLevel TeamImpactConfidence;

    // Returns default confidence levels per source (Page 05 section 14.3).
    public static CandidateConfidenceData FromSource(CandidateSource source)
    {
        switch (source)
        {
            case CandidateSource.StartingPool:
                return new CandidateConfidenceData
                {
                    SkillConfidence            = ConfidenceLevel.Low,
                    VisibleAttributeConfidence = ConfidenceLevel.Low,
                    HiddenAttributeConfidence  = ConfidenceLevel.Unknown,
                    CAConfidence               = ConfidenceLevel.Low,
                    PAConfidence               = ConfidenceLevel.Unknown,
                    SalaryConfidence           = ConfidenceLevel.Low,
                    TeamImpactConfidence       = ConfidenceLevel.Unknown
                };

            case CandidateSource.HRSearch:
                return new CandidateConfidenceData
                {
                    SkillConfidence            = ConfidenceLevel.Medium,
                    VisibleAttributeConfidence = ConfidenceLevel.Medium,
                    HiddenAttributeConfidence  = ConfidenceLevel.Low,
                    CAConfidence               = ConfidenceLevel.Medium,
                    PAConfidence               = ConfidenceLevel.Low,
                    SalaryConfidence           = ConfidenceLevel.Medium,
                    TeamImpactConfidence       = ConfidenceLevel.Low
                };

            case CandidateSource.Referral:
                return new CandidateConfidenceData
                {
                    SkillConfidence            = ConfidenceLevel.Medium,
                    VisibleAttributeConfidence = ConfidenceLevel.Medium,
                    HiddenAttributeConfidence  = ConfidenceLevel.Low,
                    CAConfidence               = ConfidenceLevel.Medium,
                    PAConfidence               = ConfidenceLevel.Low,
                    SalaryConfidence           = ConfidenceLevel.Medium,
                    TeamImpactConfidence       = ConfidenceLevel.Medium
                };

            case CandidateSource.FormerEmployee:
                return new CandidateConfidenceData
                {
                    SkillConfidence            = ConfidenceLevel.High,
                    VisibleAttributeConfidence = ConfidenceLevel.High,
                    HiddenAttributeConfidence  = ConfidenceLevel.Medium,
                    CAConfidence               = ConfidenceLevel.High,
                    PAConfidence               = ConfidenceLevel.Medium,
                    SalaryConfidence           = ConfidenceLevel.High,
                    TeamImpactConfidence       = ConfidenceLevel.High
                };

            case CandidateSource.CompetitorLayoff:
                return new CandidateConfidenceData
                {
                    SkillConfidence            = ConfidenceLevel.Low,
                    VisibleAttributeConfidence = ConfidenceLevel.Low,
                    HiddenAttributeConfidence  = ConfidenceLevel.Unknown,
                    CAConfidence               = ConfidenceLevel.Low,
                    PAConfidence               = ConfidenceLevel.Unknown,
                    SalaryConfidence           = ConfidenceLevel.Low,
                    TeamImpactConfidence       = ConfidenceLevel.Unknown
                };

            default: // OpenMarket
                return new CandidateConfidenceData
                {
                    SkillConfidence            = ConfidenceLevel.Unknown,
                    VisibleAttributeConfidence = ConfidenceLevel.Unknown,
                    HiddenAttributeConfidence  = ConfidenceLevel.Unknown,
                    CAConfidence               = ConfidenceLevel.Unknown,
                    PAConfidence               = ConfidenceLevel.Unknown,
                    SalaryConfidence           = ConfidenceLevel.Unknown,
                    TeamImpactConfidence       = ConfidenceLevel.Unknown
                };
        }
    }

    // Upgrades confidence based on interview progress (Page 05 section 17).
    // InterviewStage: 40 = FirstReport threshold, 100 = FinalReport threshold.
    public void ApplyInterview(int knowledgeLevel)
    {
        if (knowledgeLevel >= 100)
        {
            // Final report: upgrade to High / Confirmed
            SkillConfidence            = ConfidenceLevel.Confirmed;
            VisibleAttributeConfidence = ConfidenceLevel.High;
            HiddenAttributeConfidence  = ConfidenceLevel.Medium;
            CAConfidence               = ConfidenceLevel.Confirmed;
            PAConfidence               = ConfidenceLevel.High;
            SalaryConfidence           = ConfidenceLevel.High;
            TeamImpactConfidence       = ConfidenceLevel.Medium;
        }
        else if (knowledgeLevel >= 40)
        {
            // First report: upgrade skill to High, visible to Medium, improve CA/salary
            if (SkillConfidence < ConfidenceLevel.High)
                SkillConfidence = ConfidenceLevel.High;
            if (VisibleAttributeConfidence < ConfidenceLevel.Medium)
                VisibleAttributeConfidence = ConfidenceLevel.Medium;
            if (HiddenAttributeConfidence < ConfidenceLevel.Low)
                HiddenAttributeConfidence = ConfidenceLevel.Low;
            if (CAConfidence < ConfidenceLevel.Medium)
                CAConfidence = ConfidenceLevel.Medium;
            if (PAConfidence < ConfidenceLevel.Low)
                PAConfidence = ConfidenceLevel.Low;
            if (SalaryConfidence < ConfidenceLevel.Medium)
                SalaryConfidence = ConfidenceLevel.Medium;
        }
    }

    // Derives initial confidence from legacy InterviewStage int (0=none, 1=in-progress, 2=first, 3=complete)
    public static CandidateConfidenceData FromLegacyInterviewStage(int interviewStage, CandidateSource source)
    {
        var data = FromSource(source);
        if (interviewStage >= 3)
            data.ApplyInterview(100);
        else if (interviewStage >= 2)
            data.ApplyInterview(40);
        return data;
    }
}
