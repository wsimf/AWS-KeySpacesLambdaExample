using Amazon.Lambda.S3Events;

namespace MeterReading.Processor.Services;

public interface IMeterReaderFileHandlerService
{
    public Task Handle(S3Event @event);
}