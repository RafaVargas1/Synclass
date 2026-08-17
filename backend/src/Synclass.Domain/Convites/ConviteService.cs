using Synclass.Domain.Common;
using Synclass.Domain.Matriculas;
using Synclass.Domain.Usuarios;

namespace Synclass.Domain.Convites;

/// <summary>
/// Orquestra a geração e o aceite de convite direcionado (issue #2),
/// espelhando a forma de <c>LoginService</c> (dois métodos relacionados no
/// mesmo serviço).
/// </summary>
public sealed class ConviteService
{
    private readonly IConviteRepository _convites;
    private readonly IMatriculaRepository _matriculas;
    private readonly IUsuarioRepository _usuarios;
    private readonly IGeradorDeTokenConvite _geradorDeToken;
    private readonly IClock _clock;
    private readonly int _diasValidade;

    public ConviteService(
        IConviteRepository convites,
        IMatriculaRepository matriculas,
        IUsuarioRepository usuarios,
        IGeradorDeTokenConvite geradorDeToken,
        IClock clock,
        int diasValidade)
    {
        _convites = convites;
        _matriculas = matriculas;
        _usuarios = usuarios;
        _geradorDeToken = geradorDeToken;
        _clock = clock;
        _diasValidade = diasValidade;
    }

    /// <summary>
    /// Gera um convite direcionado a um contato (critério de aceite 1).
    /// Rejeita <paramref name="professorId"/> inexistente, matrícula de
    /// origem inválida (edge point) e contato já vinculado como Aluno pleno
    /// a este Professor (critério de aceite 4).
    /// </summary>
    public async Task<Convite> GerarAsync(
        Guid professorId, string contatoBruto, Guid? matriculaId, CancellationToken cancellationToken)
    {
        var contatoNormalizado = Contato.Normalizar(contatoBruto);
        var contatoTipo = Contato.IdentificarTipo(contatoNormalizado);

        await GarantirProfessorExisteAsync(professorId, cancellationToken);
        if (matriculaId is not null)
        {
            await GarantirMatriculaOrigemValidaAsync(professorId, matriculaId.Value, cancellationToken);
        }

        await GarantirContatoNaoVinculadoAsync(professorId, contatoNormalizado, cancellationToken);

        var token = _geradorDeToken.Gerar();
        var convite = Convite.Gerar(professorId, contatoNormalizado, contatoTipo, matriculaId, token, _diasValidade, _clock);
        await _convites.AdicionarAsync(convite, cancellationToken);
        await _convites.SalvarAsync(cancellationToken);
        return convite;
    }

    private async Task GarantirProfessorExisteAsync(Guid professorId, CancellationToken cancellationToken)
    {
        var professorExiste = await _usuarios.ExisteAsync(professorId, cancellationToken);
        if (!professorExiste)
        {
            throw new ProfessorNaoEncontradoException(professorId);
        }
    }

    private async Task GarantirMatriculaOrigemValidaAsync(Guid professorId, Guid matriculaId, CancellationToken cancellationToken)
    {
        var matricula = await _matriculas.BuscarPorIdAsync(matriculaId, cancellationToken);
        var invalida = matricula is null || matricula.ProfessorId != professorId || matricula.AlunoUsuarioId is not null;
        if (invalida)
        {
            throw new MatriculaOrigemInvalidaException(matriculaId);
        }
    }

    private async Task GarantirContatoNaoVinculadoAsync(Guid professorId, string contatoNormalizado, CancellationToken cancellationToken)
    {
        var usuario = await _usuarios.BuscarPorContatoAsync(contatoNormalizado, cancellationToken);
        if (usuario is null)
        {
            return;
        }

        var vinculo = await _matriculas.BuscarVinculoAsync(professorId, usuario.Id, cancellationToken);
        if (vinculo is not null)
        {
            throw new ContatoJaVinculadoException();
        }
    }
}
