using Synclass.Domain.Common;

namespace Synclass.Domain.Usuarios;

/// <summary>
/// Um papel atribuído a um <see cref="Usuario"/> em um instante específico.
/// Tabela <c>PapeisUsuario</c> com índice único em (UsuarioId, Papel) a
/// nível de banco, para impedir papel duplicado mesmo se a checagem de
/// aplicação for contornada.
/// </summary>
public sealed class PapelAtribuido
{
    private PapelAtribuido(Guid id, Guid usuarioId, PapelUsuario papel, DateTimeOffset createdAt)
    {
        Id = id;
        UsuarioId = usuarioId;
        Papel = papel;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public Guid UsuarioId { get; private set; }

    public PapelUsuario Papel { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    internal static PapelAtribuido Criar(Guid usuarioId, PapelUsuario papel, IClock clock)
    {
        return new PapelAtribuido(Guid.NewGuid(), usuarioId, papel, clock.UtcNow);
    }
}
