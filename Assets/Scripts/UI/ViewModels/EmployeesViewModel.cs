using System.Collections.Generic;

public enum EmployeeSortColumn
{
    Name,
    Role,
    Salary,
    Morale,
    Ability,
    Team
}

public enum SortDirection
{
    Ascending,
    Descending
}

public struct EmployeeRowDisplay
{
    public EmployeeId Id;
    public string Name;
    public string Role;
    public string SalaryDisplay;
    public int SalaryRaw;
    public int Morale;
    public string MoraleBandLabel;
    public int AbilityStars;
    public int PotentialStars;
    public string TeamName;
    public int ProgrammingSkill;
    public int DesignSkill;
    public int QASkill;
    public int Age;
    public bool IsFounder;
}

public class EmployeesViewModel : IViewModel
{
    private readonly List<EmployeeRowDisplay> _employees = new List<EmployeeRowDisplay>();
    public List<EmployeeRowDisplay> Employees => _employees;

    public EmployeeSortColumn CurrentSort { get; private set; }
    public SortDirection CurrentDirection { get; private set; }
    public EmployeeId? SelectedEmployeeId { get; private set; }

    private IReadOnlyGameState _lastState;

    public EmployeesViewModel() {
        CurrentSort = EmployeeSortColumn.Name;
        CurrentDirection = SortDirection.Ascending;
    }

    public void Sort(EmployeeSortColumn column) {
        if (CurrentSort == column) {
            CurrentDirection = CurrentDirection == SortDirection.Ascending
                ? SortDirection.Descending : SortDirection.Ascending;
        } else {
            CurrentSort = column;
            CurrentDirection = SortDirection.Ascending;
        }
        ApplySort();
    }

    public void SelectEmployee(EmployeeId id) {
        SelectedEmployeeId = id;
    }

    public void Refresh(IReadOnlyGameState state) {
        if (state == null) return;
        _lastState = state;

        _employees.Clear();
        var employees = state.ActiveEmployees;
        int count = employees.Count;
        for (int i = 0; i < count; i++) {
            var emp = employees[i];
            if (!emp.isActive) continue;

            string teamName = "Unassigned";
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

            int ability = state.GetEmployeeAbility(emp.id);
            int potential = state.GetEmployeePotential(emp.id);

            _employees.Add(new EmployeeRowDisplay {
                Id = emp.id,
                Name = emp.name,
                Role = UIFormatting.FormatRole(emp.role),
                SalaryDisplay = (emp.isFounder && emp.salary == 0) ? "Founder" : UIFormatting.FormatMoney(emp.salary),
                SalaryRaw = emp.salary,
                Morale = emp.morale,
                MoraleBandLabel = MoraleBandHelper.GetMoraleBandLabel(MoraleBandHelper.GetMoraleBand(emp.morale)),
                AbilityStars = AbilityCalculator.AbilityToStars(ability),
                PotentialStars = AbilityCalculator.PotentialToStars(potential),
                TeamName = teamName,
                ProgrammingSkill = emp.GetSkill(SkillType.Programming),
                DesignSkill = emp.GetSkill(SkillType.Design),
                QASkill = emp.GetSkill(SkillType.QA),
                Age = emp.age,
                IsFounder = emp.isFounder
            });
        }

        ApplySort();
    }

    private void ApplySort() {
        int count = _employees.Count;
        if (count <= 1) return;

        // Simple insertion sort (no LINQ, no allocations beyond struct copies)
        for (int i = 1; i < count; i++) {
            var key = _employees[i];
            int j = i - 1;
            while (j >= 0 && CompareRows(_employees[j], key) > 0) {
                _employees[j + 1] = _employees[j];
                j--;
            }
            _employees[j + 1] = key;
        }
    }

    private int CompareRows(EmployeeRowDisplay a, EmployeeRowDisplay b) {
        int result = 0;
        switch (CurrentSort) {
            case EmployeeSortColumn.Name:
                result = string.Compare(a.Name, b.Name, System.StringComparison.Ordinal);
                break;
            case EmployeeSortColumn.Role:
                result = string.Compare(a.Role, b.Role, System.StringComparison.Ordinal);
                break;
            case EmployeeSortColumn.Salary:
                result = a.SalaryRaw.CompareTo(b.SalaryRaw);
                break;
            case EmployeeSortColumn.Morale:
                result = a.Morale.CompareTo(b.Morale);
                break;
            case EmployeeSortColumn.Ability:
                result = a.AbilityStars.CompareTo(b.AbilityStars);
                break;
            case EmployeeSortColumn.Team:
                result = string.Compare(a.TeamName, b.TeamName, System.StringComparison.Ordinal);
                break;
        }
        return CurrentDirection == SortDirection.Descending ? -result : result;
    }
}
