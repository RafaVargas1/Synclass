using Synclass.Domain.Common;
using Synclass.Domain.Usuarios;

namespace Synclass.Domain.Autenticacao;

/// <summary>
/// Orquestra o login por código de uso único (issue #18): autentica a
/// identidade (não um papel específico — ver Regra de Negócio do card),
/// sem revelar se um contato rejeitado é um Aluno provisório ou nunca foi
/// cadastrado.
/// </summary>
public sealed class LoginService
{
    private readonly IUsuarioRepository _usuarios;
    private readonly ICodigoOtpRepository _codigosOtp;
    private readonly IGeradorDeCodigoOtp _geradorDeCodigo;
    private readonly INotificador _notificador;
    private readonly IGeradorDeTokenSessao _tokenSessao;
    private readonly IClock _clock;

    public LoginService(
        IUsuarioRepository usuarios,
        ICodigoOtpRepository codigosOtp,
        IGeradorDeCodigoOtp geradorDeCodigo,
        INotificador notificador,
        IGeradorDeTokenSessao tokenSessao,
        IClock clock)
    {
        _usuarios = usuarios;
        _codigosOtp = codigosOtp;
        _geradorDeCodigo = geradorDeCodigo;
        _notificador = notificador;
        _tokenSessao = tokenSessao;
        _clock = clock;
    }

    public async Task SolicitarCodigoAsync(string contatoBruto, CancellationToken cancellationToken)
    {
        var contatoNormalizado = Contato.Normalizar(contatoBruto);
        var usuario = await BuscarIdentidadePlenaAsync(contatoNormalizado, cancellationToken);

        await InvalidarCodigoAnteriorSeExistirAsync(usuario.Id, cancellationToken);

        var codigoGerado = _geradorDeCodigo.Gerar();
        await _codigosOtp.AdicionarAsync(CodigoOtp.Gerar(usuario.Id, codigoGerado, _clock), cancellationToken);
        await _codigosOtp.SalvarAsync(cancellationToken);
        await _notificador.EnviarCodigoOtpAsync(contatoNormalizado, codigoGerado, cancellationToken);
    }

    public async Task<ResultadoLogin> ConfirmarCodigoAsync(string contatoBruto, string codigoBruto, CancellationToken cancellationToken)
    {
        var contatoNormalizado = Contato.Normalizar(contatoBruto);
        var usuario = await BuscarIdentidadePlenaAsync(contatoNormalizado, cancellationToken);

        var codigo = await _codigosOtp.BuscarMaisRecenteNaoUsadoAsync(usuario.Id, cancellationToken);
        await ValidarCodigoOuRegistrarTentativaAsync(codigo, codigoBruto, cancellationToken);

        codigo!.Invalidar(_clock);
        await _codigosOtp.SalvarAsync(cancellationToken);

        return new ResultadoLogin(usuario, _tokenSessao.Gerar(usuario));
    }

    /// <summary>
    /// Valida o código candidato à confirmação. Cada tentativa incorreta
    /// conta para <see cref="CodigoOtp.MaxTentativasFalhas"/> (ver Fix 1 do
    /// dev-review do PR #25, issue #18) — sem isso, o OTP de 6 dígitos é
    /// brute-forçável em POST /auth/confirmacao.
    /// </summary>
    private async Task ValidarCodigoOuRegistrarTentativaAsync(CodigoOtp? codigo, string codigoBruto, CancellationToken cancellationToken)
    {
        if (codigo is null || codigo.Expirado(_clock))
        {
            throw new CodigoOtpInvalidoException();
        }

        if (codigo.Bloqueado)
        {
            throw new CodigoOtpBloqueadoException();
        }

        if (!codigo.Corresponde(codigoBruto))
        {
            codigo.RegistrarTentativaFalha();
            await _codigosOtp.SalvarAsync(cancellationToken);
            throw codigo.Bloqueado ? new CodigoOtpBloqueadoException() : new CodigoOtpInvalidoException();
        }
    }

    private async Task<Usuario> BuscarIdentidadePlenaAsync(string contatoNormalizado, CancellationToken cancellationToken)
    {
        var usuario = await _usuarios.BuscarPorContatoAsync(contatoNormalizado, cancellationToken);
        if (usuario is null || usuario.Papeis.Count == 0)
        {
            throw new ContatoSemIdentidadePlenaException();
        }

        return usuario;
    }

    private async Task InvalidarCodigoAnteriorSeExistirAsync(Guid usuarioId, CancellationToken cancellationToken)
    {
        var codigoAnterior = await _codigosOtp.BuscarMaisRecenteNaoUsadoAsync(usuarioId, cancellationToken);
        codigoAnterior?.Invalidar(_clock);
    }
}
