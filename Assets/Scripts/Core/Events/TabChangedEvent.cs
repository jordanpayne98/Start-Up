public enum TabType
{
    Overview,
    Employees,
    Contracts,
    Research,
    Finance,
    Settings,
    Inbox,
    HR
}

public class TabChangedEvent : GameEvent
{
    public TabType NewTab { get; }
    public TabType PreviousTab { get; }
    
    public TabChangedEvent(int tick, TabType newTab, TabType previousTab) : base(tick)
    {
        NewTab = newTab;
        PreviousTab = previousTab;
    }
}
