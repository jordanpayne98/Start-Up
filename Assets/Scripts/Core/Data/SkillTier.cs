// Skill-based tier derived from a candidate's RoleRelevantAverage.
// Replaces the old SeniorityLevel for HR search filtering and salary scaling.
public enum SkillTier {
    Apprentice = 0,  // avg < 40
    Competent = 1,   // avg 40–64
    Expert = 2,      // avg 65–84
    Master = 3       // avg >= 85
}
