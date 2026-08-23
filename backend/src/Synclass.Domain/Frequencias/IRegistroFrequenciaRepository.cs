using Synclass.Domain.Cobrancas;

namespace Synclass.Domain.Frequencias;

/// <summary>
/// Abstrai a persistência de <see cref="RegistroFrequencia"/>. Implementado
/// em Synclass.Infrastructure (EF Core), mesmo padrão de
/// <c>ICancelamentoAulaRepository</c>.
/// </summary>
public interface IRegistroFrequenciaRepository
{
    /// <summary>
    /// Busca a linha de um Aluno específico para uma aula, se existir —
    /// usada pelo upsert de <c>FrequenciaService.RegistrarAsync</c> (cria a
    /// linha na primeira vez, atualiza nas seguintes).
    /// </summary>
    Task<RegistroFrequencia?> BuscarAsync(Guid aulaId, Guid matriculaId, CancellationToken cancellationToken);

    /// <summary>
    /// Conta quantas <see cref="Aula"/>s desta matrícula, dentro do
    /// <paramref name="periodo"/>, têm <see cref="RegistroFrequencia.StatusProfessor"/>
    /// igual a <see cref="StatusFrequencia.Presente"/> — usado por
    /// <c>Synclass.Domain.Cobrancas.ConsultaCobrancaService</c> quando a
    /// regra de cobrança conta por presença confirmada, não por agendamento
    /// (issue #186). Aula sem <see cref="RegistroFrequencia"/> lançado ainda
    /// não conta (nem presença, nem ausência).
    /// </summary>
    Task<int> ContarPresencasNoPeriodoAsync(Guid matriculaId, PeriodoConsulta periodo, CancellationToken cancellationToken);

    Task AdicionarAsync(RegistroFrequencia registro, CancellationToken cancellationToken);

    Task SalvarAsync(CancellationToken cancellationToken);
}
