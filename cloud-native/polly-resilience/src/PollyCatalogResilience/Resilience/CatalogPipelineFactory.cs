using System.Threading.RateLimiting;
using Polly;
using Polly.Fallback;
using Polly.RateLimiting;
using Polly.Timeout;
using PollyCatalogResilience.Models;
using PollyCatalogResilience.Services;

namespace PollyCatalogResilience.Resilience;

public sealed class CatalogPipelineFactory(
    CatalogCache cache,
    ResilienceTelemetry telemetry)
{
    public ResiliencePipeline<CatalogSnapshot>
        Create() =>
            new ResiliencePipelineBuilder<
                CatalogSnapshot>()
                .AddFallback(
                    new FallbackStrategyOptions<
                        CatalogSnapshot>
                    {
                        ShouldHandle =
                            new PredicateBuilder<
                                CatalogSnapshot>()
                                .Handle<
                                    HttpRequestException>()
                                .Handle<
                                    TimeoutRejectedException>()
                                .Handle<
                                    RateLimiterRejectedException>(),

                        FallbackAction =
                            arguments =>
                                Outcome
                                    .FromResultAsValueTask(
                                        cache.CreateFallback(
                                            GetReason(
                                                arguments
                                                    .Outcome
                                                    .Exception))),

                        OnFallback =
                            arguments =>
                            {
                                telemetry.RecordFallback(
                                    GetReason(
                                        arguments
                                            .Outcome
                                            .Exception));

                                return default;
                            }
                    })
                .AddTimeout(
                    new TimeoutStrategyOptions
                    {
                        Timeout =
                            TimeSpan
                                .FromMilliseconds(
                                    500),

                        OnTimeout =
                            arguments =>
                            {
                                telemetry
                                    .RecordTimeout();

                                return default;
                            }
                    })
                .AddRateLimiter(
                    new RateLimiterStrategyOptions
                    {
                        DefaultRateLimiterOptions =
                            new ConcurrencyLimiterOptions
                            {
                                PermitLimit =
                                    1,

                                QueueLimit =
                                    0,

                                QueueProcessingOrder =
                                    QueueProcessingOrder
                                        .OldestFirst
                            },

                        OnRejected =
                            arguments =>
                            {
                                telemetry
                                    .RecordRejection();

                                return default;
                            }
                    })
                .Build();

    private static string GetReason(
        Exception? exception) =>
            exception switch
            {
                TimeoutRejectedException =>
                    "timeout",

                RateLimiterRejectedException =>
                    "bulkhead-rejected",

                HttpRequestException =>
                    "dependency-failure",

                _ =>
                    "unknown"
            };
}