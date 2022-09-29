using Cassandra;

namespace MeterReading.Core.Extensions;

public static class DateTimeExtensions
{
    public static bool TryParseDate(string date, out LocalDate parsed)
    {
        parsed = null!;

        if (DateOnly.TryParse(date, out DateOnly dateOnly))
        {
            parsed = dateOnly.ToLocalDate();
            return true;
        }

        return false;
    }

    public static bool TryParseTime(string time, out LocalTime parsed)
    {
        parsed = null!;

        if (TimeOnly.TryParse(time, out TimeOnly timeOnly))
        {
            parsed = timeOnly.ToLocalTime();
            return true;
        }

        return false;
    }

    public static LocalDate ToLocalDate(this DateOnly date) => new(date.Year, date.Month, date.Day);

    public static LocalTime ToLocalTime(this TimeOnly time) => new(time.Hour, time.Minute, time.Second, 0);
}