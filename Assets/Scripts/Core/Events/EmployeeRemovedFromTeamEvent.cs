public class EmployeeRemovedFromTeamEvent : GameEvent
{
    public EmployeeId EmployeeId { get; }
    public TeamId TeamId { get; }
    
    public EmployeeRemovedFromTeamEvent(int tick, EmployeeId employeeId, TeamId teamId) : base(tick)
    {
        EmployeeId = employeeId;
        TeamId = teamId;
    }
}
