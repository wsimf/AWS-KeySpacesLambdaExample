using Cassandra;

namespace MeterReading.Core.Models;

public sealed record MeterReadingValue
{
    public string MeterId { get; set; } = null!;
    public LocalDate Date { get; set; } = null!;
    public LocalTime Time { get; set; } = null!;
    public int Value { get; set; }
}