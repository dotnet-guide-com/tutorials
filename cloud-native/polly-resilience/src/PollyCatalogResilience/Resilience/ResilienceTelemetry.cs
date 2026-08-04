using PollyCatalogResilience.Models;

namespace PollyCatalogResilience.Resilience;

public sealed class ResilienceTelemetry
{
    private long _fallbacks;
    private long _timeouts;
    private long _rejections;

    private string? _lastFallbackReason;

    public void RecordFallback(
        string reason)
    {
        Interlocked.Increment(
            ref _fallbacks);

        Volatile.Write(
            ref _lastFallbackReason,
            reason);
    }

    public void RecordTimeout()
    {
        Interlocked.Increment(
            ref _timeouts);
    }

    public void RecordRejection()
    {
        Interlocked.Increment(
            ref _rejections);
    }

    public ResilienceStatus Snapshot() =>
        new(
            Fallbacks:
                Interlocked.Read(
                    ref _fallbacks),

            Timeouts:
                Interlocked.Read(
                    ref _timeouts),

            Rejections:
                Interlocked.Read(
                    ref _rejections),

            LastFallbackReason:
                Volatile.Read(
                    ref _lastFallbackReason)
                ?? "none");
}