using Microsoft.AspNetCore.OutputCaching;

var builder =
    WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<
    CatalogRepository>();

builder.Services.AddSingleton<
    OriginExecutionTracker>();

builder.Services.AddOutputCache(options =>
{
    options.AddPolicy(
        "ProductList",
        policy => policy
            .Expire(
                TimeSpan.FromMinutes(5))
            .SetVaryByQuery(
                "category",
                "sort")
            .Tag("products"));

    options.AddPolicy(
        "ProductDetail",
        policy => policy
            .Expire(
                TimeSpan.FromMinutes(10))
            .SetVaryByRouteValue("id")
            .Tag("products"));
});

var app =
    builder.Build();

app.UseOutputCache();

app.MapGet(
    "/",
    () => Results.Ok(new
    {
        name =
            "Output Cache Catalog Minimal",

        endpoints = new[]
        {
            "GET /products",
            "GET /products/{id}",
            "POST /products",
            "PUT /products/{id}",
            "DELETE /products/{id}"
        }
    }));

RouteGroupBuilder products =
    app.MapGroup("/products");

products.MapGet(
    "/",
    (
        int? category,
        string? sort,
        CatalogRepository repository,
        OriginExecutionTracker tracker) =>
    {
        int originExecution =
            tracker.RecordList();

        IReadOnlyList<ProductItem> items =
            repository.GetAll(
                category,
                sort);

        return Results.Ok(
            new ProductListResponse(
                originExecution,
                items));
    })
    .CacheOutput("ProductList");

products.MapGet(
    "/{id:int:min(1)}",
    (
        int id,
        CatalogRepository repository,
        OriginExecutionTracker tracker) =>
    {
        int originExecution =
            tracker.RecordDetail();

        ProductItem? product =
            repository.GetById(id);

        return product is null
            ? Results.NotFound()
            : Results.Ok(
                new ProductDetailResponse(
                    originExecution,
                    product));
    })
    .CacheOutput("ProductDetail");

products.MapPost(
    "/",
    async (
        CreateProductRequest request,
        CatalogRepository repository,
        IOutputCacheStore cache,
        CancellationToken cancellationToken) =>
    {
        Dictionary<string, string[]>? errors =
            Validate(
                request.Name,
                request.Price,
                request.CategoryId);

        if (errors is not null)
        {
            return Results.ValidationProblem(
                errors);
        }

        ProductItem created =
            repository.Create(
                request.Name!.Trim(),
                request.Price,
                request.CategoryId);

        await cache.EvictByTagAsync(
            "products",
            cancellationToken);

        return Results.Created(
            $"/products/{created.Id}",
            created);
    });

products.MapPut(
    "/{id:int:min(1)}",
    async (
        int id,
        UpdateProductRequest request,
        CatalogRepository repository,
        IOutputCacheStore cache,
        CancellationToken cancellationToken) =>
    {
        Dictionary<string, string[]>? errors =
            Validate(
                request.Name,
                request.Price,
                request.CategoryId);

        if (errors is not null)
        {
            return Results.ValidationProblem(
                errors);
        }

        ProductItem? updated =
            repository.Update(
                id,
                request.Name!.Trim(),
                request.Price,
                request.CategoryId);

        if (updated is null)
        {
            return Results.NotFound();
        }

        await cache.EvictByTagAsync(
            "products",
            cancellationToken);

        return Results.NoContent();
    });

products.MapDelete(
    "/{id:int:min(1)}",
    async (
        int id,
        CatalogRepository repository,
        IOutputCacheStore cache,
        CancellationToken cancellationToken) =>
    {
        if (!repository.Delete(id))
        {
            return Results.NotFound();
        }

        await cache.EvictByTagAsync(
            "products",
            cancellationToken);

        return Results.NoContent();
    });

app.Run();

