using Microsoft.EntityFrameworkCore;
using Synclass.Domain.Usuarios;

namespace Synclass.Infrastructure.Persistence;

/// <summary>
/// Implementação EF Core de <see cref="IUsuarioRepository"/>.
/// </summary>
public sealed class UsuarioRepository : IUsuarioRepository
{
    private readonly SynclassDbContext _dbContext;

    public UsuarioRepository(SynclassDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Usuario?> BuscarPorContatoAsync(string contatoNormalizado, CancellationToken cancellationToken)
    {
        return _dbContext.Usuarios
            .Include(u => u.Papeis)
            .FirstOrDefaultAsync(u => u.Contato == contatoNormalizado, cancellationToken);
    }

    public async Task AdicionarAsync(Usuario usuario, CancellationToken cancellationToken)
    {
        await _dbContext.Usuarios.AddAsync(usuario, cancellationToken);
    }

    public Task SalvarAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
