using FluentAssertions;
using Synclass.Domain.Tests.Fakes;
using Synclass.Domain.Usuarios;

namespace Synclass.Domain.Tests.Usuarios;

/// <summary>
/// Cobre a Regra de Negócio central da issue #1: criar ou reaproveitar
/// identidade de usuário por contato, sem nunca duplicar identidade nem
/// papel. Usa <see cref="FakeUsuarioRepository"/> no lugar de EF/banco real.
/// </summary>
public sealed class CadastroProfessorServiceTests
{
    private static readonly FixedClock Clock = new(new DateTimeOffset(2026, 8, 11, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public async Task CadastrarProfessorAsync_ContatoInexistente_CriaUsuarioComPapelProfessor()
    {
        var repositorio = new FakeUsuarioRepository();
        var servico = new CadastroProfessorService(repositorio, Clock);

        var usuario = await servico.CadastrarProfessorAsync("Maria Silva", "maria@exemplo.com", CancellationToken.None);

        usuario.Papeis.Should().ContainSingle(p => p.Papel == PapelUsuario.Professor);
        repositorio.Usuarios.Should().ContainSingle();
    }

    [Fact]
    public async Task CadastrarProfessorAsync_UsuarioJaExisteComoAluno_AdicionaPapelSemDuplicarIdentidade()
    {
        var repositorio = new FakeUsuarioRepository();
        var alunoExistente = Usuario.Cadastrar("Maria Silva", "maria@exemplo.com", PapelUsuario.Aluno, Clock);
        await repositorio.AdicionarAsync(alunoExistente, CancellationToken.None);
        var servico = new CadastroProfessorService(repositorio, Clock);

        // Capitalização e máscara diferentes do contato já cadastrado.
        var usuario = await servico.CadastrarProfessorAsync("Maria Silva", "  MARIA@EXEMPLO.COM  ", CancellationToken.None);

        usuario.Id.Should().Be(alunoExistente.Id);
        usuario.Papeis.Should().HaveCount(2);
        repositorio.Usuarios.Should().ContainSingle();
    }

    [Fact]
    public async Task CadastrarProfessorAsync_UsuarioJaEhProfessor_RejeitaComPapelJaAtribuidoException()
    {
        var repositorio = new FakeUsuarioRepository();
        var servico = new CadastroProfessorService(repositorio, Clock);
        await servico.CadastrarProfessorAsync("Maria Silva", "maria@exemplo.com", CancellationToken.None);

        var acao = () => servico.CadastrarProfessorAsync("Maria Silva", "maria@exemplo.com", CancellationToken.None);

        await acao.Should().ThrowAsync<PapelJaAtribuidoException>();
        repositorio.Usuarios.Should().ContainSingle();
    }

    [Fact]
    public async Task CadastrarProfessorAsync_ReenvioIdenticoDoMesmoCadastro_CaiEmPapelDuplicadoSemCriarSegundoRegistro()
    {
        var repositorio = new FakeUsuarioRepository();
        var servico = new CadastroProfessorService(repositorio, Clock);
        var payload = ("Maria Silva", "maria@exemplo.com");
        await servico.CadastrarProfessorAsync(payload.Item1, payload.Item2, CancellationToken.None);

        var acao = () => servico.CadastrarProfessorAsync(payload.Item1, payload.Item2, CancellationToken.None);

        await acao.Should().ThrowAsync<PapelJaAtribuidoException>();
        repositorio.Usuarios.Should().ContainSingle();
    }

    [Fact]
    public async Task CadastrarProfessorAsync_ContatoInvalido_RejeitaSemCriarUsuario()
    {
        var repositorio = new FakeUsuarioRepository();
        var servico = new CadastroProfessorService(repositorio, Clock);

        var acao = () => servico.CadastrarProfessorAsync("Maria Silva", "contato-invalido", CancellationToken.None);

        await acao.Should().ThrowAsync<ContatoInvalidoException>();
        repositorio.Usuarios.Should().BeEmpty();
    }

    [Fact]
    public async Task CadastrarProfessorAsync_NomeVazio_RejeitaSemCriarUsuario()
    {
        var repositorio = new FakeUsuarioRepository();
        var servico = new CadastroProfessorService(repositorio, Clock);

        var acao = () => servico.CadastrarProfessorAsync("   ", "maria@exemplo.com", CancellationToken.None);

        await acao.Should().ThrowAsync<NomeInvalidoException>();
        repositorio.Usuarios.Should().BeEmpty();
    }
}
