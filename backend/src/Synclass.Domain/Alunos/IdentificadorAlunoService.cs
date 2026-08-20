namespace Synclass.Domain.Alunos;

/// <summary>
/// Orquestra a geração de um identificador de Aluno único (issue #70):
/// gera via <see cref="IGeradorDeIdentificadorAluno"/> e reexecuta em caso
/// de colisão contra <see cref="IIdentificadorAlunoUnicidadeChecker"/> —
/// mesmo desenho de
/// <see cref="Synclass.Domain.Convites.ConviteService.GerarCodigoUnicoAsync"/>,
/// mas a checagem aqui é permanente (nunca expira), reutilizado por
/// <c>CadastroUsuarioService</c> (#61), <c>ConviteService</c> (#63) e
/// <c>CadastroAlunoProvisorioService</c> (provisório) — fonte única da
/// regra, sem duplicação (ver docs/specs/70-identificador-aluno/implementation.md).
/// </summary>
public sealed class IdentificadorAlunoService
{
    /// <summary>
    /// Teto do loop de retry — mesmo valor de referência de
    /// <see cref="Synclass.Domain.Convites.ConviteService.LimiteDeTentativasDeCodigo"/>:
    /// guardrail contra loop indefinido, não um limite esperado em operação
    /// normal dado o tamanho do espaço de identificadores.
    /// </summary>
    private const int LimiteDeTentativas = 20;

    private readonly IGeradorDeIdentificadorAluno _gerador;
    private readonly IIdentificadorAlunoUnicidadeChecker _checador;

    public IdentificadorAlunoService(IGeradorDeIdentificadorAluno gerador, IIdentificadorAlunoUnicidadeChecker checador)
    {
        _gerador = gerador;
        _checador = checador;
    }

    public async Task<string> GerarUnicoAsync(CancellationToken cancellationToken)
    {
        for (var tentativas = 1; tentativas <= LimiteDeTentativas; tentativas++)
        {
            var identificador = _gerador.Gerar();
            var emUso = await _checador.ExisteEmUsoAsync(identificador, cancellationToken);
            if (!emUso)
            {
                return identificador;
            }
        }

        throw new LimiteDeTentativasDeIdentificadorAlunoExcedidoException(LimiteDeTentativas);
    }
}
