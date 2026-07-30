using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PasskeyIdentityMinimal.Data;
using Xunit;

namespace PasskeyIdentityMinimal.Tests;

public sealed class PasskeyEndpointTests(PasskeyIdentityFactory factory)
    : IClassFixture<PasskeyIdentityFactory>
{
    [Fact]
    public async Task Creation_options_requires_authenticated_user()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsync(
            "/account/passkeys/creation-options", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Register_passkey_requires_authenticated_user()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsync(
            "/account/passkeys/register", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Passkey_request_options_rejects_missing_antiforgery_token()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/account/passkeys/request-options",
            new { email = "demo@example.com" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Development_user_can_sign_in_with_password()
    {
        var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions
                { HandleCookies = true });

        // Get antiforgery token first
        var antiforgeryResponse = await client.GetAsync("/antiforgery/token");
        var tokens = await antiforgeryResponse.Content
            .ReadFromJsonAsync<AntiforgeryTokenResponse>();

        var request = new HttpRequestMessage(HttpMethod.Post, "/account/login")
        {
            Content = JsonContent.Create(new
            {
                email = "demo@example.com",
                password = "DemoPass123!"
            })
        };
        request.Headers.Add("RequestVerificationToken", tokens!.Token);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.True(response.Headers.Contains("Set-Cookie"));
    }

    [Fact]
    public async Task Authenticated_user_can_list_passkeys()
    {
        var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions
                { HandleCookies = true });

        // Sign in first
        var antiforgeryResponse = await client.GetAsync("/antiforgery/token");
        var tokens = await antiforgeryResponse.Content
            .ReadFromJsonAsync<AntiforgeryTokenResponse>();

        var loginRequest = new HttpRequestMessage(HttpMethod.Post, "/account/login")
        {
            Content = JsonContent.Create(new
            {
                email = "demo@example.com",
                password = "DemoPass123!"
            })
        };
        loginRequest.Headers.Add("RequestVerificationToken", tokens!.Token);
        var loginResponse = await client.SendAsync(loginRequest);
        Assert.Equal(HttpStatusCode.NoContent, loginResponse.StatusCode);

        // List passkeys (cookies should be handled automatically)
        var passkeysResponse = await client.GetAsync("/account/passkeys");

        Assert.Equal(HttpStatusCode.OK, passkeysResponse.StatusCode);
        var passkeys = await passkeysResponse.Content
            .ReadFromJsonAsync<List<PasskeyListEntry>>();

        Assert.NotNull(passkeys);
        Assert.Empty(passkeys);
    }

    [Fact]
    public async Task Passkey_server_domain_is_configured()
    {
        // Verify by checking the options directly
        using var scope = factory.Services.CreateScope();
        var options = scope.ServiceProvider
            .GetRequiredService<IOptions<IdentityPasskeyOptions>>();

        Assert.Equal("localhost", options.Value.ServerDomain);
    }
}

internal sealed record AntiforgeryTokenResponse(string Token);

internal sealed record PasskeyListEntry(
    string? Name,
    string CredentialId,
    bool IsBackupEligible,
    bool IsBackedUp);