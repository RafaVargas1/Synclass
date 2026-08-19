# Desenho técnico — Aluno desmarca aula com antecedência configurável (#10)

## Entidades/classes afetadas

**`Synclass.Domain.Aulas`** (módulo novo — primeira vez que "aula" como
ocorrência datada existe no código; hoje só existe `Horario`, o template
recorrente, ver `Horario.cs:6-10`):

- `Aula` (sealed, ctor privado + `Criar` estático, mesmo padrão de
  `AlocacaoHorario`): `Id`, `HorarioId`, `Data` (`DateOnly`), `CreatedAt`
  (via `IClock`).
- `CancelamentoAula` (sealed, mesmo padrão): `Id`, `AulaId`, `MatriculaId`,
  `CanceladoEm`, `CreatedAt` (ambos via `IClock`, mesmo instante).
- `IAulaRepository`: `BuscarPorHorarioEDataAsync(horarioId, data, ct)`,
  `AdicionarAsync`, `SalvarAsync`.
- `ICancelamentoAulaRepository`: `BuscarAsync(aulaId, matriculaId, ct)`,
  `AdicionarAsync`, `SalvarAsync`.
- `AulaService` — orquestra os 2 casos de uso do card:
  - `CancelarAsync(professorId, horarioId, data, matriculaId, ct)`:
    1. `HorarioService.BuscarDoProfessorAsync` (reaproveitado, lança
       `HorarioNaoEncontradoException` se não existir/não pertencer).
    2. Garante que a matrícula está alocada *neste horário* — nova checagem,
       via `IAlocacaoHorarioRepository.BuscarAsync(horarioId, matriculaId,
       ct)`; `null` lança `AlocacaoNaoEncontradaException` (nova — distinta
       de `MatriculaNaoVinculadaAoProfessorException`, que valida vínculo
       Aluno-Professor, não Aluno-Horário).
    3. `ObterOuCriarAulaAsync`: busca `Aula` por (horarioId, data); se
       ausente, cria via `Aula.Criar` e persiste — é o gatilho de
       instanciação sob demanda descrito na RN.
    4. Idempotência: se já existe `CancelamentoAula` para
       (aula.Id, matriculaId), retorna o existente sem revalidar prazo (edge
       point do card — "cancelar uma aula já cancelada... não é erro").
    5. `GarantirDentroDoPrazoAsync`: busca `ConfiguracaoProfessor`; usa
       `configuracao?.PrazoCancelamentoMinutos ?? 0` (ver decisão de
       default abaixo). Calcula `inicioAula = data.ToDateTime(horario.HoraInicio)`
       e `limite = inicioAula.AddMinutes(-prazo)`; se `_clock.UtcNow >
       limite`, lança `PrazoCancelamentoExpiradoException(aula.Id, prazo,
       limite)` (mensagem inclui até quando era possível cancelar, ver
       code-style.md sobre mensagens de exceção).
    6. Cria `CancelamentoAula.Criar(aula.Id, matriculaId, _clock)`,
       persiste, retorna.
  - `ListarProximasAsync(professorId, matriculaId, ct)`:
    1. Garante vínculo da matrícula (reaproveita
       `GarantirMatriculaVinculadaAsync`, mesmo padrão de
       `AlocacaoHorarioService`).
    2. Para cada `AlocacaoHorario` da matrícula (novo
       `IAlocacaoHorarioRepository.ListarPorMatriculaAsync`), calcula a
       *próxima* ocorrência futura do `Horario` (primeira data ≥ hoje cujo
       dia da semana bate com `DiaSemana`; se hoje bate mas `HoraInicio` já
       passou, pula para a semana seguinte) e, se essa ocorrência já tem
       `CancelamentoAula` para esta matrícula, avança para a ocorrência
       seguinte (não mostra uma aula já cancelada como "próxima").
    3. Retorna 1 `AulaProxima` (record) por alocação — `HorarioId`, `Data`,
       `DiaSemana`, `HoraInicio`, `DuracaoMinutos`, `PodeCancelar` (bool,
       mesmo cálculo de prazo do item 5 acima), `CancelavelAte` (datetime,
       para a mensagem de UI).

