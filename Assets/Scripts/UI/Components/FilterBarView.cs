using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

/// <summary>
/// Reusable view for the filter / toolbar row. Drives the FilterBar.uxml template.
/// Handles count label, search input, filter chip rendering, and export button.
/// Initialize once; Bind on every refresh.
/// </summary>
public class FilterBarView
{
    private Label         _countBadge;
    private TextField     _searchInput;
    private VisualElement _filterSlots;
    private Button        _exportButton;

    private readonly List<FilterDropdownData> _filterListCache = new List<FilterDropdownData>(4);
    private ElementPool _filterPool;

    // ── Events ───────────────────────────────────────────────────────────────

    /// <summary>Fires when a filter chip changes, with the filter key as argument.</summary>
    public event Action<string> OnFilterChanged;

    /// <summary>Fires when the search text changes.</summary>
    public event Action<string> OnSearchChanged;

    // ── Initialize ───────────────────────────────────────────────────────────

    /// <summary>
    /// Query and cache elements from host. Wire search input and export handlers.
    /// Call once per view lifetime.
    /// </summary>
    public void Initialize(VisualElement host)
    {
        if (host == null) return;

        _countBadge   = host.Q<Label>("count-badge");
        _searchInput  = host.Q<TextField>("search-input");
        _filterSlots  = host.Q<VisualElement>("filter-slots");
        _exportButton = host.Q<Button>("btn-export");

        if (_searchInput != null)
            _searchInput.RegisterCallback<ChangeEvent<string>>(OnSearchValueChanged);

        if (_exportButton != null)
            _exportButton.RegisterCallback<ClickEvent>(OnExportClicked);

        if (_filterSlots != null)
            _filterPool = new ElementPool(CreateFilterChip, _filterSlots);
    }

    // ── Bind ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Update count label, search text, filter chips, and export button visibility.
    /// No queries or handler wiring in Bind.
    /// </summary>
    public void Bind(FilterBarData data)
    {
        if (_countBadge != null)
            _countBadge.text = data.CountLabel ?? string.Empty;

        if (_exportButton != null)
            _exportButton.style.display = data.ShowExport ? DisplayStyle.Flex : DisplayStyle.None;

        // Populate filter chips
        if (_filterPool != null)
        {
            _filterListCache.Clear();
            if (data.Filters != null)
            {
                for (int i = 0; i < data.Filters.Count; i++)
                    _filterListCache.Add(data.Filters[i]);
            }

            _filterPool.UpdateList(_filterListCache, (el, filterData) =>
            {
                var lbl = el.Q<Label>("filter-chip-label");
                if (lbl != null) lbl.text = filterData.Label ?? string.Empty;

                el.userData = filterData.Key;
                el.EnableInClassList("filter-chip--active", filterData.IsActive);
            });
        }
    }

    // ── Dispose ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Unregister handlers and null cached references.
    /// </summary>
    public void Dispose()
    {
        if (_searchInput != null)
            _searchInput.UnregisterCallback<ChangeEvent<string>>(OnSearchValueChanged);

        if (_exportButton != null)
            _exportButton.UnregisterCallback<ClickEvent>(OnExportClicked);

        _countBadge   = null;
        _searchInput  = null;
        _filterSlots  = null;
        _exportButton = null;
        _filterPool   = null;

        OnFilterChanged  = null;
        OnSearchChanged  = null;
    }

    // ── Pool factory ─────────────────────────────────────────────────────────

    private VisualElement CreateFilterChip()
    {
        var el = new VisualElement();
        el.AddToClassList("filter-chip");

        var lbl = new Label();
        lbl.name = "filter-chip-label";
        lbl.AddToClassList("filter-chip__label");
        el.Add(lbl);

        // Wire once — handler reads userData (filter key) to fire OnFilterChanged
        el.RegisterCallback<ClickEvent>(OnFilterChipClicked);

        return el;
    }

    // ── Handlers ─────────────────────────────────────────────────────────────

    private void OnSearchValueChanged(ChangeEvent<string> evt)
    {
        OnSearchChanged?.Invoke(evt.newValue);
    }

    private void OnFilterChipClicked(ClickEvent evt)
    {
        if (evt.currentTarget is VisualElement el && el.userData is string key)
            OnFilterChanged?.Invoke(key);
    }

    private void OnExportClicked(ClickEvent evt)
    {
        // Export action — no-op by default; screens can subscribe to OnFilterChanged
        // with key "export" or wire directly if needed in the future.
    }
}
