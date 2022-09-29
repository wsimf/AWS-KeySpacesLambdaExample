using Cassandra;
using MeterReading.Core.Extensions;
using MeterReading.Core.Infrastructure;
using MeterReading.Core.Models;

namespace MeterReading.Core.Services.Default;

public sealed class DefaultMeterReadingService : IMeterReadingService
{
    private const string CqlCalculateSum =
        $"SELECT sum({nameof(MeterReadingValue.Value)}) FROM {CassandraContext.DefaultKeySpace}.{nameof(MeterReadingValue)} where {nameof(MeterReadingValue.MeterId)}=? AND {nameof(MeterReadingValue.Date)}=?";

    private const string CqlInsertMeterReading =
        $"INSERT INTO {CassandraContext.DefaultKeySpace}.{nameof(MeterReadingValue)} ({nameof(MeterReadingValue.MeterId)}, {nameof(MeterReadingValue.Date)}, {nameof(MeterReadingValue.Time)}, {nameof(MeterReadingValue.Value)}) VALUES (?, ?, ?, ?)";

    private readonly CassandraContext _context;

    public DefaultMeterReadingService(CassandraContext context)
    {
        _context = context;
    }

    public async Task AddMeterReadings(IEnumerable<MeterReadingValue> readingValue)
    {
        var batch = new BatchStatement();
        PreparedStatement prepared = await _context.PrepareStatement(CqlInsertMeterReading).ConfigureAwait(false);

        foreach (MeterReadingValue value in readingValue)
        {
            batch.Add(prepared.Bind(value.MeterId, value.Date, value.Time, value.Value));
        }

        await _context.Execute(batch);
    }

    public async Task<int> CalculateSum(string meterId, DateOnly date)
    {
        PreparedStatement pendingStatement = await _context.PrepareStatement(CqlCalculateSum).ConfigureAwait(false);
        BoundStatement? statement = pendingStatement.Bind(meterId, new LocalDate(date.Year, date.Month, date.Day));

        ArgumentNullException.ThrowIfNull(statement);

        RowSet result = await _context.Execute(statement).ConfigureAwait(false);
        return result.GetFirstValue<int>();
    }
}