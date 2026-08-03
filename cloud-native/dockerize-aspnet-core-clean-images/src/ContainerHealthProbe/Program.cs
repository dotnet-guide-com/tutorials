string url =
    Environment.GetEnvironmentVariable(
        "HEALTHCHECK_URL")
    ?? "http://127.0.0.1:8080/health/live";

using var client =
    new HttpClient
    {
        Timeout =
            TimeSpan.FromSeconds(
                3)
    };

try
{
    using HttpResponseMessage response =
        await client.GetAsync(
            url);

    return response.IsSuccessStatusCode
        ? 0
        : 1;
}
catch
{
    return 1;
}