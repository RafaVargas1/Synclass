using Synclass.Domain.Alunos;

namespace Synclass.Domain.Tests.Fakes;

/// <summary>
/// Checador de unicidade em memória usado nos testes de unidade de
/// <see cref="IdentificadorAlunoService"/>, no lugar da implementação real
/// que consulta <c>Usuario</c>/<c>Matricula</c> no banco — permite simular
/// colisão e ausência de colisão de forma determinística (mesmo padrão de
/// <see cref="FakeGeradorDeCodigoConvite"/> / <see cref="FakeConviteRepository"/>).
/// </summary>
public sealed class FakeIdentificadorAlunoUnicidadeChecker : IIdentificadorAlunoUnicidadeChecker
{
    private readonly HashSet<string> _emUso;

    public FakeIdentificadorAlunoUnicidadeChecker()
        : this(Array.Empty<string>())
    {
    }

    public FakeIdentificadorAlunoUnicidadeChecker(params string[] emUso)
    {
        _emUso = new HashSet<string>(emUso);
    }

    public int VezesChamado { get; private set; }

    public Task<bool> ExisteEmUsoAsync(string identificador, CancellationToken cancellationToken)
    {
        VezesChamado++;
        return Task.FromResult(_emUso.Contains(identificador));
    }
}
