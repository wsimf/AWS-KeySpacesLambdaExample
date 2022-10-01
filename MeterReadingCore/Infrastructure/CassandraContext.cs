using System.Security.Cryptography.X509Certificates;
using Cassandra;
using MeterReading.Core.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ISession = Cassandra.ISession;

namespace MeterReading.Core.Infrastructure;

public sealed class CassandraContext : IDisposable
{
    public const string DefaultKeySpace = "meter_reading";

    public const string TableName = "meter_reading_values";
    public const string ColumnMeterId = "meter_id";
    public const string ColumnDate = "date";
    public const string ColumnValue = "value";
    public const string ColumnTime = "time";

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
CREATE TABLE IF NOT EXISTS {DefaultKeySpace}.{TableName} (
    {ColumnMeterId} varchar,
    {ColumnDate} date,
    {ColumnValue} int,
    {ColumnTime} time,
    PRIMARY KEY (({ColumnMeterId}, {ColumnDate}), {ColumnTime})
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

        var certCollection = new X509Certificate2Collection();
        var awsCertificate = new X509Certificate2("sf-class2-root.crt");
        
        certCollection.Add(awsCertificate);
        
        Cluster? cluster = Cluster.Builder()
            .AddContactPoint(options.ContactPoint)
            .WithPort(options.ContactPort)
            .WithAuthProvider(new PlainTextAuthProvider(options.UserName, options.Password))
            .WithSSL(new SSLOptions().SetCertificateCollection(certCollection))
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