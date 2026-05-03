using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Manages shell-level visual effects: the top bar running underline,
/// sidebar active glow, card hover glow, modal backdrop noise, and toast accents.
///
/// Applies USS effect classes from effects.uss as the primary rendering mechanism.
/// Shader material integration via generateVisualContent is a future enhancement —
/// USS fallback classes provide the visual treatment in the meantime.
///
/// Follows UI_Architecture_v3: Initialize / SetGameRunning / Dispose lifecycle.
/// Presentation-only — never reads or mutates simulation state.
/// </summary>
public class ShellEffectsController
{
    // ── Cached elements ───────────────────────────────────────────────────

    private VisualElement _root;
    private VisualElement _topStatusBar;
    private VisualElement _underline;
    private VisualElement _sidebarNavigation;
    private VisualElement _modalBackdrop;
    private VisualElement _toastLayer;

    // ── Card hover tracking ───────────────────────────────────────────────

    private readonly List<VisualElement> _registeredCards = new List<VisualElement>();

    // ── State ─────────────────────────────────────────────────────────────

    private bool _isInitialized;
    private bool _isGameRunning;

    // ── Public API ────────────────────────────────────────────────────────

    /// <summary>
    /// Resolve shell elements and create the underline VisualElement.
    /// Call once from WindowManager.Initialize() after the UIDocument root is available.
    /// </summary>
    public void Initialize(VisualElement root)
    {
        if (root == null)
        {
            Debug.LogWarning("[ShellEffectsController] Root is null — effects will not be applied.");
            return;
        }

        _root = root;

        // ── Top bar underline ────────────────────────────────────────────
        _topStatusBar = _root.Q<VisualElement>("top-status-bar");
        if (_topStatusBar != null)
        {
            _underline = new VisualElement();
            _underline.name = "topbar-underline";
            _underline.AddToClassList("top-bar__underline");
            _topStatusBar.Add(_underline);
        }

        // ── Sidebar navigation ───────────────────────────────────────────
        _sidebarNavigation = _root.Q<VisualElement>("sidebar-navigation");

        // ── Modal backdrop ───────────────────────────────────────────────
        _modalBackdrop = _root.Q<VisualElement>("modal-backdrop");
        if (_modalBackdrop != null)
        {
            _modalBackdrop.AddToClassList("effect-modal-backdrop-noise");
        }

        // ── Toast layer ──────────────────────────────────────────────────
        _toastLayer = _root.Q<VisualElement>("toast-layer");

        _isInitialized = true;

        Debug.Log("[ShellEffectsController] Initialized — USS effect classes applied.");
    }

    /// <summary>
    /// Toggle the top bar underline running animation state.
    /// Call when game advancing state changes.
    /// </summary>
    public void SetGameRunning(bool running)
    {
        _isGameRunning = running;

        if (_underline != null)
        {
            _underline.EnableInClassList("top-bar__underline--running", running);
        }
    }

    /// <summary>
    /// Register hover glow callbacks on card elements within a container.
    /// Call after a screen mounts to apply hover effects to its cards.
    /// </summary>
    public void RegisterCardHoverEffects(VisualElement container)
    {
        if (container == null) return;

        var cards = container.Query<VisualElement>(className: "card").ToList();
        int count = cards.Count;
        for (int i = 0; i < count; i++)
        {
            var card = cards[i];
            if (_registeredCards.Contains(card)) continue;

            card.RegisterCallback<MouseEnterEvent>(OnCardMouseEnter);
            card.RegisterCallback<MouseLeaveEvent>(OnCardMouseLeave);
            _registeredCards.Add(card);
        }
    }

    /// <summary>
    /// Unregister all callbacks and clean up.
    /// Call from WindowManager.OnDestroy().
    /// </summary>
    public void Dispose()
    {
        if (!_isInitialized) return;

        // Unregister card hover callbacks
        int count = _registeredCards.Count;
        for (int i = 0; i < count; i++)
        {
            _registeredCards[i].UnregisterCallback<MouseEnterEvent>(OnCardMouseEnter);
            _registeredCards[i].UnregisterCallback<MouseLeaveEvent>(OnCardMouseLeave);
        }
        _registeredCards.Clear();

        // Remove underline element
        if (_underline != null && _underline.parent != null)
        {
            _underline.parent.Remove(_underline);
        }

        _underline           = null;
        _topStatusBar        = null;
        _sidebarNavigation   = null;
        _modalBackdrop       = null;
        _toastLayer          = null;
        _root                = null;
        _isInitialized       = false;

        Debug.Log("[ShellEffectsController] Disposed.");
    }

    // ── Named handlers ────────────────────────────────────────────────────

    private static void OnCardMouseEnter(MouseEnterEvent evt)
    {
        if (evt.currentTarget is VisualElement card)
        {
            card.AddToClassList("effect-card-hover-glow");
        }
    }

    private static void OnCardMouseLeave(MouseLeaveEvent evt)
    {
        if (evt.currentTarget is VisualElement card)
        {
            card.RemoveFromClassList("effect-card-hover-glow");
        }
    }
}
