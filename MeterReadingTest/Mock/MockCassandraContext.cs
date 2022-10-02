using Cassandra;
using MeterReading.Core.Infrastructure;
using Moq;

namespace MeterReadingTest.Mock;

internal static class MockCassandraContext
{
    internal static Mock<ICassandraContext> CreateCassandraContextMock(IEnumerable<IDictionary<string, int>> values)
    {
        var result = new Mock<ICassandraContext>();

        RowSet expected = CreateMockRowSet(values).Object;
        result.Setup(x => x.Execute(It.IsAny<IStatement>())).ReturnsAsync(expected);
        result.Setup(x => x.PrepareStatement(It.IsAny<string>())).ReturnsAsync(new PreparedStatement());

        return result;
    }

    internal static Mock<RowSet> CreateMockRowSet(IEnumerable<IDictionary<string, int>> values)
    {
        var result = new Mock<RowSet>();

        List<Row> rows = values.Select(CreateMockRow).Select(x => x.Object).ToList();
        result.Setup(x => x.GetEnumerator()).Returns(rows.GetEnumerator());
        result.Setup(x => x.IsFullyFetched).Returns(true);

        return result;
    }

    internal static Mock<Row> CreateMockRow(IDictionary<string, int> values)
    {
        var result = new Mock<Row>();
        foreach ((string propertyName, int propertyValue) in values)
        {
            result.Setup(x => x.GetValue<int>(propertyName)).Returns(propertyValue);
        }

        return result;
    }
}