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

    public async Task SalvarAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            // Índice único (Contato, ou UsuarioId+Papel) violado: duas
            // requisições concorrentes passaram pela checagem de duplicidade
            // da aplicação antes de qualquer uma confirmar a escrita.
            // Preserva a causa raiz (ex) para diagnóstico — sem isso, o erro
            // real do Npgsql/EF Core fica invisível em log e em teste.
            throw new CadastroConcorrenteException(ex);
        }
    }
}
