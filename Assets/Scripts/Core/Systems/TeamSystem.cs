// TeamSystem Version: Clean v1
using System;
using System.Collections.Generic;

public class TeamSystem : ISystem
{
    public event Action<TeamId> OnTeamCreated;
    public event Action<TeamId> OnTeamDeleted;
    public event Action<TeamId> OnTeamRenamed;
    public event Action<EmployeeId, TeamId> OnEmployeeAssignedToTeam;
    public event Action<EmployeeId, TeamId> OnEmployeeRemovedFromTeam;
    public event Action<TeamId, TeamType> OnTeamFreed;
    public event Action<TeamId, bool> OnCrunchModeChanged;

    private TeamState _state;
    private ILogger _logger;
    private EmployeeSystem _employeeSystem;
    private readonly List<TeamId> _scratchFreeTeams;
    private readonly List<TeamId> _companyFreeTeamsScratch;
    private readonly List<Team> _activeTeamsCache;
    private readonly List<Team> _companyTeamsScratch;

    private enum PendingEventKind
    {
        TeamCreated,
        TeamDeleted,
        TeamRenamed,
        EmployeeAssigned,
        EmployeeRemoved,
        TeamFreed,
        CrunchModeChanged,
    }

    private struct PendingEvent
    {
        public PendingEventKind Kind;
        public TeamId TeamId;
        public EmployeeId EmployeeId;
        public TeamType TeamType;
        public bool BoolValue;
    }

    private readonly List<PendingEvent> _pendingEvents;

    public TeamSystem(TeamState state, ILogger logger)
    {
        _state = state;
        _logger = logger ?? new NullLogger();
        _pendingEvents = new List<PendingEvent>(16);
        _scratchFreeTeams = new List<TeamId>(8);
        _companyFreeTeamsScratch = new List<TeamId>(8);
        _activeTeamsCache = new List<Team>(16);
        _companyTeamsScratch = new List<Team>(8);
    }
    
    public void SetEmployeeSystem(EmployeeSystem employeeSystem)
    {
        _employeeSystem = employeeSystem;
    }

    public TeamId CreateTeam(TeamType type, int currentTick, CompanyId companyId = default, string customName = null)
    {
        string name = string.IsNullOrEmpty(customName) ? GenerateTeamName(type, companyId) : customName;
        var teamId = new TeamId(_state.nextTeamId++);
        var team = new Team(teamId, name);
        team.teamType = type;
        team.ownerCompanyId = companyId;
        
        _state.teams[teamId] = team;

        _pendingEvents.Add(new PendingEvent { Kind = PendingEventKind.TeamCreated, TeamId = teamId });

        _logger.Log($"[Tick {currentTick}] Created team '{name}' (ID: {teamId.Value}, Type: {type}, Company: {companyId.Value})");
        
        return teamId;
    }

    private string GenerateTeamName(TeamType type, CompanyId companyId)
    {
        string baseName = type.ToString();
        int count = 0;
        var keys = new List<TeamId>(_state.teams.Keys);
        for (int i = 0; i < keys.Count; i++)
        {
            var t = _state.teams[keys[i]];
            if (t.isActive && t.teamType == type && t.ownerCompanyId == companyId)
                count++;
        }
        return count == 0 ? baseName : baseName + " " + (count + 1);
    }
    
    public bool DeleteTeam(TeamId teamId)
    {
        if (!_state.teams.TryGetValue(teamId, out var team))
        {
            _logger.LogWarning($"Cannot delete team {teamId.Value}: Not found");
            return false;
        }
        
        var members = team.members;
        for (int i = 0; i < members.Count; i++)
        {
            _state.employeeToTeam.Remove(members[i]);
        }

        team.isActive = false;
        _state.teams.Remove(teamId);

        _pendingEvents.Add(new PendingEvent { Kind = PendingEventKind.TeamDeleted, TeamId = teamId });
        
        _logger.Log($"Deleted team '{team.name}' (ID: {teamId.Value})");
        
        return true;
    }
    
