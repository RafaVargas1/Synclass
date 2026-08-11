namespace Synclass.Domain.Usuarios;

/// <summary>
/// Lançada quando um usuário tenta receber um papel que já possui — reenvio
/// do mesmo cadastro (ex: duplo clique/retry) cai aqui, e é rejeitado em vez
/// de silenciosamente ignorado, para não mascarar um possível bug de reenvio
/// no cliente (ver Regra de Negócio da issue #1).
/// </summary>
public sealed class PapelJaAtribuidoException : CadastroProfessorRejeitadoException
{
    public PapelJaAtribuidoException(PapelUsuario papel)
        : base($"O contato já está cadastrado como {papel}.")
    {
        Papel = papel;
    }

    public PapelUsuario Papel { get; }
}
