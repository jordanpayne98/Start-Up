public class EmployeeAssignedToTeamEvent : GameEvent
{
    public EmployeeId EmployeeId { get; }
    public TeamId TeamId { get; }
    
    public EmployeeAssignedToTeamEvent(int tick, EmployeeId employeeId, TeamId teamId) : base(tick)
    {
        EmployeeId = employeeId;
        TeamId = teamId;
    }
}
