using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Synclass.Domain.Autenticacao;
using Synclass.Domain.Common;
using Synclass.Domain.Usuarios;

namespace Synclass.Infrastructure.Autenticacao;

/// <summary>
/// Implementação de <see cref="IGeradorDeTokenSessao"/> que envolve
/// <see cref="JwtSecurityTokenHandler"/> (ver
/// docs/spec/code-style.md#dependências — bibliotecas de terceiros ficam
/// atrás de uma interface fina do projeto). Token stateless assinado com
/// HMAC-SHA256, papéis do usuário embutidos como claims <c>role</c>.
/// </summary>
public sealed class GeradorDeTokenSessaoJwt : IGeradorDeTokenSessao
{
    private readonly SigningCredentials _credenciais;
    private readonly TimeSpan _validade;
    private readonly IClock _clock;

    public GeradorDeTokenSessaoJwt(string chaveDeAssinatura, int diasDeValidade, IClock clock)
    {
        var chave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(chaveDeAssinatura));
        _credenciais = new SigningCredentials(chave, SecurityAlgorithms.HmacSha256);
        _validade = TimeSpan.FromDays(diasDeValidade);
        _clock = clock;
    }

    public string Gerar(Usuario usuario)
    {
        var token = new JwtSecurityToken(
            claims: MontarClaims(usuario),
            expires: (_clock.UtcNow + _validade).UtcDateTime,
            signingCredentials: _credenciais);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static IEnumerable<Claim> MontarClaims(Usuario usuario)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
            new(JwtRegisteredClaimNames.Name, usuario.Nome),
        };
        claims.AddRange(usuario.Papeis.Select(p => new Claim(ClaimTypes.Role, p.Papel.ToString())));
        return claims;
    }
}
