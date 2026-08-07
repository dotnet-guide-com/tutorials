using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Http.Json;
using ConfigPrecedenceMinimal.Diagnostics;
using ConfigPrecedenceMinimal.Options;
using ConfigPrecedenceMinimal.Validation;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ConfigPrecedenceMinimal.Tests;

public sealed class ConfigurationTests
{
    private const string TestApiKey =
        "test-api-key-never-return";

    [Fact]
    public void
        Provider_order_gives_command_line_final_precedence()
    {
        const string variableName =
            "DOTNET_GUIDE_CFGTEST_App__TimeoutSeconds";

        Environment.SetEnvironmentVariable(
            variableName,
            "30");

        try
        {
            var configuration =
                new ConfigurationManager();

            configuration
                .AddInMemoryCollection(
                    new Dictionary<
                        string,
                        string?>
                    {
                        ["App:TimeoutSeconds"] =
                            "10"
                    });

            configuration
                .AddEnvironmentVariables(
                    "DOTNET_GUIDE_CFGTEST_");

            configuration
                .AddCommandLine(
                    [
                        "--App:TimeoutSeconds=40"
                    ]);

            Assert.Equal(
                "40",
                configuration[
                    "App:TimeoutSeconds"]);
        }
        finally
        {
            Environment
                .SetEnvironmentVariable(
                    variableName,
                    null);
        }
    }

    [Fact]
    public void
        Data_annotations_reject_short_api_key()
    {
        var options =
            new AppOptions
            {
                ServiceBaseUrl =
                    "https://api.example.test",

                TimeoutSeconds =
                    10,

                RequireHttps =
                    true,

                ApiKey =
                    "short"
            };

        var context =
            new ValidationContext(
                options);

        List<ValidationResult>
            results =
            [
            ];

        bool valid =
            Validator.TryValidateObject(
                options,
                context,
                results,
                validateAllProperties:
                    true);

        Assert.False(
            valid);

        Assert.Contains(
            results,
            result =>
                result.MemberNames
                    .Contains(
                        nameof(
                            AppOptions
                                .ApiKey)));
    }

    [Fact]
    public void
        Custom_validator_enforces_https_when_required()
    {
        var validator =
            new AppOptionsValidator();

        ValidateOptionsResult result =
            validator.Validate(
                name:
                    null,

                options:
                    new AppOptions
                    {
                        ServiceBaseUrl =
                            "http://api.example.test",

                        TimeoutSeconds =
                            10,

                        RequireHttps =
                            true,

                        ApiKey =
                            TestApiKey
                    });

        Assert.True(
            result.Failed);

        Assert.Contains(
            result.Failures,
            failure =>
                failure.Contains(
                    "HTTPS",
                    StringComparison
                        .Ordinal));
    }

    [Fact]
    public void
        Custom_validator_rejects_placeholder_api_key()
    {
        var validator =
            new AppOptionsValidator();

        ValidateOptionsResult result =
            validator.Validate(
                name:
                    null,

                options:
                    new AppOptions
                    {
                        ServiceBaseUrl =
                            "https://api.example.test",

                        TimeoutSeconds =
                            10,

                        RequireHttps =
                            true,

                        ApiKey =
                            "CHANGE_ME"
                    });

        Assert.True(
            result.Failed);

        Assert.Contains(
            result.Failures,
            failure =>
                failure.Contains(
                    "placeholder",
                    StringComparison
                        .OrdinalIgnoreCase));
    }

