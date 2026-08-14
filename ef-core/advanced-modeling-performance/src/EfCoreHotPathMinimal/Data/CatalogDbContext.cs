using EfCoreHotPathMinimal.Models;
using Microsoft.EntityFrameworkCore;

namespace EfCoreHotPathMinimal.Data;

public sealed class CatalogDbContext(
    DbContextOptions<CatalogDbContext>
        options) :
    DbContext(
        options)
{
    public const string
        SoftDeleteFilterName =
            "SoftDeleteFilter";

    public DbSet<Product> Products =>
        Set<Product>();

    protected override void
        OnModelCreating(
            ModelBuilder modelBuilder)
    {
        modelBuilder
            .Entity<Product>(
                entity =>
                {
                    entity.HasKey(
                        product =>
                            product.Id);

                    entity.Property(
                            product =>
                                product.Sku)
                        .HasMaxLength(
                            32)
                        .IsRequired();

                    entity.Property(
                            product =>
                                product.Name)
                        .HasMaxLength(
                            120)
                        .IsRequired();

                    entity.HasIndex(
                            product =>
                                product.Sku)
                        .IsUnique();

                    entity.HasQueryFilter(
                        SoftDeleteFilterName,
                        product =>
                            !product.IsDeleted);
                });
    }
}