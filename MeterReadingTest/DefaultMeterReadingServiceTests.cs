using Bogus;
using MeterReading.Core.Infrastructure;
using MeterReading.Core.Services.Default;
using MeterReadingTest.Mock;
using NUnit.Framework;

namespace MeterReadingTest;

public sealed class DefaultMeterReadingServiceTests
{
    private readonly Faker _faker = new();

    [SetUp]
    public void Setup() { }

    [Test]
    public async Task Test_CalculateSum()
    {
        Dictionary<string, int>[] data = Enumerable.Range(0, 20)
            .Select(_ => _faker.Random.Int(10, 500))
            .Select(x => new Dictionary<string, int> { { CassandraContext.ColumnValue, x } })
            .ToArray();

        ICassandraContext context = MockCassandraContext.CreateCassandraContextMock(data).Object;
        DefaultMeterReadingRepository repository = new(context);

        int calculated = await repository.CalculateSum("SampleMeter", new DateOnly());
        int expected = data.Select(x => x[CassandraContext.ColumnValue]).Sum();
        
        Assert.That(calculated, Is.Not.Zero);
        Assert.That(calculated, Is.EqualTo(expected));
    }
}