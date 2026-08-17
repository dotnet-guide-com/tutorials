using EfCoreProviderContrastMinimal.Models;
using Microsoft.EntityFrameworkCore;

namespace EfCoreProviderContrastMinimal.Data;

public sealed class ProviderDbContext(
    DbContextOptions<
        ProviderDbContext>
        options) :
    DbContext(
        options)
{
    public DbSet<Project> Projects =>
        Set<Project>();

    public DbSet<TaskItem> Tasks =>
        Set<TaskItem>();

    protected override void
        OnModelCreating(
            ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Project>(
            project =>
            {
                project.HasKey(
                    entity =>
                        entity.Id);

                project.Property(
                        entity =>
                            entity.Id)
                    .ValueGeneratedNever();

                project.Property(
                        entity =>
                            entity.Name)
                    .HasMaxLength(
                        80)
                    .IsRequired();

                project.Property(
                        entity =>
                            entity.OwnerId)
                    .HasMaxLength(
                        80)
                    .IsRequired();

                project.HasIndex(
                        entity =>
                            new
                            {
                                entity.Name,
                                entity.OwnerId
                            })
                    .IsUnique()
                    .HasDatabaseName(
                        "UX_Projects_Name_Owner");

                project.HasMany(
                        entity =>
                            entity.Tasks)
                    .WithOne(
                        task =>
                            task.Project)
                    .HasForeignKey(
                        task =>
                            task.ProjectId)
                    .OnDelete(
                        DeleteBehavior.Cascade);
            });

        modelBuilder.Entity<TaskItem>(
            task =>
            {
                task.HasKey(
                    entity =>
                        entity.Id);

                task.Property(
                        entity =>
                            entity.Id)
                    .ValueGeneratedNever();

                task.Property(
                        entity =>
                            entity.Title)
                    .HasMaxLength(
                        120)
                    .IsRequired();
            });
    }
}