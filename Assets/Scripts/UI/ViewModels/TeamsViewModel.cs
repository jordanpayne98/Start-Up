using System.Collections.Generic;

public struct TeamSummaryDisplay
{
    public TeamId Id;
    public string Name;
    public int MemberCount;
    public string ContractName;
    public string TeamType;
    public TeamType TeamTypeEnum;
    public int AvgMorale;
    public bool IsCrunching;
    public string MoraleBand;
}

public struct TeamMemberDisplay
{
    public EmployeeId EmployeeId;
    public string Name;
    public string Role;
    public int Morale;
    public string MoraleBand;
    public string MoraleBandClass;
}

public struct TeamDetailDisplay
{
    public TeamId Id;
    public string Name;
    public string TeamType;
    public TeamType TeamTypeEnum;
    public string ContractName;
    public ContractId? AssignedContractId;
    public List<TeamMemberDisplay> Members;
    public bool IsCrunching;
    public string MoraleBand;
}

public struct ContractSummaryDisplay
{
    public ContractId Id;
    public string Name;
}

public struct ProductSummaryDisplay
{
    public ProductId Id;
    public string Name;
}

public class TeamsViewModel : IViewModel
{
    private readonly List<TeamSummaryDisplay> _teams = new List<TeamSummaryDisplay>();
    public List<TeamSummaryDisplay> Teams => _teams;

    public TeamId? SelectedTeamId { get; private set; }
    public TeamDetailDisplay SelectedTeam { get; private set; }

    private readonly List<EmployeeRowDisplay> _unassignedEmployees = new List<EmployeeRowDisplay>();
    public List<EmployeeRowDisplay> UnassignedEmployees => _unassignedEmployees;

    private readonly List<ContractSummaryDisplay> _availableContracts = new List<ContractSummaryDisplay>();
    public List<ContractSummaryDisplay> AvailableContracts => _availableContracts;

    private readonly List<ProductSummaryDisplay> _availableProducts = new List<ProductSummaryDisplay>();
    public List<ProductSummaryDisplay> AvailableProducts => _availableProducts;

    private readonly List<TeamSummaryDisplay> _otherTeams = new List<TeamSummaryDisplay>();
    public List<TeamSummaryDisplay> OtherTeams => _otherTeams;

    public ProductId? AssignedProductId { get; private set; }

    public string MemberFilterText { get; private set; } = "";

    public void SetMemberFilter(string text) {
        MemberFilterText = text ?? "";
    }

    private IReadOnlyGameState _lastState;

    public void SelectTeam(TeamId id) {
        SelectedTeamId = id;
        if (_lastState != null) RefreshSelectedTeam(_lastState);
    }

    public void Refresh(IReadOnlyGameState state) {
        if (state == null) return;
        _lastState = state;

        _teams.Clear();
        var teams = state.ActiveTeams;
        int count = teams.Count;
        for (int i = 0; i < count; i++) {
            var team = teams[i];
            if (!team.isActive) continue;

            string contractName = "None";
            var contract = state.GetContractForTeam(team.id);
            if (contract != null) contractName = contract.Name;

            int avgMorale = 0;
            int memberCount = team.members.Count;
            if (memberCount > 0) {
                int total = 0;
                var employees = state.ActiveEmployees;
                for (int m = 0; m < memberCount; m++) {
                    int empCount = employees.Count;
                    for (int e = 0; e < empCount; e++) {
                        if (employees[e].id == team.members[m]) {
                            total += employees[e].morale;
                            break;
                        }
                    }
                }
                avgMorale = total / memberCount;
            }

            _teams.Add(new TeamSummaryDisplay {
                Id = team.id,
                Name = team.name,
                MemberCount = memberCount,
                ContractName = contractName,
                TeamType = UIFormatting.FormatTeamType(team.teamType),
                TeamTypeEnum = team.teamType,
                AvgMorale = avgMorale,
                IsCrunching = team.isCrunching,
                MoraleBand = GetMoraleBand(avgMorale)
            });
        }

        if (SelectedTeamId.HasValue) RefreshSelectedTeam(state);
        else if (_teams.Count > 0) {
            SelectedTeamId = _teams[0].Id;
            RefreshSelectedTeam(state);
        }

        RefreshUnassignedEmployees(state);
        RefreshAvailableContracts(state);
        RefreshAvailableProducts(state);
        RefreshOtherTeams();
        RefreshAssignedProduct(state);
    }

    private void RefreshUnassignedEmployees(IReadOnlyGameState state) {
        _unassignedEmployees.Clear();
        var employees = state.ActiveEmployees;
        int count = employees.Count;
        for (int i = 0; i < count; i++) {
            var emp = employees[i];
            if (!emp.isActive) continue;
            if (state.GetEmployeeTeam(emp.id).HasValue) continue;
            _unassignedEmployees.Add(new EmployeeRowDisplay {
                Id = emp.id,
                Name = emp.name,
                Role = UIFormatting.FormatRole(emp.role)
            });
        }
    }

