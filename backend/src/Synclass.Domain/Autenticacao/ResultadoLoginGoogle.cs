namespace Synclass.Domain.Autenticacao;

/// <summary>
/// Resultado do login Google (issue #65). Quando o e-mail do Google
/// corresponde a um usuário existente, <see cref="Login"/> vem preenchido
/// com a sessão; caso contrário, <c>CadastroPendente = true</c> e
/// <see cref="EmailNormalizado"/> carrega o e-mail para o fluxo de cadastro
/// (o login Google nunca cria conta implicitamente).
/// </summary>
public sealed record ResultadoLoginGoogle(
    ResultadoLogin? Login,
    bool CadastroPendente,
    string EmailNormalizado);
