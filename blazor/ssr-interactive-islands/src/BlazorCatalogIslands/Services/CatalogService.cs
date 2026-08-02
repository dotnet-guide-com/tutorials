using BlazorCatalogIslands.Models;

namespace BlazorCatalogIslands.Services;

public interface ICatalogService
{
    ProductSummary[] GetProducts();

    Task<ReviewSummary[]>
        GetFeaturedReviewsAsync();
}

public sealed class CatalogService :
    ICatalogService
{
    private static readonly
        ProductSummary[] Products =
        [
            new ProductSummary(
                1,
                "Mechanical Keyboard",
                89.00m,
                "A compact keyboard with tactile switches."),

            new ProductSummary(
                2,
                "USB-C Dock",
                129.00m,
                "A desktop dock with display and network ports."),

            new ProductSummary(
                3,
                "Monitor Arm",
                79.00m,
                "An adjustable arm for a single display.")
        ];

    private static readonly
        ReviewSummary[] Reviews =
        [
            new ReviewSummary(
                1,
                "Asha",
                5,
                "The keyboard feels excellent for long coding sessions."),

            new ReviewSummary(
                2,
                "Daniel",
                4,
                "The dock keeps my desk setup simple and reliable.")
        ];

    public ProductSummary[] GetProducts() =>
        Products.ToArray();

    public async Task<ReviewSummary[]>
        GetFeaturedReviewsAsync()
    {
        await Task.Delay(
            TimeSpan.FromMilliseconds(
                700));

        return Reviews.ToArray();
    }
}