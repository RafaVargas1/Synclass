using Synclass.Domain.Autenticacao;
using Synclass.Domain.Usuarios;

namespace Synclass.Domain.Tests.Fakes;

/// <summary>
/// Devolve um token previsível ("token-para-{UsuarioId}") em vez de assinar
/// um JWT real — a assinatura/serialização real é coberta pelo teste de
/// <c>GeradorDeTokenSessaoJwt</c> no Infrastructure.
/// </summary>
public sealed class FakeGeradorDeTokenSessao : IGeradorDeTokenSessao
{
    public string Gerar(Usuario usuario)
    {
        return $"token-para-{usuario.Id}";
    }
}
