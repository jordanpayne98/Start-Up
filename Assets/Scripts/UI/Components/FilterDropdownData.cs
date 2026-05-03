/// <summary>
/// Data struct representing a single filter dropdown chip in the filter bar.
/// </summary>
public struct FilterDropdownData
{
    /// <summary>Identifier key for this filter, e.g. "role" or "status".</summary>
    public string Key;

    /// <summary>Display label shown on the chip, e.g. "Role: Engineer".</summary>
    public string Label;

    /// <summary>Whether this filter is currently active.</summary>
    public bool IsActive;
}
