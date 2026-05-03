using System;

[Serializable]
public struct FoundingEmployeeData {
    public string Name;
    public int Age;
    public Gender Gender;
    public RoleId Role;

    // Archetype/personality/weakness from wizard selections
    public int ArchetypeId;
    public int PersonalityStyleId;
    public int WeaknessId;

    // Salary option index: 0=None, 1=Low, 2=Market, 3=Deferred
    public int SalaryChoice;

    // Resolved monthly salary amount (pre-computed from SalaryChoice)
    public int SalaryAmount;

    // Optional founder trait (-1 = none)
    public int TraitId;

    // Human-readable archetype name for save/migration readability
    public string ArchetypeName;

    public bool IsFounder;
}
