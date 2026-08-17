using EfCoreTenantGuardrailsMinimal.Context;
using EfCoreTenantGuardrailsMinimal.Models;
using Microsoft.EntityFrameworkCore;

namespace EfCoreTenantGuardrailsMinimal.Data;

public sealed class TenantDbContext : DbContext
{
    public const string TenantFilterName = "TenantFilter";
    public const string SoftDeleteFilterName = "SoftDeleteFilter";

    private readonly string _tenantId;

    public TenantDbContext(
        DbContextOptions<TenantDbContext> options,
        TenantExecutionContext executionContext)
        : base(options)
    {
        _tenantId = executionContext.TenantId;
    }

    public DbSet<TenantTask> Tasks => Set<TenantTask>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TenantTask>(entity =>
        {
            entity.HasKey(task => new { task.TenantId, task.Id });

            entity.Property(task => task.TenantId)
                .HasMaxLength(64)
                .IsRequired();

            entity.Property(task => task.Title)
                .HasMaxLength(160)
                .IsRequired();

            entity.Property(task => task.CreatedBy)
                .HasMaxLength(120)
                .IsRequired();

            entity.Property(task => task.UpdatedBy)
                .HasMaxLength(120)
                .IsRequired();

            entity.Property(task => task.DeletedBy)
                .HasMaxLength(120);

            entity
                .HasQueryFilter(
                    TenantFilterName,
                    task => task.TenantId == _tenantId)
                .HasQueryFilter(
                    SoftDeleteFilterName,
                    task => !task.IsDeleted);

            entity.HasIndex(task => new
                {
                    task.TenantId,
                    task.IsDeleted,
                    task.Id
                })
                .HasDatabaseName("IX_TenantTasks_Tenant_Deleted_Id");
        });
    }
}
