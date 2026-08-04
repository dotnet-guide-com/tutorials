using CSharp12RefactoringLab.Formatting;
using CSharp12RefactoringLab.Models;
using CSharp12RefactoringLab.Services;

TodoItem[] seedItems =
[
    new TodoItem(
        "Adopt primary constructors"),

    new TodoItem(
        "Use collection expressions",
        isComplete:
            true)
];

var service =
    new TodoService(
        seedItems);

service.Add(
    new TodoItem(
        "Try default lambda parameters"));

service.Add(
    new TodoItem(
        "Review explicit properties",
        isComplete:
            true));

var counts =
    service.GetCounts();

Console.WriteLine(
    "C# 12 Todo Refactoring Lab");

Console.WriteLine(
    $"Counts: total={counts.Total}, active={counts.Active}, completed={counts.Completed}");

foreach (string label in
    TodoFormatter.BuildLabels(
        service.GetAllWithWelcome()))
{
    Console.WriteLine(
        label);
}