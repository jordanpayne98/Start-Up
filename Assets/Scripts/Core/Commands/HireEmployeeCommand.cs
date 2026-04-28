using System.Collections.Generic;

public struct HireEmployeeCommand : ICommand
{
    public int Tick { get; set; }
    public int CandidateId;
    public string Name;
    public Gender Gender;
    public int Age;
    public EmployeeStatBlock Stats;
    public int HRSkill;
    public int Salary;
    public RoleId Role;
    public RoleId PreferredRole;
    public bool BlindHire;
    public HiringMode Mode;
    public int PotentialAbility;
    public CompanyId CompanyId; // default(CompanyId) == CompanyId.Player
    public Personality Personality;
    public EmploymentType EmploymentType;
    public ContractLengthOption ContractLength;

    // Backward-compat: delegate to Stats
    public int[] Skills => Stats.Skills;
}
