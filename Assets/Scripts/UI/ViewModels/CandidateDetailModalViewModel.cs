using System.Collections.Generic;
using UnityEngine;

public struct RoleSuitabilityEntry
{
    public RoleId Role;
    public RoleSuitability Suitability;
    public int AbilityForRole;
    public string RoleName;
    public string SuitabilityClass;
    public bool IsPreferred;
}

public struct SkillTableEntry
{
    public string Name;
    public string ValueText;
    public string ValueClass;
    public Color NameColor;
}

/// <summary>
/// ViewModel for the UXML-backed Candidate Detail Modal.
/// Computes header data, overview (projected fits, estimated skills, attributes, report, team projection),
/// interview data, personality data, bottom status cards, and action visibility.
/// All estimated values communicate confidence levels and ranges.
/// </summary>
public class CandidateDetailModalViewModel : IViewModel
{
    // ── Nested structs ───────────────────────────────────────────────────────

    public struct BadgeData
    {
        public string Label;
        public string UssClass;
    }

    public struct ProjectedRoleFitEntry
    {
        public string RoleName;
        public string FitRangeText;
        public string SuitabilityClass;
        public string ConfidenceClass;
        public string ConfidenceDotClass;
        public string Warning;
    }

    public struct EstimatedSkillEntry
    {
        public string Name;
        public string DisplayText;
        public string DisplayClass;
        public string ConfidenceDotClass;
        public Color  NameColor;
    }

    public struct EstimatedAttributeEntry
    {
        public string Name;
        public string DisplayText;
        public string DisplayClass;
        public string ConfidenceDotClass;
    }

    public struct CandidateReportData
    {
        public string SummaryLabel;
        public string[] Strengths;
        public string[] Concerns;
        public string Recommendation;
        public string ReportConfidence;
    }

    public struct TeamProjectionData
    {
        public string SelectedTeamName;
        public string ProjectedFitText;
        public string ProjectionDetailText;
        public string ProjectionConfidence;
    }

    public struct InterviewData
    {
        public string StageText;
        public string AssignedHRTeam;
        public string TimeRemaining;
        public string KnowledgeText;
        public string FirstReportText;
        public string FinalReportText;
        public string[] RevealedStrengths;
        public string[] RevealedConcerns;
    }

    public struct PersonalityData
    {
        public string PersonalityEstimate;
        public string PersonalityConfidence;
        public EstimatedAttributeEntry[] Signals;
        public string[] RiskFlags;
        public string RetentionRisk;
        public string SalaryPressure;
    }

    public enum ComparisonTarget
    {
        SameRoleEmployees,
        CurrentTeamAverage,
        AnotherCandidate,
        RoleMarketAverage
    }

    public struct ComparisonMetricRow
    {
        public string MetricName;
        public string CandidateValueText;
        public string ComparisonValueText;
        public string DeltaText;
        public string DeltaClass;
        public string ConfidenceClass;
    }

    public struct ComparisonTabData
    {
        public string[] TargetLabels;
        public int SelectedTargetIndex;
        public ComparisonMetricRow[] Metrics;
        public string RecommendationText;
        public string TargetColumnHeader;
    }

    public struct AcceptanceFactor
    {
        public string Name;
        public string ValueText;
        public string ImpactClass;
    }

    public struct OfferTabData
    {
        public string DemandStateText;
        public string SalaryEstimateText;
        public string AcceptanceChanceLabel;
        public string AcceptanceChanceClass;
        public AcceptanceFactor[] Factors;
        public string MonthlyCostText;
        public string RunwayImpactText;
        public string RunwayImpactClass;
        public bool CanMakeOffer;
        public bool CanHire;
        public bool CanWithdrawOffer;
        public bool CanAcceptCounter;
        public bool CanRejectCounter;
        public string StatusText;
        public string StatusClass;
    }

    // ── Internal state ───────────────────────────────────────────────────────

    private int _candidateId;
    private CandidateData _candidate;
    private IReadOnlyGameState _state;
    private int _targetTeamIndex = -1;

    // ── IViewModel dirty tracking ────────────────────────────────────────────

    public bool IsDirty { get; private set; }
    public void ClearDirty() => IsDirty = false;

    // ── Header properties ────────────────────────────────────────────────────

    public string Name               { get; private set; }
    public string Age                { get; private set; }
    public string RoleName           { get; private set; }
    public string RoleFamilyName     { get; private set; }
    public string RolePillClass      { get; private set; }
    public string CandidateSource    { get; private set; }
    public string SourceBadgeClass   { get; private set; }
    public string PipelineState      { get; private set; }
    public string PipelineStateClass { get; private set; }
    public string SalaryEstimateText { get; private set; }
    public string SalaryConfidenceText { get; private set; }
    public string CAEstimateText     { get; private set; }
    public string PAEstimateText     { get; private set; }
    public string OverallConfidenceText  { get; private set; }
    public string OverallConfidenceClass { get; private set; }
    public string ExpiryText         { get; private set; }
    public string ExpiryClass        { get; private set; }

    public List<BadgeData> BadgeList { get; private set; } = new List<BadgeData>(6);

    // ── Tab ──────────────────────────────────────────────────────────────────

    public int ActiveTabIndex { get; set; }

    // ── Overview data ────────────────────────────────────────────────────────

    public ProjectedRoleFitEntry[] TopProjectedFits  { get; private set; }
    public EstimatedSkillEntry[] CoreSkills           { get; private set; }
    public EstimatedSkillEntry[] SupportingSkills     { get; private set; }
    public EstimatedAttributeEntry[] EstimatedAttributes { get; private set; }
    public CandidateReportData Report                { get; private set; }
    public TeamProjectionData TeamProjection          { get; private set; }
    public string[] AvailableTeamNames               { get; private set; }

    // ── Interview data ───────────────────────────────────────────────────────

    public InterviewData Interview { get; private set; }

    // ── Personality data ─────────────────────────────────────────────────────

    public PersonalityData Personality { get; private set; }

    // ── Comparison tab data ──────────────────────────────────────────────────

    public ComparisonTabData Comparison    { get; private set; }
    public int ComparisonTargetIndex       { get; private set; }

    // ── Offer tab data ───────────────────────────────────────────────────────

    public OfferTabData Offer              { get; private set; }

    // ── Bottom status cards ──────────────────────────────────────────────────

    public string InterestCardText      { get; private set; }
    public string InterestCardClass     { get; private set; }
    public string SalaryCardText        { get; private set; }
    public string SalaryCardClass       { get; private set; }
    public string PotentialCardText     { get; private set; }
    public string PotentialCardClass    { get; private set; }
    public string RiskCardText          { get; private set; }
    public string RiskCardClass         { get; private set; }
    public string AvailabilityCardText  { get; private set; }
    public string AvailabilityCardClass { get; private set; }
    public string InterviewCardText     { get; private set; }
    public string InterviewCardClass    { get; private set; }
    public string TeamImpactCardText    { get; private set; }
    public string TeamImpactCardClass   { get; private set; }
    public string ComparisonCardText    { get; private set; }
    public string ComparisonCardClass   { get; private set; }

    // ── Action visibility ────────────────────────────────────────────────────

    public string InterviewButtonText     { get; private set; }
    public bool   InterviewButtonEnabled  { get; private set; }
    public bool   IsShortlisted           { get; private set; }
    public bool   IsOfferOnCooldown       { get; private set; }
    public bool   HasPendingCounter       { get; private set; }

    // ── Knowledge thresholds (preserved from old VM) ─────────────────────────

    public float KnowledgePercent          { get; private set; }
    public bool  IsPersonalityRevealed     { get; private set; }
    public bool  IsPreferencesRevealed     { get; private set; }
    public bool  IsPrimarySkillsRevealed   { get; private set; }
    public bool  IsAbilityRevealed         { get; private set; }
    public bool  IsPotentialRevealed       { get; private set; }
    public bool  IsSkillsRevealed          { get; private set; }

