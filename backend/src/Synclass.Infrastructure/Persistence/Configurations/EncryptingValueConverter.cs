using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Synclass.Infrastructure.Persistence.Configurations;

/// <summary>
/// <see cref="ValueConverter{TModel,TProvider}"/> que criptografa
/// <c>AccessToken</c>/<c>RefreshToken</c> da <see
/// cref="Synclass.Domain.Pagamentos.ConexaoMercadoPago"/> em repouso via
/// <see cref="DataProtectionProvider"/> (issue #203) — credenciais de
/// terceiros que abrem o cofre do Professor, ver
/// implementation.md#decisão-de-design-criptografia. Usa o provider
/// standalone (<c>DataProtectionProvider.Create</c>) em vez de DI para
/// funcionar também na geração de migrations (design time), onde o
/// <c>SynclassDbContextFactory</c> não passa por DI da Api.
///
/// <para>
/// As chaves são persistidas explicitamente em disco (variável de ambiente
/// <c>MERCADOPAGO_DATAPROTECTION_KEYS_DIR</c>, com fallback para
/// <c>AppContext.BaseDirectory/data-protection-keys</c>) — sem isso,
/// <c>DataProtectionProvider.Create("Synclass")</c> pode cair no
/// repositório de chaves efêmero (em memória) num container Linux sem
/// perfil de usuário/registro persistidos, o que torna todo
/// <c>AccessToken</c>/<c>RefreshToken</c> já criptografado
/// irrecuperável (<see cref="System.Security.Cryptography.CryptographicException"/>
/// em <c>Unprotect</c>) no próximo restart/deploy do container, ou entre
/// réplicas diferentes da Api. Ver <c>docker-compose.yml</c> para o volume
/// que garante a persistência entre restarts do container.
/// </para>
/// </summary>
public sealed class EncryptingValueConverter : ValueConverter<string, string>
{
    private static readonly IDataProtector Protector = CriarProtector();

    public EncryptingValueConverter()
        : base(
            convertToProviderExpression: plaintext => Protector.Protect(plaintext),
            convertFromProviderExpression: ciphertext => Protector.Unprotect(ciphertext))
    {
    }

    private static IDataProtector CriarProtector()
    {
        var diretorioChaves = Environment.GetEnvironmentVariable("MERCADOPAGO_DATAPROTECTION_KEYS_DIR")
            ?? Path.Combine(AppContext.BaseDirectory, "data-protection-keys");

        return DataProtectionProvider
            .Create(new DirectoryInfo(diretorioChaves), configuracao => configuracao.SetApplicationName("Synclass"))
            .CreateProtector("ConexaoMercadoPago.Tokens");
    }
}
