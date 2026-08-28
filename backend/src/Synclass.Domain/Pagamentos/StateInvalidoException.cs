namespace Synclass.Domain.Pagamentos;

/// <summary>
/// Lançada pelo callback OAuth quando o <c>state</c> recebido não confere com
/// nenhum fluxo pendente (issue #203): ou nenhum registro guarda aquele
/// <c>state</c>, ou o <c>StateExpiraEm</c> já passou (fluxo abandonado há
/// mais de <see cref="ConexaoMercadoPago.StateValidadeMinutos"/> minutos).
/// O estado é a prova de que quem chamou o callback é o mesmo Professor que
/// iniciou o fluxo — rejeitar isso impede que um link de autorização
/// vazado/reenviado fique válido indefinidamente.
/// </summary>
public sealed class StateInvalidoException : Exception
{
    public StateInvalidoException()
        : base("Estado de autorização inválido: o fluxo não existe ou expirou. Inicie uma nova conexão.")
    {
    }
}
