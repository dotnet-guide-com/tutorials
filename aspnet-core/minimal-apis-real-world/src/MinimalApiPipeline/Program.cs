using System.Diagnostics;
using System.Globalization;
using System.Threading.RateLimiting;
using Asp.Versioning;
using Asp.Versioning.Builder;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

var builder =
    WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<
    OrderRepository>();

builder.Services.AddValidatorsFromAssemblyContaining<
    Program>();

builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion =
        new ApiVersion(1, 0);

    options.AssumeDefaultVersionWhenUnspecified =
        false;

    options.ReportApiVersions =
        true;

    options.ApiVersionReader =
        new UrlSegmentApiVersionReader();
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode =
        StatusCodes.Status429TooManyRequests;

    options.AddPolicy(
        "order-writes",
        httpContext =>
        {
            string clientId =
                httpContext.Request.Headers[
                    "X-Client-Id"]
                    .FirstOrDefault()
                ?? "anonymous";

            if (string.IsNullOrWhiteSpace(
                    clientId))
            {
                clientId =
                    "anonymous";
            }

            return RateLimitPartition
                .GetFixedWindowLimiter(
                    partitionKey:
                        clientId,

                    factory:
                        _ =>
                            new FixedWindowRateLimiterOptions
                            {
                                AutoReplenishment =
                                    true,

                                PermitLimit =
                                    2,

                                Window =
                                    TimeSpan.FromMinutes(
                                        1),

                                QueueProcessingOrder =
                                    QueueProcessingOrder
                                        .OldestFirst,

                                QueueLimit =
                                    0
                            });
        });

    options.OnRejected =
        async (
            context,
            cancellationToken) =>
        {
            context.HttpContext.Response
                .StatusCode =
                    StatusCodes
                        .Status429TooManyRequests;

            if (context.Lease.TryGetMetadata(
                    MetadataName.RetryAfter,
                    out TimeSpan retryAfter))
            {
                context.HttpContext.Response
                    .Headers.RetryAfter =
                        Math.Ceiling(
                            retryAfter.TotalSeconds)
                        .ToString(
                            CultureInfo
                                .InvariantCulture);
            }

            await context.HttpContext.Response
                .WriteAsJsonAsync(
                    new ProblemDetails
                    {
                        Status =
                            StatusCodes
                                .Status429TooManyRequests,

                        Title =
                            "Rate limit exceeded",

                        Detail =
                            "This client has exhausted the demonstration write quota."
                    },
                    cancellationToken);
        };
});

var app =
    builder.Build();

app.UseRateLimiter();

app.MapGet(
    "/",
    () => Results.Ok(new
    {
        name =
            "Minimal API Pipeline",

        versions = new[]
        {
            "v1",
            "v2"
        },

        endpoints = new[]
        {
            "GET /api/v1/orders",
            "GET /api/v1/orders/{id}",
            "POST /api/v1/orders",
            "GET /api/v2/orders",
            "GET /api/v2/orders/{id}"
        }
    }));

ApiVersionSet versionSet =
    app.NewApiVersionSet()
        .HasApiVersion(
            new ApiVersion(1, 0))
        .HasApiVersion(
            new ApiVersion(2, 0))
        .ReportApiVersions()
        .Build();

RouteGroupBuilder orders =
    app.MapGroup(
            "/api/v{version:apiVersion}/orders")
        .WithApiVersionSet(
            versionSet)
        .AddEndpointFilter<
            TimingFilter>();

orders.MapGet(
        "/",
        GetOrdersV1)
    .MapToApiVersion(
        new ApiVersion(1, 0));

orders.MapGet(
        "/",
        GetOrdersV2)
    .MapToApiVersion(
        new ApiVersion(2, 0));

orders.MapGet(
        "/{id:int:min(1)}",
        GetOrderV1)
    .MapToApiVersion(
        new ApiVersion(1, 0));

orders.MapGet(
        "/{id:int:min(1)}",
        GetOrderV2)
    .MapToApiVersion(
        new ApiVersion(2, 0));

