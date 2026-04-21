using System;
using UnityEngine.UIElements;

public static class ClickableLabel
{
    public static Label Create(string text, Action onClick) {
        var label = new Label(text);
        label.AddToClassList("clickable-label");
        label.RegisterCallback<ClickEvent>(_ => onClick?.Invoke());
        return label;
    }

    public static Label Create(string text, string additionalClass, Action onClick) {
        var label = Create(text, onClick);
        label.AddToClassList(additionalClass);
        return label;
    }
}
