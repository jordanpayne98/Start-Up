using System;
using System.Collections.Generic;
using UnityEngine;

public class EmployeeDetailModalViewModel : IViewModel
{
    // ── Structs ───────────────────────────────────────────────────────────────

    public struct BadgeData
    {
        public string Label;
        public string UssClass;
    }

    // ── Overview tab data structs ─────────────────────────────────────────────

    public struct RoleFitEntry
    {
        public RoleId RoleId;
        public string RoleName;
        public RoleSuitability Suitability;
        public string SuitabilityClass;
        public int AbilityScore;
        public bool IsCurrentRole;
    }

    public struct RoleFitData
    {
        public RoleFitEntry[] TopRoleFits;
        public string CurrentRoleFitLabel;
        public int CurrentCA;
        public string BestRoleName;
        public int BestCA;
        public string SuggestionText;
        public string TeamName;
        public string TeamFitLabel;
        public string AssignmentText;
        public string[] TeamFitNotes;
    }

    public struct SkillDisplayEntry
    {
        public SkillId SkillId;
        public string Name;
        public int Value;
        public sbyte DeltaDirection;
    }

    public struct SkillSummaryData
    {
        public SkillDisplayEntry[] CoreSkills;
        public SkillDisplayEntry[] SupportingSkills;
    }

    public struct AttributeDisplayEntry
    {
        public VisibleAttributeId AttributeId;
        public string Name;
        public int Value;
    }

    public struct VisibleAttributeData
    {
        public AttributeDisplayEntry[] Attributes;
    }

    public struct SignalEntry
    {
        public string Name;
        public string Label;
        public string LabelClass;
    }

    public struct HiddenSignalReportData
    {
        public string PersonalityLabel;
        public string PersonalityClass;
        public SignalEntry[] Signals;
        public string SalaryBenchmarkNote;
        public string ContractRiskNote;
        public string RecommendationText;
    }

    public struct TeamImpactEntry
    {
        public string MeterName;
        public int Delta;
    }

    public struct TeamImpactData
    {
        public TeamImpactEntry[] Deltas;
        public bool HasTeam;
    }

    // ── Personal tab data structs ─────────────────────────────────────────────

    public struct DetailedSignalEntry
    {
        public string Name;
        public string Label;
        public string LabelClass;
        public string ConfidenceText;
    }

    public struct PersonalTabData
    {
        public string PersonalityLabel;
        public string PersonalityClass;
        public string PersonalityDescription;
        public DetailedSignalEntry[] Signals;
        public string RetentionRiskLabel;
        public string RetentionRiskClass;
        public string[] RetentionReasons;
        public string MoraleStatus;
        public string MoraleClass;
        public string StressStatus;
        public string StressClass;
        public bool   ShowBurnoutWarning;
        public bool   IsFounder;
        public string FounderArchetype;
        public string FounderTrait;
        public string FounderWeakness;
    }

    // ── Performance tab data structs ──────────────────────────────────────────

    public struct WorkHistoryDisplayEntry
    {
        public string DateText;
        public string WorkTypePillText;
        public string WorkTypePillClass;
        public string WorkName;
        public string ContributionLabel;
        public string ContributionClass;
        public string QualityText;
        public string XpSummary;
        public string OutcomeLabel;
        public string OutcomeClass;
    }

    public struct PerformanceTabData
    {
        public string FormLabel;
        public string FormClass;
        public WorkHistoryDisplayEntry[] RecentWork;
        public string AverageOutput;
        public string AverageQuality;
        public string BugContribution;
        public bool   HasWorkHistory;
    }

    // ── Growth tab data structs ───────────────────────────────────────────────

    public struct SkillGrowthEntry
    {
        public SkillId SkillId;
        public string  Name;
        public int     Value;
        public float   XpPercent;
        public sbyte   DeltaDirection;
        public bool    IsCategoryHeader;
        public string  CategoryHeaderText;
    }

    public struct AttributeTrendEntry
    {
        public string Name;
        public string TrendLabel;
        public string TrendClass;
    }

    public struct GrowthTabData
    {
        public int    CurrentRoleCA;
        public int    BestRoleCA;
        public int    PA;
        public int    PADistance;
        public string GrowthOutlookLabel;
        public string GrowthOutlookClass;
        public SkillGrowthEntry[] AllSkills;
        public AttributeTrendEntry[] AttributeTrends;
        public string AgeEffect;
        public string MentoringInfluence;
        public string PlateauWarning;
    }

    // ── Career tab data structs ───────────────────────────────────────────────

    public enum CareerEventType
    {
        Hired         = 0,
        ProductShipped = 1,
        ContractDone  = 2,
        TeamChange    = 3,
        SalaryChange  = 4,
    }

    public struct CareerTimelineEntry
    {
        public string DateText;
        public CareerEventType EventType;
        public string TypePillText;
        public string TypePillClass;
        public string Title;
        public string Subtitle;
    }

    public struct CareerTabData
    {
        public int DaysEmployed;
        public int ProductsShipped;
        public int ContractsCompleted;
        public string AverageQuality;
        public string TotalSalaryPaid;
        public int SkillIncreases;
        public CareerTimelineEntry[] Timeline;
        public bool HasCareerHistory;
    }

    // ── Comparison tab data structs ───────────────────────────────────────────

    public struct ComparisonTarget
    {
        public int Index;
        public string DisplayName;
    }

    public struct ComparisonMetricRow
    {
        public string MetricName;
        public string EmployeeValue;
        public string ComparisonValue;
        public string DifferenceText;
        public string DifferenceClass;
    }

    public struct ComparisonTabData
    {
        public ComparisonTarget[] AvailableTargets;
        public int SelectedTargetIndex;
        public ComparisonMetricRow[] Metrics;
    }



    private EmployeeId _employeeId;

    // ── Header ────────────────────────────────────────────────────────────────

    public string Name                { get; private set; }
    public int    Age                 { get; private set; }
    public string RoleName            { get; private set; }
    public string RoleFamilyName      { get; private set; }
    public string SeniorityLabel      { get; private set; }
    public string RolePillClass       { get; private set; }
    public bool   IsFounder           { get; private set; }

    public string TeamName            { get; private set; }
    public string EmploymentType      { get; private set; }
    public string SalaryText          { get; private set; }
    public string BenchmarkLabel      { get; private set; }
    public string BenchmarkClass      { get; private set; }
    public string ContractExpiryText  { get; private set; }

    public string MoraleLabel         { get; private set; }
    public string MoraleClass         { get; private set; }
    public string CAStarsText         { get; private set; }
    public string PAStarsText         { get; private set; }

    public List<BadgeData> BadgeList  { get; private set; } = new List<BadgeData>(6);

    // ── Tab ───────────────────────────────────────────────────────────────────

    public int ActiveTabIndex         { get; set; }

    // ── Bottom status cards ───────────────────────────────────────────────────

    public string MoraleCardText       { get; private set; }
    public string MoraleCardClass      { get; private set; }
    public string StressCardText       { get; private set; }
    public string StressCardClass      { get; private set; }
    public string FormCardText         { get; private set; }
    public string FormCardClass        { get; private set; }
    public string GrowthCardText       { get; private set; }
    public string GrowthCardClass      { get; private set; }
    public string SalaryCardText       { get; private set; }
    public string SalaryCardClass      { get; private set; }
    public string ContractCardText     { get; private set; }
    public string ContractCardClass    { get; private set; }
    public string TeamImpactCardText   { get; private set; }
    public string TeamImpactCardClass  { get; private set; }
    public string RecentWorkCardText   { get; private set; }
    public string RecentWorkCardClass  { get; private set; }

    // ── Action visibility ────────────────────────────────────────────────────

    public bool ShowAssignTeam      { get; private set; }
    public bool ShowRenewContract   { get; private set; }
    public bool ShowFireEmployee    { get; private set; }
    public bool ShowCompare         { get; private set; }
    public bool IsInactiveEmployee  { get; private set; }

    // ── Legacy compat (used by existing sub-views) ────────────────────────────

    public EmployeeId CurrentEmployeeId  => _employeeId;
    public IReadOnlyGameState LastState  { get; private set; }

    // Renewal negotiation (used by ContractRenewalModalView)
    public bool   HasActiveRenewalNegotiation { get; private set; }
    public bool   HasPendingRenewalCounter    { get; private set; }
    public int    RenewalMaxPatience          { get; private set; }
    public int    RenewalCurrentPatience      { get; private set; }
    public bool   IsOnCooldown                { get; private set; }
    public string CooldownText                { get; private set; }
    public string RenewalDemandText           { get; private set; }
    public string CurrentSalaryText           { get; private set; }
    public string CounterSalaryText           { get; private set; }
    public string CounterRoleName             { get; private set; }
    public string CounterTypeName             { get; private set; }
    public string CounterLengthText           { get; private set; }
    public string OriginalSalaryText          { get; private set; }