static Dictionary<string, string[]>?
    Validate(
        string? name,
        decimal price,
        int categoryId)
{
    var errors =
        new Dictionary<string, string[]>();

    if (string.IsNullOrWhiteSpace(name))
    {
        errors["name"] =
        [
            "Name is required."
        ];
    }
    else if (name.Trim().Length > 100)
    {
        errors["name"] =
        [
            "Name must contain 100 characters or fewer."
        ];
    }

    if (price <= 0)
    {
        errors["price"] =
        [
            "Price must be greater than zero."
        ];
    }

    if (categoryId <= 0)
    {
        errors["categoryId"] =
        [
            "Category ID must be greater than zero."
        ];
    }

    return errors.Count == 0
        ? null
        : errors;
}

public sealed record ProductItem(
    int Id,
    string Name,
    decimal Price,
    int CategoryId);

public sealed record ProductListResponse(
    int OriginExecution,
    IReadOnlyList<ProductItem> Items);

public sealed record ProductDetailResponse(
    int OriginExecution,
    ProductItem Item);

public sealed record CreateProductRequest(
    string? Name,
    decimal Price,
    int CategoryId);

public sealed record UpdateProductRequest(
    string? Name,
    decimal Price,
    int CategoryId);

public sealed class OriginExecutionTracker
{
    private int _listExecutions;
    private int _detailExecutions;

    public int RecordList() =>
        Interlocked.Increment(
            ref _listExecutions);

    public int RecordDetail() =>
        Interlocked.Increment(
            ref _detailExecutions);
}

public sealed class CatalogRepository
{
    private readonly object _gate =
        new();

    private readonly List<ProductItem> _items =
    [
        new ProductItem(
            1,
            "Mechanical Keyboard",
            89.00m,
            1),

        new ProductItem(
            2,
            "USB-C Dock",
            129.00m,
            1),

        new ProductItem(
            3,
            "Desk Lamp",
            49.00m,
            2)
    ];

    private int _nextId =
        3;

    public IReadOnlyList<ProductItem> GetAll(
        int? category,
        string? sort)
    {
        lock (_gate)
        {
            IEnumerable<ProductItem> query =
                _items;

            if (category.HasValue)
            {
                query =
                    query.Where(
                        item =>
                            item.CategoryId ==
                            category.Value);
            }

            query =
                sort?.Trim().ToLowerInvariant()
                switch
                {
                    "name" =>
                        query
                            .OrderBy(
                                item =>
                                    item.Name,
                                StringComparer.Ordinal)
                            .ThenBy(
                                item =>
                                    item.Id),

                    "price" =>
                        query
                            .OrderBy(
                                item =>
                                    item.Price)
                            .ThenBy(
                                item =>
                                    item.Id),

                    "price_desc" =>
                        query
                            .OrderByDescending(
                                item =>
                                    item.Price)
                            .ThenBy(
                                item =>
                                    item.Id),

                    _ =>
                        query.OrderBy(
                            item =>
                                item.Id)
                };

            return query.ToArray();
        }
    }

    public ProductItem? GetById(int id)
    {
        lock (_gate)
        {
            return _items.FirstOrDefault(
                item =>
                    item.Id == id);
        }
    }

    public ProductItem Create(
        string name,
        decimal price,
        int categoryId)
    {
        lock (_gate)
        {
            int id =
                ++_nextId;

            var created =
                new ProductItem(
                    id,
                    name,
                    price,
                    categoryId);

            _items.Add(created);

            return created;
        }
    }

    public ProductItem? Update(
        int id,
        string name,
        decimal price,
        int categoryId)
    {
        lock (_gate)
        {
            int index =
                _items.FindIndex(
                    item =>
                        item.Id == id);

            if (index < 0)
            {
                return null;
            }

            var updated =
                new ProductItem(
                    id,
                    name,
                    price,
                    categoryId);

            _items[index] =
                updated;

            return updated;
        }
    }

    public bool Delete(int id)
    {
        lock (_gate)
        {
            int index =
                _items.FindIndex(
                    item =>
                        item.Id == id);

            if (index < 0)
            {
                return false;
            }

            _items.RemoveAt(index);

            return true;
        }
    }
}

public partial class Program
{
}