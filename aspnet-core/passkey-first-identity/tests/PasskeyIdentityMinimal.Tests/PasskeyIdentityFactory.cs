using System.Data.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PasskeyIdentityMinimal.Data;

namespace PasskeyIdentityMinimal.Tests;

public sealed class PasskeyIdentityFactory
    : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(
        IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Remove the real DbContext registration
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();

            // Use a shared in-memory SQLite connection so EnsureCreated
            // and the test WebApplicationFactory share the same database.
            var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();

            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseSqlite(connection);
            });
        });
    }
}