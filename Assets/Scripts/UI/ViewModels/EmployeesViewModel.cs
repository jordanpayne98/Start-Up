using System.Collections.Generic;
using UnityEngine;

public enum EmployeeSortMode
{
    Name,
    Role,
    Salary,
    ContractExpiry,
    Morale
}

public struct EmployeeRowDisplay
{
    public EmployeeId Id;
    public string Name;
    public string RoleName;
    public string RolePillClass;
    public string TypeBadge;
    public string TypeBadgeClass;
    public string TeamName;
    public bool IsFounder;
    public string MoraleText;
    public string MoraleClass;
    public string EnergyText;
    public string EnergyClass;
    public string SalaryText;
    public string ContractExpiryText;
    public bool ShowRenewalBadge;
}

public class EmployeesViewModel : IViewModel
{
    public int EmployeeCount { get; private set; }
    public string EffectiveCapacityText { get; private set; }

    private readonly List<EmployeeRowDisplay> _employees = new List<EmployeeRowDisplay>(32);
    public List<EmployeeRowDisplay> Employees => _employees;

    public EmployeeSortMode SortMode { get; set; } = EmployeeSortMode.Name;

    public void ResortAndNotify() {
        SortEmployees();
    }

    public bool IsDirty { get; private set; }
    public void ClearDirty() => IsDirty = false;

    public void Refresh(GameStateSnapshot snapshot) {
        IReadOnlyGameState state = snapshot;
        if (state == null) return;

        _employees.Clear();

        var employees = state.ActiveEmployees;
        int count = employees.Count;
        int currentTick = state.CurrentTick;

        float totalCapacity = 0f;

        for (int i = 0; i < count; i++) {
            var emp = employees[i];

            // Team name
            string teamName = "--";
            var teamId = state.GetEmployeeTeam(emp.id);
            if (teamId.HasValue) {
                var teams = state.ActiveTeams;
                int teamCount = teams.Count;
                for (int t = 0; t < teamCount; t++) {
                    if (teams[t].id == teamId.Value) {
                        teamName = teams[t].name;
                        break;
                    }
                }
            }

            // Type badge
            string typeBadge;
            string typeBadgeClass;
            bool isFounder = emp.isFounder;
            if (isFounder) {
                typeBadge = "Founder";
                typeBadgeClass = "badge--special";
            } else if (emp.Contract.Type == EmploymentType.FullTime) {
                typeBadge = "Full-Time";
                typeBadgeClass = "badge--accent";
            } else {
                typeBadge = "Part-Time";
                typeBadgeClass = "badge--info";
            }

            // Morale
            int morale = emp.morale;
            string moraleText = morale + "%";
            string moraleClass;
            if (morale >= 70)       moraleClass = "text-success";
            else if (morale >= 40)  moraleClass = "text-warning";
            else                    moraleClass = "text-danger";

            // Energy
            var energyBand = state.GetEmployeeEnergyBand(emp.id);
            string energyText = UIFormatting.FormatEnergyBand(energyBand);
            string energyClass = UIFormatting.EnergyBandClass(energyBand);

            // Contract expiry & renewal badge
            string contractExpiryText;
            bool showRenewalBadge = false;
            if (isFounder) {
                contractExpiryText = "Permanent";
            } else if (emp.contractExpiryTick <= 0) {
                contractExpiryText = "--";
            } else {
                int daysToExpiry = (emp.contractExpiryTick - currentTick) / TimeState.TicksPerDay;
                if (daysToExpiry < 0) daysToExpiry = 0;
                contractExpiryText = daysToExpiry + "d";
                if (daysToExpiry <= 60) showRenewalBadge = true;
            }

            totalCapacity += emp.EffectiveOutput;

            _employees.Add(new EmployeeRowDisplay {
                Id = emp.id,
                Name = emp.name,
                RoleName = UIFormatting.FormatRole(emp.role),
                RolePillClass = UIFormatting.RolePillClass(emp.role),
                TypeBadge = typeBadge,
                TypeBadgeClass = typeBadgeClass,
                TeamName = teamName,
                IsFounder = isFounder,
                MoraleText = moraleText,
                MoraleClass = moraleClass,
                EnergyText = energyText,
                EnergyClass = energyClass,
                SalaryText = UIFormatting.FormatMoney(emp.salary) + "/mo",
                ContractExpiryText = contractExpiryText,
                ShowRenewalBadge = showRenewalBadge
            });
        }

        EmployeeCount = count;
        EffectiveCapacityText = count > 0 ? (totalCapacity / count * 100f).ToString("F0") + "%" : "--";

        SortEmployees();
    }

    private void SortEmployees() {
        _employees.Sort(CompareRows);
    }

    private int CompareRows(EmployeeRowDisplay a, EmployeeRowDisplay b) {
        switch (SortMode) {
            case EmployeeSortMode.Role:
                return string.Compare(a.RoleName, b.RoleName, System.StringComparison.Ordinal);
            case EmployeeSortMode.Salary:
                return string.Compare(b.SalaryText, a.SalaryText, System.StringComparison.Ordinal);
            case EmployeeSortMode.ContractExpiry:
                int aExpiry = a.IsFounder ? int.MaxValue : (a.ContractExpiryText == "--" ? int.MaxValue : ParseDays(a.ContractExpiryText));
                int bExpiry = b.IsFounder ? int.MaxValue : (b.ContractExpiryText == "--" ? int.MaxValue : ParseDays(b.ContractExpiryText));
                return aExpiry.CompareTo(bExpiry);
            case EmployeeSortMode.Morale:
                return string.Compare(b.MoraleText, a.MoraleText, System.StringComparison.Ordinal);
            default:
                return string.Compare(a.Name, b.Name, System.StringComparison.Ordinal);
        }
    }

    private static int ParseDays(string text) {
        if (string.IsNullOrEmpty(text)) return int.MaxValue;
        int d = 0;
        for (int i = 0; i < text.Length; i++) {
            char c = text[i];
            if (c >= '0' && c <= '9') d = d * 10 + (c - '0');
            else break;
        }
        return d;
    }
}
