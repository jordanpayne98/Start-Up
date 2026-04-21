public class TaxReminderEvent : GameEvent
{
    public int DaysUntilDue { get; }
    public long EstimatedTaxOwed { get; }
    public bool IsFinalWarning { get; }

    public TaxReminderEvent(int tick, int daysUntilDue, long estimatedTaxOwed, bool isFinalWarning)
        : base(tick)
    {
        DaysUntilDue = daysUntilDue;
        EstimatedTaxOwed = estimatedTaxOwed;
        IsFinalWarning = isFinalWarning;
    }
}

public class TaxDueEvent : GameEvent
{
    public long TaxOwed { get; }

    public TaxDueEvent(int tick, long taxOwed) : base(tick)
    {
        TaxOwed = taxOwed;
    }
}

public class TaxPaidEvent : GameEvent
{
    public long TaxAmount { get; }
    public long LateFees { get; }
    public long TotalPaid { get; }

    public TaxPaidEvent(int tick, long taxAmount, long lateFees, long totalPaid) : base(tick)
    {
        TaxAmount = taxAmount;
        LateFees = lateFees;
        TotalPaid = totalPaid;
    }
}

public class TaxOverdueEvent : GameEvent
{
    public long TaxOwed { get; }
    public long LateFees { get; }
    public int MonthsOverdue { get; }

    public TaxOverdueEvent(int tick, long taxOwed, long lateFees, int monthsOverdue) : base(tick)
    {
        TaxOwed = taxOwed;
        LateFees = lateFees;
        MonthsOverdue = monthsOverdue;
    }
}

public class TaxBankruptcyEvent : GameEvent
{
    public long UnpaidAmount { get; }

    public TaxBankruptcyEvent(int tick, long unpaidAmount) : base(tick)
    {
        UnpaidAmount = unpaidAmount;
    }
}
