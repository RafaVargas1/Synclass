using System.Linq;
using Synclass.Domain.Common;
using Synclass.Domain.Horarios;

namespace Synclass.Domain.Cobrancas;

/// <summary>
/// Período `[Inicio, FimExclusivo)` usado por <see cref="ConsultaCobrancaService"/>
/// (issue #12) para calcular `quantidadeDeAulasNoPeriodo` — não é acoplado a
/// Professor nem a Aluno, reaproveitável tal e qual pela issue #13 (ver
/// implementation.md#reaproveitamento-pela-issue-13).
/// </summary>
public sealed class PeriodoConsulta
{
    private PeriodoConsulta(DateOnly inicio, DateOnly fimExclusivo)
    {
        Inicio = inicio;
        FimExclusivo = fimExclusivo;
    }

    public DateOnly Inicio { get; }

    public DateOnly FimExclusivo { get; }

    /// <summary>
    /// Rejeita com <see cref="PeriodoConsultaInvalidoException"/> quando
    /// <paramref name="inicio"/> não é estritamente anterior a
    /// <paramref name="fim"/> — não há "meio-padrão" para período invertido.
    /// </summary>
    public static PeriodoConsulta Criar(DateOnly inicio, DateOnly fim)
    {
        if (inicio >= fim)
        {
            throw new PeriodoConsultaInvalidoException(inicio, fim);
        }

        return new PeriodoConsulta(inicio, fim);
    }

    /// <summary>
    /// Primeiro dia do mês corrente (a partir de <paramref name="clock"/>)
    /// até o primeiro dia do mês seguinte, exclusivo — default usado pelo
    /// endpoint quando `inicio`/`fim` não são informados.
    /// </summary>
    public static PeriodoConsulta MesCorrente(IClock clock)
    {
        var hoje = DateOnly.FromDateTime(clock.UtcNow.UtcDateTime);
        var primeiroDiaDoMes = new DateOnly(hoje.Year, hoje.Month, 1);
        return new PeriodoConsulta(primeiroDiaDoMes, primeiroDiaDoMes.AddMonths(1));
    }

    /// <summary>
    /// Conta quantas vezes <paramref name="diaSemana"/> cai dentro deste
    /// período — usado para chegar em `quantidadeDeAulasNoPeriodo` a partir
    /// de um <see cref="Horario"/> recorrente (ver decisão de domínio em
    /// implementation.md). O(dias do período): aceitável para um período de
    /// um mês, ver implementation.md#edge-points.
    /// </summary>
    public int ContarOcorrencias(DiaSemana diaSemana)
    {
        return GerarDatas(diaSemana).Count();
    }

    /// <summary>
    /// Gera cada <see cref="DateOnly"/> do período cujo dia da semana bate
    /// com <paramref name="diaSemana"/> — usado por
    /// <see cref="ContarOcorrencias"/> e por
    /// <c>FrequenciaService.ListarHistoricoAsync</c> (issue #16) para
    /// converter um <see cref="Horario"/> recorrente nas datas concretas do
    /// período consultado.
    /// </summary>
    public IEnumerable<DateOnly> GerarDatas(DiaSemana diaSemana)
    {
        for (var data = Inicio; data < FimExclusivo; data = data.AddDays(1))
        {
            if ((int)data.DayOfWeek == (int)diaSemana)
            {
                yield return data;
            }
        }
    }
}
