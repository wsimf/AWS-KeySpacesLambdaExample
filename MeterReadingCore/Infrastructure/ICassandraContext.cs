using Cassandra;

namespace MeterReading.Core.Infrastructure;

public interface ICassandraContext
{
    public Task<RowSet> Execute(IStatement statement);

    public Task<PreparedStatement> PrepareStatement(string cql);
}