using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace EnrollDesk.Api.Data;

/// <summary>Lets `dotnet ef migrations add` generate a SQL Server schema without starting the app.</summary>
public class EnrollDeskDbContextFactory : IDesignTimeDbContextFactory<EnrollDeskDbContext>
{
    public EnrollDeskDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<EnrollDeskDbContext>();
        optionsBuilder.UseSqlServer("Server=localhost,1433;Database=EnrollDesk;User Id=sa;Password=EnrollDesk_2026!;TrustServerCertificate=True;Encrypt=False");
        return new EnrollDeskDbContext(optionsBuilder.Options);
    }
}
