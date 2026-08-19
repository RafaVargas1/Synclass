# Desenho técnico — Aluno confirma sua presença em uma aula (#15)

## Entidades/classes afetadas

**`Synclass.Domain.Frequencias`** (mesmo módulo de `RegistroFrequencia`,
issue #14):

- `RegistroFrequencia` ganha `ConfirmarAluno(IClock clock)`: seta
  `ConfirmadoPeloAluno = true` e `UpdatedAt`, sem mexer em
  `StatusProfessor` — simetria exata de `RegistrarProfessor` (issue #14).
  Substitui o método interno de teste `CriarComConfirmacaoDoAluno` como
  caminho de escrita real (o método de teste continua existindo só para
  seed direto de cenário, sem passar pelo service).
- `FrequenciaService.ConfirmarPresencaAsync(professorId, horarioId, data,
  alunoUsuarioId, ct)`:
  1. `HorarioService.BuscarDoProfessorAsync` (reaproveitado).
  2. Resolve `matriculaId` via
     `AlocacaoHorarioService.ResolverMatriculaDoAlunoAsync(professorId,
     alunoUsuarioId, ct)` (issue #23) — mesmo padrão de
     `AulasController.ResolverMatriculaAsync`.
  3. `AulaService.ObterOuCriarAulaAsync` (reaproveitado, instanciação sob
     demanda).
  4. Rejeita se o próprio Aluno já cancelou esta ocorrência: consulta
     `ICancelamentoAulaRepository.BuscarAsync(aula.Id, matriculaId, ct)`;
     se existir, lança `AulaRejeitadaException` (reaproveitada de #10, não
     cria exceção nova — mesma família semântica "aula não disponível para
     esta ação").
  5. Upsert em `RegistroFrequencia`: busca por (aula.Id, matriculaId); se
     não existe, cria via `RegistroFrequencia.Criar` e adiciona; chama
     `ConfirmarAluno` na linha (nova ou existente) — idempotente por
     natureza (AC2).
  6. `SalvarAsync` uma única vez (mesma correção de N+1 já aplicada em
     `RegistrarAsync`, issue #14 pós-review).
  7. Retorna o `RegistroFrequencia` persistido para o log e a resposta da
     Api.

**`FrequenciaService` ganha as dependências**:
`IAlocacaoHorarioRepository` já não é necessária aqui (a resolução usa
`AlocacaoHorarioService`, que por sua vez usa o repositório internamente);
construtor ganha `AlocacaoHorarioService` e `ICancelamentoAulaRepository`
como novas dependências via injeção (mesmo padrão de `AulaService`).

**`Synclass.Api`**: novo endpoint no `FrequenciasController` existente —
mas como este card é ação do **Aluno** (`[Authorize(Roles = "Aluno")]`) e
`FrequenciasController` inteiro é `[Authorize(Roles = "Professor")]`
(issue #14), o endpoint vai para o `AulasController` existente (que já é
`[Authorize(Roles = "Aluno")]` e já resolve `matriculaId` da sessão via
`ResolverMatriculaAsync` — reaproveitar esse método privado):

- `POST {horarioId:guid}/aulas/{data}/confirmacao-presenca` →
  `FrequenciaService.ConfirmarPresencaAsync`. Mapeamento de exceções:
  `AlunoNaoVinculadoAoProfessorException` → 404;
  `HorarioNaoEncontradoException` → 404; `AulaRejeitadaException` → 400
  (mesmo padrão try/catch já usado em `Cancelar`).
  Log `PresencaConfirmadaPeloAluno` (Information,
  `{TrackId} {MatriculaId} {AulaId}`) após sucesso.
- Registro de DI: `FrequenciaService` ganha as novas dependências no
  `Program.cs` (ajuste do registro existente, issue #14).

**Frontend**: `src/lib/api/frequencias.ts` ganha `confirmarPresenca(
professorId, horarioId, data)`, mesmo envelope `{sucesso, mensagem}` de
`cancelamentos.ts`. Tela `minhas-aulas.tsx` (issue #10) ganha um botão
"Confirmar presença" ao lado do "Cancelar" em `AulaProximaCard`, com estado
local (`confirmado: boolean`, otimista após 200 da chamada) — **decisão de
implementação**: o contrato de `GET proximas-aulas` (`AulaProximaResponse`,
issue #10) não é estendido com `confirmadoPeloAluno` neste card, porque
isso exigiria `AulaService`/`AulaProxima` (módulo `Synclass.Domain.Aulas`)
passar a depender de `IRegistroFrequenciaRepository`
(`Synclass.Domain.Frequencias`), acoplamento cross-módulo que nenhum
critério de aceite Gherkin do card exige — o requisito é "diferenciar
confirmado de sem ação" na mesma sessão em que o Aluno confirma, não
persistir esse estado visual entre reloads/sessões. Se um uso futuro pedir
isso, o contrato pode ser estendido then.

## Contrato de API

- `POST /professores/{professorId}/horarios/{horarioId}/aulas/{data}/confirmacao-presenca`
  - `data` na rota, formato `yyyy-MM-dd`. `matriculaId` resolvida da
    sessão do Aluno (não recebida no corpo/query).
  - 200: `{ "aulaId": guid, "matriculaId": guid, "confirmadoPeloAluno": true
    }`.
  - 400: `{ "mensagem": string }` — Aluno cancelou esta ocorrência.
  - 404: horário não existe/não pertence ao Professor, ou Aluno não
    vinculado ao Professor.

## Modelo de dados

Nenhuma migration nova — reaproveita `RegistrosFrequencia` (issue #14),
criando a linha (com `StatusProfessor` nulo) se ainda não existir, mesmo
índice único `(AulaId, MatriculaId)`.

## Edge points (não cobertos por Gherkin)

- Aluno cancelou a própria ocorrência (issue #10) → confirmação rejeitada
  (400), não silenciosamente ignorada — evita um estado inconsistente onde
  o Aluno aparece como "confirmado presente" numa aula que ele mesmo disse
  que não vai.
- Não existe endpoint de "desconfirmar" ou de "marcar ausência" pelo
  Aluno — fora de escopo explícito do card (RN: "Um Aluno não pode
  registrar 'ausência' via este fluxo").
- `Aula` ainda não existe na data pedida → criada na hora, mesmo mecanismo
  de #14/#10.
- Concorrência: dois `POST` simultâneos do mesmo Aluno para a mesma aula —
  upsert idempotente, índice único como guard rail final (mesmo padrão do
  resto do domínio).

## Dependência de issues anteriores

Depende de `RegistroFrequencia`/`IRegistroFrequenciaRepository` (issue
#14, mesma tabela), `CancelamentoAula` (issue #10) e
`ResolverMatriculaDoAlunoAsync` (issue #23). É consumida pela issue #16
(histórico de frequência — leitura pura desta tabela, agora com os dois
campos preenchíveis).
