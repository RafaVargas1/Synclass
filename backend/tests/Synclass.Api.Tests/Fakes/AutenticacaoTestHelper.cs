using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Synclass.Domain.Autenticacao;
using Synclass.Domain.Common;
using Synclass.Domain.Usuarios;
using Synclass.Infrastructure.Persistence;

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
        // Assinante de teste (issue #4/#23): o IdentificadorAluno do papel
        // Aluno (issue #70) não participa de nenhum contrato validado por
        // estes testes — a autorização só checa a claim `role` do token.
        var usuarioAssinante = Usuario.Cadastrar("Usuário de Teste", $"{Guid.NewGuid()}@teste.exemplo", papeis[0], null, clock);
        foreach (var papel in papeis.Skip(1))
        {
            usuarioAssinante.AdicionarPapel(papel, null, clock);
        }

        return gerador.Gerar(usuarioAssinante);
    }

    /// <summary>
    /// Variante persistida (issue #23): endpoints que passaram a derivar
    /// <c>professorId</c>/<c>alunoUsuarioId</c> do token (em vez de um
    /// parâmetro de rota) precisam que o <see cref="Usuario"/> assinante
    /// exista de fato no banco — diferente de <see cref="ClienteAutenticado"/>,
    /// usado pelos testes de autorização por papel (issue #4) que nunca
    /// checavam a identidade contra o banco, só a claim <c>role</c>.
    /// Devolve o <see cref="Usuario.Id"/> junto do cliente para o teste
    /// poder montar o resto do cenário (Matrícula vinculada, etc).
    /// </summary>
    public static async Task<(HttpClient Client, Guid UsuarioId)> ClienteAutenticadoComoProfessorPersistidoAsync(
        WebApplicationFactory<Program> factory)
    {
        return await ClienteAutenticadoPersistidoAsync(factory, PapelUsuario.Professor);
    }

    public static async Task<(HttpClient Client, Guid UsuarioId)> ClienteAutenticadoComoAlunoPersistidoAsync(
        WebApplicationFactory<Program> factory)
    {
        return await ClienteAutenticadoPersistidoAsync(factory, PapelUsuario.Aluno);
    }

    private static async Task<(HttpClient, Guid)> ClienteAutenticadoPersistidoAsync(
        WebApplicationFactory<Program> factory, PapelUsuario papel)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SynclassDbContext>();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();
        var gerador = scope.ServiceProvider.GetRequiredService<IGeradorDeTokenSessao>();

        // Assinante de teste (issue #4/#23): o IdentificadorAluno do papel
        // Aluno (issue #70) não participa de nenhum contrato validado por
        // estes testes — a autorização só checa a claim `role` do token.
        var usuario = Usuario.Cadastrar("Usuário de Teste", $"{Guid.NewGuid()}@teste.exemplo", papel, null, clock);
        dbContext.Usuarios.Add(usuario);
        await dbContext.SaveChangesAsync();

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", gerador.Gerar(usuario));
        return (client, usuario.Id);
    }
}
