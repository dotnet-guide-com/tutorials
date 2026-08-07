using ConfigPrecedenceMinimal.Diagnostics;
using ConfigPrecedenceMinimal.Options;
using ConfigPrecedenceMinimal.Validation;
using Microsoft.Extensions.Options;

WebApplicationBuilder builder =
    WebApplication.CreateBuilder(
        args);

// WebApplication.CreateBuilder already adds:
// appsettings.json
// appsettings.{Environment}.json
// User Secrets in Development when UserSecretsId exists
// unprefixed environment variables
// command-line arguments
//
// Add an application-specific environment-variable provider after the defaults.
// The CFGPLAY_ prefix is stripped before keys enter IConfiguration.
builder.Configuration
    .AddEnvironmentVariables(
        prefix:
            "CFGPLAY_");

// Re-add command-line arguments so they remain the highest-priority
// application override after the custom prefixed provider.
builder.Configuration
    .AddCommandLine(
        args);

builder.Services
    .AddOptions<AppOptions>()
    .BindConfiguration(
        AppOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services
    .AddSingleton<
        IValidateOptions<AppOptions>,
        AppOptionsValidator>();

builder.Services
    .AddOptions<FeatureFlagOptions>()
    .BindConfiguration(
        FeatureFlagOptions.SectionName);

WebApplication app =
    builder.Build();

app.MapGet(
    "/",
    (
        IHostEnvironment environment,
        IOptions<AppOptions>
            appOptions,
        IOptionsSnapshot<
            FeatureFlagOptions>
            featureOptions) =>
        TypedResults.Ok(
            new
            {
                sample =
                    "typed-config-precedence",

                environment =
                    environment
                        .EnvironmentName,

                serviceBaseUrl =
                    appOptions
                        .Value
                        .ServiceBaseUrl,

                timeoutSeconds =
                    appOptions
                        .Value
                        .TimeoutSeconds,

                advancedSearch =
                    featureOptions
                        .Value
                        .AdvancedSearch,

                apiKeyConfigured =
                    !string
                        .IsNullOrWhiteSpace(
                            appOptions
                                .Value
                                .ApiKey)
            }));

if (app.Environment
    .IsDevelopment())
{
    app.MapGet(
        "/dev/config",
        (
            IHostEnvironment
                environment,
            IOptions<AppOptions>
                appOptions,
            IOptionsSnapshot<
                FeatureFlagOptions>
                featureOptions) =>
            TypedResults.Ok(
                SafeConfigSnapshot
                    .Create(
                        environment,
                        appOptions.Value,
                        featureOptions
                            .Value)));
}

app.Run();

public partial class Program;