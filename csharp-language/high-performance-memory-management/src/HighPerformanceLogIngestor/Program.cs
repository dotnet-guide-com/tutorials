using System.Text;
using HighPerformanceLogIngestor.Models;
using HighPerformanceLogIngestor.Parsing;
using HighPerformanceLogIngestor.Pipelines;

const string SampleData =
    """
    1785924000|2|1001|Cache warmed
    1785924001|4|1002|Request failed
    not-a-timestamp|2|1003|Malformed timestamp
    1785924002|1|1004|Worker started
    """;

byte[] utf8Data =
    Encoding.UTF8.GetBytes(
        SampleData);

using var stream =
    new MemoryStream(
        utf8Data,
        writable:
            false);

var parser =
    new Utf8LogLineParser();

var decoder =
    new LogLineDecoder(
        parser);

var ingestor =
    new LogIngestor(
        decoder);

List<LogEntry> accepted =
[
];

LogIngestionResult result =
    await ingestor.IngestAsync(
        stream,
        accepted.Add);

string eventIds =
    string.Join(
        ", ",
        accepted.Select(
            entry =>
                entry.EventId));

string levels =
    string.Join(
        ", ",
        accepted
            .GroupBy(
                entry =>
                    entry.Level)
            .OrderBy(
                group =>
                    group.Key)
            .Select(
                group =>
                    $"{group.Key}={group.Count()}"));

Console.WriteLine(
    "High-Performance UTF-8 Log Ingestor");

Console.WriteLine(
    $"Lines: total={result.TotalLines}, valid={result.ValidLines}, invalid={result.InvalidLines}");

Console.WriteLine(
    $"Accepted event IDs: {eventIds}");

Console.WriteLine(
    $"Levels: {levels}");

Console.WriteLine(
    $"First message: {accepted[0].Message}");

Console.WriteLine(
    $"Last message: {accepted[^1].Message}");