using System;
using System.Collections.Generic;

[Serializable]
public struct ActiveInterview
{
    public int candidateId;
    public int startTick;
    public int completionTick;
    public int halfwayTick;
    public bool halfwayFired;
    public bool isComplete;
    public TeamId assignedTeamId;
    public HiringMode mode;
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
