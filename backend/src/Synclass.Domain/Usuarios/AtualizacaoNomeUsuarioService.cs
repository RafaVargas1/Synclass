namespace Synclass.Domain.Usuarios;

/// <summary>
/// Orquestra a correção do próprio nome (issue #27): valida, busca o
/// <see cref="Usuario"/> pela identidade da sessão e persiste. Mesmo formato
/// de <see cref="CadastroProfessorService"/>.
/// </summary>
public sealed class AtualizacaoNomeUsuarioService
{
    private readonly IUsuarioRepository _usuarios;

    public AtualizacaoNomeUsuarioService(IUsuarioRepository usuarios)
    {
        _usuarios = usuarios;
    }

    public async Task<Usuario> AtualizarNomeAsync(Guid usuarioId, string nomeBruto, CancellationToken cancellationToken)
    {
        var nomeValidado = NomeUsuario.Validar(nomeBruto);
        var usuario = await _usuarios.BuscarPorIdAsync(usuarioId, cancellationToken)
            ?? throw new UsuarioNaoEncontradoException(usuarioId);

        usuario.AtualizarNome(nomeValidado);
        await _usuarios.SalvarAsync(cancellationToken);
        return usuario;
    }
}
