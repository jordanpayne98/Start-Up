using System.Collections.Generic;

public class ProductSkillsAwardedEvent : GameEvent
{
    public List<EmployeeId> EmployeeIds;

    public ProductSkillsAwardedEvent(int tick, List<EmployeeId> employeeIds) : base(tick)
    {
        EmployeeIds = employeeIds;
    }
}
