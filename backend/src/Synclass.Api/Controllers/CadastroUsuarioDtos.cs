using Microsoft.Extensions.Logging;
using Synclass.Domain.Usuarios;

namespace Synclass.Api.Controllers;

/// <summary>
/// DTOs e logging compartilhados por <see cref="ProfessoresController"/> e
/// <see cref="AlunosController"/> (issue #61) — o corpo de request/response
/// e o formato de log são idênticos entre os dois cadastros, só o
/// <see cref="PapelUsuario"/> muda.
/// </summary>
public sealed record CadastroUsuarioRequest(string Nome, string Contato);

public sealed record CadastroUsuarioResponse(Guid UsuarioId, string Nome);

public sealed record CadastroUsuarioErrorResponse(string Mensagem);

public sealed record VerificarContatoResponse(bool IdentidadeExistente, string? Nome);

/// <summary>
/// Formato de log compartilhado do cadastro de usuário (sucesso e
/// rejeição) — extraído para não duplicar entre <see cref="ProfessoresController"/>
/// e <see cref="AlunosController"/>, que só diferem no <see cref="PapelUsuario"/>
/// atribuído (já carregado em <see cref="ResultadoCadastroUsuario"/>).
/// </summary>
public static class CadastroUsuarioLogging
{
    public static void LogCadastroSucesso(ILogger logger, string trackId, ResultadoCadastroUsuario resultado)
    {
        if (resultado.UsuarioReaproveitado)
        {
            logger.LogInformation(
                "PapelAdicionado {TrackId} {UsuarioId} {Papel}", trackId, resultado.Usuario.Id, resultado.Papel);
            return;
        }

        logger.LogInformation(
            "UsuarioCadastrado {TrackId} {UsuarioId} {Papel}", trackId, resultado.Usuario.Id, resultado.Papel);
    }

    public static void LogCadastroRejeitado(ILogger logger, string trackId, string? contatoBruto, CadastroRejeitadoException ex)
    {
        var contatoMascarado = MascaradorDeContato.Mascarar(contatoBruto ?? string.Empty);
        logger.LogWarning(
            "CadastroRejeitado {TrackId} {Motivo} {ContatoMascarado}", trackId, ex.GetType().Name, contatoMascarado);
    }
}
