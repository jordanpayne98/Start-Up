using System;

[Serializable]
public struct SaveMetadata
{
    public string SlotName;
    public string DisplayName;
    public string CompanyName;
    public int InGameDay;
    public int InGameMonth;
    public int InGameYear;
    public long Money;
    public int EmployeeCount;
    public int CurrentTick;
    public string RealWorldTimestamp;
    public int SaveVersion;
    public bool IsAutoSave;
}
