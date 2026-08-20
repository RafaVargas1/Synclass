using FluentAssertions;
using Synclass.Infrastructure.Alunos;

namespace Synclass.Domain.Tests.Alunos;

/// <summary>
/// Cobre a forma e o alfabeto do identificador de Aluno (issue #70):
/// <c>ALU-</c> + 4 caracteres, todos do alfabeto sem caracteres ambíguos
/// (<c>0</c>, <c>O</c>, <c>1</c>, <c>I</c>, <c>L</c> fora) — pensado para
/// leitura/digitação humana (ver docs/specs/70-identificador-aluno/implementation.md).
/// O alfabeto permitido é a whitelist
/// <c>23456789ABCDEFGHJKMNPQRSTUVWXYZ</c> (31 símbolos); garantir que todo
/// caractere do sufixo pertence a ela prova, de forma determinística, a
/// exclusão dos cinco ambíguos.
/// </summary>
public sealed class GeradorDeIdentificadorAlunoTests
{
    private const string AlfabetoPermitido = "23456789ABCDEFGHJKMNPQRSTUVWXYZ";

    [Theory]
    [InlineData('0')]
    [InlineData('O')]
    [InlineData('1')]
    [InlineData('I')]
    [InlineData('L')]
    public void Gerar_CaracteresAmbiguos_NuncaAparecemNoSufixo(char ambiguo)
    {
        var gerador = new GeradorDeIdentificadorAluno();

        var identificador = gerador.Gerar();

        identificador[4..].Should().NotContain(ambiguo.ToString());
    }

    [Fact]
    public void Gerar_Formato_ProduzPrefixoAluESufixoDeQuatroCaracteresDoAlfabeto()
    {
        var gerador = new GeradorDeIdentificadorAluno();

        var identificador = gerador.Gerar();

        identificador.Should().StartWith("ALU-");
        var sufixo = identificador[4..];
        sufixo.Should().HaveLength(4);
        sufixo.ToCharArray().Should().OnlyContain(c => AlfabetoPermitido.Contains(c));
    }
}
