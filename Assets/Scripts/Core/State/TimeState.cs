using System;

[Serializable]
public class TimeState
{
    public const int TicksPerDay = 4800;
    public const int TicksPerHour = TicksPerDay / 24; // 200
    public const int TicksPerMinute = TicksPerHour / 60; // 3
    public const int MonthsPerYear = 12;
    public const int StartingYear = 2026;
    public const int DaysPerYear = 365; // 31+28+31+30+31+30+31+31+30+31+30+31

    // Index 0 = unused. Index 1–12 = Jan–Dec (no leap years).
    public static readonly int[] DaysInMonth = { 0, 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };

    public int currentDay;
    public int currentMonth;
    public int currentYear;

    // Returns 1-based day-of-month for a 0-based absolute day index.
    public static int GetDayOfMonth(int absoluteDay) {
        int dayInYear = ((absoluteDay % DaysPerYear) + DaysPerYear) % DaysPerYear;
        for (int m = 1; m <= 12; m++) {
            if (dayInYear < DaysInMonth[m]) return dayInYear + 1;
            dayInYear -= DaysInMonth[m];
        }
        return 1;
    }

    // Returns 1-based month (1–12) for a 0-based absolute day index.
    public static int GetMonth(int absoluteDay) {
        int dayInYear = ((absoluteDay % DaysPerYear) + DaysPerYear) % DaysPerYear;
        for (int m = 1; m <= 12; m++) {
            if (dayInYear < DaysInMonth[m]) return m;
            dayInYear -= DaysInMonth[m];
        }
        return 12;
    }

    // Returns the calendar year for a 0-based absolute day index.
    public static int GetYear(int absoluteDay) {
        if (absoluteDay >= 0) return StartingYear + absoluteDay / DaysPerYear;
        return StartingYear + (absoluteDay - DaysPerYear + 1) / DaysPerYear;
    }

    // Converts a (1-based dayOfMonth, 1-based month, calendar year) to a 0-based absolute day index.
    public static int ToAbsoluteDay(int dayOfMonth, int month, int year) {
        int y = year - StartingYear;
        int total = y * DaysPerYear;
        for (int m = 1; m < month; m++) total += DaysInMonth[m];
        total += dayOfMonth - 1;
        return total;
    }

    // Instance helper — delegates to the static version for backwards compatibility.
    public int GetDayOfMonth() {
        return GetDayOfMonth(currentDay);
    }

    public int GetTotalDays() {
        return currentDay;
    }

    public static TimeState CreateNew() {
        return new TimeState {
            currentDay = 0,
            currentMonth = 1,
            currentYear = StartingYear
        };
    }
}
