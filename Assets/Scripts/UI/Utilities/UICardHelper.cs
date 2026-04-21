using System;
using UnityEngine.UIElements;

public static class UICardHelper
{
    // ── Depth ──

    /// <summary>
    /// Wraps a card element in a relative container with an absolute ShadowElement behind it.
    /// Returns the wrapper — add the wrapper to the parent instead of the card directly.
    /// </summary>
    public static VisualElement WrapWithShadow(VisualElement card, float offsetY = 4f) {
        var wrapper = new VisualElement();
        wrapper.style.position = Position.Relative;

        // Copy flex behaviour from card so layout is unchanged
        wrapper.style.flexGrow  = card.style.flexGrow;
        wrapper.style.flexShrink = card.style.flexShrink;
        wrapper.style.flexBasis = card.style.flexBasis;
        wrapper.style.marginBottom = card.style.marginBottom;
        wrapper.style.marginTop    = card.style.marginTop;
        wrapper.style.marginLeft   = card.style.marginLeft;
        wrapper.style.marginRight  = card.style.marginRight;

        // Reset margins on the card itself to avoid double-spacing
        card.style.marginBottom = 0;
        card.style.marginTop    = 0;

        var shadow = new ShadowElement { OffsetY = offsetY };
        wrapper.Add(shadow);
        wrapper.Add(card);

        return wrapper;
    }

    /// <summary>
    /// Adds an absolute GradientElement as the first child of the card.
    /// The card must have overflow: hidden (or border-radius clip) for the gradient to be masked.
    /// </summary>
    public static void AddGradient(VisualElement card) {
        var gradient = new GradientElement();
        card.Insert(0, gradient);
    }

    /// <summary>Applies the .card--bevel USS class.</summary>
    public static void ApplyBevel(VisualElement card) {
        card.AddToClassList("card--bevel");
    }

    /// <summary>Applies a semantic accent border class.</summary>
    public static void ApplyAccentBorder(VisualElement card, string accentClass = "border--accent") {
        card.AddToClassList(accentClass);
    }

    /// <summary>Removes a semantic accent border class.</summary>
    public static void RemoveAccentBorder(VisualElement card, string accentClass = "border--accent") {
        card.RemoveFromClassList(accentClass);
    }

    // ── Empty states ──

    /// <summary>
    /// Creates a reusable empty-state container. Toggle .empty-state--hidden to show/hide.
    /// Caller adds it to the list container in Initialize(); never during Bind().
    /// </summary>
    public static VisualElement CreateEmptyState(string icon, string message, string actionLabel = null, Action onAction = null) {
        var container = new VisualElement();
        container.AddToClassList("empty-state");

        var iconLabel = new Label(icon);
        iconLabel.AddToClassList("empty-state__icon");
        container.Add(iconLabel);

        var msgLabel = new Label(message);
        msgLabel.AddToClassList("empty-state__message");
        container.Add(msgLabel);

        if (!string.IsNullOrEmpty(actionLabel) && onAction != null) {
            var btn = new Button(onAction) { text = actionLabel };
            btn.AddToClassList("btn-secondary");
            btn.AddToClassList("btn-sm");
            container.Add(btn);
        }

        return container;
    }

    // ── Badges ──

    /// <summary>Creates a new pill/badge Label with the given text and semantic class.</summary>
    public static Label CreateBadge(string text, string semanticClass) {
        var badge = new Label(text);
        badge.AddToClassList("badge");
        if (!string.IsNullOrEmpty(semanticClass)) {
            badge.AddToClassList(semanticClass);
        }
        return badge;
    }

    /// <summary>
    /// Updates an existing badge's text and swaps the semantic class.
    /// Previous semantic class must be passed as <paramref name="oldSemanticClass"/> to be removed.
    /// </summary>
    public static void UpdateBadge(Label badge, string text, string newSemanticClass, string oldSemanticClass = null) {
        badge.text = text;
        if (!string.IsNullOrEmpty(oldSemanticClass)) {
            badge.RemoveFromClassList(oldSemanticClass);
        }
        if (!string.IsNullOrEmpty(newSemanticClass)) {
            badge.AddToClassList(newSemanticClass);
        }
    }
}
