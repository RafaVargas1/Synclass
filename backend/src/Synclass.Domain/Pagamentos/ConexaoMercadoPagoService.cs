using System.Security.Cryptography;
using Synclass.Domain.Common;
using Synclass.Domain.Usuarios;

namespace Synclass.Domain.Pagamentos;

/// <summary>
/// Orquestra os use cases de integração do Professor com o Mercado Pago
/// (issue #203). Segue o padrão de <c>ConfiguracaoProfessorService</c>:
/// construtor com dependências injetadas, retorno antecipado para erros e
/// sem exceções para "Professor não conectado" em
/// <see cref="ObterCollectorIdAsync"/> (que retorna <see langword="null"/>,
/// contrato fechado com a Task #199).
/// </summary>
public class ConexaoMercadoPagoService
{
    private const int TamanhoStateBytes = 32;

    private readonly IConexaoMercadoPagoRepository _conexoes;
    private readonly IUsuarioRepository _usuarios;
    private readonly IClienteOAuthMercadoPago _clienteOAuth;
    private readonly IClock _clock;
    private readonly string _redirectUri;

    public ConexaoMercadoPagoService(
        IConexaoMercadoPagoRepository conexoes,
        IUsuarioRepository usuarios,
        IClienteOAuthMercadoPago clienteOAuth,
        IClock clock,
        string redirectUri)
    {
        _conexoes = conexoes;
        _usuarios = usuarios;
        _clienteOAuth = clienteOAuth;
        _clock = clock;
        _redirectUri = redirectUri;
    }

    /// <summary>
    /// Inicia o fluxo OAuth do Professor (issue #203) e retorna a URL de
    /// autorização para ele abrir no navegador. Idempotente por Professor:
    /// se já existe um registro (conectado ou fluxo abandonado), reaproveita
    /// sobrescrevendo <c>State</c>/<c>StateExpiraEm</c> — nunca cria um
    /// segundo registro nem exige desconectar antes (ver
    /// implementation.md#reconexão).
    /// </summary>
    public async Task<string> ConectarAsync(Guid professorId, CancellationToken ct)
    {
        if (!await _usuarios.ExisteAsync(professorId, ct))
        {
            throw new UsuarioNaoEncontradoException(professorId);
        }

        var state = GerarState();
        var conexao = await _conexoes.ObterPorProfessorAsync(professorId, ct);
        if (conexao is null)
        {
            conexao = ConexaoMercadoPago.IniciarFluxoDeAutorizacao(professorId, state, _clock);
            await _conexoes.AdicionarAsync(conexao, ct);
        }
        else
        {
            conexao.ReiniciarFluxoDeAutorizacao(state, _clock);
            await _conexoes.AtualizarAsync(conexao, ct);
        }

        return _clienteOAuth.MontarUrlAutorizacao(state, _redirectUri);
    }

    /// <summary>
    /// Processa o redirect do Mercado Pago de volta para o Synclass (issue
    /// #203). O endpoint é anônimo (o redirect vem do navegador do
    /// Professor, sem sessão do Synclass), então o Professor é resolvido
    /// internamente pelo <paramref name="state"/> no registro persistido —
    /// <c>state</c> que não bate (ou expirou) lança
    /// <see cref="StateInvalidoException"/> antes de qualquer troca de
    /// <c>code</c>. Devolve o <c>ProfessorId</c> e o <c>CollectorId</c> do
    /// registro persistido para o controller logar
    /// <c>ProfessorConectouMercadoPago</c> com dados reais — o Domain não
    /// injeta <c>ILogger</c> (ver
    /// implementation.md#decisão-de-design-logging-de-professorconectoumercadopago-sem-violar-camadas).
    /// </summary>
    public async Task<(Guid ProfessorId, string CollectorId)> ProcessarCallbackAsync(
        string code, string state, CancellationToken ct)
    {
        var conexao = await _conexoes.ObterPorStateAsync(state, ct);
        if (conexao is null)
        {
            throw new StateInvalidoException();
        }

        conexao.ValidarState(state, _clock);

        var resultado = await _clienteOAuth.TrocarCodePorTokenAsync(code, _redirectUri, ct);
        conexao.RegistrarConexao(
            resultado.AccessToken, resultado.RefreshToken, resultado.CollectorId, resultado.ExpiraEm, _clock);
        await _conexoes.AtualizarAsync(conexao, ct);

        return (conexao.ProfessorId, conexao.CollectorId);
    }

    /// <summary>
    /// Resolve o <c>collector_id</c> da conta Mercado Pago do Professor
    /// (issue #203) para a Task #199 montar checkout. Retorna <see
    /// langword="null"/> quando o Professor não está conectado ou quando a
    /// renovação do token expirado falha (refresh também recusado) — nº caso
    /// a Task #199 deve tratar como "Professor não conectado" (ver
    /// implementation.md#contrato-com-a-task-199).
    /// </summary>
    public virtual async Task<string?> ObterCollectorIdAsync(Guid professorId, CancellationToken ct)
    {
        var conexao = await _conexoes.ObterPorProfessorAsync(professorId, ct);
        if (conexao is null || !conexao.EstaConectada())
        {
            return null;
        }

        if (!conexao.PrecisaRenovar(_clock))
        {
            return conexao.CollectorId;
        }

        var renovada = await RenovarTokenAsync(conexao, ct);
        return renovada
            ? conexao.CollectorId
            : null;
    }

    /// <summary>
    /// Renova o access token via <c>refresh_token</c> (issue #203). Devolve
    /// <see langword="false"/> (e o registro é removido, porque o token
    /// revogado é irrecuperável e o "conectado" ficaria enganoso) só quando
    /// a renovação falha — ver implementation.md#renovação-de-token.
    /// </summary>
    private async Task<bool> RenovarTokenAsync(ConexaoMercadoPago conexao, CancellationToken ct)
    {
        TrocaCodePorTokenResultado resultado;
        try
        {
            resultado = await _clienteOAuth.RenovarTokenAsync(conexao.RefreshToken, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // Cancelamento genuíno do chamador (ex: cliente HTTP
            // desconectou) — não é falha de renovação, não deve apagar a
            // conexão do Professor. Deixa propagar como cancelamento normal.
            throw;
        }
        catch (Exception)
        {
            await _conexoes.RemoverAsync(conexao, ct);
            return false;
        }

        conexao.AtualizarCredenciais(
            resultado.AccessToken, resultado.RefreshToken, resultado.ExpiraEm, _clock);
        await _conexoes.AtualizarAsync(conexao, ct);
        return true;
    }

    /// <summary>
    /// Gera o <c>state</c> aleatório que liga o fluxo OAuth ao Professor
    /// (issue #203): 32 bytes criptográficos, base64url — o mesmo padrão e
    /// entropia do token de convite (ver
    /// <c>GeradorDeTokenConvite</c>).
    /// </summary>
    private static string GerarState()
    {
        var bytesAleatorios = RandomNumberGenerator.GetBytes(TamanhoStateBytes);
        return Convert.ToBase64String(bytesAleatorios).Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }
}
