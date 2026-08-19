# Desenho técnico — Professor registra frequência da aula (#14)

## Entidades/classes afetadas

**`Synclass.Domain.Frequencias`** (módulo novo):

- `StatusFrequencia` (enum): `Ausente = 0`, `Presente = 1` — mesma decisão
  de trafegar como inteiro já usada por `ModeloAgendamento`/`DiaSemana`.
- `RegistroFrequencia` (sealed, ctor privado + `Criar` estático, mesmo
  padrão de `CancelamentoAula`): `Id`, `AulaId`, `MatriculaId`,
  `StatusProfessor` (`StatusFrequencia?`, `null` até o Professor
  registrar), `ConfirmadoPeloAluno` (`bool?`, `null` até a issue #15
  implementar a confirmação do Aluno — este card só lê essa coluna para
  detectar divergência, nunca escreve nela), `CreatedAt`, `UpdatedAt`.
  - `Criar(aulaId, matriculaId, clock)`: cria a linha com os dois campos
    `null` (upsert parte da premissa de que a linha pode nascer tanto pelo
    Professor quanto, futuramente, pelo Aluno).
  - `RegistrarProfessor(status, clock)`: seta `StatusProfessor` e
    `UpdatedAt` — idempotente/upsert por natureza (AC4: chamar de novo
    sobrescreve, não duplica), não mexe em `ConfirmadoPeloAluno`.
- `IRegistroFrequenciaRepository`: `BuscarAsync(aulaId, matriculaId, ct)`,
  `AdicionarAsync`, `SalvarAsync` — mesmo padrão de
  `ICancelamentoAulaRepository`. Não precisa de `ListarPorAulaAsync`: o
  upsert em lote busca uma linha por vez (N pequeno, uma turma por aula).
