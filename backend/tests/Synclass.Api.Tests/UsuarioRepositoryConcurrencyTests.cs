using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Synclass.Domain.Usuarios;
using Synclass.Infrastructure.Common;
using Synclass.Infrastructure.Persistence;

namespace Synclass.Api.Tests;

/// <summary>
/// Cobre o edge point de concorrência do cadastro de Professor: duas
/// escritas para o mesmo contato podem passar pela checagem de duplicidade
/// da aplicação antes de qualquer uma confirmar (race condition) — o índice
/// único do banco é a última linha de defesa, e <see cref="UsuarioRepository"/>
/// deve traduzir essa violação para <see cref="CadastroConcorrenteException"/>
/// em vez de deixar o <see cref="DbUpdateException"/> de infraestrutura
/// vazar para a Api sem tratamento.
///
/// Usa SQLite em memória (não o provider EF Core InMemory usado nos demais
/// testes deste projeto): o provider InMemory não aplica índices únicos de
/// fato (confirmado empiricamente), então não conseguiria exercitar este
/// caminho. SQLite aplica `UNIQUE INDEX` de verdade, como o Postgres real.
/// </summary>
public sealed class UsuarioRepositoryConcurrencyTests
{
    [Fact]
    public async Task SalvarAsync_ContatoJaPersistidoPorOutraEscrita_LancaCadastroConcorrenteException()
    {
        // A conexão SQLite ":memory:" só existe enquanto uma conexão
        // permanece aberta — mantê-la aberta pela duração do teste é o que
        // faz os dois DbContext abaixo compartilharem o mesmo banco.
        await using var conexao = new SqliteConnection("DataSource=:memory:");
        await conexao.OpenAsync();
        var options = new DbContextOptionsBuilder<SynclassDbContext>()
            .UseSqlite(conexao)
            .Options;
        var clock = new SystemClock();

        await using (var dbContextDeSchema = new SynclassDbContext(options))
        {
            await dbContextDeSchema.Database.EnsureCreatedAsync();
        }

        await using (var dbContextDaPrimeiraRequisicao = new SynclassDbContext(options))
        {
            var primeiroUsuario = Usuario.Cadastrar("Maria Silva", "concorrente@exemplo.com", PapelUsuario.Professor, null, clock);
            dbContextDaPrimeiraRequisicao.Usuarios.Add(primeiroUsuario);
            await dbContextDaPrimeiraRequisicao.SaveChangesAsync();
        }

        // Segunda requisição: já passou pela checagem de duplicidade da
        // aplicação (não encontrou ninguém) antes da primeira commitar,
        // então tenta inserir outra identidade com o mesmo contato.
        await using var dbContextDaSegundaRequisicao = new SynclassDbContext(options);
        var repositorio = new UsuarioRepository(dbContextDaSegundaRequisicao);
        var usuarioConcorrente = Usuario.Cadastrar("Outra Maria", "concorrente@exemplo.com", PapelUsuario.Professor, null, clock);
        await repositorio.AdicionarAsync(usuarioConcorrente, CancellationToken.None);

        var acao = () => repositorio.SalvarAsync(CancellationToken.None);

        await acao.Should().ThrowAsync<CadastroConcorrenteException>();
    }
}