RouteHandlerBuilder createOrder =
    orders.MapPost(
            "/",
            CreateOrder)
        .AddEndpointFilter<
            ValidationFilter<
                CreateOrderRequest>>()
        .RequireRateLimiting(
            "order-writes");

createOrder.MapToApiVersion(
    new ApiVersion(1, 0));

app.Run();

static Ok<OrderV1[]> GetOrdersV1(
    OrderRepository repository)
{
    OrderV1[] response =
        repository.GetAll()
            .Select(ToV1)
            .ToArray();

    return TypedResults.Ok(
        response);
}

static Results<
    Ok<PagedOrdersV2>,
    ValidationProblem>
    GetOrdersV2(
        OrderRepository repository,
        int page = 1,
        int pageSize = 2)
{
    var errors =
        new Dictionary<
            string,
            string[]>();

    if (page < 1)
    {
        errors["page"] =
        [
            "Page must be at least 1."
        ];
    }

    if (pageSize is < 1 or > 50)
    {
        errors["pageSize"] =
        [
            "Page size must be between 1 and 50."
        ];
    }

    if (errors.Count > 0)
    {
        return TypedResults
            .ValidationProblem(
                errors);
    }

    OrderEntity[] all =
        repository.GetAll();

    OrderV2[] data =
        all.Skip(
                (page - 1) *
                pageSize)
            .Take(
                pageSize)
            .Select(
                ToV2)
            .ToArray();

    return TypedResults.Ok(
        new PagedOrdersV2(
            data,
            page,
            pageSize,
            all.Length));
}

static Results<
    Ok<OrderV1>,
    NotFound>
    GetOrderV1(
        int id,
        OrderRepository repository)
{
    OrderEntity? order =
        repository.GetById(id);

    return order is null
        ? TypedResults.NotFound()
        : TypedResults.Ok(
            ToV1(order));
}

static Results<
    Ok<OrderV2>,
    NotFound>
    GetOrderV2(
        int id,
        OrderRepository repository)
{
    OrderEntity? order =
        repository.GetById(id);

    return order is null
        ? TypedResults.NotFound()
        : TypedResults.Ok(
            ToV2(order));
}

static Created<OrderV1> CreateOrder(
    CreateOrderRequest request,
    OrderRepository repository)
{
    OrderEntity created =
        repository.Create(
            request.CustomerId!.Trim(),
            request.Items!);

    return TypedResults.Created(
        $"/api/v1/orders/{created.Id}",
        ToV1(created));
}

static OrderV1 ToV1(
    OrderEntity order) =>
        new(
            order.Id,
            order.CustomerId,
            order.Items.ToArray());

static OrderV2 ToV2(
    OrderEntity order) =>
        new(
            order.Id,
            order.CustomerId,
            order.Items.Count,
            order.CreatedAt);

public sealed record CreateOrderRequest(
    string? CustomerId,
    List<string>? Items);

public sealed record OrderV1(
    int Id,
    string CustomerId,
    string[] Items);

public sealed record OrderV2(
    int Id,
    string CustomerId,
    int ItemCount,
    DateTimeOffset CreatedAt);

public sealed record PagedOrdersV2(
    OrderV2[] Data,
    int Page,
    int PageSize,
    int TotalCount);

public sealed record OrderEntity(
    int Id,
    string CustomerId,
    IReadOnlyList<string> Items,
    DateTimeOffset CreatedAt);

public sealed class
    CreateOrderRequestValidator :
        AbstractValidator<
            CreateOrderRequest>
{
    public CreateOrderRequestValidator()
    {
        RuleFor(
                request =>
                    request.CustomerId)
            .NotEmpty()
            .WithMessage(
                "Customer ID is required.")
            .MaximumLength(50)
            .WithMessage(
                "Customer ID must contain 50 characters or fewer.");

        RuleFor(
                request =>
                    request.Items)
            .NotNull()
            .WithMessage(
                "Items are required.")
            .Must(
                items =>
                    items is
                    {
                        Count: > 0
                    })
            .WithMessage(
                "At least one item is required.")
            .Must(
                items =>
                    items is null ||
                    items.Count <= 5)
            .WithMessage(
                "An order can contain at most five items.");

        When(
            request =>
                request.Items is not null,
            () =>
            {
                RuleForEach(
                        request =>
                            request.Items!)
                    .NotEmpty()
                    .WithMessage(
                        "Item names cannot be empty.")
                    .MaximumLength(80)
                    .WithMessage(
                        "Item names must contain 80 characters or fewer.");
            });
    }
}

