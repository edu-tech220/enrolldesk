using EnrollDesk.Api.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace EnrollDesk.Tests.TestSupport;

/// <summary>Builds an isolated SQLite in-memory database per test; no external SQL Server needed.</summary>
public static class SqliteTestDb
{
    public static async Task<(EnrollDeskDbContext Context, SqliteConnection Connection)> CreateAsync()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<EnrollDeskDbContext>().UseSqlite(connection).Options;
        var context = new EnrollDeskDbContext(options);
        await context.Database.EnsureCreatedAsync();

        return (context, connection);
    }

    /// <summary>Opens a second context against the same in-memory database, as a second request would get its own scoped context.</summary>
    public static EnrollDeskDbContext CreateContext(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<EnrollDeskDbContext>().UseSqlite(connection).Options;
        return new EnrollDeskDbContext(options);
    }
}
