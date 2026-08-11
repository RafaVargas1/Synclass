namespace Synclass.Domain.Usuarios;

/// <summary>
/// Mascara um contato para uso em logs estruturados: o contato é dado
/// pessoal e nunca deve ser gravado em texto pleno (ver Critérios técnicos
/// da issue #1). Mantém os 2 primeiros e 2 últimos caracteres, substituindo
/// o restante por asteriscos — suficiente para correlacionar entradas de log
/// do mesmo contato sem expor o valor real.
/// </summary>
public static class MascaradorDeContato
{
    private const int CaracteresVisiveis = 2;

    public static string Mascarar(string contato)
    {
        if (contato.Length <= CaracteresVisiveis * 2)
        {
            return new string('*', contato.Length);
        }

        var inicio = contato[..CaracteresVisiveis];
        var fim = contato[^CaracteresVisiveis..];
        var meio = new string('*', contato.Length - CaracteresVisiveis * 2);
        return $"{inicio}{meio}{fim}";
    }
}
