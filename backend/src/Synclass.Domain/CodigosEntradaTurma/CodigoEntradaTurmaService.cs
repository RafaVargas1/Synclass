using Synclass.Domain.Common;
using Synclass.Domain.Convites;
using Synclass.Domain.Matriculas;
using Synclass.Domain.Usuarios;

namespace Synclass.Domain.CodigosEntradaTurma;

/// <summary>
/// Orquestra a geração e o aceite do código de entrada de turma —
/// espelhando a forma de <see cref="Synclass.Domain.Convites.ConviteService"/>
/// (dois métodos relacionados no mesmo serviço), mas sem uso único nem
/// vínculo a contato: qualquer Aluno autenticado que aceitar o código
/// dentro da janela de validade entra na turma, quantas vezes for.
/// </summary>
public sealed class CodigoEntradaTurmaService
{
    /// <summary>
    /// Teto do loop de <see cref="GerarCodigoUnicoAsync"/> — mesmo racional
    /// de <c>ConviteService.LimiteDeTentativasDeCodigo</c>: guardrail contra
    /// loop indefinido, não um limite esperado em operação normal.
    /// </summary>
    private const int LimiteDeTentativasDeCodigo = 20;

    private readonly ICodigoEntradaTurmaRepository _codigosEntrada;
    private readonly IMatriculaRepository _matriculas;
    private readonly IUsuarioRepository _usuarios;
    private readonly IGeradorDeCodigoConvite _geradorDeCodigo;
    private readonly IClock _clock;

    public CodigoEntradaTurmaService(
        ICodigoEntradaTurmaRepository codigosEntrada,
        IMatriculaRepository matriculas,
        IUsuarioRepository usuarios,
        IGeradorDeCodigoConvite geradorDeCodigo,
        IClock clock)
    {
        _codigosEntrada = codigosEntrada;
        _matriculas = matriculas;
        _usuarios = usuarios;
        _geradorDeCodigo = geradorDeCodigo;
        _clock = clock;
    }

    /// <summary>
    /// Gera um novo código de entrada pro Professor. Rejeita
    /// <paramref name="professorId"/> inexistente.
    /// </summary>
    public async Task<CodigoEntradaTurma> GerarAsync(Guid professorId, CancellationToken cancellationToken)
    {
        var professorExiste = await _usuarios.ExisteAsync(professorId, cancellationToken);
        if (!professorExiste)
        {
            throw new ProfessorNaoEncontradoException(professorId);
        }

        var codigo = await GerarCodigoUnicoAsync(cancellationToken);
        var codigoEntrada = CodigoEntradaTurma.Gerar(professorId, codigo, _clock);
        await _codigosEntrada.AdicionarAsync(codigoEntrada, cancellationToken);
        await _codigosEntrada.SalvarAsync(cancellationToken);
        return codigoEntrada;
    }

    /// <summary>
    /// Aceita um código de entrada em nome do Aluno autenticado
    /// <paramref name="alunoUsuarioId"/>. Idempotente: se o vínculo entre o
    /// Professor do código e este Aluno já existir, não duplica — só
    /// devolve sucesso de novo (o Aluno pode reusar o mesmo código sem
    /// erro).
    /// </summary>
    public async Task<ResultadoAceiteCodigoEntrada> AceitarAsync(
        string codigoBruto, Guid alunoUsuarioId, CancellationToken cancellationToken)
    {
        var codigoNormalizado = NormalizarCodigo(codigoBruto);
        var codigoEntrada = await _codigosEntrada.BuscarAtivoPorCodigoAsync(codigoNormalizado, _clock.UtcNow, cancellationToken)
            ?? throw new CodigoEntradaInvalidoException();

        var professor = await _usuarios.BuscarPorIdAsync(codigoEntrada.ProfessorId, cancellationToken);
        var professorNome = professor?.Nome ?? string.Empty;

        var vinculoExistente = await _matriculas.BuscarVinculoAsync(codigoEntrada.ProfessorId, alunoUsuarioId, cancellationToken);
        if (vinculoExistente is not null)
        {
            return new ResultadoAceiteCodigoEntrada(codigoEntrada.ProfessorId, professorNome, VinculoCriado: false);
        }

        var novaMatricula = Matricula.CriarVinculada(codigoEntrada.ProfessorId, alunoUsuarioId, _clock);
        await _matriculas.AdicionarAsync(novaMatricula, cancellationToken);
        await _matriculas.SalvarAsync(cancellationToken);
        return new ResultadoAceiteCodigoEntrada(codigoEntrada.ProfessorId, professorNome, VinculoCriado: true);
    }

    /// <summary>
    /// Gera um código único entre os códigos ativos (não expirados) — mesmo
    /// racional de <c>ConviteService.GerarCodigoUnicoAsync</c>, mas escopado
    /// à própria tabela <see cref="CodigoEntradaTurma"/>, não a
    /// <c>Convite</c>.
    /// </summary>
    private async Task<string> GerarCodigoUnicoAsync(CancellationToken cancellationToken)
    {
        var tentativas = 0;
        string codigo;
        bool codigoAtivo;
        do
        {
            if (tentativas >= LimiteDeTentativasDeCodigo)
            {
                throw new LimiteDeTentativasDeCodigoEntradaExcedidoException(LimiteDeTentativasDeCodigo);
            }

            codigo = _geradorDeCodigo.Gerar();
            tentativas++;
            codigoAtivo = await _codigosEntrada.ExisteCodigoAtivoAsync(codigo, _clock.UtcNow, cancellationToken);
        }
        while (codigoAtivo);

        return codigo;
    }

    /// <summary>
    /// Normaliza o código informado pelo Aluno para só dígitos, tolerando
    /// espaços/máscara — mesmo racional de
    /// <c>ConviteService.NormalizarCodigo</c>.
    /// </summary>
    private static string NormalizarCodigo(string codigoBruto)
    {
        return new string(codigoBruto.Where(char.IsDigit).ToArray());
    }
}

public sealed record ResultadoAceiteCodigoEntrada(Guid ProfessorId, string ProfessorNome, bool VinculoCriado);
