using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace HybridSearch.Api.Data;

public static class DatabaseInitializer
{
    public static async Task InitializeAsync(
        IServiceProvider services,
        string connectionString,
        CancellationToken cancellationToken = default)
    {
        await using (NpgsqlConnection connection = new(connectionString))
        {
            await connection.OpenAsync(cancellationToken);
            await using NpgsqlCommand command = new(
                "CREATE EXTENSION IF NOT EXISTS vector;",
                connection);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        using IServiceScope scope = services.CreateScope();
        SearchDbContext db = scope.ServiceProvider.GetRequiredService<SearchDbContext>();
        await db.Database.EnsureCreatedAsync(cancellationToken);
    }
}