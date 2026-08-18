# Implementação: Aluno vinculado a vários Professores — N:N (#5)

## Natureza desta Task: verificação, não construção

Diferente das Tasks anteriores do épico de contas/convites, a RN desta issue
já é satisfeita pelo desenho existente — não pela lógica em si, mas pela
**ausência** de uma restrição que impediria N:N. A investigação de código
(Domain + Infrastructure) confirmou:

- `Matricula` (`Synclass.Domain.Matriculas`) não tem nenhuma invariante que
  limite quantas linhas existem por `AlunoUsuarioId` — cada `Matricula` é
  independente, identificada pelo próprio `Id`.
- `MatriculaConfiguration` (Infrastructure) só declara um índice único em
  `(ProfessorId, IdentificadorProvisorio)` — nada em `AlunoUsuarioId` sozinho
  nem em `(ProfessorId, AlunoUsuarioId)`. O `HasIndex("AlunoUsuarioId")` que
  aparece nas migrations é o índice de FK padrão do EF Core (não único).
- O edge point do card ("um mesmo par Professor+Aluno nunca tem duas
  matrículas simultâneas — reaproveita a existente") já está implementado
  em `ConviteService`: `GerarAsync` rejeita com `ContatoJaVinculadoException`
  se `BuscarVinculoAsync(professorId, usuario.Id)` já encontra vínculo pleno
  (issue #2, critério de aceite 4); `VincularMatriculaAsync` reaproveita o
  vínculo existente em vez de criar um segundo quando o Aluno aceita um
  convite sem matrícula de origem. Essa checagem é sempre escopada por
  `professorId` — nunca bloqueia um segundo Professor diferente.
- `RegraDeCobranca` (issue #11) é keyed por `MatriculaId` com FK única
  (upsert por matrícula, nunca por Aluno) — cobrança já isolada por
  matrícula, não por identidade de usuário.

O que falta é só **prova formal em teste** de que essas propriedades
realmente compõem para N:N multi-Professor — os testes existentes
(`ConviteServiceTests`, `MatriculaTests`) cobrem cada mecanismo
isoladamente, mas nenhum cenário exercita hoje "o mesmo Aluno, dois
Professores diferentes, ao mesmo tempo".

## Entidades/classes afetadas

Nenhuma classe nova. Alterações são só de teste (Domain) e comentário XML
(documentação):

- `backend/tests/Synclass.Domain.Tests/Matriculas/MatriculaTests.cs` —
  novo teste de coexistência de `Matricula.CriarVinculada` para o mesmo
  `AlunoUsuarioId` com dois `ProfessorId`.
- `backend/tests/Synclass.Domain.Tests/Convites/ConviteServiceTests.cs` —
  dois novos testes: convite de um segundo Professor não afeta o vínculo
  existente com o primeiro (critério de aceite 1); matrícula provisória de
  um Professor permanece intacta quando o Aluno vira pleno em outro
  Professor via convite (critério de aceite 3).
- `backend/tests/Synclass.Domain.Tests/Cobrancas/RegraDeCobrancaServiceTests.cs`
  (ou arquivo cruzado novo, ver "Ordem de execução" em `task.md`) — teste de
  isolamento: regra definida numa matrícula não aparece na consulta da outra
  matrícula do mesmo Aluno (critério de aceite 4).
- `Matricula.cs` / `IMatriculaRepository.cs` — comentário XML citando a
  issue #5 como a Task que formalizou (via teste) a garantia de N:N já
  suportada pelo desenho das issues #2/#3.

## Contrato de API

Nenhum novo. Não há endpoint novo nem alterado — os testes exercitam
`ConviteService`/`Matricula`/`RegraDeCobrancaService` diretamente (Domain),
sem passar por `Synclass.Api`.

## Modelo de dados

Nenhuma migration nova (confirmado pelos Critérios técnicos do card e pela
investigação de `MatriculaConfiguration.cs`): a tabela `Matriculas` (issue
#3) já suporta uma linha por par `(ProfessorId, AlunoUsuarioId)` sem
restrição de unicidade em `AlunoUsuarioId` isolado.

## Edge points

- A checagem de duplicidade (`ContatoJaVinculadoException` /
  `BuscarVinculoAsync`) é sempre escopada por `professorId` — nunca correta
  interpretar sua ausência de disparo para um Professor B como bug: é
  exatamente o comportamento que permite N:N.
- Não há hoje reforço de unicidade a nível de banco para
  `(ProfessorId, AlunoUsuarioId)` — só best-effort na aplicação (mesmo
  padrão de tratamento de corrida documentado em
  `docs/specs/2-convite-whatsapp/implementation.md`). Fora de escopo desta
  Task adicionar esse índice: o card não pede novo comportamento de
  concorrência, e o precedente (issue #2) já aceitou esse nível de garantia
  para o par Professor+Aluno.
- `RegraDeCobranca.BuscarPorMatriculaAsync` já é escopado por `MatriculaId`,
  não por `AlunoUsuarioId` — o teste de isolamento de cobrança confirma essa
  propriedade, não introduz uma nova.
- Frontend: a issue #9 (merged em `main` depois do início desta
  investigação — `git fetch`/fast-forward local durante esta Task) já
  criou `frontend/src/app/aluno/[matriculaId]/professores/[professorId]/horarios.tsx`,
  a primeira tela do lado Aluno. Ela já segue exatamente o padrão que o
  critério técnico deste card pede: rota parametrizada por `matriculaId`
  **e** `professorId` explícitos, sem nenhuma consulta que misture dados
  entre matrículas diferentes do mesmo Aluno — `listarHorariosVagos`/
  `marcarHorario` (`frontend/src/lib/api/marcacoes.ts`) recebem
  `matriculaId` e escopam tudo por ela. Não existe ainda uma tela de
  "seletor de Professor" (dashboard agregando as matrículas de um Aluno) —
  isso é os itens 13/16 do backlog, fora do escopo desta Task, que só pede
  a isolação por `MatriculaId`, já satisfeita pelo precedente de #9.
  Nenhuma alteração de frontend nesta Task.

## Dependência de outras Tasks

O card cita dependência de "acumular papéis" (issue #4) no roadmap amplo,
mas os cenários testados aqui não exigem isso: acumular papéis (#4) é sobre
um mesmo usuário ter papel Professor **e** Aluno simultaneamente; esta Task
é sobre um usuário com papel Aluno vinculado a múltiplos Professores via
múltiplas `Matricula` — eixos independentes. `PapelUsuario.Aluno` já é
idempotente (`AdicionarPapelAlunoIdempotente` em `ConviteService`,
issue #2) para o mesmo usuário aceitar convites de Professores diferentes
sem conflito de papel. Não há bloqueio real na issue #4 (que roda em
paralelo, tocando `Synclass.Domain.Usuarios/`, módulo não tocado por esta
Task).
