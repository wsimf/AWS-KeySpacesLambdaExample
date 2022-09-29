using MeterReading.Core.Models;

namespace MeterReading.Core.Services;

public interface IMeterReadingService
{
    public Task AddMeterReadings(IEnumerable<MeterReadingValue> readingValue);
    public Task<int> CalculateSum(string meterId, DateOnly date);
}