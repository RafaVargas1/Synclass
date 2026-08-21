using Microsoft.EntityFrameworkCore;
using Synclass.Domain.Alunos;
using Synclass.Infrastructure.Persistence;

namespace Synclass.Infrastructure.Alunos;

/// <summary>
/// Checa se um identificador de Aluno já está em uso consultando as duas
/// colunas em que ele é gravado (issue #70, ver
/// <see cref="IIdentificadorAlunoUnicidadeChecker"/>): <c>Usuario.IdentificadorAluno</c>
/// e <c>Matricula.IdentificadorAluno</c>. A unicidade é permanente (nunca
/// expira), então a checagem varre todo identificador já emitido, sem
/// filtro de validade.
/// </summary>
public sealed class IdentificadorAlunoUnicidadeChecker : IIdentificadorAlunoUnicidadeChecker
{
    private readonly SynclassDbContext _dbContext;

    public IdentificadorAlunoUnicidadeChecker(SynclassDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> ExisteEmUsoAsync(string identificador, CancellationToken cancellationToken)
    {
        var emUsuario = await _dbContext.Usuarios
            .AnyAsync(u => u.IdentificadorAluno == identificador, cancellationToken);
        if (emUsuario)
        {
            return true;
        }

        return await _dbContext.Matriculas
            .AnyAsync(m => m.IdentificadorAluno == identificador, cancellationToken);
    }
}
