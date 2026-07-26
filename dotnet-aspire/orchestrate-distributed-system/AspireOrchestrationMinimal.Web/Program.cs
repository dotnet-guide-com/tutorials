using Microsoft.Extensions.ServiceDiscovery;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddServiceDiscovery();

builder.Services.AddHttpClient("api", client =>
{
    client.BaseAddress = new Uri("https+http://api");
})
.AddServiceDiscovery();

var app = builder.Build();

app.MapGet("/", async (IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient("api");

    string apiMessage;

    try
    {
        apiMessage = await client.GetStringAsync("/message");
    }
    catch (Exception ex)
    {
        apiMessage = $"API unavailable: {ex.Message}";
    }

    var html = $"""
    <!DOCTYPE html>
    <html>
    <head>
        <meta charset="utf-8" />
        <title>Aspire orchestration sample</title>
    </head>
    <body>
        <h1>Aspire orchestration sample</h1>
        <p>Web resource: running</p>
        <p>API response: {apiMessage}</p>
    </body>
    </html>
    """;

    return Results.Content(html, "text/html");
});

app.Run();