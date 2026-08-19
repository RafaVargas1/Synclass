namespace Synclass.Domain.Usuarios;

/// <summary>
/// Lançada quando um <c>usuarioId</c> (normalmente lido do token de sessão)
/// não corresponde a nenhum <see cref="Usuario"/> persistido — defensivo:
/// não deveria acontecer com um JWT válido emitido pelo próprio sistema
/// (issue #23), mas protege <see cref="AtualizacaoNomeUsuarioService"/> de
/// operar sobre uma identidade inexistente.
/// </summary>
public sealed class UsuarioNaoEncontradoException : Exception
{
    public UsuarioNaoEncontradoException(Guid usuarioId)
        : base($"Usuario não encontrado: {usuarioId}.")
    {
    }
}
