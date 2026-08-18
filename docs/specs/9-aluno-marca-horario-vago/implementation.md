# Desenho técnico — Aluno marca aula em horário vago (#9)

## Entidades/classes afetadas

**`Synclass.Domain.Alocacoes`** (extensão do bounded context da issue #8):

- `AlocacaoHorario` — ganha `OrigemAlocacao` (novo enum `OrigemAlocacao { Professor = 0, Aluno = 1 }`,
  arquivo próprio). `Criar(...)` passa a receber `OrigemAlocacao` como parâmetro
  obrigatório (não opcional — força todo call site a declarar quem iniciou).
  Continua "burra": a decisão de qual origem usar é do `AlocacaoHorarioService`,
  não da entidade.
- `AlocacaoHorarioService` — `AlocarAsync` (Professor, issue #8) passa
  `OrigemAlocacao.Professor`. Novo método `MarcarAsync(professorId, horarioId,
  matriculaId, ct)` (Aluno, esta issue) espelha `AlocarAsync` mas:
  - usa `GarantirModeloPermiteMarcacaoAsync` (nova, chama
    `ConfiguracaoProfessor.PermiteMarcacaoLivre`) em vez de
    `GarantirModeloPermiteAlocacaoAsync` — são regras opostas, cada uma serve
    um fluxo, nenhuma é adaptada para servir os dois (evita `if` condicional
    no meio de um método que decide qual regra aplicar).
  - reaproveita `GarantirVagaDisponivelAsync`, `GarantirMatriculaVinculadaAsync`,
    `GarantirAindaNaoAlocadoAsync` tal qual (mesmas regras, independente de
    quem inicia).
  - passa `OrigemAlocacao.Aluno` para `AlocacaoHorario.Criar`.
  - `GarantirModeloPermiteMarcacaoAsync` calcula `horarioPossuiAtribuicaoFixa`
    via `IAlocacaoHorarioRepository.PossuiAlocacaoOrigemProfessorAsync(horarioId, ct)`
    (novo método) — "atribuição fixa" = existe ao menos uma linha de
    `AlocacaoHorario` deste horário com `OrigemAlocacao.Professor`.
  - Novo método `ListarVagosAsync(professorId, matriculaId, ct)` — lista, para
    o modelo do Professor, os horários que este Aluno pode marcar livremente
    agora: garante vínculo da matrícula, busca `ConfiguracaoProfessor` (se
    ausente ou modelo não permitir nenhuma marcação livre — `Fixo` — devolve
    lista vazia, sem lançar; GET é consulta, não ação, então "nada disponível"
    é resultado válido, diferente de `MarcarAsync`, que rejeita com exceção
    porque é uma tentativa de ação), lista `Horario` do Professor
    (`HorarioService.ListarAsync`), e para cada um calcula
    `vagasRestantes = horario.LimiteAlunos - ContarPorHorarioAsync(horario.Id)`
    e `possuiAtribuicaoFixa` (mesmo método acima); filtra por
    `PermiteMarcacaoLivre(possuiAtribuicaoFixa) && vagasRestantes > 0`.
- `ModeloNaoPermiteMarcacaoLivreException` (novo, `AlocacaoRejeitadaException`)
  — distinto de `ModeloNaoPermiteAlocacaoException` (issue #8): mensagens
  opostas ("não permite atribuição fixa pelo Professor" vs. "não permite
  marcação livre pelo Aluno"); reaproveitar uma para o outro caso produziria
  mensagem de erro enganosa para quem lê o log/resposta 400.
- `IAlocacaoHorarioRepository` ganha
  `Task<bool> PossuiAlocacaoOrigemProfessorAsync(Guid horarioId, CancellationToken ct)`.

**`Synclass.Infrastructure`**:
- `AlocacaoHorarioConfiguration` mapeia `OrigemAlocacao` (enum → `int`, mesmo
  padrão de `ModeloAgendamento` em `ConfiguracaoProfessorConfiguration`).
- `AlocacaoHorarioRepository` implementa `PossuiAlocacaoOrigemProfessorAsync`
  (`AnyAsync(a => a.HorarioId == horarioId && a.OrigemAlocacao == OrigemAlocacao.Professor)`).
- Migration `AdicionaOrigemAlocacao`: coluna `OrigemAlocacao int not null
  default 0` (`Professor`) — backfill correto por construção, já que toda
  linha existente antes desta issue só podia vir do fluxo do Professor
  (issue #8; o fluxo do Aluno não existia ainda).

**`Synclass.Api`**: novo controller `MarcacoesHorarioController`,
`professores/{professorId:guid}/horarios`:
- `GET vagos?matriculaId={guid}` → `MarcarAsync`-adjacent read, chama
  `ListarVagosAsync`.
- `POST {horarioId:guid}/marcacoes` (`{ matriculaId }` no corpo) → chama
  `MarcarAsync`.

Controller separado de `AlocacoesHorarioController` (issue #8) — rotas e
semântica de autorização diferentes (ali é o Professor agindo sobre seus
Alunos, aqui é o Aluno agindo sobre si mesmo), mesmo padrão de separação já
usado entre `HorariosController` (Professor) e este. Registro de DI: nenhum
novo (mesmo `AlocacaoHorarioService`, já registrado).

**Frontend**: `src/lib/api/marcacoes.ts` (`listarHorariosVagos`,
`marcarHorario`, mesmo envelope de `alocacoes.ts`). Tela nova
`src/app/aluno/[matriculaId]/professores/[professorId]/horarios.tsx` — rota
de dois segmentos porque, antes da issue #5 (Aluno vinculado a vários
Professores), uma Matrícula já implica um Professor, mas a issue #3
(cadastro de Aluno provisório) e a #8 já usam `professorId` explícito nas
suas próprias rotas por não haver sessão (ver "Rota/autenticação" abaixo) —
manter os dois segmentos explícitos evita uma decisão de design que a #5 vai
revisitar de qualquer forma. Organism `HorarioVagoCard` (dia da semana,
horário, `vagasRestantes` calculado no backend, botão "Marcar") reaproveita
o mesmo componente de exibição de dia/hora de `HorarioAlocacaoCard` (issue
#8) — ver code-style.md sobre não duplicar apresentação.

## Contrato de API

- `GET /professores/{professorId}/horarios/vagos?matriculaId={guid}`
  - 200: `HorarioVagoResponse[]`, onde
    `HorarioVagoResponse = { id, diaSemana: int, horaInicio, duracaoMinutos, vagasRestantes: int }`.
  - 400: `{ "mensagem": string }` — matrícula não vinculada a este Professor
    (mesma exceção `MatriculaNaoVinculadaAoProfessorException` de #8).
  - Modelo `Fixo` ou nenhum horário elegível: 200 com array vazio (não é
    erro — ver decisão em `ListarVagosAsync` acima).
- `POST /professores/{professorId}/horarios/{horarioId}/marcacoes`
  - Request: `{ "matriculaId": guid }`
  - 200: `{ "id": guid, "horarioId": guid, "matriculaId": guid, "createdAt": datetime }`
    (mesmo `AlocacaoHorarioResponse` de #8 — a resposta não expõe
    `OrigemAlocacao`, é detalhe de servidor, não de UI).
  - 400: `{ "mensagem": string }` — modelo não permite marcação livre (Fixo,
    ou Híbrido com este horário fixado), horário lotado, matrícula não
    vinculada, ou Aluno já marcado neste horário.
  - 404: horário não existe ou não pertence a este Professor.

## Modelo de dados

Alteração em `AlocacoesHorario` (issue #8, sem tabela nova):

| Coluna | Tipo | Observação |
|---|---|---|
| `OrigemAlocacao` | `integer` | `not null default 0` (`Professor`); `0 = Professor`, `1 = Aluno` |

## Edge points (não cobertos por Gherkin)

- Aluno tentando marcar duas vezes o mesmo horário: reaproveita
  `AlocacaoJaExisteException` (mesma exceção de #8) — retorna 400 com
  mensagem clara, sem tratamento especial de idempotência silenciosa (a
  segunda tentativa é um erro visível, não um sucesso mascarado; decisão de
  UX resolvida a favor da consistência com o comportamento já existente do
  Professor em #8, em vez de introduzir um comportamento novo só para este
  fluxo).
- `ConfiguracaoProfessor` ausente ao tentar marcar: mesma política defensiva
  de #8 — `GarantirModeloPermiteMarcacaoAsync` trata ausência como "não
  permite" (`ModeloNaoPermiteMarcacaoLivreException`), nunca como `Vago`
  (aqui seria o oposto do erro conservador de #8, mas a mesma politica geral
  "falhar do jeito mais claro" se aplica).
- `PossuiAlocacaoOrigemProfessorAsync` decide "atribuição fixa" por
  existência de qualquer linha com `OrigemAlocacao.Professor` naquele
  horário — não por contagem. Um horário Híbrido onde o Professor alocou 1
  de 3 vagas fica inteiramente bloqueado para marcação livre do Aluno (as 2
  vagas restantes não ficam "meio abertas") — é a leitura mais direta do
  Gherkin ("horários não atribuídos fixamente", não "horários com vaga
  residual não atribuída"); não modela alocação por vaga individual dentro
  do mesmo horário.
- Corrida concorrente: mesma postura de #8 (`AlocacaoJaExisteException` via
  índice único cobre duplicidade; `HorarioLotadoException` não tem guard
  rail de banco, risco aceito).

## Débito técnico aberto ao final desta issue

`professorId`/`matriculaId` explícitos na rota, sem sessão real — mesmo
padrão já usado em #3/#8, e já rastreado por uma issue de débito técnico
(`#23`, hoje só cobrindo o lado Professor). Ao abrir o PR desta issue, editar
`#23` (ou abrir uma nova cobrindo os dois) para incluir também as rotas do
Aluno criadas aqui.

## Dependência da issue #17 (agora consumida)

`Horario.LimiteAlunos` e `AlocacaoHorarioService.GarantirVagaDisponivelAsync`
(issue #8, já preparados para #17) são reaproveitados tal qual — nenhuma
mudança adicional necessária, `ListarVagosAsync` só lê o mesmo dado para
compor `vagasRestantes`.