    // ── Preserved offer state (backward compat for future Offer tab) ─────────

    private readonly RoleSuitabilityEntry[] _roleSuitabilities =
        new RoleSuitabilityEntry[RoleSuitabilityCalculator.AllRoles.Length];
    public RoleSuitabilityEntry[] RoleSuitabilities => _roleSuitabilities;

    public int SalarySliderMin    { get; private set; }
    public int SalarySliderMax    { get; private set; }
    public int SalarySliderAnchor { get; private set; }
    public int CurrentOfferSalary { get; private set; }
    public RoleId SelectedRole    { get; private set; }

    public float AcceptanceChance       { get; private set; }
    public string AcceptanceChanceText  { get; private set; }
    public string AcceptanceChanceClass { get; private set; }

    public int MaxPatience     { get; private set; }
    public int CurrentPatience { get; private set; }

    public bool HasActiveNegotiation { get; private set; }

    public bool ShowMismatchSection { get; private set; }
    public string MismatchHintText  { get; private set; }
    private readonly List<string> _mismatchWarnings = new List<string>(4);
    public List<string> MismatchWarnings => _mismatchWarnings;

    private EmploymentType _currentEmploymentType = EmploymentType.FullTime;
    private ContractLengthOption _currentLength = ContractLengthOption.Standard;
    private const int MinimumWage = 500;

    // ── Cached team list ─────────────────────────────────────────────────────

    private TeamId[] _teamIds;

    public int CandidateId => _candidateId;

    // ── API ──────────────────────────────────────────────────────────────────

    public void SetCandidateId(int candidateId)
    {
        _candidateId = candidateId;
    }

    public void SetTargetTeamIndex(int teamIndex)
    {
        _targetTeamIndex = teamIndex;
        if (_state != null)
        {
            RefreshTeamProjection();
        }
    }

    public void SetComparisonTarget(int targetIndex)
    {
        ComparisonTargetIndex = targetIndex;
        if (_state != null && _candidate != null)
        {
            RefreshComparisonData(_state);
        }
    }

    public void Refresh(GameStateSnapshot snapshot)
    {
        if (snapshot == null) return;
        _state = snapshot;

        _candidate = FindCandidate(snapshot);
        if (_candidate == null) return;

        int currentTick  = snapshot.CurrentTick;
        float knowledge  = snapshot.GetInterviewKnowledgeLevel(_candidateId);
        bool inProgress  = snapshot.IsInterviewInProgress(_candidateId);
        bool interviewDone = snapshot.IsFinalReportReady(_candidateId);

        // Knowledge thresholds
        KnowledgePercent        = knowledge;
        IsPersonalityRevealed   = knowledge >= 100f;
        IsPreferencesRevealed   = knowledge >= 20f;
        IsPrimarySkillsRevealed = knowledge >= 40f;
        IsAbilityRevealed       = knowledge >= 60f;
        IsPotentialRevealed     = knowledge >= 80f;
        IsSkillsRevealed        = knowledge >= 40f;

        RefreshHeader(snapshot, currentTick, knowledge, inProgress, interviewDone);
        RefreshBadges(snapshot, currentTick, knowledge);
        RefreshOverviewData(snapshot, knowledge);
        RefreshInterviewData(snapshot, knowledge, inProgress, interviewDone);
        RefreshPersonalityData(snapshot, knowledge);
        RefreshBottomStatusCards(snapshot, currentTick, knowledge, inProgress, interviewDone);
        RefreshActionVisibility(snapshot, knowledge, inProgress, interviewDone);
        RefreshOfferState(snapshot);
        RefreshComparisonData(snapshot);
        RefreshOfferTabData(snapshot);
        IsDirty = true;
    }

    // ── Header refresh ───────────────────────────────────────────────────────

    private void RefreshHeader(IReadOnlyGameState state, int currentTick, float knowledge,
                               bool inProgress, bool interviewDone)
    {
        Name          = _candidate.Name;
        Age           = _candidate.Age + "y";
        RoleName      = UIFormatting.FormatRole(_candidate.Role);
        RolePillClass = UIFormatting.RolePillClass(_candidate.Role);

        // Source / pipeline
        if (_candidate.IsPendingReview)
        {
            CandidateSource    = "HR Sourced";
            SourceBadgeClass   = "pipeline--hr-sourced";
        }
        else if (_candidate.IsTargeted)
        {
            CandidateSource    = "Shortlisted";
            SourceBadgeClass   = "pipeline--shortlisted";
        }
        else
        {
            CandidateSource    = "Market";
            SourceBadgeClass   = "pipeline--market";
        }

        // Pipeline state
        if (state.HasActiveNegotiation(_candidateId))
        {
            PipelineState      = "Offer Pending";
            PipelineStateClass = "pipeline--offer-pending";
        }
        else if (interviewDone)
        {
            PipelineState      = "Final Report";
            PipelineStateClass = "pipeline--final-report";
        }
        else if (inProgress)
        {
            PipelineState      = "Interviewing";
            PipelineStateClass = "pipeline--interviewing";
        }
        else if (_candidate.IsTargeted)
        {
            PipelineState      = "Shortlisted";
            PipelineStateClass = "pipeline--shortlisted";
        }
        else if (_candidate.IsPendingReview)
        {
            PipelineState      = "HR Sourced";
            PipelineStateClass = "pipeline--hr-sourced";
        }
        else
        {
            PipelineState      = "Available";
            PipelineStateClass = "pipeline--market";
        }

        // Salary estimate
        bool salaryVisible = SalaryDemandCalculator.IsSalaryRevealed(_candidate);
        if (salaryVisible)
        {
            int salaryDemand = state.GetEffectiveSalaryDemand(_candidateId);
            SalaryEstimateText = UIFormatting.FormatMoney(salaryDemand) + "/mo";
            SalaryConfidenceText = "Confirmed";
        }
        else
        {
            int marketRate = SalaryBand.GetBase(_candidate.Role);
            SalaryEstimateText = "~" + UIFormatting.FormatMoney(marketRate) + "/mo";
            SalaryConfidenceText = knowledge > 0 ? "Estimated" : "Unknown";
        }

        // CA / PA estimates
        if (IsAbilityRevealed)
        {
            int abilityStars = state.GetAbilityStarEstimate(_candidateId);
            CAEstimateText = abilityStars > 0 ? abilityStars + "\u2605" : "?\u2605";
        }
        else
        {
            CAEstimateText = "?\u2605";
        }

        if (IsPotentialRevealed)
        {
            int potentialStars = state.GetPotentialStarEstimate(_candidateId);
            PAEstimateText = potentialStars > 0 ? potentialStars + "\u2605" : "?\u2605";
        }
        else
        {
            PAEstimateText = "?\u2605";
        }

        // Overall confidence
        ConfidenceLevel overall = ComputeOverallConfidence(knowledge);
        OverallConfidenceText  = FormatConfidenceLabel(overall);
        OverallConfidenceClass = ConfidenceCssClass(overall);

        // Expiry
        if (_candidate.ExpiryTick == int.MaxValue)
        {
            ExpiryText  = "Indefinite";
            ExpiryClass = "expiry--normal";
        }
        else if (_candidate.ExpiryTick <= 0)
        {
            ExpiryText  = "\u2014";
            ExpiryClass = "expiry--normal";
        }
        else
        {
            int daysLeft = (_candidate.ExpiryTick - currentTick) / TimeState.TicksPerDay;
            if (daysLeft < 0) daysLeft = 0;
            ExpiryText = daysLeft + "d remaining";

            if (daysLeft < 3)
                ExpiryClass = "expiry--urgent";
            else if (daysLeft < 7)
                ExpiryClass = "expiry--warning";
            else
                ExpiryClass = "expiry--normal";
        }
    }

    // ── Badge refresh ────────────────────────────────────────────────────────

