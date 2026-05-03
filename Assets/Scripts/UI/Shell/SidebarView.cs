using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// View for the sidebar-navigation shell region.
/// Renders collapsible category headers with nav item children per the SidebarViewModel.
/// Follows UI_Architecture_v3: Initialize wires all handlers; Bind only updates state.
/// </summary>
public class SidebarView : IGameView
{
    // ── Cached elements ───────────────────────────────────────────────────

    private VisualElement _sidebarRoot;
    private VisualElement _navTree;
    private Button        _collapseToggle;

    // ── Services ──────────────────────────────────────────────────────────

    private INavigationService _navigation;

    // ── ViewModel reference ───────────────────────────────────────────────

    private SidebarViewModel _vm;

    // ── Pooled nav item records ───────────────────────────────────────────

    // Per-category: header element + children container + collapse state
    private readonly struct CategoryRow
    {
        public readonly NavCategoryId  CategoryId;
        public readonly VisualElement  Header;
        public readonly Label          ChevronLabel;
        public readonly VisualElement  ChildrenContainer;
        public CategoryRow(NavCategoryId id, VisualElement header, Label chevron, VisualElement children)
        {
            CategoryId        = id;
            Header            = header;
            ChevronLabel      = chevron;
            ChildrenContainer = children;
        }
    }

    private readonly struct NavItemRow
    {
        public readonly ScreenId      ScreenId;
        public readonly VisualElement Element;
        public readonly Label         BadgeLabel;
        public NavItemRow(ScreenId id, VisualElement el, Label badge)
        {
            ScreenId   = id;
            Element    = el;
            BadgeLabel = badge;
        }
    }

    private readonly List<CategoryRow> _categoryRows = new List<CategoryRow>();
    private readonly List<NavItemRow>  _navItemRows  = new List<NavItemRow>();

    // ── Navigation subscription ───────────────────────────────────────────

    private Action<ScreenId, ScreenId> _onScreenChanged;

    // ── IGameView.Initialize ──────────────────────────────────────────────

    public void Initialize(VisualElement root, UIServices services)
    {
        _navigation  = services?.Navigation;
        _sidebarRoot = root.Q<VisualElement>("sidebar-navigation") ?? root;

        _navTree = _sidebarRoot.Q<VisualElement>("sidebar-nav-tree");
        if (_navTree == null)
        {
            // Create fallback container if UXML scaffold isn't present yet
            _navTree = new VisualElement();
            _navTree.name = "sidebar-nav-tree";
            _navTree.AddToClassList("sidebar-nav-tree");
            _sidebarRoot.Add(_navTree);
        }

        _collapseToggle = _sidebarRoot.Q<Button>("sidebar-collapse-toggle");
        if (_collapseToggle != null)
        {
            _collapseToggle.clicked += OnCollapseToggle;
        }

        // Subscribe to navigation screen changes for active-state update
        if (_navigation != null)
        {
            _onScreenChanged = OnNavigationScreenChanged;
            _navigation.OnScreenChanged += _onScreenChanged;
        }
    }

    // ── IGameView.Bind ────────────────────────────────────────────────────

    public void Bind(IViewModel viewModel)
    {
        if (viewModel is not SidebarViewModel vm) return;
        _vm = vm;

        // If nav tree is empty (first bind), build the pooled rows
        if (_categoryRows.Count == 0)
        {
            BuildNavTree(vm.Categories);
        }

        // Update active class on every nav item
        int itemCount = _navItemRows.Count;
        for (int i = 0; i < itemCount; i++)
        {
            var row = _navItemRows[i];
            bool isActive = row.ScreenId == vm.ActiveScreenId;
            row.Element.EnableInClassList("nav-item--active", isActive);
        }

        // Update badge counts
        int catCount = vm.Categories.Count;
        for (int c = 0; c < catCount; c++)
        {
            var cat = vm.Categories[c];
            int nodeCount = cat.Children.Count;
            for (int n = 0; n < nodeCount; n++)
            {
                var node = cat.Children[n];
                UpdateBadge(node.ScreenId, node.BadgeCount);
            }
        }

        // Collapsed state
        _sidebarRoot.EnableInClassList("sidebar--collapsed", vm.IsCollapsed);
        if (_collapseToggle != null)
        {
            _collapseToggle.text = vm.IsCollapsed ? "»" : "«";
        }
    }

    // ── IGameView.Dispose ─────────────────────────────────────────────────

    public void Dispose()
    {
        if (_collapseToggle != null)
        {
            _collapseToggle.clicked -= OnCollapseToggle;
        }

        if (_navigation != null && _onScreenChanged != null)
        {
            _navigation.OnScreenChanged -= _onScreenChanged;
            _onScreenChanged = null;
        }

        // Unregister category header callbacks
        int catCount = _categoryRows.Count;
        for (int i = 0; i < catCount; i++)
        {
            var row = _categoryRows[i];
            row.Header.UnregisterCallback<PointerDownEvent>(OnCategoryHeaderPointerDown);
        }

        // Unregister nav item callbacks
        int itemCount = _navItemRows.Count;
        for (int i = 0; i < itemCount; i++)
        {
            _navItemRows[i].Element.UnregisterCallback<PointerDownEvent>(OnNavItemPointerDown);
        }

        _categoryRows.Clear();
        _navItemRows.Clear();
        _vm         = null;
        _navigation = null;
    }

    // ── Tree builder (called once; rows are reused across Bind calls) ─────

