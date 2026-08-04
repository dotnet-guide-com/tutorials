using CSharp12RefactoringLab.Models;

namespace CSharp12RefactoringLab.Formatting;

public static class TodoFormatter
{
    public static string[] BuildLabels(
        IEnumerable<TodoItem> items,
        string? explicitPrefix = null)
    {
        var format =
            (
                TodoItem item,
                string prefix = "TODO"
            ) =>
                $"{prefix}: {item.Title} [{GetState(item)}]";

        return explicitPrefix is null
            ?
            [
                .. items.Select(
                    item =>
                        format(
                            item))
            ]
            :
            [
                .. items.Select(
                    item =>
                        format(
                            item,
                            explicitPrefix))
            ];
    }

    private static string GetState(
        TodoItem item) =>
            item.IsComplete
                ? "done"
                : "active";
}