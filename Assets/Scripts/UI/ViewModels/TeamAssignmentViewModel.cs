using System.Collections.Generic;

public struct TeamMemberDisplay
{
    public EmployeeId Id;
    public string Name;
    public string RoleName;
    public string RolePillClass;
    public string TypeText;
    public string MoraleText;
    public string MoraleClass;
    public float EffectiveOutput;
}

public struct TeamCardDisplay
{
    public TeamId Id;
    public string Name;
    public TeamType TeamTypeEnum;
    public string TeamTypeName;
    public string TeamTypeBadgeClass;
    public int MemberCount;
    public string ChemistryText;
    public string ChemistryClass;
    public bool HasActiveAssignment;
    public string AssignmentLabel;
    public string ActiveProjectName;
    public string EffectiveCapacity;
    public List<TeamMemberDisplay> Members;
}

public class TeamAssignmentViewModel : IViewModel
{
    private readonly List<TeamCardDisplay> _teams = new List<TeamCardDisplay>(8);
    public List<TeamCardDisplay> Teams => _teams;
    public bool HasTeams => _teams.Count > 0;

    private readonly List<TeamMemberDisplay> _memberScratch = new List<TeamMemberDisplay>(8);

    public bool IsDirty { get; private set; }
    public void ClearDirty() => IsDirty = false;

    public void Refresh(GameStateSnapshot snapshot) {
        IReadOnlyGameState state = snapshot;
        if (state == null) return;
        _teams.Clear();

        var allTeams = state.ActiveTeams;
        int teamCount = allTeams.Count;
        for (int t = 0; t < teamCount; t++) {
            var team = allTeams[t];
            if (team.ownerCompanyId != CompanyId.Player) continue;

            var memberRoles = state.GetTeamMemberRoles(team.id);
            var chemistry = state.GetTeamChemistry(team.id);
            var contract = state.GetContractForTeam(team.id);
            bool onProduct = state.IsTeamAssignedToProduct(team.id);

            bool hasAssignment = contract != null || onProduct;
            string assignmentLabel = "";
            string activeProjectName = "";

            if (contract != null) {
                assignmentLabel = "On Contract";
                activeProjectName = contract.Name;
            } else if (onProduct) {
                assignmentLabel = "On Product";
                var productId = state.GetProductForTeam(team.id);
                if (productId.HasValue && state.DevelopmentProducts.TryGetValue(productId.Value, out var prod)) {
                    activeProjectName = prod.ProductName;
                }
            }

            string chemText = chemistry.Score + "%";
            string chemClass = ChemistryBadgeClass(chemistry.Score);

            _memberScratch.Clear();
            float totalCapacity = 0f;
            int roleCount = memberRoles.Count;
            for (int m = 0; m < roleCount; m++) {
                var mr = memberRoles[m];
                var emp = FindEmployee(state, mr.EmployeeId);
                string typeText = "";
                string moraleText = "";
                string moraleClass = "";
                float output = 1f;
                if (emp != null) {
                    typeText = emp.ArrangementType == EmploymentType.PartTime ? "PT" : "FT";
                    output = emp.EffectiveOutput;
                    totalCapacity += output;
                    moraleText = MoraleLabel(emp.morale);
                    moraleClass = MoraleClass(emp.morale);
                }
                _memberScratch.Add(new TeamMemberDisplay {
                    Id = mr.EmployeeId,
                    Name = mr.Name,
                    RoleName = UIFormatting.FormatRole(mr.EmployeeRole),
                    RolePillClass = UIFormatting.RolePillClass(mr.EmployeeRole),
                    TypeText = typeText,
                    MoraleText = moraleText,
                    MoraleClass = moraleClass,
                    EffectiveOutput = output
                });
            }

            var members = new List<TeamMemberDisplay>(_memberScratch.Count);
            for (int m = 0; m < _memberScratch.Count; m++) {
                members.Add(_memberScratch[m]);
            }

            string effectiveCapacity = team.MemberCount > 0
                ? totalCapacity.ToString("F1") + " FTE"
                : "0 FTE";

            _teams.Add(new TeamCardDisplay {
                Id = team.id,
                Name = team.name,
                TeamTypeEnum = team.teamType,
                TeamTypeName = UIFormatting.FormatTeamType(team.teamType),
                TeamTypeBadgeClass = UIFormatting.TeamTypeBadgeClass(team.teamType),
                MemberCount = team.MemberCount,
                ChemistryText = chemText,
                ChemistryClass = chemClass,
                HasActiveAssignment = hasAssignment,
                AssignmentLabel = assignmentLabel,
                ActiveProjectName = activeProjectName,
                EffectiveCapacity = effectiveCapacity,
                Members = members
            });
        }
    }

    private static Employee FindEmployee(IReadOnlyGameState state, EmployeeId id) {
        var emps = state.ActiveEmployees;
        int count = emps.Count;
        for (int i = 0; i < count; i++) {
            if (emps[i].id == id) return emps[i];
        }
        return null;
    }

    private static string ChemistryBadgeClass(int score) {
        if (score >= 70) return "badge--success";
        if (score < 50) return "badge--warning";
        return "badge--neutral";
    }

    private static string MoraleLabel(int morale) {
        if (morale >= 80) return "High";
        if (morale >= 50) return "Normal";
        return "Low";
    }

    private static string MoraleClass(int morale) {
        if (morale >= 80) return "text-success";
        if (morale >= 50) return "text-muted";
        return "text-warning";
    }
}
