using Synclass.Domain.Usuarios;

namespace Synclass.Domain.Autenticacao;

/// <summary>
/// Sessão resultante de um login confirmado: o usuário autenticado e o
/// token assinado que representa a sessão (ver
/// <see cref="IGeradorDeTokenSessao"/>).
/// </summary>
public sealed record ResultadoLogin(Usuario Usuario, string Token);
