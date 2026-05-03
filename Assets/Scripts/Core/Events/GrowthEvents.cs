// GrowthEvents — domain events for skill and visible attribute growth.
// Emitted by GameController after processing SkillGrowthResult / AttributeIncreaseRecord lists.
// Consumed by UI dirty flags and inbox milestone checks.
// Hidden attribute shifts use EmployeePersonalitySignalChangedEvent.

public class EmployeeSkillIncreasedEvent : GameEvent
{
    public EmployeeId EmployeeId;
    public SkillId Skill;
    public int OldValue;
    public int NewValue;
    /// <summary>"Contract", "Product", "Marketing", etc.</summary>
    public string Source;

    public EmployeeSkillIncreasedEvent(int tick, EmployeeId employeeId, SkillId skill, int oldValue, int newValue, string source)
        : base(tick)
    {
        EmployeeId = employeeId;
        Skill      = skill;
        OldValue   = oldValue;
        NewValue   = newValue;
        Source     = source;
    }
}

public class EmployeeAttributeIncreasedEvent : GameEvent
{
    public EmployeeId EmployeeId;
    public VisibleAttributeId Attribute;
    public int OldValue;
    public int NewValue;
    /// <summary>"Contract", "Product", "Marketing", etc.</summary>
    public string Source;

    public EmployeeAttributeIncreasedEvent(int tick, EmployeeId employeeId, VisibleAttributeId attribute, int oldValue, int newValue, string source)
        : base(tick)
    {
        EmployeeId = employeeId;
        Attribute  = attribute;
        OldValue   = oldValue;
        NewValue   = newValue;
        Source     = source;
    }
}

/// <summary>
/// Raised when a hidden attribute shift occurs. Consumers should surface a soft narrative signal
/// (e.g. inbox message or tooltip), NOT expose the raw attribute value.
/// </summary>
public class EmployeePersonalitySignalChangedEvent : GameEvent
{
    public EmployeeId EmployeeId;
    /// <summary>Human-readable signal text, e.g. "Loyalty appears to have improved".</summary>
    public string ReportText;

    public EmployeePersonalitySignalChangedEvent(int tick, EmployeeId employeeId, string reportText)
        : base(tick)
    {
        EmployeeId = employeeId;
        ReportText = reportText;
    }
}
