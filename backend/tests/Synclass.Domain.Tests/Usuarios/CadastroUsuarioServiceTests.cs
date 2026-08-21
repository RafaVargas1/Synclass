using FluentAssertions;
using Synclass.Domain.Alunos;
using Synclass.Domain.Tests.Fakes;
using Synclass.Domain.Usuarios;

namespace Synclass.Domain.Tests.Usuarios;

/// <summary>
/// Cobre a Regra de Negócio central do cadastro de usuário, comum ao
/// Professor (issue #1) e ao Aluno (issue #61): criar ou reaproveitar
/// identidade de usuário por contato, sem nunca duplicar identidade nem
/// papel. Usa <see cref="FakeUsuarioRepository"/> e fakes de
/// <see cref="IdentificadorAlunoService"/> no lugar de EF/banco e
/// aleatoriedade reais.
/// </summary>
public sealed class CadastroUsuarioServiceTests
{
    private static readonly FixedClock Clock = new(new DateTimeOffset(2026, 8, 11, 12, 0, 0, TimeSpan.Zero));

    /// <summary>
    /// Monta um <see cref="CadastroUsuarioService"/> injetando um
    /// <see cref="IdentificadorAlunoService"/> real (com fakes de gerador e
    /// checador) e devolve junto com os fakes — os testes da issue #70 usam
    /// <see cref="IdentificadorAlunoFake.VezesGerado"/> para provar que o
    /// gerador foi (ou não) consultado conforme o papel.
    /// </summary>
    private static (CadastroUsuarioService Servico, IdentificadorAlunoFake IdentificadorAluno) CriarServico(
        FakeUsuarioRepository repositorio)
    {
        var identificadorAluno = new IdentificadorAlunoFake();
        var servico = new CadastroUsuarioService(repositorio, Clock, identificadorAluno.Servico);
        return (servico, identificadorAluno);
    }

    [Fact]
    public async Task CadastrarAsync_ContatoInexistente_CriaUsuarioComPapelProfessor()
    {
        var repositorio = new FakeUsuarioRepository();
        var (servico, _) = CriarServico(repositorio);

        var resultado = await servico.CadastrarAsync(PapelUsuario.Professor, "Maria Silva", "maria@exemplo.com", CancellationToken.None);

        resultado.Usuario.Papeis.Should().ContainSingle(p => p.Papel == PapelUsuario.Professor);
        repositorio.Usuarios.Should().ContainSingle();
    }

    [Fact]
    public async Task CadastrarAsync_ContatoInexistente_UsuarioReaproveitadoEhFalso()
    {
        var repositorio = new FakeUsuarioRepository();
        var (servico, _) = CriarServico(repositorio);

        var resultado = await servico.CadastrarAsync(PapelUsuario.Professor, "Maria Silva", "maria@exemplo.com", CancellationToken.None);

        resultado.UsuarioReaproveitado.Should().BeFalse();
    }

    [Fact]
    public async Task CadastrarAsync_UsuarioJaExisteComoAluno_AdicionaPapelProfessorSemDuplicarIdentidade()
    {
        var repositorio = new FakeUsuarioRepository();
        var alunoExistente = Usuario.Cadastrar("Maria Silva", "maria@exemplo.com", PapelUsuario.Aluno, null, Clock);
        await repositorio.AdicionarAsync(alunoExistente, CancellationToken.None);
        var (servico, _) = CriarServico(repositorio);

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
        var alunoExistente = Usuario.Cadastrar("Maria Silva", "maria@exemplo.com", PapelUsuario.Aluno, null, Clock);
        await repositorio.AdicionarAsync(alunoExistente, CancellationToken.None);
        var (servico, _) = CriarServico(repositorio);

        var resultado = await servico.CadastrarAsync(PapelUsuario.Professor, "Maria Silva", "maria@exemplo.com", CancellationToken.None);

        resultado.UsuarioReaproveitado.Should().BeTrue();
    }

    [Fact]
    public async Task CadastrarAsync_UsuarioJaEhProfessor_RejeitaComPapelJaAtribuidoException()
    {
        var repositorio = new FakeUsuarioRepository();
        var (servico, _) = CriarServico(repositorio);
        await servico.CadastrarAsync(PapelUsuario.Professor, "Maria Silva", "maria@exemplo.com", CancellationToken.None);

        var acao = () => servico.CadastrarAsync(PapelUsuario.Professor, "Maria Silva", "maria@exemplo.com", CancellationToken.None);

        await acao.Should().ThrowAsync<PapelJaAtribuidoException>();
        repositorio.Usuarios.Should().ContainSingle();
    }

