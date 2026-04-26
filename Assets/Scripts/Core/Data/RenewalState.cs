using System;

public enum RenewalPhase
{
    Active,
    WindowOpen,
    Renewed,
    Expiring,
    Departed
}

[Serializable]
public struct RenewalState
{
    public RenewalPhase Phase;
    public int WindowOpenTick;
    public int ExpiryTick;
    public int StrikeCount;
    public bool HasChangeRequest;
    public EmploymentType RequestedType;
    public ContractLengthOption RequestedLength;
    public bool RequestedTypeChange;
    public bool RequestedLengthChange;

    private const int UrgentThresholdDays = 14;

    public bool IsWindowOpen => Phase == RenewalPhase.WindowOpen;

    public bool IsUrgent(int currentTick)
    {
        int remaining = ExpiryTick - currentTick;
        return remaining < UrgentThresholdDays * TimeState.TicksPerDay;
    }
}
