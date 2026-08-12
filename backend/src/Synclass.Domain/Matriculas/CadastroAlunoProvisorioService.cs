using Synclass.Domain.Common;
using Synclass.Domain.Usuarios;

namespace Synclass.Domain.Matriculas;

/// <summary>
/// Orquestra o cadastro de Aluno provisório: valida nome e identificador,
/// garante unicidade do identificador por Professor, e cria a
/// <see cref="Matricula"/> (issue #3). Nunca exige contato/login do Aluno —
/// diferente de <c>CadastroProfessorService</c> (issue #1), que cria
/// identidade de usuário plena.
/// </summary>
public sealed class CadastroAlunoProvisorioService
{
    private readonly IMatriculaRepository _matriculas;
    private readonly IUsuarioRepository _usuarios;
    private readonly IClock _clock;

    public CadastroAlunoProvisorioService(IMatriculaRepository matriculas, IUsuarioRepository usuarios, IClock clock)
    {
        _matriculas = matriculas;
        _usuarios = usuarios;
        _clock = clock;
    }

    public async Task<Matricula> CadastrarAsync(
        Guid professorId, string nome, string identificador, CancellationToken cancellationToken)
    {
        var nomeValidado = ValidarNome(nome);
        var identificadorValidado = IdentificadorProvisorio.Validar(identificador);

        await GarantirProfessorExisteAsync(professorId, cancellationToken);
        await GarantirIdentificadorDisponivelAsync(professorId, identificadorValidado, cancellationToken);

        var matricula = Matricula.CriarProvisoria(professorId, nomeValidado, identificadorValidado, _clock);
        await _matriculas.AdicionarAsync(matricula, cancellationToken);
        await _matriculas.SalvarAsync(cancellationToken);
        return matricula;
    }

    /// <summary>
    /// Reusa <c>NomeUsuario.Validar</c> (mesma regra "nome vazio é inválido"
    /// da issue #1) sem acoplar a Api deste módulo ao tipo de exceção de
    /// Usuarios — traduz para <see cref="NomeProvisorioInvalidoException"/>,
    /// que já é um <see cref="MatriculaRejeitadaException"/>.
    /// </summary>
    private static string ValidarNome(string nomeBruto)
    {
        try
        {
            return NomeUsuario.Validar(nomeBruto);
        }
        catch (NomeInvalidoException ex)
        {
            throw new NomeProvisorioInvalidoException(ex.Message);
        }
    }

    /// <summary>
    /// Confirma que <paramref name="professorId"/> corresponde a um Usuario
    /// existente antes de criar a Matricula. Sem essa checagem, um
    /// professorId inexistente só falhava no Postgres por violação de
    /// foreign key — capturada, por engano, junto com o conflito de índice
    /// único em <c>MatriculaRepository.SalvarAsync</c> e relançada como
    /// <see cref="MatriculaConcorrenteException"/> (achado de dev-review no
    /// PR #22).
    /// </summary>
    private async Task GarantirProfessorExisteAsync(Guid professorId, CancellationToken cancellationToken)
    {
        var professorExiste = await _usuarios.ExisteAsync(professorId, cancellationToken);
        if (!professorExiste)
        {
            throw new ProfessorNaoEncontradoException(professorId);
        }
    }

    private async Task GarantirIdentificadorDisponivelAsync(
        Guid professorId, string identificadorValidado, CancellationToken cancellationToken)
    {
        var existente = await _matriculas.BuscarPorIdentificadorAsync(professorId, identificadorValidado, cancellationToken);
        if (existente is not null)
        {
            throw new IdentificadorProvisorioDuplicadoException(identificadorValidado);
        }
    }
}
