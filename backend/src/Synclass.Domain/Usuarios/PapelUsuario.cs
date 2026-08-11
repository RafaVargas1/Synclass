namespace Synclass.Domain.Usuarios;

/// <summary>
/// Papel que um usuário pode acumular na plataforma (ver
/// docs/backlog/requisitos-funcionais.md, item 4 — um usuário pode acumular
/// os papéis de Professor e Aluno simultaneamente).
/// </summary>
public enum PapelUsuario
{
    Professor,
    Aluno,
}
