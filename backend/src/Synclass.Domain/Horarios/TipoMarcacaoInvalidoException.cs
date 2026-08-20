namespace Synclass.Domain.Horarios;

/// <summary>
/// Lançada quando a política de marcação informada está fora do intervalo
/// válido de <see cref="TipoMarcacao"/> (0 a 2 — ver Critérios técnicos da
/// issue #73). Mesmo padrão de <see cref="DiaSemanaInvalidoException"/>.
/// </summary>
public sealed class TipoMarcacaoInvalidoException : HorarioRejeitadoException
{
    public TipoMarcacaoInvalidoException(int tipoMarcacao)
        : base($"Tipo de marcação inválido: {tipoMarcacao}. Esperado um valor entre 0 e 2.")
    {
    }
}
