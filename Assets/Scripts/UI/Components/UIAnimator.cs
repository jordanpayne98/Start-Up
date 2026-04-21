using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Static class owning all DOTween calls for UI Toolkit elements.
/// All methods are fire-and-forget with optional completion callbacks.
/// </summary>
public static class UIAnimator
{
    // ── Shell ──

    /// <summary>Fades and slides an element in from below. Kills any existing tween on the element first.</summary>
    public static void FadeSlideIn(VisualElement el, float duration = 0.22f, float slideY = 12f) {
        if (el == null) return;
        DOTween.Kill(el);

        el.style.opacity   = 0f;
        el.style.translate = new StyleTranslate(new Translate(new Length(0f), new Length(slideY)));

        float currentY = slideY;
        var seq = DOTween.Sequence().SetTarget(el);
        seq.Append(DOTween.To(() => el.resolvedStyle.opacity, v => el.style.opacity = v, 1f, duration));
        seq.Join(DOTween.To(() => currentY, v => {
            currentY = v;
            el.style.translate = new StyleTranslate(new Translate(new Length(0f), new Length(v)));
        }, 0f, duration));
    }

    /// <summary>Fades and slides an element out. Invokes onComplete when finished.</summary>
    public static void FadeSlideOut(VisualElement el, float duration = 0.18f, Action onComplete = null) {
        if (el == null) { onComplete?.Invoke(); return; }
        DOTween.Kill(el);

        float currentY = 0f;
        var seq = DOTween.Sequence().SetTarget(el);
        seq.Append(DOTween.To(() => el.resolvedStyle.opacity, v => el.style.opacity = v, 0f, duration));
        seq.Join(DOTween.To(() => currentY, v => {
            currentY = v;
            el.style.translate = new StyleTranslate(new Translate(new Length(0f), new Length(v)));
        }, 12f, duration));
        seq.OnComplete(() => onComplete?.Invoke());
    }

    // ── Modal ──

    /// <summary>Animates the modal backdrop and content panel in.</summary>
    public static void ModalOpen(VisualElement backdrop, VisualElement content) {
        if (backdrop != null) {
            DOTween.Kill(backdrop);
            backdrop.style.opacity = 0f;
            DOTween.To(
                () => backdrop.resolvedStyle.opacity,
                v  => backdrop.style.opacity = v,
                1f, 0.18f).SetTarget(backdrop);
        }

        if (content != null) {
            DOTween.Kill(content);
            content.style.opacity = 0f;
            content.style.scale   = new StyleScale(new Scale(new Vector2(0.93f, 0.93f)));

            float currentScale = 0.93f;
            var seq = DOTween.Sequence().SetTarget(content);
            seq.Append(DOTween.To(() => content.resolvedStyle.opacity, v => content.style.opacity = v, 1f, 0.22f));
            seq.Join(DOTween.To(() => currentScale, v => {
                currentScale = v;
                content.style.scale = new StyleScale(new Scale(new Vector2(v, v)));
            }, 1f, 0.22f).SetEase(Ease.OutBack));
        }
    }

    /// <summary>Animates the modal content out, then invokes onComplete.</summary>
    public static void ModalClose(VisualElement backdrop, VisualElement content, Action onComplete) {
        if (content == null) { onComplete?.Invoke(); return; }
        DOTween.Kill(content);

        float currentScale = 1f;
        var seq = DOTween.Sequence().SetTarget(content);
        seq.Append(DOTween.To(() => content.resolvedStyle.opacity, v => content.style.opacity = v, 0f, 0.15f));
        seq.Join(DOTween.To(() => currentScale, v => {
            currentScale = v;
            content.style.scale = new StyleScale(new Scale(new Vector2(v, v)));
        }, 0.93f, 0.15f));

        if (backdrop != null) {
            seq.Join(DOTween.To(() => backdrop.resolvedStyle.opacity, v => backdrop.style.opacity = v, 0f, 0.15f).SetTarget(backdrop));
        }

        seq.OnComplete(() => onComplete?.Invoke());
    }

    // ── Toast ──

