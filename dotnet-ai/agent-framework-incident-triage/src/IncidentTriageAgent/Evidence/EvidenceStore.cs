using System.Reflection;
using System.Text.Json;

namespace IncidentTriageAgent.Evidence;

/// <summary>
/// Loads deterministic operational evidence from the sample's
/// embedded JSON data files.
///
/// The evidence is intentionally fixed so that tool behavior and
/// automated tests remain repeatable without a live monitoring system.
/// </summary>
public sealed class EvidenceStore
{
    private static readonly JsonSerializerOptions JsonOptions =
        new()
        {
            PropertyNameCaseInsensitive = true
        };

    private readonly IReadOnlyList<EvidenceItem> _items;


    public EvidenceStore()
    {
        List<EvidenceItem> items = [];

        items.AddRange(
            LoadEvidence("services.json"));

        items.AddRange(
            LoadEvidence("deployments.json"));

        _items = items;
    }


    /// <summary>
    /// Returns the newest evidence item of the requested type
    /// for the requested service.
    /// </summary>
    public EvidenceItem? GetLatest(
        string serviceName,
        string evidenceType)
    {
        if (string.IsNullOrWhiteSpace(serviceName) ||
            string.IsNullOrWhiteSpace(evidenceType))
        {
            return null;
        }

        string service =
            Normalize(serviceName);

        string type =
            evidenceType
                .Trim()
                .ToLowerInvariant();

        return _items
            .Where(item =>
                item.Service.Equals(
                    service,
                    StringComparison.OrdinalIgnoreCase) &&
                item.Type.Equals(
                    type,
                    StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(
                item => item.ObservedAt)
            .FirstOrDefault();
    }


    /// <summary>
    /// Returns all evidence currently available for a service.
    /// </summary>
    public IReadOnlyList<EvidenceItem> GetForService(
        string serviceName)
    {
        if (string.IsNullOrWhiteSpace(serviceName))
        {
            return [];
        }

        string service =
            Normalize(serviceName);

        return _items
            .Where(item =>
                item.Service.Equals(
                    service,
                    StringComparison.OrdinalIgnoreCase))
            .OrderBy(
                item => item.ObservedAt)
            .ToList();
    }


    /// <summary>
    /// Loads one embedded JSON evidence file.
    ///
    /// Resource discovery uses the filename suffix rather than
    /// assuming a particular root namespace.
    /// </summary>
    private static IReadOnlyList<EvidenceItem> LoadEvidence(
        string fileName)
    {
        Assembly assembly =
            typeof(EvidenceStore).Assembly;

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
                $"Embedded evidence file '{fileName}' was not found.");
        }

        using Stream stream =
            assembly.GetManifestResourceStream(
                resourceName)
            ?? throw new InvalidOperationException(
                $"Embedded evidence file '{fileName}' could not be opened.");

        List<EvidenceItem>? evidence =
            JsonSerializer.Deserialize<List<EvidenceItem>>(
                stream,
                JsonOptions);

        return evidence
            ?? throw new InvalidOperationException(
                $"Embedded evidence file '{fileName}' contained no valid data.");
    }


    private static string Normalize(
        string serviceName)
    {
        return serviceName
            .Trim()
            .ToLowerInvariant();
    }
}