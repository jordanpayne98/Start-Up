/// <summary>
/// Data struct passed to PaginationView.Bind(). Contains all display-ready pagination state.
/// </summary>
public struct PaginationData
{
    /// <summary>Total number of items across all pages.</summary>
    public int TotalItems;

    /// <summary>Currently active page (1-based).</summary>
    public int CurrentPage;

    /// <summary>Number of rows displayed per page.</summary>
    public int RowsPerPage;

    /// <summary>Total number of pages.</summary>
    public int TotalPages;

    // ── Computed helpers ─────────────────────────────────────────────────────

    /// <summary>Index of the first item on the current page (1-based display).</summary>
    public int FirstItem => TotalItems == 0 ? 0 : (CurrentPage - 1) * RowsPerPage + 1;

    /// <summary>Index of the last item on the current page (1-based display).</summary>
    public int LastItem
    {
        get
        {
            int last = CurrentPage * RowsPerPage;
            return last > TotalItems ? TotalItems : last;
        }
    }
}
