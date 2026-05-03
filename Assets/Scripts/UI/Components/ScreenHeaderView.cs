using System;
using UnityEngine.UIElements;

/// <summary>
/// Reusable view for the screen header pattern (breadcrumb, icon, title, subtitle).
/// Drives the ScreenHeader.uxml template. Initialize once; Bind on every refresh.
/// </summary>
public class ScreenHeaderView
{
    private Label         _breadcrumb;
    private VisualElement _icon;
    private Label         _title;
    private Label         _subtitle;

    private Action _onBack;

    // ── Initialize ───────────────────────────────────────────────────────────

    /// <summary>
    /// Query and cache elements from the host, and wire the back-breadcrumb click handler.
    /// </summary>
    public void Initialize(VisualElement host)
    {
        if (host == null) return;

        _breadcrumb = host.Q<Label>("back-breadcrumb");
        _icon       = host.Q<VisualElement>("header-icon");
        _title      = host.Q<Label>("header-title");
        _subtitle   = host.Q<Label>("header-subtitle");

        if (_breadcrumb != null)
            _breadcrumb.RegisterCallback<ClickEvent>(OnBackClicked);
    }

    // ── Bind ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Update breadcrumb visibility/label, icon class, title, and subtitle from data.
    /// No element queries; no handler wiring.
    /// </summary>
    public void Bind(ScreenHeaderData data)
    {
        if (_breadcrumb != null)
        {
            _breadcrumb.text = data.BreadcrumbLabel ?? string.Empty;
            _breadcrumb.style.display = data.ShowBreadcrumb ? DisplayStyle.Flex : DisplayStyle.None;
        }

        if (_icon != null)
        {
            // Clear previous icon class and apply new one
            _icon.ClearClassList();
            _icon.AddToClassList("header-icon");
            if (!string.IsNullOrEmpty(data.IconClass))
                _icon.AddToClassList(data.IconClass);
        }

        if (_title    != null) _title.text    = data.Title    ?? string.Empty;
        if (_subtitle != null) _subtitle.text = data.Subtitle ?? string.Empty;
    }

    // ── SetBackAction ────────────────────────────────────────────────────────

    /// <summary>
    /// Wire a callback invoked when the breadcrumb is clicked.
    /// </summary>
    public void SetBackAction(Action onBack)
    {
        _onBack = onBack;
    }

    // ── Dispose ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Unregister handlers and null cached references.
    /// </summary>
    public void Dispose()
    {
        if (_breadcrumb != null)
            _breadcrumb.UnregisterCallback<ClickEvent>(OnBackClicked);

        _breadcrumb = null;
        _icon       = null;
        _title      = null;
        _subtitle   = null;
        _onBack     = null;
    }

    // ── Handler ──────────────────────────────────────────────────────────────

    private void OnBackClicked(ClickEvent evt)
    {
        _onBack?.Invoke();
    }
}
