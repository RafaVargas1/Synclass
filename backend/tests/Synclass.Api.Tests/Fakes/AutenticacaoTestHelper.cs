using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Synclass.Domain.Autenticacao;
using Synclass.Domain.Common;
using Synclass.Domain.Usuarios;

namespace Synclass.Api.Tests.Fakes;

/// <summary>
/// Gera um <see cref="HttpClient"/> já autenticado (header <c>Authorization:
/// Bearer</c>) para os testes de fumaça que exercitam endpoints protegidos
/// por <c>[Authorize(Roles = ...)]</c> (issue #4). Assina o token com o
/// mesmo <see cref="IGeradorDeTokenSessao"/> registrado na Api — não
/// persiste o <see cref="Usuario"/> assinante, já que a autorização por
/// papel (edge point de <c>docs/specs/4-usuario-acumula-papeis</c>) não
/// checa a identidade contra o banco, só a claim <c>role</c> do token.
/// </summary>
public static class AutenticacaoTestHelper
{
    public static HttpClient ClienteAutenticadoComoProfessor(WebApplicationFactory<Program> factory)
    {
        return ClienteAutenticado(factory, PapelUsuario.Professor);
    }

    public static HttpClient ClienteAutenticadoComoAluno(WebApplicationFactory<Program> factory)
    {
        return ClienteAutenticado(factory, PapelUsuario.Aluno);
    }

    private static HttpClient ClienteAutenticado(WebApplicationFactory<Program> factory, params PapelUsuario[] papeis)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GerarToken(factory, papeis));
        return client;
    }

    private static string GerarToken(WebApplicationFactory<Program> factory, params PapelUsuario[] papeis)
    {
        using var scope = factory.Services.CreateScope();
        var gerador = scope.ServiceProvider.GetRequiredService<IGeradorDeTokenSessao>();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();
        var usuarioAssinante = Usuario.Cadastrar("Usuário de Teste", $"{Guid.NewGuid()}@teste.exemplo", papeis[0], clock);
        foreach (var papel in papeis.Skip(1))
        {
            usuarioAssinante.AdicionarPapel(papel, clock);
        }

        return gerador.Gerar(usuarioAssinante);
    }
}
