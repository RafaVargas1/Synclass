using System.Security.Claims;

namespace Synclass.Api;

/// <summary>
/// Único ponto de leitura da identidade do <c>Usuario</c> autenticado a
/// partir do JWT (issue #23) — usado por todo controller que precisa saber
/// "quem está chamando" em vez de confiar num parâmetro de rota
/// (<c>professorId</c>/<c>matriculaId</c>). <see cref="ClaimTypes.NameIdentifier"/>,
/// não <c>"sub"</c>: <see cref="Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerOptions"/>
/// não desliga <c>MapInboundClaims</c>, então o handler já remapeia o claim
/// <c>sub</c> assinado por <c>GeradorDeTokenSessaoJwt</c> para
/// <see cref="ClaimTypes.NameIdentifier"/> antes do controller enxergar a
/// claim.
/// </summary>
public static class ClaimsPrincipalExtensions
{
    public static Guid GetUsuarioId(this ClaimsPrincipal principal)
    {
        var valor = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (valor is null || !Guid.TryParse(valor, out var usuarioId))
        {
            throw new InvalidOperationException(
                "Token autenticado sem claim de identidade válida — inesperado sob [Authorize].");
        }

        return usuarioId;
    }
}
