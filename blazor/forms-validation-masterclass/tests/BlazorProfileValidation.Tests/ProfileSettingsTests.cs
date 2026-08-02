using System.Net;
using BlazorProfileValidation.Components.Pages;
using BlazorProfileValidation.Services;
using BlazorProfileValidation.Validation;
using Bunit;
using FluentValidation;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace BlazorProfileValidation.Tests;

public sealed class ProfileSettingsTests
{
    [Fact]
    public void
        Form_renders_saved_profile_without_dirty_state()
    {
        using BunitContext context =
            CreateContext();

        var cut =
            context.Render<
                ProfileSettings>();

        Assert.Equal(
            "dotnet_reader",
            cut.Find(
                    "#profile-username")
                .GetAttribute(
                    "value"));

        Assert.Equal(
            "reader@example.com",
            cut.Find(
                    "#profile-email")
                .GetAttribute(
                    "value"));

        Assert.Empty(
            cut.FindAll(
                "[data-testid='save-bar']"));
    }

    [Fact]
    public void
        DataAnnotations_error_prevents_backend_save()
    {
        using BunitContext context =
            CreateContext();

        ProfileService service =
            context.Services
                .GetRequiredService<
                    ProfileService>();

        var cut =
            context.Render<
                ProfileSettings>();

        cut.Find(
                "#profile-username")
            .Input(
                "x");

        cut.Find(
                "form")
            .Submit();

        cut.WaitForAssertion(
            () =>
            {
                Assert.Contains(
                    "Username must contain between 3 and 30 characters.",
                    cut.Markup,
                    StringComparison.Ordinal);

                Assert.Equal(
                    0,
                    service.SaveAttempts);
            });
    }

    [Fact]
    public void
        FluentValidation_cross_field_error_prevents_save()
    {
        using BunitContext context =
            CreateContext();

        ProfileService service =
            context.Services
                .GetRequiredService<
                    ProfileService>();

        var cut =
            context.Render<
                ProfileSettings>();

        cut.Find(
                "#profile-display-name")
            .Input(
                "dotnet_reader");

        cut.Find(
                "form")
            .Submit();

        cut.WaitForAssertion(
            () =>
            {
                Assert.Contains(
                    "Display name must differ from the username.",
                    cut.Markup,
                    StringComparison.Ordinal);

                Assert.Equal(
                    0,
                    service.SaveAttempts);
            });

        cut.Find(
                "#profile-username")
            .Input(
                "different_user");

        cut.WaitForAssertion(
            () =>
            {
                Assert.DoesNotContain(
                    "Display name must differ from the username.",
                    cut.Markup,
                    StringComparison.Ordinal);

                Assert.Equal(
                    0,
                    service.SaveAttempts);
            });

        cut.Find(
                "#profile-username")
            .Input(
                "dotnet_reader");

        cut.WaitForAssertion(
            () =>
            {
                Assert.Contains(
                    "Display name must differ from the username.",
                    cut.Markup,
                    StringComparison.Ordinal);

                Assert.Equal(
                    0,
                    service.SaveAttempts);
            });
    }

    [Fact]
    public void
        Backend_errors_map_to_their_fields()
    {
        using BunitContext context =
            CreateContext();

        ProfileService service =
            context.Services
                .GetRequiredService<
                    ProfileService>();

        var cut =
            context.Render<
                ProfileSettings>();

        cut.Find(
                "#profile-username")
            .Input(
                "reserved");

        cut.Find(
                "#profile-email")
            .Input(
                "reader@blocked.example");

        cut.Find(
                "form")
            .Submit();

        cut.WaitForAssertion(
            () =>
            {
                Assert.Contains(
                    "This username is reserved by the profile service.",
                    cut.Markup,
                    StringComparison.Ordinal);

                Assert.Contains(
                    "This email domain is blocked by the profile service.",
                    cut.Markup,
                    StringComparison.Ordinal);

                Assert.Equal(
                    1,
                    service.SaveAttempts);
            });
    }

