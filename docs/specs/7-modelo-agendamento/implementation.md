# Desenho técnico — Modelo de agendamento (#7)

## Entidades/classes afetadas

**`Synclass.Domain.Configuracoes`** (novo bounded context, espelha `Horarios`):

- `ModeloAgendamento` (enum) — `Vago = 0`, `Fixo = 1`, `Hibrido = 2`.
- `ConfiguracaoProfessor` (entidade, `sealed class`) — `Id`, `ProfessorId`,
  `ModeloAgendamento`, `CreatedAt`, `UpdatedAt`. Construtor privado +
  `ConfiguracaoProfessor.Criar(...)` estático (mesmo padrão de `Horario`).
  Expõe `AlterarModelo(ModeloAgendamento novoModelo, IClock clock)` —
  sobrescreve o modelo e `UpdatedAt`, sem guardar o anterior (não
  versionado, conforme RN do card). Expõe `PermiteMarcacaoLivre(bool
  horarioPossuiAtribuicaoFixa)` — regra pura, sem I/O:
  - `Vago` → sempre `true`.
  - `Fixo` → sempre `false`.
  - `Hibrido` → `true` só quando o horário consultado não tem atribuição
    fixa (`!horarioPossuiAtribuicaoFixa`).
- `ModeloAgendamentoNaoDefinidoException` — lançada por `HorarioService`
  quando o Professor ainda não tem `ConfiguracaoProfessor`.
- `IConfiguracaoProfessorRepository` — `BuscarPorProfessorAsync`,
  `AdicionarAsync`, `SalvarAsync` (mesmo padrão de `IHorarioRepository`;
  "alterar" é só mutar a entidade rastreada e chamar `SalvarAsync`, sem
  método de update próprio).
- `ConfiguracaoProfessorService` — orquestra `DefinirModeloAsync`: busca
  configuração existente; se não existe, cria (`Criar`); se existe, chama
  `AlterarModelo`. Único caso de uso do card do ponto de vista de escrita.

**`Synclass.Domain.Horarios`** (ajuste, não novo contexto): `HorarioService`
ganha dependência de `IConfiguracaoProfessorRepository` — `CadastrarAsync`
passa a checar se existe configuração antes de validar duração/conflito;
sem ela, lança `ModeloAgendamentoNaoDefinidoException`. `ListarAsync` e
`RemoverAsync` não mudam (a exigência é só no primeiro cadastro, conforme AC
do card).

**`Synclass.Infrastructure`**: `ConfiguracaoProfessorConfiguration`
(mapeamento EF Core), `ConfiguracaoProfessorRepository`, migration
`CriaConfiguracaoProfessor`, `DbSet<ConfiguracaoProfessor>
ConfiguracoesProfessor` em `SynclassDbContext`.

**`Synclass.Api`**: `ConfiguracoesController` em
`professores/{professorId}/configuracao`, registro de DI em `Program.cs`.
`HorariosController.Criar` ganha um novo `catch
(ModeloAgendamentoNaoDefinidoException)` → 400.

**Frontend**: `src/lib/api/configuracao.ts` (mesmo padrão de
`horarios.ts`), organism `ModeloAgendamentoForm` (seleção entre os 3
modelos), gate em `src/app/professor/[professorId]/horarios.tsx`.

## Contrato de API

Base: `professores/{professorId}/configuracao` (mesmo padrão de guid na
rota usado por `HorariosController`).

- `PUT /professores/{professorId}/configuracao/modelo-agendamento`
  - Request: `{ "modeloAgendamento": 0-2 }`
  - 200 (cria ou altera, operação idempotente): `{ "modeloAgendamento": 0-2 }`
- `GET /professores/{professorId}/configuracao`
  - 200: `{ "modeloAgendamento": 0-2 }`
  - 404: configuração ainda não definida (sem body) — sinal usado pelo
    frontend para mostrar `ModeloAgendamentoForm` em vez da tela normal.

`HorariosController.Criar` (issue #6, já existente): novo caso de erro —
400 `{ "mensagem": string }` quando `ModeloAgendamentoNaoDefinidoException`.

`modeloAgendamento` trafega como inteiro (0-2), mesma decisão já tomada
para `diaSemana` na issue #6 (ver
`docs/specs/6-horarios-disponiveis/implementation.md#contrato-de-api`) —
consistência de convenção, não repete a justificativa aqui.

## Modelo de dados

Nova tabela `ConfiguracoesProfessor`:

| Coluna | Tipo | Observação |
|---|---|---|
| `Id` | `uuid` | PK, gerado pela aplicação (`Guid.NewGuid()`) |
| `ProfessorId` | `uuid` | FK → `Usuarios.Id`, índice único (1 configuração por Professor) |
| `ModeloAgendamento` | `integer` | mapeamento padrão de enum do EF Core |
| `CreatedAt` | `timestamp with time zone` | |
| `UpdatedAt` | `timestamp with time zone` | atualizado em toda chamada de `AlterarModelo` |

A issue #10 (prazo de cancelamento) adiciona `PrazoCancelamentoMinutos`
nesta mesma tabela depois — não antecipado aqui para não introduzir uma
coluna sem consumidor.

## Edge points (não cobertos por Gherkin)

- `AlterarModelo` nunca é bloqueada por alocações existentes — a RN do card
  é explícita que a troca não é retroativa nem desfaz vínculo, então não há
  validação de "existem alunos alocados" aqui (diferente do
  `HorarioService.RemoverAsync`, que bloqueia).
- `ModeloAgendamentoNaoDefinidoException` só é checada em
  `HorarioService.CadastrarAsync` — Professor sem configuração ainda pode
  listar (lista vazia) ou remover horários pré-existentes, se algum dia
  isso for possível por outro caminho.
- `PermiteMarcacaoLivre` é regra pura, sem persistência própria: não é
  chamada por nenhum controller ainda (não há endpoint de marcar/atribuir
  horário até #8/#9 existirem) — fica pronta para ser consumida, mesmo
  padrão do guard rail que a issue #6 criou para `PossuiAlunosAlocadosAsync`
  antes de #8 existir.
- Tela de configuração do Professor: o card descreve como "parte do
  onboarding pós-cadastro (#1)", mas #1 (já mergeado) não introduziu um
  fluxo de onboarding separado — só a tela de cadastro. Em vez de criar um
  fluxo novo sem consumidor hoje, o gate é feito na própria tela
  `professor/[professorId]/horarios.tsx` (que já é o único lugar do
  frontend que depende do modelo estar definido): `GET configuracao` roda
  antes da lista/form de horários; 404 mostra `ModeloAgendamentoForm` no
  lugar. Quando um fluxo de onboarding de fato existir, o formulário pode
  ser reaproveitado lá sem mudar o contrato da Api.

## Dependência das issues #8 e #9

Os critérios de aceite Gherkin 2 e 3 do card ("Aluno tenta marcar horário
livremente é bloqueado no modelo Fixo", "no Híbrido só horários não
atribuídos aparecem como marcáveis") descrevem comportamento de endpoints
que ainda não existem — marcar horário por Aluno (#9) e atribuir horário
por Professor (#8). Este card implementa a regra que esses fluxos vão
consumir (`ConfiguracaoProfessor.PermiteMarcacaoLivre`), testada
isoladamente no Domain contra os dois cenários do enunciado (Fixo bloqueia
sempre; Híbrido bloqueia só o horário com atribuição fixa, os demais
liberados) — mesmo padrão de guard rail que #6 usou para a dependência de
#8. Quando #8/#9 forem implementadas, os `Service`s dessas issues chamam
este método; nenhuma mudança de contrato aqui.
