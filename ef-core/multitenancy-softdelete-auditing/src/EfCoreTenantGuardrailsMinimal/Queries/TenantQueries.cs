using EfCoreTenantGuardrailsMinimal.Data;
using EfCoreTenantGuardrailsMinimal.Models;
using Microsoft.EntityFrameworkCore;

namespace EfCoreTenantGuardrailsMinimal.Queries;

public static class TenantQueries
{
    public static Task<List<TenantTask>> ListVisibleAsync(
        TenantDbContext context,
        CancellationToken cancellationToken = default) =>
        context.Tasks
            .AsNoTracking()
            .OrderBy(task => task.Id)
            .ToListAsync(cancellationToken);

    public static Task<List<TenantTask>> ListIncludingDeletedAsync(
        TenantDbContext context,
        CancellationToken cancellationToken = default) =>
        context.Tasks
            .IgnoreQueryFilters([TenantDbContext.SoftDeleteFilterName])
            .AsNoTracking()
            .OrderBy(task => task.Id)
            .ToListAsync(cancellationToken);
}
