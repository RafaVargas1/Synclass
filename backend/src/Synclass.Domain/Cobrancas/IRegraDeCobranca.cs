namespace Synclass.Domain.Cobrancas;

/// <summary>
/// Contrato comum a toda regra de cobrança (Strategy — issue #11): dado um
/// vínculo (<see cref="MatriculaId"/>) e uma quantidade de aulas do período,
/// calcula o valor devido. Este contrato não muda quando uma nova
/// implementação é adicionada — ver
/// <see cref="Synclass.Domain.Tests.Cobrancas.RegrasDeCobrancaContratoTests"/>.
/// </summary>
public interface IRegraDeCobranca
{
    Guid Id { get; }

    Guid MatriculaId { get; }

    decimal CalcularValorDevido(int quantidadeDeAulasNoPeriodo);
}