    // Role suitability (used by overview tab builder)
    private readonly RoleSuitabilityEntry[] _roleSuitabilities =
        new RoleSuitabilityEntry[RoleSuitabilityCalculator.AllRoles.Length];
    public RoleSuitabilityEntry[] RoleSuitabilities => _roleSuitabilities;
    public bool   IsWorkingOffRole    { get; private set; }
    public string PreferredRoleName   { get; private set; }
    public string AssignedRoleName    { get; private set; }

    // Skill table (used by overview tab builder)
    private readonly SkillTableEntry[] _skillTable =
        new SkillTableEntry[SkillIdHelper.SkillCount];
    public SkillTableEntry[] SkillTable => _skillTable;

    // Personality (used by overview tab right panel)
    public string PersonalityText  { get; private set; }
    public string PersonalityClass { get; private set; }

    // Contract detail rows (used by overview right panel)
    public string ContractType   { get; private set; }
    public string ContractLength { get; private set; }
    public string RemainingText  { get; private set; }
    public string HiredDateText  { get; private set; }
    public string MarketRateText { get; private set; }
    public string ValueEfficiencyText { get; private set; }
    public string MarketPositionText  { get; private set; }
    public string MarketPositionClass { get; private set; }

    // ── Overview tab data ────────────────────────────────────────────────────

    public RoleFitData      OverviewRoleFit      { get; private set; }
    public SkillSummaryData OverviewSkills       { get; private set; }
    public VisibleAttributeData OverviewAttributes { get; private set; }
    public HiddenSignalReportData OverviewReport  { get; private set; }
    public TeamImpactData   OverviewTeamImpact   { get; private set; }

    // ── Secondary tab data ───────────────────────────────────────────────────

    public PersonalTabData     Personal     { get; private set; }
    public PerformanceTabData  Performance  { get; private set; }
    public GrowthTabData       Growth       { get; private set; }
    public CareerTabData       Career       { get; private set; }
    public ComparisonTabData   Comparison   { get; private set; }

    private int _comparisonTargetIndex;

    // Lazy-loaded role profile table for skill partitioning
    private static RoleProfileTable _cachedProfileTable;
    private static RoleProfileTable GetProfileTable()
    {
        if (_cachedProfileTable == null)
        {
            _cachedProfileTable = new RoleProfileTable();
            var profiles = Resources.LoadAll<RoleProfileDefinition>("RoleProfiles");
            for (int i = 0; i < profiles.Length; i++)
                _cachedProfileTable.Register(profiles[i]);
        }
        return _cachedProfileTable;
    }

    // ── IViewModel dirty tracking ────────────────────────────────────────────

    public bool IsDirty { get; private set; }
    public void ClearDirty() => IsDirty = false;

    // ── API ───────────────────────────────────────────────────────────────────

    public void SetEmployeeId(EmployeeId id)
    {
        _employeeId = id;
    }

    public void Refresh(GameStateSnapshot snapshot)
    {
        if (snapshot == null) return;
        LastState = snapshot;

        var emp = FindEmployee(snapshot);
        if (emp == null) return;

        RefreshHeader(emp, snapshot);
        RefreshBadges(emp, snapshot);
        RefreshRoleSuitability(emp, snapshot);
        RefreshSkillTable(emp);
        RefreshContractDetail(emp, snapshot);
        RefreshPersonality(emp, snapshot);
        RefreshBottomStatusCards(emp, snapshot);
        RefreshActionVisibility(emp, snapshot);
        RefreshRenewalNegotiation(emp, snapshot);
        RefreshOverviewTabData(emp, snapshot);
        RefreshPersonalTab(emp, snapshot);
        RefreshPerformanceTab(emp, snapshot);
        RefreshGrowthTab(emp, snapshot);
        RefreshCareerTab(emp, snapshot);
        RefreshComparisonTab(emp, snapshot);
        IsDirty = true;
    }

    // ── Stub methods for Plans 2B-2D ─────────────────────────────────────────

    public void RefreshPersonalTabData(IReadOnlyGameState state)
    {
        var emp = FindEmployee(state);
        if (emp == null) return;
        RefreshPersonalTab(emp, state);
    }

    public void RefreshPerformanceTabData(IReadOnlyGameState state)
    {
        var emp = FindEmployee(state);
        if (emp == null) return;
        RefreshPerformanceTab(emp, state);
    }

    public void RefreshGrowthTabData(IReadOnlyGameState state)
    {
        var emp = FindEmployee(state);
        if (emp == null) return;
        RefreshGrowthTab(emp, state);
    }

    public void RefreshCareerTabData(IReadOnlyGameState state)
    {
        var emp = FindEmployee(state);
        if (emp == null) return;
        RefreshCareerTab(emp, state);
    }

    public void RefreshComparisonTabData(IReadOnlyGameState state)
    {
        var emp = FindEmployee(state);
        if (emp == null) return;
        RefreshComparisonTab(emp, state);
    }

    public void SetComparisonTarget(int targetIndex)
    {
        _comparisonTargetIndex = targetIndex;
    }

    // ── Private refresh methods ───────────────────────────────────────────────

    private void RefreshHeader(Employee emp, IReadOnlyGameState state)
    {
        Name         = emp.name;
        Age          = emp.age;
        RoleName     = UIFormatting.FormatRole(emp.role);
        RolePillClass = UIFormatting.RolePillClass(emp.role);
        RoleFamilyName = RoleIdHelper.GetFamily(emp.role).ToString();
        IsFounder    = emp.isFounder;

        // Seniority — derive from CA stars or a simple age/salary heuristic
        SeniorityLabel = DeriveSeiorityLabel(emp);

        // Team
        TeamName = "--";
        var teamId = state.GetEmployeeTeam(emp.id);
        if (teamId.HasValue)
        {
            var teams = state.ActiveTeams;
            int tc = teams.Count;
            for (int t = 0; t < tc; t++)
            {
                if (teams[t].id == teamId.Value)
                {
                    TeamName = teams[t].name;
                    break;
                }
            }
        }

        // Contract / salary
        if (emp.isFounder)
        {
            EmploymentType   = "Founder";
            SalaryText       = "$0/mo";
            ContractExpiryText = "Permanent";
        }
        else
        {
            EmploymentType = emp.Contract.Type == global::EmploymentType.FullTime
                ? "Full-Time"
                : "Part-Time";
            SalaryText = UIFormatting.FormatMoney(emp.salary) + "/mo";

            if (emp.contractExpiryTick <= 0)
            {
                ContractExpiryText = "--";
            }
            else
            {
                int daysLeft = (emp.contractExpiryTick - state.CurrentTick) / TimeState.TicksPerDay;
                if (daysLeft < 0) daysLeft = 0;
                ContractExpiryText = daysLeft + "d";
            }
        }

        // Salary benchmark
        int marketRate = SalaryBand.GetBase(emp.role);
        if (emp.isFounder)
        {
            BenchmarkLabel = "Founder";
            BenchmarkClass = "badge--neutral";
        }
        else
        {
            float ratio = marketRate > 0 ? (float)emp.salary / marketRate : 1f;
            if      (ratio <= 0.80f) { BenchmarkLabel = "Far Below Market"; BenchmarkClass = "badge--danger";  }
            else if (ratio <= 0.93f) { BenchmarkLabel = "Below Market";     BenchmarkClass = "badge--warning"; }
            else if (ratio <= 1.07f) { BenchmarkLabel = "At Market";        BenchmarkClass = "badge--neutral"; }
            else if (ratio <= 1.20f) { BenchmarkLabel = "Above Market";     BenchmarkClass = "badge--success"; }
            else                     { BenchmarkLabel = "Well Above Market"; BenchmarkClass = "badge--accent";  }
        }

        // Morale
        int morale = emp.morale;
        MoraleLabel = morale + "%";
        if      (morale >= 70) MoraleClass = "text-success";
        else if (morale >= 40) MoraleClass = "text-warning";
        else                   MoraleClass = "text-danger";

        // CA / PA stars (from ability computed for preferred role)
        int ca = emp.Stats.Skills != null
            ? state.ComputeAbilityForRole(emp.Stats.Skills, emp.role)
            : 0;
        CAStarsText = FormatAbilityStars(ca);
        int pa = emp.Stats.PotentialAbility;
        PAStarsText = pa > 0 ? FormatAbilityStars(pa) : "—";
    }

