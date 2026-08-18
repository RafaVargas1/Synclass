using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Synclass.Domain.Cobrancas;
using Synclass.Infrastructure.Common;
using Synclass.Infrastructure.Persistence;

namespace Synclass.Api.Tests;

/// <summary>
/// Cobre o edge point de concorrência do upsert de regra de cobrança
/// (achado de dev-review, rodada 2 do PR #31): duas escritas quase
/// simultâneas para a mesma <c>MatriculaId</c> podem ambas ler "sem regra
/// anterior" antes de qualquer uma confirmar — o índice único do banco é a
/// última linha de defesa, e <see cref="RegraDeCobrancaRepository"/> deve
/// traduzir essa violação para <see cref="RegraDeCobrancaConflitanteException"/>
/// em vez de deixar o <see cref="DbUpdateException"/> de infraestrutura
/// vazar para a Api sem tratamento (viraria 500).
///
/// Mesmo motivo de <c>UsuarioRepositoryConcurrencyTests</c> para usar
/// SQLite em memória em vez do provider EF Core InMemory: o InMemory não
/// aplica índice único de fato (confirmado naquele teste).
/// </summary>
public sealed class RegraDeCobrancaRepositoryConcurrencyTests
{
    [Fact]
    public async Task SalvarAsync_ComDuasEscritasConcorrentesParaMesmaMatricula_LancaRegraDeCobrancaConflitanteException()
    {
        await using var conexao = new SqliteConnection("DataSource=:memory:");
        await conexao.OpenAsync();
        var options = new DbContextOptionsBuilder<SynclassDbContext>().UseSqlite(conexao).Options;
        var clock = new SystemClock();
        var matriculaId = Guid.NewGuid();

        await using (var dbContextDeSchema = new SynclassDbContext(options))
        {
            await dbContextDeSchema.Database.EnsureCreatedAsync();
        }

        await using var dbContextDaPrimeiraRequisicao = new SynclassDbContext(options);
        await using var dbContextDaSegundaRequisicao = new SynclassDbContext(options);
        var repositorioDaPrimeira = new RegraDeCobrancaRepository(dbContextDaPrimeiraRequisicao);
        var repositorioDaSegunda = new RegraDeCobrancaRepository(dbContextDaSegundaRequisicao);

        // Ambas as requisições leem "sem regra anterior" (nenhuma commitou
        // ainda) e disparam a escrita em paralelo — mesmo cenário descrito
        // em RegraDeCobrancaConflitanteException.
        var tarefaDaPrimeira = repositorioDaPrimeira.SalvarAsync(
            RegraFixoMensal.Criar(matriculaId, 100m, clock), CancellationToken.None);
        var tarefaDaSegunda = repositorioDaSegunda.SalvarAsync(
            RegraFixoMensal.Criar(matriculaId, 200m, clock), CancellationToken.None);

        Func<Task> acao = () => Task.WhenAll(tarefaDaPrimeira, tarefaDaSegunda);

        await acao.Should().ThrowAsync<RegraDeCobrancaConflitanteException>();
    }
}