    public bool RenameTeam(TeamId teamId, string newName)
    {
        if (!_state.teams.TryGetValue(teamId, out var team))
        {
            _logger.LogWarning($"Cannot rename team {teamId.Value}: Not found");
            return false;
        }

        if (string.IsNullOrWhiteSpace(newName))
        {
            _logger.LogWarning($"Cannot rename team {teamId.Value}: Name is null, empty, or whitespace");
            return false;
        }

        team.name = newName.Trim();

        _pendingEvents.Add(new PendingEvent { Kind = PendingEventKind.TeamRenamed, TeamId = teamId });

        _logger.Log($"Renamed team {teamId.Value} to '{team.name}'");
        return true;
    }

    public bool AssignEmployeeToTeam(EmployeeId employeeId, TeamId teamId)
    {
        if (!_state.teams.TryGetValue(teamId, out var team))
        {
            _logger.LogWarning($"Cannot assign employee {employeeId.Value} to team {teamId.Value}: Team not found");
            return false;
        }
        
        if (_state.employeeToTeam.TryGetValue(employeeId, out var currentTeamId))
        {
            if (currentTeamId == teamId)
            {
                _logger.LogWarning($"Employee {employeeId.Value} is already on team {teamId.Value}");
                return false;
            }
            
            RemoveEmployeeFromTeam(employeeId);
        }
        
        team.members.Add(employeeId);
        _state.employeeToTeam[employeeId] = teamId;

        _pendingEvents.Add(new PendingEvent { Kind = PendingEventKind.EmployeeAssigned, EmployeeId = employeeId, TeamId = teamId });
        
        _logger.Log($"Assigned employee {employeeId.Value} to team '{team.name}' (ID: {teamId.Value})");
        
        return true;
    }
    
    public bool RemoveEmployeeFromTeam(EmployeeId employeeId)
    {
        if (!_state.employeeToTeam.TryGetValue(employeeId, out var teamId))
        {
            _logger.LogWarning($"Cannot remove employee {employeeId.Value} from team: Not assigned to any team");
            return false;
        }
        
        if (_state.teams.TryGetValue(teamId, out var team))
        {
            team.members.Remove(employeeId);
        }
        
        _state.employeeToTeam.Remove(employeeId);

        _pendingEvents.Add(new PendingEvent { Kind = PendingEventKind.EmployeeRemoved, EmployeeId = employeeId, TeamId = teamId });
        
        _logger.Log($"Removed employee {employeeId.Value} from team {teamId.Value}");
        
        return true;
    }
    
    public Team GetTeam(TeamId teamId)
    {
        if (_state.teams.TryGetValue(teamId, out var team))
        {
            return team;
        }
        return null;
    }
    
    public TeamId? GetEmployeeTeam(EmployeeId employeeId)
    {
        if (_state.employeeToTeam.TryGetValue(employeeId, out var teamId))
        {
            return teamId;
        }
        return null;
    }
    
    public IReadOnlyList<Team> GetAllActiveTeams()
    {
        _activeTeamsCache.Clear();
        var keys = new List<TeamId>(_state.teams.Keys);
        for (int i = 0; i < keys.Count; i++)
        {
            var team = _state.teams[keys[i]];
            if (team.isActive)
                _activeTeamsCache.Add(team);
        }
        return _activeTeamsCache;
    }
    
    public TeamType GetTeamType(TeamId teamId)
    {
        if (_state.teams.TryGetValue(teamId, out var team))
            return team.teamType;
        return TeamType.Development;
    }

    public TeamId? GetHRTeamId(CompanyId companyId = default)
    {
        var keys = new List<TeamId>(_state.teams.Keys);
        for (int i = 0; i < keys.Count; i++)
        {
            var team = _state.teams[keys[i]];
            if (team.isActive && team.teamType == TeamType.HR && team.ownerCompanyId == companyId)
                return keys[i];
        }
        return null;
    }

    public TeamState GetTeamState()
    {
        return _state;
    }

    public List<TeamId> GetFreeTeamsByType(TeamType type)
    {
        _scratchFreeTeams.Clear();
        var keys = new List<TeamId>(_state.teams.Keys);
        for (int i = 0; i < keys.Count; i++)
        {
            var team = _state.teams[keys[i]];
            if (team.isActive && team.teamType == type)
                _scratchFreeTeams.Add(keys[i]);
        }
        return _scratchFreeTeams;
    }

