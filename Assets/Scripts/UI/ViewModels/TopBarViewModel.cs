public class TopBarViewModel : IViewModel
{
    // Severity thresholds
    private const int RunwayWarningDays = 14;
    private const int RunwayDangerDays  = 7;

    // Company block
    public string CompanyName  { get; private set; }
    public string CompanyTier  { get; private set; }

    // Cash metric
    public string CashDisplay  { get; private set; }
    public string CashLabel    { get; private set; }

    // Net / month metric
    public string        NetMonthDisplay  { get; private set; }
    public string        NetMonthLabel    { get; private set; }
    public SeverityState NetMonthSeverity { get; private set; }

    // Runway metric
    public string        RunwayDisplay  { get; private set; }
    public string        RunwayLabel    { get; private set; }
    public SeverityState RunwaySeverity { get; private set; }
    public TooltipData   RunwayTooltip  { get; private set; }

    // Timeline
    public string TimelineDisplay { get; private set; }

    // Speed / pause
    public int  CurrentSpeed  { get; private set; }
    public bool IsPaused      { get; private set; }
    public string ContinueLabel { get; private set; }

    // IViewModel
    public bool IsDirty { get; private set; }
    public void ClearDirty() => IsDirty = false;

    public TopBarViewModel()
    {
        CompanyName     = "Quantum Pixel Ltd";
        CompanyTier     = "Software Startup";
        CashDisplay     = "£312,450";
        CashLabel       = "Available Funds";
        NetMonthDisplay = "-£18,200";
        NetMonthLabel   = "Burn Rate";
        NetMonthSeverity = SeverityState.Warning;
        RunwayDisplay   = "17.1 months";
        RunwayLabel     = "Est. Runway";
        RunwaySeverity  = SeverityState.Normal;
        TimelineDisplay = "Day 12 · Month 2 · Year 1";
        CurrentSpeed    = 1;
        IsPaused        = true;
        ContinueLabel   = "Continue";
    }

    public void Refresh(GameStateSnapshot snapshot)
    {
        if (snapshot == null) return;

        // Company block
        CompanyName = snapshot.CompanyName ?? "My Company";
        CompanyTier = FormatCompanyTier(snapshot);

        // Cash
        CashDisplay = UIFormatting.FormatMoney(snapshot.Money);
        CashLabel   = "Available Funds";

        // Net / month  =  revenue – expenses
        int net         = snapshot.TotalRevenue - snapshot.MonthlyExpenses;
        NetMonthDisplay = (net >= 0 ? "+" : "") + UIFormatting.FormatMoney(net);
        NetMonthLabel   = "Burn Rate";
        NetMonthSeverity = net < 0 ? SeverityState.Warning : SeverityState.Normal;

        // Runway
        int runwayDays = snapshot.RunwayDays;
        RunwayDisplay = FormatRunway(runwayDays);
        RunwayLabel   = "Est. Runway";
        RunwaySeverity = runwayDays < RunwayDangerDays
            ? SeverityState.Danger
            : runwayDays < RunwayWarningDays
                ? SeverityState.Warning
                : SeverityState.Normal;

        // Tooltip: cash ÷ burn rate = runway
        RunwayTooltip = BuildRunwayTooltip(snapshot.Money, snapshot.MonthlyExpenses, runwayDays);

        // Timeline
        TimelineDisplay = string.Format(
            "Day {0} · Month {1} · Year {2}",
            snapshot.DayOfMonth,
            snapshot.CurrentMonth,
            snapshot.CurrentYear);

        // Speed / pause – speed is managed externally; snapshot has no speed level
        IsPaused      = !snapshot.IsAdvancing;
        ContinueLabel = IsPaused ? "Continue" : "Pause";

        IsDirty = true;
    }

    // Expose a setter so WindowManager / TopBarView can push the current speed level
    public void SetCurrentSpeed(int speed)
    {
        CurrentSpeed = UnityEngine.Mathf.Clamp(speed, 1, 3);
    }

    // ── Private helpers ────────────────────────────────────────────────────────

    private static string FormatCompanyTier(GameStateSnapshot snapshot)
    {
        return snapshot.CurrentReputationTier switch
        {
            ReputationTier.Unknown        => "Indie Studio",
            ReputationTier.Startup        => "Startup Studio",
            ReputationTier.Established    => "Established Studio",
            ReputationTier.Respected      => "Respected Studio",
            ReputationTier.IndustryLeader => "Industry Leader",
            _                             => "Indie Studio"
        };
    }

    private static string FormatRunway(int days)
    {
        if (days <= 0)        return "< 1 day";
        if (days >= 999)      return "> 999 days";
        if (days == 1)        return "1 day";
        return days + " days";
    }

    private static TooltipData BuildRunwayTooltip(long cash, long monthlyExpenses, int runwayDays)
    {
        string cashStr    = UIFormatting.FormatMoney(cash);
        string burnStr    = UIFormatting.FormatMoney(monthlyExpenses > 0 ? monthlyExpenses / 30 : 0);
        string runwayStr  = FormatRunway(runwayDays);

        return new TooltipData
        {
            Title = "Estimated Runway",
            Body  = "How long the company can operate without new revenue.",
            Stats = new[]
            {
                new TooltipStatRow { Label = "Cash balance",         Value = cashStr,   Style = TooltipRowStyle.Normal },
                new TooltipStatRow { Label = "Burn rate (daily)",    Value = burnStr,   Style = TooltipRowStyle.Normal },
                new TooltipStatRow { Label = "Estimated runway",     Value = runwayStr, Style = TooltipRowStyle.Header },
            }
        };
    }
}