    private void RefreshBadges(IReadOnlyGameState state, int currentTick, float knowledge)
    {
        BadgeList.Clear();

        // Expiry warning
        if (_candidate.ExpiryTick != int.MaxValue && _candidate.ExpiryTick > 0)
        {
            int daysLeft = (_candidate.ExpiryTick - currentTick) / TimeState.TicksPerDay;
            if (daysLeft < 3)
                BadgeList.Add(new BadgeData { Label = "Expiring!", UssClass = "badge--danger" });
            else if (daysLeft < 7)
                BadgeList.Add(new BadgeData { Label = "Expiring Soon", UssClass = "badge--warning" });
        }

        // Interview state
        if (state.IsFinalReportReady(_candidateId))
            BadgeList.Add(new BadgeData { Label = "Report Ready", UssClass = "badge--success" });
        else if (state.IsInterviewInProgress(_candidateId))
            BadgeList.Add(new BadgeData { Label = "Interviewing", UssClass = "badge--accent" });

        // Knowledge level
        if (knowledge >= 80f)
            BadgeList.Add(new BadgeData { Label = "High Confidence", UssClass = "badge--success" });
        else if (knowledge >= 40f)
            BadgeList.Add(new BadgeData { Label = "Medium Confidence", UssClass = "badge--warning" });
        else if (knowledge > 0f)
            BadgeList.Add(new BadgeData { Label = "Low Confidence", UssClass = "badge--danger" });

        // Shortlisted
        if (_candidate.IsTargeted)
            BadgeList.Add(new BadgeData { Label = "Shortlisted", UssClass = "badge--accent" });
    }

    // ── Overview data refresh ────────────────────────────────────────────────

    private void RefreshOverviewData(IReadOnlyGameState state, float knowledge)
    {
        RefreshProjectedRoleFits(state);
        RefreshEstimatedSkills(knowledge);
        RefreshEstimatedAttributes(knowledge);
        RefreshReport(state, knowledge);
        RefreshTeamList(state);
        RefreshTeamProjection();
    }

    private void RefreshProjectedRoleFits(IReadOnlyGameState state)
    {
        var allRoles  = RoleSuitabilityCalculator.AllRoles;
        int roleCount = allRoles.Length;

        // Build suitability entries and sort by ability
        var entries = new List<ProjectedRoleFitEntry>(roleCount);
        for (int i = 0; i < roleCount; i++)
        {
            var role = allRoles[i];
            int ability = 0;
            RoleSuitability suitability = RoleSuitability.Unsuitable;
            ConfidenceLevel conf = IsSkillsRevealed ? ConfidenceLevel.Medium : ConfidenceLevel.Unknown;

            if (IsSkillsRevealed && _candidate.Stats.Skills != null)
            {
                ability = state.ComputeAbilityForRole(_candidate.Stats.Skills, role);
                suitability = RoleSuitabilityCalculator.GetSuitability(ability);
                conf = IsAbilityRevealed ? ConfidenceLevel.High : ConfidenceLevel.Medium;
            }

            // Store in role suitabilities array for offer tab compat
            _roleSuitabilities[i] = new RoleSuitabilityEntry
            {
                Role           = role,
                Suitability    = suitability,
                AbilityForRole = ability,
                RoleName       = UIFormatting.FormatRole(role),
                SuitabilityClass = UIFormatting.SuitabilityDotClass(suitability),
                IsPreferred    = role == _candidate.Role
            };

            string rangeText;
            if (conf == ConfidenceLevel.Unknown)
            {
                rangeText = "Unknown";
            }
            else
            {
                int spread = conf == ConfidenceLevel.High ? 1 : 2;
                int minVal = Mathf.Max(0, ability - spread);
                int maxVal = ability + spread;
                rangeText = minVal + "-" + maxVal;
            }

            entries.Add(new ProjectedRoleFitEntry
            {
                RoleName         = UIFormatting.FormatRole(role),
                FitRangeText     = rangeText,
                SuitabilityClass = UIFormatting.SuitabilityDotClass(suitability),
                ConfidenceClass  = ConfidenceCssClass(conf),
                ConfidenceDotClass = ConfidenceDotClass(conf),
                Warning          = ""
            });
        }

        TopProjectedFits = entries.ToArray();
    }

    private void RefreshEstimatedSkills(float knowledge)
    {
        int skillCount = SkillIdHelper.SkillCount;
        var core = new List<EstimatedSkillEntry>(8);
        var supporting = new List<EstimatedSkillEntry>(18);

        for (int i = 0; i < skillCount; i++)
        {
            var skillId = (SkillId)i;
            ConfidenceLevel conf = GetSkillConfidence(i);
            int value = _candidate.Stats.Skills != null && i < _candidate.Stats.Skills.Length
                ? _candidate.Stats.Skills[i]
                : 0;

            string displayText;
            string displayClass;
            ComputeEstimatedRange(value, conf, out displayText, out displayClass);

            var entry = new EstimatedSkillEntry
            {
                Name             = SkillIdHelper.GetName(skillId),
                DisplayText      = displayText,
                DisplayClass     = displayClass,
                ConfidenceDotClass = ConfidenceDotClass(conf),
                NameColor        = UIFormatting.GetSkillColor(skillId)
            };

            // Determine if core (Primary) or supporting — use best guess
            bool isCore = IsCoreSkill(i);
            if (isCore)
                core.Add(entry);
            else
                supporting.Add(entry);
        }

        CoreSkills       = core.ToArray();
        SupportingSkills = supporting.ToArray();
    }

    private bool IsCoreSkill(int skillIndex)
    {
        // Use role suitability data: if the candidate's role treats this as Primary, it's core
        // Without a RoleProfileTable reference, classify top skills as core based on value
        // For now: skills above median are "core" for the candidate's role
        // In practice, the first ~8 highest-value skills are shown as core
        if (_candidate.Stats.Skills == null) return skillIndex < 8;

        int thisValue = _candidate.Stats.Skills[skillIndex];
        int higherCount = 0;
        int skillCount = SkillIdHelper.SkillCount;
        for (int i = 0; i < skillCount; i++)
        {
            if (i == skillIndex) continue;
            if (_candidate.Stats.Skills[i] > thisValue) higherCount++;
        }
        return higherCount < 8;
    }

    private void RefreshEstimatedAttributes(float knowledge)
    {
        // Visible attributes: always show with confidence based on knowledge
        int visCount = 8; // VisibleAttributeId count
        var attrs = new List<EstimatedAttributeEntry>(visCount);

        string[] visNames = { "Leadership", "Creativity", "Focus", "Communication",
                              "Adaptability", "Work Ethic", "Composure", "Initiative" };

        for (int i = 0; i < visCount; i++)
        {
            int value = _candidate.Stats.VisibleAttributes != null &&
                        i < _candidate.Stats.VisibleAttributes.Length
                ? _candidate.Stats.VisibleAttributes[i]
                : 10;

            ConfidenceLevel conf = ConfidenceLevel.Unknown;
            if (_candidate.VisibleAttributeConfidence != null &&
                i < _candidate.VisibleAttributeConfidence.Length)
            {
                conf = _candidate.VisibleAttributeConfidence[i];
            }
            else if (knowledge >= 60f)
            {
                conf = ConfidenceLevel.Medium;
            }
            else if (knowledge >= 20f)
            {
                conf = ConfidenceLevel.Low;
            }

            string displayText;
            string displayClass;
            ComputeEstimatedRange(value, conf, out displayText, out displayClass);

            attrs.Add(new EstimatedAttributeEntry
            {
                Name              = visNames[i],
                DisplayText       = displayText,
                DisplayClass      = displayClass,
                ConfidenceDotClass = ConfidenceDotClass(conf)
            });
        }

        EstimatedAttributes = attrs.ToArray();
    }

