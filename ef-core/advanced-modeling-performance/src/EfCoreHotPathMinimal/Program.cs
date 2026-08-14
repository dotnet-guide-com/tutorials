using EfCoreHotPathMinimal.Services;

CatalogWorkflowResult result =
    await CatalogWorkflow
        .RunAsync();

Console.WriteLine(
    "EF Core Hot-Path Query Lab");

Console.WriteLine(
    $"Visible products: {result.VisibleProducts}");

Console.WriteLine(
    $"Compiled lookup: {result.CompiledLookup.Sku} | "
    + $"{result.CompiledLookup.Name} | "
    + $"{result.CompiledLookup.PriceCents} cents");

Console.WriteLine(
    $"Restocked products: {result.RestockedProducts}");

Console.WriteLine(
    $"Purged soft-deleted products: {result.PurgedProducts}");

Console.WriteLine(
    $"Final rows: {result.FinalRows}");