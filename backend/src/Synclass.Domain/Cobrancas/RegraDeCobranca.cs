using Synclass.Domain.Common;

namespace Synclass.Domain.Cobrancas;

/// <summary>
/// Estado comum, mapeável por EF Core (TPH), a toda implementação de
/// <see cref="IRegraDeCobranca"/> (issue #11). O cálculo em si fica a cargo
/// de cada subclasse concreta (<see cref="RegraFixoMensal"/>,
/// <see cref="RegraFixoPorAula"/>, <see cref="RegraValorPorAula"/>).
/// </summary>
public abstract class RegraDeCobranca : IRegraDeCobranca
{
    protected RegraDeCobranca(Guid id, Guid matriculaId, decimal valor, DateTimeOffset createdAt, DateTimeOffset updatedAt)
    {
        if (valor <= 0)
        {
            throw new ValorDeRegraDeCobrancaInvalidoException(valor);
        }

        Id = id;
        MatriculaId = matriculaId;
        Valor = valor;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public Guid Id { get; private set; }

    public Guid MatriculaId { get; private set; }

    public decimal Valor { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public abstract decimal CalcularValorDevido(int quantidadeDeAulasNoPeriodo);
}
