using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Synclass.Api.Middleware;
using Synclass.Domain.Matriculas;
using Synclass.Domain.Usuarios;

namespace Synclass.Api.Controllers;

/// <summary>
/// Vínculos (Professores) do Aluno autenticado (issue #165) — alimenta o
/// resumo de "próximo horário" do Painel, que precisa agregar
/// `GET /professores/{professorId}/horarios/proximas-aulas` por todos os
/// Professores do Aluno. Não existe hoje outra forma de obter essa lista
/// (ver docs/specs/165-aluno-ve-proximo-horario/task.md).
/// </summary>
[Authorize(Roles = "Aluno")]
[ApiController]
[Route("alunos/professores")]
public sealed class VinculosAlunoController : ControllerBase
{
    private readonly IMatriculaRepository _matriculas;
    private readonly IUsuarioRepository _usuarios;

    public VinculosAlunoController(IMatriculaRepository matriculas, IUsuarioRepository usuarios)
    {
        _matriculas = matriculas;
        _usuarios = usuarios;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken cancellationToken)
    {
        var alunoId = User.GetUsuarioId();
        var matriculas = await _matriculas.ListarPorAlunoAsync(alunoId, cancellationToken);

        var professorIds = matriculas.Select(m => m.ProfessorId).Distinct().ToList();
        var vinculos = new List<VinculoProfessorResponse>();
        foreach (var professorId in professorIds)
        {
            var professor = await _usuarios.BuscarPorIdAsync(professorId, cancellationToken);
            if (professor is not null)
            {
                vinculos.Add(new VinculoProfessorResponse(professorId, professor.Nome));
            }
        }
        return Ok(vinculos);
    }
}

public sealed record VinculoProfessorResponse(Guid ProfessorId, string Nome);
