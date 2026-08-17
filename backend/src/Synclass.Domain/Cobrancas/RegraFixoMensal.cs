using Synclass.Domain.Common;

namespace Synclass.Domain.Cobrancas;

/// <summary>
/// Regra de cobrança "fixo mensal": o valor devido não depende da
/// quantidade de aulas do período, real ou contratada (RN da issue #11 —
/// mudar a frequência real registrada, item 14, não altera o valor).
/// </summary>
public sealed class RegraFixoMensal : RegraDeCobranca
{
    private RegraFixoMensal(Guid id, Guid matriculaId, decimal valor, DateTimeOffset createdAt, DateTimeOffset updatedAt)
        : base(id, matriculaId, valor, createdAt, updatedAt)
    {
    }

    public static RegraFixoMensal Criar(Guid matriculaId, decimal valor, IClock clock)
    {
        return new RegraFixoMensal(Guid.NewGuid(), matriculaId, valor, clock.UtcNow, clock.UtcNow);
    }

    public override decimal CalcularValorDevido(int quantidadeDeAulasNoPeriodo)
    {
        return Valor;
    }
}
