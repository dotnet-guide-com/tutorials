using Microsoft.AspNetCore.Antiforgery;

namespace PasskeyIdentityMinimal.Endpoints;

internal static class AntiforgeryEndpointExtensions
{
    public static TBuilder ValidateAntiforgery<TBuilder>(
        this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder =>
        builder.AddEndpointFilter(
            async (context, next) =>
            {
                var antiforgery =
                    context.HttpContext
                        .RequestServices
                        .GetRequiredService<IAntiforgery>();

                try
                {
                    await antiforgery
                        .ValidateRequestAsync(
                            context.HttpContext);
                }
                catch (
                    AntiforgeryValidationException)
                {
                    return Results.BadRequest(
                        new
                        {
                            error =
                                "Antiforgery validation failed."
                        });
                }

                return await next(context);
            });
}
