using System;

public enum TooltipRowStyle { Normal, Header, Unlocked, Locked }

[Serializable]
public struct TooltipData {
    public string Title;
    [UnityEngine.TextArea(1, 3)]
    public string Body;
    public TooltipStatRow[] Stats;
}

[Serializable]
public struct TooltipStatRow {
    public string Label;
    public string Value;
    public TooltipRowStyle Style;
}
