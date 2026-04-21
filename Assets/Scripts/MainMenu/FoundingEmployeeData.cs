using System;

[Serializable]
public struct FoundingEmployeeData {
    public string Name;
    public int Age;
    public Gender Gender;
    public EmployeeRole Role;
    public int Tier; // 1-5: Intern, Junior, Mid-Level, Senior, Expert
}
