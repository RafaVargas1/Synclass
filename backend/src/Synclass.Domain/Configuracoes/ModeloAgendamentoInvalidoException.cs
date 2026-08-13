namespace Synclass.Domain.Configuracoes;

/// <summary>
/// Lançada quando o modelo de agendamento informado está fora do intervalo
/// válido de <see cref="ModeloAgendamento"/> (0 a 2). Mesmo padrão de
/// <c>DiaSemanaInvalidoException</c> (issue #6) — sem essa validação, um
/// inteiro fora do range (ex: 99) era persistido silenciosamente e só
/// falhava mais tarde, sem contexto, quando <see
/// cref="ConfiguracaoProfessor.PermiteMarcacaoLivre"/> fosse chamada pelas
/// issues #8/#9 (achado do dev-review no PR #26).
/// </summary>
public sealed class ModeloAgendamentoInvalidoException : Exception
{
    public ModeloAgendamentoInvalidoException(int modeloAgendamento)
        : base($"Modelo de agendamento inválido: {modeloAgendamento}. Esperado um valor entre 0 e 2.")
    {
    }
}