    private void RefreshReport(IReadOnlyGameState state, float knowledge)
    {
        string summary;
        string[] strengths;
        string[] concerns;
        string recommendation;
        string reportConf;

        if (knowledge >= 80f)
        {
            summary = "Detailed assessment available";
            strengths = BuildReportStrengths();
            concerns = BuildReportConcerns();
            recommendation = BuildRecommendation();
            reportConf = "High confidence report";
        }
        else if (knowledge >= 40f)
        {
            summary = "Preliminary assessment";
            strengths = BuildReportStrengths();
            concerns = new string[0];
            recommendation = "More interview data needed for full recommendation";
            reportConf = "Medium confidence — continue interview for details";
        }
        else if (knowledge > 0f)
        {
            summary = "Interview in progress";
            strengths = new string[0];
            concerns = new string[0];
            recommendation = "Interview not yet complete";
            reportConf = "Low confidence — early assessment only";
        }
        else
        {
            summary = "No interview data";
            strengths = new string[0];
            concerns = new string[0];
            recommendation = "Start an interview to learn about this candidate";
            reportConf = "";
        }

        Report = new CandidateReportData
        {
            SummaryLabel     = summary,
            Strengths        = strengths,
            Concerns         = concerns,
            Recommendation   = recommendation,
            ReportConfidence = reportConf
        };
    }

    private string[] BuildReportStrengths()
    {
        if (_candidate.Stats.Skills == null) return new string[0];

        var strengths = new List<string>(3);
        int skillCount = SkillIdHelper.SkillCount;
        for (int i = 0; i < skillCount && strengths.Count < 3; i++)
        {
            if (_candidate.Stats.Skills[i] >= 12)
            {
                strengths.Add("Strong " + SkillIdHelper.GetName((SkillId)i) + " (" + _candidate.Stats.Skills[i] + ")");
            }
        }

        if (strengths.Count == 0)
            strengths.Add("No standout strengths identified yet");

        return strengths.ToArray();
    }

    private string[] BuildReportConcerns()
    {
        var concerns = new List<string>(3);

        if (_candidate.Stats.Skills != null)
        {
            int skillCount = SkillIdHelper.SkillCount;
            int lowCount = 0;
            for (int i = 0; i < skillCount; i++)
            {
                if (_candidate.Stats.Skills[i] <= 3) lowCount++;
            }
            if (lowCount >= 5)
                concerns.Add("Multiple weak skill areas detected");
        }

        if (IsPersonalityRevealed)
        {
            int ego = _candidate.Stats.GetHiddenAttribute(HiddenAttributeId.Ego);
            if (ego >= 16) concerns.Add("High ego may cause team friction");

            int loyalty = _candidate.Stats.GetHiddenAttribute(HiddenAttributeId.Loyalty);
            if (loyalty <= 5) concerns.Add("Low loyalty — retention risk");
        }

        return concerns.ToArray();
    }

    private string BuildRecommendation()
    {
        if (!IsAbilityRevealed) return "Need more interview data for recommendation";

        int avgSkill = _candidate.RoleRelevantAverage;
        if (avgSkill >= 14) return "Strong candidate — consider making an offer";
        if (avgSkill >= 10) return "Solid candidate — good fit for most teams";
        if (avgSkill >= 6)  return "Average candidate — consider if role is a good match";
        return "Below average — may need development time";
    }

    private void RefreshTeamList(IReadOnlyGameState state)
    {
        var teams = state.ActiveTeams;
        int tc = teams.Count;

        var names = new string[tc + 1];
        _teamIds = new TeamId[tc + 1];

        names[0] = "Select a team...";
        _teamIds[0] = new TeamId(-1);

        for (int i = 0; i < tc; i++)
        {
            names[i + 1] = teams[i].name;
            _teamIds[i + 1] = teams[i].id;
        }

        AvailableTeamNames = names;
    }

    private void RefreshTeamProjection()
    {
        if (_targetTeamIndex <= 0 || _teamIds == null ||
            _targetTeamIndex >= _teamIds.Length || _state == null)
        {
            TeamProjection = new TeamProjectionData
            {
                SelectedTeamName    = "None",
                ProjectedFitText    = "Select a team to see projection",
                ProjectionDetailText = "",
                ProjectionConfidence = ""
            };
            return;
        }

        var teamId = _teamIds[_targetTeamIndex];
        string teamName = AvailableTeamNames != null && _targetTeamIndex < AvailableTeamNames.Length
            ? AvailableTeamNames[_targetTeamIndex]
            : "Team";

        ConfidenceLevel conf = IsSkillsRevealed ? ConfidenceLevel.Medium : ConfidenceLevel.Low;
        string confText = FormatConfidenceLabel(conf);

        // Compute projected fit using estimated stats
        string fitText = "Projected fit: Estimated";
        string detailText = "Salary impact: " + SalaryEstimateText + " | Confidence: " + confText;

        TeamProjection = new TeamProjectionData
        {
            SelectedTeamName     = teamName,
            ProjectedFitText     = fitText,
            ProjectionDetailText = detailText,
            ProjectionConfidence = confText
        };
    }

    // ── Interview data refresh ───────────────────────────────────────────────

    private void RefreshInterviewData(IReadOnlyGameState state, float knowledge,
                                       bool inProgress, bool interviewDone)
    {
        string stageText;
        if (interviewDone)
            stageText = "Complete";
        else if (inProgress)
            stageText = "In Progress (" + (int)knowledge + "%)";
        else
            stageText = "Not Started";

        string hrTeam = "\u2014";
        if (_candidate.SourcingTeamId.Value > 0)
        {
            var teams = state.ActiveTeams;
            int tc = teams.Count;
            for (int t = 0; t < tc; t++)
            {
                if (teams[t].id == _candidate.SourcingTeamId)
                {
                    hrTeam = teams[t].name;
                    break;
                }
            }
        }

        string timeRemaining = "\u2014";
        if (inProgress && knowledge < 100f)
        {
            int remaining = (int)(100f - knowledge);
            timeRemaining = "~" + remaining + "% remaining";
        }

        string firstReport = interviewDone || knowledge >= 40f
            ? "First report available"
            : "First report not yet available";

        string finalReport = interviewDone
            ? "Final report available"
            : "Final report not yet available";

        string[] revealedStrengths = BuildReportStrengths();
        string[] revealedConcerns = interviewDone ? BuildReportConcerns() : new string[0];

        Interview = new InterviewData
        {
            StageText         = stageText,
            AssignedHRTeam    = hrTeam,
            TimeRemaining     = timeRemaining,
            KnowledgeText     = (int)knowledge + "%",
            FirstReportText   = firstReport,
            FinalReportText   = finalReport,
            RevealedStrengths = revealedStrengths,
            RevealedConcerns  = revealedConcerns
        };
    }

    // ── Personality data refresh ─────────────────────────────────────────────

