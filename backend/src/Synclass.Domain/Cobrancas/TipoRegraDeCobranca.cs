namespace Synclass.Domain.Cobrancas;

/// <summary>
/// Só usado como parâmetro de entrada do Service/Controller para escolher a
/// classe concreta de <see cref="RegraDeCobranca"/> — não é o mesmo tipo do
/// discriminador de string do EF Core (ver implementation.md#modelo-de-dados),
/// para não acoplar o schema de persistência ao contrato HTTP.
/// </summary>
public enum TipoRegraDeCobranca
{
    ValorPorAula = 0,
    FixoMensal = 1,
    FixoPorAula = 2,
}
