namespace Synclass.Domain.Alocacoes;

/// <summary>
/// Abstrai a persistência de <see cref="AlocacaoHorario"/>. Implementado em
/// Synclass.Infrastructure (EF Core), permitindo que o Domain e seus testes
/// de unidade não dependam de banco de dados.
/// </summary>
public interface IAlocacaoHorarioRepository
{
    /// <summary>
    /// Busca a alocação de um Aluno específico neste horário, se existir —
    /// usado para checar duplicidade antes de alocar e para localizar a
    /// linha a remover em <see cref="AlocacaoHorarioService.DesalocarAsync"/>.
    /// </summary>
    Task<AlocacaoHorario?> BuscarAsync(Guid horarioId, Guid matriculaId, CancellationToken cancellationToken);

    /// <summary>
    /// Conta quantos Alunos já estão alocados neste horário — comparado com
    /// <c>Horario.LimiteAlunos</c> para checar vaga disponível.
    /// </summary>
    Task<int> ContarPorHorarioAsync(Guid horarioId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<AlocacaoHorario>> ListarPorHorarioAsync(Guid horarioId, CancellationToken cancellationToken);

    /// <summary>
    /// Lista todas as alocações de uma <c>Matricula</c> — análogo a
    /// <see cref="ListarPorHorarioAsync"/> invertendo o lado da busca. Usado
    /// por <see cref="Synclass.Domain.Cobrancas.ConsultaCobrancaService"/>
    /// (issue #12) para chegar nos <c>Horario</c>s alocados a um vínculo e,
    /// a partir deles, na quantidade de aulas do período.
    /// </summary>
    Task<IReadOnlyCollection<AlocacaoHorario>> ListarPorMatriculaAsync(Guid matriculaId, CancellationToken cancellationToken);

    /// <summary>
    /// Existe ao menos uma <see cref="AlocacaoHorario"/> deste horário com
    /// <see cref="OrigemAlocacao.Professor"/> — "atribuição fixa" (issue #9),
    /// usada por <c>AlocacaoHorarioService.GarantirModeloPermiteMarcacaoAsync</c>
    /// no modelo Híbrido.
    /// </summary>
    Task<bool> PossuiAlocacaoOrigemProfessorAsync(Guid horarioId, CancellationToken cancellationToken);

    Task AdicionarAsync(AlocacaoHorario alocacao, CancellationToken cancellationToken);

    Task RemoverAsync(AlocacaoHorario alocacao, CancellationToken cancellationToken);

    Task SalvarAsync(CancellationToken cancellationToken);
}