    [Fact]
    public async Task CadastrarAsync_ReenvioIdenticoDoMesmoCadastro_CaiEmPapelDuplicadoSemCriarSegundoRegistro()
    {
        var repositorio = new FakeUsuarioRepository();
        var (servico, _) = CriarServico(repositorio);
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
        var (servico, _) = CriarServico(repositorio);

        var acao = () => servico.CadastrarAsync(PapelUsuario.Professor, "Maria Silva", "contato-invalido", CancellationToken.None);

        await acao.Should().ThrowAsync<ContatoInvalidoException>();
        repositorio.Usuarios.Should().BeEmpty();
    }

    [Fact]
    public async Task CadastrarAsync_NomeVazio_RejeitaSemCriarUsuario()
    {
        var repositorio = new FakeUsuarioRepository();
        var (servico, _) = CriarServico(repositorio);

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
        var (servico, _) = CriarServico(repositorio);

        var resultado = await servico.CadastrarAsync(PapelUsuario.Aluno, "João Souza", "joao@exemplo.com", CancellationToken.None);

        resultado.Usuario.Papeis.Should().ContainSingle(p => p.Papel == PapelUsuario.Aluno);
        resultado.UsuarioReaproveitado.Should().BeFalse();
    }

    [Fact]
    public async Task CadastrarAsync_UsuarioJaExisteComoProfessor_AdicionaPapelAlunoSemDuplicarIdentidade()
    {
        var repositorio = new FakeUsuarioRepository();
        var professorExistente = Usuario.Cadastrar("João Souza", "joao@exemplo.com", PapelUsuario.Professor, null, Clock);
        await repositorio.AdicionarAsync(professorExistente, CancellationToken.None);
        var (servico, _) = CriarServico(repositorio);

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
        var (servico, _) = CriarServico(repositorio);

        var acao = () => servico.CadastrarAsync(PapelUsuario.Aluno, "João Souza", "contato-invalido", CancellationToken.None);

        await acao.Should().ThrowAsync<ContatoInvalidoException>();
        repositorio.Usuarios.Should().BeEmpty();
    }

    // Cenários específicos da issue #70: o serviço gera um
    // IdentificadorAluno único via IdentificadorAlunoService apenas quando o
    // papel Aluno é de fato anexado, e nunca para Professor.

    [Fact]
    public async Task CadastrarAsync_PapelAluno_GeraEPropagaIdentificadorAluno()
    {
        var repositorio = new FakeUsuarioRepository();
        var (servico, identificadorAluno) = CriarServico(repositorio);

        var resultado = await servico.CadastrarAsync(PapelUsuario.Aluno, "João Souza", "joao@exemplo.com", CancellationToken.None);

        identificadorAluno.VezesGerado.Should().Be(1);
        resultado.Usuario.IdentificadorAluno.Should().NotBeNull();
    }

    [Fact]
    public async Task CadastrarAsync_PapelProfessor_NaoGeraIdentificadorAluno()
    {
        var repositorio = new FakeUsuarioRepository();
        var (servico, identificadorAluno) = CriarServico(repositorio);

        var resultado = await servico.CadastrarAsync(PapelUsuario.Professor, "Maria Silva", "maria@exemplo.com", CancellationToken.None);

        identificadorAluno.VezesGerado.Should().Be(0);
        resultado.Usuario.IdentificadorAluno.Should().BeNull();
    }
}

/// <summary>
/// Compõe um <see cref="IdentificadorAlunoService"/> real a partir de
/// <see cref="FakeGeradorDeIdentificadorAluno"/> (sequência previsível) e
/// <see cref="FakeIdentificadorAlunoUnicidadeChecker"/> (nada em uso), expondo
/// <see cref="VezesGerado"/> para que os testes do
/// <see cref="CadastroUsuarioServiceTests"/> provem que o gerador foi (ou
/// não) consultado conforme o papel — mesmo padrão de mockar I/O externo com
/// fakes nomeados (docs/spec/testing-standards.md).
/// </summary>
public sealed class IdentificadorAlunoFake
{
    public IdentificadorAlunoFake()
    {
        var gerador = new FakeGeradorDeIdentificadorAluno();
        var checador = new FakeIdentificadorAlunoUnicidadeChecker();
        Servico = new IdentificadorAlunoService(gerador, checador);
        _gerador = gerador;
    }

    private readonly FakeGeradorDeIdentificadorAluno _gerador;

    public IdentificadorAlunoService Servico { get; }

    public int VezesGerado => _gerador.VezesChamado;
}
