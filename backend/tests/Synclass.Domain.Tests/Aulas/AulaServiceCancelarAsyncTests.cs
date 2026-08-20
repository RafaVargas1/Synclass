using FluentAssertions;
using Synclass.Domain.Alocacoes;
using Synclass.Domain.Aulas;
using Synclass.Domain.Configuracoes;
using Synclass.Domain.Horarios;
using Synclass.Domain.Matriculas;
using Synclass.Domain.Tests.Fakes;

namespace Synclass.Domain.Tests.Aulas;

/// <summary>
/// Cobre <see cref="AulaService.CancelarAsync"/> (issue #10) — o Aluno
/// desmarca uma aula já alocada, respeitando o prazo configurado pelo
/// Professor. Mesmo padrão de cenário de
/// <c>AlocacaoHorarioServiceMarcarAsyncTests</c> (issue #9).
/// </summary>
public sealed class AulaServiceCancelarAsyncTests
{
    private static readonly FixedClock Clock = new(new DateTimeOffset(2026, 8, 18, 12, 0, 0, TimeSpan.Zero));
    private static readonly Guid ProfessorId = Guid.NewGuid();

    private sealed record Cenario(
        AulaService AulaService,
        FakeAulaRepository Aulas,
        FakeCancelamentoAulaRepository Cancelamentos,
        FakeAlocacaoHorarioRepository Alocacoes,
        FakeMatriculaRepository Matriculas,
        FakeConfiguracaoProfessorRepository Configuracoes,
        HorarioService HorarioService);

    private static Cenario CriarCenario(int prazoCancelamentoMinutos = 0)
    {
        var configuracoes = new FakeConfiguracaoProfessorRepository();
        configuracoes.Configuracoes.Add(
            ConfiguracaoProfessor.Criar(ProfessorId, ModeloAgendamento.Vago, Clock, prazoCancelamentoMinutos));

        var horarios = new FakeHorarioRepository();
        var horarioService = new HorarioService(horarios, Clock);
        var aulas = new FakeAulaRepository();
        var cancelamentos = new FakeCancelamentoAulaRepository();
        var alocacoes = new FakeAlocacaoHorarioRepository();
        var matriculas = new FakeMatriculaRepository();
        var aulaService = new AulaService(
            aulas, cancelamentos, alocacoes, matriculas, configuracoes, horarioService, Clock);
        return new Cenario(aulaService, aulas, cancelamentos, alocacoes, matriculas, configuracoes, horarioService);
    }

    private static async Task<Matricula> CriarMatriculaAlocadaAsync(Cenario cenario, Guid horarioId)
    {
        var matricula = Matricula.CriarProvisoria(ProfessorId, "Aluno Um", $"aluno-{Guid.NewGuid()}", Clock);
        await cenario.Matriculas.AdicionarAsync(matricula, CancellationToken.None);
        var alocacao = AlocacaoHorario.Criar(horarioId, matricula.Id, OrigemAlocacao.Aluno, Clock);
        await cenario.Alocacoes.AdicionarAsync(alocacao, CancellationToken.None);
        return matricula;
    }

    [Fact]
    public async Task CancelarAsync_AulaAindaNaoExisteParaEssaData_InstanciaSobDemanda()
    {
        var cenario = CriarCenario();
        var horario = await cenario.HorarioService.CadastrarAsync(
            ProfessorId, DiaSemana.Terca, new TimeOnly(10, 0), 60, TipoMarcacao.Livre, CancellationToken.None);
        var matricula = await CriarMatriculaAlocadaAsync(cenario, horario.Id);
        var data = new DateOnly(2026, 8, 20);

        await cenario.AulaService.CancelarAsync(ProfessorId, horario.Id, data, matricula.Id, CancellationToken.None);

        cenario.Aulas.Aulas.Should().ContainSingle(a => a.HorarioId == horario.Id && a.Data == data);
    }

