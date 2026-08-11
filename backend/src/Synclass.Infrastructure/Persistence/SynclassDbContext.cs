using Microsoft.EntityFrameworkCore;

namespace Synclass.Infrastructure.Persistence;

/// <summary>
/// Contexto de banco de dados do Synclass. Nesta fundação não há DbSets
/// ainda — os requisitos funcionais (Professor, Aluno, Horário, etc.) serão
/// modelados aqui conforme forem implementados, seguindo
/// docs/backlog/requisitos-funcionais.md.
/// </summary>
public sealed class SynclassDbContext : DbContext
{
    public SynclassDbContext(DbContextOptions<SynclassDbContext> options)
        : base(options)
    {
    }
}
