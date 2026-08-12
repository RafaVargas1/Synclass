namespace Synclass.Domain.Matriculas;

/// <summary>
/// Lançada ao tentar promover uma <see cref="Matricula"/> que já tem
/// <see cref="Matricula.AlunoUsuarioId"/> definido — a promoção nunca
/// sobrescreve um vínculo já existente (edge point da issue #3).
/// </summary>
public sealed class MatriculaJaPromovidaException : MatriculaRejeitadaException
{
    public MatriculaJaPromovidaException(Guid matriculaId)
        : base($"A matrícula {matriculaId} já foi promovida para um usuário pleno.")
    {
    }
}
