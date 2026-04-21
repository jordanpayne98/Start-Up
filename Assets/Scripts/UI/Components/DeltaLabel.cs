using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Pooled, DOTween-driven floating delta label (+$2,500, -$750).
/// Max 2 active at once; overflow is queued and dequeued on exit.
/// Call Dispose() in OnDestroy to kill all active tweens.
/// </summary>
public static class DeltaLabel
{
    private const int MaxActive = 2;

    private static readonly List<Label> _pool    = new List<Label>();
    private static readonly List<Label> _active  = new List<Label>();
    private static readonly Queue<(string text, bool positive)> _queue =
        new Queue<(string, bool)>();

    public static void Show(VisualElement container, string text, bool isPositive) {
        if (container == null) return;

        if (_active.Count >= MaxActive) {
            _queue.Enqueue((text, isPositive));
            return;
        }

        var label = Depool(container, text, isPositive);
        Animate(container, label);
    }

    public static void Dispose() {
        int activeCount = _active.Count;
        for (int i = 0; i < activeCount; i++) {
            DOTween.Kill(_active[i]);
        }
        _active.Clear();
        _pool.Clear();
        _queue.Clear();
    }

    // ── Private ──

    private static Label Depool(VisualElement container, string text, bool isPositive) {
        Label label;
        if (_pool.Count > 0) {
            label = _pool[_pool.Count - 1];
            _pool.RemoveAt(_pool.Count - 1);
        } else {
            label = new Label();
            label.AddToClassList("delta-label");
        }

        label.text = text;

        // Color: green for positive, red for negative
        if (isPositive) {
            label.style.color = new StyleColor(new Color(0.32f, 0.72f, 0.53f, 1f)); // accent-success
        } else {
            label.style.color = new StyleColor(new Color(0.90f, 0.22f, 0.27f, 1f)); // accent-danger
        }

        label.style.opacity = 0f;
        label.style.translate = new StyleTranslate(new Translate(0, 0, 0));

        container.Add(label);
        _active.Add(label);
        return label;
    }

    private static void Animate(VisualElement container, Label label) {
        DOTween.Kill(label);

        var seq = DOTween.Sequence();

        // Enter: opacity 0→1 + translateY 0→-6, 0.12s
        float currentY = 0f;
        seq.Append(DOTween.To(() => label.resolvedStyle.opacity, v => label.style.opacity = v, 1f, 0.12f));
        seq.Join(DOTween.To(() => currentY, v => {
            currentY = v;
            label.style.translate = new StyleTranslate(new Translate(new Length(0f), new Length(v)));
        }, -6f, 0.12f));

        // Hold
        seq.AppendInterval(0.4f);

        // Exit: opacity 1→0 + translateY -6→-12, 0.18s
        seq.Append(DOTween.To(() => label.resolvedStyle.opacity, v => label.style.opacity = v, 0f, 0.18f));
        seq.Join(DOTween.To(() => currentY, v => {
            currentY = v;
            label.style.translate = new StyleTranslate(new Translate(new Length(0f), new Length(v)));
        }, -12f, 0.18f));

        seq.OnComplete(() => ReturnToPool(container, label));
        seq.SetTarget(label);
        seq.Play();
    }

    private static void ReturnToPool(VisualElement container, Label label) {
        if (container != null && label.parent == container) {
            container.Remove(label);
        }
        _active.Remove(label);
        _pool.Add(label);

        // Dequeue next if any pending
        if (_queue.Count > 0 && container != null) {
            var (text, positive) = _queue.Dequeue();
            var next = Depool(container, text, positive);
            Animate(container, next);
        }
    }
}
