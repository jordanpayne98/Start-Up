using System.Collections.Generic;

public class AssignmentModalViewModel : IViewModel
{
    private static readonly RoleId[] _roles = (RoleId[])System.Enum.GetValues(typeof(RoleId));

    private readonly List<string> _availableRoles = new List<string>();
    public List<string> AvailableRoles => _availableRoles;

    public int SelectedRoleIndex { get; set; }
    public int MinAbility        { get; set; }
    public int MinPotential      { get; set; }
    public int BatchSize         { get; set; } = 1;

    public int EstimatedCost         { get; private set; }
    public int EstimatedDurationDays { get; private set; }
    public bool CanConfirm           { get; private set; }
    public bool IsEditMode           { get; private set; }
    public bool HasHRTeam            { get; private set; }

    // The resolved HR team id — populated during Refresh, used by the View when dispatching the command
    public TeamId HRTeamId { get; private set; }

    public RoleId SelectedRole {
        get {
            if (SelectedRoleIndex >= 0 && SelectedRoleIndex < _roles.Length)
                return _roles[SelectedRoleIndex];
            return RoleId.SoftwareEngineer;
        }
    }

    public void SetEditMode(bool isEdit) {
        IsEditMode = isEdit;
    }

    public bool IsDirty { get; private set; }
    public void ClearDirty() => IsDirty = false;

    public void Refresh(GameStateSnapshot snapshot) {
        IReadOnlyGameState state = snapshot;
        if (state == null) return;

        if (_availableRoles.Count == 0) {
            int roleCount = _roles.Length;
            for (int i = 0; i < roleCount; i++) {
                _availableRoles.Add(RoleIdHelper.GetName(_roles[i]));
            }
        }

        // Find HR team
        TeamId? hrTeamId = null;
        var teams = state.ActiveTeams;
        int teamCount = teams.Count;
        for (int i = 0; i < teamCount; i++) {
            var team = teams[i];
            if (state.GetTeamType(team.id) == TeamType.HR) {
                hrTeamId = team.id;
                break;
            }
        }

        HasHRTeam = hrTeamId.HasValue;
        if (hrTeamId.HasValue) {
            HRTeamId = hrTeamId.Value;
            EstimatedDurationDays = state.ComputeSearchDurationDays(HRTeamId);
        } else {
            HRTeamId = default;
            EstimatedDurationDays = 0;
        }

        int desiredSkillCount = 0;
        int batchSize = BatchSize < 1 ? 1 : BatchSize;
        EstimatedCost = state.ComputeSearchCost(MinAbility, MinPotential, desiredSkillCount, batchSize);

        CanConfirm = HasHRTeam && SelectedRoleIndex >= 0 && SelectedRoleIndex < _roles.Length;
    }
}
