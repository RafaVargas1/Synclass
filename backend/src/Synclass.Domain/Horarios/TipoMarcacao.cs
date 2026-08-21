namespace Synclass.Domain.Horarios;

/// <summary>
/// Política de marcação escolhida para este <see cref="Horario"/> específico
/// (issue #73), obrigatória na criação — mesmos nomes/semântica de <see
/// cref="Synclass.Domain.Configuracoes.ModeloAgendamento"/> (<c>Vago</c> →
/// <c>Livre</c>, <c>Fixo</c> → <c>Fixo</c>, <c>Hibrido</c> → <c>Hibrido</c>),
/// mas escopado ao Horário, não ao Professor. Trafega como inteiro no
/// contrato de Api (não como string), mesma decisão já tomada para
/// <c>DiaSemana</c> na issue #6.
/// </summary>
public enum TipoMarcacao
{
    /// <summary>Qualquer horário disponível pode ser marcado livremente por um Aluno.</summary>
    Livre = 0,

    /// <summary>Só o Professor atribui Alunos a este horário — o Aluno nunca marca sozinho.</summary>
    Fixo = 1,

    /// <summary>
    /// Atribuído primeiro pelo Professor; se sobrar, fica aberto para
    /// marcação livre, seguindo a regra de <see cref="TipoMarcacao.Livre"/>.
    /// </summary>
    Hibrido = 2,
}