    /// <summary>
    /// AC1 — Professor configura prazo de 24h; aula em 30h de antecedência
    /// (dentro do prazo) permite o cancelamento.
    /// </summary>
    [Fact]
    public async Task CancelarAsync_DentroDoPrazoConfigurado_PermiteECriaOCancelamento()
    {
        var cenario = CriarCenario(prazoCancelamentoMinutos: 24 * 60);
        // Relógio fixo em 18/08 12:00; aula em 20/08 18:00 = 30h de antecedência.
        var horario = await cenario.HorarioService.CadastrarAsync(
            ProfessorId, DiaSemana.Quinta, new TimeOnly(18, 0), 60, TipoMarcacao.Livre, CancellationToken.None);
        var matricula = await CriarMatriculaAlocadaAsync(cenario, horario.Id);
        var data = new DateOnly(2026, 8, 20);

        var cancelamento = await cenario.AulaService.CancelarAsync(
            ProfessorId, horario.Id, data, matricula.Id, CancellationToken.None);

        cancelamento.MatriculaId.Should().Be(matricula.Id);
        cenario.Cancelamentos.Cancelamentos.Should().ContainSingle();
    }

    /// <summary>
    /// AC2 — mesmo prazo de 24h, mas aula em 10h de antecedência (fora do
    /// prazo): rejeitado com a mensagem indicando até quando era possível
    /// cancelar.
    /// </summary>
    [Fact]
    public async Task CancelarAsync_ForaDoPrazoConfigurado_RejeitaComPrazoCancelamentoExpiradoException()
    {
        var cenario = CriarCenario(prazoCancelamentoMinutos: 24 * 60);
        // Relógio fixo em 18/08 12:00; aula em 18/08 22:00 = 10h de antecedência.
        var horario = await cenario.HorarioService.CadastrarAsync(
            ProfessorId, DiaSemana.Terca, new TimeOnly(22, 0), 60, TipoMarcacao.Livre, CancellationToken.None);
        var matricula = await CriarMatriculaAlocadaAsync(cenario, horario.Id);
        var data = new DateOnly(2026, 8, 18);

        var acao = () => cenario.AulaService.CancelarAsync(ProfessorId, horario.Id, data, matricula.Id, CancellationToken.None);

        await acao.Should().ThrowAsync<PrazoCancelamentoExpiradoException>();
        cenario.Cancelamentos.Cancelamentos.Should().BeEmpty();
    }

    /// <summary>
    /// AC3 — cancelar afeta só a ocorrência daquela data (o `CancelamentoAula`
    /// criado), sem remover ou alterar a `AlocacaoHorario` recorrente: o
    /// Aluno continua alocado no horário nas semanas seguintes.
    /// </summary>
    [Fact]
    public async Task CancelarAsync_CancelamentoValido_NaoAfetaAAlocacaoHorarioRecorrente()
    {
        var cenario = CriarCenario();
        var horario = await cenario.HorarioService.CadastrarAsync(
            ProfessorId, DiaSemana.Terca, new TimeOnly(10, 0), 60, TipoMarcacao.Livre, CancellationToken.None);
        var matricula = await CriarMatriculaAlocadaAsync(cenario, horario.Id);

        await cenario.AulaService.CancelarAsync(
            ProfessorId, horario.Id, new DateOnly(2026, 8, 20), matricula.Id, CancellationToken.None);

        cenario.Alocacoes.Alocacoes.Should().ContainSingle(a => a.HorarioId == horario.Id && a.MatriculaId == matricula.Id);
    }

    /// <summary>
    /// AC4 — dois Alunos alocados no mesmo horário: o cancelamento de um não
    /// cria `CancelamentoAula` para o outro (independência).
    /// </summary>
    [Fact]
    public async Task CancelarAsync_HorarioComDoisAlunosAlocados_CancelaApenasOAlunoQuePediu()
    {
        var cenario = CriarCenario();
        var horario = await cenario.HorarioService.CadastrarAsync(
            ProfessorId, DiaSemana.Terca, new TimeOnly(10, 0), 60, TipoMarcacao.Livre, CancellationToken.None, limiteAlunos: 2);
        var matriculaQueCancela = await CriarMatriculaAlocadaAsync(cenario, horario.Id);
        var outraMatricula = await CriarMatriculaAlocadaAsync(cenario, horario.Id);
        var data = new DateOnly(2026, 8, 20);

        await cenario.AulaService.CancelarAsync(ProfessorId, horario.Id, data, matriculaQueCancela.Id, CancellationToken.None);

        cenario.Cancelamentos.Cancelamentos.Should().ContainSingle(c => c.MatriculaId == matriculaQueCancela.Id);
        cenario.Cancelamentos.Cancelamentos.Should().NotContain(c => c.MatriculaId == outraMatricula.Id);
    }

