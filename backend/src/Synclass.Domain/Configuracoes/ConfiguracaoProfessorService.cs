using Synclass.Domain.Common;

namespace Synclass.Domain.Configuracoes;

/// <summary>
/// Orquestra o único caso de uso de escrita do card (issue #7):
/// <see cref="DefinirModeloAsync"/> cria a configuração do Professor se ela
/// ainda não existir, ou altera o modelo já existente — operação idempotente
/// do ponto de vista do chamador (mesmo endpoint da Api cobre os dois casos,
/// ver implementation.md#contrato-de-api).
/// </summary>
public sealed class ConfiguracaoProfessorService
{
    private readonly IConfiguracaoProfessorRepository _configuracoes;
    private readonly IClock _clock;

    public ConfiguracaoProfessorService(IConfiguracaoProfessorRepository configuracoes, IClock clock)
    {
        _configuracoes = configuracoes;
        _clock = clock;
    }

    public async Task<ConfiguracaoProfessor> DefinirModeloAsync(
        Guid professorId, ModeloAgendamento modeloAgendamento, CancellationToken cancellationToken)
    {
        var configuracaoExistente = await _configuracoes.BuscarPorProfessorAsync(professorId, cancellationToken);
        if (configuracaoExistente is not null)
        {
            configuracaoExistente.AlterarModelo(modeloAgendamento, _clock);
            await _configuracoes.SalvarAsync(cancellationToken);
            return configuracaoExistente;
        }

        var configuracao = ConfiguracaoProfessor.Criar(professorId, modeloAgendamento, _clock);
        await _configuracoes.AdicionarAsync(configuracao, cancellationToken);
        await _configuracoes.SalvarAsync(cancellationToken);
        return configuracao;
    }
}
