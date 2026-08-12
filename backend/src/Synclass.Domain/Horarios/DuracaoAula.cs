namespace Synclass.Domain.Horarios;

/// <summary>
/// Valida a duração (em minutos) de um <see cref="Horario"/>. A Regra de
/// Negócio da issue #6 só impõe um mínimo (1 minuto) — não há máximo
/// imposto pelo sistema; um valor anormalmente longo é responsabilidade da
/// UX do frontend avisar, não deste validador.
/// </summary>
public static class DuracaoAula
{
    public const int MinimoMinutos = 1;

    public static void Validar(int duracaoMinutos)
    {
        if (duracaoMinutos < MinimoMinutos)
        {
            throw new DuracaoInvalidaException(duracaoMinutos);
        }
    }
}
