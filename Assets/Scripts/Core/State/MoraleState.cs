using System;
using System.Collections.Generic;

[Serializable]
public struct MoraleData
{
    public float currentMorale;
    public bool idleAlertSent;
    public int crunchDaysActive;
    public int recentCrunchDays;
    public int consecutiveIdleDays;

    public MoraleData(float startingMorale) {
        currentMorale = startingMorale;
        idleAlertSent = false;
        crunchDaysActive = 0;
        recentCrunchDays = 0;
        consecutiveIdleDays = 0;
    }
}

[Serializable]
public class MoraleState
{
    public Dictionary<EmployeeId, MoraleData> employeeMorale;

    public static MoraleState CreateNew() {
        return new MoraleState
        {
            employeeMorale = new Dictionary<EmployeeId, MoraleData>()
        };
    }
}