- `FrequenciaService` — orquestra o único caso de uso do card:
  - `RegistrarAsync(professorId, horarioId, data, IReadOnlyDictionary<Guid, StatusFrequencia> statusPorMatricula, ct)`:
    1. `HorarioService.BuscarDoProfessorAsync` (reaproveitado — mesma
       checagem de posse de `AulaService.CancelarAsync`, lança
       `HorarioNaoEncontradoException` se não existir/não pertencer).
    2. `IAlocacaoHorarioRepository.ListarPorHorarioAsync(horarioId, ct)` —
       para cada `MatriculaId` em `statusPorMatricula` que não aparece
       nessa lista, lança `AlocacaoNaoEncontradaException` (reaproveitada de
       `Synclass.Domain.Aulas`, mesmo significado: Aluno não alocado *neste
       horário*). Não valida cancelamento (issue #10) — RN do card é
       explícita: registrar frequência de quem cancelou é permitido, a UI
       avisa, não bloqueia.
    3. `AulaService.ObterOuCriarAulaAsync(horarioId, data, ct)` — promovido
       de `private` para `internal` (issue #10 já implementa a
       instanciação sob demanda; reaproveitar em vez de duplicar).
    4. Para cada `(matriculaId, status)`: busca `RegistroFrequencia`
       existente via `BuscarAsync(aula.Id, matriculaId, ct)`; se ausente,
       cria via `RegistroFrequencia.Criar` primeiro; em seguida chama
       `.RegistrarProfessor(status, clock)` e persiste (upsert único).
    5. Retorna `IReadOnlyCollection<RegistroFrequencia>` (uma por Aluno do
       lote) — o controller usa para montar a resposta e decidir os logs.

**`Synclass.Domain.Aulas.AulaService`**: `ObterOuCriarAulaAsync` muda de
`private` para `internal` (mesmo assembly, `FrequenciaService` passa a
chamá-lo via uma referência a `AulaService` injetada) — não duplica a lógica
de instanciação sob demanda descrita em
`docs/specs/10-cancelamento-aula/implementation.md`.

**`Synclass.Infrastructure`**:
- `RegistroFrequenciaConfiguration` (EF mapping, mesmo padrão de
  `CancelamentoAulaConfiguration`), `RegistroFrequenciaRepository`.
- Migration `CriaRegistroFrequencia`: tabela `RegistrosFrequencia` — `Id`
  (uuid, pk), `AulaId` (uuid, FK → `Aulas`), `MatriculaId` (uuid, FK →
  `Matriculas`), `StatusProfessor` (int, nullable), `ConfirmadoPeloAluno`
  (bool, nullable), `CreatedAt`, `UpdatedAt` (timestamp). Índice único
  `(AulaId, MatriculaId)`.

**`Synclass.Api`**: novo controller `FrequenciasController`,
`[Authorize(Roles = "Professor")]` (contrasta com `AulasController`, que é
`Roles = "Aluno")` — não dá para reaproveitar a mesma classe de controller
por causa da autorização diferente, mesmo padrão de separação já usado
entre `AulasController` (Aluno) e `AlocacoesHorarioController` (Professor)):
- `[Route("professores/{professorId:guid}/horarios")]`
- `POST {horarioId:guid}/aulas/{data}/frequencias` — corpo
  `RegistrarFrequenciaRequest(IReadOnlyList<RegistroFrequenciaItemRequest> Registros)`,
  `RegistroFrequenciaItemRequest(Guid MatriculaId, bool Presente)` (`bool`
  no contrato de Api, convertido para `StatusFrequencia` no controller —
  mesma decisão de simplicidade de superfície que `ModeloAgendamento` já
  não seguiu, mas aqui só existem 2 valores e o card não cogita um 3º
  estado, então `bool` evita um enum de 2 valores no contrato).
  - 200: `RegistroFrequenciaResponse[]`, `{ matriculaId, presente: bool,
    confirmadoPeloAluno: bool? }`.
  - 400: `{ "mensagem": string }` — alguma `matriculaId` do corpo não está
    alocada neste horário.
  - 404: horário não existe ou não pertence a este Professor.
  - Logs: `FrequenciaRegistrada` (Information, `TrackId`, `ProfessorId`,
    `AulaId`, contagem de presentes/ausentes do lote) sempre; para cada item
    do lote onde `StatusProfessor != ConfirmadoPeloAluno` (considerando
    `ConfirmadoPeloAluno == null` como "sem confirmação", portanto nunca
    divergente), `FrequenciaDivergente` (Warning, `TrackId`, `AulaId`,
    `MatriculaId`) — mesmo padrão de log por item da resposta usado em
    `AulasController.LogCancelamentoRejeitadoPorPrazo`.
- Registro de DI: `FrequenciaService`, `IRegistroFrequenciaRepository`
  (`Program.cs`, mesmo padrão scoped dos demais).

**Frontend**: `src/lib/api/frequencias.ts` (`registrarFrequencia`, mesmo
envelope `{sucesso, mensagem}` de `cancelamentos.ts`). Tela nova
`src/app/professor/[professorId]/horarios/[horarioId]/chamada.tsx` — `data`
recebida por query string (`?data=yyyy-MM-dd`, mesmo padrão de parâmetro
opcional já usado em telas existentes), lista os Alunos alocados naquele
horário (reaproveita `listarAlocacoes` de `lib/api/alocacoes.ts`, issue #8)
com um toggle presente/ausente por Aluno (organism novo
`FrequenciaAlunoToggle.tsx` ou reaproveitando `ChipSelector` com 2 opções) e
botão "Salvar chamada" que envia o lote de uma vez. Alunos com cancelamento
registrado para aquela data (issue #10) aparecem com aviso visual, não
ficam ocultos nem bloqueados (RN explícita do card).

## Contrato de API

- `POST /professores/{professorId}/horarios/{horarioId}/aulas/{data}/frequencias`
  - Request: `{ "registros": [{ "matriculaId": guid, "presente": bool }] }`.
  - 200: `[{ "matriculaId": guid, "presente": bool, "confirmadoPeloAluno": bool | null }]`.
  - 400: `{ "mensagem": string }`.
  - 404: horário não encontrado/não pertence ao Professor.

## Modelo de dados

Novo: `RegistrosFrequencia` (`Id`, `AulaId` FK, `MatriculaId` FK,
`StatusProfessor` int nullable, `ConfirmadoPeloAluno` bool nullable,
`CreatedAt`, `UpdatedAt`). Índice único `(AulaId, MatriculaId)` — a mesma
linha desta tabela é reaproveitada pela issue #15 para gravar a confirmação
do Aluno (só lê/escreve `ConfirmadoPeloAluno`, nunca `StatusProfessor`).

## Edge points (não cobertos por Gherkin)

- Registrar frequência de um Aluno que cancelou aquela data (issue #10) é
  permitido — a RN é explícita que o Professor pode ter motivo válido (ex:
  correção de cancelamento indevido). `FrequenciaService` não consulta
  `ICancelamentoAulaRepository`; a UI é quem avisa visualmente, sem
  bloquear a chamada de negócio (não implementar checagem de bloqueio no
  Domain/Api).
- `StatusProfessor` é a única fonte usada em cálculos/histórico (issues #15
  futuro, #16) quando há divergência — este card só grava a coluna,
  qualquer consumidor futuro de leitura (histórico do item 16) que precise
  decidir isso é responsabilidade da issue correspondente, não desta.
- Upsert em lote não é transacional entre linhas (mesma postura simplificada
  do resto do domínio, sem uso explícito de transação EF) — uma falha no
  meio do lote deixa algumas linhas gravadas e outras não; risco aceito
  igual ao resto do código (não introduzir transação como escopo novo aqui).
- Justificativa de falta: explicitamente fora de escopo (card cita como
  "campo opcional futuro").

## Dependência de issues anteriores

Depende de `AlocacaoHorario` (issues #8/#9) e da instanciação sob demanda de
`Aula` (issue #10, `AulaService.ObterOuCriarAulaAsync`, promovido a
`internal` para reaproveito). Compartilha a tabela `RegistrosFrequencia` com
a issue #15 (ainda não implementada) — a coluna `ConfirmadoPeloAluno` é
criada por esta migration mas só é escrita quando a #15 for implementada;
até lá, permanece sempre `null` para linhas criadas por este card.
