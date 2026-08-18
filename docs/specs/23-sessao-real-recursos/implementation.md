# Implementação: sessão real nos endpoints de #3/#8/#9 (#23)

## Contexto e decisões de domínio (sem resposta óbvia no card)

O card #23 lista "trocar `professorId`/`matriculaId` de parâmetro de rota
por leitura da sessão" para os três controllers, mas nem todo `professorId`
de rota representa a mesma coisa — duas decisões foram necessárias sem um
usuário disponível para confirmar em tempo real (lote autônomo); ambas
tomadas pelo lado mais conservador (fecha a lacuna de segurança real, sem
inventar fluxo de navegação novo fora do escopo do card):

1. **Em `AlunosProvisoriosController`/`AlocacoesHorarioController`,
   `professorId` sempre foi a identidade de quem chama** (um Professor
   gerencia os próprios Alunos/horários) — não há conceito de "Professor
   agindo sobre outro Professor". Por isso o parâmetro de rota é **removido
   por completo**, não só validado: `professorId` passa a vir só de
   `User.GetUsuarioId()` (claim do JWT).
2. **Em `MarcacoesHorarioController`, `professorId` é o Professor sendo
   navegado pelo Aluno** (não a identidade de quem chama) — trocar por
   sessão não faz sentido aqui, a sessão do Aluno não carrega "qual
   Professor estou vendo agora". O parâmetro de rota **permanece**. O que
   de fato precisa de sessão é `matriculaId`: hoje é enviado pelo cliente
   sem checagem de posse (qualquer Aluno autenticado podia mandar a
   `matriculaId` de outro Aluno). Fix: `matriculaId` deixa de ser
   parâmetro do cliente — é resolvido no servidor via
   `IMatriculaRepository.BuscarVinculoAsync(professorId, alunoUsuarioId)`
   (já existente, usado por `ConviteService` desde a issue #2/#5), usando
   `alunoUsuarioId` de `User.GetUsuarioId()`. Isso satisfaz a letra do card
   ("matriculaId... por leitura da sessão autenticada") sem exigir uma
   tela nova de seleção de Professor (fora de escopo, tema de #33/futuro).

## Entidades/classes afetadas

**Api** (`Synclass.Api/`):

- `ClaimsPrincipalExtensions.cs` (novo) — `GetUsuarioId(this ClaimsPrincipal)`
  lê `ClaimTypes.NameIdentifier` (não `"sub"`: `JwtBearerOptions` não
  desliga `MapInboundClaims`, então o handler já remapeia `sub` →
  `ClaimTypes.NameIdentifier` antes do controller ver a claim) e faz
  `Guid.Parse`. Único ponto de leitura de identidade da sessão — evita
  `User.FindFirst(...)` espalhado e divergente entre os três controllers.
- `AlunosProvisoriosController`: rota de classe
  `professores/{professorId:guid}/alunos-provisorios` →
  `professores/alunos-provisorios`; `Cadastrar`/`Listar` trocam o parâmetro
  de rota `Guid professorId` por `User.GetUsuarioId()`.
- `AlocacoesHorarioController`: rota de classe
  `professores/{professorId:guid}/horarios/{horarioId:guid}/alocacoes` →
  `professores/horarios/{horarioId:guid}/alocacoes`; `Alocar`/`Listar`/
  `Desalocar` idem. Nenhuma mudança de negócio necessária além disso: a
  checagem já existente `HorarioService.BuscarDoProfessorAsync`/
  `AlocacaoHorarioService.GarantirMatriculaVinculadaAsync` já rejeita
  `horarioId`/`matriculaId` que não pertencem ao `professorId` recebido —
  ela virava checagem de posse de verdade agora que `professorId` não é
  mais adulterável pelo cliente.
- `MarcacoesHorarioController`: rota de classe inalterada
  (`professores/{professorId:guid}/horarios`). `ListarVagos` perde o
  parâmetro de query `matriculaId`; `Marcar` perde `MatriculaId` do body
  (`CriarMarcacaoHorarioRequest` fica sem campos — removido, ação sem
  corpo). Os dois resolvem `matriculaId` via
  `AlocacaoHorarioService.ResolverMatriculaDoAlunoAsync`.

**Domain** (`Synclass.Domain/Alocacoes/`):

- `AlocacaoHorarioService.ResolverMatriculaDoAlunoAsync(Guid professorId, Guid alunoUsuarioId, CancellationToken)`
  — usa `IMatriculaRepository.BuscarVinculoAsync`; lança
  `AlunoNaoVinculadoAoProfessorException` (novo, mesma forma de
  `MatriculaNaoVinculadaAoProfessorException`) se não houver vínculo.
  Chamado no início de `ListarVagosAsync`/`MarcarAsync` do controller (não
  dentro do service existente, para não mudar a assinatura pública usada
  por `AlocarAsync`/`DesalocarAsync`/`ListarPorHorarioAsync`, que
  continuam recebendo `matriculaId` já resolvido pelo controller de
  `AlocacoesHorarioController`, papel de Professor).

**Frontend** (`frontend/src/`):

- `lib/api/alunosProvisorios.ts`: `CadastroAlunoProvisorioInput` perde
  `professorId`; `cadastrarAlunoProvisorio`/`listarAlunosProvisorios` usam
  caminho fixo `/professores/alunos-provisorios`.
- `app/professor/[professorId]/alunos/cadastro.tsx` →
  `app/professor/alunos/cadastro.tsx` (Expo Router: mover arquivo remove o
  segmento da rota). Nenhum call site de navegação apontava para esta tela
  ainda (`grep` no repo não achou `router.push`/`href` para
  `alunos/cadastro`), então não há link a atualizar.
- `lib/api/alocacoes.ts`: `alocarAluno`/`listarAlocacoes`/`desalocarAluno`
  perdem `professorId`, caminho fixo
  `/professores/horarios/${horarioId}/alocacoes`.
- `app/professor/[professorId]/alocacoes.tsx`: continua lendo
  `professorId` da rota (ainda usado por `listarHorarios`/
  `obterConfiguracao`, `HorariosController`/`ConfiguracoesController` fora
  de escopo desta Task) — só para de repassá-lo para
  `alocarAluno`/`desalocarAluno`/`listarAlocacoes`/`listarAlunosProvisorios`.
- `lib/api/marcacoes.ts`: `listarHorariosVagos`/`marcarHorario` perdem
  `matriculaId`.
- `app/aluno/[matriculaId]/professores/[professorId]/horarios.tsx` →
  `app/aluno/professores/[professorId]/horarios.tsx` (remove só
  `[matriculaId]`, mantém `[professorId]`). Mesma checagem de call sites:
  nenhuma encontrada.

## Contrato de API

- `POST /professores/{professorId}/alunos-provisorios` → `POST /professores/alunos-provisorios`
- `GET /professores/{professorId}/alunos-provisorios` → `GET /professores/alunos-provisorios`
- `POST /professores/{professorId}/horarios/{horarioId}/alocacoes` → `POST /professores/horarios/{horarioId}/alocacoes`
- `GET /professores/{professorId}/horarios/{horarioId}/alocacoes` → `GET /professores/horarios/{horarioId}/alocacoes`
- `DELETE /professores/{professorId}/horarios/{horarioId}/alocacoes/{matriculaId}` → `DELETE /professores/horarios/{horarioId}/alocacoes/{matriculaId}`
- `GET /professores/{professorId}/horarios/vagos?matriculaId=` → `GET /professores/{professorId}/horarios/vagos` (rota inalterada, query removida)
- `POST /professores/{professorId}/horarios/{horarioId}/marcacoes` (body `{matriculaId}` → sem body)

Todos os cinco primeiros endpoints continuam exigindo
`Authorization: Bearer <token>` com `role=Professor` (inalterado desde #4);
os dois últimos continuam exigindo `role=Aluno`. Novo comportamento: sem
vínculo (`Matricula`/`Horario` não pertence ao chamador) → `404`, mesmo
código já usado para `HorarioNaoEncontradoException` — não se distingue
"não existe" de "não é seu" na resposta, mesmo padrão de segurança já usado
por `MatriculaNaoVinculadaAoProfessorException`.

## Modelo de dados

Nenhuma migration — reaproveita `Usuario.Id`/`Matricula.ProfessorId`/
`Matricula.AlunoUsuarioId` já existentes.

## Edge points

- `ProfessoresController` (`POST /professores/cadastro`) fica fora de
  escopo — não tem `professorId` de rota (é quem cria a identidade), segue
  público.
- `HorariosController`, `RegraDeCobrancaController`, `ConfiguracoesController`,
  `ConvitesController`: fora de escopo do card #23 (não listados), mantêm
  `professorId` de rota como está — inconsistência de convenção de URL
  entre eles e `AlunosProvisoriosController`/`AlocacoesHorarioController` é
  aceita como resultado de uma migração parcial e intencional, não erro.
- `ClaimsPrincipalExtensions.GetUsuarioId()` lança se a claim estiver
  ausente/inválida — não deveria ser alcançável com `[Authorize]` já
  validando o token antes da action rodar; mantido como defesa, não como
  caminho de erro de negócio (sem teste de fumaça dedicado, mesmo padrão de
  outras invariantes "impossíveis" do código-base).

## Débito técnico encontrado em revisão (fora de escopo desta Task)

`dev-review` do PR #36 apontou que `ResolverMatriculaDoAlunoAsync` (novo
nesta Task) assume que existe no máximo uma `Matricula` por par
`(ProfessorId, AlunoUsuarioId)` — pressuposto que `BuscarVinculoAsync`
(`FirstOrDefaultAsync`, sem índice único no par) não garante. A causa raiz
não é código desta Task: `ConviteService.VincularMatriculaAsync` (issue #2)
promove `matriculaOrigem` quando o convite tem `MatriculaId` explícito sem
checar se o Aluno já tem outro vínculo com o mesmo Professor — decisão
documentada e testada em `docs/specs/2-convite-whatsapp/implementation.md`
("nunca cria uma segunda linha... conforme a RN", especificamente para o
caso de promoção direta). Mudar esse comportamento é uma decisão de
produto (o que fazer quando já existe vínculo: rejeitar o convite? mesclar
matrículas? ignorar a nova?), não uma correção técnica local — por isso não
alterado aqui, mesmo padrão desta própria Task nascendo de um débito
documentado em vez de corrigido inline nos PRs #22/#30/#32. Rastreado para
decisão de produto: ver issue nova criada a partir deste achado.

## Dependência de outras Tasks

Depende conceitualmente de #4 (mergeada — `[Authorize]`/JWT no backend,
`SessaoProvider`/`useSessao()` no frontend) e #5/#33 (mergeadas — relação
N:N Aluno↔Professor via múltiplas `Matricula`, sustenta
`BuscarVinculoAsync`). Sem sobreposição de arquivo esperada com as issues
#10/#12 rodando em paralelo no mesmo lote — controllers tocados aqui não
aparecem nos specs delas; se algum PR delas mergear primeiro tocando os
mesmos controllers, rebase da branch desta Task antes de prosseguir a
revisão.
