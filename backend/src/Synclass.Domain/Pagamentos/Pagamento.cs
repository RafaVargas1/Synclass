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
}
