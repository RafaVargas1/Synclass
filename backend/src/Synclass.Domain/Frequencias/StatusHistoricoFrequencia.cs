namespace Synclass.Domain.Frequencias;

/// <summary>
/// Status de uma ocorrência exibido no histórico de frequência do Aluno
/// (issue #16). Distinto de <see cref="StatusFrequencia"/> (issue #14, só
/// <c>Presente</c>/<c>Ausente</c>) — o histórico precisa dos 4 estados
/// visíveis ao Aluno (nenhuma fonte registrou nada ainda, presença
/// confirmada pelo Professor, ausência confirmada pelo Professor, ou
/// cancelamento pelo próprio Aluno), então não reaproveita o enum
/// existente.
/// </summary>
public enum StatusHistoricoFrequencia
{
    NaoRegistrada = 0,
    Presente = 1,
    Ausente = 2,
    Cancelada = 3,
}
