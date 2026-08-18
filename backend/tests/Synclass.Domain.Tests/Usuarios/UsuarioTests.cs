using FluentAssertions;
using Synclass.Domain.Tests.Fakes;
using Synclass.Domain.Usuarios;

namespace Synclass.Domain.Tests.Usuarios;

public sealed class UsuarioTests
{
    private static readonly FixedClock Clock = new(new DateTimeOffset(2026, 8, 11, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Cadastrar_DadosValidos_CriaUsuarioComPapelInformado()
    {
        var usuario = Usuario.Cadastrar("Maria Silva", "maria@exemplo.com", PapelUsuario.Professor, Clock);

        usuario.Nome.Should().Be("Maria Silva");
        usuario.Contato.Should().Be("maria@exemplo.com");
        usuario.Papeis.Should().ContainSingle(p => p.Papel == PapelUsuario.Professor);
    }

    [Fact]
    public void AdicionarPapel_PapelDiferenteDoExistente_AdicionaSemDuplicarIdentidade()
    {
        var usuario = Usuario.Cadastrar("Maria Silva", "maria@exemplo.com", PapelUsuario.Aluno, Clock);

        usuario.AdicionarPapel(PapelUsuario.Professor, Clock);

        usuario.Papeis.Should().HaveCount(2);
        usuario.Papeis.Should().Contain(p => p.Papel == PapelUsuario.Aluno);
        usuario.Papeis.Should().Contain(p => p.Papel == PapelUsuario.Professor);
    }

    [Fact]
    public void AdicionarPapel_PapelJaAtribuido_LancaPapelJaAtribuidoException()
    {
        var usuario = Usuario.Cadastrar("Maria Silva", "maria@exemplo.com", PapelUsuario.Professor, Clock);

        var acao = () => usuario.AdicionarPapel(PapelUsuario.Professor, Clock);

        acao.Should().Throw<PapelJaAtribuidoException>();
    }

    [Fact]
    public void AtualizarNome_NomeValido_TrocaNome()
    {
        var usuario = Usuario.Cadastrar("Maria Silva", "maria@exemplo.com", PapelUsuario.Professor, Clock);

        usuario.AtualizarNome("Maria Souza");

        usuario.Nome.Should().Be("Maria Souza");
    }

    [Fact]
    public void AtualizarNome_ComEspacosNasBordas_Normaliza()
    {
        var usuario = Usuario.Cadastrar("Maria Silva", "maria@exemplo.com", PapelUsuario.Professor, Clock);

        usuario.AtualizarNome("  Maria Souza  ");

        usuario.Nome.Should().Be("Maria Souza");
    }

    [Fact]
    public void AtualizarNome_NomeVazio_LancaNomeInvalidoExceptionSemAlterarNome()
    {
        var usuario = Usuario.Cadastrar("Maria Silva", "maria@exemplo.com", PapelUsuario.Professor, Clock);

        var acao = () => usuario.AtualizarNome("   ");

        acao.Should().Throw<NomeInvalidoException>();
        usuario.Nome.Should().Be("Maria Silva");
    }
}