    private void RefreshPersonalityData(IReadOnlyGameState state, float knowledge)
    {
        string personalityEstimate;
        string personalityConfidence;

        if (IsPersonalityRevealed)
        {
            personalityEstimate = UIFormatting.FormatPersonality(_candidate.personality);
            personalityConfidence = "Confirmed";
        }
        else if (knowledge > 0f)
        {
            personalityEstimate = "Partially revealed";
            personalityConfidence = "Low-Medium";
        }
        else
        {
            personalityEstimate = "Unknown";
            personalityConfidence = "None — interview required";
        }

        // Build hidden attribute signals (never show exact values, only labels)
        string[] hiddenNames = { "Learning Rate", "Ambition", "Loyalty",
                                  "Pressure Tolerance", "Ego", "Consistency", "Mentoring" };
        int hiddenCount = 7;
        var signals = new List<EstimatedAttributeEntry>(hiddenCount);

        for (int i = 0; i < hiddenCount; i++)
        {
            int value = _candidate.Stats.HiddenAttributes != null &&
                        i < _candidate.Stats.HiddenAttributes.Length
                ? _candidate.Stats.HiddenAttributes[i]
                : 10;

            ConfidenceLevel conf = ConfidenceLevel.Unknown;
            if (_candidate.HiddenAttributeConfidence != null &&
                i < _candidate.HiddenAttributeConfidence.Length)
            {
                conf = _candidate.HiddenAttributeConfidence[i];
            }
            else if (knowledge >= 80f)
            {
                conf = ConfidenceLevel.Medium;
            }

            // Hidden attributes show labels, not values
            string displayText;
            string displayClass;
            if (conf == ConfidenceLevel.Unknown)
            {
                displayText = "Unknown";
                displayClass = "cdm-range-value--unknown";
            }
            else if (conf <= ConfidenceLevel.Low)
            {
                displayText = GetAttributeLabel(value, 4);
                displayClass = "confidence--low";
            }
            else
            {
                displayText = GetAttributeLabel(value, 2);
                displayClass = conf >= ConfidenceLevel.High ? "confidence--high" : "confidence--medium";
            }

            signals.Add(new EstimatedAttributeEntry
            {
                Name              = hiddenNames[i],
                DisplayText       = displayText,
                DisplayClass      = displayClass,
                ConfidenceDotClass = ConfidenceDotClass(conf)
            });
        }

        // Risk flags
        var riskFlags = new List<string>(3);
        if (IsPersonalityRevealed)
        {
            int ego = _candidate.Stats.GetHiddenAttribute(HiddenAttributeId.Ego);
            if (ego >= 16) riskFlags.Add("High ego — potential team friction");

            int loyalty = _candidate.Stats.GetHiddenAttribute(HiddenAttributeId.Loyalty);
            if (loyalty <= 5) riskFlags.Add("Low loyalty — flight risk");

            int pressure = _candidate.Stats.GetHiddenAttribute(HiddenAttributeId.PressureTolerance);
            if (pressure <= 5) riskFlags.Add("Low pressure tolerance — burnout risk");
        }
        else if (knowledge >= 40f)
        {
            riskFlags.Add("Risk assessment requires more interview data");
        }

        // Retention risk
        string retentionRisk;
        if (IsPersonalityRevealed)
        {
            int loyalty = _candidate.Stats.GetHiddenAttribute(HiddenAttributeId.Loyalty);
            if (loyalty >= 14) retentionRisk = "Low";
            else if (loyalty >= 8) retentionRisk = "Medium";
            else retentionRisk = "High";
        }
        else
        {
            retentionRisk = "Unknown";
        }

        // Salary pressure
        string salaryPressure;
        if (IsPersonalityRevealed)
        {
            int ambition = _candidate.Stats.GetHiddenAttribute(HiddenAttributeId.Ambition);
            if (ambition >= 14) salaryPressure = "High";
            else if (ambition >= 8) salaryPressure = "Medium";
            else salaryPressure = "Low";
        }
        else
        {
            salaryPressure = "Unknown";
        }

        Personality = new PersonalityData
        {
            PersonalityEstimate   = personalityEstimate,
            PersonalityConfidence = personalityConfidence,
            Signals               = signals.ToArray(),
            RiskFlags             = riskFlags.ToArray(),
            RetentionRisk         = retentionRisk,
            SalaryPressure        = salaryPressure
        };
    }

    // ── Bottom status cards refresh ──────────────────────────────────────────

    private void RefreshBottomStatusCards(IReadOnlyGameState state, int currentTick,
                                          float knowledge, bool inProgress, bool interviewDone)
    {
        // Interest
        if (state.HasActiveNegotiation(_candidateId))
        {
            InterestCardText  = "Negotiating";
            InterestCardClass = "cdm-status-card--warning";
        }
        else if (_candidate.IsTargeted)
        {
            InterestCardText  = "Interested";
            InterestCardClass = "cdm-status-card--success";
        }
        else
        {
            InterestCardText  = "Available";
            InterestCardClass = "";
        }

        // Salary
        bool salaryVis = SalaryDemandCalculator.IsSalaryRevealed(_candidate);
        if (salaryVis)
        {
            int demand = state.GetEffectiveSalaryDemand(_candidateId);
            SalaryCardText  = UIFormatting.FormatMoney(demand);
            SalaryCardClass = "";
        }
        else
        {
            SalaryCardText  = "???";
            SalaryCardClass = "";
        }

        // Potential
        if (IsPotentialRevealed)
        {
            int paStars = state.GetPotentialStarEstimate(_candidateId);
            PotentialCardText  = paStars > 0 ? paStars + "\u2605" : "?\u2605";
            PotentialCardClass = paStars >= 4 ? "cdm-status-card--success" :
                                 paStars <= 2 ? "cdm-status-card--warning" : "";
        }
        else
        {
            PotentialCardText  = "?\u2605";
            PotentialCardClass = "";
        }

        // Risk
        if (IsPersonalityRevealed)
        {
            int ego = _candidate.Stats.GetHiddenAttribute(HiddenAttributeId.Ego);
            int loyalty = _candidate.Stats.GetHiddenAttribute(HiddenAttributeId.Loyalty);
            bool highRisk = ego >= 16 || loyalty <= 5;
            RiskCardText  = highRisk ? "High" : "Low";
            RiskCardClass = highRisk ? "cdm-status-card--danger" : "cdm-status-card--success";
        }
        else
        {
            RiskCardText  = "Unknown";
            RiskCardClass = "";
        }

        // Availability
        if (_candidate.ExpiryTick == int.MaxValue)
        {
            AvailabilityCardText  = "Permanent";
            AvailabilityCardClass = "cdm-status-card--success";
        }
        else if (_candidate.ExpiryTick > 0)
        {
            int daysLeft = (_candidate.ExpiryTick - currentTick) / TimeState.TicksPerDay;
            if (daysLeft < 0) daysLeft = 0;
            AvailabilityCardText = daysLeft + "d";
            AvailabilityCardClass = daysLeft < 3 ? "cdm-status-card--danger" :
                                    daysLeft < 7 ? "cdm-status-card--warning" : "";
        }
        else
        {
            AvailabilityCardText  = "\u2014";
            AvailabilityCardClass = "";
        }

        // Interview
        if (interviewDone)
        {
            InterviewCardText  = "Complete";
            InterviewCardClass = "cdm-status-card--success";
        }
        else if (inProgress)
        {
            InterviewCardText  = (int)knowledge + "%";
            InterviewCardClass = "cdm-status-card--warning";
        }
        else
        {
            InterviewCardText  = "None";
            InterviewCardClass = "";
        }

        // Team impact
        TeamImpactCardText  = "\u2014";
        TeamImpactCardClass = "";

        // Comparison
        ComparisonCardText  = "\u2014";
        ComparisonCardClass = "";
    }

    // ── Action visibility refresh ────────────────────────────────────────────

    private void RefreshActionVisibility(IReadOnlyGameState state, float knowledge,
                                          bool inProgress, bool interviewDone)
    {
        IsShortlisted    = _candidate.IsTargeted;
        IsOfferOnCooldown = state.IsOfferOnCooldown(_candidateId);
        HasPendingCounter = state.HasPendingCounterOffer(_candidateId);

        if (interviewDone)
        {
            InterviewButtonText    = "Interviewed";
            InterviewButtonEnabled = false;
        }
        else if (inProgress)
        {
            int pct = (int)knowledge;
            InterviewButtonText    = "Interviewing... " + pct + "%";
            InterviewButtonEnabled = false;
        }
        else
        {
            bool canInterview = state.CanStartInterview(_candidateId,
                _candidate.IsTargeted ? HiringMode.HR : HiringMode.Manual);
            InterviewButtonText    = "Start Interview";
            InterviewButtonEnabled = canInterview;
        }
    }

    // ── Offer state (backward compat) ────────────────────────────────────────

