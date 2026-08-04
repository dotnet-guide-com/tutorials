namespace CSharp12RefactoringLab.Models;

public sealed class TodoItem(
    string title,
    bool isComplete = false)
{
    public string Title { get; } =
        NormalizeTitle(
            title);

    public bool IsComplete { get; } =
        isComplete;

    public TodoItem Complete() =>
        IsComplete
            ? this
            : new TodoItem(
                Title,
                isComplete:
                    true);

    private static string NormalizeTitle(
        string value)
    {
        ArgumentException
            .ThrowIfNullOrWhiteSpace(
                value);

        return value.Trim();
    }
}