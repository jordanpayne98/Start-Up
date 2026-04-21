using System.Collections.Generic;

public struct SkillDisplay
{
    public string Name;
    public int Value;
    public int MaxValue;
    public float XpProgress;    // 0.0–1.0, progress toward next level
    public int DeltaDirection;  // 1 = growth, -1 = decay, 0 = no change
}

public struct MoraleModifierDisplay
{
    public string Source;
    public int Value;
}

public class EmployeeProfileViewModel : IViewModel
{
    public EmployeeId EmployeeId { get; private set; }
    public string Name { get; private set; }
    public string Role { get; private set; }
    public string TeamName { get; private set; }
    public int AbilityStars { get; private set; }
    public int PotentialStars { get; private set; }
    public int Morale { get; private set; }
    public string MoraleLabel { get; private set; }
    public string MoraleBandLabel { get; private set; }
    public string SalaryDisplay { get; private set; }
    public string HireDateDisplay { get; private set; }
    public bool CanFire { get; private set; }
    public bool CanAssignToTeam { get; private set; }
    public bool IsFounder { get; private set; }

    private readonly List<SkillDisplay> _skills = new List<SkillDisplay>();
    public List<SkillDisplay> Skills => _skills;

    public EmployeeProfileViewModel() {
        Name = "";
        Role = "";
        TeamName = "Unassigned";
    }

    public void SetEmployee(EmployeeId id) {
        EmployeeId = id;
    }

    public void Refresh(IReadOnlyGameState state) {
        if (state == null) return;

        var employees = state.ActiveEmployees;
        Employee employee = null;
        int count = employees.Count;
        for (int i = 0; i < count; i++) {
            if (employees[i].id == EmployeeId) {
                employee = employees[i];
                break;
            }
        }

        if (employee == null || !employee.isActive) return;

        Name = employee.name;
        Role = UIFormatting.FormatRole(employee.role);
        SalaryDisplay = (employee.isFounder && employee.salary == 0) ? "Founder" : UIFormatting.FormatMoney(employee.salary);
        Morale = employee.morale;
        HireDateDisplay = "Hired: Day " + (employee.hireDate / 4800);

        var band = MoraleBandHelper.GetMoraleBand(employee.morale);
        MoraleBandLabel = MoraleBandHelper.GetMoraleBandLabel(band);
        MoraleLabel = MoraleBandLabel;

        int ability = state.GetEmployeeAbility(EmployeeId);
        int potential = state.GetEmployeePotential(EmployeeId);
        AbilityStars = AbilityCalculator.AbilityToStars(ability);
        PotentialStars = AbilityCalculator.PotentialToStars(potential);

        var teamId = state.GetEmployeeTeam(EmployeeId);
        TeamName = "Unassigned";
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

        IsFounder = employee.isFounder;
        CanFire = !employee.isFounder;
        CanAssignToTeam = true;

        // Skills
        _skills.Clear();
        for (int s = 0; s < SkillTypeHelper.SkillTypeCount; s++) {
            _skills.Add(new SkillDisplay {
                Name          = SkillTypeHelper.GetName((SkillType)s),
                Value         = employee.GetSkill((SkillType)s),
                MaxValue      = 20,
                XpProgress    = employee.skillXp != null ? employee.skillXp[s] : 0f,
                DeltaDirection = employee.skillDeltaDirection != null ? employee.skillDeltaDirection[s] : (sbyte)0
            });
        }
    }
}
