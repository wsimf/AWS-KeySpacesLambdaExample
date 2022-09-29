namespace MeterReading.Processor.Services;

public interface IMeterReaderFileRetrieverService
{
    public Task<Stream> RetrieveFile(string bucketName, string key);
}