    [Fact]
    public async Task
        Root_returns_resolved_values_without_secret()
    {
        await using var factory =
            CreateFactory(
                environment:
                    "Development",

                overrides:
                    new Dictionary<
                        string,
                        string?>
                    {
                        ["App:ApiKey"] =
                            TestApiKey,

                        ["App:TimeoutSeconds"] =
                            "33",

                        ["Features:AdvancedSearch"] =
                            "false"
                    });

        HttpClient client =
            factory.CreateClient();

        HttpResponseMessage response =
            await client.GetAsync(
                "/",
                TestContext.Current
                    .CancellationToken);

        response
            .EnsureSuccessStatusCode();

        string body =
            await response.Content
                .ReadAsStringAsync(
                    TestContext.Current
                        .CancellationToken);

        Assert.Contains(
            "\"timeoutSeconds\":33",
            body,
            StringComparison.Ordinal);

        Assert.Contains(
            "\"advancedSearch\":false",
            body,
            StringComparison.Ordinal);

        Assert.Contains(
            "\"apiKeyConfigured\":true",
            body,
            StringComparison.Ordinal);

        Assert.DoesNotContain(
            TestApiKey,
            body,
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task
        Development_diagnostics_is_allowlisted_and_redacted()
    {
        await using var factory =
            CreateFactory(
                environment:
                    "Development",

                overrides:
                    ValidOverrides());

        HttpClient client =
            factory.CreateClient();

        SafeConfigSnapshot? snapshot =
            await client
                .GetFromJsonAsync<
                    SafeConfigSnapshot>(
                        "/dev/config",
                        TestContext.Current
                            .CancellationToken);

        Assert.NotNull(
            snapshot);

        Assert.Equal(
            "Development",
            snapshot.Environment);

        Assert.Equal(
            "[REDACTED]",
            snapshot.ApiKey);

        Assert.True(
            snapshot.ApiKeyConfigured);

        string raw =
            await client.GetStringAsync(
                "/dev/config",
                TestContext.Current
                    .CancellationToken);

        Assert.DoesNotContain(
            TestApiKey,
            raw,
            StringComparison.Ordinal);

        Assert.DoesNotContain(
            "ConfigurationRoot",
            raw,
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task
        Production_does_not_register_dev_diagnostics()
    {
        await using var factory =
            CreateFactory(
                environment:
                    "Production",

                overrides:
                    ValidOverrides());

        HttpClient client =
            factory.CreateClient(
                new
                    WebApplicationFactoryClientOptions
                {
                    AllowAutoRedirect =
                        false
                });

        HttpResponseMessage response =
            await client.GetAsync(
                "/dev/config",
                TestContext.Current
                    .CancellationToken);

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    [Fact]
    public void
        Missing_required_secret_fails_application_startup()
    {
        using var factory =
            CreateFactory(
                environment:
                    "Production",

                overrides:
                    new Dictionary<
                        string,
                        string?>
                    {
                        ["App:ApiKey"] =
                            "",

                        ["App:ServiceBaseUrl"] =
                            "https://api.example.test",

                        ["App:RequireHttps"] =
                            "true"
                    });

        Exception exception =
            Assert.ThrowsAny<
                Exception>(
                    () =>
                        factory.CreateClient());

        Assert.Contains(
            "ApiKey",
            exception.ToString(),
            StringComparison
                .OrdinalIgnoreCase);
    }

    private static
        Dictionary<string, string?>
        ValidOverrides() =>
            new()
            {
                ["App:ApiKey"] =
                    TestApiKey,

                ["App:ServiceBaseUrl"] =
                    "https://api.example.test",

                ["App:TimeoutSeconds"] =
                    "25",

                ["App:RequireHttps"] =
                    "true",

                ["Features:AdvancedSearch"] =
                    "true"
            };

    private static
        ConfigFactory
        CreateFactory(
            string environment,
            Dictionary<
                string,
                string?>
                overrides) =>
            new(
                environment,
                overrides);

    private sealed class ConfigFactory(
        string environment,
        Dictionary<
            string,
            string?>
            overrides) :
        WebApplicationFactory<Program>
    {
        protected override void
            ConfigureWebHost(
                IWebHostBuilder builder)
        {
            builder.UseEnvironment(
                environment);

            builder.ConfigureAppConfiguration(
                (
                    _,
                    configuration) =>
                    configuration
                        .AddInMemoryCollection(
                            overrides));
        }
    }
}