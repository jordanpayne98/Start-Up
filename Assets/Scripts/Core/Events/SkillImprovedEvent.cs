public class SkillImprovedEvent : GameEvent
{
    public EmployeeId EmployeeId { get; }
    public SkillType Skill { get; }
    public int NewSkillValue { get; }

    public SkillImprovedEvent(int tick, EmployeeId employeeId, SkillType skill, int newSkillValue) : base(tick) {
        EmployeeId = employeeId;
        Skill = skill;
        NewSkillValue = newSkillValue;
    }
}
