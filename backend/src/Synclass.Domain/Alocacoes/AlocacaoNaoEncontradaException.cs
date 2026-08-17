namespace Synclass.Domain.Alocacoes;

/// <summary>
/// Lançada por <see cref="AlocacaoHorarioService.DesalocarAsync"/> quando
/// não existe alocação deste Aluno neste horário — não é uma rejeição de
/// negócio, mapeada para 404 pelo controller (mesmo papel de
/// <c>HorarioNaoEncontradoException</c>), por isso não herda de
/// <see cref="AlocacaoRejeitadaException"/>.
/// </summary>
public sealed class AlocacaoNaoEncontradaException : Exception
{
    public AlocacaoNaoEncontradaException(Guid horarioId, Guid matriculaId)
        : base($"Não existe alocação da matrícula {matriculaId} no horário {horarioId}.")
    {
    }
}
