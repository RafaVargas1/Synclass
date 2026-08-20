namespace Synclass.Domain.Alunos;

/// <summary>
/// Checa se um identificador de Aluno já está em uso, cobrindo
/// simultaneamente <c>Usuario.IdentificadorAluno</c> e
/// <c>Matricula.IdentificadorAluno</c> — os dois lugares onde um
/// identificador de Aluno é gravado (issue #70). A unicidade é permanente
/// (nunca expira, diferente do código de convite de 5 dígitos), por isso a
/// checagem vale contra todo identificador já emitido. Implementado em
/// Synclass.Infrastructure consultando os repositórios/banco, permitindo ao
/// Domain e seus testes de unidade não dependerem de banco.
/// </summary>
public interface IIdentificadorAlunoUnicidadeChecker
{
    /// <summary>
    /// Devolve <c>true</c> se <paramref name="identificador"/> já pertence a
    /// um <c>Usuario</c> (papel Aluno) ou a uma <c>Matricula</c> — ver
    /// docs/specs/70-identificador-aluno/implementation.md ("Unicidade
    /// permanente").
    /// </summary>
    Task<bool> ExisteEmUsoAsync(string identificador, CancellationToken cancellationToken);
}
