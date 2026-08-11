namespace Synclass.Domain.Matriculas;

/// <summary>
/// Base para toda rejeição de cadastro/promoção de Aluno provisório (nome
/// inválido, identificador inválido/duplicado, ou promoção inválida).
/// Permite à Api tratar qualquer rejeição de forma uniforme (HTTP 400 + log
/// de <c>CadastroAlunoProvisorioRejeitado</c>) sem conhecer cada subtipo
/// individualmente — mesmo papel de
/// <c>Synclass.Domain.Usuarios.CadastroProfessorRejeitadoException</c> na
/// issue #1.
/// </summary>
public abstract class MatriculaRejeitadaException : Exception
{
    protected MatriculaRejeitadaException(string message)
        : base(message)
    {
    }

    protected MatriculaRejeitadaException(string message, Exception causaRaiz)
        : base(message, causaRaiz)
    {
    }
}