    /// <summary>Slides a toast in from the right.</summary>
    public static void ToastIn(VisualElement toast) {
        if (toast == null) return;
        DOTween.Kill(toast);

        toast.style.opacity   = 0f;
        toast.style.translate = new StyleTranslate(new Translate(new Length(40f), new Length(0f)));

        float currentX = 40f;
        var seq = DOTween.Sequence().SetTarget(toast);
        seq.Append(DOTween.To(() => toast.resolvedStyle.opacity, v => toast.style.opacity = v, 1f, 0.2f).SetEase(Ease.OutCubic));
        seq.Join(DOTween.To(() => currentX, v => {
            currentX = v;
            toast.style.translate = new StyleTranslate(new Translate(new Length(v), new Length(0f)));
        }, 0f, 0.2f).SetEase(Ease.OutCubic));
    }

    /// <summary>Slides a toast out to the right, then invokes onComplete.</summary>
    public static void ToastOut(VisualElement toast, Action onComplete) {
        if (toast == null) { onComplete?.Invoke(); return; }
        DOTween.Kill(toast);

        float currentX = 0f;
        var seq = DOTween.Sequence().SetTarget(toast);
        seq.Append(DOTween.To(() => toast.resolvedStyle.opacity, v => toast.style.opacity = v, 0f, 0.2f));
        seq.Join(DOTween.To(() => currentX, v => {
            currentX = v;
            toast.style.translate = new StyleTranslate(new Translate(new Length(v), new Length(0f)));
        }, 40f, 0.2f));
        seq.OnComplete(() => onComplete?.Invoke());
    }

    // ── Money ──

    /// <summary>Flashes an element's color between base and target, then back.</summary>
    public static void MoneyFlash(VisualElement el, Color targetColor, Color baseColor, float duration = 0.4f) {
        if (el == null) return;
        DOTween.Kill(el, true);

        DOTween.To(
            () => 0f,
            t => {
                var c = Color.Lerp(baseColor, targetColor, t < 0.5f ? t * 2f : (1f - t) * 2f);
                el.style.color = new StyleColor(c);
            },
            1f, duration).SetTarget(el);
    }

    /// <summary>Animates a label's text as a rolling counter from fromValue to toValue.</summary>
    public static void CounterRollUp(Label label, float fromValue, float toValue, string format = "{0:N0}", float duration = 0.4f) {
        if (label == null) return;
        DOTween.Kill(label);

        float current = fromValue;
        DOTween.To(
            () => current,
            v => {
                current = v;
                label.text = string.Format(format, Mathf.RoundToInt(v));
            },
            toValue, duration).SetTarget(label);
    }

    // ── Progress ──

    /// <summary>Tweens the width of a progress fill element from fromPercent to toPercent (0–100 range).</summary>
    public static Tweener ProgressFill(VisualElement fill, float fromPercent, float toPercent, float duration = 0.35f) {
        if (fill == null) return null;
        DOTween.Kill(fill);

        float current = fromPercent;
        return DOTween.To(
            () => current,
            v => {
                current = v;
                fill.style.width = Length.Percent(v);
            },
            toPercent, duration).SetEase(Ease.OutCubic).SetTarget(fill);
    }

    // ── List ──

    /// <summary>Staggers a list of elements in with opacity/slide animations.</summary>
    public static void StaggerIn(List<VisualElement> items, float itemDuration = 0.18f, float staggerDelay = 0.03f) {
        if (items == null) return;
        int count = items.Count;
        for (int i = 0; i < count; i++) {
            var capturedEl = items[i];
            if (capturedEl == null) continue;
            DOTween.Kill(capturedEl);

            capturedEl.style.opacity   = 0f;
            capturedEl.style.translate = new StyleTranslate(new Translate(new Length(0f), new Length(8f)));

            float currentY = 8f;
            float delay    = i * staggerDelay;
            var seq = DOTween.Sequence().SetTarget(capturedEl).SetDelay(delay);
            seq.Append(DOTween.To(() => capturedEl.resolvedStyle.opacity, v => capturedEl.style.opacity = v, 1f, itemDuration));
            seq.Join(DOTween.To(() => currentY, v => {
                currentY = v;
                capturedEl.style.translate = new StyleTranslate(new Translate(new Length(0f), new Length(v)));
            }, 0f, itemDuration));
        }
    }

    // ── Microfeedback ──

