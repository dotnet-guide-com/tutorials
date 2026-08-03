using System.Globalization;
using System.Net;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace ResilientOrdersMinimal.Payments;

public sealed class PaymentGatewayClient(
    HttpClient client,
    PaymentSimulationState state)
{
    public async Task<
        PaymentAuthorizationResult>
        AuthorizeAsync(
            int orderId,
            int failuresBeforeSuccess,
            HttpStatusCode failureStatus,
            int delayMilliseconds,
            CancellationToken cancellationToken)
    {
        string operationId =
            Guid.NewGuid()
                .ToString(
                    "N",
                    CultureInfo.InvariantCulture);

        using var request =
            new HttpRequestMessage(
                HttpMethod.Post,
                $"/payments/orders/{orderId}/authorize");

        request.Headers.Add(
            "X-Demo-Operation-Id",
            operationId);

        request.Headers.Add(
            "X-Demo-Failures-Before-Success",
            failuresBeforeSuccess
                .ToString(
                    CultureInfo.InvariantCulture));

        request.Headers.Add(
            "X-Demo-Failure-Status",
            ((int)failureStatus)
                .ToString(
                    CultureInfo.InvariantCulture));

        request.Headers.Add(
            "X-Demo-Delay-Milliseconds",
            delayMilliseconds
                .ToString(
                    CultureInfo.InvariantCulture));

        try
        {
            using HttpResponseMessage response =
                await client.SendAsync(
                    request,
                    cancellationToken);

            return new PaymentAuthorizationResult(
                OrderId:
                    orderId,

                Succeeded:
                    response.IsSuccessStatusCode,

                Attempts:
                    state.GetAttempts(
                        operationId),

                CircuitOpen:
                    false,

                StatusCode:
                    (int)response.StatusCode);
        }
        catch (BrokenCircuitException)
        {
            return new PaymentAuthorizationResult(
                OrderId:
                    orderId,

                Succeeded:
                    false,

                Attempts:
                    state.GetAttempts(
                        operationId),

                CircuitOpen:
                    true,

                StatusCode:
                    StatusCodes
                        .Status503ServiceUnavailable);
        }
        catch (TimeoutRejectedException)
        {
            return new PaymentAuthorizationResult(
                OrderId:
                    orderId,

                Succeeded:
                    false,

                Attempts:
                    state.GetAttempts(
                        operationId),

                CircuitOpen:
                    false,

                StatusCode:
                    StatusCodes
                        .Status504GatewayTimeout);
        }
        finally
        {
            state.Remove(
                operationId);
        }
    }
}

public sealed record
    PaymentAuthorizationResult(
        int OrderId,
        bool Succeeded,
        int Attempts,
        bool CircuitOpen,
        int StatusCode);