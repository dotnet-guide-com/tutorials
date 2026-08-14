using EfCoreRelationshipsMinimal.Services;

LoadingWorkflowResult result =
    await LoadingScenarios
        .RunAsync();

Console.WriteLine(
    "EF Core Relationship Loading Lab");

Console.WriteLine(
    $"Seeded graph: categories={result.SeededGraph.Categories}, "
    + $"todos={result.SeededGraph.Todos}, "
    + $"tags={result.SeededGraph.Tags}");

Console.WriteLine(
    $"N+1 baseline: todos={result.NPlusOne.TodoCount}, "
    + $"SELECTs={result.NPlusOne.SelectCount}");

Console.WriteLine(
    $"Eager Include: todos={result.EagerInclude.TodoCount}, "
    + $"SELECTs={result.EagerInclude.SelectCount}");

Console.WriteLine(
    $"Split graph: categories={result.SplitGraph.CategoryCount}, "
    + $"todos={result.SplitGraph.TodoCount}, "
    + $"tags={result.SplitGraph.TagCount}, "
    + $"SELECTs={result.SplitGraph.SelectCount}");

Console.WriteLine(
    $"Explicit reference load: todo={result.ExplicitLoad.TodoTitle}, "
    + $"category={result.ExplicitLoad.CategoryName}, "
    + $"SELECTs={result.ExplicitLoad.SelectCount}");