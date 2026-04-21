using UnityEngine.UIElements;

/// <summary>
/// Stores tooltip metadata on a VisualElement via userData.
/// Does NOT use element.tooltip to avoid triggering Unity's built-in tooltip system.
/// </summary>
public sealed class TooltipInfo {
    public string SimpleText;
    public string RegistryKey;
    public TooltipData? DirectData;
    public bool IsRich;
}

public static class TooltipExtensions {
    public static void SetSimpleTooltip(this VisualElement element, string text, TooltipService service) {
        if (element == null || service == null) return;
        element.tooltip = null;
        element.userData = new TooltipInfo { SimpleText = text, IsRich = false };
        service.Register(element);
    }

    public static void SetRichTooltip(this VisualElement element, string registryKey, TooltipService service) {
        if (element == null || service == null) return;
        element.tooltip = null;
        element.userData = new TooltipInfo { RegistryKey = registryKey, IsRich = true };
        service.Register(element);
    }

    public static void SetRichTooltip(this VisualElement element, TooltipData data, TooltipService service) {
        if (element == null || service == null) return;
        element.tooltip = null;
        element.userData = new TooltipInfo { DirectData = data, IsRich = true };
        service.Register(element);
    }

    public static void ClearTooltip(this VisualElement element, TooltipService service) {
        if (element == null) return;
        service?.Unregister(element);
        element.tooltip = null;
        element.userData = null;
    }
}
