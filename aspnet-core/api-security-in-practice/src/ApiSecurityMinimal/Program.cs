using System.Text;
using ApiSecurityMinimal;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// JWT authentication
string? jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException(
        "JWT signing key is not configured. " +
        "Set Jwt:Key via user-secrets or environment variable.");

if (jwtKey.Length < 32)
    throw new InvalidOperationException(
        "JWT signing key must be at least 32 bytes.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.FromSeconds(30)
        };

        options.Events = new JwtBearerEvents
        {
            OnChallenge = context =>
            {
                context.HandleResponse();
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/problem+json";
                return context.Response.WriteAsync(
                    """{"title":"Unauthorized","status":401,"detail":"A valid bearer token is required."}""");
            }
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddSingleton<DemoStore>();
builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();

// Rate limiting
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode =
            StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.ContentType =
            "application/problem+json";
        context.HttpContext.Response.Headers.RetryAfter = "60";
        await context.HttpContext.Response.WriteAsync(
            """{"title":"Too Many Requests","status":429,"detail":"Rate limit exceeded. Try again later."}""",
            cancellationToken);
    };

    // Login policy: 5 requests per minute
    options.AddFixedWindowLimiter("login", opt =>
    {
        opt.PermitLimit = 5;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 0;
    });

    // Per-user policy: 20 requests per minute
    options.AddFixedWindowLimiter("per-user", opt =>
    {
        opt.PermitLimit = 20;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 0;
        opt.AutoReplenishment = true;
    });
});

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

// Health endpoint (anonymous)
app.MapGet("/health", () =>
{
    return Results.Ok(new { status = "healthy" });
})
.AllowAnonymous();

// Login endpoint (rate limited)
app.MapPost("/auth/token", async (
    [FromBody] LoginRequest request,
    DemoStore store,
    IJwtTokenService tokenService,
    HttpContext context) =>
{
    DemoUser? user = store.FindUserByEmail(request.Email);

    if (user is null || user.Password != request.Password)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsync(
            """{"title":"Unauthorized","status":401,"detail":"Invalid email or password."}""");
        return;
    }

    string token = tokenService.CreateToken(user);

    context.Response.ContentType = "application/json";
    await context.Response.WriteAsync(
        $$"""{"accessToken":"{{token}}","tokenType":"Bearer","expiresInSeconds":1800}""");
})
.RequireRateLimiting("login");

// Protected endpoints
var notes = app.MapGroup("/notes")
    .RequireAuthorization()
    .RequireRateLimiting("per-user");

notes.MapGet("/", (DemoStore store, HttpContext context) =>
{
    string userId = context.User.FindFirst(
        System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";
    var userNotes = store.GetNotesByOwner(userId);
    return Results.Ok(userNotes);
});

notes.MapPost("/", (CreateNoteRequest request, DemoStore store, HttpContext context) =>
{
    string userId = context.User.FindFirst(
        System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";
    var note = store.CreateNote(userId, request.Title, request.Body);
    return Results.Created($"/notes/{note.Id}", note);
});

notes.MapDelete("/{noteId}", (string noteId, DemoStore store, HttpContext context) =>
{
    string userId = context.User.FindFirst(
        System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";

    bool deleted = store.DeleteNote(noteId, userId);
    if (!deleted)
    {
        return Results.NotFound(new
        {
            title = "Not Found",
            status = 404,
            detail = "The requested note was not found."
        });
    }

    return Results.NoContent();
});

app.Run();

public partial class Program;