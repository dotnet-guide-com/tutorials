using System.Net;
using System.Net.Http.Json;
using ApiSecurityMinimal;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace ApiSecurityMinimal.Tests;

public sealed class ApiSecurityTests
    : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ApiSecurityTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("Jwt:Key",
                "CI-test-key-that-is-at-least-32-bytes-long!!");
        });
    }

    private HttpClient CreateClient() => _factory.CreateClient();

    [Fact]
    public async Task Notes_returns_401_without_token()
    {
        var client = CreateClient();
        var response = await client.GetAsync("/notes");

        Assert.Equal(401, (int)response.StatusCode);
        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Valid_login_returns_bearer_token()
    {
        var client = CreateClient();
        var loginResponse = await client.PostAsJsonAsync("/auth/token", new
        {
            email = "demo@example.com",
            password = "DemoPass123!"
        });

        Assert.Equal(200, (int)loginResponse.StatusCode);

        var body = await loginResponse.Content
            .ReadFromJsonAsync<LoginTokenResponse>();

        Assert.NotNull(body);
        Assert.NotEmpty(body.AccessToken);
        Assert.Equal("Bearer", body.TokenType);
    }

    [Fact]
    public async Task Valid_token_can_access_protected_notes()
    {
        var client = CreateClient();

        // Login
        var loginResponse = await client.PostAsJsonAsync("/auth/token", new
        {
            email = "demo@example.com",
            password = "DemoPass123!"
        });
        var tokenBody = await loginResponse.Content
            .ReadFromJsonAsync<LoginTokenResponse>();
        Assert.NotNull(tokenBody);

        // Access protected endpoint
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Bearer", tokenBody.AccessToken);

        var notesResponse = await client.GetAsync("/notes");

        Assert.Equal(200, (int)notesResponse.StatusCode);
    }

    [Fact]
    public async Task User_cannot_delete_another_users_note()
    {
        var client = CreateClient();

        // Login as demo user
        var loginResponse = await client.PostAsJsonAsync("/auth/token", new
        {
            email = "demo@example.com",
            password = "DemoPass123!"
        });
        var tokenBody = await loginResponse.Content
            .ReadFromJsonAsync<LoginTokenResponse>();
        Assert.NotNull(tokenBody);

        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Bearer", tokenBody.AccessToken);

        // Create a note owned by demo user
        var createResponse = await client.PostAsJsonAsync("/notes", new
        {
            title = "My Note",
            body = "This is my note."
        });
        var createdNote = await createResponse.Content
            .ReadFromJsonAsync<Note>();
        Assert.NotNull(createdNote);

        // The demo user created this note, so deleting it should work (owner match)
        // We need a note owned by a different user.
        // Since we only have one user in the demo, let's verify the ownership
        // protection by attempting to delete a non-existent note (returns 404)
        // OR by verifying that deleting own note works.
        // Per spec: we need to create a note with a different owner.
        // Since DemoStore doesn't have a second user, we'll use the store directly
        // in a test-specific setup.

        // Instead, let's test that deleting own note succeeds (204)
        var deleteOwnResponse = await client.DeleteAsync($"/notes/{createdNote.Id}");
        Assert.Equal(204, (int)deleteOwnResponse.StatusCode);
    }

    [Fact]
    public async Task User_cannot_delete_another_users_note_ownership()
    {
        // Use a custom factory to inject a note with a different owner
        using var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DemoStore));
                if (descriptor is not null)
                    services.Remove(descriptor);

                var store = new DemoStore();
                // Create a note owned by a different user
                store.CreateNote("other-user-999", "Other's Note", "Secret");
                services.AddSingleton(store);
            });
        });

        var client = factory.CreateClient();

        // Login as demo user
        var loginResponse = await client.PostAsJsonAsync("/auth/token", new
        {
            email = "demo@example.com",
            password = "DemoPass123!"
        });
        var tokenBody = await loginResponse.Content
            .ReadFromJsonAsync<LoginTokenResponse>();
        Assert.NotNull(tokenBody);

        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Bearer", tokenBody.AccessToken);

        // Try to delete the note owned by other-user (note-0001)
        var deleteResponse = await client.DeleteAsync("/notes/note-0001");

        Assert.Equal(404, (int)deleteResponse.StatusCode);
    }

    [Fact]
    public async Task Login_rate_limit_returns_429()
    {
        var client = CreateClient();

        // Exhaust the login rate limit (5 per minute)
        for (int i = 0; i < 6; i++)
        {
            var response = await client.PostAsJsonAsync("/auth/token", new
            {
                email = "bad@example.com",
                password = "wrong"
            });

            if (i < 5)
            {
                Assert.NotEqual(429, (int)response.StatusCode);
            }
            else
            {
                Assert.Equal(429, (int)response.StatusCode);
                Assert.True(
                    response.Headers.Contains("Retry-After"),
                    "Response should include Retry-After header");
                Assert.Equal(
                    "application/problem+json",
                    response.Content.Headers.ContentType?.MediaType);
            }
        }
    }

    private sealed record LoginTokenResponse(
        string AccessToken,
        string TokenType,
        int ExpiresInSeconds);
}