    private void RefreshAvailableContracts(IReadOnlyGameState state) {
        _availableContracts.Clear();
        foreach (var contract in state.GetActiveContracts()) {
            if (contract.Status != ContractStatus.Accepted && contract.Status != ContractStatus.InProgress) continue;
            if (!contract.AssignedTeamId.HasValue || (SelectedTeamId.HasValue && contract.AssignedTeamId.Value == SelectedTeamId.Value)) {
                _availableContracts.Add(new ContractSummaryDisplay { Id = contract.Id, Name = contract.Name });
            }
        }
    }

    private void RefreshAvailableProducts(IReadOnlyGameState state) {
        _availableProducts.Clear();
        if (state.DevelopmentProducts == null) return;
        foreach (var kvp in state.DevelopmentProducts) {
            var product = kvp.Value;
            if (!product.IsInDevelopment) continue;
            _availableProducts.Add(new ProductSummaryDisplay { Id = product.Id, Name = product.ProductName });
        }
    }

    private void RefreshOtherTeams() {
        _otherTeams.Clear();
        int count = _teams.Count;
        for (int i = 0; i < count; i++) {
            if (SelectedTeamId.HasValue && _teams[i].Id == SelectedTeamId.Value) continue;
            _otherTeams.Add(_teams[i]);
        }
    }

    private void RefreshAssignedProduct(IReadOnlyGameState state) {
        AssignedProductId = null;
        if (!SelectedTeamId.HasValue) return;
        AssignedProductId = state.GetProductForTeam(SelectedTeamId.Value);
    }

    private void RefreshSelectedTeam(IReadOnlyGameState state) {
        if (!SelectedTeamId.HasValue) return;

        var teams = state.ActiveTeams;
        Team selectedTeam = null;
        int count = teams.Count;
        for (int i = 0; i < count; i++) {
            if (teams[i].id == SelectedTeamId.Value) {
                selectedTeam = teams[i];
                break;
            }
        }

        if (selectedTeam == null) return;

        string contractName = "None";
        ContractId? contractId = null;
        var contract = state.GetContractForTeam(selectedTeam.id);
        if (contract != null) {
            contractName = contract.Name;
            contractId = contract.Id;
        }

        var memberRoles = state.GetTeamMemberRoles(selectedTeam.id);
        var members = new List<TeamMemberDisplay>();
        int memberCount = memberRoles.Count;
        var employees = state.ActiveEmployees;
        int empCount = employees.Count;
        for (int i = 0; i < memberCount; i++) {
            var mr = memberRoles[i];
            int morale = 0;
            for (int e = 0; e < empCount; e++) {
                if (employees[e].id == mr.EmployeeId) {
                    morale = employees[e].morale;
                    break;
                }
            }
            members.Add(new TeamMemberDisplay {
                EmployeeId = mr.EmployeeId,
                Name = mr.Name,
                Role = UIFormatting.FormatRole(mr.EmployeeRole),
                Morale = morale,
                MoraleBand = GetMoraleBand(morale),
                MoraleBandClass = GetMoraleBandClass(morale)
            });
        }

        int avgTeamMorale = 0;
        if (memberCount > 0) {
            int total = 0;
            for (int i = 0; i < memberCount; i++) total += members[i].Morale;
            avgTeamMorale = total / memberCount;
        }

        SelectedTeam = new TeamDetailDisplay {
            Id = selectedTeam.id,
            Name = selectedTeam.name,
            TeamType = UIFormatting.FormatTeamType(selectedTeam.teamType),
            TeamTypeEnum = selectedTeam.teamType,
            ContractName = contractName,
            AssignedContractId = contractId,
            Members = members,
            IsCrunching = selectedTeam.isCrunching,
            MoraleBand = GetMoraleBand(avgTeamMorale)
        };
    }

    public List<TeamSummaryDisplay> GetTeamsByType(global::TeamType type) {
        var result = new List<TeamSummaryDisplay>();
        int count = _teams.Count;
        for (int i = 0; i < count; i++) {
            if (_teams[i].TeamTypeEnum == type) result.Add(_teams[i]);
        }
        return result;
    }

    private static string GetMoraleBand(int morale) {
        if (morale >= 90) return "Inspired";
        if (morale >= 75) return "Motivated";
        if (morale >= 55) return "Stable";
        if (morale >= 35) return "Unhappy";
        if (morale >= 20) return "Miserable";
        return "Critical";
    }

    private static string GetMoraleBandClass(int morale) {
        if (morale >= 90) return "morale-band--inspired";
        if (morale >= 75) return "morale-band--motivated";
        if (morale >= 55) return "morale-band--stable";
        if (morale >= 35) return "morale-band--unhappy";
        if (morale >= 20) return "morale-band--miserable";
        return "morale-band--critical";
    }
}
