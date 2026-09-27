namespace IncidentTriageAgent.Evidence;

/// <summary>
/// Represents one deterministic piece of operational evidence.
///
/// Evidence items contain observed facts only.
/// They must not contain model-generated conclusions or inferred facts.
/// </summary>
public sealed record EvidenceItem(
    string Id,
    string Type,
    string Service,
    DateTimeOffset ObservedAt,
    IReadOnlyDictionary<string, string> Facts)
{
    /// <summary>
    /// Returns a fact value when the field exists.
    /// Missing fields remain missing rather than being inferred.
    /// </summary>
    public string? GetFact(string key)
    {
        return Facts.TryGetValue(key, out string? value)
            ? value
            : null;
    }
}