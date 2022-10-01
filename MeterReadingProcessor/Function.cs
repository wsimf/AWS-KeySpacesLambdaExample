using Amazon.Lambda.Core;
using Amazon.Lambda.S3Events;
using JetBrains.Annotations;
using MeterReading.Core.Infrastructure;
using MeterReading.Core.Options;
using MeterReading.Core.Services;
using MeterReading.Core.Services.Default;
using MeterReading.Processor.Services;
using MeterReading.Processor.Services.Default;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace MeterReading.Processor;

[UsedImplicitly]
public class Function
{
    private readonly ServiceProvider _serviceProvider;

    public Function()
    {
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddEnvironmentVariables() // Cassandra options are injected through env variables when running in lambda
            .Build();

        var services = new ServiceCollection();

        services.Configure<CassandraOptions>(configuration.GetSection(CassandraOptions.SectionName));
        services.Configure<AWSOptions>(configuration.GetSection(AWSOptions.SectionName));

        services.AddSingleton<CassandraContext>();

        services.AddScoped<IMeterReadingService, DefaultMeterReadingService>();
        services.AddScoped<IMeterFileProcessService, DefaultMeterFileProcessService>();
        services.AddScoped<IMeterReaderFileRetrieverService, DefaultMeterReaderFileRetrieverService>();
        services.AddScoped<IMeterReaderFileHandlerService, DefaultMeterReaderFileHandlerService>();

        _serviceProvider = services.BuildServiceProvider();

        LambdaLogger.Log("New Function Context initialised");
    }

    [UsedImplicitly]
    public Task FunctionHandler(S3Event @event, ILambdaContext context)
    {
        context.Logger.LogInformation($"Handing S3 event: {context.AwsRequestId}");

        using IServiceScope scope = _serviceProvider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IMeterReaderFileHandlerService>();

        return service.Handle(@event);
    }
}