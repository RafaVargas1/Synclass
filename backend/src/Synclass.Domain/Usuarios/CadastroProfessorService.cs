using Synclass.Domain.Common;

namespace Synclass.Domain.Usuarios;

/// <summary>
/// Orquestra o cadastro de Professor: cria ou reaproveita a identidade de
/// usuário única por contato e associa a ela o papel Professor (issue #1).
/// Nunca cria um registro duplicado se a pessoa já existir (ex: como Aluno),
/// e nunca duplica o papel Professor se a pessoa já for Professor.
/// </summary>
public sealed class CadastroProfessorService
{
    private readonly IUsuarioRepository _usuarios;
    private readonly IClock _clock;

    public CadastroProfessorService(IUsuarioRepository usuarios, IClock clock)
    {
        _usuarios = usuarios;
        _clock = clock;
    }

    public async Task<ResultadoCadastroProfessor> CadastrarProfessorAsync(string nome, string contatoBruto, CancellationToken cancellationToken)
    {
        var nomeValidado = NomeUsuario.Validar(nome);
        var contatoNormalizado = Contato.Normalizar(contatoBruto);
        var usuarioExistente = await _usuarios.BuscarPorContatoAsync(contatoNormalizado, cancellationToken);

        var usuario = await ObterOuCriarUsuarioAsync(usuarioExistente, nomeValidado, contatoNormalizado, cancellationToken);
        await _usuarios.SalvarAsync(cancellationToken);
        return new ResultadoCadastroProfessor(usuario, usuarioExistente is not null);
    }

    private async Task<Usuario> ObterOuCriarUsuarioAsync(
        Usuario? usuarioExistente,
        string nomeValidado,
        string contatoNormalizado,
        CancellationToken cancellationToken)
    {
        if (usuarioExistente is not null)
        {
            usuarioExistente.AdicionarPapel(PapelUsuario.Professor, _clock);
            return usuarioExistente;
        }

        var novoUsuario = Usuario.Cadastrar(nomeValidado, contatoNormalizado, PapelUsuario.Professor, _clock);
        await _usuarios.AdicionarAsync(novoUsuario, cancellationToken);
        return novoUsuario;
    }
}

/// <summary>
/// Resultado do cadastro de Professor: o <see cref="Usuario"/> resultante
/// (criado ou reaproveitado) e se o papel Professor foi anexado a uma
/// identidade já existente (<c>true</c>) ou a um usuário recém-criado
/// (<c>false</c>) — logado como <c>PapelAdicionado</c> pela Api quando
/// <c>true</c> (Critérios técnicos da issue #4).
/// </summary>
public sealed record ResultadoCadastroProfessor(Usuario Usuario, bool UsuarioReaproveitado);
