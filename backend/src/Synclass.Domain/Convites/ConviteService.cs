using Synclass.Domain.Alunos;
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
    private readonly IGeradorDeCodigoConvite _geradorDeCodigo;
    private readonly IClock _clock;
    private readonly int _diasValidade;
    private readonly IdentificadorAlunoService _identificadorAluno;

    /// <summary>
    /// Teto do loop de <see cref="GerarCodigoUnicoAsync"/> — o espaço de
    /// 100.000 combinações torna colisões repetidas extremamente raras (ver
    /// docs/specs/62-codigo-convite-curto/implementation.md), então este
    /// valor é só um guardrail contra loop indefinido, não um limite
    /// esperado em operação normal.
    /// </summary>
    private const int LimiteDeTentativasDeCodigo = 20;

    public ConviteService(
        IConviteRepository convites,
        IMatriculaRepository matriculas,
        IUsuarioRepository usuarios,
        IGeradorDeTokenConvite geradorDeToken,
        IGeradorDeCodigoConvite geradorDeCodigo,
        IClock clock,
        int diasValidade,
        IdentificadorAlunoService identificadorAluno)
    {
        _convites = convites;
        _matriculas = matriculas;
        _usuarios = usuarios;
        _geradorDeToken = geradorDeToken;
        _geradorDeCodigo = geradorDeCodigo;
        _clock = clock;
        _diasValidade = diasValidade;
        _identificadorAluno = identificadorAluno;
    }

    /// <summary>
    /// Gera um convite direcionado a um contato (critério de aceite 1).
    /// Rejeita <paramref name="professorId"/> inexistente, matrícula de
    /// origem inválida (edge point) e contato já vinculado como Aluno pleno
    /// a este Professor (critério de aceite 4).
    /// </summary>
    public async Task<ResultadoGeracaoConvite> GerarAsync(
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
        var (codigo, tentativas) = await GerarCodigoUnicoAsync(cancellationToken);
        var convite = Convite.Gerar(professorId, contatoNormalizado, contatoTipo, matriculaId, token, codigo, _diasValidade, _clock);
        await _convites.AdicionarAsync(convite, cancellationToken);
        await _convites.SalvarAsync(cancellationToken);
        return new ResultadoGeracaoConvite(convite, tentativas);
    }

    /// <summary>
    /// Gera um código único entre os convites ativos (não usados, não
    /// expirados) — colisão com um convite finalizado (usado ou expirado)
    /// não conta como colisão, pois esse código pode ser reaproveitado (ver
    /// edge points de docs/specs/62-codigo-convite-curto/implementation.md).
    /// Devolve também quantas tentativas foram necessárias, usado pelo log
    /// estruturado <c>ConviteGerado</c> (Critérios técnicos da issue #62).
    /// </summary>
    private async Task<(string Codigo, int Tentativas)> GerarCodigoUnicoAsync(CancellationToken cancellationToken)
    {
        var tentativas = 0;
        string codigo;
        bool codigoAtivo;
        do
        {
            if (tentativas >= LimiteDeTentativasDeCodigo)
            {
                throw new LimiteDeTentativasDeCodigoConviteExcedidoException(LimiteDeTentativasDeCodigo);
            }

            codigo = _geradorDeCodigo.Gerar();
            tentativas++;
            codigoAtivo = await _convites.ExisteCodigoAtivoAsync(codigo, _clock.UtcNow, cancellationToken);
        }
        while (codigoAtivo);

        return (codigo, tentativas);
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
        return await AceitarResolvidoAsync(convite, nome, contatoBruto, rejeitarVinculoExistente: false, cancellationToken);
    }

    /// <summary>
    /// Aceita um convite pelo código curto de 5 dígitos (issue #63), mesma
    /// regra de negócio de <see cref="AceitarAsync"/>, mas rejeitando (em vez
    /// de reaproveitar silenciosamente) um vínculo já existente entre o
    /// contato e o Professor — critério de aceite 5 da issue #63, ver
    /// desenho em docs/specs/63-entrar-turma-codigo/implementation.md.
    /// </summary>
    public async Task<ResultadoAceiteConvite> AceitarPorCodigoAsync(
        string codigoBruto, string nome, string contatoBruto, CancellationToken cancellationToken)
    {
        var codigoNormalizado = NormalizarCodigo(codigoBruto);
        var convite = await _convites.BuscarPorCodigoAsync(codigoNormalizado, cancellationToken) ?? throw new ConviteInvalidoException();
        return await AceitarResolvidoAsync(convite, nome, contatoBruto, rejeitarVinculoExistente: true, cancellationToken);
    }

    /// <summary>
    /// Corpo comum de <see cref="AceitarAsync"/> (token) e
    /// <see cref="AceitarPorCodigoAsync"/> (código): valida o contato
    /// submetido contra o do convite, marca o convite como usado antes de
    /// qualquer mutação de Usuario/Matricula (uso único), e então
    /// cria/reaproveita a identidade e promove ou cria o vínculo.
    /// <paramref name="rejeitarVinculoExistente"/> distingue os dois fluxos:
    /// o fluxo por link reaproveita silenciosamente um vínculo já existente
    /// (decisão documentada em docs/specs/2-convite-whatsapp/implementation.md),
    /// enquanto o fluxo por código rejeita com
    /// <see cref="ContatoJaVinculadoException"/> (critério de aceite 5 da
    /// issue #63) — a checagem roda antes de <see cref="Convite.MarcarUsado"/>,
    /// preservando o edge point "rejeição não altera nada" já estabelecido
    /// para <see cref="ObterMatriculaOrigemValidaAsync"/> (achado de
    /// code-review do PR #29).
    /// </summary>
    private async Task<ResultadoAceiteConvite> AceitarResolvidoAsync(
        Convite convite, string nome, string contatoBruto, bool rejeitarVinculoExistente, CancellationToken cancellationToken)
    {
        var contatoNormalizado = Contato.Normalizar(contatoBruto);
        if (contatoNormalizado != convite.Contato)
        {
            throw new ConviteContatoDivergenteException();
        }

        var nomeValidado = NomeUsuario.Validar(nome);
        var matriculaOrigem = await ObterMatriculaOrigemValidaAsync(convite, cancellationToken);
        if (rejeitarVinculoExistente)
        {
            await GarantirContatoNaoVinculadoAsync(convite.ProfessorId, contatoNormalizado, cancellationToken);
        }

        convite.MarcarUsado(_clock);

        var (usuario, papelAdicionado) = await ObterOuCriarUsuarioAsync(nomeValidado, convite.Contato, cancellationToken);
        var matriculaPromovida = await VincularMatriculaAsync(convite, matriculaOrigem, usuario.Id, cancellationToken);

        await _usuarios.SalvarAsync(cancellationToken);
        await _matriculas.SalvarAsync(cancellationToken);
        await _convites.SalvarAsync(cancellationToken);
        return new ResultadoAceiteConvite(usuario, convite.Id, matriculaPromovida, papelAdicionado);
    }

    /// <summary>
    /// Normaliza o código informado pelo Aluno para só dígitos, tolerando
    /// espaços/máscara (ex: "12 345" ou "1-2-3-4-5" viram "12345") — mesmo
    /// racional de <see cref="Contato.Normalizar"/>. Validação de entrada do
    /// caller, não invariante de <see cref="Convite"/> (ver edge points de
    /// docs/specs/63-entrar-turma-codigo/implementation.md).
    /// </summary>
    private static string NormalizarCodigo(string codigoBruto)
    {
        return new string(codigoBruto.Where(char.IsDigit).ToArray());
    }

    private async Task<(Usuario Usuario, bool PapelAdicionado)> ObterOuCriarUsuarioAsync(
        string nomeValidado, string contatoNormalizado, CancellationToken cancellationToken)
    {
        var usuarioExistente = await _usuarios.BuscarPorContatoAsync(contatoNormalizado, cancellationToken);
        if (usuarioExistente is not null)
        {
            var jaEAluno = usuarioExistente.Papeis.Any(p => p.Papel == PapelUsuario.Aluno);
            var identificadorAluno = jaEAluno ? null : await _identificadorAluno.GerarUnicoAsync(cancellationToken);
            var papelAdicionado = AdicionarPapelAlunoIdempotente(usuarioExistente, identificadorAluno);
            return (usuarioExistente, papelAdicionado);
        }

        var identificador = await _identificadorAluno.GerarUnicoAsync(cancellationToken);
        var novoUsuario = Usuario.Cadastrar(nomeValidado, contatoNormalizado, PapelUsuario.Aluno, identificador, _clock);
        await _usuarios.AdicionarAsync(novoUsuario, cancellationToken);
        return (novoUsuario, false);
    }

    /// <summary>
    /// "Já é Aluno" não deveria impedir a promoção de uma Matricula de
    /// origem específica — decisão documentada em
    /// docs/specs/2-convite-whatsapp/implementation.md. Devolve se o papel
    /// foi de fato anexado (<c>true</c>) ou já estava presente, virando
    /// no-op (<c>false</c>) — usado pelo log estruturado
    /// <c>PapelAdicionado</c> (Critérios técnicos da issue #4).
    /// </summary>
    private bool AdicionarPapelAlunoIdempotente(Usuario usuario, string? identificadorAluno)
    {
        try
        {
            usuario.AdicionarPapel(PapelUsuario.Aluno, identificadorAluno, _clock);
            return true;
        }
        catch (PapelJaAtribuidoException)
        {
            return false;
        }
    }

    /// <summary>
    /// Busca e valida a matrícula de origem do convite, se houver, antes de
    /// qualquer mutação — sem isso, dois convites apontando para a mesma
    /// matrícula ainda não promovida (ex: reenvio acidental) faziam o
    /// segundo aceite lançar <see cref="MatriculaJaPromovidaException"/> sem
    /// tratamento (500) depois de já ter marcado aquele convite como usado
    /// (achado de code-review no PR #29). Validar aqui, antes de
    /// <see cref="Convite.MarcarUsado"/>, preserva o edge point "rejeição
    /// não altera nada".
    /// </summary>
    private async Task<Matricula?> ObterMatriculaOrigemValidaAsync(Convite convite, CancellationToken cancellationToken)
    {
        if (convite.MatriculaId is null)
        {
            return null;
        }

        var matriculaOrigem = await _matriculas.BuscarPorIdAsync(convite.MatriculaId.Value, cancellationToken);
        if (matriculaOrigem is null || matriculaOrigem.AlunoUsuarioId is not null)
        {
            throw new MatriculaOrigemInvalidaException(convite.MatriculaId.Value);
        }

        return matriculaOrigem;
    }

    /// <summary>
    /// Promove <paramref name="matriculaOrigem"/> ou o vínculo já existente
    /// (devolve <c>true</c>), ou cria uma nova matrícula já vinculada quando
    /// nenhuma das duas existir (devolve <c>false</c>) — usado pelo log
    /// estruturado <c>ConviteAceito</c> para indicar qual caminho ocorreu
    /// (Critérios técnicos da issue #2).
    /// </summary>
    private async Task<bool> VincularMatriculaAsync(
        Convite convite, Matricula? matriculaOrigem, Guid alunoUsuarioId, CancellationToken cancellationToken)
    {
        if (matriculaOrigem is not null)
        {
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
/// (criado ou reaproveitado), o <see cref="Convite.Id"/> aceito, se a
/// matrícula foi promovida (origem específica ou vínculo já existente) ou
/// criada nova — logado como <c>ConviteAceito</c> pela Api (Critérios
/// técnicos da issue #2) — e se o papel Aluno foi de fato anexado a uma
/// identidade já existente — logado como <c>PapelAdicionado</c> (Critérios
/// técnicos da issue #4).
/// </summary>
public sealed record ResultadoAceiteConvite(Usuario Usuario, Guid ConviteId, bool MatriculaPromovida, bool PapelAdicionado);

/// <summary>
/// Resultado da geração de convite: o <see cref="Convite"/> criado e quantas
/// <see cref="Tentativas"/> o gerador de código precisou até achar um código
/// livre entre os convites ativos — logado como <c>ConviteGerado</c> pela Api
/// (Critérios técnicos da issue #62).
/// </summary>
public sealed record ResultadoGeracaoConvite(Convite Convite, int Tentativas);
