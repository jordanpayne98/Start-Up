// AssignmentContext — Wave 3C
// Describes what a team is working on. Used by Technical Strength and Creativity
// to determine which skills are relevant for meter computation.
// Until Waves 5+ wire product/contract assignment, callers may pass default(AssignmentContext)
// which resolves to AssignmentType.Unassigned and uses generic relevant skills.

public struct AssignmentContext
{
    public AssignmentType Type;
    public ProductCategory? ProductCategory; // Software, Game, Hardware, etc.
    public SkillId[] RelevantSkills;         // From contract requirements or product phase

    public static AssignmentContext Unassigned()
    {
        return new AssignmentContext
        {
            Type            = AssignmentType.Unassigned,
            ProductCategory = null,
            RelevantSkills  = null
        };
    }
}

public enum AssignmentType
{
    Unassigned,
    Contract,
    Product,
    Marketing,
    HR
}
