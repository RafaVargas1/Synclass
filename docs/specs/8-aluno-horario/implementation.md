# Desenho técnico — Aluno em horário específico (#8)

## Entidades/classes afetadas

**`Synclass.Domain.Alocacoes`** (novo bounded context, espelha `Horarios`):

- `AlocacaoHorario` (entidade, `sealed class`) — `Id`, `HorarioId`, `MatriculaId`,
  `CreatedAt`. Construtor privado + `AlocacaoHorario.Criar(...)` estático,
  mesmo padrão de `Horario`/`Matricula`. Entidade "burra": não valida nada
  sozinha (modelo, limite, vínculo) — toda regra de negócio fica em
  `AlocacaoHorarioService`, que já precisa buscar `Horario`/`ConfiguracaoProfessor`/
  `Matricula` para orquestrar, então validar ali evita duplicar consultas.
- `AlocacaoRejeitadaException` (abstract base, mesmo papel de
  `HorarioRejeitadoException`/`MatriculaRejeitadaException`) com 4 subtipos,
  todos mapeados para 400 pelo controller:
  - `ModeloNaoPermiteAlocacaoException` — modelo Vago.
  - `HorarioLotadoException` — `ContarPorHorarioAsync >= Horario.LimiteAlunos`.
  - `MatriculaNaoVinculadaAoProfessorException` — matrícula inexistente ou de
    outro Professor.
  - `AlocacaoJaExisteException` — mesmo Aluno já alocado neste horário
    (guard rail de aplicação; o índice único do banco é o guard rail final
    para a corrida concorrente, mesmo padrão de `MatriculaConcorrenteException`).
- `AlocacaoNaoEncontradaException` — não é rejeição de negócio, mapeada para
  404 pelo controller (mesmo papel de `HorarioNaoEncontradoException`):
  lançada por `DesalocarAsync` quando não existe alocação daquele Aluno
  naquele horário.
- `IAlocacaoHorarioRepository` — abstrai persistência.
- `AlocacaoHorarioService` — orquestra `AlocarAsync`/`DesalocarAsync`/
  `ListarPorHorarioAsync`. Depende de `HorarioService` (não de
  `IHorarioRepository` diretamente) para reaproveitar a checagem de posse
  "horário pertence a este Professor" já implementada ali — ver
  `HorarioService.BuscarDoProfessorAsync`, que passa de `private` para
  `internal` (mesmo assembly `Synclass.Domain`) só para isto, evitando
  duplicar a lógica (`docs/spec/code-style.md` — sem duplicação de código).

**`Synclass.Domain.Matriculas`** (extensão, não novo contexto):

- `IMatriculaRepository` ganha `BuscarPorIdAsync(Guid matriculaId, ...)`
  (checar vínculo com o Professor antes de alocar) e
  `ListarPorProfessorAsync(Guid professorId, ...)` (alimentar o seletor de
  Aluno do frontend — não existia nenhum endpoint de listagem de Matriculas
  antes desta issue). Extensão aditiva; se a issue #2 (convite WhatsApp, em
  paralelo) mergear primeiro e também tiver estendido esta interface, o
  conflito de merge é resolvível (métodos diferentes, mesmo arquivo).

