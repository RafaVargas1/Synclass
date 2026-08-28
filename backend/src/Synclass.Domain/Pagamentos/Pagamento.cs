using Synclass.Domain.Common;

namespace Synclass.Domain.Pagamentos;

/// <summary>
/// Pagamento de valor devido de uma <see cref="Matricula"/> num período
/// (issue #199). O <see cref="Id"/> é passado pelo chamador, não gerado no
/// construtor — <c>PagamentoService.IniciarAsync</c> precisa do id antes de
/// criar a preferência de checkout no Mercado Pago, porque o id vira o
/// <c>external_reference</c> do payload (ver
/// implementation.md#entidade-pagamento). O <see cref="Valor"/> é congelado
/// na criação — nunca recalcular ao confirmar. <see cref="Confirmar"/>/
/// <see cref="Falhar"/> são idempotentes: o webhook de #200 pode chegar
/// duplicado sem mudar estado já terminal. Todos os timestamps vêm de
/// <see cref="IClock"/> (nunca <c>DateTime.UtcNow</c> direto — ver
/// docs/spec/code-style.md#dependências), mesmo padrão de
/// <see cref="Convites.Convite"/>/<see cref="Alocacoes.AlocacaoHorario"/>.
/// <see cref="EventoId"/> é a chave de idempotência do webhook de #200: o
/// <c>data.id</c> do ÚLTIMO evento do Mercado Pago processado com sucesso
/// para este pagamento (ver <see cref="WebhookMercadoPagoService"/>).
/// </summary>
public sealed class Pagamento
{
    private Pagamento()
    {
        // EF Core
        UrlCheckout = string.Empty;
    }

    public Pagamento(
        Guid id,
        Guid matriculaId,
        Guid alunoUsuarioId,
        Guid professorId,
        decimal valor,
        DateOnly inicio,
        DateOnly fimExclusivo,
        string urlCheckout,
        string referenciaExterna,
        IClock clock)
    {
        if (valor <= 0)
        {
            throw new ArgumentException($"Valor deve ser maior que zero: {valor}.", nameof(valor));
        }

        Id = id;
        MatriculaId = matriculaId;
        AlunoUsuarioId = alunoUsuarioId;
        ProfessorId = professorId;
        Valor = valor;
        PeriodoInicio = inicio;
        PeriodoFimExclusivo = fimExclusivo;
        Status = StatusPagamento.Pendente;
        UrlCheckout = urlCheckout;
        ReferenciaExterna = referenciaExterna;
        CriadoEm = clock.UtcNow.UtcDateTime;
    }

    public Guid Id { get; private set; }

    public Guid MatriculaId { get; private set; }

    public Guid AlunoUsuarioId { get; private set; }

    public Guid ProfessorId { get; private set; }

    public decimal Valor { get; private set; }

    public DateOnly PeriodoInicio { get; private set; }

    public DateOnly PeriodoFimExclusivo { get; private set; }

    public StatusPagamento Status { get; private set; }

    /// <summary>
    /// Id da preferência no Mercado Pago — é o que casa a notificação do
    /// webhook de #200 com este pagamento.
    /// </summary>
    public string? ReferenciaExterna { get; private set; }

    /// <summary>
    /// URL do checkout (init_point) gravada na criação para reaproveitar um
    /// <c>Pendente</c> sem nova chamada ao Mercado Pago.
    /// </summary>
    public string UrlCheckout { get; private set; }

    public DateTime CriadoEm { get; private set; }

    public DateTime? ConfirmadoEm { get; private set; }

    public DateTime? FalhouEm { get; private set; }

    /// <summary>
    /// O <c>data.id</c> do webhook do Mercado Pago do ÚLTIMO evento
    /// processado com sucesso pra este pagamento (issue #200). Chave de
    /// idempotência: se o <see cref="WebhookMercadoPagoService"/> já viu esse
    /// evento antes (<c>EventoId == data.id</c>), não reprocessa. É
    /// sobrescrito a cada evento novo processado (nunca <c>??=</c>) — se
    /// travasse no primeiro, uma reentrega de um evento *seguinte* (ex:
    /// estorno depois da confirmação) não seria detectada como duplicata (ver
    /// implementation.md#entidade-pagamento e task.md#inconsistências-encontradas,
    /// item 2). Nulo enquanto nenhum evento de webhook relacionado a este
    /// pagamento foi processado.
    /// </summary>
    public string? EventoId { get; private set; }

    /// <summary>
    /// Registra que o evento de webhook <paramref name="eventoId"/> (o
    /// <c>data.id</c> do Mercado Pago, issue #200) foi processado com sucesso
    /// pra este pagamento, sobrescrevendo <see cref="EventoId"/> — sempre
    /// reflete o ÚLTIMO evento tratado (ver a doc de <see cref="EventoId"/>
    /// e implementation.md#entidade-pagamento).
    /// </summary>
    public void RegistrarEventoId(string eventoId)
    {
        EventoId = eventoId;
    }

    /// <summary>
    /// Transição <c>Pendente → Confirmado</c> marcando <see cref="ConfirmadoEm"/>.
    /// Idempotente: chamada em estado já <c>Confirmado</c> não muda nada.
    /// </summary>
    public void Confirmar(IClock clock)
    {
        if (Status is StatusPagamento.Confirmado)
        {
            return;
        }

        Status = StatusPagamento.Confirmado;
        ConfirmadoEm = clock.UtcNow.UtcDateTime;
    }

    /// <summary>
    /// Transição para <c>Falhou</c> marcando <see cref="FalhouEm"/>.
    /// Idempotente: <c>Falhou</c> ou <c>Confirmado</c> já terminal não muda.
    /// </summary>
    public void Falhar(IClock clock)
    {
        if (Status is StatusPagamento.Falhou or StatusPagamento.Confirmado)
        {
            return;
        }

        Status = StatusPagamento.Falhou;
        FalhouEm = clock.UtcNow.UtcDateTime;
    }

    /// <summary>
    /// Transição <c>Confirmado → Estornado</c> (issue #200): pagamento que
    /// tinha sido confirmado foi estornado/reembolsado pelo Mercado Pago
    /// (evento <c>refunded</c>/<c>rejected</c> depois de <c>approved</c>). Só
    /// age quando <c>Status == Confirmado</c> — a regra "o valor volta a
    /// aparecer como devido" só faz sentido se o pagamento tinha sido de fato
    /// confirmado antes; nos demais estados (<c>Pendente</c>, <c>Falhou</c>, já
    /// <c>Estornado</c>) é no-op. Não toca em <see cref="ConfirmadoEm"/> —
    /// esse timestamp continua registrando quando o pagamento foi confirmado
    /// de fato, não o estorno. <c>clock</c> é recebido por consistência de
    /// assinatura com <see cref="Confirmar"/>/<see cref="Falhar"/> (issue
    /// #200), mas a transição não registra timestamp próprio (ver
    /// implementation.md#entidade-pagamento).
    /// </summary>
    public void Estornar(IClock clock)
    {
        if (Status is not StatusPagamento.Confirmado)
        {
            return;
        }

        Status = StatusPagamento.Estornado;
    }
}
