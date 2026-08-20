namespace Synclass.Domain.Alunos;

/// <summary>
/// Gera um identificador de Aluno único e human-readable no formato
/// <c>ALU-XXXX</c> (4 caracteres), usando um alfabeto sem caracteres
/// ambíguos (<c>0</c>, <c>O</c>, <c>1</c>, <c>I</c>, <c>L</c> excluídos)
/// — ver docs/specs/70-identificador-aluno/implementation.md. Interface
/// fina sobre a fonte de aleatoriedade real, mesmo racional de
/// <see cref="Synclass.Domain.Convites.IGeradorDeTokenConvite"/>.
/// </summary>
public interface IGeradorDeIdentificadorAluno
{
    string Gerar();
}
