using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Manages screen lifecycle within the screen-content-host shell region.
/// Caches views by ScreenId. Hidden screens stay mounted (DisplayStyle.None) to
/// preserve scroll position and element state across navigation.
/// </summary>
public class ScreenHost
{
    // ── View cache ────────────────────────────────────────────────────────

    private readonly Dictionary<ScreenId, (IGameView view, IViewModel vm, VisualElement container)> _cache
        = new Dictionary<ScreenId, (IGameView, IViewModel, VisualElement)>();

    // ── Shell host elements ───────────────────────────────────────────────

    private VisualElement _screenBodyHost;
    private VisualElement _screenHeaderHost;
    private VisualElement _screenToolbarHost;
    private UIServices    _services;

    // ── State ─────────────────────────────────────────────────────────────

    public ScreenId? CurrentScreenId { get; private set; }

    /// <summary>Returns the currently active view, or null if none is mounted.</summary>
    public IGameView CurrentView =>
        CurrentScreenId.HasValue && _cache.TryGetValue(CurrentScreenId.Value, out var c) ? c.view : null;

    /// <summary>Returns the currently active ViewModel, or null if none is mounted.</summary>
    public IViewModel CurrentViewModel =>
        CurrentScreenId.HasValue && _cache.TryGetValue(CurrentScreenId.Value, out var c) ? c.vm : null;

    // ── Snapshot delegate ─────────────────────────────────────────────────

    /// <summary>
    /// Delegate used to build a fresh GameStateSnapshot for ViewModel.Refresh().
    /// Assigned by WindowManager during Initialize.
    /// </summary>
    public Func<GameStateSnapshot> SnapshotBuilder { get; set; }

    // ── Initialization ────────────────────────────────────────────────────

    /// <summary>Call once from WindowManager after the UIDocument root is available.</summary>
    public void Initialize(VisualElement screenContentHost, UIServices services)
    {
        if (screenContentHost == null) throw new ArgumentNullException(nameof(screenContentHost));
        _services = services ?? throw new ArgumentNullException(nameof(services));

        _screenBodyHost    = screenContentHost.Q<VisualElement>("screen-body-host");
        _screenHeaderHost  = screenContentHost.Q<VisualElement>("screen-header-host");
        _screenToolbarHost = screenContentHost.Q<VisualElement>("screen-toolbar-host");

        if (_screenBodyHost == null)
        {
            // Fallback: use screenContentHost itself as body host
            Debug.LogWarning("[ScreenHost] screen-body-host not found — using screen-content-host as fallback.");
            _screenBodyHost = screenContentHost;
        }
    }

    // ── Public API ────────────────────────────────────────────────────────

    /// <summary>
    /// Mount or show the screen for the given config.
    /// Previous screen's container is hidden via DisplayStyle.None.
    /// </summary>
    public void ShowScreen(ScreenConfig config)
    {
        if (_screenBodyHost == null)
        {
            Debug.LogError("[ScreenHost] Not initialized — call Initialize() first.");
            return;
        }

        if (config.ViewFactory == null || config.ViewModelFactory == null)
        {
            Debug.LogWarning($"[ScreenHost] Null factories for screen {config.Id} — cannot show.");
            return;
        }

        // Hide current screen
        HideCurrentScreen();

        CurrentScreenId = config.Id;

        if (_cache.TryGetValue(config.Id, out var cached))
        {
            // Cache hit — restore visibility and refresh
            cached.container.style.display = DisplayStyle.Flex;

            var snapshot = SnapshotBuilder?.Invoke();
            if (snapshot != null)
            {
                SetProductBrowserTemplates(cached.vm, snapshot);
                cached.vm.Refresh(snapshot);
            }
            cached.view.Bind(cached.vm);
        }
        else
        {
            // Cache miss — build new view
            var vm   = config.ViewModelFactory();
            var view = config.ViewFactory();

            var container = new VisualElement();
            container.name = "screen-container--" + config.Id;
            container.AddToClassList("screen-container");
            container.style.flexGrow = 1;
            _screenBodyHost.Add(container);

            view.Initialize(container, _services);

            var snapshot = SnapshotBuilder?.Invoke();
            if (snapshot != null)
            {
                SetProductBrowserTemplates(vm, snapshot);
                vm.Refresh(snapshot);
            }
            view.Bind(vm);

            _cache[config.Id] = (view, vm, container);
        }
    }

    /// <summary>Hide the currently visible screen container.</summary>
    public void HideCurrentScreen()
    {
        if (CurrentScreenId.HasValue && _cache.TryGetValue(CurrentScreenId.Value, out var prev))
        {
            prev.container.style.display = DisplayStyle.None;
        }
    }

    /// <summary>Refresh the active screen's ViewModel from a fresh snapshot and rebind.</summary>
    public void RefreshCurrentScreen()
    {
        if (!CurrentScreenId.HasValue) return;
        if (!_cache.TryGetValue(CurrentScreenId.Value, out var current)) return;

        var snapshot = SnapshotBuilder?.Invoke();
        if (snapshot == null) return;

        SetProductBrowserTemplates(current.vm, snapshot);
        current.vm.Refresh(snapshot);
        current.view.Bind(current.vm);
    }

    /// <summary>Dispose all cached views and clear the cache.</summary>
    public void DisposeAll()
    {
        foreach (var kvp in _cache)
        {
            try { kvp.Value.view?.Dispose(); }
            catch (Exception ex) { Debug.LogError($"[ScreenHost] Exception disposing view {kvp.Key}: {ex}"); }
        }
        _cache.Clear();
        CurrentScreenId = null;
    }

    // ── Private helpers ───────────────────────────────────────────────────

    /// <summary>
    /// ProductsBrowserViewModel requires template injection that is not in the standard snapshot path.
    /// This is the one specialization — mirrors what WindowManager.RefreshCurrentViewModel does.
    /// </summary>
    private static void SetProductBrowserTemplates(IViewModel vm, GameStateSnapshot snapshot)
    {
        // No-op: template injection depends on GameController which ScreenHost does not own.
        // WindowManager.RefreshCurrentViewModel remains the owner for this specialization.
        // ScreenHost delegates snapshot refresh only.
    }
}
