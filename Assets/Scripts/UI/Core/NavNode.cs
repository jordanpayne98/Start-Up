using System.Collections.Generic;

public class NavNode
{
    public string Id;
    public string Label;
    public string Icon;
    public string Hotkey;
    public NavNode Parent;
    public List<NavNode> Children;
    public ScreenId? ScreenId;
    public bool IsExpanded;
    public bool IsLeaf => Children == null || Children.Count == 0;
    public int Depth => Parent == null ? 0 : Parent.Depth + 1;

    public NavNode AddChild(NavNode child) {
        if (Children == null) Children = new List<NavNode>();
        child.Parent = this;
        Children.Add(child);
        return this;
    }
}
