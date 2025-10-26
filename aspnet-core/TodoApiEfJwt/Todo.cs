using System.ComponentModel.DataAnnotations;

public class Todo
{
    public int Id { get; set; }

    [Required, MinLength(2)]
    public string Title { get; set; } = string.Empty;

    public bool IsComplete { get; set; }
}
