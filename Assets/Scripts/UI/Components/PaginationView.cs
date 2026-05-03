using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

/// <summary>
/// Reusable view for the pagination row. Drives the Pagination.uxml template.
/// Shows "Showing X to Y of Z" label, page number buttons (max 7 visible with ellipsis),
/// and a rows-per-page dropdown.
/// Initialize once; Bind on every refresh.
/// </summary>
public class PaginationView
{
    private Label         _showingLabel;
    private VisualElement _pageButtonsContainer;
    private DropdownField _rowsPerPageDropdown;

    private readonly List<Button> _pageButtons = new List<Button>(7);
    private ElementPool           _pageButtonPool;

    private static readonly List<string> RowsPerPageChoices = new List<string> { "10", "25", "50", "100" };

    // ── Events ───────────────────────────────────────────────────────────────

    /// <summary>Fires when a page number button is clicked.</summary>
    public event Action<int> OnPageChanged;

    /// <summary>Fires when the rows-per-page dropdown value changes, with the new row count.</summary>
    public event Action<int> OnRowsPerPageChanged;

    // ── Initialize ───────────────────────────────────────────────────────────

    /// <summary>
    /// Query and cache elements from host. Wire rows-per-page dropdown.
    /// Call once per view lifetime.
    /// </summary>
    public void Initialize(VisualElement host)
    {
        if (host == null) return;

        _showingLabel        = host.Q<Label>("showing-label");
        _pageButtonsContainer = host.Q<VisualElement>("page-buttons-container");
        _rowsPerPageDropdown = host.Q<DropdownField>("rows-per-page-dropdown");

        if (_rowsPerPageDropdown != null)
        {
            _rowsPerPageDropdown.choices = RowsPerPageChoices;
            _rowsPerPageDropdown.RegisterValueChangedCallback(OnRowsPerPageValueChanged);
        }

        if (_pageButtonsContainer != null)
            _pageButtonPool = new ElementPool(CreatePageButton, _pageButtonsContainer);
    }

    // ── Bind ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Update showing label, page buttons, and rows-per-page dropdown.
    /// No queries or handler wiring in Bind.
    /// </summary>
    public void Bind(PaginationData data)
    {
        // Update "Showing X to Y of Z" label
        if (_showingLabel != null)
        {
            if (data.TotalItems == 0)
                _showingLabel.text = "No items";
            else
                _showingLabel.text = $"Showing {data.FirstItem}–{data.LastItem} of {data.TotalItems}";
        }

        // Update rows-per-page dropdown (suppress event during bind)
        if (_rowsPerPageDropdown != null)
        {
            string rppStr = data.RowsPerPage.ToString();
            if (_rowsPerPageDropdown.value != rppStr)
                _rowsPerPageDropdown.SetValueWithoutNotify(rppStr);
        }

        // Build page number list with ellipsis logic
        var pages = BuildPageList(data.CurrentPage, data.TotalPages);

        if (_pageButtonPool != null)
        {
            _pageButtonPool.UpdateList(pages, (el, pageData) =>
            {
                var btn = el as Button;
                if (btn == null) return;

                bool isEllipsis = pageData == -1;
                bool isCurrent  = pageData == data.CurrentPage;

                btn.text    = isEllipsis ? "…" : pageData.ToString();
                btn.userData = pageData;
                btn.SetEnabled(!isEllipsis && !isCurrent);
                btn.EnableInClassList("page-btn--active",   isCurrent);
                btn.EnableInClassList("page-btn--ellipsis", isEllipsis);
            });
        }
    }

    // ── Dispose ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Unregister handlers and null cached references.
    /// </summary>
    public void Dispose()
    {
        if (_rowsPerPageDropdown != null)
            _rowsPerPageDropdown.UnregisterValueChangedCallback(OnRowsPerPageValueChanged);

        _showingLabel         = null;
        _pageButtonsContainer = null;
        _rowsPerPageDropdown  = null;
        _pageButtonPool       = null;

        OnPageChanged        = null;
        OnRowsPerPageChanged = null;
    }

    // ── Page list builder ────────────────────────────────────────────────────

    /// <summary>
    /// Builds the page number list with ellipsis (-1). Shows at most 7 buttons:
    /// first page, last page, current ±2, and ellipsis for gaps.
    /// </summary>
    private static List<int> BuildPageList(int current, int total)
    {
        var result = new List<int>(7);

        if (total <= 0) return result;

        if (total <= 7)
        {
            for (int i = 1; i <= total; i++)
                result.Add(i);
            return result;
        }

        // Always include page 1
        result.Add(1);

        int rangeStart = current - 2;
        int rangeEnd   = current + 2;

        // Clamp to valid range (exclude 1 and total which are always shown)
        if (rangeStart < 2) rangeStart = 2;
        if (rangeEnd > total - 1) rangeEnd = total - 1;

        if (rangeStart > 2)
            result.Add(-1); // ellipsis

        for (int i = rangeStart; i <= rangeEnd; i++)
            result.Add(i);

        if (rangeEnd < total - 1)
            result.Add(-1); // ellipsis

        result.Add(total);

        return result;
    }

    // ── Pool factory ─────────────────────────────────────────────────────────

    private VisualElement CreatePageButton()
    {
        var btn = new Button();
        btn.AddToClassList("page-btn");
        btn.RegisterCallback<ClickEvent>(OnPageButtonClicked);
        return btn;
    }

    // ── Handlers ─────────────────────────────────────────────────────────────

    private void OnPageButtonClicked(ClickEvent evt)
    {
        if (evt.currentTarget is VisualElement el && el.userData is int page && page > 0)
            OnPageChanged?.Invoke(page);
    }

    private void OnRowsPerPageValueChanged(ChangeEvent<string> evt)
    {
        if (int.TryParse(evt.newValue, out int rows))
            OnRowsPerPageChanged?.Invoke(rows);
    }
}
