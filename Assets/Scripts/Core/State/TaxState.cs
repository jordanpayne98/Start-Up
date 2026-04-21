using System;
using System.Collections.Generic;

[Serializable]
public class TaxState
{
    public float taxRate;
    public int cycleLengthDays;
    public int firstDueTick;
    public int nextDueTick;
    public long profitSinceLastCycle;
    public int cycleStartTick;
    public bool hasPendingTax;
    public long pendingTaxAmount;
    public long pendingLateFees;
    public int pendingTaxDueTick;
    public int overdueMonthsApplied;
    public int lastReminderDay;
    public List<int> sentReminderDays;

    public static TaxState CreateNew()
    {
        int firstDueTick = TimeState.ToAbsoluteDay(1, 1, 2027) * TimeState.TicksPerDay;
        return new TaxState
        {
            taxRate = 0.30f,
            cycleLengthDays = 365,
            firstDueTick = firstDueTick,
            nextDueTick = firstDueTick,
            cycleStartTick = 0,
            profitSinceLastCycle = 0L,
            hasPendingTax = false,
            pendingTaxAmount = 0L,
            pendingLateFees = 0L,
            pendingTaxDueTick = 0,
            overdueMonthsApplied = 0,
            lastReminderDay = -1,
            sentReminderDays = new List<int>()
        };
    }
}
