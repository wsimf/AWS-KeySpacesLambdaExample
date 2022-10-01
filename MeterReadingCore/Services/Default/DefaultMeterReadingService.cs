using Amazon.Lambda.Core;
using Cassandra;
using MeterReading.Core.Infrastructure;
using MeterReading.Core.Models;

namespace MeterReading.Core.Services.Default;

public sealed class DefaultMeterReadingService : IMeterReadingService
{
    private const string CqlCalculateSum =
        $"SELECT {CassandraContext.ColumnValue} FROM {CassandraContext.DefaultKeySpace}.{CassandraContext.TableName} WHERE {CassandraContext.ColumnMeterId}=? AND {CassandraContext.ColumnDate}=?";

    private const string CqlInsertMeterReading =
        $"INSERT INTO {CassandraContext.DefaultKeySpace}.{CassandraContext.TableName} ({CassandraContext.ColumnMeterId}, {CassandraContext.ColumnDate}, {CassandraContext.ColumnTime}, {CassandraContext.ColumnValue}) VALUES (?, ?, ?, ?)";

    private readonly CassandraContext _context;

    public DefaultMeterReadingService(CassandraContext context)
    {
        _context = context;
    }

    public async Task AddMeterReadings(IEnumerable<MeterReadingValue> readingValue)
    {
        PreparedStatement prepared = await _context.PrepareStatement(CqlInsertMeterReading).ConfigureAwait(false);

        IEnumerable<MeterReadingValue[]> chunked = readingValue.Chunk(30); // AWS KeySpaces only support 30 batched statements
        foreach (MeterReadingValue[] values in chunked)
        {
            var batch = new BatchStatement();
            batch.SetBatchType(BatchType.Unlogged);
            batch.SetConsistencyLevel(ConsistencyLevel.LocalQuorum);

            foreach (MeterReadingValue value in values)
            {
                batch.Add(prepared.Bind(value.MeterId, value.Date, value.Time, value.Value));
            }

            await _context.Execute(batch);
        }

        LambdaLogger.Log("Added meter reading values successfully");
    }

    public async Task<int> CalculateSum(string meterId, DateOnly date)
    {
        LambdaLogger.Log($"Calculating SUM for {meterId} on date {date}");

        PreparedStatement pendingStatement = await _context.PrepareStatement(CqlCalculateSum).ConfigureAwait(false);
        BoundStatement? statement = pendingStatement.Bind(meterId, new LocalDate(date.Year, date.Month, date.Day));

        ArgumentNullException.ThrowIfNull(statement);

        var results = new List<int>();

        RowSet result = await _context.Execute(statement).ConfigureAwait(false);
        do
        {
            results.AddRange(result.Select(r => r.GetValue<int>(CassandraContext.ColumnValue)));
            
            await result.FetchMoreResultsAsync().ConfigureAwait(false);
        } while (!result.IsFullyFetched);

        return results.Sum();
    }
}