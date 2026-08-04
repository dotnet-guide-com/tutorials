using PollyCatalogResilience.Models;

namespace PollyCatalogResilience.Services;

public sealed class CatalogDependency(
    CatalogHoldGate holdGate)
{
    public async ValueTask<CatalogSnapshot>
        GetSnapshotAsync(
            CatalogSimulationMode mode,
            int delayMilliseconds,
            CancellationToken cancellationToken)
    {
        switch (mode)
        {
            case CatalogSimulationMode.Live:
                break;

            case CatalogSimulationMode.Failure:
                throw new HttpRequestException(
                    "The demonstration catalog dependency failed.");

            case CatalogSimulationMode.Slow:
                await Task.Delay(
                    delayMilliseconds,
                    cancellationToken);

                break;

            case CatalogSimulationMode.Hold:
                await holdGate.WaitAsync(
                    cancellationToken);

                break;

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(mode),
                    mode,
                    "Unknown simulation mode.");
        }

        return CreateLiveSnapshot();
    }

    private static CatalogSnapshot
        CreateLiveSnapshot() =>
            new(
                GeneratedAtUtc:
                    DateTimeOffset.UtcNow,

                Source:
                    "live-dependency",

                IsStale:
                    false,

                DegradedReason:
                    null,

                Products:
                [
                    new CatalogProduct(
                        1,
                        "Mechanical Keyboard",
                        85.00m),

                    new CatalogProduct(
                        2,
                        "USB-C Dock",
                        125.00m),

                    new CatalogProduct(
                        3,
                        "Monitor Arm",
                        75.00m)
                ]);
}