**`Synclass.Infrastructure`**: `AlocacaoHorarioConfiguration` (mapeamento EF
Core), `AlocacaoHorarioRepository` (implementação de
`IAlocacaoHorarioRepository`), migration `CriaAlocacaoHorario`,
`DbSet<AlocacaoHorario> AlocacoesHorario` em `SynclassDbContext`. Também:
`HorarioRepository.PossuiAlunosAlocadosAsync` troca o stub (`false` fixo,
issue #6) por uma consulta real em `AlocacoesHorario` — ver "Dependência da
issue #6" abaixo. `MatriculaRepository` ganha as duas implementações novas.

**`Synclass.Api`**: `AlocacoesHorarioController`, novo, em
`professores/{professorId}/horarios/{horarioId}/alocacoes`. Extensão em
`AlunosProvisoriosController`: `GET` (listagem) além do `POST` já existente.
Registro de DI em `Program.cs`.

**Frontend**: `src/lib/api/alocacoes.ts` (alocar/listar/desalocar, mesmo
envelope de `horarios.ts`), `listarAlunosProvisorios` novo em
`alunosProvisorios.ts`. Organism `HorarioAlocacaoCard` (grade: vagas
ocupadas/total, lista de Alunos alocados com botão remover, seletor
`ChipSelector` dos Alunos ainda não alocados naquele horário + botão
"Alocar"). Tela nova `src/app/professor/[professorId]/alocacoes.tsx` (mesmo
gate de `ConfiguracaoProfessor` de `horarios.tsx`, mas bloqueando o
conteúdo — não escondendo a tela — quando o modelo é Vago, com mensagem
explicando que esse modelo não usa atribuição fixa).

## Contrato de API

- `POST /professores/{professorId}/horarios/{horarioId}/alocacoes`
  - Request: `{ "matriculaId": guid }`
  - 200: `{ "id": guid, "horarioId": guid, "matriculaId": guid, "createdAt": datetime }`
  - 400: `{ "mensagem": string }` — modelo Vago, horário lotado, Aluno não
    vinculado ao Professor, ou Aluno já alocado neste horário.
  - 404: horário não existe ou não pertence a este Professor (`{}`, sem
    corpo — mesmo padrão de `DELETE /horarios/{id}`).
- `GET /professores/{professorId}/horarios/{horarioId}/alocacoes`
  - 200: `AlocacaoResponse[]` (mesmo formato do item acima).
  - 404: horário não existe ou não pertence a este Professor.
- `DELETE /professores/{professorId}/horarios/{horarioId}/alocacoes/{matriculaId}`
  - 204: desalocado.
  - 404: horário não existe/não pertence, ou não há alocação deste Aluno
    neste horário (os dois casos não são distinguidos na resposta —
    idempotência do "não existe" não importa para o Professor, que só vê
    "esse vínculo não existe").
- `GET /professores/{professorId}/alunos-provisorios` (novo, para o
  seletor do frontend — não tinha listagem antes desta issue)
  - 200: `[{ "matriculaId": guid, "nome": string, "identificador": string }]`

`AlocacaoResponse` não inclui nome/identificador do Aluno (mesma decisão de
`HorarioResponse` não incluir dados de outro contexto) — o frontend cruza
com a resposta de `GET alunos-provisorios`, já buscada para popular o
seletor.

## Modelo de dados

Nova tabela `AlocacoesHorario`:

| Coluna | Tipo | Observação |
|---|---|---|
| `Id` | `uuid` | PK, gerado pela aplicação (`Guid.NewGuid()`, `ValueGeneratedNever()`) |
| `HorarioId` | `uuid` | FK → `Horarios.Id`, `OnDelete: Cascade` — remover o template recorrente remove suas alocações; na prática nunca dispara via fluxo normal, já que `HorarioService.RemoverAsync` bloqueia remoção com Alunos alocados (issue #6), mas é a semântica correta se um horário for removido por outro caminho no futuro |
| `MatriculaId` | `uuid` | FK → `Matriculas.Id`, `OnDelete: Restrict` — não existe remoção de Matrícula ainda; `Restrict` evita apagar silenciosamente uma alocação se isso mudar |
| `CreatedAt` | `timestamp with time zone` | |

Índice único em (`HorarioId`, `MatriculaId`) — impede duplicidade a nível de
banco (guard rail final contra a corrida concorrente; `AlocacaoJaExisteException`
já cobre o caminho feliz de checagem prévia na aplicação).

## Edge points (não cobertos por Gherkin)

- Alocação é sempre a um `Horario` (template recorrente), nunca a uma `Aula`
  (ocorrência datada — conceito só instanciado a partir da issue #10).
  "Desfazer atribuição" (AC) remove a linha de `AlocacaoHorario` inteira,
  diferente de um cancelamento pontual de uma data (issue #10), que age
  sobre uma `Aula` específica sem tocar na alocação recorrente.
- `ConfiguracaoProfessor` ausente ao tentar alocar: na prática não deveria
  acontecer (criar um `Horario` já exige configuração definida, issue #7),
  mas `AlocacaoHorarioService` trata defensivamente como modelo Vago
  (rejeita com `ModeloNaoPermiteAlocacaoException`) em vez de lançar uma
  exceção não relacionada — mesma politica conservadora de "falhar do jeito
  mais claro para o caller", sem introduzir um novo tipo de exceção só para
  um estado que não deveria existir.
- `GET /professores/{professorId}/alunos-provisorios` lista todas as
  Matrículas do Professor (provisórias e plenas — a distinção não importa
  para fins de alocação, qualquer Matrícula vinculada é "um Aluno deste
  Professor"). Implementado direto no controller via `IMatriculaRepository`
  (sem um serviço dedicado): é leitura pura sem regra de negócio, mesmo
  padrão simplificado de `HorarioService.ListarAsync`, que também é só um
  passthrough — criar um serviço só para uma listagem seria over-engineering
  nesta Task, que já é grande.
  `professorId` na rota, sem sessão — mesma decisão já documentada em
  `docs/specs/3-aluno-provisorio/implementation.md` (login, issue #18, ainda
  não plugado nas rotas de Professor).
- Corrida concorrente na alocação (dois requests simultâneos preenchendo a
  última vaga, ou alocando o mesmo Aluno duas vezes): a checagem de
  `ContarPorHorarioAsync`/`BuscarAsync` na aplicação não é atômica com o
  `INSERT`. O índice único cobre a duplicidade (o segundo `SaveChangesAsync`
  falha com `DbUpdateException`, tratado como `AlocacaoJaExisteException` —
  mesmo padrão de `MatriculaRepository.SalvarAsync`/`MatriculaConcorrenteException`).
  O limite de vagas (`HorarioLotadoException`) não tem um guard rail
  equivalente a nível de banco (não é uma constraint de unicidade, é uma
  contagem) — janela de corrida rara aceita como risco conhecido, mesma
  postura já adotada para conflito de horário na issue #6 (não há lock
  explícito).

## Dependência da issue #6 (agora resolvida)

`IHorarioRepository.PossuiAlunosAlocadosAsync` era um stub sempre-`false`
desde a issue #6 (ver `docs/specs/6-horarios-disponiveis/implementation.md#dependência-da-issue-8`),
porque não existia tabela de alocação para consultar. Esta issue implementa
a consulta real em `HorarioRepository` (`Synclass.Infrastructure`):

```csharp
public Task<bool> PossuiAlunosAlocadosAsync(Guid horarioId, CancellationToken cancellationToken)
{
    return _dbContext.Set<AlocacaoHorario>().AnyAsync(a => a.HorarioId == horarioId, cancellationToken);
}
```

O contrato do método e o comportamento de `HorarioService.RemoverAsync` não
mudam — só a implementação EF Core, exatamente como a issue #6 previu.

## Dependência para a issue #17

`Horario.AlterarLimiteAlunos(novoLimite, quantidadeAlunosAlocados)` (issue
#17) já existe no domínio mas nenhum endpoint chama — ligar isso a um
endpoint de edição de limite fica fora do escopo desta issue (não é um
Critério de aceite do card #8). `AlocacaoHorarioService` só *lê* a
contagem (`ContarPorHorarioAsync`) para validar vaga disponível; não chama
`AlterarLimiteAlunos`.
