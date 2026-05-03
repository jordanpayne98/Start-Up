using System.Collections.Generic;

public struct AvailableEmployeeDisplay
{
    public EmployeeId Id;
    public string Name;
    public string RoleName;
    public string RolePillClass;
    public string TypeText;
    public RoleId Role;
}

public class AddMemberModalViewModel : IViewModel
{
    private TeamId _teamId;
    public TeamId TeamId => _teamId;
    public string TeamName { get; private set; }

    private readonly List<AvailableEmployeeDisplay> _availableEmployees = new List<AvailableEmployeeDisplay>(16);
    public List<AvailableEmployeeDisplay> AvailableEmployees => _availableEmployees;
    public bool HasAvailable => _availableEmployees.Count > 0;

    public void SetTeamId(TeamId id) {
        _teamId = id;
    }

    public bool IsDirty { get; private set; }
    public void ClearDirty() => IsDirty = false;

    public void Refresh(GameStateSnapshot snapshot) {
        IReadOnlyGameState state = snapshot;
        if (state == null) return;
        _availableEmployees.Clear();
        TeamName = "";

        var allTeams = state.ActiveTeams;
        int teamCount = allTeams.Count;
        for (int t = 0; t < teamCount; t++) {
            if (allTeams[t].id == _teamId) {
                TeamName = allTeams[t].name;
                break;
            }
        }

        var employees = state.ActiveEmployees;
        int count = employees.Count;
        for (int i = 0; i < count; i++) {
            var emp = employees[i];
            if (emp.ownerCompanyId != CompanyId.Player) continue;
            if (!emp.isActive) continue;
            var assignedTeam = state.GetEmployeeTeam(emp.id);
            if (assignedTeam.HasValue) continue;

            _availableEmployees.Add(new AvailableEmployeeDisplay {
                Id = emp.id,
                Name = emp.name,
                RoleName = UIFormatting.FormatRole(emp.role),
                RolePillClass = UIFormatting.RolePillClass(emp.role),
                TypeText = emp.ArrangementType == EmploymentType.PartTime ? "PT" : "FT",
                Role = emp.role
            });
        }
        IsDirty = true;
    }
}
