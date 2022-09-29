using Cassandra;
using MeterReading.Core.Models;
using MeterReading.Core.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ISession = Cassandra.ISession;

namespace MeterReading.Core.Infrastructure;

public sealed class CassandraContext : IDisposable
{
    public const string DefaultKeySpace = "meter_reading";

    private readonly ILogger<CassandraContext> _logger;
    private readonly IOptions<CassandraOptions> _options;
    private ISession? _currentSession;

    public CassandraContext(IOptions<CassandraOptions> options, ILogger<CassandraContext> logger)
    {
        _options = options;
        _logger = logger;
    }

    public async Task<RowSet> Execute(IStatement statement)
    {
        _currentSession ??= await Connect().ConfigureAwait(false);
        return await _currentSession.ExecuteAsync(statement).ConfigureAwait(false);
    }
    
    public async Task<PreparedStatement> PrepareStatement(string cql)
    {
        _currentSession ??= await Connect().ConfigureAwait(false);
        return await _currentSession.PrepareAsync(cql).ConfigureAwait(false);
    }
    
    public async Task MigrateIfRequired()
    {
        const string keyspace =
            $@"
CREATE KEYSPACE IF NOT EXISTS {DefaultKeySpace} 
WITH REPLICATION = 
    {{ 'class' : 'SimpleStrategy', 
       'replication_factor' : '1' 
    }};";

        await Execute(keyspace).ConfigureAwait(false);

        const string readings = $@"
CREATE TABLE IF NOT EXISTS {DefaultKeySpace}.{nameof(MeterReadingValue)} (
    {nameof(MeterReadingValue.MeterId)} varchar,
    {nameof(MeterReadingValue.Date)} date,
    {nameof(MeterReadingValue.Value)} int,
    {nameof(MeterReadingValue.Time)} time,
    PRIMARY KEY (({nameof(MeterReadingValue.MeterId)}, {nameof(MeterReadingValue.Date)}), {nameof(MeterReadingValue.Time)})
)";

        await Execute(readings).ConfigureAwait(false);
    }

    private async ValueTask Execute(string cql)
    {
        _currentSession ??= await Connect().ConfigureAwait(false);
        _logger.LogDebug("Executing {Cql}", cql);

        _currentSession.Execute(cql);
    }

    private Task<ISession> Connect()
    {
        CassandraOptions options = _options.Value;
        _logger.LogDebug("Connecting to {Server}", options.ContactPoint);

        Cluster? cluster = Cluster.Builder()
            .AddContactPoint(options.ContactPoint)
            .WithPort(options.ContactPort)
            .WithAuthProvider(new PlainTextAuthProvider(options.UserName, options.Password))
            .Build();

        ArgumentNullException.ThrowIfNull(cluster);

        return cluster.ConnectAsync()
            .ContinueWith(x =>
            {
                _logger.LogInformation("Connected to {Server} successfully", options.ContactPoint);
                return x.Result;
            });
    }

    public void Dispose()
    {
        _currentSession?.Dispose();
    }
}