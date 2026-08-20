using FluentAssertions;
using Synclass.Domain.Tests.Fakes;
using Synclass.Domain.Usuarios;

namespace Synclass.Domain.Tests.Usuarios;

/// <summary>
/// Cobre a Regra de Negócio central do cadastro de usuário, comum ao
/// Professor (issue #1) e ao Aluno (issue #61): criar ou reaproveitar
/// identidade de usuário por contato, sem nunca duplicar identidade nem
/// papel. Usa <see cref="FakeUsuarioRepository"/> no lugar de EF/banco real.
/// </summary>
public sealed class CadastroUsuarioServiceTests
{
    private static readonly FixedClock Clock = new(new DateTimeOffset(2026, 8, 11, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public async Task CadastrarAsync_ContatoInexistente_CriaUsuarioComPapelProfessor()
    {
        var repositorio = new FakeUsuarioRepository();
        var servico = new CadastroUsuarioService(repositorio, Clock);

        var resultado = await servico.CadastrarAsync(PapelUsuario.Professor, "Maria Silva", "maria@exemplo.com", CancellationToken.None);

        resultado.Usuario.Papeis.Should().ContainSingle(p => p.Papel == PapelUsuario.Professor);
        repositorio.Usuarios.Should().ContainSingle();
    }

    [Fact]
    public async Task CadastrarAsync_ContatoInexistente_UsuarioReaproveitadoEhFalso()
    {
        var repositorio = new FakeUsuarioRepository();
        var servico = new CadastroUsuarioService(repositorio, Clock);

        var resultado = await servico.CadastrarAsync(PapelUsuario.Professor, "Maria Silva", "maria@exemplo.com", CancellationToken.None);

        resultado.UsuarioReaproveitado.Should().BeFalse();
    }

    [Fact]
    public async Task CadastrarAsync_UsuarioJaExisteComoAluno_AdicionaPapelProfessorSemDuplicarIdentidade()
    {
        var repositorio = new FakeUsuarioRepository();
        var alunoExistente = Usuario.Cadastrar("Maria Silva", "maria@exemplo.com", PapelUsuario.Aluno, Clock);
        await repositorio.AdicionarAsync(alunoExistente, CancellationToken.None);
        var servico = new CadastroUsuarioService(repositorio, Clock);

        // Capitalização e máscara diferentes do contato já cadastrado.
        var resultado = await servico.CadastrarAsync(PapelUsuario.Professor, "Maria Silva", "  MARIA@EXEMPLO.COM  ", CancellationToken.None);

        resultado.Usuario.Id.Should().Be(alunoExistente.Id);
        resultado.Usuario.Papeis.Should().HaveCount(2);
        repositorio.Usuarios.Should().ContainSingle();
    }

    [Fact]
    public async Task CadastrarAsync_UsuarioJaExisteComoAluno_UsuarioReaproveitadoEhTrue()
    {
        var repositorio = new FakeUsuarioRepository();
        var alunoExistente = Usuario.Cadastrar("Maria Silva", "maria@exemplo.com", PapelUsuario.Aluno, Clock);
        await repositorio.AdicionarAsync(alunoExistente, CancellationToken.None);
        var servico = new CadastroUsuarioService(repositorio, Clock);

        var resultado = await servico.CadastrarAsync(PapelUsuario.Professor, "Maria Silva", "maria@exemplo.com", CancellationToken.None);

        resultado.UsuarioReaproveitado.Should().BeTrue();
    }

    [Fact]
    public async Task CadastrarAsync_UsuarioJaEhProfessor_RejeitaComPapelJaAtribuidoException()
    {
        var repositorio = new FakeUsuarioRepository();
        var servico = new CadastroUsuarioService(repositorio, Clock);
        await servico.CadastrarAsync(PapelUsuario.Professor, "Maria Silva", "maria@exemplo.com", CancellationToken.None);

        var acao = () => servico.CadastrarAsync(PapelUsuario.Professor, "Maria Silva", "maria@exemplo.com", CancellationToken.None);

        await acao.Should().ThrowAsync<PapelJaAtribuidoException>();
        repositorio.Usuarios.Should().ContainSingle();
    }

    [Fact]
    public async Task CadastrarAsync_ReenvioIdenticoDoMesmoCadastro_CaiEmPapelDuplicadoSemCriarSegundoRegistro()
    {
        var repositorio = new FakeUsuarioRepository();
        var servico = new CadastroUsuarioService(repositorio, Clock);
        var payload = ("Maria Silva", "maria@exemplo.com");
        await servico.CadastrarAsync(PapelUsuario.Professor, payload.Item1, payload.Item2, CancellationToken.None);

        var acao = () => servico.CadastrarAsync(PapelUsuario.Professor, payload.Item1, payload.Item2, CancellationToken.None);

        await acao.Should().ThrowAsync<PapelJaAtribuidoException>();
        repositorio.Usuarios.Should().ContainSingle();
    }

    [Fact]
    public async Task CadastrarAsync_ContatoInvalido_RejeitaSemCriarUsuario()
    {
        var repositorio = new FakeUsuarioRepository();
        var servico = new CadastroUsuarioService(repositorio, Clock);

        var acao = () => servico.CadastrarAsync(PapelUsuario.Professor, "Maria Silva", "contato-invalido", CancellationToken.None);

        await acao.Should().ThrowAsync<ContatoInvalidoException>();
        repositorio.Usuarios.Should().BeEmpty();
    }

    [Fact]
    public async Task CadastrarAsync_NomeVazio_RejeitaSemCriarUsuario()
    {
        var repositorio = new FakeUsuarioRepository();
        var servico = new CadastroUsuarioService(repositorio, Clock);

        var acao = () => servico.CadastrarAsync(PapelUsuario.Professor, "   ", "maria@exemplo.com", CancellationToken.None);

        await acao.Should().ThrowAsync<NomeInvalidoException>();
        repositorio.Usuarios.Should().BeEmpty();
    }

    // Cenários específicos da issue #61 (cadastro de Aluno) — os três
    // Critérios de aceite do card, espelhando os testes de Professor acima
    // mas com o papel invertido, para confirmar que o serviço genérico se
    // comporta simetricamente para os dois papéis.

    [Fact]
    public async Task CadastrarAsync_ContatoInexistente_CriaUsuarioComPapelAlunoSemVinculoComProfessor()
    {
        var repositorio = new FakeUsuarioRepository();
        var servico = new CadastroUsuarioService(repositorio, Clock);

        var resultado = await servico.CadastrarAsync(PapelUsuario.Aluno, "João Souza", "joao@exemplo.com", CancellationToken.None);

        resultado.Usuario.Papeis.Should().ContainSingle(p => p.Papel == PapelUsuario.Aluno);
        resultado.UsuarioReaproveitado.Should().BeFalse();
    }

    [Fact]
    public async Task CadastrarAsync_UsuarioJaExisteComoProfessor_AdicionaPapelAlunoSemDuplicarIdentidade()
    {
        var repositorio = new FakeUsuarioRepository();
        var professorExistente = Usuario.Cadastrar("João Souza", "joao@exemplo.com", PapelUsuario.Professor, Clock);
        await repositorio.AdicionarAsync(professorExistente, CancellationToken.None);
        var servico = new CadastroUsuarioService(repositorio, Clock);

        var resultado = await servico.CadastrarAsync(PapelUsuario.Aluno, "João Souza", "joao@exemplo.com", CancellationToken.None);

        resultado.Usuario.Id.Should().Be(professorExistente.Id);
        resultado.Usuario.Papeis.Should().HaveCount(2);
        resultado.UsuarioReaproveitado.Should().BeTrue();
        repositorio.Usuarios.Should().ContainSingle();
    }

    [Fact]
    public async Task CadastrarAsync_ContatoInvalidoParaAluno_RejeitaSemCriarUsuario()
    {
        var repositorio = new FakeUsuarioRepository();
        var servico = new CadastroUsuarioService(repositorio, Clock);

        var acao = () => servico.CadastrarAsync(PapelUsuario.Aluno, "João Souza", "contato-invalido", CancellationToken.None);

        await acao.Should().ThrowAsync<ContatoInvalidoException>();
        repositorio.Usuarios.Should().BeEmpty();
    }
}
