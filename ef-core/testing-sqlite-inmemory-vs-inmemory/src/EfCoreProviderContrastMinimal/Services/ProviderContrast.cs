using EfCoreProviderContrastMinimal.Data;
using EfCoreProviderContrastMinimal.Models;
using EfCoreProviderContrastMinimal.Providers;
using Microsoft.EntityFrameworkCore;

namespace EfCoreProviderContrastMinimal.Services;

public sealed record
    ConstraintContrast(
        bool InMemoryAccepted,
        bool SqliteRejected);

public sealed record
    TransactionContrast(
        bool InMemoryUnsupported,
        bool SqliteRolledBack);

public sealed record
    CascadeContrast(
        bool InMemoryOrphanRemains,
        bool SqliteChildRemoved);

public sealed record
    ProviderContrastResult(
        ConstraintContrast UniqueConstraint,
        ConstraintContrast ForeignKey,
        TransactionContrast Transaction,
        CascadeContrast Cascade);

public static class ProviderContrast
{
    public static async Task<
        ConstraintContrast>
        CheckUniqueConstraintAsync(
            CancellationToken
                cancellationToken =
                    default)
    {
        bool inMemoryAccepted;

        await using (
            var database =
                new InMemoryTestDatabase())
        {
            await using
                ProviderDbContext context =
                    database
                        .CreateContext();

            context.Projects.AddRange(
                new Project
                {
                    Id = 1,
                    Name = "Duplicate",
                    OwnerId = "owner"
                },
                new Project
                {
                    Id = 2,
                    Name = "Duplicate",
                    OwnerId = "owner"
                });

            await context
                .SaveChangesAsync(
                    cancellationToken);

            inMemoryAccepted =
                await context.Projects
                    .CountAsync(
                        cancellationToken)
                    == 2;
        }

        bool sqliteRejected =
            false;

        await using (
            SqliteTestDatabase database =
                await SqliteTestDatabase
                    .CreateAsync(
                        cancellationToken))
        {
            await using
                ProviderDbContext context =
                    database
                        .CreateContext();

            context.Projects.AddRange(
                new Project
                {
                    Id = 1,
                    Name = "Duplicate",
                    OwnerId = "owner"
                },
                new Project
                {
                    Id = 2,
                    Name = "Duplicate",
                    OwnerId = "owner"
                });

            try
            {
                await context
                    .SaveChangesAsync(
                        cancellationToken);
            }
            catch (DbUpdateException)
            {
                sqliteRejected =
                    true;
            }
        }

        return new ConstraintContrast(
            InMemoryAccepted:
                inMemoryAccepted,

            SqliteRejected:
                sqliteRejected);
    }

    public static async Task<
        ConstraintContrast>
        CheckForeignKeyAsync(
            CancellationToken
                cancellationToken =
                    default)
    {
        bool inMemoryAccepted;

        await using (
            var database =
                new InMemoryTestDatabase())
        {
            await using
                ProviderDbContext context =
                    database
                        .CreateContext();

            context.Tasks.Add(
                new TaskItem
                {
                    Id = 1,
                    Title = "Orphan",
                    ProjectId = 999
                });

            await context
                .SaveChangesAsync(
                    cancellationToken);

            inMemoryAccepted =
                await context.Tasks
                    .AnyAsync(
                        cancellationToken);
        }

        bool sqliteRejected =
            false;

        await using (
            SqliteTestDatabase database =
                await SqliteTestDatabase
                    .CreateAsync(
                        cancellationToken))
        {
            await using
                ProviderDbContext context =
                    database
                        .CreateContext();

            context.Tasks.Add(
                new TaskItem
                {
                    Id = 1,
                    Title = "Orphan",
                    ProjectId = 999
                });

            try
            {
                await context
                    .SaveChangesAsync(
                        cancellationToken);
            }
            catch (DbUpdateException)
            {
                sqliteRejected =
                    true;
            }
        }

        return new ConstraintContrast(
            InMemoryAccepted:
                inMemoryAccepted,

            SqliteRejected:
                sqliteRejected);
    }

