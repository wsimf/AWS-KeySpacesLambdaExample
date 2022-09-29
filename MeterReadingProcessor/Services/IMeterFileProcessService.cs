namespace MeterReading.Processor.Services;

public interface IMeterFileProcessService
{
    public Task Process(string fileKey, string bucketName);
}