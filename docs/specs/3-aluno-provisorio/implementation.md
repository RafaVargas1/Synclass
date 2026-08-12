# Implementação: Aluno provisório (#3)

## Entidades/classes afetadas

**Domain** (`Synclass.Domain.Matriculas`, novo módulo — paralelo a
`Synclass.Domain.Usuarios`, sem alterar nenhum arquivo existente de
`Usuarios/` para não colidir com o trabalho paralelo da issue #18):

- `Matricula` — entidade nova. `Id`, `ProfessorId`, `AlunoUsuarioId?`
  (nulo enquanto provisória), `NomeProvisorio?`, `IdentificadorProvisorio?`,
  `CreatedAt`. Factory `CriarProvisoria(...)`; método `Promover(alunoUsuarioId)`
  (usado pelo fluxo de convite da issue #2 — aqui só o método de domínio e
  seu teste, sem endpoint de promoção).
- `IdentificadorProvisorio` — validador estático (mesmo padrão de
  `NomeUsuario`): não vazio, trim, máximo 60 caracteres.
- `IMatriculaRepository` — `BuscarPorIdentificadorAsync(professorId, identificador, ct)`,
  `AdicionarAsync`, `SalvarAsync`.
- `CadastroAlunoProvisorioService` — orquestra validação de nome (reusa
  `Synclass.Domain.Usuarios.NomeUsuario.Validar`, só leitura, sem editar
  arquivos de `Usuarios/`) + identificador, checagem de duplicidade
  por Professor, criação.
- `MatriculaRejeitadaException` (abstract, mesma função de
  `CadastroProfessorRejeitadoException` na issue #1) e subtipos:
  `NomeProvisorioInvalidoException`, `IdentificadorProvisorioInvalidoException`,
  `IdentificadorProvisorioDuplicadoException`, `MatriculaJaPromovidaException`,
  `MatriculaConcorrenteException` (corrida de índice único, mesmo padrão de
  `CadastroConcorrenteException`), `ProfessorNaoEncontradoException`
  (`professorId` sem `Usuario` correspondente — checado explicitamente antes
  de salvar, ao invés de deixar a violação de FK do Postgres ser confundida
  com `MatriculaConcorrenteException`; achado de dev-review no PR #22).

**Infrastructure**: `MatriculaConfiguration` (índice único parcial em
`(ProfessorId, IdentificadorProvisorio)` via `HasFilter`), `MatriculaRepository`
(EF Core), `DbSet<Matricula> Matriculas` em `SynclassDbContext`, migration
`CriaMatricula`.

**Api**: `AlunosProvisoriosController` — `POST
/professores/{professorId}/alunos-provisorios`.

**Frontend**: `frontend/src/lib/api/alunosProvisorios.ts` (mesmo padrão de
`professores.ts` — nunca lança, sempre devolve resultado tipado, timeout de
15s); organism `CadastroAlunoProvisorioForm`; tela
`frontend/src/app/professor/[professorId]/alunos/cadastro.tsx`.

## Decisão documentada: `professorId` como parâmetro de rota, não de sessão

Este card pressupõe um Professor autenticado cadastrando seu próprio Aluno.
Mas login (issue #18) roda em paralelo e ainda não está mergeado em `main` —
não existe sessão de onde ler o `ProfessorId` atual. Decisão conservadora:
o endpoint recebe `professorId` explicitamente na rota
(`/professores/{professorId}/alunos-provisorios`), sem validar que o chamador
*é* aquele Professor (não há mecanismo de auth ainda para essa checagem — a
mesma limitação já aceita no cadastro de Professor da issue #1, que também
não tem auth). O frontend espelha isso com um segmento dinâmico
`[professorId]` na rota, em vez de embutir a tela numa área logada que ainda
não existe.

Isso é rastreável, não é ambiguidade de regra de negócio (a RN do card não
fala de auth) — não bloqueia o fluxo. Abrir issue de acompanhamento
`[Revisar] Ligar professorId de sessão real no cadastro de Aluno provisório
quando #18 mergear` para não perder o rastro do que precisa mudar depois
(trocar o parâmetro de rota por leitura de sessão, e então sim validar que o
chamador é o próprio Professor).

## Contrato de API

`POST /professores/{professorId}/alunos-provisorios`

Request:
```json
{ "nome": "João Pedro", "identificador": "2024-013" }
```

Response 200:
```json
{ "matriculaId": "guid", "nome": "João Pedro", "identificador": "2024-013" }
```

Response 400 (nome/identificador inválido, identificador duplicado, ou
conflito de concorrência):
```json
{ "mensagem": "..." }
```

Response 404 (`professorId` sem `Usuario` correspondente):
```json
{ "mensagem": "..." }
```

## Modelo de dados

Nova tabela `Matriculas`:

| Coluna | Tipo | Notas |
|---|---|---|
| `Id` | uuid PK | gerado pela aplicação |
| `ProfessorId` | uuid FK → `Usuarios.Id` | `Restrict` no delete |
| `AlunoUsuarioId` | uuid FK → `Usuarios.Id`, nullable | nulo enquanto provisória |
| `NomeProvisorio` | varchar(200), nullable | |
| `IdentificadorProvisorio` | varchar(60), nullable | |
| `CreatedAt` | timestamptz | |

Índice único parcial em `(ProfessorId, IdentificadorProvisorio)` onde
`IdentificadorProvisorio IS NOT NULL`.

## Edge points

- Unicidade do identificador é por `(ProfessorId, IdentificadorProvisorio)`,
  nunca global — dois Professores podem repetir o número.
- Nome vazio/só espaços inválido, igual à issue #1 (reuso de
  `NomeUsuario.Validar`, mensagem original preservada).
- `Promover` nunca cria segunda linha — só define `AlunoUsuarioId` na
  `Matricula` existente; rejeita se já promovida.
- Corrida de concorrência no índice único (duas requisições simultâneas com o
  mesmo identificador) tratada como `MatriculaConcorrenteException`, mesmo
  padrão de `CadastroConcorrenteException` da issue #1.
- `professorId` inexistente é checado via `IUsuarioRepository.ExisteAsync`
  antes de criar a `Matricula`, e rejeitado com `ProfessorNaoEncontradoException`
  (HTTP 404) — não confundir com o 400 de `MatriculaConcorrenteException`. O
  EF Core InMemory usado nos testes de integração não aplica a FK
  `Matriculas.ProfessorId → Usuarios.Id` como o Postgres aplicaria, então essa
  checagem explícita é a única rede de segurança testável em CI.

## Dependência de outras Tasks

Nenhuma — issue #3 é independente das issues #18 (login) e #6 (horários) que
rodam em paralelo. A promoção via convite (issue #2, ainda não implementada)
consumirá `Matricula.Promover` como está.
