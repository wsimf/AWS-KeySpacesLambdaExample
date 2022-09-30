using Amazon;
using Amazon.Runtime;
using Amazon.S3.Util;
using Amazon.SQS;
using Amazon.SQS.Model;
using MeterReading.Core.Options;
using MeterReading.Processor.Options;
using MeterReading.Processor.Services;
using Microsoft.Extensions.Options;

namespace MeterReading.Processor;

public sealed class MeterReaderFileListenerService : BackgroundService
{
    private readonly ILogger<MeterReaderFileListenerService> _logger;
    private readonly IOptions<AWSOptions> _awsOptions;
    private readonly IOptions<FileListenerOptions> _fileListenerOptions;
    private readonly IServiceProvider _serviceProvider;

    public MeterReaderFileListenerService(IOptions<AWSOptions> awsOptions,
        IOptions<FileListenerOptions> fileListenerOptions,
        ILogger<MeterReaderFileListenerService> logger,
        IServiceProvider serviceProvider)
    {
        _awsOptions = awsOptions;
        _fileListenerOptions = fileListenerOptions;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var auth = new BasicAWSCredentials(_awsOptions.Value.UserKey, _awsOptions.Value.UserSecret);
        _logger.LogInformation("Using user key {Account} for SQS Client", auth.GetCredentials().AccessKey);

        var client = new AmazonSQSClient(auth, RegionEndpoint.GetBySystemName(_awsOptions.Value.Region));

        do
        {
            var request = new ReceiveMessageRequest
            {
                QueueUrl = _fileListenerOptions.Value.QueueUrl,
                WaitTimeSeconds = _fileListenerOptions.Value.WaitTimeSeconds,
                MaxNumberOfMessages = 1
            };

            try
            {
                ReceiveMessageResponse? result = await client.ReceiveMessageAsync(request, cancellationToken).ConfigureAwait(false);
                await HandleMessage(result, client, cancellationToken).ConfigureAwait(false);
            }
            catch (TaskCanceledException)
            {
                break;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error handling message from queue");
            }
        } while (!cancellationToken.IsCancellationRequested);
    }

    private async Task HandleMessage(ReceiveMessageResponse? result, IAmazonSQS client, CancellationToken cancellationToken)
    {
        if (result is null || !result.Messages.Any())
        {
            return;
        }

        foreach (Message message in result.Messages)
        {
            using IDisposable logScope = _logger.BeginScope("{Id}", message.MessageId);
            
            _logger.LogInformation("Handling message {MessageId}", message.MessageId);

            S3EventNotification? @event = S3EventNotification.ParseJson(message.Body);
            if (@event is not null && @event.Records.Any())
            {
                using IServiceScope scope = _serviceProvider.CreateScope();
                var fileProcessService = scope.ServiceProvider.GetRequiredService<IMeterFileProcessService>();

                foreach (S3EventNotification.S3EventNotificationRecord record in @event.Records)
                {
                    await fileProcessService.Process(record.S3.Object.Key, record.S3.Bucket.Name);
                }
            }

            await DeleteMessage(client, message, cancellationToken).ConfigureAwait(false); // delete handled message
        }
    }

    private Task DeleteMessage(IAmazonSQS client, Message message, CancellationToken cancellationToken)
    {
        var request = new DeleteMessageRequest
        {
            QueueUrl = _fileListenerOptions.Value.QueueUrl,
            ReceiptHandle = message.ReceiptHandle
        };

        _logger.LogDebug("Mark message handled: {Id}", message.MessageId);
        return client.DeleteMessageAsync(request, cancellationToken);
    }
}