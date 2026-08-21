# Implementação: Aluno usa o código de convite (#63)

## Entidades/classes afetadas

- `Synclass.Domain.Convites.IConviteRepository` — novo método
  `BuscarPorCodigoAsync(string codigo, CancellationToken ct)`.
- `Synclass.Infrastructure.Persistence.ConviteRepository` — implementa o
  método acima.
- `Synclass.Domain.Tests.Fakes.FakeConviteRepository` — implementa o método
  acima em memória.
- `Synclass.Domain.Convites.ConviteService` — novo método público
  `AceitarPorCodigoAsync`; `AceitarAsync` (token) e o novo método passam a
  compartilhar um método privado `AceitarResolvidoAsync` (refactor do corpo
  atual de `AceitarAsync`, ver abaixo). Nenhuma mudança de assinatura em
  `AceitarAsync`.
- `Synclass.Api.Controllers.ConvitesController` — nova action `AceitarPorCodigo`
  em `POST /convites/codigo/{codigo}/aceite`, reaproveitando
  `AceitarConviteRequest`/`AceitarConviteResponse` já existentes.
- Frontend: `lib/api/convites.ts` (`aceitarConvitePorCodigo`),
  `app/aluno/entrar-turma.tsx` (tela nova).

Nenhuma migration: `Codigo` já existe em `Convite` desde a issue #62.

## Contrato de API

`POST /convites/codigo/{codigo}/aceite` — `AllowAnonymous`, mesmo padrão de
`POST /convites/{token}/aceite`:

- Request body: `{ "nome": string, "contato": string }` (reaproveita
  `AceitarConviteRequest`).
- `codigo` normalizado pelo Domain antes da busca (só dígitos — tolera
  espaços/máscara vindos da rota, ver Edge points).
- Response 200: `{ "usuarioId": guid, "nome": string, "papeis": string[] }`
  (reaproveita `AceitarConviteResponse`).
- Response 400: `{ "mensagem": string }` — `ConviteInvalidoException`
  (código inexistente/já usado), `ConviteExpiradoException`,
  `ConviteContatoDivergenteException`, `ContatoJaVinculadoException`,
  `NomeInvalidoException`, `ContatoInvalidoException`.

## Desenho: `AceitarResolvidoAsync`

`AceitarAsync` (token) e `AceitarPorCodigoAsync` (código) só divergem em (a)
como o `Convite` é localizado e (b) se um vínculo já existente entre o
contato e o Professor deve ser silenciosamente reaproveitado (comportamento
atual do fluxo por link — decisão documentada em
`docs/specs/2-convite-whatsapp/implementation.md`) ou rejeitado (novo
critério de aceite 5 da issue #63 — o Aluno já vinculado que reenvia o
código de novo deve ver uma mensagem clara, não um vínculo duplicado
silencioso). Extraia:

```csharp
private async Task<ResultadoAceiteConvite> AceitarResolvidoAsync(
    Convite convite, string nome, string contatoBruto, bool rejeitarVinculoExistente, CancellationToken ct)
{
    var contatoNormalizado = Contato.Normalizar(contatoBruto);
    if (contatoNormalizado != convite.Contato)
        throw new ConviteContatoDivergenteException();

    var nomeValidado = NomeUsuario.Validar(nome);
    var matriculaOrigem = await ObterMatriculaOrigemValidaAsync(convite, ct);
    if (rejeitarVinculoExistente)
        await GarantirContatoNaoVinculadoAsync(convite.ProfessorId, contatoNormalizado, ct);

    convite.MarcarUsado(_clock);
    var (usuario, papelAdicionado) = await ObterOuCriarUsuarioAsync(nomeValidado, convite.Contato, ct);
    var matriculaPromovida = await VincularMatriculaAsync(convite, matriculaOrigem, usuario.Id, ct);

    await _usuarios.SalvarAsync(ct);
    await _matriculas.SalvarAsync(ct);
    await _convites.SalvarAsync(ct);
    return new ResultadoAceiteConvite(usuario, convite.Id, matriculaPromovida, papelAdicionado);
}
```

`AceitarAsync` vira: busca por token, chama
`AceitarResolvidoAsync(convite, nome, contatoBruto, rejeitarVinculoExistente: false, ct)`.
`AceitarPorCodigoAsync` vira: normaliza código, busca por código, chama
`AceitarResolvidoAsync(convite, nome, contatoBruto, rejeitarVinculoExistente: true, ct)`.

`GarantirContatoNaoVinculadoAsync` já existe (hoje só usado por `GerarAsync`)
e já lança `ContatoJaVinculadoException` na condição exata do critério 5 —
reaproveite tal como está, não duplique a checagem.

A checagem de vínculo roda **antes** de `convite.MarcarUsado`, preservando o
mesmo edge point já estabelecido para `matriculaOrigem` ("rejeição não muda
nada" — achado de code-review do PR #29, ver comentário em
`ObterMatriculaOrigemValidaAsync`): um código ainda ativo que foi rejeitado
por esse motivo continua utilizável por outra tentativa legítima.

## Modelo de dados

Nenhum. `Convite.Codigo` já existe (issue #62); nenhuma coluna/tabela nova.

## Edge points

- **Normalização do código**: aceitar apenas dígitos, descartando qualquer
  outro caractere (espaço, hífen) antes de comparar — mesmo racional do
  `Contato.Normalizar` já usado no projeto. Implementado como método privado
  em `ConviteService` (`NormalizarCodigo`), não em `Convite` (é validação de
  entrada do caller, não invariante da entidade).
- **Colisão de código entre convites finalizados e um convite ativo mais
  novo**: como o código só é único entre convites *ativos* (issue #62), um
  código pode, em teoria, ter sido usado por um convite já finalizado e
  reaproveitado por um convite novo. `BuscarPorCodigoAsync` deve devolver o
  mais recente (`OrderByDescending(CreatedAt)`) para sempre resolver o
  convite ativo quando houver um — evita que um convite finalizado antigo
  "esconda" um convite ativo mais novo com o mesmo código.
- **Rota reaproveita `{codigo}` como segmento de URL**: o frontend já
  normaliza (só dígitos) antes de montar a URL, então não há necessidade de
  lidar com espaço/URL-encoding no path — o Domain normaliza de novo por
  segurança (defesa em profundidade, não confia só no cliente).

## Dependência de outras Tasks

Depende da issue #62 (`Convite.Codigo`, já mergeada) e da issue #61
(`CadastroUsuarioService`/papéis acumuláveis, já mergeada) — ambas já em
`main`, nenhum bloqueio. A issue #64 ("Home apresenta com clareza os
caminhos de entrada") depende desta Task existir como rota alcançável
(`app/aluno/entrar-turma.tsx`) para compor a tela única — esta Task não
precisa saber disso, só publicar a rota.
