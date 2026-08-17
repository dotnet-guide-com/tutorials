namespace EfCoreTenantGuardrailsMinimal.Models;

public sealed class TenantTask
{
    public string TenantId { get; set; } = "";
    public int Id { get; set; }
    public string Title { get; set; } = "";

    public bool IsDeleted { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public string CreatedBy { get; set; } = "";
    public DateTimeOffset UpdatedAt { get; set; }
    public string UpdatedBy { get; set; } = "";

    public DateTimeOffset? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
}
