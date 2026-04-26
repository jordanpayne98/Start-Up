using System.Collections.Generic;

public struct RoleSuitabilityEntry
{
    public EmployeeRole Role;
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
    public UnityEngine.Color NameColor;
}

public class CandidateDetailModalViewModel : IViewModel
{
    private int _candidateId;

    // Identity
    public string Name       { get; private set; }
    public string Age        { get; private set; }
    public string RoleName   { get; private set; }
    public string RolePillClass { get; private set; }

    // Meta
    public string Source     { get; private set; }
    public string ExpiryText { get; private set; }

    // Personality
    public string PersonalityText { get; private set; }

    // Employment preferences
    public string FTPrefText   { get; private set; }
    public string FTPrefClass  { get; private set; }
    public string LengthPrefText { get; private set; }

    // Knowledge-based progressive reveal
    public float KnowledgePercent       { get; private set; }
    public bool IsPersonalityRevealed   { get; private set; }
    public bool IsPreferencesRevealed   { get; private set; }
    public bool IsPrimarySkillsRevealed { get; private set; }
    public bool IsAbilityRevealed       { get; private set; }
    public bool IsPotentialRevealed     { get; private set; }
    public string ConfidenceText        { get; private set; }
    public string ConfidenceClass       { get; private set; }
    public string ReliabilityText       { get; private set; }
    public string ReliabilityClass      { get; private set; }


    // Skills reveal
    public bool IsSkillsRevealed { get; private set; }

    // Skills table (9 entries, one per SkillType)
    private readonly SkillTableEntry[] _skillTable = new SkillTableEntry[SkillTypeHelper.SkillTypeCount];
    public SkillTableEntry[] SkillTable => _skillTable;

    // Ability / Potential
    public string AbilityEstimate   { get; private set; }
    public string PotentialEstimate { get; private set; }

    // Salary
    public int RawSalaryDemand { get; private set; }
    public string SalaryAsking { get; private set; }
    public string SalaryMarket { get; private set; }

    // Interview state
    public bool IsInterviewed         { get; private set; }
    public bool IsInterviewInProgress { get; private set; }
    public bool CanInterview          { get; private set; }
    public string InterviewButtonText   { get; private set; }
    public string InterviewButtonClass  { get; private set; }
    public bool InterviewButtonEnabled  { get; private set; }
    public string InterviewDisabledReason { get; private set; }
    public float InterviewProgressPercent { get; private set; }

    // Shortlist
    public bool IsShortlisted { get; private set; }

    // Salary visibility / offer cooldown
    public bool IsSalaryDemandVisible { get; private set; }
    public bool IsOfferOnCooldown     { get; private set; }
    public string OfferCooldownText   { get; private set; }

    // Offer — role suitability entries (one per valid role)
    private readonly RoleSuitabilityEntry[] _roleSuitabilities = new RoleSuitabilityEntry[RoleSuitabilityCalculator.AllRoles.Length];
    public RoleSuitabilityEntry[] RoleSuitabilities => _roleSuitabilities;

    // Offer — salary slider bounds and current state
    public int SalarySliderMin    { get; private set; }
    public int SalarySliderMax    { get; private set; }
    public int SalarySliderAnchor { get; private set; }
    public int CurrentOfferSalary { get; private set; }
    public EmployeeRole SelectedRole { get; private set; }

    // Offer — acceptance preview
    public float AcceptanceChance      { get; private set; }
    public string AcceptanceChanceText { get; private set; }
    public string AcceptanceChanceClass { get; private set; }

    // Offer — patience
    public int MaxPatience     { get; private set; }
    public int CurrentPatience { get; private set; }

    // Offer — negotiation flags
    public bool HasActiveNegotiation { get; private set; }
    public bool HasPendingCounter    { get; private set; }

    // Offer — mismatch
    public bool ShowMismatchSection { get; private set; }
    public string MismatchHintText  { get; private set; }
    private readonly List<string> _mismatchWarnings = new List<string>(4);
    public List<string> MismatchWarnings => _mismatchWarnings;

