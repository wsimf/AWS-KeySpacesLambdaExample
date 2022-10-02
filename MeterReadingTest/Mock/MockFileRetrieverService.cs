using MeterReading.Processor.Services;

namespace MeterReadingTest.Mock;

public sealed class MockFileRetrieverService : IMeterReaderFileRetrieverService
{
    public const string MockFileName = "consumption.csv";

    public Task<Stream> RetrieveFile(string bucketName, string key)
    {
        Stream result = File.OpenRead(MockFileName);

        return Task.FromResult(result);
    }
}