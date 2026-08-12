using System.Security.Cryptography;
using System.Text;
using Synclass.Domain.Common;

namespace Synclass.Domain.Autenticacao;

/// <summary>
/// Código de uso único (OTP) emitido para autenticar um <see cref="Usuarios.Usuario"/>
/// (ver Regra de Negócio da issue #18). Expira em <see cref="Validade"/> e só
/// pode ser usado uma vez — <see cref="Invalidar"/> marca tanto o uso
/// bem-sucedido quanto a substituição por um código mais novo, já que a
/// migration não guarda os dois motivos separadamente (ver
/// docs/specs/18-login-otp/implementation.md#edge-points). Bloqueia após
/// <see cref="MaxTentativasFalhas"/> tentativas incorretas para dificultar
/// brute-force do código de 6 dígitos (dev-review do PR #25, issue #18).
/// </summary>
public sealed class CodigoOtp
{
    public static readonly TimeSpan Validade = TimeSpan.FromMinutes(10);

    public const int MaxTentativasFalhas = 5;

    private CodigoOtp(Guid id, Guid usuarioId, string codigoHash, DateTimeOffset expiraEm, DateTimeOffset? usadoEm, DateTimeOffset createdAt, int tentativasFalhas)
    {
        Id = id;
        UsuarioId = usuarioId;
        CodigoHash = codigoHash;
        ExpiraEm = expiraEm;
        UsadoEm = usadoEm;
        CreatedAt = createdAt;
        TentativasFalhas = tentativasFalhas;
    }

    public Guid Id { get; private set; }

    public Guid UsuarioId { get; private set; }

    public string CodigoHash { get; private set; }

    public DateTimeOffset ExpiraEm { get; private set; }

    public DateTimeOffset? UsadoEm { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public int TentativasFalhas { get; private set; }

    /// <summary>
    /// Verdadeiro quando o código atingiu <see cref="MaxTentativasFalhas"/>
    /// tentativas incorretas e não deve mais ser aceito, mesmo que o código
    /// informado esteja correto (dificulta brute-force do OTP de 6 dígitos).
    /// </summary>
    public bool Bloqueado => TentativasFalhas >= MaxTentativasFalhas;

    /// <summary>
    /// Cria um novo código OTP para o usuário. <paramref name="codigo"/> já
    /// vem gerado (ex: por <c>IGeradorDeCodigoOtp</c>) — esta entidade só
    /// guarda o hash, nunca o código em texto puro.
    /// </summary>
    public static CodigoOtp Gerar(Guid usuarioId, string codigo, IClock clock)
    {
        return new CodigoOtp(Guid.NewGuid(), usuarioId, HashDeCodigoOtp.Gerar(codigo), clock.UtcNow.Add(Validade), null, clock.UtcNow, 0);
    }

    /// <summary>
    /// Compara o código informado com o hash guardado em tempo constante
    /// (<see cref="CryptographicOperations.FixedTimeEquals"/>), evitando que
    /// a duração da comparação vaze informação sobre quantos caracteres do
    /// hash já coincidem (timing attack — dev-review do PR #25, issue #18).
    /// </summary>
    public bool Corresponde(string codigoBruto)
    {
        var hashInformado = HashDeCodigoOtp.Gerar(codigoBruto);
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(CodigoHash),
            Encoding.UTF8.GetBytes(hashInformado));
    }

    public bool Expirado(IClock clock)
    {
        return clock.UtcNow >= ExpiraEm;
    }

    public void Invalidar(IClock clock)
    {
        UsadoEm = clock.UtcNow;
    }

    /// <summary>
    /// Registra uma tentativa de confirmação com código incorreto. Após
    /// <see cref="MaxTentativasFalhas"/> chamadas, <see cref="Bloqueado"/>
    /// passa a ser verdadeiro e o código não é mais aceito.
    /// </summary>
    public void RegistrarTentativaFalha()
    {
        TentativasFalhas++;
    }
}
