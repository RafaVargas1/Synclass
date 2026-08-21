using Synclass.Domain.Usuarios;

namespace Synclass.Domain.Autenticacao;

/// <summary>
/// Orquestra o login via idToken do Google (issue #65) — fluxo paralelo ao
/// <see cref="LoginService"/> (OTP), não o substitui nem o modifica. Mesma
/// identidade única por contato: o login Google só autentica quem já existe,
/// nunca cria conta implicitamente (quando o e-mail não corresponde a nenhum
/// usuário, devolve <see cref="ResultadoLoginGoogle.CadastroPendente"/> com
/// o e-mail para o fluxo de cadastro seguir).
/// </summary>
public sealed class LoginComGoogleService
{
    private readonly IUsuarioRepository _usuarios;
    private readonly IValidadorDeIdTokenGoogle _validador;
    private readonly IGeradorDeTokenSessao _tokenSessao;

    public LoginComGoogleService(
        IUsuarioRepository usuarios,
        IValidadorDeIdTokenGoogle validador,
        IGeradorDeTokenSessao tokenSessao)
    {
        _usuarios = usuarios;
        _validador = validador;
        _tokenSessao = tokenSessao;
    }

    public async Task<ResultadoLoginGoogle> AutenticarAsync(string idToken, CancellationToken cancellationToken)
    {
        var informacoes = await _validador.ValidarAsync(idToken, cancellationToken);
        if (informacoes is null)
        {
            throw new TokenGoogleInvalidoException();
        }

        var emailNormalizado = Contato.Normalizar(informacoes.Email);
        if (!informacoes.EmailVerificado)
        {
            throw new EmailGoogleNaoVerificadoException(emailNormalizado);
        }

        var usuario = await _usuarios.BuscarPorContatoAsync(emailNormalizado, cancellationToken);
        if (usuario is null)
        {
            return new ResultadoLoginGoogle(Login: null, CadastroPendente: true, EmailNormalizado: emailNormalizado);
        }

        return new ResultadoLoginGoogle(
            Login: new ResultadoLogin(usuario, _tokenSessao.Gerar(usuario)),
            CadastroPendente: false,
            EmailNormalizado: emailNormalizado);
    }
}
