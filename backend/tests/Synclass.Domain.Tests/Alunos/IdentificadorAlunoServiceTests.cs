using FluentAssertions;
using Synclass.Domain.Alunos;
using Synclass.Domain.Tests.Fakes;

namespace Synclass.Domain.Tests.Alunos;

/// <summary>
/// Cobre <see cref="IdentificadorAlunoService.GerarUnicoAsync"/> (issue #70):
/// geração sem colisão, retry em colisão (mesmo desenho de
/// <see cref="Synclass.Domain.Convites.ConviteService.GerarCodigoUnicoAsync"/>)
/// e teto de tentativas contra loop indefinido. Usa
/// <see cref="FakeGeradorDeIdentificadorAluno"/> e
/// <see cref="FakeIdentificadorAlunoUnicidadeChecker"/> no lugar de
/// aleatoriedade/banco reais.
/// </summary>
public sealed class IdentificadorAlunoServiceTests
{
    [Fact]
    public async Task GerarUnicoAsync_SemColisao_DevolveIdentificadorGeradoSemRetry()
    {
        var gerador = new FakeGeradorDeIdentificadorAluno("ALU-0001");
        var checador = new FakeIdentificadorAlunoUnicidadeChecker();
        var servico = new IdentificadorAlunoService(gerador, checador);

        var identificador = await servico.GerarUnicoAsync(CancellationToken.None);

        identificador.Should().Be("ALU-0001");
        gerador.VezesChamado.Should().Be(1);
        checador.VezesChamado.Should().Be(1);
    }

    [Fact]
    public async Task GerarUnicoAsync_UmaColisao_GeraNovamenteAteAcharIdentificadorLivre()
    {
        var gerador = new FakeGeradorDeIdentificadorAluno("ALU-0001", "ALU-0001", "ALU-0002");
        var checador = new FakeIdentificadorAlunoUnicidadeChecker("ALU-0001");
        var servico = new IdentificadorAlunoService(gerador, checador);

        var identificador = await servico.GerarUnicoAsync(CancellationToken.None);

        identificador.Should().Be("ALU-0002");
        gerador.VezesChamado.Should().Be(3);
        checador.VezesChamado.Should().Be(3);
    }

    /// <summary>
    /// Teto de tentativas (mesmo valor de referência de
    /// <see cref="Synclass.Domain.Convites.ConviteService"/>, 20 tentativas —
    /// ver docs/specs/70-identificador-aluno/implementation.md): sem ele, uma
    /// colisão persistente prenderia a requisição num loop indefinido. O
    /// gerador é consultado exatamente 20 vezes (uma por tentativa) e então a
    /// exceção é lançada.
    /// </summary>
    [Fact]
    public async Task GerarUnicoAsync_ColisoesRepetidasAteOTeto_LancaLimiteDeTentativasExcedido()
    {
        var identificadoresRepetidos = Enumerable.Repeat("ALU-0001", 20).ToArray();
        var gerador = new FakeGeradorDeIdentificadorAluno(identificadoresRepetidos);
        var checador = new FakeIdentificadorAlunoUnicidadeChecker("ALU-0001");
        var servico = new IdentificadorAlunoService(gerador, checador);

        var acao = () => servico.GerarUnicoAsync(CancellationToken.None);

        await acao.Should().ThrowAsync<LimiteDeTentativasDeIdentificadorAlunoExcedidoException>();
        gerador.VezesChamado.Should().Be(20);
    }
}
