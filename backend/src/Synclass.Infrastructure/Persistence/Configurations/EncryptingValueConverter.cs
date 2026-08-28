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
/// </summary>
public sealed class EncryptingValueConverter : ValueConverter<string, string>
{
    private static readonly IDataProtector Protector =
        DataProtectionProvider.Create("Synclass").CreateProtector("ConexaoMercadoPago.Tokens");

    public EncryptingValueConverter()
        : base(
            convertToProviderExpression: plaintext => Protector.Protect(plaintext),
            convertFromProviderExpression: ciphertext => Protector.Unprotect(ciphertext))
    {
    }
}
