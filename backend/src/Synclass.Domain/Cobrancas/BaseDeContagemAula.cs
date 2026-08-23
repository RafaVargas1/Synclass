namespace Synclass.Domain.Cobrancas;

/// <summary>
/// Como <see cref="ConsultaCobrancaService"/> conta aulas do período para
/// regras de cobrança por aula (<see cref="RegraFixoPorAula"/>/
/// <see cref="RegraValorPorAula"/>, issue #186) — <see cref="Agendamento"/>
/// (valor <c>0</c>, default) conta toda ocorrência semanal agendada, mesmo
/// comportamento de antes desta issue; <see cref="PresencaConfirmada"/>
/// conta só aulas com <see cref="Synclass.Domain.Frequencias.RegistroFrequencia.StatusProfessor"/>
/// igual a <c>Presente</c>. Default <c>Agendamento</c> para a migration não
/// mudar o valor cobrado de regras já configuradas.
/// </summary>
public enum BaseDeContagemAula
{
    Agendamento = 0,
    PresencaConfirmada = 1,
}

/// <summary>
/// Implementada só por <see cref="RegraFixoPorAula"/>/<see cref="RegraValorPorAula"/>
/// — <see cref="RegraFixoMensal"/> não tem <see cref="BaseDeContagemAula"/>
/// porque sua fórmula não depende de contagem de aula (RN da issue #11).
/// </summary>
public interface IRegraComBaseDeContagemAula
{
    BaseDeContagemAula BaseDeContagemAula { get; }
}
