namespace MeterReading.Core.Extensions;

public static class StringExtensions
{
    public static bool IsPresent(this string s) => !string.IsNullOrWhiteSpace(s);
}