using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class AccordionSidebar
{
    public event Action<ScreenId> OnScreenSelected;

    public bool IsCollapsed => _collapsed;

    private VisualElement _sidebarRoot;
    private NavNode _rootNode;
    private bool _collapsed;

    // Maps nodeId -> element for badge/active management
    private readonly Dictionary<string, VisualElement> _nodeElements = new Dictionary<string, VisualElement>();
    private readonly Dictionary<string, Label> _badgeLabels = new Dictionary<string, Label>();

    // Active tracking
    private string _activeLeafId;

    // Focusable items for keyboard nav (path in order of display)
    private readonly List<(NavNode node, VisualElement element)> _focusableItems
        = new List<(NavNode, VisualElement)>();
    private int _focusedIndex = -1;

    public void Initialize(VisualElement sidebarRoot, NavNode rootNode) {
        _sidebarRoot = sidebarRoot;
        _rootNode = rootNode;
        RebuildTree();
    }

    public void SetActiveScreen(ScreenId screenId) {
        // Find matching leaf
        NavNode leaf = FindLeafById(screenId, _rootNode);
        if (leaf == null) return;

        string prevActiveId = _activeLeafId;
        _activeLeafId = leaf.Id;

        // Clear old active classes
        foreach (var kvp in _nodeElements) {
            kvp.Value.RemoveFromClassList("sidebar-item--active");
            kvp.Value.RemoveFromClassList("sidebar-group--active");
        }

        // Mark leaf active
        if (_nodeElements.TryGetValue(leaf.Id, out var leafEl)) {
            leafEl.AddToClassList("sidebar-item--active");
        }

        // Mark parent chain active
        var parent = leaf.Parent;
        while (parent != null && parent != _rootNode) {
            if (_nodeElements.TryGetValue(parent.Id, out var parentEl)) {
                parentEl.AddToClassList("sidebar-group--active");
            }
            parent = parent.Parent;
        }

        // Expand parent chain
        ExpandParentChain(leaf);
    }

    public void SetCollapsed(bool collapsed) {
        _collapsed = collapsed;
        if (_sidebarRoot == null) return;
        if (collapsed) {
            _sidebarRoot.AddToClassList("sidebar--collapsed");
        } else {
            _sidebarRoot.RemoveFromClassList("sidebar--collapsed");
        }
        RebuildTree();
    }

    public void SetBadge(string nodeId, int count) {
        if (!_badgeLabels.TryGetValue(nodeId, out var badge)) return;
        if (count <= 0) {
            badge.AddToClassList("sidebar-badge--hidden");
        } else {
            badge.RemoveFromClassList("sidebar-badge--hidden");
            badge.text = count > 99 ? "99+" : count.ToString();
        }
    }

    public void ClearBadge(string nodeId) {
        SetBadge(nodeId, 0);
    }

    // Keyboard navigation
    public void HandleKeyDown(KeyCode key) {
        if (_focusableItems.Count == 0) return;

        switch (key) {
            case KeyCode.UpArrow:
                MoveFocus(-1);
                break;
            case KeyCode.DownArrow:
                MoveFocus(1);
                break;
            case KeyCode.RightArrow:
                ExpandFocused();
                break;
            case KeyCode.LeftArrow:
                CollapseFocused();
                break;
            case KeyCode.Return:
            case KeyCode.KeypadEnter:
                SelectFocused();
                break;
        }
    }

    public void HandleTopLevelHotkey(int index) {
        if (_rootNode?.Children == null) return;
        if (index < 0 || index >= _rootNode.Children.Count) return;
        var topNode = _rootNode.Children[index];
        ToggleExpand(topNode);
    }

    public bool HandleLeafHotkey(string key) {
        int count = _focusableItems.Count;
        for (int i = 0; i < count; i++) {
            var (node, _) = _focusableItems[i];
            if (!node.IsLeaf || !node.ScreenId.HasValue) continue;
            if (string.IsNullOrEmpty(node.Hotkey)) continue;
            if (!string.Equals(node.Hotkey, key, System.StringComparison.OrdinalIgnoreCase)) continue;
            var parent = node.Parent;
            if (parent == null || !parent.IsExpanded) continue;
            OnScreenSelected?.Invoke(node.ScreenId.Value);
            return true;
        }
        return false;
    }

    public void RegisterKeyboardEvents(VisualElement panel) {
        panel.RegisterCallback<KeyDownEvent>(OnKeyDown);
    }

    private void OnKeyDown(KeyDownEvent evt) {
        if (evt.target is UnityEngine.UIElements.TextField || evt.target is UnityEngine.UIElements.IntegerField) return;

        var key = evt.keyCode;
        if (key >= KeyCode.Alpha1 && key <= KeyCode.Alpha6) {
            HandleTopLevelHotkey(key - KeyCode.Alpha1);
            evt.StopPropagation();
            return;
        }
        if (key >= KeyCode.Keypad1 && key <= KeyCode.Keypad6) {
            HandleTopLevelHotkey(key - KeyCode.Keypad1);
            evt.StopPropagation();
            return;
        }
        if (key >= KeyCode.A && key <= KeyCode.Z) {
            bool consumed = HandleLeafHotkey(key.ToString());
            if (consumed) evt.StopPropagation();
            return;
        }
        if (key == KeyCode.UpArrow || key == KeyCode.DownArrow ||
            key == KeyCode.LeftArrow || key == KeyCode.RightArrow ||
            key == KeyCode.Return || key == KeyCode.KeypadEnter) {
            HandleKeyDown(key);
            evt.StopPropagation();
        }
    }

    // --- Private: Tree building ---

    private void RebuildTree() {
        if (_sidebarRoot == null || _rootNode == null) return;

        // Find the scroll content container or use sidebarRoot directly
        ScrollView scroll = _sidebarRoot.Q<ScrollView>("sidebar-content");
        VisualElement container = scroll != null ? scroll.contentContainer : _sidebarRoot.Q<VisualElement>("sidebar-content");
        if (container == null) container = _sidebarRoot;

        container.Clear();
        _nodeElements.Clear();
        _badgeLabels.Clear();
        _focusableItems.Clear();
        _focusedIndex = -1;

        if (_rootNode.Children == null) return;

        int count = _rootNode.Children.Count;
        for (int i = 0; i < count; i++) {
            var topNode = _rootNode.Children[i];
            var groupEl = BuildGroupElement(topNode, i);
            container.Add(groupEl);
        }

        // Restore active state
        if (_activeLeafId != null) {
            var leaf = FindLeafByStringId(_activeLeafId, _rootNode);
            if (leaf != null) {
                if (_nodeElements.TryGetValue(leaf.Id, out var leafEl)) {
                    leafEl.AddToClassList("sidebar-item--active");
                }
                var parent = leaf.Parent;
                while (parent != null && parent != _rootNode) {
                    if (_nodeElements.TryGetValue(parent.Id, out var parentEl)) {
                        parentEl.AddToClassList("sidebar-group--active");
                    }
                    parent = parent.Parent;
                }
            }
        }
    }

    private VisualElement BuildGroupElement(NavNode node, int topLevelIndex) {
        var group = new VisualElement();
        group.AddToClassList("sidebar-group");
        _nodeElements[node.Id] = group;

        // Header row
        var header = new VisualElement();
        header.AddToClassList("sidebar-group__header");

        // Icon (top-level only)
        if (node.Depth == 0 && !string.IsNullOrEmpty(node.Icon)) {
            var icon = new Label(node.Icon);
            icon.AddToClassList("sidebar-group__icon");
            header.Add(icon);
        } else if (node.Depth > 0) {
            // Connector indent for nested groups
            var indent = BuildConnector(node.Depth);
            header.Add(indent);
        }

        // Label
        var label = new Label(node.Label);
        label.AddToClassList("sidebar-group__label");
        header.Add(label);

        // Hotkey hint
        if (!string.IsNullOrEmpty(node.Hotkey)) {
            var hotkey = new Label(node.Hotkey);
            hotkey.AddToClassList("sidebar-hotkey");
            header.Add(hotkey);
        }

        // Badge
        var badge = new Label("0");
        badge.AddToClassList("sidebar-badge");
        badge.AddToClassList("sidebar-badge--hidden");
        _badgeLabels[node.Id] = badge;
        header.Add(badge);

        // Arrow
        var arrow = new Label(node.IsExpanded ? "▾" : "▸");
        arrow.AddToClassList("sidebar-group__arrow");
        if (node.IsLeaf) {
            arrow.style.visibility = Visibility.Hidden;
        }
        header.Add(arrow);

        group.Add(header);
        _focusableItems.Add((node, group));

        // Children container
        var children = new VisualElement();
        children.AddToClassList("sidebar-group__children");
        if (!node.IsExpanded || _collapsed) {
            children.AddToClassList("hidden");
        }
        group.Add(children);

        if (node.Children != null) {
            int childCount = node.Children.Count;
            for (int i = 0; i < childCount; i++) {
                var child = node.Children[i];
                if (child.IsLeaf) {
                    var itemEl = BuildLeafElement(child);
                    children.Add(itemEl);
                } else {
                    var subGroup = BuildGroupElement(child, i);
                    children.Add(subGroup);
                }
            }
        }

        // Wire click on header
        var capturedNode = node;
        var capturedArrow = arrow;
        var capturedChildren = children;
        header.RegisterCallback<ClickEvent>(evt => {
            OnGroupHeaderClicked(capturedNode, capturedArrow, capturedChildren);
        });

        return group;
    }

    private VisualElement BuildLeafElement(NavNode node) {
        var item = new VisualElement();
        item.AddToClassList("sidebar-item");
        _nodeElements[node.Id] = item;
        _focusableItems.Add((node, item));

        // Connector indent
        if (node.Depth > 0) {
            var indent = BuildConnector(node.Depth);
            item.Add(indent);
        }

        var label = new Label(node.Label);
        label.AddToClassList("sidebar-item__label");
        item.Add(label);

        // Hotkey hint
        if (!string.IsNullOrEmpty(node.Hotkey)) {
            var hotkey = new Label(node.Hotkey);
            hotkey.AddToClassList("sidebar-hotkey");
            item.Add(hotkey);
        }

        // Badge
        var badge = new Label("0");
        badge.AddToClassList("sidebar-badge");
        badge.AddToClassList("sidebar-badge--hidden");
        _badgeLabels[node.Id] = badge;
        item.Add(badge);

        var capturedNode = node;
        item.RegisterCallback<ClickEvent>(evt => {
            if (capturedNode.ScreenId.HasValue) {
                OnScreenSelected?.Invoke(capturedNode.ScreenId.Value);
            }
        });

        return item;
    }

    private VisualElement BuildConnector(int depth) {
        var connector = new VisualElement();
        connector.AddToClassList("sidebar-connector");
        connector.style.width = depth * 12;
        return connector;
    }

    private void OnGroupHeaderClicked(NavNode node, Label arrow, VisualElement childrenContainer) {
        if (_collapsed) {
            // In collapsed mode, clicking expands the sidebar
            SetCollapsed(false);
            return;
        }

        if (node.IsLeaf && node.ScreenId.HasValue) {
            OnScreenSelected?.Invoke(node.ScreenId.Value);
            return;
        }

        bool nowExpanded = !node.IsExpanded;
        node.IsExpanded = nowExpanded;

        // Single-expand at top level: collapse siblings
        if (node.Depth == 0 && nowExpanded && _rootNode.Children != null) {
            int count = _rootNode.Children.Count;
            for (int i = 0; i < count; i++) {
                var sibling = _rootNode.Children[i];
                if (sibling != node && sibling.IsExpanded) {
                    sibling.IsExpanded = false;
                    CollapseNodeElement(sibling);
                }
            }
        }

        arrow.text = nowExpanded ? "▾" : "▸";
        if (nowExpanded) {
            childrenContainer.RemoveFromClassList("hidden");
        } else {
            childrenContainer.AddToClassList("hidden");
        }
    }

    private void CollapseNodeElement(NavNode node) {
        if (!_nodeElements.TryGetValue(node.Id, out var groupEl)) return;
        var childrenContainer = groupEl.Q<VisualElement>(className: "sidebar-group__children");
        var arrow = groupEl.Q<Label>(className: "sidebar-group__arrow");
        if (childrenContainer != null) childrenContainer.AddToClassList("hidden");
        if (arrow != null) arrow.text = "▸";
    }

    private void ExpandParentChain(NavNode leaf) {
        var parent = leaf.Parent;
        while (parent != null && parent != _rootNode) {
            if (!parent.IsExpanded) {
                parent.IsExpanded = true;
                if (_nodeElements.TryGetValue(parent.Id, out var parentEl)) {
                    var childrenContainer = parentEl.Q<VisualElement>(className: "sidebar-group__children");
                    var arrow = parentEl.Q<Label>(className: "sidebar-group__arrow");
                    if (childrenContainer != null) childrenContainer.RemoveFromClassList("hidden");
                    if (arrow != null) arrow.text = "▾";
                }
            }
            parent = parent.Parent;
        }
    }

    private void ToggleExpand(NavNode node) {
        if (!_nodeElements.TryGetValue(node.Id, out var groupEl)) return;
        var childrenContainer = groupEl.Q<VisualElement>(className: "sidebar-group__children");
        var arrow = groupEl.Q<Label>(className: "sidebar-group__arrow");
        if (childrenContainer == null || arrow == null) return;

        bool nowExpanded = !node.IsExpanded;
        node.IsExpanded = nowExpanded;

        if (node.Depth == 0 && nowExpanded && _rootNode.Children != null) {
            int count = _rootNode.Children.Count;
            for (int i = 0; i < count; i++) {
                var sibling = _rootNode.Children[i];
                if (sibling != node && sibling.IsExpanded) {
                    sibling.IsExpanded = false;
                    CollapseNodeElement(sibling);
                }
            }
        }

        arrow.text = nowExpanded ? "▾" : "▸";
        if (nowExpanded) {
            childrenContainer.RemoveFromClassList("hidden");
        } else {
            childrenContainer.AddToClassList("hidden");
        }
    }

    // --- Keyboard helpers ---

    private void MoveFocus(int delta) {
        if (_focusableItems.Count == 0) return;
        _focusedIndex = Mathf.Clamp(_focusedIndex + delta, 0, _focusableItems.Count - 1);
        ApplyFocusHighlight();
    }

    private void ExpandFocused() {
        if (_focusedIndex < 0 || _focusedIndex >= _focusableItems.Count) return;
        var (node, _) = _focusableItems[_focusedIndex];
        if (!node.IsLeaf) ToggleExpand(node);
    }

    private void CollapseFocused() {
        if (_focusedIndex < 0 || _focusedIndex >= _focusableItems.Count) return;
        var (node, _) = _focusableItems[_focusedIndex];
        if (!node.IsLeaf && node.IsExpanded) {
            node.IsExpanded = false;
            CollapseNodeElement(node);
        } else if (node.Parent != null && node.Parent != _rootNode) {
            // Move focus to parent
            int parentIdx = _focusableItems.FindIndex(t => t.node == node.Parent);
            if (parentIdx >= 0) {
                _focusedIndex = parentIdx;
                ApplyFocusHighlight();
            }
        }
    }

    private void SelectFocused() {
        if (_focusedIndex < 0 || _focusedIndex >= _focusableItems.Count) return;
        var (node, _) = _focusableItems[_focusedIndex];
        if (node.IsLeaf && node.ScreenId.HasValue) {
            OnScreenSelected?.Invoke(node.ScreenId.Value);
        }
    }

    private void ApplyFocusHighlight() {
        int count = _focusableItems.Count;
        for (int i = 0; i < count; i++) {
            var (_, el) = _focusableItems[i];
            if (i == _focusedIndex) {
                el.AddToClassList("sidebar-item--focused");
            } else {
                el.RemoveFromClassList("sidebar-item--focused");
            }
        }
    }

    // --- Tree search helpers ---

    private NavNode FindLeafById(ScreenId screenId, NavNode node) {
        if (node == null) return null;
        if (node.IsLeaf && node.ScreenId.HasValue && node.ScreenId.Value == screenId) return node;
        if (node.Children == null) return null;
        int count = node.Children.Count;
        for (int i = 0; i < count; i++) {
            var result = FindLeafById(screenId, node.Children[i]);
            if (result != null) return result;
        }
        return null;
    }

    private NavNode FindLeafByStringId(string nodeId, NavNode node) {
        if (node == null) return null;
        if (node.Id == nodeId) return node;
        if (node.Children == null) return null;
        int count = node.Children.Count;
        for (int i = 0; i < count; i++) {
            var result = FindLeafByStringId(nodeId, node.Children[i]);
            if (result != null) return result;
        }
        return null;
    }
}
