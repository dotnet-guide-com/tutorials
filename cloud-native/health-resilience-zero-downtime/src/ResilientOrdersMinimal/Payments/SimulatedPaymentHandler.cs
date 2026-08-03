using System.Globalization;
using System.Net;
using System.Net.Http.Json;

namespace ResilientOrdersMinimal.Payments;

public sealed class SimulatedPaymentHandler(
    PaymentSimulationState state) :
    HttpMessageHandler
{
    protected override Task<
        HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
    {
        cancellationToken
            .ThrowIfCancellationRequested();

        string operationId =
            ReadRequiredHeader(
                request,
                "X-Demo-Operation-Id");

        int failuresBeforeSuccess =
            int.Parse(
                ReadRequiredHeader(
                    request,
                    "X-Demo-Failures-Before-Success"),
                CultureInfo.InvariantCulture);

        int failureStatusCode =
            int.Parse(
                ReadRequiredHeader(
                    request,
                    "X-Demo-Failure-Status"),
                CultureInfo.InvariantCulture);

        int attempt =
            state.RecordAttempt(
                operationId);

        bool shouldFail =
            attempt
            <= failuresBeforeSuccess;

        HttpStatusCode statusCode =
            shouldFail
                ? (HttpStatusCode)
                    failureStatusCode
                : HttpStatusCode.OK;

        var response =
            new HttpResponseMessage(
                statusCode)
            {
                RequestMessage =
                    request,

                Content =
                    JsonContent.Create(
                        new
                        {
                            authorized =
                                !shouldFail,

                            attempt
                        })
            };

        response.Headers.Add(
            "X-Demo-Attempt",
            attempt.ToString(
                CultureInfo.InvariantCulture));

        return Task.FromResult(
            response);
    }

    private static string
        ReadRequiredHeader(
            HttpRequestMessage request,
            string name)
    {
        if (request.Headers
            .TryGetValues(
                name,
                out IEnumerable<
                    string>? values))
        {
            string? value =
                values.SingleOrDefault();

            if (!string.IsNullOrWhiteSpace(
                    value))
            {
                return value;
            }
        }

        throw new InvalidOperationException(
            $"Required demonstration header '{name}' is missing.");
    }
}