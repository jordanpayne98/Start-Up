using System;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Implements ITooltipProvider for the shell tooltip-layer.
/// Wraps the hover-based TooltipService and exposes imperative Show/Hide.
/// Plain C# class; initialized by WindowManager and passed via UIServices.
/// </summary>
public class ShellTooltipProvider : ITooltipProvider
{
    // ── Wrapped service ───────────────────────────────────────────────────

    private readonly TooltipService _tooltipService;

    // ── ITooltipProvider ──────────────────────────────────────────────────

    public TooltipService TooltipService => _tooltipService;

    // ── Initialization ────────────────────────────────────────────────────

    public ShellTooltipProvider(TooltipService tooltipService)
    {
        _tooltipService = tooltipService ?? throw new ArgumentNullException(nameof(tooltipService));
    }

    // ── ITooltipProvider members ──────────────────────────────────────────

    /// <summary>
    /// Show a tooltip anchored to the given VisualElement.
    /// Calculates the anchor world bounds and positions the tooltip below it.
    /// </summary>
    public void Show(TooltipData data, VisualElement anchor)
    {
        if (_tooltipService == null || anchor == null) return;

        var bounds = anchor.worldBound;
        var position = new Vector2(bounds.center.x, bounds.yMin);
        _tooltipService.Show(data, position);
    }

    /// <summary>Hide the active tooltip.</summary>
    public void Hide()
    {
        _tooltipService?.Hide();
    }
}
