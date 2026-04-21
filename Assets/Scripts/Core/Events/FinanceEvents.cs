public class FinancialHealthChangedEvent : GameEvent
{
    public FinancialHealthState OldHealth { get; }
    public FinancialHealthState NewHealth { get; }

    public FinancialHealthChangedEvent(int tick, FinancialHealthState oldHealth, FinancialHealthState newHealth) : base(tick)
    {
        OldHealth = oldHealth;
        NewHealth = newHealth;
    }
}

public class LoanTakenEvent : GameEvent
{
    public int Principal { get; }
    public float InterestRate { get; }
    public int DurationMonths { get; }
    public LoanRiskBand RiskBand { get; }

    public LoanTakenEvent(int tick, int principal, float interestRate, int durationMonths, LoanRiskBand riskBand) : base(tick)
    {
        Principal = principal;
        InterestRate = interestRate;
        DurationMonths = durationMonths;
        RiskBand = riskBand;
    }
}

public class LoanEarlyRepaidEvent : GameEvent
{
    public int AmountPaid { get; }
    public int InterestAvoided { get; }
    public bool FullyRepaid { get; }

    public LoanEarlyRepaidEvent(int tick, int amountPaid, int interestAvoided, bool fullyRepaid) : base(tick)
    {
        AmountPaid = amountPaid;
        InterestAvoided = interestAvoided;
        FullyRepaid = fullyRepaid;
    }
}

public class LoanFullyRepaidEvent : GameEvent
{
    public LoanFullyRepaidEvent(int tick) : base(tick) { }
}
