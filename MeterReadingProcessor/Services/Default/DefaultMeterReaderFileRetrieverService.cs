using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using MeterReading.Core.Options;
using Microsoft.Extensions.Options;

namespace MeterReading.Processor.Services.Default;

public sealed class DefaultMeterReaderFileRetrieverService : IMeterReaderFileRetrieverService
{
    private readonly ILogger<DefaultMeterReaderFileRetrieverService> _logger;
    private readonly IOptions<AWSOptions> _awsOptions;

    public DefaultMeterReaderFileRetrieverService(ILogger<DefaultMeterReaderFileRetrieverService> logger, IOptions<AWSOptions> awsOptions)
    {
        _logger = logger;
        _awsOptions = awsOptions;
    }

    public async Task<Stream> RetrieveFile(string bucketName, string key)
    {
        var auth = new BasicAWSCredentials(_awsOptions.Value.UserKey, _awsOptions.Value.UserSecret);
        _logger.LogDebug("Using user key {Account} for S3 Client", auth.GetCredentials().AccessKey);

        var client = new AmazonS3Client(auth, RegionEndpoint.GetBySystemName(_awsOptions.Value.Region));
        _logger.LogDebug("Retrieving S3 file {Key} from {Bucket}", key, bucketName);

        var request = new GetObjectRequest
        {
            BucketName = bucketName,
            Key = key
        };

        try
        {
            using GetObjectResponse response = await client.GetObjectAsync(request).ConfigureAwait(false);

            // assuming the CSV files aren't bulky, if it is, we may need to write the content to the disk
            var memoryStream = new MemoryStream();
            await response.ResponseStream.CopyToAsync(memoryStream);
            memoryStream.Seek(0, SeekOrigin.Begin);

            return memoryStream;
        }
        catch (AmazonS3Exception e)
        {
            if ("NoSuchKey".Equals(e.ErrorCode, StringComparison.OrdinalIgnoreCase))
            {
                throw new FileNotFoundException($"Couldn't find {key} in bucket {bucketName}");
            }

            throw;
        }
    }
}