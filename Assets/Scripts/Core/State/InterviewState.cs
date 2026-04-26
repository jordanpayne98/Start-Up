using System;
using System.Collections.Generic;

[Serializable]
public struct ActiveInterview
{
    public int candidateId;
    public int startTick;
    public TeamId assignedTeamId;
    public HiringMode mode;

    // Knowledge-based progression (replaces completionTick/halfwayTick/halfwayFired/isComplete)
    public float knowledgeLevel;         // 0 to 100
    public int lastRevealThreshold;      // 0/20/40/60/80/100 — highest threshold crossed

    // HR lead cached at interview start
    public EmployeeId hrLeadId;
    public int hrLeadSkill;

    // Deterministic noise offsets computed at start, one per skill (9 skills)
    public int skillNoise0;
    public int skillNoise1;
    public int skillNoise2;
    public int skillNoise3;
    public int skillNoise4;
    public int skillNoise5;
    public int skillNoise6;
    public int skillNoise7;
    public int skillNoise8;

    // Star estimate noise
    public int abilityStarNoise;
    public int potentialStarNoise;

    // Tick when knowledge reached 100 (used for follow-up idle timer)
    public int completedTick;

    public int GetSkillNoise(int index)
    {
        switch (index)
        {
            case 0: return skillNoise0;
            case 1: return skillNoise1;
            case 2: return skillNoise2;
            case 3: return skillNoise3;
            case 4: return skillNoise4;
            case 5: return skillNoise5;
            case 6: return skillNoise6;
            case 7: return skillNoise7;
            case 8: return skillNoise8;
            default: return 0;
        }
    }

    public void SetSkillNoise(int index, int value)
    {
        switch (index)
        {
            case 0: skillNoise0 = value; break;
            case 1: skillNoise1 = value; break;
            case 2: skillNoise2 = value; break;
            case 3: skillNoise3 = value; break;
            case 4: skillNoise4 = value; break;
            case 5: skillNoise5 = value; break;
            case 6: skillNoise6 = value; break;
            case 7: skillNoise7 = value; break;
            case 8: skillNoise8 = value; break;
        }
    }
}

[Serializable]
public class InterviewState
{
    public Dictionary<int, ActiveInterview> activeInterviews;
    public int totalInterviewsCompleted;
    public int totalInterviewCost;

    public static InterviewState CreateNew()
    {
        return new InterviewState
        {
            activeInterviews = new Dictionary<int, ActiveInterview>(),
            totalInterviewsCompleted = 0,
            totalInterviewCost = 0
        };
    }
}
