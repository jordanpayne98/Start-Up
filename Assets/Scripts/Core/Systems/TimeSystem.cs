// TimeSystem Version: Clean v1
using System;

public class TimeSystem : ISystem
{
    public event Action<int> OnDayChanged;
    public event Action<int> OnMonthChanged;
    public event Action<int> OnYearChanged;

    private TimeState _state;
    private int _lastTick = -1;

    private bool _dayChanged;
    private bool _monthChanged;
    private bool _yearChanged;
    private int _capturedDay;
    private int _capturedMonth;
    private int _capturedYear;

    public int CurrentDay => _state.currentDay;
    public int CurrentMonth => _state.currentMonth;
    public int CurrentYear => _state.currentYear;
    public int DayOfMonth => _state.GetDayOfMonth();
    public int CurrentHour => (_lastTick >= 0 ? _lastTick : 0) % TimeState.TicksPerDay / TimeState.TicksPerHour;
    public int CurrentMinute => ((_lastTick >= 0 ? _lastTick : 0) % TimeState.TicksPerDay % TimeState.TicksPerHour) / TimeState.TicksPerMinute;

    public TimeSystem(TimeState state)
    {
        _state = state;
    }

    public void PreTick(int tick)
    {
    }

    public void Tick(int tick)
    {
        _lastTick = tick;

        int previousDay = _state.currentDay;
        int previousMonth = _state.currentMonth;
        int previousYear = _state.currentYear;

        _state.currentDay = tick / TimeState.TicksPerDay;

        int totalDays = _state.currentDay;
        _state.currentMonth = TimeState.GetMonth(totalDays);
        _state.currentYear = TimeState.GetYear(totalDays);

        if (_state.currentDay != previousDay) {
            _dayChanged = true;
            _capturedDay = _state.currentDay;
        }

        if (_state.currentMonth != previousMonth) {
            _monthChanged = true;
            _capturedMonth = _state.currentMonth;
        }

        if (_state.currentYear != previousYear) {
            _yearChanged = true;
            _capturedYear = _state.currentYear;
        }
    }

    public void PostTick(int tick)
    {
        if (_dayChanged) {
            OnDayChanged?.Invoke(_capturedDay);
            _dayChanged = false;
        }

        if (_monthChanged) {
            OnMonthChanged?.Invoke(_capturedMonth);
            _monthChanged = false;
        }

        if (_yearChanged) {
            OnYearChanged?.Invoke(_capturedYear);
            _yearChanged = false;
        }
    }

    public void ApplyCommand(ICommand command)
    {
    }

    public void Dispose()
    {
        OnDayChanged = null;
        OnMonthChanged = null;
        OnYearChanged = null;
    }
}
