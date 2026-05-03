/// <summary>
/// Data struct representing a single tab item in the tab bar.
/// </summary>
public struct TabItemData
{
    /// <summary>Display label, e.g. "All Contracts".</summary>
    public string Label;

    /// <summary>
    /// Optional count badge value. -1 means no badge is shown.
    /// </summary>
    public int Count;

    /// <summary>Whether this tab is currently active.</summary>
    public bool IsActive;
}
