using Cassandra;

namespace MeterReading.Core.Extensions;

public static class RowSetExtensions
{
    public static T? GetFirstValue<T>(this RowSet set)
    {
        Row? firstRow = set.GetRows().FirstOrDefault();
        return firstRow is null ? default : firstRow.GetValue<T>(0);
    }
}