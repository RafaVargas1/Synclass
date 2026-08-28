using Synclass.Domain.Common;

namespace Synclass.Domain.Pagamentos;

/// <summary>
/// Vínculo do Professor (um <see cref="Usuarios.Usuario"/> com papel
/// Professor) com a conta Mercado Pago dele (issue #203). É a primeira
/// entidade de domínio de integração financeira do Synclass — armazena as
/// credenciais OAuth do Mercado Pago para a Task #199 poder criar checkout
/// em nome do Professor. <see cref="AccessToken"/>/<see cref="RefreshToken"/>
/// são criptografados em repouso na persistência (ver
/// <c>ConexaoMercadoPagoConfiguration</c>); aqui na entidade ficam em texto
/// plano, no ciclo de vida da aplicação.
/// </summary>
public sealed class ConexaoMercadoPago
{
    /// <summary>
    /// Janela de validade do <see cref="State"/> pendente (issue #203) —
    /// limite de minutos antes do callback ser considerado expirado.
    /// </summary>
    public const int StateValidadeMinutos = 10;

    /// <summary>
    /// Margem de segurança de minutos antes da expiração real do
    /// <see cref="AccessToken"/> em que a renovação passa a ser necessária
    /// (issue #203) — evita corrida de borda do token vencendo entre a
    /// checagem e o uso (ver implementation.md#edge-points).
    /// </summary>
    public const int MargemRenovacaoMinutos = 5;

    private ConexaoMercadoPago(
        Guid id,
        Guid professorId,
        string accessToken,
        string refreshToken,
        string collectorId,
        string? state,
        DateTimeOffset expiraEm,
        DateTimeOffset? stateExpiraEm,
        DateTimeOffset criadoEm,
        DateTimeOffset atualizadoEm)
    {
        Id = id;
        ProfessorId = professorId;
        AccessToken = accessToken;
        RefreshToken = refreshToken;
        CollectorId = collectorId;
        State = state;
        ExpiraEm = expiraEm;
        StateExpiraEm = stateExpiraEm;
        CriadoEm = criadoEm;
        AtualizadoEm = atualizadoEm;
    }

    public Guid Id { get; private set; }

    /// <summary>
    /// Identifica o Professor (que É um <see cref="Usuarios.Usuario"/>, ver
    /// <c>ProfessoresController.Cadastrar</c>). Sem propriedade de navegação
    /// para <c>Usuario</c>: nenhum consumer precisa navegar de conexão para
    /// usuário ou vice-versa (ver implementation.md#por-que-sem-navegação).
    /// </summary>
    public Guid ProfessorId { get; private set; }

    /// <summary>
    /// Token de acesso à API do Mercado Pago. Criptografado em repouso —
    /// texto plano apenas durante o ciclo de vida da aplicação.
    /// </summary>
    public string AccessToken { get; private set; }

    /// <summary>
    /// Token de renovação do <see cref="AccessToken"/> (OAuth 2.0
    /// <c>refresh_token</c>). Criptografado em repouso, mesmo padrão do
    /// access token.
    /// </summary>
    public string RefreshToken { get; private set; }

    /// <summary>
    /// Identificador público da conta Mercado Pago do Professor, vindo do
    /// <c>user_id</c> no payload de troca de <c>code</c> — consumido pela
    /// Task #199 no <c>collector_id</c> da preferência de checkout.
    /// </summary>
    public string CollectorId { get; private set; }

    /// <summary>
    /// Valor aleatório temporário que liga o fluxo OAuth ao Professor (issue
    /// #203) — limpo (nullable) após o callback bem-sucedido; não é para
    /// reuso. <see langword="null"/> quando não há fluxo em andamento.
    /// </summary>
    public string? State { get; private set; }

    /// <summary>
    /// Quando <see cref="AccessToken"/> expira (vindo de <c>expires_in</c>
    /// do Mercado Pago), em UTC.
    /// </summary>
    public DateTimeOffset ExpiraEm { get; private set; }

    /// <summary>
    /// Fim da janela de validade do <see cref="State"/> pendente; <see
    /// langword="null"/> quando não há fluxo OAuth em andamento.
    /// </summary>
    public DateTimeOffset? StateExpiraEm { get; private set; }

    public DateTimeOffset CriadoEm { get; private set; }