**Decisão de implementação (sem impacto de domínio, documentada por não
haver round-trip síncrono com o mantenedor neste lote)**: a lista de
"próximas aulas" mostra só a ocorrência imediatamente seguinte de cada
horário alocado, não uma janela de N semanas — nenhum critério de aceite do
card pede múltiplas datas futuras por horário, e a ação de cancelar só faz
sentido sobre a próxima ocorrência acionável (ocorrências futuras distantes
ainda nem existem como conceito relevante para o Aluno até chegar a vez
delas). Se um uso futuro pedir cancelar uma data específica mais à frente, a
API já suporta isso via `data` explícito no `POST`, só a listagem que fica
restrita à próxima.

**`ConfiguracaoProfessor`** (`Configuracoes/ConfiguracaoProfessor.cs`) ganha
`PrazoCancelamentoMinutos` (int). **Decisão de implementação**: coluna
`not null default 0` — Professores existentes (cadastrados antes desta
issue) não tinham essa configuração; `0` minutos significa "sem antecedência
mínima exigida" (Aluno pode cancelar até o próprio início da aula), que é o
comportamento menos surpreendente para quem já usa o sistema sem essa
feature — evita bloquear cancelamento por omissão de configuração, e é
consistente com o card não definir um valor-teto/mínimo. `Criar`/`Atualizar`
do agregado devem validar `PrazoCancelamentoMinutos >= 0`.

**`Synclass.Infrastructure`**:
- `AulaConfiguration`, `CancelamentoAulaConfiguration` (EF mappings, mesmo
  padrão de `AlocacaoHorarioConfiguration`).
- `AulaRepository`, `CancelamentoAulaRepository`.
- `AlocacaoHorarioRepository` ganha `ListarPorMatriculaAsync(matriculaId, ct)`.
- Migration `CriaAulaECancelamento`:
  - `Aulas`: `Id` (uuid, pk), `HorarioId` (uuid, FK → `Horarios`), `Data`
    (date), `CreatedAt` (timestamp). Índice único `(HorarioId, Data)`.
  - `CancelamentosAula`: `Id` (uuid, pk), `AulaId` (uuid, FK → `Aulas`),
    `MatriculaId` (uuid, FK → `Matriculas`), `CanceladoEm` (timestamp),
    `CreatedAt` (timestamp). Índice único `(AulaId, MatriculaId)`.
  - `ConfiguracoesProfessor`: `AddColumn PrazoCancelamentoMinutos int not
    null default 0`.

**`Synclass.Api`**: novo controller `AulasController`,
`professores/{professorId:guid}/horarios`:
- `GET proximas-aulas?matriculaId={guid}` → `AulaService.ListarProximasAsync`.
- `POST {horarioId:guid}/aulas/{data}/cancelamentos` (`{ matriculaId }` no
  corpo, `data` como `yyyy-MM-dd` na rota) → `AulaService.CancelarAsync`.
  Mapeamento de exceções: `HorarioNaoEncontradoException` → 404;
  `AlocacaoNaoEncontradaException`, `PrazoCancelamentoExpiradoException` →
  400 (mesmo padrão try/catch de `MarcacoesHorarioController`), com log
  `LogCancelamentoRejeitadoPorPrazo` no segundo caso.
- Registro de DI: `AulaService`, `IAulaRepository`, `ICancelamentoAulaRepository`
  (`Program.cs`, mesmo padrão scoped dos demais repositórios).

**Frontend**: `src/lib/api/cancelamentos.ts` (`listarProximasAulas`,
`cancelarAula`, mesmo envelope `{sucesso, mensagem}` de `marcacoes.ts`).
Tela nova `src/app/aluno/[matriculaId]/professores/[professorId]/minhas-aulas.tsx`
(rota irmã de `horarios.tsx`, mesmo padrão de segmentos) — separada de
`horarios.tsx` porque são ações opostas (marcar um vago vs. cancelar um já
marcado) sobre listas diferentes (vagos vs. próprias alocações). Organism
novo `AulaProximaCard.tsx` (dia da semana, hora, botão "Cancelar" —
desabilitado com texto explicando o motivo/prazo quando `podeCancelar ===
false`), reaproveitando a mesma apresentação de dia/hora de
`HorarioVagoCard`/`HorarioAlocacaoCard` (extrair helper compartilhado se a
duplicação passar de um trecho pequeno, ver code-style.md).

