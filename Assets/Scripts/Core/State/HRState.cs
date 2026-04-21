using System;
using System.Collections.Generic;

[Serializable]
public class ActiveHRSearch
{
    public HRSearchId searchId;
    public EmployeeRole targetRole;
    public int startTick;
    public int completionTick;
    public int cost;
    public TeamId assignedTeamId;
    public int searchCount;                  // how many candidates to find in one task (1–3)
    public int retryCount;                   // number of times this search has been re-queued on failure

    // CA/PA criteria (0 = no constraint)
    public int minCA;
    public int maxCA;
    public int minPAStars;
    public int maxPAStars;
    public bool[] desiredSkills;             // length = SkillTypeHelper.SkillTypeCount; null = no preference
}

[Serializable]
public class HRState
{
    public List<ActiveHRSearch> activeSearches;
    public int nextSearchId;
    public int totalSearchesCompleted;
    public int totalSearchesFailed;

    /// <summary>Maps TeamId.Value to the candidateId currently being interviewed by that team.</summary>
    public Dictionary<int, int> activeInterviewAssignments;

    public static HRState CreateNew()
    {
        return new HRState
        {
            activeSearches = new List<ActiveHRSearch>(),
            nextSearchId = 1,
            totalSearchesCompleted = 0,
            totalSearchesFailed = 0,
            activeInterviewAssignments = new Dictionary<int, int>()
        };
    }
}