    // Offer summary (kept for backward compat with dispatch)
    public string OfferSummaryType  { get; private set; }

    private CandidateData _candidate;
    private IReadOnlyGameState _state;

    // Current offer parameters — persisted across RefreshOfferData calls
    private EmploymentType _currentEmploymentType = EmploymentType.FullTime;
    private ContractLengthOption _currentLength = ContractLengthOption.Standard;

    private const int MinimumWage = 500;

    public int CandidateId => _candidateId;

    public void SetCandidateId(int candidateId) {
        _candidateId = candidateId;
    }

    public void Refresh(IReadOnlyGameState state) {
        if (state == null) return;
        _state = state;

        _candidate = FindCandidate(state);
        if (_candidate == null) return;

        int currentTick  = state.CurrentTick;
        float knowledge  = state.GetInterviewKnowledgeLevel(_candidateId);
        bool inProgress  = state.IsInterviewInProgress(_candidateId);
        bool interviewDone = state.IsFinalReportReady(_candidateId);

        // Salary visibility and offer cooldown
        IsSalaryDemandVisible = SalaryDemandCalculator.IsSalaryRevealed(_candidate);
        IsOfferOnCooldown     = state.IsOfferOnCooldown(_candidateId);
        OfferCooldownText     = IsOfferOnCooldown ? "Candidate is reviewing options" : "";

        // Knowledge thresholds
        KnowledgePercent        = knowledge;
        IsPersonalityRevealed   = knowledge >= 100f;
        IsPreferencesRevealed   = knowledge >= 20f;
        IsPrimarySkillsRevealed = knowledge >= 40f;
        IsAbilityRevealed       = knowledge >= 60f;
        IsPotentialRevealed     = knowledge >= 80f;
        IsSkillsRevealed        = knowledge >= 40f;

        ConfidenceText  = knowledge > 0f ? state.GetInterviewConfidenceLabel(_candidateId) : "";
        ConfidenceClass = knowledge > 0f ? state.GetInterviewConfidenceClass(_candidateId) : "";
        ReliabilityText  = knowledge > 0f ? state.GetInterviewReliabilityLabel(_candidateId) : "";
        ReliabilityClass = knowledge > 0f ? state.GetInterviewReliabilityClass(_candidateId) : "";

        // Identity
        Name          = _candidate.Name;
        Age           = _candidate.Age + "y";
        RoleName      = UIFormatting.FormatRole(_candidate.Role);
        RolePillClass = UIFormatting.RolePillClass(_candidate.Role);

        // Meta
        Source = _candidate.IsPendingReview ? "HR Sourced" :
                 (_candidate.IsTargeted ? "Shortlisted" : "Market");
        if (_candidate.ExpiryTick == int.MaxValue)
            ExpiryText = "Shortlisted indefinitely";
        else if (_candidate.ExpiryTick <= 0)
            ExpiryText = "";
        else {
            int daysLeft = (_candidate.ExpiryTick - currentTick) / TimeState.TicksPerDay;
            if (daysLeft < 0) daysLeft = 0;
            ExpiryText = daysLeft + "d remaining";
        }

        // Personality — revealed at knowledge 100
        PersonalityText = IsPersonalityRevealed
            ? UIFormatting.FormatPersonality(_candidate.personality)
            : (knowledge > 0f ? "Interview in progress..." : "Interview required to reveal");

        // Employment preferences
        ApplyPreferenceVisibility();

        // Skills table — show at knowledge >= 40
        BuildSkillTable();

        // Ability / Potential estimates from InterviewSystem
        if (IsAbilityRevealed) {
            int abilityStars = state.GetAbilityStarEstimate(_candidateId);
            AbilityEstimate = abilityStars > 0 ? abilityStars + "★" : "?★";
        } else {
            AbilityEstimate = "?★";
        }

        if (IsPotentialRevealed) {
            int potentialStars = state.GetPotentialStarEstimate(_candidateId);
            PotentialEstimate = potentialStars > 0 ? potentialStars + "★" : "?★";
        } else {
            PotentialEstimate = "?★";
        }

        // Salary
        int salaryDemand = state.GetEffectiveSalaryDemand(_candidateId);
        int marketRate   = SalaryBand.GetBase(_candidate.Role);
        RawSalaryDemand  = salaryDemand;
        SalaryAsking     = IsSalaryDemandVisible ? UIFormatting.FormatMoney(salaryDemand) + "/mo" : "???";
        SalaryMarket     = UIFormatting.FormatMoney(marketRate) + "/mo";

        // Interview button
        IsInterviewed         = interviewDone;
        IsInterviewInProgress = inProgress;
        CanInterview          = state.CanStartInterview(_candidateId,
            _candidate.IsTargeted ? HiringMode.HR : HiringMode.Manual);

        if (interviewDone) {
            InterviewButtonText     = "Interviewed";
            InterviewButtonClass    = "btn-ghost";
            InterviewButtonEnabled  = false;
            InterviewDisabledReason = "Interview complete";
        } else if (inProgress) {
            int pct = (int)knowledge;
            InterviewButtonText     = "Interviewing... " + pct + "%";
            InterviewButtonClass    = "btn-ghost";
            InterviewButtonEnabled  = false;
            InterviewDisabledReason = "Interview in progress";
        } else if (CanInterview) {
            InterviewButtonText     = "Start Interview";
            InterviewButtonClass    = "btn-secondary";
            InterviewButtonEnabled  = true;
            InterviewDisabledReason = "";
        } else {
            InterviewButtonText     = "Start Interview";
            InterviewButtonClass    = "btn-secondary";
            InterviewButtonEnabled  = false;
            InterviewDisabledReason = "Cannot start interview right now";
        }

        InterviewProgressPercent = inProgress ? knowledge / 100f : (interviewDone ? 1f : 0f);

        // Shortlist
        IsShortlisted = _candidate.IsTargeted;

        // Negotiation state
        HasActiveNegotiation = state.HasActiveNegotiation(_candidateId);
        HasPendingCounter    = state.HasPendingCounterOffer(_candidateId);
        MaxPatience          = state.GetCandidateMaxPatience(_candidateId);
        CurrentPatience      = state.GetCandidateCurrentPatience(_candidateId);

        // Initialize offer data — defaults to preferred role, current type & length
        SelectedRole = _candidate.Role;
        BuildRoleSuitabilities();
        RefreshOfferData(_currentEmploymentType, _currentLength);
    }

