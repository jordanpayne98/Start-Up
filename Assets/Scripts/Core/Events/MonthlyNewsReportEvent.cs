public class MonthlyNewsReportEvent : GameEvent
{
    public MonthlyNewsReport Report;

    public MonthlyNewsReportEvent(int tick, MonthlyNewsReport report) : base(tick)
    {
        Report = report;
    }
}
