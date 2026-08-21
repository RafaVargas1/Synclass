using Synclass.Domain.Alunos;
using Synclass.Domain.Common;

namespace Synclass.Domain.Usuarios;

/// <summary>
/// Orquestra o cadastro de usuário por papel: cria ou reaproveita a
/// identidade de usuário única por contato e associa a ela o
/// <see cref="PapelUsuario"/> informado — usado tanto pelo cadastro de
/// Professor (issue #1) quanto pelo de Aluno (issue #61), que diferem só no
/// papel atribuído (mesma Regra de Negócio das duas issues). Nunca cria um
/// registro duplicado se a pessoa já existir (ex: como Aluno cadastrando-se
/// também como Professor), e nunca duplica o mesmo papel se a pessoa já o
/// possuir.
/// </summary>
public sealed class CadastroUsuarioService
{
    private readonly IUsuarioRepository _usuarios;
    private readonly IClock _clock;
    private readonly IdentificadorAlunoService _identificadorAluno;

    public CadastroUsuarioService(IUsuarioRepository usuarios, IClock clock, IdentificadorAlunoService identificadorAluno)
    {
        _usuarios = usuarios;
        _clock = clock;
        _identificadorAluno = identificadorAluno;
    }

    public async Task<ResultadoCadastroUsuario> CadastrarAsync(
        PapelUsuario papel, string nome, string contatoBruto, CancellationToken cancellationToken)
    {
        var nomeValidado = NomeUsuario.Validar(nome);
        var contatoNormalizado = Contato.Normalizar(contatoBruto);
        var usuarioExistente = await _usuarios.BuscarPorContatoAsync(contatoNormalizado, cancellationToken);

        var usuario = await ObterOuCriarUsuarioAsync(usuarioExistente, papel, nomeValidado, contatoNormalizado, cancellationToken);
        await _usuarios.SalvarAsync(cancellationToken);
        return new ResultadoCadastroUsuario(usuario, papel, usuarioExistente is not null);
    }

    /// <summary>
    /// Verifica se um contato já pertence a uma identidade existente
    /// (issue #27) — alimenta o campo Nome readonly do formulário de
    /// cadastro (Professor ou Aluno) sem exigir sessão. Fail-open: um
    /// contato ainda incompleto ou inválido enquanto o usuário digita
    /// devolve "não existe" silenciosamente, nunca rejeita.
    /// </summary>
    public async Task<VerificacaoContato> VerificarContatoAsync(string contatoBruto, CancellationToken cancellationToken)
    {
        Usuario? usuarioExistente;
        try
        {
            var contatoNormalizado = Contato.Normalizar(contatoBruto ?? string.Empty);
            usuarioExistente = await _usuarios.BuscarPorContatoAsync(contatoNormalizado, cancellationToken);
        }
        catch (ContatoInvalidoException)
        {
            usuarioExistente = null;
        }

        return usuarioExistente is null
            ? new VerificacaoContato(false, null)
            : new VerificacaoContato(true, usuarioExistente.Nome);
    }

    private async Task<Usuario> ObterOuCriarUsuarioAsync(
        Usuario? usuarioExistente,
        PapelUsuario papel,
        string nomeValidado,
        string contatoNormalizado,
        CancellationToken cancellationToken)
    {
        var identificadorAluno = await GerarIdentificadorSeNecessarioAsync(papel, usuarioExistente, cancellationToken);
        if (usuarioExistente is not null)
        {
            usuarioExistente.AdicionarPapel(papel, identificadorAluno, _clock);
            return usuarioExistente;
        }

        var novoUsuario = Usuario.Cadastrar(nomeValidado, contatoNormalizado, papel, identificadorAluno, _clock);
        await _usuarios.AdicionarAsync(novoUsuario, cancellationToken);
        return novoUsuario;
    }

    /// <summary>
    /// Gera um <c>IdentificadorAluno</c> único (issue #70) apenas quando o
    /// papel Aluno é de fato anexado neste cadastro — ou seja, papel é Aluno
    /// E (novo usuário, ou usuário existente sem o papel Aluno ainda). Se o
    /// usuário existente já for Aluno, <see cref="Usuario.AdicionarPapel"/>
    /// lançará <see cref="PapelJaAtribuidoException"/> e nenhum identificador
    /// é gerado nem gravado. Professor nunca gera identificador.
    /// </summary>
    private async Task<string?> GerarIdentificadorSeNecessarioAsync(
        PapelUsuario papel, Usuario? usuarioExistente, CancellationToken cancellationToken)
    {
        if (papel != PapelUsuario.Aluno)
        {
            return null;
        }

        if (usuarioExistente is not null && usuarioExistente.Papeis.Any(p => p.Papel == PapelUsuario.Aluno))
        {
            return null;
        }

        return await _identificadorAluno.GerarUnicoAsync(cancellationToken);
    }
}

/// <summary>
/// Resultado do cadastro de usuário: o <see cref="Usuario"/> resultante
/// (criado ou reaproveitado), o <see cref="PapelUsuario"/> atribuído nesta
/// chamada e se ele foi anexado a uma identidade já existente (<c>true</c>)
/// ou a um usuário recém-criado (<c>false</c>) — logado como
/// <c>PapelAdicionado</c> pela Api quando <c>true</c> (Critérios técnicos
/// da issue #4).
/// </summary>
public sealed record ResultadoCadastroUsuario(Usuario Usuario, PapelUsuario Papel, bool UsuarioReaproveitado);

/// <summary>
/// Resultado de <see cref="CadastroUsuarioService.VerificarContatoAsync"/>:
/// se o contato já pertence a uma identidade existente e, se sim, o nome já
/// cadastrado.
/// </summary>
public sealed record VerificacaoContato(bool IdentidadeExistente, string? Nome);
