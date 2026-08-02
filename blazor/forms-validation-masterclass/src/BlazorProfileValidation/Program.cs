using BlazorProfileValidation.Components;
using BlazorProfileValidation.Services;
using BlazorProfileValidation.Validation;
using FluentValidation;

var builder =
    WebApplication.CreateBuilder(args);

builder.Services
    .AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddScoped<
    ProfileService>();

builder.Services
    .AddValidatorsFromAssemblyContaining<
        ProfileValidator>(
        ServiceLifetime.Transient);

var app =
    builder.Build();

app.UseStatusCodePagesWithReExecute(
    "/not-found",
    createScopeForStatusCodePages: true);

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

public partial class Program
{
}