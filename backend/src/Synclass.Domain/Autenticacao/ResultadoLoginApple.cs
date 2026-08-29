namespace Synclass.Domain.Autenticacao;

/// <summary>
/// Resultado do login Apple (issue #212). Quando o e-mail da Apple
/// corresponde a um usuário existente, <see cref="Login"/> vem preenchido
/// com a sessão; caso contrário, <c>CadastroPendente = true</c> e
/// <see cref="EmailNormalizado"/> carrega o e-mail para o fluxo de cadastro
/// (a Apple não emite conta nova — mesma RN do Google, issue #65). Espelha
/// <see cref="ResultadoLoginGoogle"/> sem abstração compartilhada (ver
/// implementation.md#decisão-sem-serviço-genérico-compartilhado).
/// </summary>
public sealed record ResultadoLoginApple(
    ResultadoLogin? Login,
    bool CadastroPendente,
    string EmailNormalizado);
