using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Synclass.Infrastructure.Persistence;

/// <summary>
/// Permite rodar `dotnet ef migrations add/update` diretamente a partir do
/// projeto Infrastructure, sem depender do host da Api em tempo de design.
/// A connection string real (runtime) vem de appsettings/variáveis de
/// ambiente configuradas em Synclass.Api; aqui usamos uma string local de
/// desenvolvimento equivalente à do docker-compose.
/// </summary>
public sealed class SynclassDbContextFactory : IDesignTimeDbContextFactory<SynclassDbContext>
{
    private const string LocalDevelopmentConnectionString =
        "Host=localhost;Port=5432;Database=synclass;Username=synclass;Password=synclass";

    public SynclassDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("CONNECTIONSTRINGS__DEFAULT")
            ?? LocalDevelopmentConnectionString;

        var optionsBuilder = new DbContextOptionsBuilder<SynclassDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new SynclassDbContext(optionsBuilder.Options);
    }
}
