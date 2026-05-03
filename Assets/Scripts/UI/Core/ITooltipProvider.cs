using UnityEngine.UIElements;

public interface ITooltipProvider
{
    TooltipService TooltipService { get; }
    void Show(TooltipData data, VisualElement anchor);
    void Hide();
}
