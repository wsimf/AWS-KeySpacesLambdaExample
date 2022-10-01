using Amazon;
using Amazon.Lambda.Core;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using MeterReading.Core.Options;
using Microsoft.Extensions.Options;

namespace MeterReading.Processor.Services.Default;

public sealed class DefaultMeterReaderFileRetrieverService : IMeterReaderFileRetrieverService
{
    private readonly IOptions<AWSOptions> _awsOptions;

    public DefaultMeterReaderFileRetrieverService(IOptions<AWSOptions> awsOptions)
    {
        _awsOptions = awsOptions;
    }

    public async Task<Stream> RetrieveFile(string bucketName, string key)
    {
        AmazonS3Client client = GetClient();
        LambdaLogger.Log($"Retrieving S3 file {key} from {bucketName}");

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

    private AmazonS3Client GetClient()
    {
        AWSOptions options = _awsOptions.Value;
        if (!string.IsNullOrWhiteSpace(options.UserKey) && !string.IsNullOrWhiteSpace(options.UserSecret))
        {
            var auth = new BasicAWSCredentials(_awsOptions.Value.UserKey, _awsOptions.Value.UserSecret);
            LambdaLogger.Log($"Using user key {auth.GetCredentials().AccessKey} for S3 Client");

            return new AmazonS3Client(auth, RegionEndpoint.GetBySystemName(_awsOptions.Value.Region));
        }

        // UserKey or UserSecret is not required in lambda

        string? region = Environment.GetEnvironmentVariable("AWS_REGION");
        LambdaLogger.Log($"Using S3 client in {region}");

        return new AmazonS3Client(RegionEndpoint.GetBySystemName(region));
    }
}