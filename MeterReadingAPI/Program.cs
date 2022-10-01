using MeterReading.Core.Infrastructure;
using MeterReading.Core.Options;
using MeterReading.Core.Services;
using MeterReading.Core.Services.Default;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<CassandraOptions>(builder.Configuration.GetSection(CassandraOptions.SectionName));
builder.Services.Configure<AWSOptions>(builder.Configuration.GetSection(AWSOptions.SectionName));

builder.Services.AddScoped<CassandraContext>();
builder.Services.AddScoped<IMeterReadingService, DefaultMeterReadingService>();

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddAWSLambdaHosting(LambdaEventSource.HttpApi);

builder.Host.ConfigureLogging((context, log) =>
{
    var options = new LambdaLoggerOptions(context.Configuration);
    log.AddLambdaLogger(options);
});

WebApplication app = builder.Build();

// Migrate the DB before starting the requests if needed
// using (IServiceScope scope = app.Services.CreateScope())
// {
//     var cassandraContext = scope.ServiceProvider.GetRequiredService<CassandraContext>();
//     await cassandraContext.MigrateIfRequired().ConfigureAwait(false);
// }

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.Run();