namespace Synclass.Domain.Convites;

/// <summary>
/// Lançada ao gerar um convite para um contato que já corresponde a um
/// Aluno pleno (matrícula já promovida) deste mesmo Professor — critério de
/// aceite 4 da issue #2. Impede reconvite desnecessário.
/// </summary>
public sealed class ContatoJaVinculadoException : ConviteRejeitadoException
{
    public ContatoJaVinculadoException()
        : base("Este Aluno já está vinculado a este Professor.")
    {
    }
}
