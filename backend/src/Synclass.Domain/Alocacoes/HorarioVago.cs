using Synclass.Domain.Horarios;

namespace Synclass.Domain.Alocacoes;

/// <summary>
/// Um <see cref="Horarios.Horario"/> elegível para marcação livre do Aluno,
/// já com a quantidade de vagas restantes calculada — devolvido por
/// <see cref="AlocacaoHorarioService.ListarVagosAsync"/> (issue #9). Par
/// puro (sem I/O), montado pelo Service depois de consultar
/// <c>IAlocacaoHorarioRepository.ContarPorHorarioAsync</c>.
/// </summary>
public sealed record HorarioVago(Horario Horario, int VagasRestantes);
