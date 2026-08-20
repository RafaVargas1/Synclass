namespace Synclass.Domain.Usuarios;

/// <summary>
/// Lançada quando a persistência rejeita o cadastro por violação do índice
/// único (Contato, ou UsuarioId+Papel) — a checagem de duplicidade da
/// aplicação (<see cref="CadastroUsuarioService"/>) já valida isso antes
/// de salvar, mas duas requisições concorrentes para o mesmo contato podem
/// passar por essa checagem antes de qualquer uma delas confirmar a escrita
/// (race condition). Nesse caso, o índice único do banco é a última linha
/// de defesa contra duplicidade — esta exceção traduz essa rejeição de
/// infraestrutura para o mesmo contrato das demais rejeições de cadastro.
/// </summary>
public sealed class CadastroConcorrenteException : CadastroRejeitadoException
{
    public CadastroConcorrenteException(Exception causaRaiz)
        : base(
            "Não foi possível concluir o cadastro devido a uma tentativa concorrente com o mesmo contato. Tente novamente.",
            causaRaiz)
    {
    }
}
