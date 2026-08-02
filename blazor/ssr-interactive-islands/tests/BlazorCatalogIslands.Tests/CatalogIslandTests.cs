using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using BlazorCatalogIslands.Components.Pages;
using BlazorCatalogIslands.Components.Shared;
using BlazorCatalogIslands.Models;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc.Testing;

namespace BlazorCatalogIslands.Tests;

public sealed class CatalogIslandTests
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
                "A desktop dock with display and network ports.")
        ];

    private static readonly
        ReviewSummary[] Reviews =
        [
            new ReviewSummary(
                1,
                "Asha",
                5,
                "Excellent for long coding sessions."),

            new ReviewSummary(
                2,
                "Daniel",
                4,
                "A reliable desk accessory.")
        ];

    [Fact]
    public void
        Product_card_renders_semantic_catalog_content()
    {
        using var context =
            new BunitContext();

        var cut =
            context.Render<
                ProductCard>(
                parameters =>
                    parameters.Add(
                        component =>
                            component.Product,
                        Products[0]));

        Assert.Contains(
            "Mechanical Keyboard",
            cut.Markup,
            StringComparison.Ordinal);

        Assert.Contains(
            "USD 89.00",
            cut.Markup,
            StringComparison.Ordinal);

        Assert.Equal(
            "1",
            cut.Find(
                    "[data-testid='product-card']")
                .GetAttribute(
                    "data-product-id"));
    }

    [Fact]
    public void
        Reviews_section_renders_loading_placeholder()
    {
        using var context =
            new BunitContext();

        var cut =
            context.Render<
                ReviewsSection>();

        Assert.Single(
            cut.FindAll(
                "[data-testid='reviews-loading']"));

        Assert.Empty(
            cut.FindAll(
                "[data-testid='review']"));
    }

    [Fact]
    public void
        Reviews_section_renders_completed_reviews()
    {
        using var context =
            new BunitContext();

        var cut =
            context.Render<
                ReviewsSection>(
                parameters =>
                    parameters.Add(
                        component =>
                            component.Reviews,
                        Reviews));

        Assert.Equal(
            2,
            cut.FindAll(
                "[data-testid='review']")
                .Count);

        Assert.Contains(
            "Excellent for long coding sessions.",
            cut.Markup,
            StringComparison.Ordinal);
    }

    [Fact]
    public void
        Cart_island_starts_empty()
    {
        using var context =
            new BunitContext();

        var cut =
            RenderCart(
                context);

        Assert.Contains(
            "0 items",
            Regex.Replace(
                cut.Find(
                    "[data-testid='cart-summary']")
                    .TextContent
                    .Trim(),
                @"\s+",
                " "),
            StringComparison.Ordinal);

        Assert.Single(
            cut.FindAll(
                "[data-testid='cart-empty']"));
    }

    [Fact]
    public void
        Cart_island_adds_a_product()
    {
        using var context =
            new BunitContext();

        var cut =
            RenderCart(
                context);

        cut.Find(
                "[data-action='add'][data-product-id='1']")
            .Click();

        cut.WaitForAssertion(
            () =>
            {
                Assert.Contains(
                    "1 item",
                    Regex.Replace(
                        cut.Find(
                            "[data-testid='cart-summary']")
                            .TextContent
                            .Trim(),
                        @"\s+",
                        " "),
                    StringComparison.Ordinal);

                Assert.Contains(
                    "USD 89.00",
                    Regex.Replace(
                        cut.Find(
                            "[data-testid='cart-summary']")
                            .TextContent
                            .Trim(),
                        @"\s+",
                        " "),
                    StringComparison.Ordinal);

                Assert.Equal(
                    "1",
                    cut.Find(
                            "[data-cart-product-id='1'] [data-testid='cart-quantity']")
                        .TextContent
                        .Trim());
            });
    }

    [Fact]
    public void
        Repeated_add_updates_quantity_and_total()
    {
        using var context =
            new BunitContext();

        var cut =
            RenderCart(
                context);

        var add =
            cut.Find(
                "[data-action='add'][data-product-id='1']");

        add.Click();
        add.Click();

        cut.WaitForAssertion(
            () =>
            {
                Assert.Contains(
                    "2 items",
                    Regex.Replace(
                        cut.Find(
                            "[data-testid='cart-summary']")
                            .TextContent
                            .Trim(),
                        @"\s+",
                        " "),
                    StringComparison.Ordinal);

                Assert.Contains(
                    "USD 178.00",
                    Regex.Replace(
                        cut.Find(
                            "[data-testid='cart-summary']")
                            .TextContent
                            .Trim(),
                        @"\s+",
                        " "),
                    StringComparison.Ordinal);

                Assert.Equal(
                    "2",
                    cut.Find(
                            "[data-cart-product-id='1'] [data-testid='cart-quantity']")
                        .TextContent
                        .Trim());
            });
    }

    [Fact]
    public void
        Cart_increment_decrement_and_remove_update_state()
    {
        using var context =
            new BunitContext();

        var cut =
            RenderCart(
                context);

        cut.Find(
                "[data-action='add'][data-product-id='1']")
            .Click();

        cut.WaitForAssertion(
            () =>
            {
                Assert.Equal(
                    "1",
                    cut.Find(
                            "[data-cart-product-id='1'] [data-testid='cart-quantity']")
                        .TextContent
                        .Trim());

                Assert.Contains(
                    "USD 89.00",
                    Normalize(
                        cut.Find(
                            "[data-testid='cart-summary']")),
                    StringComparison.Ordinal);
            });

        cut.Find(
                "[data-cart-product-id='1'] [data-action='increment']")
            .Click();

        cut.WaitForAssertion(
            () =>
            {
                Assert.Equal(
                    "2",
                    cut.Find(
                            "[data-cart-product-id='1'] [data-testid='cart-quantity']")
                        .TextContent
                        .Trim());

                Assert.Contains(
                    "USD 178.00",
                    Normalize(
                        cut.Find(
                            "[data-testid='cart-summary']")),
                    StringComparison.Ordinal);
            });

        cut.Find(
                "[data-cart-product-id='1'] [data-action='decrement']")
            .Click();

        cut.WaitForAssertion(
            () =>
            {
                Assert.Equal(
                    "1",
                    cut.Find(
                            "[data-cart-product-id='1'] [data-testid='cart-quantity']")
                        .TextContent
                        .Trim());

                Assert.Contains(
                    "USD 89.00",
                    Normalize(
                        cut.Find(
                            "[data-testid='cart-summary']")),
                    StringComparison.Ordinal);
            });

        cut.Find(
                "[data-cart-product-id='1'] [data-action='decrement']")
            .Click();

        cut.WaitForAssertion(
            () =>
            {
                Assert.Empty(
                    cut.FindAll(
                        "[data-cart-product-id='1']"));

                Assert.Contains(
                    "0 items",
                    Normalize(
                        cut.Find(
                            "[data-testid='cart-summary']")),
                    StringComparison.Ordinal);
            });

        cut.Find(
                "[data-action='add'][data-product-id='1']")
            .Click();

        cut.Find(
                "[data-cart-product-id='1'] [data-action='remove']")
            .Click();

        cut.WaitForAssertion(
            () =>
            {
                Assert.Empty(
                    cut.FindAll(
                        "[data-cart-product-id='1']"));

                Assert.Contains(
                    "0 items",
                    Normalize(
                        cut.Find(
                            "[data-testid='cart-summary']")),
                    StringComparison.Ordinal);
            });
    }

    [Fact]
    public void
        Catalog_component_declares_stream_rendering()
    {
        StreamRenderingAttribute? attribute =
            typeof(Catalog)
                .GetCustomAttribute<
                    StreamRenderingAttribute>();

        Assert.NotNull(
            attribute);
    }

    [Fact]
    public async Task
        Root_response_contains_catalog_reviews_and_blazor_script()
    {
        using var factory =
            new WebApplicationFactory<
                Program>();

        using HttpClient client =
            factory.CreateClient();

        CancellationToken ct =
            Xunit.TestContext.Current
                .CancellationToken;

        HttpResponseMessage response =
            await client.GetAsync(
                "/",
                ct);

        string body =
            await response.Content
                .ReadAsStringAsync(
                    ct);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        Assert.Contains(
            "Mechanical Keyboard",
            body,
            StringComparison.Ordinal);

        Assert.Contains(
            "USB-C Dock",
            body,
            StringComparison.Ordinal);

        Assert.Contains(
            "Monitor Arm",
            body,
            StringComparison.Ordinal);

        Assert.Contains(
            "A server-rendered product catalog with streamed reviews and an Interactive Server cart island.",
            body,
            StringComparison.Ordinal);

        Assert.Contains(
            "The keyboard feels excellent for long coding sessions.",
            body,
            StringComparison.Ordinal);

        Assert.Contains(
            "Interactive cart island",
            body,
            StringComparison.Ordinal);

        Assert.Contains(
            "_framework/blazor.web.js",
            body,
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task
        Unknown_path_returns_custom_not_found_page()
    {
        using var factory =
            new WebApplicationFactory<
                Program>();

        using HttpClient client =
            factory.CreateClient(
                new WebApplicationFactoryClientOptions
                {
                    AllowAutoRedirect =
                        false
                });

        CancellationToken ct =
            Xunit.TestContext.Current
                .CancellationToken;

        HttpResponseMessage response =
            await client.GetAsync(
                "/this-page-does-not-exist",
                ct);

        string body =
            await response.Content
                .ReadAsStringAsync(
                    ct);

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);

        Assert.Contains(
            "Page not found",
            body,
            StringComparison.Ordinal);
    }

    private static
        IRenderedComponent<CartIsland>
        RenderCart(
            BunitContext context) =>
                context.Render<
                    CartIsland>(
                    parameters =>
                        parameters.Add(
                            component =>
                                component.Products,
                            Products));

    private static string Normalize(
        IElement element) =>
            Regex.Replace(
                element.TextContent
                    .Trim(),
                @"\s+",
                " ");
}