namespace Synclass.Domain.Autenticacao;

/// <summary>
/// Lançada quando o contato informado não corresponde a nenhuma identidade
/// plena cadastrada — usuário inexistente, ou existente sem nenhum papel
/// (caso hoje inalcançável, mas previsto para o desenho futuro de Aluno
/// provisório, issue #3). Mensagem deliberadamente genérica: não revela qual
/// dos dois casos ocorreu, para não permitir enumerar contas existentes (ver
/// Critérios de aceite da issue #18).
/// </summary>
public sealed class ContatoSemIdentidadePlenaException : LoginRejeitadoException
{
    public ContatoSemIdentidadePlenaException()
        : base("Nenhuma conta encontrada para esse contato.")
    {
    }
}
