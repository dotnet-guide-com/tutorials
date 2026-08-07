using ConfigPrecedenceMinimal.Options;

namespace ConfigPrecedenceMinimal.Diagnostics;

public sealed record SafeConfigSnapshot(
    string Environment,
    string ServiceBaseUrl,
    int TimeoutSeconds,
    bool AdvancedSearch,
    string ApiKey,
    bool ApiKeyConfigured)
{
    public static SafeConfigSnapshot Create(
        IHostEnvironment environment,
        AppOptions appOptions,
        FeatureFlagOptions
            featureOptions)
    {
        ArgumentNullException
            .ThrowIfNull(
                environment);

        ArgumentNullException
            .ThrowIfNull(
                appOptions);

        ArgumentNullException
            .ThrowIfNull(
                featureOptions);

        return new SafeConfigSnapshot(
            Environment:
                environment.EnvironmentName,

            ServiceBaseUrl:
                appOptions.ServiceBaseUrl,

            TimeoutSeconds:
                appOptions.TimeoutSeconds,

            AdvancedSearch:
                featureOptions.AdvancedSearch,

            ApiKey:
                "[REDACTED]",

            ApiKeyConfigured:
                !string.IsNullOrWhiteSpace(
                    appOptions.ApiKey));
    }
}