# Desenho técnico — Horários disponíveis (#6)

## Entidades/classes afetadas

**`Synclass.Domain.Horarios`** (novo bounded context, espelha `Usuarios`):

- `DiaSemana` (enum) — `Domingo = 0` .. `Sabado = 6`, mesma ordenação de
  `System.DayOfWeek`.
- `Horario` (entidade, `sealed class`) — `Id`, `ProfessorId`, `DiaSemana`,
  `HoraInicio` (`TimeOnly`), `DuracaoMinutos` (`int`), `CreatedAt`. Construtor
  privado + `Horario.Criar(...)` estático (mesmo padrão de `Usuario`).
  Expõe `HoraFim` (computado) e `Sobrepoe(Horario outro)` — lógica pura,
  testável sem repositório.
- `DuracaoAula` (classe estática) — valida duração mínima (1 minuto), mesmo
  padrão de `NomeUsuario`/`Contato`.
- `HorarioRejeitadoException` (abstract base, mesmo padrão de
  `CadastroProfessorRejeitadoException`) com `DuracaoInvalidaException` e
  `HorarioConflitanteException`.
- `HorarioComAlunosAlocadosException` e `HorarioNaoEncontradoException` —
  rejeições específicas do fluxo de remoção (a segunda não é 400 e sim 404,
  ver Contrato de API).
- `IHorarioRepository` — abstrai persistência (Domain não conhece EF Core).
- `HorarioService` — orquestra os 3 casos de uso (criar/listar/remover),
  mesmo papel de `CadastroProfessorService`, mas para as 3 operações do card
  (coesas o bastante para não justificar 3 classes separadas).

**`Synclass.Infrastructure`**: `HorarioConfiguration` (mapeamento EF Core),
`HorarioRepository` (implementação de `IHorarioRepository`), migration
`CriaHorario`, `DbSet<Horario> Horarios` em `SynclassDbContext`.

**`Synclass.Api`**: `HorariosController` em
`professores/{professorId}/horarios`, registro de DI em `Program.cs`.

**Frontend**: `src/lib/api/horarios.ts` (envelope de `fetch`, mesmo padrão de
`professores.ts`), organism `HorarioForm` (criação, com validação de
conflito no cliente), organism `HorarioCard` (item da lista), tela
`src/app/professor/[professorId]/horarios.tsx`.

## Contrato de API

Base: `professores/{professorId}/horarios` (guid na rota, mesmo padrão de
`ProfessoresController`).

- `POST /professores/{professorId}/horarios`
  - Request: `{ "diaSemana": 0-6, "horaInicio": "HH:mm:ss", "duracaoMinutos": number }`
  - 200: `{ "id": guid, "diaSemana": 0-6, "horaInicio": "HH:mm:ss", "duracaoMinutos": number }`
  - 400: `{ "mensagem": string }` (duração inválida ou conflito)
- `GET /professores/{professorId}/horarios`
  - 200: `HorarioResponse[]` (mesmo formato do item acima), ordenado por
    `DiaSemana`, `HoraInicio`.
- `DELETE /professores/{professorId}/horarios/{horarioId}`
  - 204: removido.
  - 404: horário não existe ou não pertence a este Professor.
  - 409: existem Alunos alocados (`{ "mensagem": string }`).

`diaSemana` trafega como inteiro (0-6), não como string — decisão explícita
para bater com a descrição literal do card ("DiaSemana enum 0-6") e manter o
frontend simples (índice direto num array local de nomes de dia em
português, sem depender de nomes de enum em inglês/serialização). Isto
diverge da convenção usada para `PapelUsuario` (que é serializado como
string no banco, embora nunca tenha aparecido em um contrato JSON até agora)
— decisão documentada aqui por não haver um precedente de contrato JSON para
enum no projeto.

## Modelo de dados

Nova tabela `Horarios`:

| Coluna | Tipo | Observação |
|---|---|---|
| `Id` | `uuid` | PK, gerado pela aplicação (`Guid.NewGuid()`, `ValueGeneratedNever()`) |
| `ProfessorId` | `uuid` | FK → `Usuarios.Id`, `OnDelete: Cascade` |
| `DiaSemana` | `integer` | mapeamento padrão de enum do EF Core (int) |
| `HoraInicio` | `time` | `TimeOnly` |
| `DuracaoMinutos` | `integer` | |
| `CreatedAt` | `timestamp with time zone` | |

Índice não-único em (`ProfessorId`, `DiaSemana`) — acelera a consulta de
conflito, que roda a cada criação; não pode ser um índice único porque a
regra de não-sobreposição não é expressável como constraint simples de
unicidade (é um teste de intervalo, não de igualdade).

## Edge points (não cobertos por Gherkin)

- Sobreposição é [`HoraInicio`, `HoraInicio + DuracaoMinutos`) — bordas que
  só se tocam não conflitam (already no card).
- Duração é imutável — sem endpoint de update, só create/delete.
- Um horário que cruza a meia-noite (ex: 23:50 + 20min) não é tratado
  especificamente: `TimeOnly.AddMinutes` dá *wrap-around*, o que quebraria a
  comparação de sobreposição. Não é um cenário do card (aulas não cruzam
  meia-noite no domínio do produto) — assumido fora de escopo, não validado
  nem bloqueado explicitamente.
- `ProfessorId` na rota não é validado contra "esse usuário é de fato um
  Professor" (não há autenticação/login ainda — issue #18 em paralelo). A
  tela recebe `professorId` como *route param* dinâmico
  (`professor/[professorId]/horarios`) em vez de inferir do usuário logado;
  quando #18 mergear, a tela troca a origem do id (navegação) sem mudar o
  contrato da Api. FK do banco garante que o id corresponde a um `Usuario`
  existente (senão a criação falha com erro de integridade referencial).

## Dependência da issue #8

O critério de aceite "horário com Alunos alocados não pode ser removido"
depende de um conceito (Aluno alocado a um horário) que só é modelado na
issue #8, ainda não implementada. Decisão conservadora: `HorarioService`
já expõe o *guard rail* (`IHorarioRepository.PossuiAlunosAlocadosAsync`),
testável no Domain via `FakeHorarioRepository` com o flag setável — cobre o
critério Gherkin do ponto de vista de comportamento. A implementação EF Core
(`HorarioRepository.PossuiAlunosAlocadosAsync`) retorna sempre `false` por
ora (comentário no código referenciando a issue #8): não existe tabela de
alocação ainda para consultar de verdade. Quando #8 for implementada, essa
implementação troca para uma consulta real — o contrato do método e o
comportamento do `HorarioService` não mudam.
