using UnityEngine;

public class EmployeeDetailModalViewModel : IViewModel
{
    private EmployeeId _employeeId;

    // Summary
    public string Name { get; private set; }
    public int Age { get; private set; }
    public string RoleName { get; private set; }
    public string RolePillClass { get; private set; }
    public string PersonalityText { get; private set; }
    public string PersonalityClass { get; private set; }
    public string TeamName { get; private set; }
    public string MoraleText { get; private set; }
    public string MoraleClass { get; private set; }
    public string EnergyText { get; private set; }
    public string EnergyClass { get; private set; }

    // Skills table
    private readonly SkillTableEntry[] _skillTable = new SkillTableEntry[SkillTypeHelper.SkillTypeCount];
    public SkillTableEntry[] SkillTable => _skillTable;

    // Role suitability
    private readonly RoleSuitabilityEntry[] _roleSuitabilities = new RoleSuitabilityEntry[RoleSuitabilityCalculator.AllRoles.Length];
    public RoleSuitabilityEntry[] RoleSuitabilities => _roleSuitabilities;
    public bool IsWorkingOffRole { get; private set; }
    public string PreferredRoleName { get; private set; }
    public string AssignedRoleName { get; private set; }

    // Contract
    public string ContractType { get; private set; }
    public string ContractLength { get; private set; }
    public string SalaryText { get; private set; }
    public string RemainingText { get; private set; }
    public string HiredDateText { get; private set; }

    // Escalation
    public int StrikeCount { get; private set; }
    public string EscalationText { get; private set; }
    public string EscalationClass { get; private set; }
    public bool ShowEscalation { get; private set; }

    // Departure risk
    public bool ShowDepartureRisk { get; private set; }
    public string DepartureRiskText { get; private set; }

    // Salary benchmark
    public string MarketRateText { get; private set; }
    public string MarketPositionText { get; private set; }
    public string MarketPositionClass { get; private set; }
    public string ValueEfficiencyText { get; private set; }

    // Founder
    public bool IsFounder { get; private set; }
    public bool ShowOfferNewContract { get; private set; }
    public EmployeeId CurrentEmployeeId => _employeeId;
    public IReadOnlyGameState LastState { get; private set; }

    // Renewal negotiation
    public bool ShowRenewalSection { get; private set; }
    public bool HasActiveRenewalNegotiation { get; private set; }
    public bool HasPendingRenewalCounter { get; private set; }
    public int RenewalMaxPatience { get; private set; }
    public int RenewalCurrentPatience { get; private set; }
    public bool IsOnCooldown { get; private set; }
    public string CooldownText { get; private set; }
    public string RenewalDemandText { get; private set; }
    public string CurrentSalaryText { get; private set; }

    // Counter-offer comparison
    public string CounterSalaryText { get; private set; }
    public string CounterRoleName { get; private set; }
    public string CounterTypeName { get; private set; }
    public string CounterLengthText { get; private set; }
    public string OriginalSalaryText { get; private set; }

    public void SetEmployeeId(EmployeeId id) {
        _employeeId = id;
    }

