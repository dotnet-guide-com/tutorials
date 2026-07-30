using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PasskeyIdentityMinimal.Data;
using PasskeyIdentityMinimal.Endpoints;

var builder = WebApplication.CreateBuilder(args);

// Identity
builder.Services
    .AddIdentityCore<ApplicationUser>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

// Cookie authentication
builder.Services
    .AddAuthentication(IdentityConstants.ApplicationScheme)
    .AddIdentityCookies();

builder.Services.AddAuthorization();

// Passkey options
builder.Services.Configure<IdentityPasskeyOptions>(options =>
{
    var serverDomain = builder.Configuration["Passkeys:ServerDomain"]
        ?? throw new InvalidOperationException(
            "Passkeys:ServerDomain must be configured.");

    options.ServerDomain = serverDomain;
    options.AuthenticatorTimeout = TimeSpan.FromMinutes(3);
    options.ChallengeSize = 32;
    options.UserVerificationRequirement = "preferred";
    options.ResidentKeyRequirement = "required";
});

// Antiforgery
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "RequestVerificationToken";
    options.Cookie.Name = "XSRF-TOKEN";
    options.Cookie.HttpOnly = false;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

// Database
var dbPath = Path.Combine(
    builder.Environment.ContentRootPath, "PasskeyIdentityMinimal.db");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

// Cookie configuration
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.HttpOnly = true;
    options.Events.OnRedirectToLogin = context =>
    {
        context.Response.StatusCode = 401;
        return Task.CompletedTask;
    };
});

var app = builder.Build();

// Create database and seed bootstrap user
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.EnsureCreated();

    var userManager = scope.ServiceProvider
        .GetRequiredService<UserManager<ApplicationUser>>();

    if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Testing"))
    {
        var demoUser = await userManager.FindByEmailAsync("demo@example.com");
        if (demoUser is null)
        {
            demoUser = new ApplicationUser
            {
                UserName = "demo@example.com",
                Email = "demo@example.com"
            };
            var createResult = await userManager.CreateAsync(
                demoUser, "DemoPass123!");

            if (!createResult.Succeeded)
                throw new InvalidOperationException(
                    "Failed to seed bootstrap user: " +
                    string.Join(", ", createResult.Errors.Select(e => e.Description)));
        }
    }
}

app.UseAuthentication();
app.UseAuthorization();

app.UseStaticFiles();

app.MapPasskeyEndpoints();

app.Run();

// Expose for WebApplicationFactory
public partial class Program { }