    private void RefreshOfferState(IReadOnlyGameState state)
    {
        HasActiveNegotiation = state.HasActiveNegotiation(_candidateId);
        MaxPatience          = state.GetCandidateMaxPatience(_candidateId);
        CurrentPatience      = state.GetCandidateCurrentPatience(_candidateId);

        SelectedRole = _candidate.Role;

        bool salaryVisible = SalaryDemandCalculator.IsSalaryRevealed(_candidate);
        int salaryDemand = state.GetEffectiveSalaryDemand(_candidateId);
        int marketRate = SalaryBand.GetBase(_candidate.Role);

        int anchor = salaryVisible ? salaryDemand : marketRate;
        SalarySliderAnchor = anchor;
        SalarySliderMin = SalaryDemandCalculator.Round50(anchor * 0.5f);
        if (SalarySliderMin < MinimumWage) SalarySliderMin = MinimumWage;
        SalarySliderMax = SalaryDemandCalculator.Round50(anchor * 1.5f);

        if (CurrentOfferSalary < SalarySliderMin || CurrentOfferSalary > SalarySliderMax)
            CurrentOfferSalary = anchor;

        AcceptanceChance     = 0f;
        AcceptanceChanceText = "???";
        AcceptanceChanceClass = "acceptance-bar--medium";
    }

    // ── Comparison tab refresh ───────────────────────────────────────────────

    private static readonly string[] _comparisonTargetLabels = {
        "Same Role Employees",
        "Current Team Average",
        "Role Market Average"
    };

    private void RefreshComparisonData(IReadOnlyGameState state)
    {
        string[] labels = _comparisonTargetLabels;
        int targetIndex = Mathf.Clamp(ComparisonTargetIndex, 0, labels.Length - 1);

        string targetHeader;
        ComparisonMetricRow[] metrics = BuildComparisonMetrics(state, targetIndex, out targetHeader);
        string recommendation = BuildComparisonRecommendation(metrics);

        Comparison = new ComparisonTabData
        {
            TargetLabels        = labels,
            SelectedTargetIndex = targetIndex,
            Metrics             = metrics,
            RecommendationText  = recommendation,
            TargetColumnHeader  = targetHeader
        };
    }

    private ComparisonMetricRow[] BuildComparisonMetrics(IReadOnlyGameState state, int targetIndex,
                                                          out string targetHeader)
    {
        float knowledge = KnowledgePercent;
        int candidateAbility = 0;
        int candidateSalary  = state.GetEffectiveSalaryDemand(_candidateId);
        int marketRate       = SalaryBand.GetBase(_candidate.Role);

        if (IsSkillsRevealed && _candidate.Stats.Skills != null)
            candidateAbility = state.ComputeAbilityForRole(_candidate.Stats.Skills, _candidate.Role);

        string abilityText = IsSkillsRevealed ? candidateAbility.ToString() : "~?";
        string salaryText  = SalaryDemandCalculator.IsSalaryRevealed(_candidate)
            ? UIFormatting.FormatMoney(candidateSalary) + "/mo"
            : "~" + UIFormatting.FormatMoney(marketRate) + "/mo";

        int paStars = IsPotentialRevealed ? state.GetPotentialStarEstimate(_candidateId) : 0;
        string paText = IsPotentialRevealed ? paStars + "\u2605" : "?";

        int caStar = IsAbilityRevealed ? state.GetAbilityStarEstimate(_candidateId) : 0;
        string caText = IsAbilityRevealed ? caStar + "\u2605" : "?";

        // Gather target values
        int targetAbility = 0;
        int targetSalary  = marketRate;
        int targetCA      = 0;
        int targetPA      = 0;
        int comparatorCount = 0;

        switch (targetIndex)
        {
            case 0: // Same Role Employees
            {
                targetHeader = "Same Role Avg";
                var employees = state.ActiveEmployees;
                int ec = employees.Count;
                for (int i = 0; i < ec; i++)
                {
                    var emp = employees[i];
                    if (emp.role != _candidate.Role) continue;
                    if (emp.Stats.Skills != null)
                    {
                        targetAbility += state.ComputeAbilityForRole(emp.Stats.Skills, emp.role);
                        targetCA      += AbilityToStars(state.ComputeAbilityForRole(emp.Stats.Skills, emp.role));
                    }
                    targetSalary  += emp.salary;
                    comparatorCount++;
                }
                if (comparatorCount > 0)
                {
                    targetAbility /= comparatorCount;
                    targetSalary  /= comparatorCount;
                    targetCA      /= comparatorCount;
                }
                break;
            }
            case 1: // Current Team Average
            {
                targetHeader = "Team Average";
                var employees = state.ActiveEmployees;
                int ec = employees.Count;
                for (int i = 0; i < ec; i++)
                {
                    var emp = employees[i];
                    if (emp.Stats.Skills != null)
                    {
                        int ab = state.ComputeAbilityForRole(emp.Stats.Skills, emp.role);
                        targetAbility += ab;
                        targetCA      += AbilityToStars(ab);
                    }
                    targetSalary  += emp.salary;
                    comparatorCount++;
                }
                if (comparatorCount > 0)
                {
                    targetAbility /= comparatorCount;
                    targetSalary  /= comparatorCount;
                    targetCA      /= comparatorCount;
                }
                break;
            }
            default: // Role Market Average
            {
                targetHeader  = "Market Average";
                targetAbility = 8;
                targetSalary  = marketRate;
                targetCA      = 3;
                targetPA      = 3;
                break;
            }
        }

        string targetAbilityText = comparatorCount > 0 || targetIndex == 2 ? targetAbility.ToString() : "\u2014";
        string targetSalaryText  = UIFormatting.FormatMoney(targetSalary) + "/mo";
        string targetCAText      = comparatorCount > 0 || targetIndex == 2 ? targetCA + "\u2605" : "\u2014";

        var rows = new ComparisonMetricRow[5];

        rows[0] = MakeComparisonRow("Role Fit Ability", abilityText, targetAbilityText,
            candidateAbility, targetAbility, IsSkillsRevealed);
        rows[1] = MakeComparisonRow("CA (Stars)", caText, targetCAText,
            caStar, targetCA, IsAbilityRevealed);
        rows[2] = MakeComparisonRow("PA (Stars)", paText,
            targetIndex == 2 ? targetPA + "\u2605" : "\u2014",
            paStars, targetPA, IsPotentialRevealed);
        rows[3] = MakeComparisonRow("Salary Demand", salaryText, targetSalaryText,
            -candidateSalary, -targetSalary, true);
        rows[4] = new ComparisonMetricRow
        {
            MetricName           = "Confidence",
            CandidateValueText   = FormatConfidenceLabel(ComputeOverallConfidence(knowledge)),
            ComparisonValueText  = comparatorCount > 0 ? "Confirmed" : "Estimated",
            DeltaText            = "\u2014",
            DeltaClass           = "cdm-comparison-delta--neutral",
            ConfidenceClass      = ConfidenceCssClass(ComputeOverallConfidence(knowledge))
        };

        return rows;
    }

    private static ComparisonMetricRow MakeComparisonRow(string name,
        string candidateText, string targetText,
        int candidateVal, int targetVal, bool confident)
    {
        int delta = candidateVal - targetVal;
        string deltaText;
        string deltaClass;

        if (!confident || targetVal == 0)
        {
            deltaText  = "\u2014";
            deltaClass = "cdm-comparison-delta--neutral";
        }
        else if (delta > 0)
        {
            deltaText  = "+" + delta;
            deltaClass = "cdm-comparison-delta--positive";
        }
        else if (delta < 0)
        {
            deltaText  = delta.ToString();
            deltaClass = "cdm-comparison-delta--negative";
        }
        else
        {
            deltaText  = "=";
            deltaClass = "cdm-comparison-delta--neutral";
        }

        return new ComparisonMetricRow
        {
            MetricName          = name,
            CandidateValueText  = candidateText,
            ComparisonValueText = targetText,
            DeltaText           = deltaText,
            DeltaClass          = deltaClass,
            ConfidenceClass     = confident ? "confidence--medium" : "confidence--unknown"
        };
    }

