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
    private readonly IClock _clock;

    public CadastroAlunoProvisorioService(IMatriculaRepository matriculas, IClock clock)
    {
        _matriculas = matriculas;
        _clock = clock;
    }

    public async Task<Matricula> CadastrarAsync(
        Guid professorId, string nome, string identificador, CancellationToken cancellationToken)
    {
        var nomeValidado = ValidarNome(nome);
        var identificadorValidado = IdentificadorProvisorio.Validar(identificador);

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