    public static async Task<
        TransactionContrast>
        CheckTransactionAsync(
            CancellationToken
                cancellationToken =
                    default)
    {
        bool inMemoryUnsupported =
            false;

        await using (
            var database =
                new InMemoryTestDatabase())
        {
            await using
                ProviderDbContext context =
                    database
                        .CreateContext();

            try
            {
                await context.Database
                    .BeginTransactionAsync(
                        cancellationToken);
            }
            catch (InvalidOperationException)
            {
                inMemoryUnsupported =
                    true;
            }
        }

        bool sqliteRolledBack;

        await using (
            SqliteTestDatabase database =
                await SqliteTestDatabase
                    .CreateAsync(
                        cancellationToken))
        {
            await using (
                ProviderDbContext write =
                    database
                        .CreateContext())
            {
                await using
                    var transaction =
                        await write.Database
                            .BeginTransactionAsync(
                                cancellationToken);

                write.Projects.Add(
                    new Project
                    {
                        Id = 1,
                        Name = "Rolled Back",
                        OwnerId = "owner"
                    });

                await write
                    .SaveChangesAsync(
                        cancellationToken);

                await transaction
                    .RollbackAsync(
                        cancellationToken);
            }

            await using
                ProviderDbContext verify =
                    database
                        .CreateContext();

            sqliteRolledBack =
                !await verify.Projects
                    .AnyAsync(
                        cancellationToken);
        }

        return new TransactionContrast(
            InMemoryUnsupported:
                inMemoryUnsupported,

            SqliteRolledBack:
                sqliteRolledBack);
    }

    public static async Task<
        CascadeContrast>
        CheckCascadeAsync(
            CancellationToken
                cancellationToken =
                    default)
    {
        bool inMemoryOrphanRemains;

        await using (
            var database =
                new InMemoryTestDatabase())
        {
            await SeedCascadeGraphAsync(
                database.CreateContext(),
                cancellationToken);

            await using (
                ProviderDbContext delete =
                    database
                        .CreateContext())
            {
                Project project =
                    await delete.Projects
                        .SingleAsync(
                            entity =>
                                entity.Id
                                    == 1,
                            cancellationToken);

                delete.Projects
                    .Remove(
                        project);

                await delete
                    .SaveChangesAsync(
                        cancellationToken);
            }

            await using
                ProviderDbContext verify =
                    database
                        .CreateContext();

            inMemoryOrphanRemains =
                await verify.Tasks
                    .AnyAsync(
                        task =>
                            task.ProjectId
                                == 1,
                        cancellationToken);
        }

        bool sqliteChildRemoved;

        await using (
            SqliteTestDatabase database =
                await SqliteTestDatabase
                    .CreateAsync(
                        cancellationToken))
        {
            await SeedCascadeGraphAsync(
                database.CreateContext(),
                cancellationToken);

            await using (
                ProviderDbContext delete =
                    database
                        .CreateContext())
            {
                Project project =
                    await delete.Projects
                        .SingleAsync(
                            entity =>
                                entity.Id
                                    == 1,
                            cancellationToken);

                delete.Projects
                    .Remove(
                        project);

                await delete
                    .SaveChangesAsync(
                        cancellationToken);
            }

            await using
                ProviderDbContext verify =
                    database
                        .CreateContext();

            sqliteChildRemoved =
                !await verify.Tasks
                    .AnyAsync(
                        task =>
                            task.ProjectId
                                == 1,
                        cancellationToken);
        }

        return new CascadeContrast(
            InMemoryOrphanRemains:
                inMemoryOrphanRemains,

            SqliteChildRemoved:
                sqliteChildRemoved);
    }

    public static async Task<
        ProviderContrastResult>
        RunAsync(
            CancellationToken
                cancellationToken =
                    default)
    {
        ConstraintContrast
            unique =
                await CheckUniqueConstraintAsync(
                    cancellationToken);

        ConstraintContrast
            foreignKey =
                await CheckForeignKeyAsync(
                    cancellationToken);

        TransactionContrast
            transaction =
                await CheckTransactionAsync(
                    cancellationToken);

        CascadeContrast
            cascade =
                await CheckCascadeAsync(
                    cancellationToken);

        return new ProviderContrastResult(
            UniqueConstraint:
                unique,

            ForeignKey:
                foreignKey,

            Transaction:
                transaction,

            Cascade:
                cascade);
    }

    private static async Task
        SeedCascadeGraphAsync(
            ProviderDbContext context,
            CancellationToken
                cancellationToken)
    {
        await using (
            context)
        {
            var project =
                new Project
                {
                    Id = 1,
                    Name = "Cascade",
                    OwnerId = "owner"
                };

            project.Tasks.Add(
                new TaskItem
                {
                    Id = 1,
                    Title = "Child",
                    ProjectId = 1
                });

            context.Projects.Add(
                project);

            await context
                .SaveChangesAsync(
                    cancellationToken);
        }
    }
}