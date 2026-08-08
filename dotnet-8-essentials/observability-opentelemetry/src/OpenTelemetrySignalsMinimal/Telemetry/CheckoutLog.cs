namespace OpenTelemetrySignalsMinimal.Telemetry;

internal static partial class CheckoutLog
{
    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Information,
        Message = "Checkout accepted for channel {Channel} with {ItemCount} item(s).")]
    public static partial void CheckoutAccepted(
        this ILogger logger,
        string channel,
        int itemCount);

    [LoggerMessage(
        EventId = 1002,
        Level = LogLevel.Warning,
        Message = "Checkout rejected with reason {Reason}.")]
    public static partial void CheckoutRejected(
        this ILogger logger,
        string reason);
}