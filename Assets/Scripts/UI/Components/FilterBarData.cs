using System.Collections.Generic;

/// <summary>
/// Data struct passed to FilterBarView.Bind(). Contains display-ready filter bar state.
/// </summary>
public struct FilterBarData
{
    /// <summary>Item count label shown in the bar, e.g. "24 Contracts".</summary>
    public string CountLabel;

    /// <summary>Active filter chips shown in the filter row.</summary>
    public IReadOnlyList<FilterDropdownData> Filters;

    /// <summary>Current value in the search input.</summary>
    public string SearchText;

    /// <summary>Whether the export action button is visible.</summary>
    public bool ShowExport;
}