    private static string BuildComparisonRecommendation(ComparisonMetricRow[] metrics)
    {
        if (metrics == null || metrics.Length == 0)
            return "No comparison data available.";

        int positiveDeltas = 0;
        int negativeDeltas = 0;

        for (int i = 0; i < metrics.Length - 1; i++)
        {
            if (metrics[i].DeltaClass == "cdm-comparison-delta--positive") positiveDeltas++;
            if (metrics[i].DeltaClass == "cdm-comparison-delta--negative") negativeDeltas++;
        }

        if (positiveDeltas >= 3) return "Candidate compares favourably. Strong relative profile.";
        if (negativeDeltas >= 3) return "Candidate falls below comparison target on most metrics.";
        return "Mixed comparison — candidate offers trade-offs versus target.";
    }

    // ── Offer tab refresh ────────────────────────────────────────────────────

    private void RefreshOfferTabData(IReadOnlyGameState state)
    {
        bool salaryRevealed = SalaryDemandCalculator.IsSalaryRevealed(_candidate);
        int salaryDemand    = state.GetEffectiveSalaryDemand(_candidateId);
        int marketRate      = SalaryBand.GetBase(_candidate.Role);
        float knowledge     = KnowledgePercent;

        // Demand state label
        string demandStateText;
        string salaryDisplayText;
        if (salaryRevealed)
        {
            demandStateText  = "Revealed";
            salaryDisplayText = UIFormatting.FormatMoney(salaryDemand) + "/mo";
        }
        else if (knowledge >= 60f)
        {
            demandStateText  = "High Confidence";
            int spread = (int)(marketRate * 0.1f);
            salaryDisplayText = "~" + UIFormatting.FormatMoney(marketRate - spread) + " – " +
                                UIFormatting.FormatMoney(marketRate + spread) + "/mo";
        }
        else if (knowledge > 0f)
        {
            demandStateText  = "Estimated";
            int spread = (int)(marketRate * 0.2f);
            salaryDisplayText = "~" + UIFormatting.FormatMoney(marketRate - spread) + " – " +
                                UIFormatting.FormatMoney(marketRate + spread) + "/mo";
        }
        else
        {
            demandStateText  = "Unknown";
            salaryDisplayText = "Interview required";
        }

        // Monthly cost = salary demand (or market estimate)
        int monthlyCost = salaryRevealed ? salaryDemand : marketRate;
        string monthlyCostText = UIFormatting.FormatMoney(monthlyCost) + "/mo";

        // Runway impact
        int currentCash = state.Money;
        string runwayImpactText;
        string runwayImpactClass;
        if (currentCash > 0 && monthlyCost > 0)
        {
            int monthsOfRunway = currentCash / monthlyCost;
            runwayImpactText  = "-" + UIFormatting.FormatMoney(monthlyCost) + "/mo (" + monthsOfRunway + " mo runway)";
            runwayImpactClass = monthsOfRunway < 3 ? "runway-impact--danger" :
                                monthsOfRunway < 6 ? "runway-impact--warning" : "";
        }
        else
        {
            runwayImpactText  = "\u2014";
            runwayImpactClass = "";
        }

        // Acceptance chance and factors
        string acceptanceLabel;
        string acceptanceClass;
        var factors = BuildAcceptanceFactors(state, salaryDemand, salaryRevealed, knowledge,
                                             out acceptanceLabel, out acceptanceClass);

        // Negotiation state
        NegotiationStatus negStatus = state.GetNegotiationStatus(_candidateId);
        bool hasNeg         = state.HasActiveNegotiation(_candidateId);
        bool hasCounter     = state.HasPendingCounterOffer(_candidateId);
        bool onCooldown     = state.IsOfferOnCooldown(_candidateId);
        bool hardRejected   = state.IsCandidateHardRejected(_candidateId);
        bool interviewDone  = state.IsFinalReportReady(_candidateId);
        bool inProgress     = state.IsInterviewInProgress(_candidateId);

        bool canMakeOffer    = !hasNeg && !onCooldown && !hardRejected &&
                               !inProgress && (interviewDone || knowledge > 0f);
        bool canHire         = hasNeg && negStatus == NegotiationStatus.Accepted;
        bool canWithdraw     = hasNeg && negStatus == NegotiationStatus.Pending;
        bool canAcceptCounter = hasCounter;
        bool canRejectCounter = hasCounter;

        // Status text
        string statusText;
        string statusClass;
        if (hardRejected)
        {
            statusText  = "This candidate has declined and is temporarily unavailable for offers.";
            statusClass = "cdm-offer__status-label--danger";
        }
        else if (negStatus == NegotiationStatus.Accepted)
        {
            statusText  = "Offer accepted! Proceed to hire this candidate.";
            statusClass = "cdm-offer__status-label--success";
        }
        else if (negStatus == NegotiationStatus.PatienceExhausted)
        {
            statusText  = "Candidate lost patience and withdrew.";
            statusClass = "cdm-offer__status-label--danger";
        }
        else if (hasNeg && negStatus == NegotiationStatus.Pending)
        {
            statusText  = "Offer pending. Waiting for candidate response.";
            statusClass = "cdm-offer__status-label--warning";
        }
        else if (hasCounter)
        {
            statusText  = "Candidate has submitted a counter-offer.";
            statusClass = "cdm-offer__status-label--warning";
        }
        else if (!interviewDone && knowledge == 0f)
        {
            statusText  = "Interview recommended before making an offer.";
            statusClass = "cdm-offer__status-label";
        }
        else
        {
            statusText  = "";
            statusClass = "cdm-offer__status-label";
        }

        Offer = new OfferTabData
        {
            DemandStateText      = demandStateText,
            SalaryEstimateText   = salaryDisplayText,
            AcceptanceChanceLabel = acceptanceLabel,
            AcceptanceChanceClass = acceptanceClass,
            Factors              = factors,
            MonthlyCostText      = monthlyCostText,
            RunwayImpactText     = runwayImpactText,
            RunwayImpactClass    = runwayImpactClass,
            CanMakeOffer         = canMakeOffer,
            CanHire              = canHire,
            CanWithdrawOffer     = canWithdraw,
            CanAcceptCounter     = canAcceptCounter,
            CanRejectCounter     = canRejectCounter,
            StatusText           = statusText,
            StatusClass          = statusClass
        };
    }