    private void RefreshBadges(Employee emp, IReadOnlyGameState state)
    {
        BadgeList.Clear();

        if (emp.isFounder)
            BadgeList.Add(new BadgeData { Label = "Founder", UssClass = "badge--founder" });

        // Contract expiring soon (< 30 days)
        if (!emp.isFounder && emp.contractExpiryTick > 0)
        {
            int daysLeft = (emp.contractExpiryTick - state.CurrentTick) / TimeState.TicksPerDay;
            if (daysLeft < 30)
                BadgeList.Add(new BadgeData { Label = "Expiring Soon", UssClass = "badge--warning" });
        }

        // Salary risk
        int marketRate = SalaryBand.GetBase(emp.role);
        if (!emp.isFounder && marketRate > 0)
        {
            float ratio = (float)emp.salary / marketRate;
            if (ratio <= 0.80f)
                BadgeList.Add(new BadgeData { Label = "Underpaid", UssClass = "badge--danger" });
        }

        // Burnout risk — low morale
        if (emp.morale <= 30)
            BadgeList.Add(new BadgeData { Label = "At Risk", UssClass = "badge--danger" });
        else if (emp.morale <= 45)
            BadgeList.Add(new BadgeData { Label = "Low Morale", UssClass = "badge--warning" });

        // Improving / declining via skill delta
        if (emp.Stats.SkillDeltaDirection != null)
        {
            int ups = 0, downs = 0;
            int deltaLen = emp.Stats.SkillDeltaDirection.Length;
            for (int i = 0; i < deltaLen; i++)
            {
                sbyte d = emp.Stats.SkillDeltaDirection[i];
                if (d > 0) ups++;
                else if (d < 0) downs++;
            }
            if (ups > downs && ups >= 2)
                BadgeList.Add(new BadgeData { Label = "Improving", UssClass = "badge--success" });
            else if (downs > ups && downs >= 2)
                BadgeList.Add(new BadgeData { Label = "Declining", UssClass = "badge--danger" });
        }
    }

    private void RefreshRoleSuitability(Employee emp, IReadOnlyGameState state)
    {
        AssignedRoleName  = UIFormatting.FormatRole(emp.role);
        PreferredRoleName = UIFormatting.FormatRole(emp.preferredRole);
        IsWorkingOffRole  = emp.role != emp.preferredRole;

        var allRoles  = RoleSuitabilityCalculator.AllRoles;
        int roleCount = allRoles.Length;
        for (int i = 0; i < roleCount; i++)
        {
            var role      = allRoles[i];
            int ability   = 0;
            RoleSuitability suitability = RoleSuitability.Unsuitable;
            if (emp.Stats.Skills != null)
            {
                ability    = state.ComputeAbilityForRole(emp.Stats.Skills, role);
                suitability = RoleSuitabilityCalculator.GetSuitability(ability);
            }
            _roleSuitabilities[i] = new RoleSuitabilityEntry
            {
                Role            = role,
                Suitability     = suitability,
                AbilityForRole  = ability,
                RoleName        = UIFormatting.FormatRole(role),
                SuitabilityClass = UIFormatting.SuitabilityDotClass(suitability),
                IsPreferred     = role == emp.preferredRole
            };
        }
    }

    private void RefreshSkillTable(Employee emp)
    {
        int skillCount = SkillIdHelper.SkillCount;
        for (int i = 0; i < skillCount; i++)
        {
            var skillType = (SkillId)i;
            int level = emp.GetSkill(skillType);
            sbyte delta = (emp.Stats.SkillDeltaDirection != null && emp.Stats.SkillDeltaDirection.Length > i)
                ? emp.Stats.SkillDeltaDirection[i]
                : (sbyte)0;

            string valueText;
            string valueClass;
            if (delta > 0)      { valueText = level + " \u25B2"; valueClass = "skill-row__value--up";   }
            else if (delta < 0) { valueText = level + " \u25BC"; valueClass = "skill-row__value--down"; }
            else                { valueText = level.ToString();    valueClass = "";                      }

            _skillTable[i] = new SkillTableEntry
            {
                Name       = SkillIdHelper.GetName(skillType),
                ValueText  = valueText,
                ValueClass = valueClass,
                NameColor  = UIFormatting.GetSkillColor(skillType)
            };
        }
    }

    private void RefreshContractDetail(Employee emp, IReadOnlyGameState state)
    {
        bool isFounder = emp.isFounder;
        if (isFounder)
        {
            ContractType   = "Founder";
            ContractLength = "Permanent";
            RemainingText  = "Permanent";
        }
        else
        {
            ContractType   = emp.Contract.Type == global::EmploymentType.FullTime ? "Full-Time" : "Part-Time";
            ContractLength = FormatLength(emp.Contract.Length);

            if (emp.contractExpiryTick <= 0)
            {
                RemainingText = "--";
            }
            else
            {
                int daysLeft = (emp.contractExpiryTick - state.CurrentTick) / TimeState.TicksPerDay;
                if (daysLeft < 0) daysLeft = 0;
                RemainingText = daysLeft + "d remaining";
            }
        }

        HiredDateText = "Day " + (emp.hireDate / TimeState.TicksPerDay);

        int marketRate = SalaryBand.GetBase(emp.role);
        MarketRateText = UIFormatting.FormatMoney(marketRate) + "/mo";

        if (isFounder)
        {
            MarketPositionText  = "Founder";
            MarketPositionClass = "badge--neutral";
            ValueEfficiencyText = "N/A";
        }
        else
        {
            float ratio = marketRate > 0 ? (float)emp.salary / marketRate : 1f;
            if      (ratio <= 0.80f) { MarketPositionText = "Far Below Market"; MarketPositionClass = "badge--danger";  }
            else if (ratio <= 0.93f) { MarketPositionText = "Below Market";     MarketPositionClass = "badge--warning"; }
            else if (ratio <= 1.07f) { MarketPositionText = "At Market";        MarketPositionClass = "badge--neutral"; }
            else if (ratio <= 1.20f) { MarketPositionText = "Above Market";     MarketPositionClass = "badge--success"; }
            else                     { MarketPositionText = "Well Above Market"; MarketPositionClass = "badge--accent";  }

            float valueEff = (marketRate > 0 && emp.salary > 0)
                ? (float)marketRate / emp.salary * 100f
                : 0f;
            ValueEfficiencyText = emp.salary <= 0
                ? "N/A"
                : System.Math.Round(valueEff).ToString("F0") + "%";
        }
    }

    private void RefreshPersonality(Employee emp, IReadOnlyGameState state)
    {
        var personality = state.GetEmployeePersonality(emp.id);
        PersonalityText  = UIFormatting.FormatPersonality(personality);
        PersonalityClass = UIFormatting.PersonalityBadgeClass(personality);
    }

    private void RefreshBottomStatusCards(Employee emp, IReadOnlyGameState state)
    {
        // Morale card
        int morale = emp.morale;
        MoraleCardText = morale + "%";
        MoraleCardClass = morale >= 70 ? "edm-status-card--success"
                        : morale >= 40 ? "edm-status-card--warning"
                        :                "edm-status-card--danger";

        // Stress card (use energy band as proxy until dedicated stress field exists)
        var energyBand = state.GetEmployeeEnergyBand(emp.id);
        StressCardText  = UIFormatting.FormatEnergyBand(energyBand);
        StressCardClass = UIFormatting.EnergyBandClass(energyBand) == "energy-band--exhausted"
                        ? "edm-status-card--danger"
                        : UIFormatting.EnergyBandClass(energyBand) == "energy-band--drained"
                        ? "edm-status-card--warning"
                        : "";

        // Form card — based on skill delta trend
        int ups = 0, downs = 0;
        if (emp.Stats.SkillDeltaDirection != null)
        {
            int deltaLen = emp.Stats.SkillDeltaDirection.Length;
            for (int i = 0; i < deltaLen; i++)
            {
                sbyte d = emp.Stats.SkillDeltaDirection[i];
                if (d > 0) ups++;
                else if (d < 0) downs++;
            }
        }
        if      (ups > downs && ups >= 2) { FormCardText = "Rising";   FormCardClass = "edm-status-card--success"; }
        else if (downs > ups && downs >= 2) { FormCardText = "Falling"; FormCardClass = "edm-status-card--danger";  }
        else                                { FormCardText = "Stable";  FormCardClass = ""; }

        // Growth card — CA indicator
        GrowthCardText  = CAStarsText;
        GrowthCardClass = "";

        // Salary card — benchmark shorthand
        SalaryCardText  = BenchmarkLabel;
        SalaryCardClass = BenchmarkClass switch
        {
            "badge--danger"  => "edm-status-card--danger",
            "badge--warning" => "edm-status-card--warning",
            "badge--success" => "edm-status-card--success",
            _                => ""
        };

        // Contract card — days remaining
        ContractCardText  = ContractExpiryText;
        ContractCardClass = emp.isFounder ? "" :
            emp.contractExpiryTick > 0 && (emp.contractExpiryTick - state.CurrentTick) / TimeState.TicksPerDay < 30
                ? "edm-status-card--warning"
                : "";

        // Team impact — simple team membership check
        TeamImpactCardText  = string.IsNullOrEmpty(TeamName) || TeamName == "--" ? "No Team" : TeamName;
        TeamImpactCardClass = "";

        // Recent work — stub for Plan 2B
        RecentWorkCardText  = "—";
        RecentWorkCardClass = "";
    }

