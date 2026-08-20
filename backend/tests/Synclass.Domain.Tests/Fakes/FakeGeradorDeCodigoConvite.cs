using Synclass.Domain.Convites;

namespace Synclass.Domain.Tests.Fakes;

/// <summary>
/// Gera códigos sequenciais e previsíveis a partir de uma fila configurada
/// (ou "00001", "00002", ... por padrão), no lugar de aleatoriedade real —
/// permite aos testes de <see cref="ConviteService"/> tanto referenciar o
/// código gerado quanto simular colisão (fila com códigos repetidos).
/// </summary>
public sealed class FakeGeradorDeCodigoConvite : IGeradorDeCodigoConvite
{
    private readonly Queue<string>? _fila;
    private int _contador;

    public FakeGeradorDeCodigoConvite()
    {
    }

    public FakeGeradorDeCodigoConvite(params string[] codigos)
    {
        _fila = new Queue<string>(codigos);
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
        return _contador.ToString("D5");
    }
}
