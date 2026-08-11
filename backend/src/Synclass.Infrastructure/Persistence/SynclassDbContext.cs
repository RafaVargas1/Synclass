using Microsoft.EntityFrameworkCore;
using Synclass.Domain.Matriculas;
using Synclass.Domain.Usuarios;

namespace Synclass.Infrastructure.Persistence;

/// <summary>
/// Contexto de banco de dados do Synclass. Os requisitos funcionais
/// (Professor, Aluno, Horário, etc.) são modelados aqui conforme
/// implementados, seguindo docs/backlog/requisitos-funcionais.md.
/// </summary>
public sealed class SynclassDbContext : DbContext
{
    public SynclassDbContext(DbContextOptions<SynclassDbContext> options)
        : base(options)
    {
    }

    public DbSet<Usuario> Usuarios => Set<Usuario>();

    public DbSet<Matricula> Matriculas => Set<Matricula>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SynclassDbContext).Assembly);
    }
}
