using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;

public enum WorkEntryType
{
    Contract = 0,
    Product  = 1,
}

public enum WorkOutcome
{
    Completed = 0,
    Cancelled = 1,
    Ongoing   = 2,
}

[Serializable]
public struct WorkHistoryEntry
{
    public int        CompletedTick;
    public WorkEntryType EntryType;
    public string     WorkName;
    public string     TeamName;
    public RoleId     Role;
    public string     ContributionLabel;
    public int        QualityScore;
    public string     XpSummary;
    public WorkOutcome Outcome;
}

public class EmployeeIdConverter : TypeConverter {
    public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) {
        return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
    }

    public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) {
        if (value is string s && int.TryParse(s, out int id))
            return new EmployeeId(id);
        return base.ConvertFrom(context, culture, value);
    }

    public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType) {
        if (destinationType == typeof(string) && value is EmployeeId eid)
            return eid.Value.ToString();
        return base.ConvertTo(context, culture, value, destinationType);
    }
}

[TypeConverter(typeof(EmployeeIdConverter))]
[Serializable]
public struct EmployeeId
{
    public int Value;
    
    public EmployeeId(int value)
    {
        Value = value;
    }
    
    public override bool Equals(object obj)
    {
        return obj is EmployeeId id && Value == id.Value;
    }
    
    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }
    
    public static bool operator ==(EmployeeId left, EmployeeId right)
    {
        return left.Value == right.Value;
    }
    
    public static bool operator !=(EmployeeId left, EmployeeId right)
    {
        return left.Value != right.Value;
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}

[Serializable]
public class Employee
{
    public EmployeeId id;
    public string name;
    public Gender gender;
    public int age;
    public int salary;
    public int morale;
    public int hireDate;
    public bool isActive;
    public RoleId role;
    public int decayOnsetAge;          // Age at which skill decay begins [55-65]; 0 = not yet assigned
    public int contractExpiryTick;         // tick when contract expires; 0 = no expiry set (legacy save)
    public bool contractRenewalPending;    // true = waiting for player Accept/Decline
    public int renewalDemand;              // cached salary demand at renewal trigger; 0 = not computed
    public bool isFounder;                 // founders never quit, retire, or decay; grow skills 1.5x faster
    public RoleId preferredRole;           // candidate's original preferred role at hire time
    public CompanyId ownerCompanyId;
    public Personality personality;

    // Employment arrangement — populated at hire, updated on renewal
    public ContractTerms Contract;
    public CandidatePreferences OriginalPreferences;
    public RenewalState Renewal;
    public int StrikeCount;

    // Computed convenience properties backed by Contract
    public float WorkCapacity    => Contract.WorkCapacity;
    public float EffectiveOutput => Contract.EffectiveOutput;
    public EmploymentType ArrangementType => Contract.Type;

    // Stat model
    public EmployeeStatBlock Stats;

    // Founder metadata — only meaningful when isFounder == true; default to 0/-1 for non-founders
    public int FounderArchetypeId;
    public int FounderPersonalityStyleId;
    public int FounderWeaknessId;
    public int FounderTraitId;
    public int FounderSalaryChoice;
    public bool IsFounderSalaryDeferred;
    public float DeferredSalaryOwed;

    // Work history — max 20 entries, appended on contract/product completion
    public List<WorkHistoryEntry> WorkHistory;
    public const int MaxWorkHistoryEntries = 20;

    public void AppendWorkHistory(WorkHistoryEntry entry)
    {
        if (WorkHistory == null) WorkHistory = new List<WorkHistoryEntry>(MaxWorkHistoryEntries);
        if (WorkHistory.Count >= MaxWorkHistoryEntries)
            WorkHistory.RemoveAt(0);
        WorkHistory.Add(entry);
    }

    public int GetSkill(SkillId id) => Stats.GetSkill(id);
    public void SetSkill(SkillId id, int value) => Stats.SetSkill(id, value);

    private Employee() { }

    public Employee(EmployeeId id, string name, Gender gender, int age, int salary, int hireDate, RoleId role)
    {
        this.id = id;
        this.name = name;
        this.gender = gender;
        this.age = age;
        this.Stats = EmployeeStatBlock.Create();
        this.salary = salary;
        this.morale = 100;
        this.hireDate = hireDate;
        this.isActive = true;
        this.role = role;
        this.personality = Personality.Professional;
    }

    public Employee(EmployeeId id, string name, Gender gender, int age, EmployeeStatBlock stats, int salary, int hireDate, RoleId role)
    {
        this.id = id;
        this.name = name;
        this.gender = gender;
        this.age = age;
        this.Stats = stats;
        this.salary = salary;
        this.morale = 100;
        this.hireDate = hireDate;
        this.isActive = true;
        this.role = role;
        this.personality = Personality.Professional;
    }
}

[Serializable]
public class EmployeeState
{
    public Dictionary<EmployeeId, Employee> employees = new Dictionary<EmployeeId, Employee>();
    public int nextEmployeeId;
    public int nextCandidateId;
    public List<CandidateData> availableCandidates = new List<CandidateData>();
    public int lastCandidateGenerationTick;
    public int candidateGenerationInterval = 15 * 4800; // DEPRECATED: monthly refresh uses calendar check
    public int lastYearProcessed;
    public int candidatePoolSeed;
    public int candidateRerollsUsedThisCycle;
    
    public static EmployeeState CreateNew()
    {
        return new EmployeeState
        {
            nextEmployeeId = 1,
            nextCandidateId = 1,
            lastCandidateGenerationTick = -1,
            candidatePoolSeed = System.Environment.TickCount,
            candidateRerollsUsedThisCycle = 0
        };
    }
}