## Contrato de API

- `GET /professores/{professorId}/horarios/proximas-aulas?matriculaId={guid}`
  - 200: `AulaProximaResponse[]`, onde `AulaProximaResponse = { horarioId,
    data: "yyyy-MM-dd", diaSemana: int, horaInicio, duracaoMinutos,
    podeCancelar: bool, cancelavelAte: datetime, prazoCancelamentoMinutos: int }`.
  - 400: `{ "mensagem": string }` — matrícula não vinculada a este Professor.
- `POST /professores/{professorId}/horarios/{horarioId}/aulas/{data}/cancelamentos`
  - Request: `{ "matriculaId": guid }`; `data` na rota, formato `yyyy-MM-dd`.
  - 200: `{ "id": guid, "aulaId": guid, "matriculaId": guid, "canceladoEm": datetime }`.
  - 400: `{ "mensagem": string }` — fora do prazo (mensagem inclui até
    quando era possível cancelar), ou matrícula não alocada neste horário.
  - 404: horário não existe ou não pertence a este Professor.

## Modelo de dados

Novo (ver migration acima): `Aulas`, `CancelamentosAula`. Alteração em
`ConfiguracoesProfessor`: `PrazoCancelamentoMinutos int not null default 0`.

## Edge points (não cobertos por Gherkin)

- `Aula` ainda não existe na data pedida → criada na hora pelo próprio
  `CancelarAsync` (RN explícita do card).
- Cancelar uma aula já cancelada pelo mesmo Aluno é idempotente — retorna o
  `CancelamentoAula` existente, sem revalidar prazo (evita que uma segunda
  tentativa, feita já fora do prazo por atraso de rede, vire erro confuso
  para uma ação que already succeeded).
- Prazo aplicado é sempre o vigente no momento do cancelamento, não o
  vigente na alocação — `AulaService` lê `ConfiguracaoProfessor` a cada
  chamada, nunca cacheia/snapshotta o prazo em `AlocacaoHorario` ou `Aula`.
- Sem tratamento de fuso horário — mesma simplificação já usada no resto do
  domínio (`Horario.HoraInicio`/`IClock.UtcNow` tratados num único fuso
  implícito); fora de escopo desta issue introduzir timezone por Professor.
- Corrida concorrente entre dois cancelamentos simultâneos do mesmo Aluno na
  mesma aula: o índice único `(AulaId, MatriculaId)` é o guard rail final,
  mesma postura de `AlocacaoJaExisteException` nas issues #8/#9 — a segunda
  gravação falha na constraint; a camada de aplicação trata isso como
  "idempotente" só quando a leitura prévia já encontra o registro (corrida
  restante é risco aceito, mesmo padrão do resto do domínio).

## Dependência de issues anteriores

Depende de `AlocacaoHorario` (issues #8/#9, vínculo Aluno-Horário) e
`ConfiguracaoProfessor` (issue #7). Não depende de #17 (limite de alunos por
horário) — cancelamento libera vaga automaticamente por não existir mais
`AlocacaoHorario` bloqueando a contagem daquela aula específica (a
`AlocacaoHorario` em si não muda; é o `CancelamentoAula` que, ao ser
consultado por quem lista vagas *daquela data*, precisaria ser considerado —
fora do escopo desta issue, que não mexe em `ListarVagosAsync`; ver nota
abaixo).

**Nota para issue futura (fora de escopo aqui)**: a RN do card #10 diz que
"cancelar libera o espaço apenas naquela data para outro Aluno se marcar",
mas `AlocacaoHorarioService.ListarVagosAsync`/`GarantirVagaDisponivelAsync`
(issues #8/#9) contam vagas por `AlocacaoHorario` recorrente, sem noção de
data — hoje uma aula cancelada não libera vaga para *aquela data* porque o
fluxo de marcação livre (#9) não sabe filtrar por `CancelamentoAula`. Cobrir
isso integralmente exigiria mudar `ListarVagosAsync` para ser
data-consciente, o que extrapola o escopo desta Task (o card #10 não pede
isso como critério de aceite Gherkin — os 5 critérios cobrem só o
cancelamento em si, não a re-marcação de outro Aluno na vaga liberada).
Registrar como débito técnico ao abrir o PR.
