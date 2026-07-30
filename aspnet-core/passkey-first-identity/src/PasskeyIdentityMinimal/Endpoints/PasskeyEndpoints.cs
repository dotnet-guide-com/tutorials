using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Identity;
using PasskeyIdentityMinimal.Data;

namespace PasskeyIdentityMinimal.Endpoints;

internal static class PasskeyEndpoints
{
    public static void MapPasskeyEndpoints(this WebApplication app)
    {
        // Antiforgery token endpoint
        app.MapGet("/antiforgery/token", (IAntiforgery antiforgery, HttpContext context) =>
        {
            var tokens = antiforgery.GetAndStoreTokens(context);
            return Results.Ok(new { token = tokens.RequestToken });
        });

        // Home page
        app.MapGet("/", () => Results.Redirect("/index.html"));

        // Bootstrap login — anonymous + antiforgery
        app.MapPost("/account/login", async (
            LoginRequest request,
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager) =>
        {
            var user = await userManager.FindByEmailAsync(request.Email);
            if (user is null)
                return Results.Unauthorized();

            var result = await signInManager.PasswordSignInAsync(
                user, request.Password, isPersistent: false, lockoutOnFailure: false);

            return result.Succeeded
                ? Results.StatusCode(204)
                : Results.Unauthorized();
        }).ValidateAntiforgery();

        // Logout — authenticated + antiforgery
        app.MapPost("/account/logout", async (
            SignInManager<ApplicationUser> signInManager) =>
        {
            await signInManager.SignOutAsync();
            return Results.StatusCode(204);
        }).RequireAuthorization().ValidateAntiforgery();

        // Passkey creation options — authenticated + antiforgery
        app.MapPost("/account/passkeys/creation-options", async (
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            HttpContext context) =>
        {
            var user = await userManager.GetUserAsync(context.User);
            if (user is null)
                return Results.Unauthorized();

            var userEntity = new PasskeyUserEntity
            {
                Id = user.Id,
                Name = user.UserName ?? user.Email!,
                DisplayName = user.Email!
            };

            var creationOptionsJson = await signInManager.MakePasskeyCreationOptionsAsync(userEntity);

            return Results.Content(creationOptionsJson, "application/json");
        }).RequireAuthorization().ValidateAntiforgery();

        // Passkey registration — authenticated + antiforgery
        app.MapPost("/account/passkeys/register", async (
            PasskeyCredentialRequest request,
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            HttpContext context) =>
        {
            var user = await userManager.GetUserAsync(context.User);
            if (user is null)
                return Results.Unauthorized();

            var attestationResult = await signInManager.PerformPasskeyAttestationAsync(
                request.CredentialJson);

            if (!attestationResult.Succeeded)
                return Results.BadRequest(new { error = "Passkey attestation failed." });

            await userManager.AddOrUpdatePasskeyAsync(
                user, attestationResult.Passkey);

            return Results.StatusCode(204);
        }).RequireAuthorization().ValidateAntiforgery();

        // Passkey request options — anonymous + antiforgery
        app.MapPost("/account/passkeys/request-options", async (
            PasskeyRequestOptionsRequest request,
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager) =>
        {
            var user = await userManager.FindByEmailAsync(request.Email);
            if (user is null)
                return Results.Json(new { }, statusCode: 200); // Don't reveal account existence

            var requestOptionsJson = await signInManager.MakePasskeyRequestOptionsAsync(user);

            return Results.Content(requestOptionsJson, "application/json");
        }).ValidateAntiforgery();

        // Passkey sign-in — anonymous + antiforgery
        app.MapPost("/account/passkeys/sign-in", async (
            PasskeyCredentialRequest request,
            SignInManager<ApplicationUser> signInManager) =>
        {
            var result = await signInManager.PasskeySignInAsync(request.CredentialJson);

            return result.Succeeded
                ? Results.StatusCode(204)
                : Results.Unauthorized();
        }).ValidateAntiforgery();

        // Passkey list — authenticated, read-only (no antiforgery needed)
        app.MapGet("/account/passkeys", async (
            UserManager<ApplicationUser> userManager,
            HttpContext context) =>
        {
            var user = await userManager.GetUserAsync(context.User);
            if (user is null)
                return Results.Unauthorized();

            var passkeys = await userManager.GetPasskeysAsync(user);

            var result = passkeys.Select(p => new
            {
                name = p.Name,
                credentialId = Convert.ToBase64String(p.CredentialId),
                isBackupEligible = p.IsBackupEligible,
                isBackedUp = p.IsBackedUp
            });

            return Results.Ok(result);
        }).RequireAuthorization();
    }
}

// Request DTOs

internal sealed record LoginRequest(string Email, string Password);

internal sealed record PasskeyCredentialRequest(string CredentialJson);

internal sealed record PasskeyRequestOptionsRequest(string Email);