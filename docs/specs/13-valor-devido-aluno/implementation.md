# Especificação técnica: Aluno consulta o total devido com detalhamento por Professor (#13)

Card: https://github.com/RafaVargas1/Synclass/issues/13

## Reaproveitamento do serviço de domínio da issue #12 (decisão confirmada nesta reflexão)

A issue #12 (`docs/specs/12-valor-devido-professor/implementation.md#reaproveitamento-pela-issue-13`)
já deixou o desenho pronto para esta Task, e o código mergeado (#35, commit `91ecf70`) confirma:

- `Synclass.Domain.Cobrancas.ConsultaCobrancaService` — nome/namespace não amarrados a
  "Professor" de propósito. Esta Task adiciona `ConsultarPorAlunoAsync(Guid alunoUsuarioId,
  PeriodoConsulta periodo, CancellationToken)` na mesma classe, reaproveitando
  `CalcularParaMatriculaAsync`/`ContarAulasNoPeriodoAsync` sem duplicar a lógica de cálculo.
- `Synclass.Domain.Cobrancas.PeriodoConsulta` — reaproveitado tal e qual (`MesCorrente`,
  `Criar`, `ContarOcorrencias`), nenhuma mudança.
- `Synclass.Domain.Cobrancas.ValorDevidoPorMatricula` — DTO de domínio por vínculo, reaproveitado
  tal e qual; `Nome` passa a significar "nome de quem aparece na linha" (Aluno na direção #12,
  Professor na direção #13), não muda de shape.
- `IRegraDeCobrancaRepository`, `IAlocacaoHorarioRepository`, `IHorarioRepository` — reaproveitados
  sem nenhuma mudança de contrato.

**Nenhuma lógica de cálculo é duplicada.** O que esta Task adiciona são só os dois pontos de
entrada que já estavam previstos e ainda não existiam:

1. `IMatriculaRepository.ListarPorAlunoAsync(Guid alunoUsuarioId, CancellationToken)` — análogo a
   `ListarPorProfessorAsync`, filtrando por `Matricula.AlunoUsuarioId` em vez de `ProfessorId`.
   Como `AlunoUsuarioId` só é definido em matrículas plenas (`Matricula.Promover`), este método
   nunca retorna matrícula provisória — resolve sozinho a RN "Aluno provisório sem nenhum vínculo
   pleno não tem nada a pagar" (a lista vem vazia, sem tratamento especial no service).
2. Resolução de nome do **Professor** (não do Aluno): `ConsultaCobrancaService.ResolverNomeAluno`
   (existente, usa `matricula.NomeProvisorio ?? string.Empty`) não serve para a direção do Aluno —
   o Professor sempre tem identidade de `Usuario` completa (nunca é "provisório"), então o nome
   certo vem de `Usuario.Nome` buscado por `Matricula.ProfessorId`. Isso exige um método novo:
   `IUsuarioRepository.BuscarPorIdAsync(Guid id, CancellationToken)` (o repositório hoje só tem
   `ExisteAsync`/`BuscarPorContatoAsync`, nenhum dos dois carrega o `Usuario` inteiro por id).

`ConsultaCobrancaService` passa a receber `IUsuarioRepository` no construtor (novo parâmetro) e os
dois métodos públicos (`ConsultarPorProfessorAsync`/`ConsultarPorAlunoAsync`) passam um
`Func<Matricula, CancellationToken, Task<string>>` de resolução de nome para o método privado
compartilhado `CalcularParaMatriculaAsync`, para não duplicar o loop de cálculo nem o tratamento de
"sem regra definida".

## Releitura de `requisitos-funcionais.md`/specs relacionadas (sem regra conflitante)

- Item 13 do backlog ("Um Aluno pode ver quanto tem que pagar no mês") é exatamente o escopo desta
  Task — RN da issue já cobre o "por Professor" além do "no mês" (generalizado para "no período").
- Item 11 (regra de cobrança) e a nota de modelagem "identidade de usuário e matrícula são
  conceitos separados" seguem exatamente como já usadas pela #12 — nenhuma mudança de modelo.
- `docs/specs/5-aluno-multiplos-professores` (N:N) já formalizou em teste que um mesmo
  `AlunoUsuarioId` pode ter uma `Matricula` por Professor — é exatamente essa lista que
  `ListarPorAlunoAsync` devolve, uma linha por vínculo, nunca agregada num total único (RN desta
  issue: "sem misturar débitos... de um Professor com outro").
- `docs/specs/23-sessao-real-recursos`: `MarcacoesHorarioController` já estabeleceu o padrão para
  ações do Aluno sobre si mesmo — identidade resolvida de `User.GetUsuarioId()` (token), nunca de
  parâmetro de rota/query. Esta Task segue o mesmo padrão (diferente do Professor em `ValorDevidoController`,
  que usa `professorId` de rota por decisão pré-existente da #12, não revisitada).

## Entidades/contratos envolvidos (nenhuma migration nova)

- `IMatriculaRepository` — **novo método** `ListarPorAlunoAsync(Guid alunoUsuarioId,
  CancellationToken)`. Implementado em `Synclass.Infrastructure.Persistence.MatriculaRepository`
  (`WHERE AlunoUsuarioId == alunoUsuarioId`, mesma tabela `Matriculas`, nenhuma migration).
- `IUsuarioRepository` — **novo método** `BuscarPorIdAsync(Guid id, CancellationToken)`.
  Implementado em `Synclass.Infrastructure.Persistence.UsuarioRepository` (mesma tabela
  `Usuarios`, nenhuma migration).
- `ConsultaCobrancaService` — novo parâmetro de construtor `IUsuarioRepository`, novo método
  público `ConsultarPorAlunoAsync`.

## Contrato de API

`GET /alunos/valor-devido?inicio=yyyy-MM-dd&fim=yyyy-MM-dd`

- `[Authorize(Roles = "Aluno")]`. `alunoUsuarioId` vem de `User.GetUsuarioId()` (token da sessão),
  **não** de parâmetro de rota — não existe "lista de valor devido de outro Aluno" a proteger
  contra IDOR, então nem faz sentido expor o parâmetro (mesma decisão de
  `MarcacoesHorarioController.ResolverMatriculaAsync`).
- `inicio`/`fim` opcionais, mesmo comportamento de `ValorDevidoController` (ausentes os dois → mês
  corrente; só um dos dois → 400, sem "meio-padrão").
- Resposta 200 (mesmo shape de `ValorDevidoResponse`, reaproveitado tal e qual — controller novo,
  DTO igual):
  ```json
  [
    {
      "matriculaId": "guid",
      "alunoUsuarioId": "guid",
      "nome": "string (nome do Professor)",
      "valor": "decimal|null",
      "semRegraDefinida": "bool"
    }
  ]
  ```
- Lista vazia (não 404) quando o Aluno não tem nenhum vínculo pleno — cobre tanto "Aluno sem
  nenhum Professor" quanto "Aluno provisório ainda sem login" (este último nem alcança o
  endpoint, porque não tem sessão autenticada para gerar o token).

## Logs

Evento `ConsultaTotalDevidoRealizada` (Information) emitido pelo controller, uma vez por chamada:
`TrackId`, `UsuarioId` (o Aluno autenticado), `PeriodoInicio`, `PeriodoFim`. Nome do evento
deliberadamente diferente de `ConsultaValorDevidoRealizada` (#12) — são fluxos/atores distintos,
mesmo padrão de nomeação distinta já usado entre `AlocacaoHorarioController`/`MarcacoesHorarioController`.

## Edge points

- Vínculo sem `RegraDeCobranca`: mesmo comportamento de `SemRegraDefinida = true`/`Valor = null`
  da #12, herdado de graça por reaproveitar `CalcularParaMatriculaAsync`.
- Aluno provisório sem vínculo pleno: não modelável neste endpoint (precisa de sessão — issue
  #18/#23, já mergeadas), então o "sem nada a pagar" é coberto pela lista vazia de
  `ListarPorAlunoAsync` para qualquer Aluno autenticado sem `Matricula` própria — não há caminho
  de teste de fumaça que simule literalmente um Aluno provisório chamando o endpoint (ele não tem
  login), então o critério técnico é coberto pelo caso "Aluno autenticado sem nenhuma Matricula".
- Resolução de nome do Professor via `IUsuarioRepository.BuscarPorIdAsync`: `Usuario?` pode ser
  `null` só se `ProfessorId` referenciar um `Usuario` apagado — não existe exclusão de `Usuario`
  no domínio hoje, então trata-se como `string.Empty` (mesmo "melhor esforço" da #12), não lança
  exceção.
- N+1 de `BuscarPorIdAsync` do Professor por matrícula: mesma decisão de escala aceitável da #12
  (poucos vínculos por Aluno).

## Frontend

- `frontend/src/lib/api/valorDevido.ts` — adiciona `listarValorDevidoDoAluno(periodo?)`
  (sem parâmetro de id, GET `/alunos/valor-devido`), reaproveitando o tipo `ValorDevidoPorMatricula`
  e `PeriodoConsultaInput` já existentes.
- `ValorDevidoCard` — reaproveitado tal e qual (já é genérico: mostra `nome` + valor formatado,
  nunca amarrado a "Aluno" ou "Professor" no texto).
- Nova rota `frontend/src/app/aluno/valor-devido.tsx` — tela de nível superior (sem segmento
  dinâmico `[professorId]`, diferente da tela do Professor): lista agregada de todos os
  Professores do Aluno autenticado, mesmo padrão de estados/seletor de período de
  `professor/[professorId]/valor-devido.tsx`.
- Sem total somado entre Professores diferentes na UI (RN explícita) — a tela só renderiza a
  lista, sem `reduce`/soma.

## Dependência de outras Tasks

Nenhuma migration ou issue pendente — depende só da #11/#12 (mergeadas) e #18/#23 (sessão real,
mergeadas, usadas só para autenticação, sem mudança de contrato aqui).
