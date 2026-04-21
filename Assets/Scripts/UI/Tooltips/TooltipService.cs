using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class TooltipService {
    private const int ShowDelayMs = 400;
    private const float OffsetAbove = 12f;
    private const float OffsetBelow = 20f;
    private const string VisibleClass = "tooltip--visible";

    private static readonly string[] RowStyleClasses = {
        "tooltip__stat-row--normal",
        "tooltip__stat-row--header",
        "tooltip__stat-row--unlocked",
        "tooltip__stat-row--locked"
    };

    private VisualElement _root;
    private VisualElement _tooltipContainer;
    private TooltipRegistry _registry;

    private Label _titleLabel;
    private Label _bodyLabel;

    // Stat rows pool
    private readonly List<VisualElement> _statRows = new List<VisualElement>();
    private VisualElement _statDivider;
    private VisualElement _statsContainer;

    // State
    private bool _isShowing;
    private Vector2 _lastMousePosition;
    private VisualElement _pendingElement;
    private IVisualElementScheduledItem _showSchedule;

    // Per-element registrations (so we can unregister on Dispose)
    private readonly List<VisualElement> _registeredElements = new List<VisualElement>();

    // Registered scroll views (for scroll-dismiss)
    private readonly List<VisualElement> _scrollContentContainers = new List<VisualElement>();

    public void Initialize(VisualElement root, VisualElement tooltipContainer, TooltipRegistry registry) {
        _root = root;
        _tooltipContainer = tooltipContainer;
        _registry = registry;

        if (_tooltipContainer == null) {
            Debug.LogError("[TooltipService] tooltip-container not found in visual tree. Tooltips disabled.");
            return;
        }

        Debug.Log($"[TooltipService] Initialized. Root={_root?.name}, Container={_tooltipContainer?.name}, Registry={(_registry != null ? "assigned" : "NULL")}");

        _titleLabel = _tooltipContainer.Q<Label>("tooltip-title");
        _bodyLabel  = _tooltipContainer.Q<Label>("tooltip-body");

        if (_titleLabel == null) Debug.LogWarning("[TooltipService] tooltip-title label not found in container.");
        if (_bodyLabel == null)  Debug.LogWarning("[TooltipService] tooltip-body label not found in container.");

        _statDivider = new VisualElement();
        _statDivider.AddToClassList("tooltip__divider");
        _statDivider.style.display = DisplayStyle.None;
        _tooltipContainer.Add(_statDivider);

        _statsContainer = new VisualElement();
        _statsContainer.name = "tooltip-stats";
        _statsContainer.style.display = DisplayStyle.None;
        _tooltipContainer.Add(_statsContainer);

        // Ensure tooltip and its children never intercept pointer events
        _tooltipContainer.pickingMode = PickingMode.Ignore;
        if (_titleLabel != null) _titleLabel.pickingMode = PickingMode.Ignore;
        if (_bodyLabel != null)  _bodyLabel.pickingMode = PickingMode.Ignore;

        _root.RegisterCallback<PointerMoveEvent>(OnPointerMove, TrickleDown.TrickleDown);

        RegisterScrollViews();
    }

    public void ReregisterScrollViews() {
        UnregisterScrollViews();
        RegisterScrollViews();
    }

    public void Dispose() {
        if (_root == null) return;

        _root.UnregisterCallback<PointerMoveEvent>(OnPointerMove, TrickleDown.TrickleDown);

        int count = _registeredElements.Count;
        for (int i = 0; i < count; i++) DetachCallbacks(_registeredElements[i]);
        _registeredElements.Clear();

        UnregisterScrollViews();
        _showSchedule?.Pause();
        _showSchedule = null;
    }

    // --- Element Registration ---

    public void Register(VisualElement element) {
        if (element == null || _root == null) {
            Debug.LogWarning($"[TooltipService] Register skipped: element={(element != null ? element.name : "null")}, root={(_root != null ? _root.name : "null")}");
            return;
        }
        if (_registeredElements.Contains(element)) return;

        element.RegisterCallback<PointerEnterEvent>(OnPointerEnter);
        element.RegisterCallback<PointerLeaveEvent>(OnPointerLeave);
        _registeredElements.Add(element);
    }

    public void Unregister(VisualElement element) {
        if (element == null) return;
        DetachCallbacks(element);
        _registeredElements.Remove(element);
    }

    private void DetachCallbacks(VisualElement element) {
        element.UnregisterCallback<PointerEnterEvent>(OnPointerEnter);
        element.UnregisterCallback<PointerLeaveEvent>(OnPointerLeave);
    }

    // --- Public API ---

    public void Show(TooltipData data, Vector2 screenPosition) {
        _lastMousePosition = screenPosition;
        ShowData(data);
    }

    public void Hide() {
        _showSchedule?.Pause();
        _showSchedule = null;
        _pendingElement = null;

        if (_tooltipContainer == null) return;
        _tooltipContainer.RemoveFromClassList(VisibleClass);
        _isShowing = false;
    }

    // --- Event Handlers ---

    private void OnPointerEnter(PointerEnterEvent evt) {
        var target = evt.currentTarget as VisualElement;
        if (target == null || _pendingElement == target) return;

        // Verify this element has tooltip data
        if (target.userData is not TooltipInfo) return;

        _showSchedule?.Pause();
        _showSchedule = null;
        _pendingElement = target;

        var captured = target;
        _showSchedule = _root.schedule.Execute(() => ShowForElement(captured));
        _showSchedule.ExecuteLater(ShowDelayMs);
    }

    private void OnPointerLeave(PointerLeaveEvent evt) {
        var target = evt.currentTarget as VisualElement;
        if (target == _pendingElement) {
            _showSchedule?.Pause();
            _showSchedule = null;
            _pendingElement = null;
        }
        Hide();
    }

    private void OnPointerMove(PointerMoveEvent evt) {
        _lastMousePosition = evt.position;
        if (_isShowing) PositionTooltip();
    }

    // --- Show Logic ---

    private void ShowForElement(VisualElement element) {
        _showSchedule = null;
        if (element == null) return;

        var info = element.userData as TooltipInfo;
        if (info == null) {
            Debug.LogWarning($"[TooltipService] ShowForElement: no TooltipInfo on element '{element.name}'");
            return;
        }

        if (info.IsRich) {
            // Direct data first
            if (info.DirectData.HasValue) {
                ShowData(info.DirectData.Value);
                return;
            }

            // Registry key lookup
            string key = info.RegistryKey;
            if (!string.IsNullOrEmpty(key) && _registry != null && _registry.TryGet(key, out var registryData)) {
                ShowData(registryData);
                return;
            }

            // Fallback: show registry key literally if registry miss
            if (!string.IsNullOrEmpty(key)) {
                Debug.LogWarning($"[TooltipService] Registry miss for key '{key}'. Showing literal text.");
                ShowData(new TooltipData { Title = string.Empty, Body = key, Stats = null });
                return;
            }
        }

        // Simple text tooltip
        string text = info.SimpleText;
        if (string.IsNullOrEmpty(text)) return;

        ShowData(new TooltipData { Title = string.Empty, Body = text, Stats = null });
    }

    private void ShowData(TooltipData data) {
        if (_tooltipContainer == null) return;

        bool hasTitle = !string.IsNullOrEmpty(data.Title);
        if (_titleLabel != null) {
            _titleLabel.text = hasTitle ? data.Title : string.Empty;
            _titleLabel.style.display = hasTitle ? DisplayStyle.Flex : DisplayStyle.None;
        }

        if (_bodyLabel != null) _bodyLabel.text = data.Body ?? string.Empty;

        bool hasStats = data.Stats != null && data.Stats.Length > 0;
        if (_statDivider != null) _statDivider.style.display = hasStats ? DisplayStyle.Flex : DisplayStyle.None;
        if (_statsContainer != null) {
            _statsContainer.style.display = hasStats ? DisplayStyle.Flex : DisplayStyle.None;
            if (hasStats) PopulateStats(data.Stats);
        }

        // Make visible FIRST so resolvedStyle has valid dimensions for positioning
        _tooltipContainer.AddToClassList(VisibleClass);
        _isShowing = true;

        // Best-effort position now, then reposition after layout resolves
        PositionTooltip();
        _tooltipContainer.RegisterCallbackOnce<GeometryChangedEvent>(_ => {
            if (_isShowing) PositionTooltip();
        });
    }

    private void PopulateStats(TooltipStatRow[] stats) {
        int needed = stats.Length;

        while (_statRows.Count < needed) {
            var row = new VisualElement();
            row.AddToClassList("tooltip__stat-row");
            row.pickingMode = PickingMode.Ignore;

            var labelEl = new Label();
            labelEl.AddToClassList("tooltip__stat-label");
            labelEl.pickingMode = PickingMode.Ignore;
            row.Add(labelEl);

            var valueEl = new Label();
            valueEl.AddToClassList("tooltip__stat-value");
            valueEl.pickingMode = PickingMode.Ignore;
            row.Add(valueEl);

            _statsContainer.Add(row);
            _statRows.Add(row);
        }

        int rowCount = _statRows.Count;
        for (int i = 0; i < rowCount; i++) {
            if (i < needed) {
                _statRows[i].style.display = DisplayStyle.Flex;
                var labelEl = _statRows[i].ElementAt(0) as Label;
                var valueEl = _statRows[i].ElementAt(1) as Label;
                if (labelEl != null) labelEl.text = stats[i].Label;
                if (valueEl != null) valueEl.text  = stats[i].Value;
                int styleCount = RowStyleClasses.Length;
                for (int s = 0; s < styleCount; s++) {
                    _statRows[i].EnableInClassList(RowStyleClasses[s], s == (int)stats[i].Style);
                }
            } else {
                _statRows[i].style.display = DisplayStyle.None;
            }
        }
    }

    private void PositionTooltip() {
        if (_tooltipContainer == null || _root == null) return;

        float tooltipWidth  = _tooltipContainer.resolvedStyle.width;
        float tooltipHeight = _tooltipContainer.resolvedStyle.height;

        // Guard against NaN when layout hasn't resolved yet
        if (float.IsNaN(tooltipWidth))  tooltipWidth  = 0f;
        if (float.IsNaN(tooltipHeight)) tooltipHeight = 0f;

        Rect rootBounds = _root.worldBound;
        float mouseX    = _lastMousePosition.x;
        float mouseY    = _lastMousePosition.y;

        float x = mouseX - tooltipWidth * 0.5f;
        float y = mouseY - tooltipHeight - OffsetAbove;

        if (y < rootBounds.yMin) y = mouseY + OffsetBelow;
        if (x < rootBounds.xMin) x = rootBounds.xMin;
        else if (x + tooltipWidth > rootBounds.xMax) x = rootBounds.xMax - tooltipWidth;
        if (y + tooltipHeight > rootBounds.yMax) y = rootBounds.yMax - tooltipHeight;

        _tooltipContainer.style.left = new Length(x, LengthUnit.Pixel);
        _tooltipContainer.style.top  = new Length(y, LengthUnit.Pixel);
    }

    // --- Scroll Dismiss ---

    private void RegisterScrollViews() {
        if (_root == null) return;
        _root.Query<ScrollView>().ForEach(sv => {
            var content = sv.contentContainer;
            if (content == null) return;
            content.RegisterCallback<GeometryChangedEvent>(OnScrollContentChanged);
            _scrollContentContainers.Add(content);
        });
    }

    private void UnregisterScrollViews() {
        int count = _scrollContentContainers.Count;
        for (int i = 0; i < count; i++)
            _scrollContentContainers[i].UnregisterCallback<GeometryChangedEvent>(OnScrollContentChanged);
        _scrollContentContainers.Clear();
    }

    private void OnScrollContentChanged(GeometryChangedEvent evt) {
        if (_isShowing) Hide();
    }
}
