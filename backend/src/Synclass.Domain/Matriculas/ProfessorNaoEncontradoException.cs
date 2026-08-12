namespace Synclass.Domain.Matriculas;

/// <summary>
/// Lançada quando o <c>professorId</c> recebido para o cadastro de Aluno
/// provisório (issue #3) não corresponde a nenhum <c>Usuario</c> existente.
/// Sem essa checagem antecipada, o <c>professorId</c> inválido só falharia no
/// Postgres por violação de foreign key (23503) — capturada, por engano,
/// junto com o conflito de índice único (23505) em
/// <c>MatriculaRepository.SalvarAsync</c>, e relançada como
/// <see cref="MatriculaConcorrenteException"/>. Isso instruía o cliente a
/// "tentar novamente" um cadastro que nunca teria sucesso — achado de
/// dev-review no PR #22. Distinta de <see cref="MatriculaConcorrenteException"/>
/// também no HTTP mapeado pela Api (404, não 400): são semânticas diferentes.
/// </summary>
public sealed class ProfessorNaoEncontradoException : MatriculaRejeitadaException
{
    public ProfessorNaoEncontradoException(Guid professorId)
        : base($"Professor não encontrado: {professorId}. Esperado o id de um Usuario com papel Professor já cadastrado.")
    {
    }
}
