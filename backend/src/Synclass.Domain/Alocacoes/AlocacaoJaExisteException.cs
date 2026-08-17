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

    /// <summary>
    /// Usado por <c>AlocacaoHorarioRepository.SalvarAsync</c> quando o
    /// índice único é violado a nível de banco — a checagem prévia da
    /// aplicação (<see cref="AlocacaoHorarioService"/>) já passou, então só
    /// a corrida concorrente explica a violação aqui.
    /// </summary>
    public AlocacaoJaExisteException(Exception causaRaiz)
        : base("Conflito ao salvar a alocação: este Aluno já está alocado neste horário. Tente novamente.", causaRaiz)
    {
    }
}
