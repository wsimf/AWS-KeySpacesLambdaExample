using JetBrains.Annotations;

namespace MeterReading.Core.Options;

public sealed record AWSOptions
{
    public const string SectionName = "AWS";

    public string? UserKey { get; [UsedImplicitly] set; }
    public string? UserSecret { get; [UsedImplicitly] set; }
    public string? Region { get; [UsedImplicitly] set; }
}