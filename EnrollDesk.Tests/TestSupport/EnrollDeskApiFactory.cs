using EnrollDesk.Api.Auth;
using EnrollDesk.Api.Data;
using EnrollDesk.Api.Domain;
using EnrollDesk.Api.Security;
using EnrollDesk.Api.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace EnrollDesk.Tests.TestSupport;

/// <summary>Boots the real API pipeline against an isolated SQLite in-memory database; no SQL Server required.</summary>
public class EnrollDeskApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private SqliteConnection _connection = null!;

    public FakeClock Clock { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<EnrollDeskDbContext>>();
            services.AddDbContext<EnrollDeskDbContext>(options => options.UseSqlite(_connection));

            services.RemoveAll<IClock>();
            services.AddSingleton<IClock>(Clock);
        });
    }

    public async Task InitializeAsync()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        await _connection.OpenAsync();

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EnrollDeskDbContext>();
        await db.Database.EnsureCreatedAsync();
    }

    public new async Task DisposeAsync()
    {
        await _connection.DisposeAsync();
        Dispose();
    }

    public async Task<string> CreateRegistrarTokenAsync(string email = "registrar@test.local")
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EnrollDeskDbContext>();
        var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();

        var (hash, salt) = PasswordHasher.Hash("Registrar#1234");
        var user = new User
        {
            FullName = "Test Registrar", Email = email, PasswordHash = hash, PasswordSalt = salt,
            Role = UserRole.Registrar, CreatedAtUtc = Clock.UtcNow
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        return tokenService.CreateToken(user).Token;
    }

    public async Task<T> ExecuteDbAsync<T>(Func<EnrollDeskDbContext, Task<T>> action)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EnrollDeskDbContext>();
        return await action(db);
    }

    public async Task ExecuteDbAsync(Func<EnrollDeskDbContext, Task> action)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EnrollDeskDbContext>();
        await action(db);
    }
}