    public void Refresh(IReadOnlyGameState state) {
        if (state == null) return;
        LastState = state;

        var emp = FindEmployee(state);
        if (emp == null) return;

        // Summary
        Name = emp.name;
        Age = emp.age;
        RoleName = UIFormatting.FormatRole(emp.role);
        RolePillClass = UIFormatting.RolePillClass(emp.role);

        var personality = state.GetEmployeePersonality(emp.id);
        PersonalityText = UIFormatting.FormatPersonality(personality);
        PersonalityClass = UIFormatting.PersonalityBadgeClass(personality);

        TeamName = "--";
        var teamId = state.GetEmployeeTeam(emp.id);
        if (teamId.HasValue) {
            var teams = state.ActiveTeams;
            int teamCount = teams.Count;
            for (int t = 0; t < teamCount; t++) {
                if (teams[t].id == teamId.Value) {
                    TeamName = teams[t].name;
                    break;
                }
            }
        }

        int morale = emp.morale;
        MoraleText = morale + "%";
        if (morale >= 70)       MoraleClass = "text-success";
        else if (morale >= 40)  MoraleClass = "text-warning";
        else                    MoraleClass = "text-danger";

        var energyBand = state.GetEmployeeEnergyBand(emp.id);
        EnergyText = UIFormatting.FormatEnergyBand(energyBand);
        EnergyClass = UIFormatting.EnergyBandClass(energyBand);

        // Skills table with delta arrows
        int skillCount = SkillTypeHelper.SkillTypeCount;
        for (int i = 0; i < skillCount; i++) {
            var skillType = (SkillType)i;
            int level = emp.GetSkill(skillType);
            sbyte delta = (emp.skillDeltaDirection != null && emp.skillDeltaDirection.Length > i)
                ? emp.skillDeltaDirection[i]
                : (sbyte)0;

            string valueText;
            string valueClass;
            if (delta > 0) {
                valueText = level + " \u25B2";
                valueClass = "skill-row__value--up";
            } else if (delta < 0) {
                valueText = level + " \u25BC";
                valueClass = "skill-row__value--down";
            } else {
                valueText = level.ToString();
                valueClass = "";
            }

            _skillTable[i] = new SkillTableEntry {
                Name       = SkillTypeHelper.GetName(skillType),
                ValueText  = valueText,
                ValueClass = valueClass,
                NameColor  = UIFormatting.GetSkillColor(skillType)
            };
        }

        // Role suitability
        AssignedRoleName  = UIFormatting.FormatRole(emp.role);
        PreferredRoleName = UIFormatting.FormatRole(emp.preferredRole);
        IsWorkingOffRole  = emp.role != emp.preferredRole;

        var allRoles = RoleSuitabilityCalculator.AllRoles;
        int roleCount = allRoles.Length;
        for (int i = 0; i < roleCount; i++) {
            var role = allRoles[i];
            int ability = 0;
            RoleSuitability suitability = RoleSuitability.Unsuitable;
            if (emp.skills != null) {
                ability = state.ComputeAbilityForRole(emp.skills, role);
                suitability = RoleSuitabilityCalculator.GetSuitability(ability);
            }
            _roleSuitabilities[i] = new RoleSuitabilityEntry {
                Role           = role,
                Suitability    = suitability,
                AbilityForRole = ability,
                RoleName       = UIFormatting.FormatRole(role),
                SuitabilityClass = UIFormatting.SuitabilityDotClass(suitability),
                IsPreferred    = role == emp.preferredRole
            };
        }

        // Contract
        bool isFounder = emp.isFounder;
        IsFounder = isFounder;
        ShowOfferNewContract = !isFounder;

        if (isFounder) {
            ContractType = "Founder";
            ContractLength = "Permanent";
            SalaryText = "$0/mo";
            RemainingText = "Permanent";
        } else {
            ContractType = emp.Contract.Type == EmploymentType.FullTime ? "Full-Time" : "Part-Time";
            ContractLength = FormatLength(emp.Contract.Length);
            SalaryText = UIFormatting.FormatMoney(emp.salary) + "/mo";

            if (emp.contractExpiryTick <= 0) {
                RemainingText = "--";
            } else {
                int daysLeft = (emp.contractExpiryTick - state.CurrentTick) / TimeState.TicksPerDay;
                if (daysLeft < 0) daysLeft = 0;
                RemainingText = daysLeft + "d remaining";
            }
        }

        HiredDateText = "Day " + (emp.hireDate / TimeState.TicksPerDay);

        // Escalation (3-strike)
        int strikes = emp.StrikeCount;
        StrikeCount = strikes;
        ShowEscalation = strikes > 0;
        if (strikes >= 3) {
            EscalationText = "Strike 3 — Refusing renewal";
            EscalationClass = "badge--danger";
        } else if (strikes == 2) {
            EscalationText = "Strike 2 of 3 — Will leave soon";
            EscalationClass = "badge--warning";
        } else if (strikes == 1) {
            EscalationText = "Strike 1 of 3";
            EscalationClass = "badge--warning";
        } else {
            EscalationText = "";
            EscalationClass = "badge--neutral";
        }

        // Departure risk — morale below quit threshold
        const int quitThreshold = 20;
        ShowDepartureRisk = morale <= quitThreshold + 10;
        if (morale <= quitThreshold) {
            DepartureRiskText = "High departure risk — morale critical";
        } else if (morale <= quitThreshold + 10) {
            DepartureRiskText = "Elevated departure risk — morale low";
        } else {
            DepartureRiskText = "";
        }

        // Salary benchmark
        int marketRate = SalaryBand.GetBase(emp.role);

        if (isFounder) {
            MarketRateText = UIFormatting.FormatMoney(marketRate) + "/mo";
            MarketPositionText = "Founder";
            MarketPositionClass = "badge--neutral";
            ValueEfficiencyText = "N/A";
        } else {
            MarketRateText = UIFormatting.FormatMoney(marketRate) + "/mo";

            float ratio = marketRate > 0 ? (float)emp.salary / marketRate : 1f;
            if (ratio <= 0.80f)      { MarketPositionText = "Far Below Market";  MarketPositionClass = "badge--danger";  }
            else if (ratio <= 0.93f) { MarketPositionText = "Below Market";      MarketPositionClass = "badge--warning"; }
            else if (ratio <= 1.07f) { MarketPositionText = "At Market";         MarketPositionClass = "badge--neutral"; }
            else if (ratio <= 1.20f) { MarketPositionText = "Above Market";      MarketPositionClass = "badge--success"; }
            else                     { MarketPositionText = "Well Above Market";  MarketPositionClass = "badge--accent";  }

            float valueEff = (marketRate > 0 && emp.salary > 0) ? (float)marketRate / emp.salary * 100f : 0f;
            ValueEfficiencyText = emp.salary <= 0 ? "N/A" : System.Math.Round(valueEff).ToString("F0") + "%";
        }

        // Renewal negotiation
        ShowRenewalSection = !isFounder;
        CurrentSalaryText  = UIFormatting.FormatMoney(emp.salary) + "/mo";

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

        if (!isFounder) {
            var empNeg = state.GetEmployeeNegotiation(emp.id);
            if (empNeg.HasValue) {
                var neg = empNeg.Value;
                HasActiveRenewalNegotiation = neg.status == NegotiationStatus.Pending
                    || neg.status == NegotiationStatus.CounterOffered;
                HasPendingRenewalCounter = neg.hasCounterOffer
                    && neg.status == NegotiationStatus.CounterOffered;
                RenewalMaxPatience     = neg.maxPatience;
                RenewalCurrentPatience = neg.currentPatience;
                IsOnCooldown = state.IsEmployeeOnNegotiationCooldown(emp.id);

                if (IsOnCooldown && neg.cooldownExpiryTick > 0) {
                    int daysLeft = (neg.cooldownExpiryTick - state.CurrentTick) / TimeState.TicksPerDay;
                    if (daysLeft < 0) daysLeft = 0;
                    CooldownText = daysLeft + " days remaining";
                }

                if (HasPendingRenewalCounter) {
                    var counter = neg.counterOffer;
                    CounterSalaryText  = UIFormatting.FormatMoney(counter.CounterSalary) + "/mo";
                    CounterRoleName    = UIFormatting.FormatRole(counter.CounterRole);
                    CounterTypeName    = counter.CounterType == EmploymentType.FullTime ? "Full-Time" : "Part-Time";
                    CounterLengthText  = FormatLength(counter.CounterLength);
                    OriginalSalaryText = UIFormatting.FormatMoney(neg.lastOffer.OfferedSalary) + "/mo";
                }
            }
        }
    }

    private Employee FindEmployee(IReadOnlyGameState state) {
        var employees = state.ActiveEmployees;
        int count = employees.Count;
        for (int i = 0; i < count; i++) {
            if (employees[i].id.Equals(_employeeId)) return employees[i];
        }
        return null;
    }

    private static string FormatLength(ContractLengthOption length) {
        switch (length) {
            case ContractLengthOption.Short:    return "Short";
            case ContractLengthOption.Standard: return "Standard";
            case ContractLengthOption.Long:     return "Long";
            default:                            return "Standard";
        }
    }
}
