using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ResilientOrdersMinimal.Health;

public static class HealthResponseWriter
{
    public static Task WriteAsync(
        HttpContext context,
        HealthReport report)
    {
        context.Response.ContentType =
            "application/json";

        var response =
            new
            {
                status =
                    report.Status.ToString(),

                durationMilliseconds =
                    report.TotalDuration
                        .TotalMilliseconds,

                checks =
                    report.Entries
                        .OrderBy(
                            entry =>
                                entry.Key)
                        .Select(
                            entry =>
                                new
                                {
                                    name =
                                        entry.Key,

                                    status =
                                        entry.Value.Status
                                            .ToString(),

                                    description =
                                        entry.Value
                                            .Description,

                                    durationMilliseconds =
                                        entry.Value.Duration
                                            .TotalMilliseconds
                                })
            };

        return context.Response
            .WriteAsJsonAsync(
                response,
                cancellationToken:
                    context.RequestAborted);
    }
}