    public void SetSelectedRole(EmployeeRole role) {
        SelectedRole = role;
        RefreshOfferData(_currentEmploymentType, _currentLength);
    }

    public void SetOfferSalary(int salary) {
        if (salary < SalarySliderMin) salary = SalarySliderMin;
        if (salary > SalarySliderMax) salary = SalarySliderMax;
        CurrentOfferSalary = salary;
        RefreshAcceptanceChance();
    }

    public void RefreshOfferData(EmploymentType type, ContractLengthOption length) {
        if (_candidate == null) return;
        _currentEmploymentType = type;
        _currentLength = length;

        int baseDemand = RawSalaryDemand;
        int marketRate = SalaryBand.GetBase(SelectedRole);

        // Compute the adjusted demand for this offer configuration
        int anchor;
        if (IsSalaryDemandVisible) {
            RoleSuitability suitability = GetSuitabilityForSelected();
            anchor = SalaryModifierCalculator.ComputeOfferSalary(
                baseDemand, type, length, _candidate.Preferences,
                _candidate.Role, SelectedRole, suitability);
        } else {
            anchor = SalaryModifierCalculator.ComputeOfferSalary(
                marketRate, type, length, _candidate.Preferences);
        }

        SalarySliderAnchor = anchor;
        SalarySliderMin    = SalaryDemandCalculator.Round50(anchor * 0.5f);
        if (SalarySliderMin < MinimumWage) SalarySliderMin = MinimumWage;
        SalarySliderMax    = SalaryDemandCalculator.Round50(anchor * 1.5f);

        // Clamp current salary to new bounds; init to anchor if unset
        if (CurrentOfferSalary < SalarySliderMin || CurrentOfferSalary > SalarySliderMax)
            CurrentOfferSalary = anchor;

        OfferSummaryType = type == EmploymentType.FullTime ? "Full-Time" : "Part-Time";

        RefreshMismatchWarnings(type, length);
        RefreshAcceptanceChance();
    }

