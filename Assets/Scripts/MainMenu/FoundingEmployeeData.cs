using System;

[Serializable]
public struct FoundingEmployeeData {
    public string Name;
    public int Age;
    public Gender Gender;
    public RoleId Role;
    public int Tier; // 1-5: Intern, Junior, Mid-Level, Senior, Expert

    public int ArchetypeId;
    public int PersonalityStyleId;
    public int WeaknessId;
    public int SalaryAmount;
    public bool IsFounder;
}