    /// <summary>Pops a badge in with scale overshoot (OutBack).</summary>
    public static void BadgePopIn(VisualElement badge) {
        if (badge == null) return;
        DOTween.Kill(badge);

        badge.style.scale = new StyleScale(new Scale(new Vector2(0f, 0f)));
        float currentScale = 0f;
        DOTween.To(
            () => currentScale,
            v => {
                currentScale = v;
                badge.style.scale = new StyleScale(new Scale(new Vector2(v, v)));
            },
            1f, 0.2f).SetEase(Ease.OutBack).SetTarget(badge);
    }

    /// <summary>Pulses an element with a brief scale throb. Guards against stacking via userData flag.</summary>
    public static void WarningPulse(VisualElement el) {
        if (el == null) return;
        if (el.userData is bool pulsing && pulsing) return;

        el.userData = true;
        DOTween.Kill(el);

        float currentScale = 1f;
        var seq = DOTween.Sequence().SetTarget(el).SetLoops(2);
        seq.Append(DOTween.To(() => currentScale, v => {
            currentScale = v;
            el.style.scale = new StyleScale(new Scale(new Vector2(v, v)));
        }, 1.04f, 0.2f));
        seq.Append(DOTween.To(() => currentScale, v => {
            currentScale = v;
            el.style.scale = new StyleScale(new Scale(new Vector2(v, v)));
        }, 1f, 0.2f));
        seq.OnComplete(() => el.userData = false);
    }

    /// <summary>Slides a detail panel in or out from/to the right edge.</summary>
    public static void DetailPanelSlide(VisualElement panel, bool isEntering) {
        if (panel == null) return;
        DOTween.Kill(panel);

        if (isEntering) {
            panel.style.opacity   = 0f;
            panel.style.translate = new StyleTranslate(new Translate(new Length(20f), new Length(0f)));

            float currentX = 20f;
            var seq = DOTween.Sequence().SetTarget(panel);
            seq.Append(DOTween.To(() => panel.resolvedStyle.opacity, v => panel.style.opacity = v, 1f, 0.18f));
            seq.Join(DOTween.To(() => currentX, v => {
                currentX = v;
                panel.style.translate = new StyleTranslate(new Translate(new Length(v), new Length(0f)));
            }, 0f, 0.18f));
        } else {
            float currentX = 0f;
            var seq = DOTween.Sequence().SetTarget(panel);
            seq.Append(DOTween.To(() => panel.resolvedStyle.opacity, v => panel.style.opacity = v, 0f, 0.14f));
            seq.Join(DOTween.To(() => currentX, v => {
                currentX = v;
                panel.style.translate = new StyleTranslate(new Translate(new Length(v), new Length(0f)));
            }, 20f, 0.14f));
        }
    }

    /// <summary>Flashes all four border colors then returns them to their original resolved color.</summary>
    public static void BorderFlash(VisualElement el, Color flashColor, float duration = 0.3f) {
        if (el == null) return;
        DOTween.Kill(el, true);

        Color orig = el.resolvedStyle.borderTopColor;
        DOTween.To(
            () => 0f,
            t => {
                var c = Color.Lerp(orig, flashColor, t < 0.5f ? t * 2f : (1f - t) * 2f);
                var sc = new StyleColor(c);
                el.style.borderTopColor    = sc;
                el.style.borderBottomColor = sc;
                el.style.borderLeftColor   = sc;
                el.style.borderRightColor  = sc;
            },
            1f, duration).SetTarget(el);
    }

    /// <summary>Briefly brightens a stat tile's background (glow effect).</summary>
    public static void StatTileGlow(VisualElement tile) {
        if (tile == null) return;
        DOTween.Kill(tile, true);

        Color base_ = tile.resolvedStyle.backgroundColor;
        Color glow  = new Color(
            Mathf.Clamp01(base_.r + 0.1f),
            Mathf.Clamp01(base_.g + 0.1f),
            Mathf.Clamp01(base_.b + 0.1f),
            base_.a);

        DOTween.To(
            () => 0f,
            t => {
                var c  = Color.Lerp(base_, glow, t < 0.5f ? t * 2f : (1f - t) * 2f);
                tile.style.backgroundColor = new StyleColor(c);
            },
            1f, 0.35f).SetTarget(tile);
    }
}