    private void RefreshAcceptanceChance() {
        if (_candidate == null || !IsSalaryDemandVisible) {
            AcceptanceChance      = 0f;
            AcceptanceChanceText  = "???";
            AcceptanceChanceClass = "acceptance-bar--medium";
            return;
        }

        int baseDemand = RawSalaryDemand;
        RoleSuitability suitability = GetSuitabilityForSelected();
        int demand = SalaryModifierCalculator.ComputeOfferSalary(
            baseDemand, _currentEmploymentType, _currentLength, _candidate.Preferences,
            _candidate.Role, SelectedRole, suitability);

        var offer = new OfferPackage {
            CandidateId    = _candidateId,
            OfferedRole    = SelectedRole,
            EmploymentType = _currentEmploymentType,
            Length         = _currentLength,
            OfferedSalary  = CurrentOfferSalary,
            Mode           = HiringMode.Manual
        };

        float chance = OfferEvaluator.ComputeSatisfaction(offer, demand, _candidate.Role,
            _candidate.Preferences, suitability);
        if (chance < 0f) chance = 0f;
        if (chance > 100f) chance = 100f;

        AcceptanceChance      = chance;
        AcceptanceChanceText  = "~" + (int)chance + "%";
        AcceptanceChanceClass = chance >= 70f ? "acceptance-bar--high"
                              : chance >= 40f ? "acceptance-bar--medium"
                              : "acceptance-bar--low";
    }

    private void RefreshMismatchWarnings(EmploymentType type, ContractLengthOption length) {
        _mismatchWarnings.Clear();
        if (_candidate == null) {
            ShowMismatchSection = false;
            MismatchHintText = "";
            return;
        }

        if (IsPersonalityRevealed) {
            MismatchHintText = "";
            var prefs = _candidate.Preferences;

            if (type == EmploymentType.FullTime && prefs.FtPtPref == FtPtPreference.PrefersPartTime)
                _mismatchWarnings.Add("Candidate prefers part-time work");
            else if (type == EmploymentType.PartTime && prefs.FtPtPref == FtPtPreference.PrefersFullTime)
                _mismatchWarnings.Add("Candidate prefers full-time work");

            if (length == ContractLengthOption.Short && prefs.LengthPref == LengthPreference.PrefersSecurity)
                _mismatchWarnings.Add("Candidate prefers longer contract security");
            else if (length == ContractLengthOption.Long && prefs.LengthPref == LengthPreference.PrefersFlexibility)
                _mismatchWarnings.Add("Candidate prefers shorter, flexible contracts");

            ShowMismatchSection = _mismatchWarnings.Count > 0;
        } else if (IsPreferencesRevealed) {
            ShowMismatchSection = true;
            MismatchHintText = "Full preference details revealed after interview";
        } else {
            ShowMismatchSection = false;
            MismatchHintText = "";
        }
    }

    private void BuildRoleSuitabilities() {
        if (_candidate == null) return;
        var allRoles = RoleSuitabilityCalculator.AllRoles;
        int roleCount = allRoles.Length;
        for (int i = 0; i < roleCount; i++) {
            var role = allRoles[i];
            int ability = 0;
            RoleSuitability suitability = RoleSuitability.Unsuitable;
            if (IsSkillsRevealed && _candidate.Skills != null && _state != null) {
                ability = _state.ComputeAbilityForRole(_candidate.Skills, role);
                suitability = RoleSuitabilityCalculator.GetSuitability(ability);
            }
            _roleSuitabilities[i] = new RoleSuitabilityEntry {
                Role          = role,
                Suitability   = suitability,
                AbilityForRole = ability,
                RoleName      = UIFormatting.FormatRole(role),
                SuitabilityClass = UIFormatting.SuitabilityDotClass(suitability),
                IsPreferred   = role == _candidate.Role
            };
        }
    }

