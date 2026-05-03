// HiddenAttributeShiftHelper — stateless utility for event-driven hidden attribute changes.
// Called explicitly by GameController on qualifying game events.
// All shifts are rare (low base chance), clamped [1, 20].
// RNG stream key: "hiddenShift"

public static class HiddenAttributeShiftHelper
{
    private const float DefaultBaseChance = 0.04f;

    // -------------------------------------------------------------------------
    // Core shift method
    // -------------------------------------------------------------------------
    /// <summary>
    /// Attempts a hidden attribute shift. Returns a result if the shift occurred, null otherwise.
    /// </summary>
    /// <param name="employee">The employee whose attribute may shift.</param>
    /// <param name="attribute">The hidden attribute to potentially shift.</param>
    /// <param name="delta">+1 or -1 typically.</param>
    /// <param name="rng">Deterministic RNG — must use stream key "hiddenShift".</param>
    /// <param name="baseChance">Probability of shift occurring (e.g. 0.05 = 5%).</param>
    public static HiddenAttributeShiftResult? TryShift(
        Employee employee,
        HiddenAttributeId attribute,
        int delta,
        IRng rng,
        float baseChance)
    {
        if (employee == null || employee.Stats.HiddenAttributes == null) return null;
        if (baseChance <= 0f) return null;

        float roll = rng.NextFloat01();
        if (roll >= baseChance) return null;

        int idx = (int)attribute;
        if (idx < 0 || idx >= employee.Stats.HiddenAttributes.Length) return null;

        int oldValue = employee.Stats.HiddenAttributes[idx];
        int newValue = oldValue + delta;
        if (newValue < 1) newValue = 1;
        if (newValue > 20) newValue = 20;
        if (newValue == oldValue) return null;

        employee.Stats.HiddenAttributes[idx] = newValue;

        return new HiddenAttributeShiftResult
        {
            EmployeeId = employee.id,
            Attribute  = attribute,
            OldValue   = oldValue,
            NewValue   = newValue,
            ReportText = BuildReportText(employee.name, attribute, delta > 0)
        };
    }

    // -------------------------------------------------------------------------
    // Predefined event handlers (spec section 6.2)
    // -------------------------------------------------------------------------

    /// <summary>Contract success — Ambition may increase; Loyalty may increase.</summary>
    public static HiddenAttributeShiftResult? OnContractSuccess(Employee employee, IRng rng)
    {
        // Ambition rises on major success (4% chance)
        var ambitionShift = TryShift(employee, HiddenAttributeId.Ambition, +1, rng, 0.04f);
        if (ambitionShift.HasValue) return ambitionShift;

        // Loyalty rises with positive outcomes (3% chance)
        return TryShift(employee, HiddenAttributeId.Loyalty, +1, rng, 0.03f);
    }

    /// <summary>Contract failure — Ambition may decrease; Consistency may decrease.</summary>
    public static HiddenAttributeShiftResult? OnContractFailure(Employee employee, IRng rng)
    {
        // Ambition drops on repeated failures (5% chance)
        var ambitionShift = TryShift(employee, HiddenAttributeId.Ambition, -1, rng, 0.05f);
        if (ambitionShift.HasValue) return ambitionShift;

        // Consistency drops under repeated stress (3% chance)
        return TryShift(employee, HiddenAttributeId.Consistency, -1, rng, 0.03f);
    }

    /// <summary>Burnout event — PressureTolerance may decrease; Consistency may decrease.</summary>
    public static HiddenAttributeShiftResult? OnBurnout(Employee employee, IRng rng)
    {
        var ptShift = TryShift(employee, HiddenAttributeId.PressureTolerance, -1, rng, 0.06f);
        if (ptShift.HasValue) return ptShift;

        return TryShift(employee, HiddenAttributeId.Consistency, -1, rng, 0.04f);
    }

    /// <summary>Long tenure / good treatment — Loyalty may increase; Consistency may increase.</summary>
    /// <param name="monthsEmployed">Used to scale chance; starts meaningful at 12+ months.</param>
    public static HiddenAttributeShiftResult? OnLongTenure(Employee employee, int monthsEmployed, IRng rng)
    {
        if (monthsEmployed < 12) return null;
        float chance = monthsEmployed >= 36 ? 0.06f : 0.03f;

        var loyaltyShift = TryShift(employee, HiddenAttributeId.Loyalty, +1, rng, chance);
        if (loyaltyShift.HasValue) return loyaltyShift;

        return TryShift(employee, HiddenAttributeId.Consistency, +1, rng, chance * 0.5f);
    }

    /// <summary>Successful junior development — Mentoring may increase; Ego may decrease.</summary>
    public static HiddenAttributeShiftResult? OnMentorSuccess(Employee employee, IRng rng)
    {
        var mentoringShift = TryShift(employee, HiddenAttributeId.Mentoring, +1, rng, 0.05f);
        if (mentoringShift.HasValue) return mentoringShift;

        // Being a mentor reduces Ego slightly
        return TryShift(employee, HiddenAttributeId.Ego, -1, rng, 0.03f);
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------
    private static string BuildReportText(string employeeName, HiddenAttributeId attribute, bool increased)
    {
        string attrName = HiddenAttributeHelper.GetName(attribute);
        string direction = increased ? "appears to have improved" : "seems to have shifted";
        return $"{employeeName}'s {attrName} {direction}.";
    }
}

public struct HiddenAttributeShiftResult
{
    public EmployeeId EmployeeId;
    public HiddenAttributeId Attribute;
    public int OldValue;
    public int NewValue;
    /// <summary>Human-readable signal text for inbox/tooltip use.</summary>
    public string ReportText;
}
