using Synclass.Domain.Common;

namespace Synclass.Domain.Autenticacao;

/// <summary>
/// Código de uso único (OTP) emitido para autenticar um <see cref="Usuarios.Usuario"/>
/// (ver Regra de Negócio da issue #18). Expira em <see cref="Validade"/> e só
/// pode ser usado uma vez — <see cref="Invalidar"/> marca tanto o uso
/// bem-sucedido quanto a substituição por um código mais novo, já que a
/// migration não guarda os dois motivos separadamente (ver
/// docs/specs/18-login-otp/implementation.md#edge-points).
/// </summary>
public sealed class CodigoOtp
{
    public static readonly TimeSpan Validade = TimeSpan.FromMinutes(10);

    private CodigoOtp(Guid id, Guid usuarioId, string codigoHash, DateTimeOffset expiraEm, DateTimeOffset? usadoEm, DateTimeOffset createdAt)
    {
        Id = id;
        UsuarioId = usuarioId;
        CodigoHash = codigoHash;
        ExpiraEm = expiraEm;
        UsadoEm = usadoEm;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public Guid UsuarioId { get; private set; }

    public string CodigoHash { get; private set; }

    public DateTimeOffset ExpiraEm { get; private set; }

    public DateTimeOffset? UsadoEm { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// Cria um novo código OTP para o usuário. <paramref name="codigo"/> já
    /// vem gerado (ex: por <c>IGeradorDeCodigoOtp</c>) — esta entidade só
    /// guarda o hash, nunca o código em texto puro.
    /// </summary>
    public static CodigoOtp Gerar(Guid usuarioId, string codigo, IClock clock)
    {
        return new CodigoOtp(Guid.NewGuid(), usuarioId, HashDeCodigoOtp.Gerar(codigo), clock.UtcNow.Add(Validade), null, clock.UtcNow);
    }

    public bool Corresponde(string codigoBruto)
    {
        return CodigoHash == HashDeCodigoOtp.Gerar(codigoBruto);
    }

    public bool Expirado(IClock clock)
    {
        return clock.UtcNow >= ExpiraEm;
    }

    public void Invalidar(IClock clock)
    {
        UsadoEm = clock.UtcNow;
    }
}
