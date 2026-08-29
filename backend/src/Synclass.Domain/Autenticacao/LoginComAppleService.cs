using Synclass.Domain.Usuarios;

namespace Synclass.Domain.Autenticacao;

/// <summary>
/// Orquestra o login via idToken da Apple (issue #212) — fluxo paralelo ao
/// <see cref="LoginService"/> (OTP) e ao <see cref="LoginComGoogleService"/>.
/// Estruturalmente parecido com o Google mas fechado na própria mecânica de
/// validação (JWT + JWKS manual, sem SDK pronto) e sem abstração compartilhada
/// entre os dois provedores — só dois existem hoje e cada um tem sua própria
/// validação de token (YAGNI, ver implementation.md#decisão-sem-serviço-genérico-compartilhado).
///
/// Mesma regra de identidade por contato: a Apple só autentica quem já
/// existe, nunca cria conta implicitamente (quando o e-mail não corresponde
/// a nenhum usuário, devolve <see cref="ResultadoLoginApple.CadastroPendente"/>
/// com o e-mail para o fluxo de cadastro seguir).
/// </summary>
public sealed class LoginComAppleService
{
    private readonly IUsuarioRepository _usuarios;
    private readonly IValidadorDeIdTokenApple _validador;
    private readonly IGeradorDeTokenSessao _tokenSessao;

    public LoginComAppleService(
        IUsuarioRepository usuarios,
        IValidadorDeIdTokenApple validador,
        IGeradorDeTokenSessao tokenSessao)
    {
        _usuarios = usuarios;
        _validador = validador;
        _tokenSessao = tokenSessao;
    }

    public async Task<ResultadoLoginApple> AutenticarAsync(string idToken, CancellationToken cancellationToken)
    {
        var informacoes = await _validador.ValidarAsync(idToken, cancellationToken);
        if (informacoes is null)
        {
            throw new TokenAppleInvalidoException();
        }

        var emailNormalizado = Contato.Normalizar(informacoes.Email);
        if (!informacoes.EmailVerificado)
        {
            throw new EmailAppleNaoVerificadoException(emailNormalizado);
        }

        var usuario = await _usuarios.BuscarPorContatoAsync(emailNormalizado, cancellationToken);
        if (usuario is null)
        {
            return new ResultadoLoginApple(Login: null, CadastroPendente: true, EmailNormalizado: emailNormalizado);
        }

        return new ResultadoLoginApple(
            Login: new ResultadoLogin(usuario, _tokenSessao.Gerar(usuario)),
            CadastroPendente: false,
            EmailNormalizado: emailNormalizado);
    }
}
