using Synclass.Domain.Common;
using Synclass.Domain.Usuarios;

namespace Synclass.Domain.Convites;

/// <summary>
/// Convite direcionado de um Professor a um Aluno, aceito via link único
/// (issue #2). Uso único: <see cref="MarcarUsado"/> rejeita reuso ou uso
/// após <see cref="ExpiraEm"/>, antes de qualquer mutação de
/// Usuario/Matricula (ver edge points de
/// docs/specs/2-convite-whatsapp/implementation.md).
/// </summary>
public sealed class Convite
{
    private Convite(
        Guid id,
        Guid professorId,
        string contato,
        TipoContato contatoTipo,
        Guid? matriculaId,
        string token,
        string codigo,
        DateTimeOffset expiraEm,
        DateTimeOffset createdAt)
    {
        Id = id;
        ProfessorId = professorId;
        Contato = contato;
        ContatoTipo = contatoTipo;
        MatriculaId = matriculaId;
        Token = token;
        Codigo = codigo;
        ExpiraEm = expiraEm;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public Guid ProfessorId { get; private set; }

    public string Contato { get; private set; }

    public TipoContato ContatoTipo { get; private set; }

    /// <summary>
    /// Matrícula provisória de origem, quando o convite foi gerado a partir
    /// de um Aluno provisório específico (item 3 do backlog). Nula quando o
    /// convite não parte de nenhuma matrícula existente.
    /// </summary>
    public Guid? MatriculaId { get; private set; }

    public string Token { get; private set; }

    /// <summary>
    /// Código curto de 5 dígitos numéricos (issue #62), alternativa ao link
    /// para o Aluno entrar na turma digitando o código em vez de abrir a URL
    /// — já validado/gerado por <see cref="IGeradorDeCodigoConvite"/>, mesmo
    /// racional de <see cref="Token"/>.
    /// </summary>
    public string Codigo { get; private set; }

    public DateTimeOffset ExpiraEm { get; private set; }

    public DateTimeOffset? UsadoEm { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// Cria um convite válido por <paramref name="diasValidade"/> dias a
    /// partir de agora (critério de aceite 1 da issue #2). O
    /// <paramref name="token"/> já validado/gerado por
    /// <see cref="IGeradorDeTokenConvite"/> — esta factory não gera
    /// aleatoriedade, só monta a entidade.
    /// </summary>
    public static Convite Gerar(
        Guid professorId,
        string contatoNormalizado,
        TipoContato contatoTipo,
        Guid? matriculaId,
        string token,
        string codigo,
        int diasValidade,
        IClock clock)
    {
        var agora = clock.UtcNow;
        return new Convite(
            Guid.NewGuid(), professorId, contatoNormalizado, contatoTipo, matriculaId, token, codigo, agora.AddDays(diasValidade), agora);
    }

    /// <summary>
    /// Marca o convite como usado no aceite. Rejeita reuso
    /// (<see cref="ConviteInvalidoException"/>) e uso após a expiração
    /// (<see cref="ConviteExpiradoException"/>) antes de definir
    /// <see cref="UsadoEm"/> — uso único, critério de aceite 3.
    /// </summary>
    public void MarcarUsado(IClock clock)
    {
        if (UsadoEm is not null)
        {
            throw new ConviteInvalidoException();
        }

        if (clock.UtcNow > ExpiraEm)
        {
            throw new ConviteExpiradoException();
        }

        UsadoEm = clock.UtcNow;
    }
}
