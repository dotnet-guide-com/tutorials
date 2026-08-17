namespace EfCoreProviderContrastMinimal.Models;

public sealed class Project
{
    public int Id
    {
        get;
        set;
    }

    public string Name
    {
        get;
        set;
    } = "";

    public string OwnerId
    {
        get;
        set;
    } = "";

    public List<TaskItem> Tasks
    {
        get;
    } = [];
}

public sealed class TaskItem
{
    public int Id
    {
        get;
        set;
    }

    public string Title
    {
        get;
        set;
    } = "";

    public int ProjectId
    {
        get;
        set;
    }

    public Project Project
    {
        get;
        set;
    } = null!;
}