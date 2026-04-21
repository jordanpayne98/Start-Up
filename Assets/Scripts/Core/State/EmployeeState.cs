using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;

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
    public int[] skills;
    public int salary;
    public int morale;
    public int hireDate;
    public bool isActive;
    public EmployeeRole role;
    public int hrSkill;
    public int potentialAbility;       // 0-200; 0 = not yet generated (pre-migration save)
    public HiddenAttributes hiddenAttributes;
    public int decayOnsetAge;          // Age at which skill decay begins [55-65]; 0 = not yet assigned
    public float[] skillXp;            // sub-level growth accumulator [0,1) per skill
    public sbyte[] skillDeltaDirection; // 1=growth, -1=decay, 0=never changed
    public int contractExpiryTick;         // tick when contract expires; 0 = no expiry set (legacy save)
    public bool contractRenewalPending;    // true = waiting for player Accept/Decline
    public int renewalDemand;              // cached salary demand at renewal trigger; 0 = not computed
    public bool isFounder;                 // founders never quit, retire, or decay; grow skills 1.5x faster
    public CompanyId ownerCompanyId;

    // Backward-compat properties for save migration and existing code
    public int programmingSkill { get => skills[(int)SkillType.Programming]; set => skills[(int)SkillType.Programming] = value; }
    public int designSkill { get => skills[(int)SkillType.Design]; set => skills[(int)SkillType.Design] = value; }
    public int qaSkill { get => skills[(int)SkillType.QA]; set => skills[(int)SkillType.QA] = value; }

    public int GetSkill(SkillType type) => skills[(int)type];
    public void SetSkill(SkillType type, int value) => skills[(int)type] = value;

    private Employee() { }

    public Employee(EmployeeId id, string name, Gender gender, int age, int programmingSkill, int designSkill, int qaSkill, int salary, int hireDate, EmployeeRole role, int hrSkill = 0)
    {
        this.id = id;
        this.name = name;
        this.gender = gender;
        this.age = age;
        this.skills = new int[SkillTypeHelper.SkillTypeCount];
        this.skills[(int)SkillType.Programming] = programmingSkill;
        this.skills[(int)SkillType.Design] = designSkill;
        this.skills[(int)SkillType.QA] = qaSkill;
        this.skillXp = new float[SkillTypeHelper.SkillTypeCount];
        this.skillDeltaDirection = new sbyte[SkillTypeHelper.SkillTypeCount];
        this.salary = salary;
        this.morale = 100;
        this.hireDate = hireDate;
        this.isActive = true;
        this.role = role;
        this.hrSkill = hrSkill;
    }

    public Employee(EmployeeId id, string name, Gender gender, int age, int[] skills, int salary, int hireDate, EmployeeRole role, int hrSkill = 0)
    {
        this.id = id;
        this.name = name;
        this.gender = gender;
        this.age = age;
        this.skills = new int[SkillTypeHelper.SkillTypeCount];
        if (skills != null)
        {
            int len = skills.Length < SkillTypeHelper.SkillTypeCount ? skills.Length : SkillTypeHelper.SkillTypeCount;
            for (int i = 0; i < len; i++) this.skills[i] = skills[i];
        }
        this.skillXp = new float[SkillTypeHelper.SkillTypeCount];
        this.skillDeltaDirection = new sbyte[SkillTypeHelper.SkillTypeCount];
        this.salary = salary;
        this.morale = 100;
        this.hireDate = hireDate;
        this.isActive = true;
        this.role = role;
        this.hrSkill = hrSkill;
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
    public int candidateGenerationInterval = 15 * 4800; // 15 days × TicksPerDay
    public int lastYearProcessed;
    public int candidatePoolSeed;
    public int candidateRerollsUsedThisCycle;
    
    public static EmployeeState CreateNew()
    {
        return new EmployeeState
        {
            nextEmployeeId = 1,
            nextCandidateId = 1,
            lastCandidateGenerationTick = -(15 * 4800), // 15 days so pool generates immediately on day 1
            candidatePoolSeed = System.Environment.TickCount,
            candidateRerollsUsedThisCycle = 0
        };
    }
}
