using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Synclass.Domain.Usuarios;
using Synclass.Infrastructure.Common;
using Synclass.Infrastructure.Persistence;

namespace Synclass.Api.Tests;

/// <summary>
/// Cobre o caminho de reaproveitamento de identidade (usuário já existe,
/// ganha um novo papel) contra um provider relacional de verdade — o
/// provider EF Core InMemory usado nos demais testes não executa
/// UPDATE/INSERT reais nem calcula linhas afetadas, então não detectaria a
/// regressão real encontrada em produção: <see cref="PapelAtribuido"/> tem
/// chave Guid gerada pela aplicação, e sem <c>ValueGeneratedNever()</c> na
/// configuração, o EF Core assume — ao descobrir a nova entidade via a
/// navegação <c>Papeis</c> sem um <c>Add()</c> explícito — que ela já existe
/// no banco, gera um UPDATE em vez de INSERT, e lança
/// <c>DbUpdateConcurrencyException</c> (0 linhas afetadas) mesmo sem
/// nenhuma concorrência real (ver <see cref="Configurations.PapelAtribuidoConfiguration"/>).
/// </summary>
public sealed class UsuarioRepositoryReaproveitamentoTests
{
    [Fact]
    public async Task SalvarAsync_AdicionaPapelAUsuarioJaExistente_PersisteSemLancarConcorrencia()
    {
        await using var conexao = new SqliteConnection("DataSource=:memory:");
        await conexao.OpenAsync();
        var options = new DbContextOptionsBuilder<SynclassDbContext>().UseSqlite(conexao).Options;
        var clock = new SystemClock();

        await using (var dbContextDeSchema = new SynclassDbContext(options))
        {
            await dbContextDeSchema.Database.EnsureCreatedAsync();
        }

        Guid usuarioId;
        await using (var dbContextDoCadastroInicial = new SynclassDbContext(options))
        {
            var repositorioInicial = new UsuarioRepository(dbContextDoCadastroInicial);
            var usuario = Usuario.Cadastrar("Joao Aluno", "joao@exemplo.com", PapelUsuario.Aluno, clock);
            usuarioId = usuario.Id;
            await repositorioInicial.AdicionarAsync(usuario, CancellationToken.None);
            await repositorioInicial.SalvarAsync(CancellationToken.None);
        }

        // Segunda "requisição": carrega o usuário já existente (novo
        // DbContext, exatamente como acontece entre requisições HTTP reais)
        // e adiciona o papel Professor, sem nenhum Add() explícito no
        // repositório — só a mutação do agregado via AdicionarPapel.
        await using var dbContextDoNovoCadastro = new SynclassDbContext(options);
        var repositorio = new UsuarioRepository(dbContextDoNovoCadastro);
        var usuarioExistente = await repositorio.BuscarPorContatoAsync("joao@exemplo.com", CancellationToken.None);
        usuarioExistente!.AdicionarPapel(PapelUsuario.Professor, clock);

        var acao = () => repositorio.SalvarAsync(CancellationToken.None);

        await acao.Should().NotThrowAsync();

        await using var dbContextDeVerificacao = new SynclassDbContext(options);
        var papeis = await dbContextDeVerificacao.Usuarios
            .Include(u => u.Papeis)
            .Where(u => u.Id == usuarioId)
            .SelectMany(u => u.Papeis)
            .Select(p => p.Papel)
            .ToListAsync();
        papeis.Should().BeEquivalentTo([PapelUsuario.Aluno, PapelUsuario.Professor]);
    }
}