    [Fact]
    public void
        Editing_a_field_clears_only_its_backend_error()
    {
        using BunitContext context =
            CreateContext();

        var cut =
            context.Render<
                ProfileSettings>();

        cut.Find(
                "#profile-username")
            .Input(
                "reserved");

        cut.Find(
                "#profile-email")
            .Input(
                "reader@blocked.example");

        cut.Find(
                "form")
            .Submit();

        cut.WaitForAssertion(
            () =>
                Assert.Contains(
                    "This username is reserved by the profile service.",
                    cut.Markup,
                    StringComparison.Ordinal));

        cut.Find(
                "#profile-username")
            .Input(
                "available_user");

        cut.WaitForAssertion(
            () =>
            {
                Assert.DoesNotContain(
                    "This username is reserved by the profile service.",
                    cut.Markup,
                    StringComparison.Ordinal);

                Assert.Contains(
                    "This email domain is blocked by the profile service.",
                    cut.Markup,
                    StringComparison.Ordinal);
            });
    }

    [Fact]
    public void
        Dirty_state_and_discard_restore_saved_values()
    {
        using BunitContext context =
            CreateContext();

        var cut =
            context.Render<
                ProfileSettings>();

        cut.Find(
                "#profile-display-name")
            .Input(
                "Changed display name");

        cut.WaitForAssertion(
            () =>
                Assert.Single(
                    cut.FindAll(
                        "[data-testid='save-bar']")));

        cut.Find(
                "[data-action='discard']")
            .Click();

        cut.WaitForAssertion(
            () =>
            {
                Assert.Equal(
                    "DOTNET Reader",
                    cut.Find(
                            "#profile-display-name")
                        .GetAttribute(
                            "value"));

                Assert.Empty(
                    cut.FindAll(
                        "[data-testid='save-bar']"));
            });
    }

    [Fact]
    public void
        Successful_save_clears_dirty_state()
    {
        using BunitContext context =
            CreateContext();

        ProfileService service =
            context.Services
                .GetRequiredService<
                    ProfileService>();

        var cut =
            context.Render<
                ProfileSettings>();

        cut.Find(
                "#profile-display-name")
            .Input(
                "Updated Reader");

        cut.Find(
                "[data-action='save']")
            .Click();

        cut.WaitForAssertion(
            () =>
            {
                Assert.Contains(
                    "Profile saved successfully.",
                    cut.Find(
                            "[data-testid='status-message']")
                        .TextContent,
                    StringComparison.Ordinal);

                Assert.Empty(
                    cut.FindAll(
                        "[data-testid='save-bar']"));

                Assert.Equal(
                    "Updated Reader",
                    service.LastSavedProfile
                        .DisplayName);
            });
    }

    [Fact]
    public void
        Invalid_field_exposes_accessible_error_metadata()
    {
        using BunitContext context =
            CreateContext();

        var cut =
            context.Render<
                ProfileSettings>();

        cut.Find(
                "#profile-username")
            .Input(
                "x");

        cut.Find(
                "form")
            .Submit();

        cut.WaitForAssertion(
            () =>
            {
                var input =
                    cut.Find(
                        "#profile-username");

                Assert.Equal(
                    "true",
                    input.GetAttribute(
                        "aria-invalid"));

                Assert.Equal(
                    "profile-username-error",
                    input.GetAttribute(
                        "aria-describedby"));

                Assert.Contains(
                    "Username must contain between 3 and 30 characters.",
                    cut.Find(
                            "#profile-username-error")
                        .TextContent,
                    StringComparison.Ordinal);
            });
    }

    [Fact]
    public async Task
        Unknown_path_returns_custom_not_found_page()
    {
        using var factory =
            new WebApplicationFactory<
                Program>();

        using HttpClient client =
            factory.CreateClient(
                new WebApplicationFactoryClientOptions
                {
                    AllowAutoRedirect =
                        false
                });

        CancellationToken ct =
            Xunit.TestContext.Current
                .CancellationToken;

        HttpResponseMessage response =
            await client.GetAsync(
                "/this-page-does-not-exist",
                ct);

        string body =
            await response.Content
                .ReadAsStringAsync(
                    ct);

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);

        Assert.Contains(
            "Page not found",
            body,
            StringComparison.Ordinal);
    }

    private static BunitContext
        CreateContext()
    {
        var context =
            new BunitContext();

        context.Services.AddScoped<
            ProfileService>();

        context.Services
            .AddValidatorsFromAssemblyContaining<
                ProfileValidator>(
                ServiceLifetime.Transient);

        return context;
    }
}