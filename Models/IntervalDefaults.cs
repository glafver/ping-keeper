namespace PingKeeper.Models;

/// <summary>
/// Interval limits and unit conversion, so the magic numbers live in one place.
/// </summary>
public static class IntervalDefaults
{
    public const int MinutesPerHour = 60;
    public const int MinutesPerDay = 1440;
    public const int MinutesPerWeek = 10080;

    public const int MinMinutes = 5;
    public const int MaxMinutes = MinutesPerWeek;
    public const int DefaultMinutes = MinutesPerDay;

    /// <summary>Converts a value + unit (as posted by the form) into minutes, clamped to the valid range.</summary>
    public static int ToMinutes(int value, string unit)
    {
        var minutes = unit switch
        {
            "minutes" => value,
            "hours" => value * MinutesPerHour,
            "weeks" => value * MinutesPerWeek,
            _ => value * MinutesPerDay, // days
        };
        return Math.Clamp(minutes, MinMinutes, MaxMinutes);
    }

    /// <summary>Splits minutes back into a friendly (value, unit) pair for the edit form.</summary>
    public static (int Value, string Unit) FromMinutes(int minutes)
    {
        if (minutes <= 0) minutes = DefaultMinutes;
        if (minutes % MinutesPerWeek == 0) return (minutes / MinutesPerWeek, "weeks");
        if (minutes % MinutesPerDay == 0) return (minutes / MinutesPerDay, "days");
        if (minutes % MinutesPerHour == 0) return (minutes / MinutesPerHour, "hours");
        return (minutes, "minutes");
    }
}
