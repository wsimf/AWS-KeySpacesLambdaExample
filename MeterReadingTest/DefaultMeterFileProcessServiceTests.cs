using MeterReading.Core.Models;
using MeterReading.Core.Services;
using MeterReading.Processor.Services.Default;
using MeterReadingTest.Mock;
using Moq;
using NUnit.Framework;

namespace MeterReadingTest;

[TestFixture]
public class DefaultMeterFileProcessServiceTests
{
    [Test]
    public async Task Test_CSVFileHandle()
    {
        var fileRetrieverService = new MockFileRetrieverService();
        var fileProcessServiceMock = new Mock<IMeterReadingRepository>();

        var processor = new DefaultMeterFileProcessService(fileRetrieverService, fileProcessServiceMock.Object);
        await processor.Process("TestFile", "Any").ConfigureAwait(false);

        fileProcessServiceMock.Verify(x => x.AddMeterReadings(It.Is<IEnumerable<MeterReadingValue>>(values => values.Any())));
        fileProcessServiceMock.Verify(x => x.AddMeterReadings(It.Is<IEnumerable<MeterReadingValue>>(values => values.All(v => v.Date.Year == 2019))));
        fileProcessServiceMock.Verify(x => x.AddMeterReadings(It.Is<IEnumerable<MeterReadingValue>>(values => values.All(v => v.Date.Month == 1))));

        foreach (int date in Enumerable.Range(1, 31)) // check if there are values for all dates
        {
            foreach (int hour in Enumerable.Range(0, 23)) // check if there are values for all hours at 30 min intervals
            {
                fileProcessServiceMock.Verify(x =>
                    x.AddMeterReadings(It.Is<IEnumerable<MeterReadingValue>>(values => values.Any(v => v.Date.Day == date && v.Time.Hour == hour))));

                fileProcessServiceMock.Verify(x =>
                    x.AddMeterReadings(It.Is<IEnumerable<MeterReadingValue>>(values => values.Any(v => v.Date.Day == date && v.Time.Minute == 0))));

                fileProcessServiceMock.Verify(x =>
                    x.AddMeterReadings(It.Is<IEnumerable<MeterReadingValue>>(values => values.Any(v => v.Date.Day == date && v.Time.Minute == 30))));
            }
        }

        fileProcessServiceMock.Verify(x =>
            x.AddMeterReadings(It.Is<IEnumerable<MeterReadingValue>>(values => values.All(value => value.MeterId == "EE00027"))));
    }
}