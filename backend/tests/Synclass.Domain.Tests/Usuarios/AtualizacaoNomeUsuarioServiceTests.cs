using FluentAssertions;
using Synclass.Domain.Tests.Fakes;
using Synclass.Domain.Usuarios;

namespace Synclass.Domain.Tests.Usuarios;

/// <summary>
/// Cobre a Regra de Negócio da issue #27: corrigir o próprio nome via canal
/// explícito, com a mesma validação de <see cref="NomeUsuario"/> usada no
/// cadastro (issue #1).
/// </summary>
public sealed class AtualizacaoNomeUsuarioServiceTests
{
    private static readonly FixedClock Clock = new(new DateTimeOffset(2026, 8, 18, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public async Task AtualizarNomeAsync_NomeValido_AtualizaEPersisteUsuario()
    {
        var repositorio = new FakeUsuarioRepository();
        var usuario = Usuario.Cadastrar("Maria Silva", "maria@exemplo.com", PapelUsuario.Professor, null, Clock);
        await repositorio.AdicionarAsync(usuario, CancellationToken.None);
        var servico = new AtualizacaoNomeUsuarioService(repositorio);

        var resultado = await servico.AtualizarNomeAsync(usuario.Id, "Maria Souza", CancellationToken.None);

        resultado.Nome.Should().Be("Maria Souza");
        repositorio.Usuarios.Should().ContainSingle(u => u.Nome == "Maria Souza");
    }

    [Fact]
    public async Task AtualizarNomeAsync_UsuarioInexistente_LancaUsuarioNaoEncontradoException()
    {
        var repositorio = new FakeUsuarioRepository();
        var servico = new AtualizacaoNomeUsuarioService(repositorio);

        var acao = () => servico.AtualizarNomeAsync(Guid.NewGuid(), "Maria Souza", CancellationToken.None);

        await acao.Should().ThrowAsync<UsuarioNaoEncontradoException>();
    }

    [Fact]
    public async Task AtualizarNomeAsync_NomeVazio_LancaNomeInvalidoExceptionSemAlterarUsuario()
    {
        var repositorio = new FakeUsuarioRepository();
        var usuario = Usuario.Cadastrar("Maria Silva", "maria@exemplo.com", PapelUsuario.Professor, null, Clock);
        await repositorio.AdicionarAsync(usuario, CancellationToken.None);
        var servico = new AtualizacaoNomeUsuarioService(repositorio);

        var acao = () => servico.AtualizarNomeAsync(usuario.Id, "   ", CancellationToken.None);

        await acao.Should().ThrowAsync<NomeInvalidoException>();
        usuario.Nome.Should().Be("Maria Silva");
    }
}
