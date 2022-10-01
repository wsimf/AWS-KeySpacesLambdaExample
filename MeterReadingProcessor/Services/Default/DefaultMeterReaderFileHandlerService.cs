using Amazon.Lambda.S3Events;

namespace MeterReading.Processor.Services.Default;

public sealed class DefaultMeterReaderFileHandlerService : IMeterReaderFileHandlerService
{
    private readonly IMeterFileProcessService _fileProcessService;

    public DefaultMeterReaderFileHandlerService(IMeterFileProcessService fileProcessService)
    {
        _fileProcessService = fileProcessService;
    }

    public async Task Handle(S3Event @event)
    {
        if (@event.Records.Any())
        {
            foreach (S3Event.S3EventNotificationRecord? record in @event.Records)
            {
                await _fileProcessService.Process(record.S3.Object.Key, record.S3.Bucket.Name).ConfigureAwait(false);
            }
        }
    }
}