    private void RefreshActionVisibility(Employee emp, IReadOnlyGameState state)
    {
        IsInactiveEmployee = false;
        ShowAssignTeam    = !state.GetEmployeeTeam(emp.id).HasValue;
        ShowRenewContract = !emp.isFounder;
        ShowFireEmployee  = !emp.isFounder;
        ShowCompare       = true;
    }

    private void RefreshRenewalNegotiation(Employee emp, IReadOnlyGameState state)
    {
        CurrentSalaryText = UIFormatting.FormatMoney(emp.salary) + "/mo";

        int renewalBase = emp.renewalDemand > 0 ? emp.renewalDemand : emp.salary;
        RenewalDemandText = UIFormatting.FormatMoney(renewalBase) + "/mo";

        HasActiveRenewalNegotiation = false;
        HasPendingRenewalCounter    = false;
        RenewalMaxPatience          = 0;
        RenewalCurrentPatience      = 0;
        IsOnCooldown                = false;
        CooldownText                = "";
        CounterSalaryText           = "";
        CounterRoleName             = "";
        CounterTypeName             = "";
        CounterLengthText           = "";
        OriginalSalaryText          = "";

        if (emp.isFounder) return;

        var empNeg = state.GetEmployeeNegotiation(emp.id);
        if (!empNeg.HasValue) return;

        var neg = empNeg.Value;
        HasActiveRenewalNegotiation = neg.status == NegotiationStatus.Pending
            || neg.status == NegotiationStatus.CounterOffered;
        HasPendingRenewalCounter = neg.hasCounterOffer
            && neg.status == NegotiationStatus.CounterOffered;
        RenewalMaxPatience      = neg.maxPatience;
        RenewalCurrentPatience  = neg.currentPatience;
        IsOnCooldown            = state.IsEmployeeOnNegotiationCooldown(emp.id);

        if (IsOnCooldown && neg.cooldownExpiryTick > 0)
        {
            int daysLeft = (neg.cooldownExpiryTick - state.CurrentTick) / TimeState.TicksPerDay;
            if (daysLeft < 0) daysLeft = 0;
            CooldownText = daysLeft + " days remaining";
        }

        if (HasPendingRenewalCounter)
        {
            var counter   = neg.counterOffer;
            CounterSalaryText  = UIFormatting.FormatMoney(counter.CounterSalary) + "/mo";
            CounterRoleName    = UIFormatting.FormatRole(counter.CounterRole);
            CounterTypeName    = counter.CounterType == global::EmploymentType.FullTime ? "Full-Time" : "Part-Time";
            CounterLengthText  = FormatLength(counter.CounterLength);
            OriginalSalaryText = UIFormatting.FormatMoney(neg.lastOffer.OfferedSalary) + "/mo";
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void RefreshOverviewTabData(Employee emp, IReadOnlyGameState state)
    {
        RefreshOverviewRoleFit(emp, state);
        RefreshOverviewSkills(emp);
        RefreshOverviewAttributes(emp);
        RefreshOverviewReport(emp, state);
        RefreshOverviewTeamImpact(emp, state);
    }

    private void RefreshOverviewRoleFit(Employee emp, IReadOnlyGameState state)
    {
        var allRoles = RoleSuitabilityCalculator.AllRoles;
        int roleCount = allRoles.Length;

        // Build scored list for all roles
        var scored = new (RoleId role, int ability, RoleSuitability suit)[roleCount];
        for (int i = 0; i < roleCount; i++)
        {
            int ability = emp.Stats.Skills != null
                ? state.ComputeAbilityForRole(emp.Stats.Skills, allRoles[i])
                : 0;
            scored[i] = (allRoles[i], ability, RoleSuitabilityCalculator.GetSuitability(ability));
        }

        // Sort descending by ability
        Array.Sort(scored, (a, b) => b.ability.CompareTo(a.ability));

        // Top 5
        int topCount = Math.Min(5, roleCount);
        var topFits = new RoleFitEntry[topCount];
        for (int i = 0; i < topCount; i++)
        {
            topFits[i] = new RoleFitEntry
            {
                RoleId          = scored[i].role,
                RoleName        = UIFormatting.FormatRole(scored[i].role),
                Suitability     = scored[i].suit,
                SuitabilityClass = UIFormatting.SuitabilityDotClass(scored[i].suit),
                AbilityScore    = scored[i].ability,
                IsCurrentRole   = scored[i].role == emp.role
            };
        }

        // Current role stats
        int currentCA = emp.Stats.Skills != null
            ? state.ComputeAbilityForRole(emp.Stats.Skills, emp.role)
            : 0;
        var currentSuit = RoleSuitabilityCalculator.GetSuitability(currentCA);
        string currentFitLabel = currentSuit.ToString();

        // Best role
        string bestRoleName = topFits.Length > 0 ? topFits[0].RoleName : "—";
        int bestCA = topFits.Length > 0 ? topFits[0].AbilityScore : 0;

        // Suggestion
        string suggestion = "";
        if (topFits.Length > 0 && topFits[0].RoleId != emp.role && bestCA > currentCA + 20)
            suggestion = $"Consider reassigning to {bestRoleName} for +{bestCA - currentCA} CA.";

        // Team fit
        string teamName = TeamName;
        string teamFitLabel = "—";
        string assignmentText = "";
        string[] teamFitNotes = Array.Empty<string>();

        var teamId = state.GetEmployeeTeam(emp.id);
        if (teamId.HasValue)
        {
            var chemistry = state.GetTeamChemistry(teamId.Value);
            teamFitLabel = UIFormatting.FormatChemistryBand(chemistry.Band);

            int projectedChange = state.GetProjectedChemistryChange(teamId.Value, emp.id);
            if (projectedChange != 0)
                assignmentText = $"Chemistry impact: {(projectedChange > 0 ? "+" : "")}{projectedChange}";

            var notes = new List<string>();
            if (emp.role != emp.preferredRole)
                notes.Add($"Working off-role (prefers {UIFormatting.FormatRole(emp.preferredRole)})");
            if (emp.morale <= 45)
                notes.Add("Low morale may affect team output");
            teamFitNotes = notes.ToArray();
        }
        else
        {
            teamFitLabel = "Unassigned";
            assignmentText = "Not currently on a team.";
        }

        OverviewRoleFit = new RoleFitData
        {
            TopRoleFits       = topFits,
            CurrentRoleFitLabel = currentFitLabel,
            CurrentCA         = currentCA,
            BestRoleName      = bestRoleName,
            BestCA            = bestCA,
            SuggestionText    = suggestion,
            TeamName          = teamName,
            TeamFitLabel      = teamFitLabel,
            AssignmentText    = assignmentText,
            TeamFitNotes      = teamFitNotes
        };
    }

    private void RefreshOverviewSkills(Employee emp)
    {
        var profileTable = GetProfileTable();
        var profile = profileTable.Get(emp.role);

        var coreList = new List<SkillDisplayEntry>(8);
        var supportList = new List<SkillDisplayEntry>(8);

        if (profile != null && profile.SkillBands != null)
        {
            int skillCount = SkillIdHelper.SkillCount;
            for (int i = 0; i < skillCount; i++)
            {
                if (i >= profile.SkillBands.Length) continue;
                var band = profile.SkillBands[i];
                if (band == RoleWeightBand.Primary || band == RoleWeightBand.Secondary)
                {
                    coreList.Add(MakeSkillEntry(emp, (SkillId)i));
                }
                else if (band == RoleWeightBand.Tertiary)
                {
                    supportList.Add(MakeSkillEntry(emp, (SkillId)i));
                }
            }
        }

        // Sort core: Primary first (by band priority), then by value descending
        if (profile != null)
        {
            coreList.Sort((a, b) =>
            {
                var bandA = profile.SkillBands[(int)a.SkillId];
                var bandB = profile.SkillBands[(int)b.SkillId];
                int bandCmp = bandA.CompareTo(bandB); // Primary < Secondary
                if (bandCmp != 0) return bandCmp;
                return b.Value.CompareTo(a.Value);
            });
        }

        // Sort supporting by value descending
        supportList.Sort((a, b) => b.Value.CompareTo(a.Value));

        OverviewSkills = new SkillSummaryData
        {
            CoreSkills       = coreList.ToArray(),
            SupportingSkills = supportList.ToArray()
        };
    }

    private static SkillDisplayEntry MakeSkillEntry(Employee emp, SkillId skillId)
    {
        int idx = (int)skillId;
        int value = emp.Stats.Skills != null && idx < emp.Stats.Skills.Length
            ? emp.Stats.Skills[idx] : 0;
        sbyte delta = emp.Stats.SkillDeltaDirection != null && idx < emp.Stats.SkillDeltaDirection.Length
            ? emp.Stats.SkillDeltaDirection[idx] : (sbyte)0;
        return new SkillDisplayEntry
        {
            SkillId        = skillId,
            Name           = SkillIdHelper.GetName(skillId),
            Value          = value,
            DeltaDirection = delta
        };
    }

    private void RefreshOverviewAttributes(Employee emp)
    {
        int attrCount = VisibleAttributeHelper.AttributeCount;
        var attrs = new AttributeDisplayEntry[attrCount];
        for (int i = 0; i < attrCount; i++)
        {
            var attrId = (VisibleAttributeId)i;
            attrs[i] = new AttributeDisplayEntry
            {
                AttributeId = attrId,
                Name        = VisibleAttributeHelper.GetName(attrId),
                Value       = emp.Stats.GetVisibleAttribute(attrId)
            };
        }
        OverviewAttributes = new VisibleAttributeData { Attributes = attrs };
    }

    private void RefreshOverviewReport(Employee emp, IReadOnlyGameState state)
    {
        var personality = state.GetEmployeePersonality(emp.id);

        // Hidden signals — never show exact values, only labels
        int hiddenCount = HiddenAttributeHelper.AttributeCount;
        var signals = new SignalEntry[hiddenCount];
        for (int i = 0; i < hiddenCount; i++)
        {
            var attrId = (HiddenAttributeId)i;
            int value = emp.Stats.GetHiddenAttribute(attrId);
            string label = HiddenAttributeHelper.GetLabel(value);
            signals[i] = new SignalEntry
            {
                Name       = HiddenAttributeHelper.GetName(attrId),
                Label      = label,
                LabelClass = GetSignalLabelClass(label)
            };
        }

        // Salary benchmark note
        int marketRate = SalaryBand.GetBase(emp.role);
        string salaryNote = "";
        if (!emp.isFounder && marketRate > 0)
        {
            float ratio = (float)emp.salary / marketRate;
            if (ratio <= 0.80f) salaryNote = "Significantly underpaid — high quit risk.";
            else if (ratio <= 0.93f) salaryNote = "Below market rate — may affect morale.";
            else if (ratio >= 1.20f) salaryNote = "Well above market — consider renegotiation.";
        }

        // Contract risk note
        string contractNote = "";
        if (!emp.isFounder && emp.contractExpiryTick > 0)
        {
            int daysLeft = (emp.contractExpiryTick - state.CurrentTick) / TimeState.TicksPerDay;
            if (daysLeft < 14) contractNote = "Contract expires very soon — renew urgently.";
            else if (daysLeft < 30) contractNote = "Contract expiring within a month.";
        }

        // Recommendation
        string recommendation = BuildRecommendation(emp, state);

        OverviewReport = new HiddenSignalReportData
        {
            PersonalityLabel    = UIFormatting.FormatPersonality(personality),
            PersonalityClass    = UIFormatting.PersonalityBadgeClass(personality),
            Signals             = signals,
            SalaryBenchmarkNote = salaryNote,
            ContractRiskNote    = contractNote,
            RecommendationText  = recommendation
        };
    }

    private static string GetSignalLabelClass(string label)
    {
        switch (label)
        {
            case "Exceptional": return "signal--exceptional";
            case "High":        return "signal--high";
            case "Average":     return "signal--average";
            case "Low":         return "signal--low";
            case "Very Low":    return "signal--very-low";
            default:            return "";
        }
    }

    private string BuildRecommendation(Employee emp, IReadOnlyGameState state)
    {
        // Check for role mismatch
        if (OverviewRoleFit.TopRoleFits != null && OverviewRoleFit.TopRoleFits.Length > 0
            && OverviewRoleFit.TopRoleFits[0].RoleId != emp.role
            && OverviewRoleFit.BestCA > OverviewRoleFit.CurrentCA + 20)
        {
            return $"Strongest fit is {OverviewRoleFit.BestRoleName}. Consider reassignment.";
        }

        if (emp.morale <= 30)
            return "Morale critically low. Address grievances or risk departure.";

        if (!emp.isFounder)
        {
            int marketRate = SalaryBand.GetBase(emp.role);
            if (marketRate > 0 && (float)emp.salary / marketRate <= 0.80f)
                return "Severely underpaid — high attrition risk.";
        }

        return "";
    }

    private void RefreshOverviewTeamImpact(Employee emp, IReadOnlyGameState state)
    {
        var teamId = state.GetEmployeeTeam(emp.id);
        if (!teamId.HasValue)
        {
            OverviewTeamImpact = new TeamImpactData { HasTeam = false, Deltas = Array.Empty<TeamImpactEntry>() };
            return;
        }

        // Chemistry with vs projected without
        var chemistry = state.GetTeamChemistry(teamId.Value);
        int projectedChange = state.GetProjectedChemistryChange(teamId.Value, emp.id);

        var deltas = new TeamImpactEntry[]
        {
            new TeamImpactEntry { MeterName = "Chemistry", Delta = projectedChange }
        };

        OverviewTeamImpact = new TeamImpactData { HasTeam = true, Deltas = deltas };
    }

    // ── Personal tab refresh ──────────────────────────────────────────────────

    private void RefreshPersonalTab(Employee emp, IReadOnlyGameState state)
    {
        var personality = state.GetEmployeePersonality(emp.id);
        string personalityLabel = UIFormatting.FormatPersonality(personality);
        string personalityClass = UIFormatting.PersonalityBadgeClass(personality);
        string personalityDesc  = BuildPersonalityDescription(personality);

        // Detailed hidden signals (employees always High/Confirmed)
        int hiddenCount = HiddenAttributeHelper.AttributeCount;
        var signals = new DetailedSignalEntry[hiddenCount];
        for (int i = 0; i < hiddenCount; i++)
        {
            var attrId = (HiddenAttributeId)i;
            int value = emp.Stats.GetHiddenAttribute(attrId);
            string label = HiddenAttributeHelper.GetLabel(value);
            signals[i] = new DetailedSignalEntry
            {
                Name           = HiddenAttributeHelper.GetName(attrId),
                Label          = label,
                LabelClass     = GetSignalLabelClass(label),
                ConfidenceText = "Confirmed"
            };
        }

        // Retention risk
        ComputeRetentionRisk(emp, out string riskLabel, out string riskClass, out string[] riskReasons);

        // Morale / stress / burnout
        int morale = emp.morale;
        string moraleStatus = morale >= 70 ? "Good" : morale >= 40 ? "Moderate" : "Low";
        string moraleClass  = morale >= 70 ? "text-success" : morale >= 40 ? "text-warning" : "text-danger";

        var energyBand = state.GetEmployeeEnergyBand(emp.id);
        string stressStatus = UIFormatting.FormatEnergyBand(energyBand);
        string stressClass  = "";
        bool showBurnout    = false;
        if (energyBand == EnergyBand.Exhausted)
        {
            stressClass  = "text-danger";
            showBurnout  = true;
        }
        else if (energyBand == EnergyBand.Drained)
        {
            stressClass  = "text-warning";
        }

        // Founder traits
        string founderArchetype = "—";
        string founderTrait     = "—";
        string founderWeakness  = "—";
        if (emp.isFounder)
        {
            founderArchetype = UIFormatting.FormatRole(emp.preferredRole) + " Specialist";
            founderTrait     = DeriveFounderTrait(emp);
            founderWeakness  = DeriveFounderWeakness(emp);
        }

        Personal = new PersonalTabData
        {
            PersonalityLabel       = personalityLabel,
            PersonalityClass       = personalityClass,
            PersonalityDescription = personalityDesc,
            Signals                = signals,
            RetentionRiskLabel     = riskLabel,
            RetentionRiskClass     = riskClass,
            RetentionReasons       = riskReasons,
            MoraleStatus           = moraleStatus,
            MoraleClass            = moraleClass,
            StressStatus           = stressStatus,
            StressClass            = stressClass,
            ShowBurnoutWarning     = showBurnout,
            IsFounder              = emp.isFounder,
            FounderArchetype       = founderArchetype,
            FounderTrait           = founderTrait,
            FounderWeakness        = founderWeakness
        };
    }

    private void ComputeRetentionRisk(
        Employee emp,
        out string riskLabel,
        out string riskClass,
        out string[] riskReasons)
    {
        var reasons = new List<string>(4);
        int riskScore = 0;

        int marketRate = SalaryBand.GetBase(emp.role);
        if (!emp.isFounder && marketRate > 0)
        {
            float ratio = (float)emp.salary / marketRate;
            if (ratio <= 0.80f)
            {
                riskScore += 3;
                reasons.Add("Salary is far below market rate");
            }
            else if (ratio <= 0.93f)
            {
                riskScore += 1;
                reasons.Add("Salary is below market rate");
            }
        }

        // Ambition hidden attribute
        int ambition = emp.Stats.GetHiddenAttribute(HiddenAttributeId.Ambition);
        if (ambition >= 15) { riskScore += 2; reasons.Add("Ambition appears high"); }
        else if (ambition >= 11) { riskScore += 1; reasons.Add("Ambition appears moderate"); }

        // Morale
        if (emp.morale <= 30) { riskScore += 2; reasons.Add("Morale is critically low"); }
        else if (emp.morale <= 50) { riskScore += 1; reasons.Add("Morale is below average"); }
        else if (emp.morale >= 75) reasons.Add("Morale is stable");

        // Loyalty hidden attribute
        int loyalty = emp.Stats.GetHiddenAttribute(HiddenAttributeId.Loyalty);
        if (loyalty >= 13) reasons.Add("Loyalty appears high");
        else if (loyalty <= 6) { riskScore += 1; reasons.Add("Loyalty appears low"); }

        if (riskScore <= 1)       { riskLabel = "Low";    riskClass = "edm-retention-risk--low"; }
        else if (riskScore <= 3)  { riskLabel = "Medium"; riskClass = "edm-retention-risk--medium"; }
        else if (riskScore <= 5)  { riskLabel = "High";   riskClass = "edm-retention-risk--high"; }
        else                      { riskLabel = "Severe"; riskClass = "edm-retention-risk--severe"; }

        riskReasons = reasons.ToArray();
    }

    private static string BuildPersonalityDescription(Personality personality)
    {
        switch (personality)
        {
            case Personality.Collaborative:  return "Team-first attitude. Elevates those around them.";
            case Personality.Professional:   return "Composed and focused. Gets on with the job.";
            case Personality.Easygoing:      return "Relaxed and adaptable. Low conflict risk.";
            case Personality.Independent:    return "Prefers self-directed work. May resist micromanagement.";
            case Personality.Competitive:    return "Goal-oriented and highly motivated. May push hard.";
            case Personality.Perfectionist:  return "High standards. May slow delivery but raises quality.";
            case Personality.Intense:        return "Deeply focused. Works best under pressure with clear goals.";
            case Personality.Abrasive:       return "Strong opinions. May cause friction but can be effective.";
            case Personality.Volatile:       return "Talented but unpredictable. Requires careful management.";
            default:                         return "";
        }
    }

    private static string DeriveFounderTrait(Employee emp)
    {
        int creativity = emp.Stats.GetVisibleAttribute(VisibleAttributeId.Creativity);
        int leadership = emp.Stats.GetVisibleAttribute(VisibleAttributeId.Leadership);
        if (leadership >= creativity) return "Natural Leader";
        if (creativity > 14) return "Visionary";
        return "Specialist";
    }

    private static string DeriveFounderWeakness(Employee emp)
    {
        int comm  = emp.Stats.GetVisibleAttribute(VisibleAttributeId.Communication);
        int focus = emp.Stats.GetVisibleAttribute(VisibleAttributeId.Focus);
        if (comm < focus) return "Communication gaps under pressure";
        if (focus < 8)    return "Prone to losing focus";
        return "Perfectionist tendencies";
    }

    // ── Performance tab refresh ───────────────────────────────────────────────

    private void RefreshPerformanceTab(Employee emp, IReadOnlyGameState state)
    {
        var history = emp.WorkHistory;
        bool hasHistory = history != null && history.Count > 0;

        WorkHistoryDisplayEntry[] entries = Array.Empty<WorkHistoryDisplayEntry>();

        if (hasHistory)
        {
            int count = history.Count;
            entries = new WorkHistoryDisplayEntry[count];
            int totalQuality = 0;
            int completedCount = 0;

            for (int i = count - 1; i >= 0; i--)
            {
                var h = history[i];
                int displayIdx = (count - 1) - i;

                int dayNumber = h.CompletedTick / TimeState.TicksPerDay;
                string dateText = "Day " + dayNumber;

                string typePillText  = h.EntryType == WorkEntryType.Contract ? "Contract" : "Product";
                string typePillClass = h.EntryType == WorkEntryType.Contract
                    ? "edm-work-type--contract"
                    : "edm-work-type--product";

                string contribClass = h.ContributionLabel == "High"   ? "contribution--high"
                                    : h.ContributionLabel == "Medium" ? "contribution--medium"
                                    :                                   "contribution--low";

                string outcomeText  = h.Outcome == WorkOutcome.Completed ? "Completed"
                                    : h.Outcome == WorkOutcome.Cancelled ? "Cancelled"
                                    :                                      "Ongoing";
                string outcomeClass = h.Outcome == WorkOutcome.Completed ? "outcome--completed"
                                    : h.Outcome == WorkOutcome.Cancelled ? "outcome--cancelled"
                                    :                                      "";

                entries[displayIdx] = new WorkHistoryDisplayEntry
                {
                    DateText          = dateText,
                    WorkTypePillText  = typePillText,
                    WorkTypePillClass = typePillClass,
                    WorkName          = h.WorkName ?? "—",
                    ContributionLabel = h.ContributionLabel ?? "—",
                    ContributionClass = contribClass,
                    QualityText       = h.QualityScore > 0 ? "Q: " + h.QualityScore : "—",
                    XpSummary         = h.XpSummary ?? "",
                    OutcomeLabel      = outcomeText,
                    OutcomeClass      = outcomeClass
                };

                if (h.Outcome == WorkOutcome.Completed)
                {
                    totalQuality += h.QualityScore;
                    completedCount++;
                }
            }

            // Form: average quality of last 3-5 completed entries
            int formSamples = 0;
            int formQualitySum = 0;
            int startIdx = count - 1;
            for (int i = startIdx; i >= 0 && formSamples < 5; i--)
            {
                if (history[i].Outcome == WorkOutcome.Completed)
                {
                    formQualitySum += history[i].QualityScore;
                    formSamples++;
                }
            }

            string formLabel;
            string formClass;
            if (formSamples == 0)
            {
                formLabel = "No Data";
                formClass = "";
            }
            else
            {
                float avgQ = (float)formQualitySum / formSamples;
                if      (avgQ >= 85) { formLabel = "Strong";   formClass = "edm-form-score--strong"; }
                else if (avgQ >= 70) { formLabel = "Good";     formClass = "edm-form-score--good"; }
                else if (avgQ >= 50) { formLabel = "Average";  formClass = "edm-form-score--average"; }
                else if (avgQ >= 35) { formLabel = "Poor";     formClass = "edm-form-score--poor"; }
                else                 { formLabel = "Declining"; formClass = "edm-form-score--declining"; }
            }

            string avgOutput = "—";
            string avgQuality = "—";
            if (completedCount > 0)
            {
                avgOutput  = completedCount + " completed";
                avgQuality = ((float)totalQuality / completedCount).ToString("F0");
            }

            Performance = new PerformanceTabData
            {
                FormLabel      = formLabel,
                FormClass      = formClass,
                RecentWork     = entries,
                AverageOutput  = avgOutput,
                AverageQuality = avgQuality,
                BugContribution = "—",
                HasWorkHistory = true
            };
        }
        else
        {
            Performance = new PerformanceTabData
            {
                FormLabel      = "No Data",
                FormClass      = "",
                RecentWork     = entries,
                AverageOutput  = "—",
                AverageQuality = "—",
                BugContribution = "—",
                HasWorkHistory = false
            };
        }
    }

    // ── Growth tab refresh ────────────────────────────────────────────────────

    private void RefreshGrowthTab(Employee emp, IReadOnlyGameState state)
    {
        // CA / PA
        int currentCA = emp.Stats.Skills != null
            ? state.ComputeAbilityForRole(emp.Stats.Skills, emp.role)
            : 0;

        // Best role CA (already computed in overview)
        int bestCA = OverviewRoleFit.BestCA;

        int pa = emp.Stats.PotentialAbility;
        int paDistance = pa > currentCA ? pa - currentCA : 0;

        // Growth outlook
        string outlookLabel;
        string outlookClass;
        if (pa <= 0 || pa < currentCA + 10)
        {
            outlookLabel = "Plateauing";
            outlookClass = "growth-outlook--plateauing";
        }
        else if (paDistance >= 60)
        {
            outlookLabel = "Strong";
            outlookClass = "growth-outlook--strong";
        }
        else if (paDistance >= 30)
        {
            outlookLabel = "Good";
            outlookClass = "growth-outlook--good";
        }
        else
        {
            outlookLabel = "Plateauing";
            outlookClass = "growth-outlook--plateauing";
        }

        // Check for declining skill deltas
        int downCount = 0;
        if (emp.Stats.SkillDeltaDirection != null)
        {
            int len = emp.Stats.SkillDeltaDirection.Length;
            for (int i = 0; i < len; i++)
            {
                if (emp.Stats.SkillDeltaDirection[i] < 0) downCount++;
            }
        }
        if (downCount >= 3)
        {
            outlookLabel = "Declining";
            outlookClass = "growth-outlook--declining";
        }

        // All 26 skills with category headers
        int skillCount = SkillIdHelper.SkillCount;
        var skillEntries = new List<SkillGrowthEntry>(skillCount + 7);
        SkillCategory? lastCategory = null;

        for (int i = 0; i < skillCount; i++)
        {
            var skillId   = (SkillId)i;
            var category  = SkillIdHelper.GetCategory(skillId);

            if (lastCategory == null || category != lastCategory.Value)
            {
                skillEntries.Add(new SkillGrowthEntry
                {
                    IsCategoryHeader   = true,
                    CategoryHeaderText = FormatSkillCategory(category)
                });
                lastCategory = category;
            }

            int value = emp.Stats.Skills != null && i < emp.Stats.Skills.Length
                ? emp.Stats.Skills[i] : 0;
            float xpPct = emp.Stats.SkillXp != null && i < emp.Stats.SkillXp.Length
                ? Mathf.Clamp01(emp.Stats.SkillXp[i]) * 100f
                : 0f;
            sbyte delta = emp.Stats.SkillDeltaDirection != null && i < emp.Stats.SkillDeltaDirection.Length
                ? emp.Stats.SkillDeltaDirection[i] : (sbyte)0;

            skillEntries.Add(new SkillGrowthEntry
            {
                SkillId          = skillId,
                Name             = SkillIdHelper.GetName(skillId),
                Value            = value,
                XpPercent        = xpPct,
                DeltaDirection   = delta,
                IsCategoryHeader = false
            });
        }

        // Attribute trends — derive from delta directions and raw values
        int attrCount = VisibleAttributeHelper.AttributeCount;
        var attrTrends = new AttributeTrendEntry[attrCount];
        for (int i = 0; i < attrCount; i++)
        {
            var attrId = (VisibleAttributeId)i;
            int value  = emp.Stats.GetVisibleAttribute(attrId);
            string trend;
            string trendClass;
            if (value >= 14) { trend = "Improving";  trendClass = "attr-trend--improving"; }
            else if (value >= 9) { trend = "Unchanged"; trendClass = "attr-trend--unchanged"; }
            else { trend = "Uncertain"; trendClass = "attr-trend--uncertain"; }

            attrTrends[i] = new AttributeTrendEntry
            {
                Name       = VisibleAttributeHelper.GetName(attrId),
                TrendLabel = trend,
                TrendClass = trendClass
            };
        }

        // Growth factors
        string ageEffect = emp.age <= 30 ? "Peak growth years"
                         : emp.age <= 40 ? "Stable growth"
                         : emp.age <= 50 ? "Moderate slowdown"
                         :                 "Skill decay risk";

        int mentoring = emp.Stats.GetHiddenAttribute(HiddenAttributeId.Mentoring);
        string mentoringInfluence = mentoring >= 13 ? "Strong team benefit"
                                  : mentoring >= 9  ? "Moderate influence"
                                  :                   "Minimal influence";

        string plateauWarning = "";
        if (pa > 0 && paDistance < 15)
            plateauWarning = "Approaching potential ceiling. Growth may slow soon.";

        Growth = new GrowthTabData
        {
            CurrentRoleCA     = currentCA,
            BestRoleCA        = bestCA,
            PA                = pa,
            PADistance        = paDistance,
            GrowthOutlookLabel = outlookLabel,
            GrowthOutlookClass = outlookClass,
            AllSkills          = skillEntries.ToArray(),
            AttributeTrends    = attrTrends,
            AgeEffect          = ageEffect,
            MentoringInfluence = mentoringInfluence,
            PlateauWarning     = plateauWarning
        };
    }

    private static string FormatSkillCategory(SkillCategory cat)
    {
        switch (cat)
        {
            case SkillCategory.SoftwareEngineering:  return "SOFTWARE ENGINEERING";
            case SkillCategory.HardwareEngineering:  return "HARDWARE ENGINEERING";
            case SkillCategory.ProductAndUx:         return "PRODUCT & UX";
            case SkillCategory.CreativeProduction:   return "CREATIVE PRODUCTION";
            case SkillCategory.QualityAndDelivery:   return "QUALITY & DELIVERY";
            case SkillCategory.Commercial:           return "COMMERCIAL";
            case SkillCategory.CompanyOperations:    return "COMPANY OPERATIONS";
            default:                                 return cat.ToString().ToUpper();
        }
    }

    // ── Career tab refresh ────────────────────────────────────────────────────

    private void RefreshCareerTab(Employee emp, IReadOnlyGameState state)
    {
        int hireDay = emp.hireDate / TimeState.TicksPerDay;
        int currentDay = state.CurrentTick / TimeState.TicksPerDay;
        int daysEmployed = Math.Max(0, currentDay - hireDay);

        int productsShipped = 0;
        int contractsDone   = 0;
        int totalQuality    = 0;
        int qualityCount    = 0;
        int skillIncreases  = 0;

        var history = emp.WorkHistory;
        int histCount = history != null ? history.Count : 0;

        // Count up/down skill deltas as proxy for skill increases recorded
        if (emp.Stats.SkillDeltaDirection != null)
        {
            int len = emp.Stats.SkillDeltaDirection.Length;
            for (int i = 0; i < len; i++)
                if (emp.Stats.SkillDeltaDirection[i] > 0) skillIncreases++;
        }

        for (int i = 0; i < histCount; i++)
        {
            var h = history[i];
            if (h.EntryType == WorkEntryType.Product)  productsShipped++;
            if (h.EntryType == WorkEntryType.Contract) contractsDone++;
            if (h.Outcome == WorkOutcome.Completed && h.QualityScore > 0)
            {
                totalQuality += h.QualityScore;
                qualityCount++;
            }
        }

        int totalSalary = emp.salary * daysEmployed / 30;

        string avgQuality = qualityCount > 0 ? ((float)totalQuality / qualityCount).ToString("F0") : "—";
        string totalSalaryText = UIFormatting.FormatMoney(totalSalary);

        // Build timeline — Hired event first, then work history newest-first
        int timelineCapacity = 1 + histCount;
        var timeline = new List<CareerTimelineEntry>(timelineCapacity);

        // Hired entry
        timeline.Add(new CareerTimelineEntry
        {
            DateText     = "Day " + hireDay,
            EventType    = CareerEventType.Hired,
            TypePillText = "Hired",
            TypePillClass = "timeline-type--hired",
            Title        = emp.name,
            Subtitle     = UIFormatting.FormatRole(emp.role)
        });

        // Work history entries (newest first)
        for (int i = histCount - 1; i >= 0; i--)
        {
            var h = history[i];
            int dayNum = h.CompletedTick / TimeState.TicksPerDay;
            CareerEventType evtType;
            string pillText;
            string pillClass;
            if (h.EntryType == WorkEntryType.Product)
            {
                evtType   = CareerEventType.ProductShipped;
                pillText  = "Shipped";
                pillClass = "timeline-type--shipped";
            }
            else
            {
                evtType   = CareerEventType.ContractDone;
                pillText  = "Contract";
                pillClass = "timeline-type--contract";
            }

            string subtitle = h.Outcome == WorkOutcome.Completed ? "Completed" : "Cancelled";
            if (h.QualityScore > 0)
                subtitle += " · Q: " + h.QualityScore;

            timeline.Add(new CareerTimelineEntry
            {
                DateText      = "Day " + dayNum,
                EventType     = evtType,
                TypePillText  = pillText,
                TypePillClass = pillClass,
                Title         = h.WorkName ?? "—",
                Subtitle      = subtitle
            });
        }

        Career = new CareerTabData
        {
            DaysEmployed       = daysEmployed,
            ProductsShipped    = productsShipped,
            ContractsCompleted = contractsDone,
            AverageQuality     = avgQuality,
            TotalSalaryPaid    = totalSalaryText,
            SkillIncreases     = skillIncreases,
            Timeline           = timeline.ToArray(),
            HasCareerHistory   = histCount > 0
        };
    }

    // ── Comparison tab refresh ────────────────────────────────────────────────

    private void RefreshComparisonTab(Employee emp, IReadOnlyGameState state)
    {
        var targets = new ComparisonTarget[]
        {
            new ComparisonTarget { Index = 0, DisplayName = "Team Average" },
            new ComparisonTarget { Index = 1, DisplayName = "Role Company Avg" },
            new ComparisonTarget { Index = 2, DisplayName = "Role Market" },
        };

        int selectedIdx = Math.Max(0, Math.Min(_comparisonTargetIndex, targets.Length - 1));

        int empCA = emp.Stats.Skills != null
            ? state.ComputeAbilityForRole(emp.Stats.Skills, emp.role)
            : 0;
        int empPA     = emp.Stats.PotentialAbility;
        int empSalary = emp.salary;
        int empMorale = emp.morale;
        int empFit    = empCA;

        int benchCA     = 0;
        int benchPA     = 0;
        int benchSalary = 0;
        int benchMorale = 0;
        string benchmarkLabel = "—";

        switch (selectedIdx)
        {
            case 0: // Team Average
            {
                var teamId = state.GetEmployeeTeam(emp.id);
                if (teamId.HasValue)
                {
                    var employees = state.ActiveEmployees;
                    int sumCA = 0, sumSalary = 0, sumMorale = 0, sumPA = 0, memberCount = 0;
                    int ec = employees.Count;
                    for (int i = 0; i < ec; i++)
                    {
                        var other = employees[i];
                        if (!state.GetEmployeeTeam(other.id).HasValue) continue;
                        if (state.GetEmployeeTeam(other.id).Value != teamId.Value) continue;
                        if (other.id.Equals(emp.id)) continue;
                        sumCA += other.Stats.Skills != null
                            ? state.ComputeAbilityForRole(other.Stats.Skills, other.role)
                            : 0;
                        sumPA     += other.Stats.PotentialAbility;
                        sumSalary += other.salary;
                        sumMorale += other.morale;
                        memberCount++;
                    }
                    if (memberCount > 0)
                    {
                        benchCA     = sumCA     / memberCount;
                        benchPA     = sumPA     / memberCount;
                        benchSalary = sumSalary / memberCount;
                        benchMorale = sumMorale / memberCount;
                    }
                }
                benchmarkLabel = "Team Average";
                break;
            }
            case 1: // Role Company Average
            {
                var employees = state.ActiveEmployees;
                int sumCA = 0, sumSalary = 0, sumMorale = 0, sumPA = 0, memberCount = 0;
                int ec = employees.Count;
                for (int i = 0; i < ec; i++)
                {
                    var other = employees[i];
                    if (other.id.Equals(emp.id)) continue;
                    if (other.role != emp.role) continue;
                    sumCA += other.Stats.Skills != null
                        ? state.ComputeAbilityForRole(other.Stats.Skills, other.role)
                        : 0;
                    sumPA     += other.Stats.PotentialAbility;
                    sumSalary += other.salary;
                    sumMorale += other.morale;
                    memberCount++;
                }
                if (memberCount > 0)
                {
                    benchCA     = sumCA     / memberCount;
                    benchPA     = sumPA     / memberCount;
                    benchSalary = sumSalary / memberCount;
                    benchMorale = sumMorale / memberCount;
                }
                benchmarkLabel = "Role Avg";
                break;
            }
            case 2: // Role Market Benchmark
            {
                benchSalary = SalaryBand.GetBase(emp.role);
                benchCA     = 60;
                benchPA     = 80;
                benchMorale = 70;
                benchmarkLabel = "Market";
                break;
            }
        }

        var metrics = new ComparisonMetricRow[]
        {
            MakeComparisonRow("CA",      empCA,     benchCA,     false),
            MakeComparisonRow("PA",      empPA,     benchPA,     false),
            MakeComparisonRow("Role Fit", empFit,   benchCA,     false),
            MakeComparisonRowMoney("Salary", empSalary, benchSalary, false),
            MakeComparisonRow("Morale",  empMorale, benchMorale, false),
        };

        Comparison = new ComparisonTabData
        {
            AvailableTargets    = targets,
            SelectedTargetIndex = selectedIdx,
            Metrics             = metrics
        };
    }

    private static ComparisonMetricRow MakeComparisonRow(
        string name, int empVal, int benchVal, bool lowerIsBetter)
    {
        int diff = empVal - benchVal;
        string diffText = diff > 0 ? "+" + diff : diff.ToString();
        string diffClass = diff > 0
            ? (lowerIsBetter ? "diff--negative" : "diff--positive")
            : diff < 0
                ? (lowerIsBetter ? "diff--positive" : "diff--negative")
                : "diff--neutral";
        return new ComparisonMetricRow
        {
            MetricName      = name,
            EmployeeValue   = empVal.ToString(),
            ComparisonValue = benchVal > 0 ? benchVal.ToString() : "—",
            DifferenceText  = benchVal > 0 ? diffText : "—",
            DifferenceClass = benchVal > 0 ? diffClass : "diff--neutral"
        };
    }

    private static ComparisonMetricRow MakeComparisonRowMoney(
        string name, int empVal, int benchVal, bool lowerIsBetter)
    {
        int diff = empVal - benchVal;
        string diffText = diff > 0 ? "+" + UIFormatting.FormatMoney(diff) : UIFormatting.FormatMoney(diff);
        string diffClass = diff > 0
            ? (lowerIsBetter ? "diff--negative" : "diff--positive")
            : diff < 0
                ? (lowerIsBetter ? "diff--positive" : "diff--negative")
                : "diff--neutral";
        return new ComparisonMetricRow
        {
            MetricName      = name,
            EmployeeValue   = UIFormatting.FormatMoney(empVal),
            ComparisonValue = benchVal > 0 ? UIFormatting.FormatMoney(benchVal) : "—",
            DifferenceText  = benchVal > 0 ? diffText : "—",
            DifferenceClass = benchVal > 0 ? diffClass : "diff--neutral"
        };
    }



    private Employee FindEmployee(IReadOnlyGameState state)
    {
        var employees = state.ActiveEmployees;
        int count = employees.Count;
        for (int i = 0; i < count; i++)
        {
            if (employees[i].id.Equals(_employeeId)) return employees[i];
        }
        return null;
    }

    private static string FormatLength(ContractLengthOption length)
    {
        switch (length)
        {
            case ContractLengthOption.Short:    return "Short";
            case ContractLengthOption.Standard: return "Standard";
            case ContractLengthOption.Long:     return "Long";
            default:                            return "Standard";
        }
    }

    private static string DeriveSeiorityLabel(Employee emp)
    {
        if (emp.isFounder) return "Founder";
        if (emp.age >= 45) return "Senior";
        if (emp.age >= 35) return "Mid-Level";
        return "Junior";
    }

    private static string FormatAbilityStars(int ability)
    {
        // 0–20 per star bucket, 5 stars max
        int stars = Mathf.Clamp(ability / 20, 0, 5);
        var sb = new System.Text.StringBuilder(5);
        for (int i = 0; i < 5; i++)
            sb.Append(i < stars ? '\u2605' : '\u2606');
        return sb.ToString();
    }
}
