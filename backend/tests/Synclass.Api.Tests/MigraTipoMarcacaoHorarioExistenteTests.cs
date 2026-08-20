using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Synclass.Domain.Configuracoes;
using Synclass.Domain.Horarios;
using Synclass.Infrastructure.Persistence;

namespace Synclass.Api.Tests;

/// <summary>
/// Cobre a migration de dados <c>MigraTipoMarcacaoHorarioExistente</c>
/// (issue #75): deriva <c>Horarios.TipoMarcacao</c> a partir de
/// <c>ConfiguracoesProfessor.ModeloAgendamento</c> +
/// <c>AlocacoesHorario.OrigemAlocacao</c> para horários cadastrados antes da
/// issue #73 existir — ver docs/specs/75-migra-tipo-marcacao-horario/implementation.md.
///
/// Usa <c>Database.MigrateAsync</c> (não <c>EnsureCreatedAsync</c>, usado nos
/// demais testes Sqlite deste projeto): <c>EnsureCreatedAsync</c> cria o
/// schema a partir do modelo atual, pulando o histórico de migrations, e por
/// isso não exercitaria o <c>Up()</c> desta migration especificamente.
/// </summary>
public sealed class MigraTipoMarcacaoHorarioExistenteTests
{
    // Migration anterior a esta (Task #73, já em main) — migrar só até ela
    // reproduz o estado "pré-#75": coluna TipoMarcacao já existe, mas nenhum
    // dado ainda foi migrado por esta Task.
    private const string MigrationAnterior = "20260820022555_AdicionaTipoMarcacaoHorario";

    [Fact]
    public async Task Migrate_ProfessorComModeloVago_AplicaTipoMarcacaoLivre()
    {
        await ExecutarCenarioEVerificarAsync(ModeloAgendamento.Vago, comAlocacaoOrigemProfessor: false, TipoMarcacao.Livre);
    }

    [Fact]
    public async Task Migrate_ProfessorComModeloFixo_AplicaTipoMarcacaoFixo()
    {
        await ExecutarCenarioEVerificarAsync(ModeloAgendamento.Fixo, comAlocacaoOrigemProfessor: false, TipoMarcacao.Fixo);
    }

    [Fact]
    public async Task Migrate_ProfessorComModeloHibridoComAlocacaoDeProfessor_AplicaTipoMarcacaoFixo()
    {
        await ExecutarCenarioEVerificarAsync(ModeloAgendamento.Hibrido, comAlocacaoOrigemProfessor: true, TipoMarcacao.Fixo);
    }

    [Fact]
    public async Task Migrate_ProfessorComModeloHibridoSemAlocacao_AplicaTipoMarcacaoLivre()
    {
        await ExecutarCenarioEVerificarAsync(ModeloAgendamento.Hibrido, comAlocacaoOrigemProfessor: false, TipoMarcacao.Livre);
    }

    [Fact]
    public async Task Migrate_HorarioOrfaoSemConfiguracaoProfessor_AplicaTipoMarcacaoLivreComoDefault()
    {
        await ExecutarCenarioEVerificarAsync(modeloAgendamento: null, comAlocacaoOrigemProfessor: false, TipoMarcacao.Livre);
    }

    private static async Task ExecutarCenarioEVerificarAsync(
        ModeloAgendamento? modeloAgendamento, bool comAlocacaoOrigemProfessor, TipoMarcacao tipoMarcacaoEsperado)
    {
        await using var conexao = new SqliteConnection("DataSource=:memory:");
        await conexao.OpenAsync();
        var options = new DbContextOptionsBuilder<SynclassDbContext>().UseSqlite(conexao).Options;

        var horarioId = await PrepararEstadoPreMigrationAsync(options, modeloAgendamento, comAlocacaoOrigemProfessor);

        await using (var dbContextDaMigration = new SynclassDbContext(options))
        {
            // Sem argumento: avança até a última migration pendente,
            // incluindo MigraTipoMarcacaoHorarioExistente.
            await dbContextDaMigration.Database.MigrateAsync();
        }

        var tipoMarcaoPersistido = await LerTipoMarcacaoAsync(options, horarioId);

        tipoMarcaoPersistido.Should().Be((int)tipoMarcacaoEsperado);
    }

