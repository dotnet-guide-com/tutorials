using System.ComponentModel.DataAnnotations;

public record Todo(
    [Range(1, int.MaxValue)] int Id,
    [Required, MinLength(2)] string Title,
    bool IsComplete = false
);
