using JetBrains.Annotations;

namespace MeterReading.Core.Options;

public sealed record CassandraOptions
{
    public const string SectionName = "Cassandra";

    public string? ContactPoint { get; [UsedImplicitly] set; }
    public int ContactPort { get; [UsedImplicitly] set; } = 9142;
    public string? UserName { get; [UsedImplicitly] set; }
    public string? Password { get; [UsedImplicitly] set; }
}