    /// <summary>
    /// Migra só até a migration anterior a #75 e popula, via SQL cru, o
    /// cenário "pré-migration" da tabela de derivação — o DbContext/EF model
    /// atual não tem mais como gerar uma entidade <see cref="Horario"/> sem
    /// um <see cref="TipoMarcacao"/> qualquer definido, então inserir direto
    /// via SQL é a única forma de simular o estado anterior a esta Task
    /// (o valor de TipoMarcacao inserido aqui é só um placeholder,
    /// sobrescrito pela migration sob teste).
    /// </summary>
    private static async Task<Guid> PrepararEstadoPreMigrationAsync(
        DbContextOptions<SynclassDbContext> options, ModeloAgendamento? modeloAgendamento, bool comAlocacaoOrigemProfessor)
    {
        await using (var dbContextDeSchema = new SynclassDbContext(options))
        {
            // DatabaseFacade.MigrateAsync não aceita migration alvo (só a
            // versão síncrona aceita) — usa o IMigrator diretamente para
            // migrar só até a migration anterior a #75.
            var migrator = dbContextDeSchema.GetInfrastructure().GetRequiredService<IMigrator>();
            await migrator.MigrateAsync(MigrationAnterior);
        }

        var professorId = Guid.NewGuid();
        var horarioId = Guid.NewGuid();
        var agora = DateTimeOffset.UtcNow;

        await using var dbContext = new SynclassDbContext(options);
        await dbContext.Database.ExecuteSqlAsync(
            $"INSERT INTO Usuarios (Id, Nome, Contato, CreatedAt) VALUES ({professorId}, 'Professor Teste', {$"professor-{professorId}@exemplo.com"}, {agora})");
        await dbContext.Database.ExecuteSqlAsync(
            $"INSERT INTO Horarios (Id, ProfessorId, DiaSemana, HoraInicio, DuracaoMinutos, TipoMarcacao, CreatedAt, LimiteAlunos) VALUES ({horarioId}, {professorId}, 0, '08:00:00', 60, 0, {agora}, 1)");

        if (modeloAgendamento.HasValue)
        {
            await dbContext.Database.ExecuteSqlAsync(
                $"INSERT INTO ConfiguracoesProfessor (Id, ProfessorId, ModeloAgendamento, CreatedAt, UpdatedAt) VALUES ({Guid.NewGuid()}, {professorId}, {(int)modeloAgendamento.Value}, {agora}, {agora})");
        }

        if (comAlocacaoOrigemProfessor)
        {
            await CriarAlocacaoDeOrigemProfessorAsync(dbContext, professorId, horarioId, agora);
        }

        return horarioId;
    }

    private static async Task CriarAlocacaoDeOrigemProfessorAsync(
        SynclassDbContext dbContext, Guid professorId, Guid horarioId, DateTimeOffset agora)
    {
        var alunoId = Guid.NewGuid();
        var matriculaId = Guid.NewGuid();

        await dbContext.Database.ExecuteSqlAsync(
            $"INSERT INTO Usuarios (Id, Nome, Contato, CreatedAt) VALUES ({alunoId}, 'Aluno Teste', {$"aluno-{alunoId}@exemplo.com"}, {agora})");
        await dbContext.Database.ExecuteSqlAsync(
            $"INSERT INTO Matriculas (Id, ProfessorId, AlunoUsuarioId, CreatedAt) VALUES ({matriculaId}, {professorId}, {alunoId}, {agora})");
        await dbContext.Database.ExecuteSqlAsync(
            $"INSERT INTO AlocacoesHorario (Id, HorarioId, MatriculaId, OrigemAlocacao, CreatedAt) VALUES ({Guid.NewGuid()}, {horarioId}, {matriculaId}, 0, {agora})");
    }

    private static async Task<int> LerTipoMarcacaoAsync(DbContextOptions<SynclassDbContext> options, Guid horarioId)
    {
        await using var dbContext = new SynclassDbContext(options);
        return await dbContext.Database
            .SqlQuery<int>($"SELECT \"TipoMarcacao\" AS \"Value\" FROM \"Horarios\" WHERE \"Id\" = {horarioId}")
            .SingleAsync();
    }
}
