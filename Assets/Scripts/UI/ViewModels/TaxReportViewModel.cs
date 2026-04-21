public class TaxReportViewModel : IViewModel
{
    public string TaxRate { get; private set; }
    public string AccumulatedProfit { get; private set; }
    public string EstimatedTaxOwed { get; private set; }
    public string DueDate { get; private set; }
    public string DaysUntilDue { get; private set; }
    public string PendingTaxAmount { get; private set; }
    public string PendingLateFees { get; private set; }
    public string TotalOwed { get; private set; }
    public string OverdueStatus { get; private set; }
    public bool ShowPayButton { get; private set; }
    public bool ShowOverdueSection { get; private set; }
    public bool ShowBankruptcyWarning { get; private set; }
    public string NextCycleEstimate { get; private set; }

    public void Refresh(IReadOnlyGameState state)
    {
        var snapshot = state as GameStateSnapshot;
        if (snapshot == null)
        {
            SetDefaults();
            return;
        }

        float taxRate = snapshot.TaxRate;
        TaxRate = ((int)(taxRate * 100f)) + "%";

        long profit = snapshot.TaxAccumulatedProfit;
        AccumulatedProfit = UIFormatting.FormatMoney(profit);
        EstimatedTaxOwed = UIFormatting.FormatMoney(snapshot.TaxEstimatedOwed);

        int dueTick = snapshot.TaxNextDueTick;
        int currentTick = snapshot.CurrentTick;
        int dueDayAbs = dueTick / TimeState.TicksPerDay;
        int dueYear = TimeState.GetYear(dueDayAbs);
        int dueMonth = TimeState.GetMonth(dueDayAbs);
        int dueDayOfMonth = TimeState.GetDayOfMonth(dueDayAbs);
        DueDate = UIFormatting.FormatDate(dueDayOfMonth, dueMonth, dueYear);

        int days = snapshot.TaxDaysUntilDue;
        DaysUntilDue = days > 0 ? days + " days" : "OVERDUE";

        bool hasPending = snapshot.TaxHasPending;
        long pendingTax = snapshot.TaxPendingAmount;
        long lateFees = snapshot.TaxPendingLateFees;
        long total = snapshot.TaxTotalPending;

        PendingTaxAmount = UIFormatting.FormatMoney(pendingTax);
        PendingLateFees = UIFormatting.FormatMoney(lateFees);
        TotalOwed = UIFormatting.FormatMoney(total);

        int overdueMonths = snapshot.TaxOverdueMonths;
        if (!hasPending)
            OverdueStatus = "On Time";
        else if (overdueMonths == 0)
            OverdueStatus = "Due Now";
        else if (overdueMonths == 1)
            OverdueStatus = "1 Month Overdue";
        else if (overdueMonths == 2)
            OverdueStatus = "2 Months Overdue";
        else
            OverdueStatus = "3 Months Overdue";

        ShowPayButton = hasPending;
        ShowOverdueSection = hasPending;
        ShowBankruptcyWarning = hasPending && days <= 30;
        NextCycleEstimate = UIFormatting.FormatMoney((long)(profit * taxRate));
    }

    private void SetDefaults()
    {
        TaxRate = "30%";
        AccumulatedProfit = "$0";
        EstimatedTaxOwed = "$0";
        DueDate = "--";
        DaysUntilDue = "--";
        PendingTaxAmount = "$0";
        PendingLateFees = "$0";
        TotalOwed = "$0";
        OverdueStatus = "On Time";
        ShowPayButton = false;
        ShowOverdueSection = false;
        ShowBankruptcyWarning = false;
        NextCycleEstimate = "$0";
    }
}
