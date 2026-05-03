using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Manages the modal-layer region. Shows and dismisses modals with backdrop,
/// optional close button, and a modal-over-modal stack for confirmation dialogs.
/// Plain C# class; initialized by WindowManager and passed via UIServices.
/// </summary>
public class ModalPresenter : IModalPresenter
{
    // ── Stack entry ───────────────────────────────────────────────────────

    private struct ModalEntry
    {
        public IGameView   View;
        public IViewModel  ViewModel;
        public ModalOptions Options;
        public VisualElement Wrapper;
    }

    // ── Cached layer elements ─────────────────────────────────────────────

    private VisualElement _modalLayer;
    private VisualElement _modalBackdrop;
    private UIServices    _services;

    // ── Modal stack ───────────────────────────────────────────────────────

    private readonly Stack<ModalEntry> _stack = new Stack<ModalEntry>();

    // ── IModalPresenter ───────────────────────────────────────────────────

    public bool IsModalOpen => _stack.Count > 0;

    // ── Initialization ────────────────────────────────────────────────────

    /// <summary>
    /// Call once from WindowManager after the UIDocument root is available.
    /// </summary>
    public void Initialize(VisualElement modalLayer, UIServices services)
    {
        _modalLayer  = modalLayer  ?? throw new ArgumentNullException(nameof(modalLayer));
        _services    = services;

        _modalBackdrop = _modalLayer.Q<VisualElement>("modal-backdrop");

        if (_modalBackdrop != null)
        {
            _modalBackdrop.RegisterCallback<PointerDownEvent>(OnBackdropPointerDown);
        }
    }

    // ── Public API ────────────────────────────────────────────────────────

    /// <summary>
    /// Open a modal. Pushes onto the stack. Supports modal-over-modal for
    /// confirmation dialogs layered over detail modals.
    /// </summary>
    public void ShowModal(IGameView view, IViewModel viewModel, ModalOptions options = default)
    {
        if (view == null)
        {
            Debug.LogWarning("[ModalPresenter] ShowModal called with null view — ignored.");
            return;
        }
        if (viewModel == null)
        {
            Debug.LogWarning("[ModalPresenter] ShowModal called with null viewModel — ignored.");
            return;
        }

        if (_modalLayer == null)
        {
            Debug.LogError("[ModalPresenter] Not initialized — call Initialize() first.");
            return;
        }

        // Resolve options defaults
        if (options.WidthClass == null) options = ModalOptions.Default;

        // Hide the current top wrapper without disposing it
        if (_stack.Count > 0)
        {
            _stack.Peek().Wrapper.style.display = DisplayStyle.None;
        }

        // Build the modal wrapper
        var wrapper = BuildWrapper(view, viewModel, options);
        _modalLayer.Add(wrapper);

        // Push entry
        _stack.Push(new ModalEntry
        {
            View      = view,
            ViewModel = viewModel,
            Options   = options,
            Wrapper   = wrapper,
        });

        // Show layer
        _modalLayer.style.display = DisplayStyle.Flex;

        // Initialize and bind the view
        view.Initialize(wrapper, _services);
        RefreshAndBind(view, viewModel);
    }

    /// <summary>Dismiss the topmost modal.</summary>
    public void DismissModal()
    {
        if (_stack.Count == 0) return;

        var entry = _stack.Pop();
        SafeDispose(entry.View, entry.Wrapper);

        if (_stack.Count == 0)
        {
            _modalLayer.style.display = DisplayStyle.None;
        }
        else
        {
            // Restore the previous modal
            _stack.Peek().Wrapper.style.display = DisplayStyle.Flex;
        }
    }

    /// <summary>Dismiss all open modals.</summary>
    public void DismissAll()
    {
        while (_stack.Count > 0)
        {
            var entry = _stack.Pop();
            SafeDispose(entry.View, entry.Wrapper);
        }

        if (_modalLayer != null)
        {
            _modalLayer.style.display = DisplayStyle.None;
        }
    }

    // Convenience openers — delegated back to the caller via stored references
    // These are expected to be wired by WindowManager which passes itself via UIServices.
    // ModalPresenter itself does not know about game-specific modal types.

    void IModalPresenter.OpenCompetitorProfile(CompetitorId competitorId)
    {
        Debug.LogWarning("[ModalPresenter] OpenCompetitorProfile must be handled by the WindowManager wrapper.");
    }

    void IModalPresenter.OpenProductDetail(ProductId productId)
    {
        Debug.LogWarning("[ModalPresenter] OpenProductDetail must be handled by the WindowManager wrapper.");
    }

    void IModalPresenter.OpenRenewalModal(EmployeeId? autoExpandId)
    {
        Debug.LogWarning("[ModalPresenter] OpenRenewalModal must be handled by the WindowManager wrapper.");
    }

    void IModalPresenter.ShowCandidateDetailModal(int candidateId, bool showCounterOffer)
    {
        Debug.LogWarning("[ModalPresenter] ShowCandidateDetailModal must be handled by the WindowManager wrapper.");
    }

    // ── Private helpers ───────────────────────────────────────────────────

    private VisualElement BuildWrapper(IGameView view, IViewModel viewModel, ModalOptions options)
    {
        var wrapper = new VisualElement();
        wrapper.AddToClassList("modal-wrapper");

        // Size class
        string widthClass = string.IsNullOrEmpty(options.WidthClass) ? "modal-wrapper--md" : options.WidthClass;
        wrapper.AddToClassList(widthClass);

        // Close button
        if (options.ShowCloseButton)
        {
            var closeBtn = new Button(DismissModal);
            closeBtn.AddToClassList("modal-close");
            closeBtn.text = "✕";
            wrapper.Add(closeBtn);
        }

        return wrapper;
    }

    private static void RefreshAndBind(IGameView view, IViewModel viewModel)
    {
        // ViewModel.Refresh requires a snapshot; the view is expected to receive
        // a valid snapshot via the view's Initialize, or the caller should refresh
        // before calling ShowModal. We bind with the current ViewModel state here.
        view.Bind(viewModel);
    }

    private static void SafeDispose(IGameView view, VisualElement wrapper)
    {
        try { view?.Dispose(); }
        catch (Exception ex) { Debug.LogError($"[ModalPresenter] Exception during view Dispose: {ex}"); }

        wrapper?.RemoveFromHierarchy();
    }

    private void OnBackdropPointerDown(PointerDownEvent evt)
    {
        if (_stack.Count == 0) return;
        var top = _stack.Peek();
        if (top.Options.DismissOnBackdropClick)
        {
            DismissModal();
        }
    }
}
