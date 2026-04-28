public class SkillImprovedEvent : GameEvent
{
    public EmployeeId EmployeeId { get; }
    public SkillId Skill { get; }
    public int NewSkillValue { get; }

    public SkillImprovedEvent(int tick, EmployeeId employeeId, SkillId skill, int newSkillValue) : base(tick) {
        EmployeeId = employeeId;
        Skill = skill;
        NewSkillValue = newSkillValue;
    }
}