    public List<Team> GetActiveTeamsForCompany(CompanyId companyId)
    {
        _companyTeamsScratch.Clear();
        var keys = new List<TeamId>(_state.teams.Keys);
        for (int i = 0; i < keys.Count; i++)
        {
            var team = _state.teams[keys[i]];
            if (team.isActive && team.ownerCompanyId == companyId)
                _companyTeamsScratch.Add(team);
        }
        return _companyTeamsScratch;
    }

    public List<TeamId> GetFreeTeamsByTypeForCompany(TeamType type, CompanyId companyId)
    {
        _companyFreeTeamsScratch.Clear();
        var keys = new List<TeamId>(_state.teams.Keys);
        for (int i = 0; i < keys.Count; i++)
        {
            var team = _state.teams[keys[i]];
            if (team.isActive && team.teamType == type && team.ownerCompanyId == companyId)
                _companyFreeTeamsScratch.Add(keys[i]);
        }
        return _companyFreeTeamsScratch;
    }

    public void NotifyTeamFreed(TeamId teamId)
    {
        if (!_state.teams.TryGetValue(teamId, out var team)) return;
        _pendingEvents.Add(new PendingEvent { Kind = PendingEventKind.TeamFreed, TeamId = teamId, TeamType = team.teamType });
    }

    public void PreTick(int tick)
    {
    }
    
    public void Tick(int tick)
    {
    }
    
    public void PostTick(int tick)
    {
        for (int i = 0; i < _pendingEvents.Count; i++)
        {
            var e = _pendingEvents[i];
            switch (e.Kind)
            {
                case PendingEventKind.TeamCreated:          OnTeamCreated?.Invoke(e.TeamId); break;
                case PendingEventKind.TeamDeleted:          OnTeamDeleted?.Invoke(e.TeamId); break;
                case PendingEventKind.TeamRenamed:          OnTeamRenamed?.Invoke(e.TeamId); break;
                case PendingEventKind.EmployeeAssigned:     OnEmployeeAssignedToTeam?.Invoke(e.EmployeeId, e.TeamId); break;
                case PendingEventKind.EmployeeRemoved:      OnEmployeeRemovedFromTeam?.Invoke(e.EmployeeId, e.TeamId); break;
                case PendingEventKind.TeamFreed:            OnTeamFreed?.Invoke(e.TeamId, e.TeamType); break;
                case PendingEventKind.CrunchModeChanged:    OnCrunchModeChanged?.Invoke(e.TeamId, e.BoolValue); break;
            }
        }
        _pendingEvents.Clear();
    }
    
    public void ApplyCommand(ICommand command)
    {
        if (command is CreateTeamCommand createTeam)
        {
            CreateTeam(createTeam.TeamType, command.Tick, createTeam.CompanyId, createTeam.Name);
        }
        else if (command is DeleteTeamCommand deleteTeam)
        {
            DeleteTeam(deleteTeam.TeamId);
        }
        else if (command is AssignEmployeeToTeamCommand assignEmployee)
        {
            AssignEmployeeToTeam(assignEmployee.EmployeeId, assignEmployee.TeamId);
        }
        else if (command is RemoveEmployeeFromTeamCommand removeEmployee)
        {
            RemoveEmployeeFromTeam(removeEmployee.EmployeeId);
        }
        else if (command is RenameTeamCommand renameTeam)
        {
            RenameTeam(renameTeam.TeamId, renameTeam.NewName);
        }
        else if (command is SetCrunchModeCommand crunchCmd)
        {
            if (_state.teams.TryGetValue(crunchCmd.TeamId, out var crunchTeam))
            {
                crunchTeam.isCrunching = crunchCmd.Enable;
                _pendingEvents.Add(new PendingEvent { Kind = PendingEventKind.CrunchModeChanged, TeamId = crunchCmd.TeamId, BoolValue = crunchCmd.Enable });
                _logger.Log($"[TeamSystem] Team {crunchCmd.TeamId.Value} crunch mode set to {crunchCmd.Enable}");
            }
        }
    }
    
    public void Dispose()
    {
        _pendingEvents.Clear();
        OnTeamCreated = null;
        OnTeamDeleted = null;
        OnTeamRenamed = null;
        OnEmployeeAssignedToTeam = null;
        OnEmployeeRemovedFromTeam = null;
        OnTeamFreed = null;
        OnCrunchModeChanged = null;
    }
}
