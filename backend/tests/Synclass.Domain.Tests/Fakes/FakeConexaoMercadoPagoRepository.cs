using Synclass.Domain.Pagamentos;

namespace Synclass.Domain.Tests.Fakes;

/// <summary>
/// Repositório em memória de <see cref="ConexaoMercadoPago"/> usado nos
/// testes de unidade do Domain, no lugar de um banco real (ver
/// docs/spec/code-style.md#testes). Expoe a lista de conexões para o teste
/// poder inspecionar o que foi persistido sem consultar o banco.
/// </summary>
public sealed class FakeConexaoMercadoPagoRepository : IConexaoMercadoPagoRepository
{
    public List<ConexaoMercadoPago> Conexoes { get; } = new();

    public Task<ConexaoMercadoPago?> ObterPorProfessorAsync(Guid professorId, CancellationToken cancellationToken)
    {
        var conexao = Conexoes.FirstOrDefault(c => c.ProfessorId == professorId);
        return Task.FromResult(conexao);
    }

    public Task<ConexaoMercadoPago?> ObterPorStateAsync(string state, CancellationToken cancellationToken)
    {
        var conexao = Conexoes.FirstOrDefault(c => c.State == state);
        return Task.FromResult(conexao);
    }

    public Task AdicionarAsync(ConexaoMercadoPago conexao, CancellationToken cancellationToken)
    {
        Conexoes.Add(conexao);
        return Task.CompletedTask;
    }

    public Task AtualizarAsync(ConexaoMercadoPago conexao, CancellationToken cancellationToken)
    {
        // O objeto de memória já é o mesmo da lista — nada a fazer além de
        // garantir que ele está presente (substitui por si mesmo).
        var indice = Conexoes.FindIndex(c => c.Id == conexao.Id);
        if (indice >= 0)
        {
            Conexoes[indice] = conexao;
        }

        return Task.CompletedTask;
    }

    public Task RemoverAsync(ConexaoMercadoPago conexao, CancellationToken cancellationToken)
    {
        Conexoes.RemoveAll(c => c.Id == conexao.Id);
        return Task.CompletedTask;
    }
}
