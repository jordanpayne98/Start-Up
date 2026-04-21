public class StartHRSearchCommand : ICommand
{
    public int Tick { get; set; }
    public TeamId TeamId;
    public EmployeeRole TargetRole;

    // CA/PA range criteria (Any = 0 min, 200 max / 1 star min, 5 star max)
    public int MinCA;       // 0 = no minimum
    public int MaxCA;       // 0 = no maximum (treated as 200)
    public int MinPAStars;  // 0 or 1 = any
    public int MaxPAStars;  // 0 or 5 = any

    // Desired skills — only skills with a true entry are preferred; others may still be present
    // Index matches SkillType enum. null = no preference.
    public bool[] DesiredSkills; // length = SkillTypeHelper.SkillTypeCount

    public int SearchCount; // how many candidates to find in one task (1–3), default 1
}
