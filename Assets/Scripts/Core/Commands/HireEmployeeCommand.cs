using System.Collections.Generic;

public struct HireEmployeeCommand : ICommand
{
    public int Tick { get; set; }
    public int CandidateId;
    public string Name;
    public Gender Gender;
    public int Age;
    public int[] Skills;
    public int HRSkill;
    public int Salary;
    public EmployeeRole Role;
    public bool BlindHire;
    public HiringMode Mode;
    public int PotentialAbility;
    public CompanyId CompanyId; // default(CompanyId) == CompanyId.Player

    // Backward-compat properties
    public int ProgrammingSkill { get => Skills != null && Skills.Length > 0 ? Skills[(int)SkillType.Programming] : 0; set { EnsureSkills(); Skills[(int)SkillType.Programming] = value; } }
    public int DesignSkill { get => Skills != null && Skills.Length > 1 ? Skills[(int)SkillType.Design] : 0; set { EnsureSkills(); Skills[(int)SkillType.Design] = value; } }
    public int QASkill { get => Skills != null && Skills.Length > 2 ? Skills[(int)SkillType.QA] : 0; set { EnsureSkills(); Skills[(int)SkillType.QA] = value; } }

    private void EnsureSkills()
    {
        if (Skills == null || Skills.Length < SkillTypeHelper.SkillTypeCount)
            Skills = new int[SkillTypeHelper.SkillTypeCount];
    }
}
