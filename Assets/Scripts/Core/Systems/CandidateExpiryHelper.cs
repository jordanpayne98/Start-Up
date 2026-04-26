public static class CandidateExpiryHelper
{
    private const int HRSourcedExpiryDays = 20;
    private const float UrgencyMediumThreshold = 0.50f;
    private const float UrgencyHighThreshold   = 0.25f;

    public static void AssignExpiryTicks(EmployeeState employeeState, IRng rng, int currentTick,
        TuningConfig tuning = null)
    {
        int hrExpiry = tuning != null ? tuning.CandidateHRExpiryDays : HRSourcedExpiryDays;

        int count = employeeState.availableCandidates.Count;
        for (int i = 0; i < count; i++)
        {
            var candidate = employeeState.availableCandidates[i];
            if (!candidate.IsTargeted)
            {
                candidate.ExpiryTick = 0;
                employeeState.availableCandidates[i] = candidate;
                continue;
            }

            int windowTicks = hrExpiry * TimeState.TicksPerDay;
            if (candidate.ExpiryTick <= 0 || candidate.ExpiryTick < currentTick)
            {
                int varianceTicks = rng.Range(0, TimeState.TicksPerDay);
                candidate.ExpiryTick = currentTick + windowTicks + varianceTicks;
                employeeState.availableCandidates[i] = candidate;
            }
        }
    }

    public static void TickExpiryTimers(EmployeeState employeeState, InterviewSystem interviewSystem,
        NegotiationSystem negotiationSystem, int currentTick, System.Collections.Generic.List<int> expiredIds)
    {
        int count = employeeState.availableCandidates.Count;
        for (int i = 0; i < count; i++)
        {
            var candidate = employeeState.availableCandidates[i];
            if (!candidate.IsTargeted) continue;
            if (candidate.ExpiryTick <= 0) continue;

            bool isPaused = false;
            if (interviewSystem != null)
            {
                bool inInterview = interviewSystem.IsInterviewInProgress(candidate.CandidateId);
                bool hasKnowledge = interviewSystem.GetKnowledgeLevel(candidate.CandidateId) > 0f;
                if (inInterview || hasKnowledge) isPaused = true;
            }
            if (!isPaused && negotiationSystem != null && negotiationSystem.HasActiveNegotiation(candidate.CandidateId))
                isPaused = true;
            if (!isPaused && candidate.IsTargeted)
                isPaused = candidate.ExpiryTick == int.MaxValue;

            candidate.IsTimerPaused = isPaused;
            if (isPaused)
            {
                if (candidate.ExpiryTick != int.MaxValue)
                    candidate.ExpiryTick += TimeState.TicksPerDay;
            }
            else if (currentTick >= candidate.ExpiryTick)
            {
                expiredIds.Add(candidate.CandidateId);
            }
            employeeState.availableCandidates[i] = candidate;
        }

        int expiredCount = expiredIds.Count;
        for (int e = 0; e < expiredCount; e++)
        {
            int expiredId = expiredIds[e];
            for (int i = employeeState.availableCandidates.Count - 1; i >= 0; i--)
            {
                if (employeeState.availableCandidates[i].CandidateId == expiredId)
                {
                    employeeState.availableCandidates.RemoveAt(i);
                    break;
                }
            }
        }
    }

    public static string GetUrgencyDisplay(EmployeeState employeeState, int candidateId, int currentTick,
        TuningConfig tuning = null)
    {
        CandidateData candidate = FindCandidate(employeeState, candidateId);
        if (candidate == null) return "";
        if (!candidate.IsTargeted || candidate.ExpiryTick <= 0) return "";
        if (candidate.ExpiryTick == int.MaxValue) return "";

        float percent = GetTimeRemainingPercent(employeeState, candidateId, currentTick, tuning);
        float medThreshold = tuning != null ? tuning.CandidateUrgencyMediumThreshold : UrgencyMediumThreshold;
        float highThreshold = tuning != null ? tuning.CandidateUrgencyHighThreshold  : UrgencyHighThreshold;
        if (percent < 0f)            return "Plenty of time";
        if (percent > medThreshold)  return "Plenty of time";
        if (percent > highThreshold) return "Considering offers";
        return "Likely leaving soon";
    }

    public static float GetTimeRemainingPercent(EmployeeState employeeState, int candidateId, int currentTick,
        TuningConfig tuning = null)
    {
        CandidateData candidate = FindCandidate(employeeState, candidateId);
        if (candidate == null) return -1f;
        if (!candidate.IsTargeted || candidate.ExpiryTick <= 0) return 1f;
        if (candidate.ExpiryTick == int.MaxValue) return 1f;

        int hrExpiry = tuning != null ? tuning.CandidateHRExpiryDays : HRSourcedExpiryDays;
        int totalTicks = hrExpiry * TimeState.TicksPerDay;
        if (totalTicks <= 0) return 1f;

        int remaining = candidate.ExpiryTick - currentTick;
        if (remaining < 0) remaining = 0;

        return (float)remaining / totalTicks;
    }

    private static CandidateData FindCandidate(EmployeeState employeeState, int candidateId)
    {
        int count = employeeState.availableCandidates.Count;
        for (int i = 0; i < count; i++)
        {
            if (employeeState.availableCandidates[i].CandidateId == candidateId)
                return employeeState.availableCandidates[i];
        }
        return null;
    }
}