    private AcceptanceFactor[] BuildAcceptanceFactors(IReadOnlyGameState state,
        int salaryDemand, bool salaryRevealed, float knowledge,
        out string acceptanceLabel, out string acceptanceClass)
    {
        int score = 50; // base
        var factors = new System.Collections.Generic.List<AcceptanceFactor>(6);

        // Recruitment reputation
        int recScore = state.RecruitmentScore;
        string repImpact;
        string repClass;
        if (recScore >= 70)      { repImpact = "Strong"; repClass = "factor--positive"; score += 15; }
        else if (recScore >= 40) { repImpact = "Neutral"; repClass = "factor--neutral"; }
        else                     { repImpact = "Weak"; repClass = "factor--negative"; score -= 10; }
        factors.Add(new AcceptanceFactor { Name = "Company Reputation", ValueText = repImpact, ImpactClass = repClass });

        // Salary match (if revealed)
        if (salaryRevealed)
        {
            int marketRate = SalaryBand.GetBase(_candidate.Role);
            float ratio = marketRate > 0 ? (float)salaryDemand / marketRate : 1f;
            string salaryImpact;
            string salaryClass;
            if (ratio <= 0.9f)      { salaryImpact = "Under Market"; salaryClass = "factor--negative"; score -= 15; }
            else if (ratio <= 1.05f){ salaryImpact = "At Market"; salaryClass = "factor--positive"; score += 10; }
            else                    { salaryImpact = "Above Market"; salaryClass = "factor--positive"; score += 20; }
            factors.Add(new AcceptanceFactor { Name = "Salary vs Market", ValueText = salaryImpact, ImpactClass = salaryClass });
        }
        else
        {
            factors.Add(new AcceptanceFactor { Name = "Salary vs Market", ValueText = "Unknown", ImpactClass = "factor--neutral" });
        }

        // Role fit
        if (IsAbilityRevealed && _candidate.Stats.Skills != null)
        {
            int ability = state.ComputeAbilityForRole(_candidate.Stats.Skills, _candidate.Role);
            string fitImpact;
            string fitClass;
            if (ability >= 12)      { fitImpact = "Good Fit"; fitClass = "factor--positive"; score += 5; }
            else if (ability >= 7)  { fitImpact = "Adequate"; fitClass = "factor--neutral"; }
            else                    { fitImpact = "Weak Fit"; fitClass = "factor--negative"; score -= 5; }
            factors.Add(new AcceptanceFactor { Name = "Role Fit", ValueText = fitImpact, ImpactClass = fitClass });
        }
        else
        {
            factors.Add(new AcceptanceFactor { Name = "Role Fit", ValueText = "Unknown", ImpactClass = "factor--neutral" });
        }

        // Loyalty / Ambition
        if (IsPersonalityRevealed)
        {
            int loyalty = _candidate.Stats.GetHiddenAttribute(HiddenAttributeId.Loyalty);
            string loyaltyText = loyalty >= 12 ? "High" : loyalty >= 7 ? "Medium" : "Low";
            string loyaltyClass = loyalty >= 12 ? "factor--positive" :
                                  loyalty >= 7  ? "factor--neutral"  : "factor--negative";
            score += loyalty >= 12 ? 5 : loyalty < 7 ? -5 : 0;
            factors.Add(new AcceptanceFactor { Name = "Candidate Loyalty", ValueText = loyaltyText, ImpactClass = loyaltyClass });

            int ambition = _candidate.Stats.GetHiddenAttribute(HiddenAttributeId.Ambition);
            string ambitionText = ambition >= 14 ? "Very Ambitious" : ambition >= 9 ? "Moderate" : "Low";
            string ambitionClass = ambition >= 14 ? "factor--negative" : "factor--neutral";
            score += ambition >= 14 ? -5 : 0;
            factors.Add(new AcceptanceFactor { Name = "Ambition", ValueText = ambitionText, ImpactClass = ambitionClass });
        }
        else
        {
            factors.Add(new AcceptanceFactor { Name = "Candidate Loyalty", ValueText = "Unknown", ImpactClass = "factor--neutral" });
            factors.Add(new AcceptanceFactor { Name = "Ambition", ValueText = "Unknown", ImpactClass = "factor--neutral" });
        }

        score = Mathf.Clamp(score, 0, 100);

        if (score >= 80)      { acceptanceLabel = "Very Likely";   acceptanceClass = "offer-acceptance--very-likely"; }
        else if (score >= 60) { acceptanceLabel = "Likely";        acceptanceClass = "offer-acceptance--likely"; }
        else if (score >= 40) { acceptanceLabel = "Medium";        acceptanceClass = "offer-acceptance--medium"; }
        else if (score >= 20) { acceptanceLabel = "Unlikely";      acceptanceClass = "offer-acceptance--unlikely"; }
        else                  { acceptanceLabel = "Very Unlikely"; acceptanceClass = "offer-acceptance--very-unlikely"; }

        return factors.ToArray();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private ConfidenceLevel GetSkillConfidence(int skillIndex)
    {
        if (_candidate.SkillConfidence != null &&
            skillIndex < _candidate.SkillConfidence.Length)
        {
            return _candidate.SkillConfidence[skillIndex];
        }

        // Fallback based on knowledge
        if (IsSkillsRevealed) return ConfidenceLevel.Medium;
        if (KnowledgePercent > 0f) return ConfidenceLevel.Low;
        return ConfidenceLevel.Unknown;
    }

    private static void ComputeEstimatedRange(int value, ConfidenceLevel conf,
                                                out string displayText, out string displayClass)
    {
        switch (conf)
        {
            case ConfidenceLevel.Confirmed:
                displayText  = value.ToString();
                displayClass = "confidence--confirmed";
                break;
            case ConfidenceLevel.High:
                int hMin = Mathf.Max(0, value - 1);
                int hMax = Mathf.Min(20, value + 1);
                displayText  = hMin + "-" + hMax;
                displayClass = "confidence--high";
                break;
            case ConfidenceLevel.Medium:
                int mMin = Mathf.Max(0, value - 2);
                int mMax = Mathf.Min(20, value + 2);
                displayText  = mMin + "-" + mMax;
                displayClass = "confidence--medium";
                break;
            case ConfidenceLevel.Low:
                int lMin = Mathf.Max(0, value - 4);
                int lMax = Mathf.Min(20, value + 4);
                displayText  = lMin + "-" + lMax;
                displayClass = "confidence--low";
                break;
            default:
                displayText  = "Unknown";
                displayClass = "cdm-range-value--unknown";
                break;
        }
    }

    private static int AbilityToStars(int ability)
    {
        if (ability >= 20) return 5;
        if (ability >= 15) return 4;
        if (ability >= 10) return 3;
        if (ability >= 5)  return 2;
        return 1;
    }

    private static string GetAttributeLabel(int value, int spread)
    {
        int mid = value;
        if (mid >= 16) return "High";
        if (mid >= 12) return "Above Average";
        if (mid >= 8)  return "Average";
        if (mid >= 4)  return "Below Average";
        return "Low";
    }

    private static ConfidenceLevel ComputeOverallConfidence(float knowledge)
    {
        if (knowledge >= 100f) return ConfidenceLevel.Confirmed;
        if (knowledge >= 80f)  return ConfidenceLevel.High;
        if (knowledge >= 40f)  return ConfidenceLevel.Medium;
        if (knowledge > 0f)    return ConfidenceLevel.Low;
        return ConfidenceLevel.Unknown;
    }

    private static string FormatConfidenceLabel(ConfidenceLevel level)
    {
        switch (level)
        {
            case ConfidenceLevel.Confirmed: return "Confirmed";
            case ConfidenceLevel.High:      return "High";
            case ConfidenceLevel.Medium:    return "Medium";
            case ConfidenceLevel.Low:       return "Low";
            default:                        return "Unknown";
        }
    }

    private static string ConfidenceCssClass(ConfidenceLevel level)
    {
        switch (level)
        {
            case ConfidenceLevel.Confirmed: return "confidence--confirmed";
            case ConfidenceLevel.High:      return "confidence--high";
            case ConfidenceLevel.Medium:    return "confidence--medium";
            case ConfidenceLevel.Low:       return "confidence--low";
            default:                        return "confidence--unknown";
        }
    }

    private static string ConfidenceDotClass(ConfidenceLevel level)
    {
        switch (level)
        {
            case ConfidenceLevel.Confirmed: return "confidence-dot--confirmed";
            case ConfidenceLevel.High:      return "confidence-dot--high";
            case ConfidenceLevel.Medium:    return "confidence-dot--medium";
            case ConfidenceLevel.Low:       return "confidence-dot--low";
            default:                        return "confidence-dot--unknown";
        }
    }

    private CandidateData FindCandidate(IReadOnlyGameState state)
    {
        var available = state.AvailableCandidates;
        int count = available.Count;
        for (int i = 0; i < count; i++)
        {
            if (available[i].CandidateId == _candidateId) return available[i];
        }
        var pending = state.PendingReviewCandidates;
        int pCount = pending.Count;
        for (int i = 0; i < pCount; i++)
        {
            if (pending[i].CandidateId == _candidateId) return pending[i];
        }
        return null;
    }
}
