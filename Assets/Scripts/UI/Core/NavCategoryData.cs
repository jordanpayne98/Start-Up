using System.Collections.Generic;

/// <summary>
/// Data class representing a sidebar category group (e.g. "OVERVIEW", "HR PORTAL").
/// Distinct from the NavCategoryId enum — this is the runtime-mutable display record.
/// </summary>
public class NavCategoryData
{
    public NavCategoryId        Id          { get; set; }
    public string               Label       { get; set; }   // Uppercase display label
    public bool                 IsCollapsed { get; set; }
    public List<NavNodeData>    Children    { get; set; } = new List<NavNodeData>();
}
