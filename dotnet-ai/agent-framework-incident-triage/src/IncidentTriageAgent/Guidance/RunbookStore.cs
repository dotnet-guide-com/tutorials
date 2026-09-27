using System.Reflection;
using System.Text.Json;

namespace IncidentTriageAgent.Guidance;

/// <summary>
/// Loads deterministic runbook guidance from the
/// sample's embedded JSON data file.
/// </summary>
public sealed class RunbookStore
{
    private static readonly JsonSerializerOptions JsonOptions =
        new()
        {
            PropertyNameCaseInsensitive = true
        };

    private readonly IReadOnlyList<RunbookItem> _runbooks;


    public RunbookStore()
    {
        _runbooks =
            LoadRunbooks("runbooks.json");
    }


    /// <summary>
    /// Returns the runbook for the requested service.
    /// </summary>
    public RunbookItem? GetForService(
        string serviceName)
    {
        if (string.IsNullOrWhiteSpace(serviceName))
        {
            return null;
        }

        string service =
            serviceName
                .Trim()
                .ToLowerInvariant();

        return _runbooks
            .FirstOrDefault(runbook =>
                runbook.Service.Equals(
                    service,
                    StringComparison.OrdinalIgnoreCase));
    }


    private static IReadOnlyList<RunbookItem> LoadRunbooks(
        string fileName)
    {
        Assembly assembly =
            typeof(RunbookStore).Assembly;

        string expectedSuffix =
            $".Data.{fileName}";

        string? resourceName =
            assembly
                .GetManifestResourceNames()
                .SingleOrDefault(name =>
                    name.EndsWith(
                        expectedSuffix,
                        StringComparison.OrdinalIgnoreCase));

        if (resourceName is null)
        {
            throw new InvalidOperationException(
                $"Embedded runbook file '{fileName}' was not found.");
        }

        using Stream stream =
            assembly.GetManifestResourceStream(
                resourceName)
            ?? throw new InvalidOperationException(
                $"Embedded runbook file '{fileName}' could not be opened.");

        List<RunbookItem>? runbooks =
            JsonSerializer.Deserialize<List<RunbookItem>>(
                stream,
                JsonOptions);

        return runbooks
            ?? throw new InvalidOperationException(
                $"Embedded runbook file '{fileName}' contained no valid data.");
    }
}