namespace EfCoreTenantGuardrailsMinimal.Context;

public sealed class TenantExecutionContext
{
    public TenantExecutionContext(
        string tenantId,
        string actorId,
        DateTimeOffset utcNow)
    {
        string normalizedTenant = tenantId.Trim().ToLowerInvariant();
        string normalizedActor = actorId.Trim();

        if (normalizedTenant.Length is < 1 or > 64)
            throw new ArgumentException(
                "TenantId must contain between 1 and 64 characters.",
                nameof(tenantId));

        if (normalizedActor.Length is < 1 or > 120)
            throw new ArgumentException(
                "ActorId must contain between 1 and 120 characters.",
                nameof(actorId));

        TenantId = normalizedTenant;
        ActorId = normalizedActor;
        UtcNow = utcNow.ToUniversalTime();
    }

    public string TenantId { get; }
    public string ActorId { get; }
    public DateTimeOffset UtcNow { get; }
}
