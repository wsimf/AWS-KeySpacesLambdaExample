using System.Globalization;
using Amazon.Lambda.Core;
using Cassandra;
using CsvHelper;
using MeterReading.Core.Extensions;
using MeterReading.Core.Models;
using MeterReading.Core.Services;

namespace MeterReading.Processor.Services.Default;

public sealed class DefaultMeterFileProcessService : IMeterFileProcessService
{
    private const string CsvHeaderMeterId = "Meter";
    private const string CsvHeaderDate = "Date";

    private readonly IMeterReaderFileRetrieverService _retrieverService;
    private readonly IMeterReadingRepository _meterReadingRepository;

    public DefaultMeterFileProcessService(IMeterReaderFileRetrieverService retrieverService, IMeterReadingRepository meterReadingRepository)
    {
        _retrieverService = retrieverService;
        _meterReadingRepository = meterReadingRepository;
    }

    public async Task Process(string fileKey, string bucketName)
    {
        if (!(fileKey.IsPresent() && bucketName.IsPresent()))
        {
            return;
        }

        await using Stream content = await _retrieverService.RetrieveFile(bucketName, fileKey).ConfigureAwait(false);

        using var reader = new StreamReader(content);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

        var readings = new List<MeterReadingValue>();

        IAsyncEnumerable<dynamic>? records = csv.GetRecordsAsync<dynamic>();
        await foreach (IDictionary<string, object> record in records)
        {
            readings.AddRange(GetMeterReadings(record));
        }

        await _meterReadingRepository.AddMeterReadings(readings).ConfigureAwait(false);
        LambdaLogger.Log($"{readings.Count} reading(s) processed successfully");
    }

    /// <summary>
    /// Returns meter readings given a CSV row
    /// </summary>
    /// <param name="record"></param>
    /// <returns></returns>
    private static IEnumerable<MeterReadingValue> GetMeterReadings(IDictionary<string, object> record)
    {
        if (record.TryGetValue(CsvHeaderMeterId, out object? meterId)
            && record.TryGetValue(CsvHeaderDate, out object? date))
        {
            if (DateTimeExtensions.TryParseDate(date.ToString()!, out LocalDate dateParsed))
            {
                // We need all CSV columns except MeterId and Date 
                IEnumerable<KeyValuePair<string, object>> filtered =
                    record.Where(k => !string.Equals(k.Key, CsvHeaderMeterId) && !string.Equals(k.Key, CsvHeaderDate));

                foreach ((string key, object value) in filtered)
                {
                    if (int.TryParse(value.ToString() ?? "0", out int parsedValue) && DateTimeExtensions.TryParseTime(key, out LocalTime timeParsed))
                    {
                        yield return new MeterReadingValue
                        {
                            MeterId = meterId.ToString()!,
                            Date = dateParsed,
                            Value = parsedValue,
                            Time = timeParsed
                        };
                    }
                    else
                    {
                        LambdaLogger.Log($"Unable to process MeterReading - Either reader value or time is invalid. Value: {value}, Time: {key}");
                    }
                }
            }
            else
            {
                LambdaLogger.Log($"Unable to process CSV record - Invalid Date {date}");
            }
        }
        else
        {
            LambdaLogger.Log($"Unable to process CSV record - Either MeterId or Date is missing. Row: {record}");
        }
    }
}