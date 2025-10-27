using System.ComponentModel.DataAnnotations;

namespace BlogApp;

public class Post
{
    public int Id { get; set; }

    [Required]
    public string? Title { get; set; }

    public string? Content { get; set; }

    public int BlogId { get; set; }

    public Blog? Blog { get; set; }
}
