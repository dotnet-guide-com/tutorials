var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.MapGet("/", () => "AspireOrchestrationMinimal.Api is running.");

app.MapGet("/message", () => Results.Ok(new
{
    message = "Hello from the Aspire-managed API",
    service = "api"
}));

app.Run();