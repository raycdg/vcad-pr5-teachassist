using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TeachAssist.Domain.Data;

public class DomainDbContextFactory : IDesignTimeDbContextFactory<DomainDbContext>
{
    public DomainDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<DomainDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Port=5433;Database=teachassist_3_1;Username=postgres;Password=postgres");
        return new DomainDbContext(optionsBuilder.Options);
    }
}