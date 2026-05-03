using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

/// <summary>
/// Reusable view for the tab bar pattern. Drives the tab-bar container element.
/// Uses ElementPool for tab items — factory wires handler once; Bind only updates
/// text, classes, and userData.
/// </summary>
public class TabBarView
{
    private VisualElement _container;
    private ElementPool   _tabPool;

    // ── Events ───────────────────────────────────────────────────────────────

    /// <summary>Fires when a tab button is clicked, with the tab index as argument.</summary>
    public event Action<int> OnTabSelected;

    // ── Initialize ───────────────────────────────────────────────────────────

    /// <summary>
    /// Query and cache the tab-bar container from host. Build the element pool.
    /// Call once per view lifetime.
    /// </summary>
    public void Initialize(VisualElement host)
    {
        if (host == null) return;

        _container = host.Q<VisualElement>("tab-bar");

        // Fall back to host itself if no "tab-bar" child is found
        if (_container == null)
            _container = host;

        _tabPool = new ElementPool(CreateTabElement, _container);
    }

    // ── Bind ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Populate or refresh tab items from data. Single named handler shared across pool slots.
    /// No queries; no handler wiring in Bind.
    /// </summary>
    public void Bind(IReadOnlyList<TabItemData> tabs, int activeIndex)
    {
        if (_tabPool == null || tabs == null) return;

        // Convert to List for ElementPool.UpdateList
        var list = new List<TabItemData>(tabs.Count);
        for (int i = 0; i < tabs.Count; i++)
            list.Add(tabs[i]);

        _tabPool.UpdateList(list, (el, data) =>
        {
            // Store index in userData for routing
            int idx = list.IndexOf(data);
            el.userData = idx;

            var lbl = el.Q<Label>("tab-label");
            var badge = el.Q<Label>("tab-count");

            if (lbl != null) lbl.text = data.Label ?? string.Empty;

            if (badge != null)
            {
                bool hasBadge = data.Count >= 0;
                badge.style.display = hasBadge ? DisplayStyle.Flex : DisplayStyle.None;
                if (hasBadge) badge.text = data.Count.ToString();
            }

            el.EnableInClassList("tab-item--active", idx == activeIndex);
        });
    }

    // ── Dispose ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Dispose does not need to unregister tab handlers explicitly — pool elements
    /// are wired in the factory with named method references. Just null references.
    /// </summary>
    public void Dispose()
    {
        _container   = null;
        _tabPool     = null;
        OnTabSelected = null;
    }

    // ── Pool factory ─────────────────────────────────────────────────────────

    private VisualElement CreateTabElement()
    {
        var el = new VisualElement();
        el.AddToClassList("tab-item");

        var lbl = new Label();
        lbl.name = "tab-label";
        lbl.AddToClassList("tab-item__label");
        el.Add(lbl);

        var badge = new Label();
        badge.name = "tab-count";
        badge.AddToClassList("tab-item__count");
        badge.style.display = DisplayStyle.None;
        el.Add(badge);

        // Wire handler once — shared across all pool slots
        el.RegisterCallback<ClickEvent>(OnTabItemClicked);

        return el;
    }

    // ── Handler ──────────────────────────────────────────────────────────────

    private void OnTabItemClicked(ClickEvent evt)
    {
        if (evt.currentTarget is VisualElement el && el.userData is int index)
            OnTabSelected?.Invoke(index);
    }
}
