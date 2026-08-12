using Synclass.Domain.Usuarios;

namespace Synclass.Domain.Autenticacao;

/// <summary>
/// Gera o token de sessão (JWT stateless, ver Regra de Negócio da issue #18)
/// para um usuário autenticado, com os papéis embutidos. Envolve a
/// biblioteca de assinatura de JWT (Synclass.Infrastructure) atrás de uma
/// interface fina, conforme docs/spec/code-style.md#dependências.
/// </summary>
public interface IGeradorDeTokenSessao
{
    string Gerar(Usuario usuario);
}