    private RoleSuitability GetSuitabilityForSelected() {
        var allRoles = RoleSuitabilityCalculator.AllRoles;
        int roleCount = allRoles.Length;
        for (int i = 0; i < roleCount; i++) {
            if (_roleSuitabilities[i].Role == SelectedRole)
                return _roleSuitabilities[i].Suitability;
        }
        return RoleSuitability.Unsuitable;
    }

    private void ApplyPreferenceVisibility() {
        if (_candidate == null) return;
        var prefs = _candidate.Preferences;

        if (IsPersonalityRevealed) {
            FTPrefText   = FormatFTPref(prefs.FtPtPref);
            FTPrefClass  = "";
            LengthPrefText = FormatLengthPref(prefs.LengthPref);
        } else if (IsPreferencesRevealed) {
            FTPrefText   = GetFTPrefHint(prefs.FtPtPref);
            FTPrefClass  = "detail-row__value--hint";
            LengthPrefText = GetLengthPrefHint(prefs.LengthPref);
        } else {
            FTPrefText   = "Unknown";
            FTPrefClass  = "text-muted";
            LengthPrefText = "Unknown";
        }
    }

    private static string FormatFTPref(FtPtPreference pref) {
        switch (pref) {
            case FtPtPreference.PrefersFullTime: return "Prefers Full-Time";
            case FtPtPreference.PrefersPartTime: return "Prefers Part-Time";
            default:                             return "Flexible";
        }
    }

    private static string GetFTPrefHint(FtPtPreference pref) {
        switch (pref) {
            case FtPtPreference.PrefersFullTime: return "Likely prefers full-time";
            case FtPtPreference.PrefersPartTime: return "Likely prefers part-time";
            default:                             return "Seems flexible";
        }
    }

    private static string FormatLengthPref(LengthPreference pref) {
        switch (pref) {
            case LengthPreference.PrefersSecurity:    return "Prefers Security (Long)";
            case LengthPreference.PrefersFlexibility: return "Prefers Flexibility (Short)";
            default:                                  return "No Preference";
        }
    }

    private static string GetLengthPrefHint(LengthPreference pref) {
        switch (pref) {
            case LengthPreference.PrefersSecurity:    return "Values job security";
            case LengthPreference.PrefersFlexibility: return "Values flexibility";
            default:                                  return "Seems neutral on length";
        }
    }

    private void BuildSkillTable() {
        int count = SkillTypeHelper.SkillTypeCount;
        for (int i = 0; i < count; i++) {
            var skill = (SkillType)i;
            string valueText;
            string valueClass;
            if (IsSkillsRevealed && _candidate.Skills != null && i < _candidate.Skills.Length) {
                valueText  = _candidate.Skills[i].ToString();
                valueClass = "";
            } else {
                valueText  = "?";
                valueClass = "skill-row__value--unknown";
            }
            _skillTable[i] = new SkillTableEntry {
                Name       = SkillTypeHelper.GetName(skill),
                ValueText  = valueText,
                ValueClass = valueClass,
                NameColor  = UIFormatting.GetSkillColor(skill)
            };
        }
    }

    private CandidateData FindCandidate(IReadOnlyGameState state) {
        var available = state.AvailableCandidates;
        int count = available.Count;
        for (int i = 0; i < count; i++) {
            if (available[i].CandidateId == _candidateId) return available[i];
        }
        var pending = state.PendingReviewCandidates;
        int pCount = pending.Count;
        for (int i = 0; i < pCount; i++) {
            if (pending[i].CandidateId == _candidateId) return pending[i];
        }
        return null;
    }
}
