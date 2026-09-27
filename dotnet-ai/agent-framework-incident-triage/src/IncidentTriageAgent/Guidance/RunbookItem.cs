namespace IncidentTriageAgent.Guidance;

/// <summary>
/// Represents operational reference guidance.
///
/// A runbook is not observed incident evidence.
/// It describes investigation steps that engineers may follow.
/// </summary>
public sealed record RunbookItem(
    string Id,
    string Service,
    string Title,
    string[] RecommendedChecks,
    string Caution);