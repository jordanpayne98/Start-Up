/// <summary>
/// Data class representing a single sidebar navigation item leaf.
/// Distinct from the tree-oriented NavNode — this is the flat, display-ready record.
/// </summary>
public class NavNodeData
{
    public ScreenId ScreenId   { get; set; }
    public string   Label      { get; set; }     // Display label
    public string   IconClass  { get; set; }     // USS class for icon glyph (may be empty)
    public int      BadgeCount { get; set; }     // 0 = no badge, >0 = show count
    public bool     IsActive   { get; set; }     // true when this is the current screen
}
