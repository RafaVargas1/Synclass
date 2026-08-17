namespace Synclass.Domain.Alocacoes;

/// <summary>
/// Lançada quando o mesmo Aluno já está alocado neste horário — checagem
/// prévia na aplicação; o índice único (HorarioId, MatriculaId) é o guard
/// rail final contra a corrida concorrente, mesmo padrão de
/// <c>MatriculaConcorrenteException</c> (ver
/// docs/specs/8-aluno-horario/implementation.md#edge-points).
/// </summary>
public sealed class AlocacaoJaExisteException : AlocacaoRejeitadaException
{
    public AlocacaoJaExisteException(Guid horarioId, Guid matriculaId)
        : base($"A matrícula {matriculaId} já está alocada no horário {horarioId}.")
    {
    }
}
