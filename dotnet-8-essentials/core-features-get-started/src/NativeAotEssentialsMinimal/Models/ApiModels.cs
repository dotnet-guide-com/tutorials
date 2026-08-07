namespace NativeAotEssentialsMinimal.Models;

public sealed record SampleInfo(
    string Sample,
    string Builder,
    string Json);

public sealed record RuntimeInfo(
    string Framework,
    string Architecture,
    bool DynamicCodeSupported,
    bool DynamicCodeCompiled);

public sealed record EchoRequest(
    string? Message);

public sealed record EchoResponse(
    string Message,
    int Length);

public sealed record ApiError(
    string Code,
    string Message);