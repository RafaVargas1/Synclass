using System.Security.Cryptography;
using Synclass.Domain.Alunos;

namespace Synclass.Infrastructure.Alunos;

/// <summary>
/// Gera um identificador de Aluno no formato <c>ALU-XXXX</c> usando uma
/// fonte de aleatoriedade criptograficamente segura — mesma técnica de
/// <see cref="Synclass.Infrastructure.Convites.GeradorDeCodigoConvite"/>.
/// O alfabeto exclui caracteres ambíguos (<c>0</c>, <c>O</c>, <c>1</c>,
/// <c>I</c>, <c>L</c>) para ser legível/digitável por humanos (issue #70 —
/// ver docs/specs/70-identificador-aluno/implementation.md).
/// </summary>
public sealed class GeradorDeIdentificadorAluno : IGeradorDeIdentificadorAluno
{
    // 31 símbolos, sem 0/O/1/I/L — mesmo racional de "pensado para
    // leitura/digitação humana" da issue #70.
    private const string Alfabeto = "23456789ABCDEFGHJKMNPQRSTUVWXYZ";
    private const string Prefixo = "ALU-";
    private const int TamanhoSufixo = 4;

    /// <summary>
    /// Comprimento total do identificador no formato <c>ALU-XXXX</c> —
    /// usado pelos mapeamentos EF Core (<c>UsuarioConfiguration</c>,
    /// <c>MatriculaConfiguration</c>) para dimensionar a coluna.
    /// </summary>
    public const int TamanhoIdentificador = TamanhoSufixo + 4; // "ALU-" são 4 caracteres

    public string Gerar()
    {
        var sufixo = new char[TamanhoSufixo];
        for (var i = 0; i < TamanhoSufixo; i++)
        {
            sufixo[i] = Alfabeto[RandomNumberGenerator.GetInt32(0, Alfabeto.Length)];
        }

        return Prefixo + new string(sufixo);
    }
}
