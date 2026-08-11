using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Synclass.Infrastructure.Persistence;

public static class MigrationRunner
{
    /// <summary>
    /// Aplica migrations pendentes do EF Core. Usado no startup da API em
    /// ambientes de desenvolvimento (ver appsettings.Development.json /
    /// docker-compose.yml) para que o container suba com o schema em dia
    /// sem passo manual.
    /// </summary>
    public static void ApplyPendingMigrations(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SynclassDbContext>();
        dbContext.Database.Migrate();
    }
}
