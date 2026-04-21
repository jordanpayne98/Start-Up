public class DayChangedEvent : GameEvent
{
    public int Day { get; }
    public int Month { get; }
    public int Year { get; }
    
    public DayChangedEvent(int tick, int day, int month, int year) : base(tick)
    {
        Day = day;
        Month = month;
        Year = year;
    }
}