    public DateTimeOffset AtualizadoEm { get; private set; }

    /// <summary>
    /// Cria uma conexão nova, ainda sem credenciais (preenchidas só no
    /// callback), já no início de um fluxo de autorização — o
    /// <paramref name="state"/> fica pendente por <see cref="StateValidadeMinutos"/>
    /// minutos.
    /// </summary>
    public static ConexaoMercadoPago IniciarFluxoDeAutorizacao(
        Guid professorId, string state, IClock clock)
    {
        var agora = clock.UtcNow;
        return new ConexaoMercadoPago(
            Guid.NewGuid(), professorId, string.Empty, string.Empty, string.Empty,
            state, agora, agora.AddMinutes(StateValidadeMinutos), agora, agora);
    }

    /// <summary>
    /// Reconexão (issue #203): reaproveita o registro do Professor que já
    /// tinha uma conexão (ativa ou fluxo abandonado), sobrescrevendo <see
    /// cref="State"/>/<see cref="StateExpiraEm"/> sem tocar nas credenciais
    /// antigas (que continuam válidas até o novo callback confirmar a
    /// troca). Não cria segundo registro nem exige desconectar antes.
    /// </summary>
    public void ReiniciarFluxoDeAutorizacao(string state, IClock clock)
    {
        var agora = clock.UtcNow;
        State = state;
        StateExpiraEm = agora.AddMinutes(StateValidadeMinutos);
        AtualizadoEm = agora;
    }

    /// <summary>
    /// Valida o <paramref name="state"/> recebido no callback (issue #203):
    /// ou o estado não bate com o pendente, ou a janela de validade expirou
    /// (fluxo abandonado). Lança <see cref="StateInvalidoException"/>; nunca
    /// chega a trocar <c>code</c> quando o estado não confere.
    /// </summary>
    public void ValidarState(string state, IClock clock)
    {
        if (State is null || State != state || clock.UtcNow > StateExpiraEm)
        {
            throw new StateInvalidoException();
        }
    }

    /// <summary>
    /// Registra as credenciais obtidas na troca de <c>code</c> (issue #203):
    /// grava access/refresh token e <see cref="CollectorId"/> (do
    /// <c>user_id</c> do payload), ajusta <see cref="ExpiraEm"/> e limpa o
    /// fluxo pendente (<see cref="State"/>/<see cref="StateExpiraEm"/>).
    /// </summary>
    public void RegistrarConexao(
        string accessToken, string refreshToken, string collectorId, DateTimeOffset expiraEm, IClock clock)
    {
        AccessToken = accessToken;
        RefreshToken = refreshToken;
        CollectorId = collectorId;
        ExpiraEm = expiraEm;
        State = null;
        StateExpiraEm = null;
        AtualizadoEm = clock.UtcNow;
    }

    /// <summary>
    /// Substitui as credenciais após renovação via <c>refresh_token</c>
    /// (issue #203) — só o token/refresh/expiração mudam; o <see
    /// cref="CollectorId"/> permanece o mesmo.
    /// </summary>
    public void AtualizarCredenciais(
        string accessToken, string refreshToken, DateTimeOffset expiraEm, IClock clock)
    {
        AccessToken = accessToken;
        RefreshToken = refreshToken;
        ExpiraEm = expiraEm;
        AtualizadoEm = clock.UtcNow;
    }

    /// <summary>
    /// <see cref="AccessToken"/> vencido (com margem de segurança de
    /// <see cref="MargemRenovacaoMinutos"/> minutos antes da expiração real)
    /// — nesse caso a renovação via <c>refresh_token</c> se faz necessária
    /// antes de usar o token (issue #203).
    /// </summary>
    public bool PrecisaRenovar(IClock clock)
    {
        return ExpiraEm < clock.UtcNow.AddMinutes(MargemRenovacaoMinutos);
    }

    /// <summary>
    /// A conexão está completa (tem coletor e credenciais válidas em
    /// memória) — usada na clínica de "Professor conectado" pelas chamadas
    /// de leitura (issue #203).
    /// </summary>
    public bool EstaConectada()
    {
        return !string.IsNullOrWhiteSpace(CollectorId) && !string.IsNullOrWhiteSpace(AccessToken);
    }
}
