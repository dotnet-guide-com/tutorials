using EfCoreRelationshipsMinimal.Models;
using Microsoft.EntityFrameworkCore;

namespace EfCoreRelationshipsMinimal.Data;

public sealed class RelationshipDbContext(
    DbContextOptions<
        RelationshipDbContext>
        options) :
    DbContext(
        options)
{
    public DbSet<Category> Categories =>
        Set<Category>();

    public DbSet<TodoItem> Todos =>
        Set<TodoItem>();

    public DbSet<Tag> Tags =>
        Set<Tag>();

    protected override void
        OnModelCreating(
            ModelBuilder modelBuilder)
    {
        modelBuilder
            .Entity<Category>(
                category =>
                {
                    category.HasKey(
                        entity =>
                            entity.Id);

                    category.Property(
                            entity =>
                                entity.Name)
                        .HasMaxLength(
                            80)
                        .IsRequired();
                });

        modelBuilder
            .Entity<TodoItem>(
                todo =>
                {
                    todo.HasKey(
                        entity =>
                            entity.Id);

                    todo.Property(
                            entity =>
                                entity.Title)
                        .HasMaxLength(
                            120)
                        .IsRequired();

                    todo.HasIndex(
                        entity =>
                            entity.CategoryId);

                    todo.HasOne(
                            entity =>
                                entity.Category)
                        .WithMany(
                            category =>
                                category.Todos)
                        .HasForeignKey(
                            entity =>
                                entity.CategoryId)
                        .OnDelete(
                            DeleteBehavior
                                .Restrict);

                    todo.HasMany(
                            entity =>
                                entity.Tags)
                        .WithMany(
                            tag =>
                                tag.Todos)
                        .UsingEntity(
                            "TodoTag");
                });

        modelBuilder
            .Entity<Tag>(
                tag =>
                {
                    tag.HasKey(
                        entity =>
                            entity.Id);

                    tag.Property(
                            entity =>
                                entity.Name)
                        .HasMaxLength(
                            40)
                        .IsRequired();

                    tag.HasIndex(
                            entity =>
                                entity.Name)
                        .IsUnique();
                });
    }
}