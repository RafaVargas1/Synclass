namespace Synclass.Domain.Matriculas;

/// <summary>
/// Lançada quando o nome do Aluno provisório é vazio, só espaços, ou excede
/// o tamanho máximo aceito. Traduz
/// <see cref="Synclass.Domain.Usuarios.NomeInvalidoException"/> (reusada de
/// <c>NomeUsuario.Validar</c> — mesma regra da issue #1, "igual à issue #1"
/// no card) para o tipo base deste módulo, para a Api só precisar conhecer
/// <see cref="MatriculaRejeitadaException"/>.
/// </summary>
public sealed class NomeProvisorioInvalidoException : MatriculaRejeitadaException
{
    public NomeProvisorioInvalidoException(string mensagemOriginal)
        : base(mensagemOriginal)
    {
    }
}