public sealed class
    ValidationFilter<T> :
        IEndpointFilter
    where T : class
{
    public async ValueTask<object?>
        InvokeAsync(
            EndpointFilterInvocationContext
                context,
            EndpointFilterDelegate next)
    {
        T? request =
            context.Arguments
                .OfType<T>()
                .FirstOrDefault();

        if (request is null)
        {
            return await next(
                context);
        }

        IValidator<T> validator =
            context.HttpContext
                .RequestServices
                .GetRequiredService<
                    IValidator<T>>();

        FluentValidation.Results
            .ValidationResult result =
                await validator
                    .ValidateAsync(
                        request,
                        context.HttpContext
                            .RequestAborted);

        if (result.IsValid)
        {
            return await next(
                context);
        }

        Dictionary<
            string,
            string[]> errors =
                result.Errors
                    .GroupBy(
                        failure =>
                            failure
                                .PropertyName)
                    .ToDictionary(
                        group =>
                            group.Key,

                        group =>
                            group.Select(
                                    failure =>
                                        failure
                                            .ErrorMessage)
                                .Distinct(
                                    StringComparer
                                        .Ordinal)
                                .ToArray());

        return TypedResults
            .ValidationProblem(
                errors);
    }
}

public sealed class TimingFilter :
    IEndpointFilter
{
    public async ValueTask<object?>
        InvokeAsync(
            EndpointFilterInvocationContext
                context,
            EndpointFilterDelegate next)
    {
        var stopwatch =
            Stopwatch.StartNew();

        context.HttpContext.Response
            .OnStarting(
                () =>
                {
                    stopwatch.Stop();

                    context.HttpContext
                        .Response
                        .Headers[
                            "X-Endpoint-Elapsed-Ms"] =
                            stopwatch
                                .ElapsedMilliseconds
                                .ToString(
                                    CultureInfo
                                        .InvariantCulture);

                    return Task.CompletedTask;
                });

        return await next(
            context);
    }
}

public sealed class OrderRepository
{
    private readonly object _gate =
        new();

    private readonly List<OrderEntity>
        _orders =
        [
            new OrderEntity(
                1,
                "customer-100",
                [
                    "keyboard",
                    "mouse"
                ],
                new DateTimeOffset(
                    2026,
                    1,
                    10,
                    9,
                    0,
                    0,
                    TimeSpan.Zero)),

            new OrderEntity(
                2,
                "customer-200",
                [
                    "monitor"
                ],
                new DateTimeOffset(
                    2026,
                    1,
                    11,
                    10,
                    0,
                    0,
                    TimeSpan.Zero)),

            new OrderEntity(
                3,
                "customer-300",
                [
                    "dock",
                    "webcam",
                    "headset"
                ],
                new DateTimeOffset(
                    2026,
                    1,
                    12,
                    11,
                    0,
                    0,
                    TimeSpan.Zero))
        ];

    private int _nextId =
        3;

    public OrderEntity[] GetAll()
    {
        lock (_gate)
        {
            return _orders
                .OrderBy(
                    order =>
                        order.Id)
                .ToArray();
        }
    }

    public OrderEntity? GetById(
        int id)
    {
        lock (_gate)
        {
            return _orders
                .FirstOrDefault(
                    order =>
                        order.Id ==
                        id);
        }
    }

    public OrderEntity Create(
        string customerId,
        IReadOnlyList<string> items)
    {
        lock (_gate)
        {
            int id =
                ++_nextId;

            var created =
                new OrderEntity(
                    id,
                    customerId,
                    items.ToArray(),
                    DateTimeOffset.UtcNow);

            _orders.Add(
                created);

            return created;
        }
    }
}

public partial class Program
{
}