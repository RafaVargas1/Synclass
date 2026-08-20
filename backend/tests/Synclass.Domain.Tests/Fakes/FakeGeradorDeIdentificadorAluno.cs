using Synclass.Domain.Alunos;

namespace Synclass.Domain.Tests.Fakes;

/// <summary>
/// Gera identificadores sequenciais e previsíveis a partir de uma fila
/// configurada (ou <c>ALU-0001</c>, <c>ALU-0002</c>, ... por padrão), no
/// lugar de aleatoriedade real — permite aos testes de
/// <see cref="IdentificadorAlunoService"/> tanto referenciar o
/// identificador gerado quanto simular colisão (fila com identificadores
/// repetidos). Não valida o alfabeto da regra real (issue #70): a forma é
/// responsabilidade de <see cref="GeradorDeIdentificadorAluno"/>, testada à
/// parte.
/// </summary>
public sealed class FakeGeradorDeIdentificadorAluno : IGeradorDeIdentificadorAluno
{
    private readonly Queue<string>? _fila;
    private int _contador;

    public FakeGeradorDeIdentificadorAluno()
    {
    }

    public FakeGeradorDeIdentificadorAluno(params string[] identificadores)
    {
        _fila = new Queue<string>(identificadores);
    }

    public int VezesChamado { get; private set; }

    public string Gerar()
    {
        VezesChamado++;
        if (_fila is not null && _fila.Count > 0)
        {
            return _fila.Dequeue();
        }

        _contador++;
        return $"ALU-{_contador:D4}";
    }
}
