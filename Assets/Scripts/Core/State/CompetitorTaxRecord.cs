using System;

[Serializable]
public struct CompetitorTaxRecord
{
    public long profitSinceLastCycle;
    public bool hasPendingTax;
    public long pendingTaxAmount;
    public long pendingLateFees;
    public int pendingTaxDueTick;
    public int overdueMonthsApplied;
    public int plannedPaymentTick;
}
