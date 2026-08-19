using Synclass.Domain.Common;

namespace Synclass.Domain.Frequencias;

/// <summary>
/// Registra, para um par (<see cref="AulaId"/>, <see cref="MatriculaId"/>), o
/// status de presença marcado pelo Professor (<see cref="StatusProfessor"/>,
/// issue #14) e a confirmação do Aluno (<see cref="ConfirmadoPeloAluno"/>,
/// issue #15). Índice único (AulaId, MatriculaId) garante 1 linha por Aluno
/// por aula — mesma postura de <c>CancelamentoAula</c> (issue #10), ver
/// <c>RegistroFrequenciaConfiguration</c>.
/// </summary>
public sealed class RegistroFrequencia
{
    private RegistroFrequencia(
        Guid id,
        Guid aulaId,
        Guid matriculaId,
        StatusFrequencia? statusProfessor,
        bool? confirmadoPeloAluno,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
    {
        Id = id;
        AulaId = aulaId;
        MatriculaId = matriculaId;
        StatusProfessor = statusProfessor;
        ConfirmadoPeloAluno = confirmadoPeloAluno;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public Guid Id { get; private set; }

    public Guid AulaId { get; private set; }

    public Guid MatriculaId { get; private set; }

    public StatusFrequencia? StatusProfessor { get; private set; }

    public bool? ConfirmadoPeloAluno { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>
    /// Cria a linha com os dois campos de status em branco — o upsert parte
    /// da premissa de que a linha pode nascer tanto pelo Professor (este
    /// card) quanto, futuramente, pelo Aluno (issue #15).
    /// </summary>
    public static RegistroFrequencia Criar(Guid aulaId, Guid matriculaId, IClock clock)
    {
        return new RegistroFrequencia(Guid.NewGuid(), aulaId, matriculaId, null, null, clock.UtcNow, clock.UtcNow);
    }

    /// <summary>
    /// <c>internal</c>, visível só para <c>Synclass.Domain.Tests</c> (ver
    /// <c>InternalsVisibleTo</c> no <c>.csproj</c>) — seeda uma linha com
    /// <see cref="ConfirmadoPeloAluno"/> já preenchido para os testes de
    /// reconciliação de <c>FrequenciaService.RegistrarAsync</c> (AC2/AC3).
    /// Não é o caminho de escrita real da confirmação do Aluno: esse é
    /// <see cref="ConfirmarAluno"/> (issue #15), chamado por
    /// <see cref="FrequenciaService.ConfirmarPresencaAsync"/>.
    /// </summary>
    internal static RegistroFrequencia CriarComConfirmacaoDoAluno(
        Guid aulaId, Guid matriculaId, bool confirmadoPeloAluno, IClock clock)
    {
        return new RegistroFrequencia(
            Guid.NewGuid(), aulaId, matriculaId, null, confirmadoPeloAluno, clock.UtcNow, clock.UtcNow);
    }

    /// <summary>
    /// Marca a presença/ausência decidida pelo Professor — idempotente por
    /// natureza (AC4: chamar de novo sobrescreve, não duplica). Nunca mexe em
    /// <see cref="ConfirmadoPeloAluno"/>, preservando o valor existente
    /// mesmo em divergência (AC3).
    /// </summary>
    public void RegistrarProfessor(StatusFrequencia status, IClock clock)
    {
        StatusProfessor = status;
        UpdatedAt = clock.UtcNow;
    }

    /// <summary>
    /// Marca a confirmação de presença feita pelo próprio Aluno (issue #15)
    /// — idempotente por natureza (AC2: confirmar de novo não duplica nem
    /// muda o resultado). Simetria exata de <see cref="RegistrarProfessor"/>:
    /// nunca mexe em <see cref="StatusProfessor"/>, preservando o valor
    /// existente mesmo que o Professor já tenha registrado antes (AC3, ordem
    /// inversa).
    /// </summary>
    public void ConfirmarAluno(IClock clock)
    {
        ConfirmadoPeloAluno = true;
        UpdatedAt = clock.UtcNow;
    }
}
