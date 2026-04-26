public class TeamConflictEvent : GameEvent
{
    public TeamId TeamId;
    public EmployeeId EmployeeA;
    public EmployeeId EmployeeB;

    public TeamConflictEvent(int tick, TeamId teamId, EmployeeId employeeA, EmployeeId employeeB) : base(tick)
    {
        TeamId = teamId;
        EmployeeA = employeeA;
        EmployeeB = employeeB;
    }
}
