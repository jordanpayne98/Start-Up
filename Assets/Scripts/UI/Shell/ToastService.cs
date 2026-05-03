using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Manages the toast-layer region. Stacks toast notifications top-right
/// with auto-dismiss timers, optional action buttons, and a 5-toast maximum.
/// Toast elements are ephemeral (created on Show, removed on Dismiss) so
/// per-instance lambda captures for close/action are acceptable here.
/// Plain C# class; initialized by WindowManager and passed via UIServices.
/// </summary>
public class ToastService : IToastService
{
    private const int MaxToasts       = 5;
    private const int DefaultDuration = 5000; // ms

    // ── Cached elements ───────────────────────────────────────────────────

    private VisualElement _toastLayer;

    // ── Active toast tracking ─────────────────────────────────────────────

    private readonly List<(int id, VisualElement element)> _active
        = new List<(int id, VisualElement element)>();

    private int _nextId = 1;

    // ── Initialization ────────────────────────────────────────────────────

    /// <summary>Call once from WindowManager after the UIDocument root is available.</summary>
    public void Initialize(VisualElement toastLayer)
    {
        _toastLayer = toastLayer ?? throw new ArgumentNullException(nameof(toastLayer));
    }

    // ── IToastService ─────────────────────────────────────────────────────

    /// <summary>Display a toast notification.</summary>
    public void Show(ToastData data)
    {
        if (_toastLayer == null)
        {
            Debug.LogError("[ToastService] Not initialized — call Initialize() first.");
            return;
        }

        // Trim oldest if at cap
        while (_active.Count >= MaxToasts)
        {
            var (oldId, _) = _active[_active.Count - 1];
            Dismiss(oldId);
        }

        int toastId = _nextId++;
        data.Id = toastId;

        // Build toast element; captures toastId and OnAction
        var toast = BuildToast(data, toastId);

        // Insert at top of layer (newest on top)
        _toastLayer.Insert(0, toast);
        _active.Insert(0, (toastId, toast));

        // Show layer
        _toastLayer.style.display = DisplayStyle.Flex;

        // Auto-dismiss timer — frame-safe via IVisualElementSchedule, no coroutines
        int durationMs = data.Duration > 0f ? Mathf.RoundToInt(data.Duration * 1000f) : DefaultDuration;
        int capturedId = toastId;
        toast.schedule.Execute(() => Dismiss(capturedId)).ExecuteLater(durationMs);
    }

    /// <summary>Dismiss a specific toast by ID.</summary>
    public void Dismiss(int toastId)
    {
        if (_toastLayer == null) return;

        int count = _active.Count;
        for (int i = 0; i < count; i++)
        {
            if (_active[i].id != toastId) continue;

            var element = _active[i].element;
            _active.RemoveAt(i);
            element.RemoveFromHierarchy();

            if (_active.Count == 0)
            {
                _toastLayer.style.display = DisplayStyle.None;
            }
            return;
        }
    }

    // ── Builder ───────────────────────────────────────────────────────────

    private VisualElement BuildToast(ToastData data, int toastId)
    {
        var toast = new VisualElement();
        toast.AddToClassList("toast");

        // Type modifier class
        string typeClass = data.Type switch
        {
            ToastType.Success => "toast--success",
            ToastType.Warning => "toast--warning",
            ToastType.Danger  => "toast--error",
            _                 => "toast--info",
        };
        toast.AddToClassList(typeClass);

        // Icon
        var icon = new Label();
        icon.AddToClassList("toast-icon");
        icon.text = data.Type switch
        {
            ToastType.Success => "✓",
            ToastType.Warning => "!",
            ToastType.Danger  => "✕",
            _                 => "i",
        };
        if (!string.IsNullOrEmpty(data.IconClass))
        {
            icon.AddToClassList(data.IconClass);
        }
        toast.Add(icon);

        // Content column
        var content = new VisualElement();
        content.AddToClassList("toast-content");
        toast.Add(content);

        if (!string.IsNullOrEmpty(data.Title))
        {
            var title = new Label(data.Title);
            title.AddToClassList("toast-title");
            content.Add(title);
        }

        if (!string.IsNullOrEmpty(data.Message))
        {
            var message = new Label(data.Message);
            message.AddToClassList("toast-message");
            content.Add(message);
        }

        // Action button — toasts are ephemeral, captures are acceptable
        if (!string.IsNullOrEmpty(data.ActionLabel) && data.OnAction != null)
        {
            var actionBtn = new Button();
            actionBtn.AddToClassList("toast-action");
            actionBtn.text = data.ActionLabel;
            Action captured = data.OnAction;
            actionBtn.clicked += () => captured();
            content.Add(actionBtn);
        }

        // Close button — captures toastId for ephemeral lifetime element
        var closeBtn = new Button();
        closeBtn.AddToClassList("toast-close");
        closeBtn.text = "✕";
        int capturedId = toastId;
        closeBtn.clicked += () => Dismiss(capturedId);
        toast.Add(closeBtn);

        return toast;
    }
}