    private void BuildNavTree(IReadOnlyList<NavCategoryData> categories)
    {
        _navTree.Clear();
        _categoryRows.Clear();
        _navItemRows.Clear();

        int catCount = categories.Count;
        Debug.Log($"[SidebarView] BuildNavTree: {catCount} categories");

        for (int c = 0; c < catCount; c++)
        {
            var cat = categories[c];

            // ── Category header ──────────────────────────────────────────

            var header = new VisualElement();
            header.AddToClassList("nav-category__header");
            header.userData = cat.Id;    // NavCategoryId stored for handler
            header.RegisterCallback<PointerDownEvent>(OnCategoryHeaderPointerDown);

            var headerLabel = new Label(cat.Label);
            headerLabel.AddToClassList("nav-category__label");
            header.Add(headerLabel);

            var chevron = new Label(cat.IsCollapsed ? "›" : "∨");
            chevron.AddToClassList("nav-category__chevron");
            if (cat.IsCollapsed) chevron.AddToClassList("nav-category__chevron--collapsed");
            header.Add(chevron);

            _navTree.Add(header);

            // ── Children container ───────────────────────────────────────

            var childrenContainer = new VisualElement();
            childrenContainer.AddToClassList("nav-category__children");
            if (cat.IsCollapsed)
                childrenContainer.style.display = DisplayStyle.None;

            int nodeCount = cat.Children.Count;
            for (int n = 0; n < nodeCount; n++)
            {
                var node = cat.Children[n];

                var item = new VisualElement();
                item.AddToClassList("nav-item");
                item.userData = node.ScreenId;
                item.RegisterCallback<PointerDownEvent>(OnNavItemPointerDown);

                if (!string.IsNullOrEmpty(node.IconClass))
                {
                    var icon = new VisualElement();
                    icon.AddToClassList("nav-item__icon");
                    icon.AddToClassList(node.IconClass);
                    item.Add(icon);
                }

                var itemLabel = new Label(node.Label);
                itemLabel.AddToClassList("nav-item__label");
                item.Add(itemLabel);

                var badge = new Label("0");
                badge.AddToClassList("badge");
                badge.AddToClassList("badge--hidden");
                item.Add(badge);

                childrenContainer.Add(item);
                _navItemRows.Add(new NavItemRow(node.ScreenId, item, badge));
            }

            _navTree.Add(childrenContainer);
            _categoryRows.Add(new CategoryRow(cat.Id, header, chevron, childrenContainer));
        }
    }

    // ── Named handler: category header collapse/expand ────────────────────

    private void OnCategoryHeaderPointerDown(PointerDownEvent evt)
    {
        if (evt.currentTarget is not VisualElement header) return;
        if (header.userData is not NavCategoryId catId) return;

        // Find the matching category row
        int count = _categoryRows.Count;
        for (int i = 0; i < count; i++)
        {
            if (_categoryRows[i].CategoryId != catId) continue;

            var row = _categoryRows[i];
            bool nowCollapsed = row.ChildrenContainer.style.display == DisplayStyle.None;

            // Toggle children visibility immediately (local UI state)
            row.ChildrenContainer.style.display = nowCollapsed ? DisplayStyle.Flex : DisplayStyle.None;

            // Rotate chevron
            if (nowCollapsed)
            {
                row.ChevronLabel.text = "∨";
                row.ChevronLabel.RemoveFromClassList("nav-category__chevron--collapsed");
            }
            else
            {
                row.ChevronLabel.text = "›";
                row.ChevronLabel.AddToClassList("nav-category__chevron--collapsed");
            }

            // Mirror into ViewModel — immediate local refresh
            _vm?.SetCollapsed(false); // IsCollapsed is sidebar-level, not per-category
            break;
        }
    }

    // ── Named handler: nav item click ─────────────────────────────────────

    private void OnNavItemPointerDown(PointerDownEvent evt)
    {
        if (evt.currentTarget is not VisualElement item) return;
        if (item.userData is not ScreenId screenId) return;

        _navigation?.NavigateTo(screenId);
    }

    // ── Named handler: collapse toggle ────────────────────────────────────

    private void OnCollapseToggle()
    {
        bool nowCollapsed = _vm != null && !_vm.IsCollapsed;
        _vm?.SetCollapsed(nowCollapsed);

        if (_sidebarRoot != null)
        {
            _sidebarRoot.EnableInClassList("sidebar--collapsed", nowCollapsed);
        }
        if (_collapseToggle != null)
        {
            _collapseToggle.text = nowCollapsed ? "»" : "«";
        }
    }

    // ── Navigation event handler ──────────────────────────────────────────

    private void OnNavigationScreenChanged(ScreenId previous, ScreenId current)
    {
        _vm?.SetActiveScreen(current);

        // Immediate local active-state update without waiting for coalesced refresh
        int itemCount = _navItemRows.Count;
        for (int i = 0; i < itemCount; i++)
        {
            var row = _navItemRows[i];
            row.Element.EnableInClassList("nav-item--active", row.ScreenId == current);
        }
    }

    // ── Badge update helper ───────────────────────────────────────────────

    private void UpdateBadge(ScreenId screenId, int count)
    {
        int itemCount = _navItemRows.Count;
        for (int i = 0; i < itemCount; i++)
        {
            var row = _navItemRows[i];
            if (row.ScreenId != screenId) continue;

            if (count > 0)
            {
                row.BadgeLabel.text = count > 99 ? "99+" : count.ToString();
                row.BadgeLabel.RemoveFromClassList("badge--hidden");
            }
            else
            {
                row.BadgeLabel.AddToClassList("badge--hidden");
            }
            return;
        }
    }
}
