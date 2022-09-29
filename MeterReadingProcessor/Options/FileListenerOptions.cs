namespace MeterReading.Processor.Options;

public sealed record FileListenerOptions
{
    public const string SectionName = "FileListener";
    
    public string? QueueUrl { get; set; }
    public int WaitTimeSeconds { get; set; } = 15;
}