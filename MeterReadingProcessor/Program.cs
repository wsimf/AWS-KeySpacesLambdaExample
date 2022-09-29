using MeterReading.Core.Infrastructure;
using MeterReading.Core.Options;
using MeterReading.Core.Services;
using MeterReading.Core.Services.Default;
using MeterReading.Processor;
using MeterReading.Processor.Options;
using MeterReading.Processor.Services;
using MeterReading.Processor.Services.Default;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

IHost host = Host.CreateDefaultBuilder(args)
    .UseSerilog((_, loggerConfig) =>
    {
        loggerConfig.MinimumLevel.Debug();

        loggerConfig.WriteTo.Async(c =>
            c.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{Id}] {SourceContext} {Message:lj}{NewLine}{Exception}",
                theme: AnsiConsoleTheme.Code));
    })
    .ConfigureServices((context, services) =>
    {
        services.Configure<CassandraOptions>(context.Configuration.GetSection(CassandraOptions.SectionName));
        services.Configure<AWSOptions>(context.Configuration.GetSection(AWSOptions.SectionName));
        services.Configure<FileListenerOptions>(context.Configuration.GetSection(FileListenerOptions.SectionName));

        services.AddScoped<CassandraContext>();
        services.AddScoped<IMeterReadingService, DefaultMeterReadingService>();
        services.AddScoped<IMeterFileProcessService, DefaultMeterFileProcessService>();
        services.AddScoped<IMeterReaderFileRetrieverService, DefaultMeterReaderFileRetrieverService>();

        services.AddHostedService<MeterReaderFileListenerService>();
    })
    .Build();

await host.RunAsync().ConfigureAwait(false);