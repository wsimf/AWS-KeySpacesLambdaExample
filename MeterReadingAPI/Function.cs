using System.Net;
using System.Text.Json;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using JetBrains.Annotations;
using MeterReading.Core.Infrastructure;
using MeterReading.Core.Options;
using MeterReading.Core.Services;
using MeterReading.Core.Services.Default;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace MeterReading.Web.API;

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

        services.AddSingleton<ICassandraContext, CassandraContext>();
        services.AddScoped<IMeterReadingRepository, DefaultMeterReadingRepository>();

        _serviceProvider = services.BuildServiceProvider();

        LambdaLogger.Log("New Function Context initialised");
    }

    [UsedImplicitly]
    public async Task<APIGatewayHttpApiV2ProxyResponse> FunctionHandler(APIGatewayHttpApiV2ProxyRequest @event, ILambdaContext context)
    {
        static APIGatewayHttpApiV2ProxyResponse GenerateError(string error) => new()
        {
            StatusCode = (int)HttpStatusCode.BadRequest,
            Body = JsonSerializer.Serialize(new { error }, new JsonSerializerOptions { WriteIndented = true })
        };

        context.Logger.LogInformation($"Handing HTTP event: {context.AwsRequestId}");

        if (@event.QueryStringParameters?.TryGetValue("date", out string? date) != true || string.IsNullOrWhiteSpace(date))
        {
            return GenerateError("Date parameter is required. Pass date as a query parameter with key date");
        }

        if (@event.QueryStringParameters?.TryGetValue("meterId", out string? meterId) != true || string.IsNullOrWhiteSpace(meterId))
        {
            return GenerateError("MeterId parameter is required. Pass mater id as a query parameter with key meterId");
        }

        if (!DateOnly.TryParseExact(date, "dd-MM-yyyy", out DateOnly parsedDate))
        {
            return GenerateError($"Invalid date {date}. Use format dd-MM-yyyy");
        }

        context.Logger.LogInformation("HTTP content validated successfully");

        using IServiceScope scope = _serviceProvider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IMeterReadingRepository>();

        int sum = await service.CalculateSum(meterId, parsedDate).ConfigureAwait(false);
        var result = new { meterId, date = parsedDate.ToString("dd-MM-yyyy"), sum };

        return new APIGatewayHttpApiV2ProxyResponse
        {
            StatusCode = (int)HttpStatusCode.OK,
            Body = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true })
        };
    }
}