using Synclass.Domain.Common;

namespace Synclass.Domain.Usuarios;

/// <summary>
/// Identidade de usuário única por contato (e-mail ou telefone). Um mesmo
/// Usuario pode acumular múltiplos <see cref="PapelUsuario"/> (Professor e
/// Aluno) — ver Regra de Negócio da issue #1 e
/// docs/backlog/requisitos-funcionais.md, item 4.
/// </summary>
public sealed class Usuario
{
    private readonly List<PapelAtribuido> _papeis = new();

    private Usuario(Guid id, string nome, string contato, string? identificadorAluno, DateTimeOffset createdAt)
    {
        Id = id;
        Nome = nome;
        Contato = contato;
        IdentificadorAluno = identificadorAluno;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public string Nome { get; private set; }

    public string Contato { get; private set; }

    /// <summary>
    /// Identificador único e human-readable do papel Aluno desta identidade
    /// (issue #70, formato <c>ALU-XXXX</c>), gerado uma única vez quando o
    /// papel Aluno é anexado e imutável depois — nulo enquanto o usuário não
    /// for Aluno. Sem setter público: só gravado em
    /// <see cref="Cadastrar"/> (quando o papel nasce Aluno) e em
    /// <see cref="AdicionarPapel"/> (na primeira vez que o papel Aluno é
    /// anexado a uma identidade existente).
    /// </summary>
    public string? IdentificadorAluno { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public IReadOnlyCollection<PapelAtribuido> Papeis => _papeis.AsReadOnly();

    /// <summary>
    /// Cria uma nova identidade de usuário com o papel informado. Chamado
    /// apenas quando nenhum usuário existe para o contato normalizado — ver
    /// <see cref="AdicionarPapel"/> para o caso de reaproveitamento.
    /// <paramref name="identificadorAluno"/> é gravado quando
    /// <paramref name="papel"/> é <see cref="PapelUsuario.Aluno"/> e ignorado
    /// quando Professor (issue #70).
    /// </summary>
    public static Usuario Cadastrar(
        string nomeValidado, string contatoNormalizado, PapelUsuario papel, string? identificadorAluno, IClock clock)
    {
        var usuario = new Usuario(
            Guid.NewGuid(),
            nomeValidado,
            contatoNormalizado,
            papel == PapelUsuario.Aluno ? identificadorAluno : null,
            clock.UtcNow);
        usuario._papeis.Add(PapelAtribuido.Criar(usuario.Id, papel, clock));
        return usuario;
    }

    /// <summary>
    /// Adiciona um novo papel a este usuário, reaproveitando a identidade
    /// existente. Rejeita (em vez de ignorar silenciosamente) se o usuário
    /// já possuir o papel, para não mascarar um possível bug de reenvio.
    /// <paramref name="identificadorAluno"/> é gravado quando
    /// <paramref name="papel"/> é <see cref="PapelUsuario.Aluno"/> e ignorado
    /// quando Professor (issue #70).
    /// </summary>
    public void AdicionarPapel(PapelUsuario papel, string? identificadorAluno, IClock clock)
    {
        if (_papeis.Any(p => p.Papel == papel))
        {
            throw new PapelJaAtribuidoException(papel);
        }

        if (papel == PapelUsuario.Aluno)
        {
            IdentificadorAluno = identificadorAluno;
        }

        _papeis.Add(PapelAtribuido.Criar(Id, papel, clock));
    }

    /// <summary>
    /// Corrige o próprio nome (issue #27) — canal explícito para quem
    /// cadastrou o nome errado, já que um segundo cadastro com o mesmo
    /// contato não sobrescreve o nome existente (RN da issue #20). Valida
    /// internamente (diferente de <see cref="Cadastrar"/>, que recebe o nome
    /// já validado pelo chamador): este é o único ponto de mutação de
    /// <see cref="Nome"/> depois da criação, então a entidade protege o
    /// próprio invariante aqui em vez de depender do chamador validar antes.
    /// </summary>
    public void AtualizarNome(string nomeBruto)
    {
        Nome = NomeUsuario.Validar(nomeBruto);
    }
}
