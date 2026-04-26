using System;
using System.Collections.Generic;

[Serializable]
public struct FatigueData
{
    public float Energy;
    public int CrunchDaysActive;
    public int RecentCrunchDays;
    public int ConsecutiveLowEnergyDays;
    public bool BurnoutPressure;

    public FatigueData(float energy) {
        Energy = energy;
        CrunchDaysActive = 0;
        RecentCrunchDays = 0;
        ConsecutiveLowEnergyDays = 0;
        BurnoutPressure = false;
    }
}

[Serializable]
public class FatigueState
{
    public Dictionary<EmployeeId, FatigueData> employeeFatigue;

    public static FatigueState CreateNew() {
        return new FatigueState
        {
            employeeFatigue = new Dictionary<EmployeeId, FatigueData>()
        };
    }
}
