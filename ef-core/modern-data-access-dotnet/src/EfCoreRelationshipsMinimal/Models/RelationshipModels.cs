namespace EfCoreRelationshipsMinimal.Models;

public sealed class Category
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

    public List<TodoItem> Todos
    {
        get;
    } = [];
}

public sealed class TodoItem
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

    public int CategoryId
    {
        get;
        set;
    }

    public Category Category
    {
        get;
        set;
    } = null!;

    public List<Tag> Tags
    {
        get;
    } = [];
}

public sealed class Tag
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

    public List<TodoItem> Todos
    {
        get;
    } = [];
}