    /// <summary>
    /// AC5 — o prazo aplicado é sempre o vigente no momento do cancelamento:
    /// um cancelamento feito quando o prazo era 24h não é reavaliado quando
    /// o Professor muda para 48h depois.
    /// </summary>
    [Fact]
    public async Task CancelarAsync_PrazoAlteradoAposCancelamentoAnterior_NaoReavaliaCancelamentoJaFeito()
    {
        var cenario = CriarCenario(prazoCancelamentoMinutos: 24 * 60);
        // Aula em 20/08 14:00 = 26h de antecedência: dentro dos 24h vigentes.
        var horario = await cenario.HorarioService.CadastrarAsync(
            ProfessorId, DiaSemana.Quinta, new TimeOnly(14, 0), 60, TipoMarcacao.Livre, CancellationToken.None);
        var matricula = await CriarMatriculaAlocadaAsync(cenario, horario.Id);
        var data = new DateOnly(2026, 8, 20);
        await cenario.AulaService.CancelarAsync(ProfessorId, horario.Id, data, matricula.Id, CancellationToken.None);

        cenario.Configuracoes.Configuracoes.Single().AlterarPrazoCancelamento(48 * 60, Clock);

        // Cancelamento já feito continua registrado, sem lançar/reverter.
        cenario.Cancelamentos.Cancelamentos.Should().ContainSingle(c => c.MatriculaId == matricula.Id);
    }

    /// <summary>
    /// Edge point — cancelar uma aula já cancelada pelo mesmo Aluno é
    /// idempotente: não lança, retorna o `CancelamentoAula` existente sem
    /// revalidar o prazo (mesmo se a segunda tentativa já está fora dele).
    /// </summary>
    [Fact]
    public async Task CancelarAsync_AulaJaCanceladaPeloMesmoAluno_EIdempotenteERetornaOExistente()
    {
        var cenario = CriarCenario(prazoCancelamentoMinutos: 24 * 60);
        var horario = await cenario.HorarioService.CadastrarAsync(
            ProfessorId, DiaSemana.Quinta, new TimeOnly(18, 0), 60, TipoMarcacao.Livre, CancellationToken.None);
        var matricula = await CriarMatriculaAlocadaAsync(cenario, horario.Id);
        var data = new DateOnly(2026, 8, 20);
        var primeiroCancelamento = await cenario.AulaService.CancelarAsync(
            ProfessorId, horario.Id, data, matricula.Id, CancellationToken.None);

        var segundoCancelamento = await cenario.AulaService.CancelarAsync(
            ProfessorId, horario.Id, data, matricula.Id, CancellationToken.None);

        segundoCancelamento.Id.Should().Be(primeiroCancelamento.Id);
        cenario.Cancelamentos.Cancelamentos.Should().ContainSingle();
    }

    /// <summary>
    /// Aluno não alocado neste horário tentando cancelar é rejeitado, mesmo
    /// padrão de `MatriculaNaoVinculadaAoProfessorException` (issue #8/#9).
    /// </summary>
    [Fact]
    public async Task CancelarAsync_MatriculaNaoAlocadaNesteHorario_RejeitaComAlocacaoNaoEncontradaException()
    {
        var cenario = CriarCenario();
        var horario = await cenario.HorarioService.CadastrarAsync(
            ProfessorId, DiaSemana.Terca, new TimeOnly(10, 0), 60, TipoMarcacao.Livre, CancellationToken.None);
        var matriculaNaoAlocada = Matricula.CriarProvisoria(ProfessorId, "Aluno Dois", "aluno-2", Clock);
        await cenario.Matriculas.AdicionarAsync(matriculaNaoAlocada, CancellationToken.None);

        var acao = () => cenario.AulaService.CancelarAsync(
            ProfessorId, horario.Id, new DateOnly(2026, 8, 20), matriculaNaoAlocada.Id, CancellationToken.None);

        // Nome ambíguo com Synclass.Domain.Alocacoes.AlocacaoNaoEncontradaException
        // (mesmo namespace importado neste arquivo) — ver
        // Synclass.Domain.Aulas.AlocacaoNaoEncontradaException para a
        // justificativa da duplicidade de nome.
        await acao.Should().ThrowAsync<Synclass.Domain.Aulas.AlocacaoNaoEncontradaException>();
        cenario.Cancelamentos.Cancelamentos.Should().BeEmpty();
    }
}
