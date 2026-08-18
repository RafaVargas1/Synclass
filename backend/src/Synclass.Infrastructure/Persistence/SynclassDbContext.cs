using Microsoft.EntityFrameworkCore;
using Synclass.Domain.Alocacoes;
using Synclass.Domain.Autenticacao;
using Synclass.Domain.Cobrancas;
using Synclass.Domain.Configuracoes;
using Synclass.Domain.Convites;
using Synclass.Domain.Horarios;
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

    public DbSet<CodigoOtp> CodigosOtp => Set<CodigoOtp>();
    public DbSet<Horario> Horarios => Set<Horario>();
    public DbSet<Matricula> Matriculas => Set<Matricula>();
    public DbSet<ConfiguracaoProfessor> ConfiguracoesProfessor => Set<ConfiguracaoProfessor>();
    public DbSet<Convite> Convites => Set<Convite>();
    public DbSet<RegraDeCobranca> RegrasDeCobranca => Set<RegraDeCobranca>();
    public DbSet<AlocacaoHorario> AlocacoesHorario => Set<AlocacaoHorario>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SynclassDbContext).Assembly);
    }
}
