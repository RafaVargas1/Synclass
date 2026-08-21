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
        var usuario = Usuario.Cadastrar("Maria Silva", "maria@exemplo.com", PapelUsuario.Professor, null, Clock);

        usuario.Nome.Should().Be("Maria Silva");
        usuario.Contato.Should().Be("maria@exemplo.com");
        usuario.Papeis.Should().ContainSingle(p => p.Papel == PapelUsuario.Professor);
    }

    [Fact]
    public void AdicionarPapel_PapelDiferenteDoExistente_AdicionaSemDuplicarIdentidade()
    {
        var usuario = Usuario.Cadastrar("Maria Silva", "maria@exemplo.com", PapelUsuario.Aluno, null, Clock);

        usuario.AdicionarPapel(PapelUsuario.Professor, null, Clock);

        usuario.Papeis.Should().HaveCount(2);
        usuario.Papeis.Should().Contain(p => p.Papel == PapelUsuario.Aluno);
        usuario.Papeis.Should().Contain(p => p.Papel == PapelUsuario.Professor);
    }

    [Fact]
    public void AdicionarPapel_PapelJaAtribuido_LancaPapelJaAtribuidoException()
    {
        var usuario = Usuario.Cadastrar("Maria Silva", "maria@exemplo.com", PapelUsuario.Professor, null, Clock);

        var acao = () => usuario.AdicionarPapel(PapelUsuario.Professor, null, Clock);

        acao.Should().Throw<PapelJaAtribuidoException>();
    }

    [Fact]
    public void AtualizarNome_NomeValido_TrocaNome()
    {
        var usuario = Usuario.Cadastrar("Maria Silva", "maria@exemplo.com", PapelUsuario.Professor, null, Clock);

        usuario.AtualizarNome("Maria Souza");

        usuario.Nome.Should().Be("Maria Souza");
    }

    [Fact]
    public void AtualizarNome_ComEspacosNasBordas_Normaliza()
    {
        var usuario = Usuario.Cadastrar("Maria Silva", "maria@exemplo.com", PapelUsuario.Professor, null, Clock);

        usuario.AtualizarNome("  Maria Souza  ");

        usuario.Nome.Should().Be("Maria Souza");
    }

    [Fact]
    public void AtualizarNome_NomeVazio_LancaNomeInvalidoExceptionSemAlterarNome()
    {
        var usuario = Usuario.Cadastrar("Maria Silva", "maria@exemplo.com", PapelUsuario.Professor, null, Clock);

        var acao = () => usuario.AtualizarNome("   ");

        acao.Should().Throw<NomeInvalidoException>();
        usuario.Nome.Should().Be("Maria Silva");
    }

    [Fact]
    public void Cadastrar_PapelAlunoComIdentificador_GravaIdentificadorAluno()
    {
        var usuario = Usuario.Cadastrar("João Pedro", "joao@exemplo.com", PapelUsuario.Aluno, "ALU-2B7K", Clock);

        usuario.IdentificadorAluno.Should().Be("ALU-2B7K");
    }

    [Fact]
    public void Cadastrar_PapelProfessorComIdentificador_NaoGravaIdentificadorAluno()
    {
        var usuario = Usuario.Cadastrar("Maria Silva", "maria@exemplo.com", PapelUsuario.Professor, "ALU-2B7K", Clock);

        usuario.IdentificadorAluno.Should().BeNull();
    }

    [Fact]
    public void AdicionarPapel_AlunoComIdentificador_PrimeiraVezGravaIdentificador()
    {
        var usuario = Usuario.Cadastrar("Maria Silva", "maria@exemplo.com", PapelUsuario.Professor, null, Clock);

        usuario.AdicionarPapel(PapelUsuario.Aluno, "ALU-2B7K", Clock);

        usuario.IdentificadorAluno.Should().Be("ALU-2B7K");
        usuario.Papeis.Should().Contain(p => p.Papel == PapelUsuario.Aluno);
    }

    [Fact]
    public void AdicionarPapel_AlunoJaPresenteComNovoIdentificador_LancaPapelJaAtribuidoSemTocarNoIdentificadorGravado()
    {
        var usuario = Usuario.Cadastrar("Maria Silva", "maria@exemplo.com", PapelUsuario.Aluno, "ALU-2B7K", Clock);

        var acao = () => usuario.AdicionarPapel(PapelUsuario.Aluno, "ALU-4RTY", Clock);

        acao.Should().Throw<PapelJaAtribuidoException>();
        usuario.IdentificadorAluno.Should().Be("ALU-2B7K");
    }
}
