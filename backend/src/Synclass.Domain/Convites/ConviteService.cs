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

    /// <summary>
    /// Aceita um convite: valida o contato submetido contra o do convite
    /// (Regra de Negócio — "mesmo contato do convite"), marca o convite como
    /// usado antes de qualquer mutação de Usuario/Matricula (uso único), e
    /// então cria/reaproveita a identidade e promove ou cria o vínculo.
    /// </summary>
    public async Task<ResultadoAceiteConvite> AceitarAsync(
        string token, string nome, string contatoBruto, CancellationToken cancellationToken)
    {
        var convite = await _convites.BuscarPorTokenAsync(token, cancellationToken) ?? throw new ConviteInvalidoException();
        var contatoNormalizado = Contato.Normalizar(contatoBruto);
        if (contatoNormalizado != convite.Contato)
        {
            throw new ConviteContatoDivergenteException();
        }

        var nomeValidado = NomeUsuario.Validar(nome);
        convite.MarcarUsado(_clock);

        var usuario = await ObterOuCriarUsuarioAsync(nomeValidado, convite.Contato, cancellationToken);
        var matriculaPromovida = await VincularMatriculaAsync(convite, usuario.Id, cancellationToken);

        await _usuarios.SalvarAsync(cancellationToken);
        await _matriculas.SalvarAsync(cancellationToken);
        await _convites.SalvarAsync(cancellationToken);
        return new ResultadoAceiteConvite(usuario, convite.Id, matriculaPromovida);
    }

    private async Task<Usuario> ObterOuCriarUsuarioAsync(string nomeValidado, string contatoNormalizado, CancellationToken cancellationToken)
    {
        var usuarioExistente = await _usuarios.BuscarPorContatoAsync(contatoNormalizado, cancellationToken);
        if (usuarioExistente is not null)
        {
            AdicionarPapelAlunoIdempotente(usuarioExistente);
            return usuarioExistente;
        }

        var novoUsuario = Usuario.Cadastrar(nomeValidado, contatoNormalizado, PapelUsuario.Aluno, _clock);
        await _usuarios.AdicionarAsync(novoUsuario, cancellationToken);
        return novoUsuario;
    }

    /// <summary>
    /// "Já é Aluno" não deveria impedir a promoção de uma Matricula de
    /// origem específica — decisão documentada em
    /// docs/specs/2-convite-whatsapp/implementation.md.
    /// </summary>
    private void AdicionarPapelAlunoIdempotente(Usuario usuario)
    {
        try
        {
            usuario.AdicionarPapel(PapelUsuario.Aluno, _clock);
        }
        catch (PapelJaAtribuidoException)
        {
        }
    }

    /// <summary>
    /// Promove a matrícula de origem ou o vínculo já existente (devolve
    /// <c>true</c>), ou cria uma nova matrícula já vinculada quando nenhuma
    /// das duas existir (devolve <c>false</c>) — usado pelo log estruturado
    /// <c>ConviteAceito</c> para indicar qual caminho ocorreu (Critérios
    /// técnicos da issue #2).
    /// </summary>
    private async Task<bool> VincularMatriculaAsync(Convite convite, Guid alunoUsuarioId, CancellationToken cancellationToken)
    {
        if (convite.MatriculaId is not null)
        {
            var matriculaOrigem = await _matriculas.BuscarPorIdAsync(convite.MatriculaId.Value, cancellationToken)
                ?? throw new MatriculaOrigemInvalidaException(convite.MatriculaId.Value);
            matriculaOrigem.Promover(alunoUsuarioId);
            return true;
        }

        var vinculoExistente = await _matriculas.BuscarVinculoAsync(convite.ProfessorId, alunoUsuarioId, cancellationToken);
        if (vinculoExistente is not null)
        {
            return true;
        }

        var novaMatricula = Matricula.CriarVinculada(convite.ProfessorId, alunoUsuarioId, _clock);
        await _matriculas.AdicionarAsync(novaMatricula, cancellationToken);
        return false;
    }
}

/// <summary>
/// Resultado do aceite de convite: o <see cref="Usuario"/> resultante
/// (criado ou reaproveitado), o <see cref="Convite.Id"/> aceito, e se a
/// matrícula foi promovida (origem específica ou vínculo já existente) ou
/// criada nova — logado como <c>ConviteAceito</c> pela Api (Critérios
/// técnicos da issue #2).
/// </summary>
public sealed record ResultadoAceiteConvite(Usuario Usuario, Guid ConviteId, bool MatriculaPromovida);
