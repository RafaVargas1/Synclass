# Implementação: prazo de cancelamento configurável por Horário (#187)

## Entidades/classes afetadas

- `backend/src/Synclass.Domain/Horarios/Horario.cs` — novo campo
  `PrazoCancelamentoMinutos` (int, private set) + novo método
  `AlterarPrazoCancelamento(int novoPrazo)`, mesmo padrão de
  `AlterarTipoMarcacao` (linha ~110 do arquivo atual). `Criar(...)` ganha
  parâmetro opcional `int? prazoCancelamentoMinutos = null`, resolvendo
  para `0` quando nulo (mesmo default de
  `ConfiguracaoProfessor.Criar`/comportamento atual). Validação: rejeita
  valor negativo lançando `HorarioRejeitadoException` (reaproveitar a
  hierarquia de exceção já usada por `TipoMarcacaoInvalidoException` —
  confirmar no arquivo se `HorarioRejeitadoException` é a base comum antes
  de decidir a exceção concreta nova, ex: `PrazoCancelamentoInvalidoException`).
- `backend/src/Synclass.Domain/Horarios/HorarioService.cs` — novo método
  `AlterarPrazoCancelamentoAsync(Guid professorId, Guid horarioId, int novoPrazo, CancellationToken)`,
  espelhando `AlterarPoliticaAsync` (busca do Professor via
  `IHorarioRepository`, chama o método de domínio, salva).
- `backend/src/Synclass.Domain/Aulas/AulaService.cs` —
  `GarantirDentroDoPrazoAsync` (linha ~95) e `ListarProximasAsync` (linha
  ~142) trocam a leitura de
  `configuracao?.PrazoCancelamentoMinutos ?? 0` (via
  `IConfiguracaoProfessorRepository`) por `horario.PrazoCancelamentoMinutos`
  — o `Horario` já é buscado nas duas funções (`_horarioService.BuscarDoProfessorAsync`),
  então isso remove a dependência de `_configuracoes`/`IConfiguracaoProfessorRepository`
  nessas duas funções (mas não do construtor de `AulaService` inteiro —
  conferir se `_configuracoes` ainda é usado em outro lugar da classe antes
  de removê-lo do construtor; se não for, remover o campo e o parâmetro do
  construtor, e ajustar o registro de DI/testes que instanciam `AulaService`
  diretamente).
- `backend/src/Synclass.Api/Controllers/HorariosController.cs` — novo
  endpoint `PATCH /professores/{professorId}/horarios/{horarioId}/prazo-cancelamento`,
  mesmo padrão do `AlterarPolitica` (linha ~66): lê o valor anterior antes
  de alterar, loga `HorarioPrazoCancelamentoAlterado {TrackId} {ProfessorId}
  {HorarioId} {PrazoAnterior} {PrazoNovo}`. `CriarHorarioRequest` ganha
  `int? PrazoCancelamentoMinutos = null`; `HorarioResponse` ganha
  `int PrazoCancelamentoMinutos`.
- Migration nova em
  `backend/src/Synclass.Infrastructure/Persistence/Migrations/` — coluna
  `PrazoCancelamentoMinutos int NOT NULL DEFAULT 0` na tabela de
  `Horario`. Rodar `dotnet ef migrations add AdicionaPrazoCancelamentoAoHorario
  --project src/Synclass.Infrastructure --startup-project src/Synclass.Api`
  a partir de `backend/` — não escrever a migration à mão.
- `frontend/src/lib/api/horarios.ts` — `Horario`/`CriarHorarioInput` ganham
  `prazoCancelamentoMinutos: number` (obrigatório no tipo `Horario`
  retornado pela Api, opcional em `CriarHorarioInput`). Nova função
  `alterarPrazoCancelamentoHorario(professorId, horarioId, minutos)`,
  mesmo padrão de `alterarTipoMarcacaoHorario` já existente no mesmo
  arquivo (copiar a função, trocar rota/corpo).
- `frontend/src/components/organisms/HorarioForm.tsx` — novo `FormField`
  "Prazo de cancelamento (minutos)" logo depois do campo "Limite de
  alunos" (linha ~91-97), opcional (campo vazio = 0, mesmo tratamento de
  ausência que os outros campos numéricos do form, mas SEM validação de
  obrigatoriedade — só valida "não negativo" se preenchido). Incluído no
  `CriarHorarioInput` montado em `validar(...)`.
- `frontend/src/components/organisms/HorarioCard.tsx` — o painel de edição
  já existente (`editando`, ativado por "Editar política", ver linhas
  ~49-80 do arquivo atual) ganha um `FormField` numérico "Prazo de
  cancelamento (minutos)" logo abaixo do `ChipSelector` de política,
  inicializado com `horario.prazoCancelamentoMinutos` via novo estado
  `const [prazoSelecionado, setPrazoSelecionado] = useState(String(horario.prazoCancelamentoMinutos))`.
  `handleSalvar` passa a chamar as duas ações — `onAlterarPolitica` (já
  existe) e o novo `onAlterarPrazoCancelamento(horario.id, Number(prazoSelecionado))`
  — na mesma submissão do botão "Salvar" (não dois botões separados: é o
  mesmo painel de edição de configurações do Horário, seguindo o mesmo
  princípio de "não introduzir mais decisões simultâneas que o necessário"
  de `docs/spec/ux-heuristics.md#número-de-opções-simultâneas`). Nova prop
  `onAlterarPrazoCancelamento: (horarioId: string, minutos: number) => void`
  em `HorarioCardProps`.
- `frontend/src/app/professor/[professorId]/horarios.tsx` — novo
  `criarHandleAlterarPrazoCancelamento`, mesmo padrão de
  `criarHandleAlterarPolitica` (chama a função de `lib/api/horarios.ts`,
  atualiza a lista local com o horário retornado), passado como nova prop
  para `HorarioCard`.

## Padrão de estilo a seguir

Siga `AlterarTipoMarcacao`/`AlterarPoliticaAsync`/`AlterarPolitica`
(domínio → serviço → controller) ponta a ponta — é exatamente o mesmo
formato de "propriedade editável depois da criação" que este card
introduz para `PrazoCancelamentoMinutos`, só que numérico em vez de enum.
Não invente uma segunda forma de fazer PATCH parcial de `Horario`.

No frontend, siga o painel de edição já existente em `HorarioCard.tsx`
(`editando`/`handleEditar`/`handleSalvar`) — não crie um segundo modo de
edição separado só para o prazo.

## Contrato de API

`PATCH /professores/{professorId}/horarios/{horarioId}/prazo-cancelamento`
- Request: `AlterarPrazoCancelamentoHorarioRequest(int PrazoCancelamentoMinutos)`
- Response 200: `HorarioResponse` (já existente, com o novo campo)
- 404: horário não existe ou é de outro Professor
- 400: valor negativo

`POST /professores/{professorId}/horarios` (`CriarHorarioRequest`) e
`GET /professores/{professorId}/horarios` (lista de `HorarioResponse`)
ganham o campo `PrazoCancelamentoMinutos`/`prazoCancelamentoMinutos` sem
mudar o formato geral do contrato existente.

## Modelo de dados

`Horario`: nova coluna `PrazoCancelamentoMinutos int NOT NULL DEFAULT 0`.
Sem nova tabela, sem nova relação.

## Edge points

- Horários já existentes no banco recebem `0` via `DEFAULT 0` da migration
  — mesmo comportamento de hoje (`ConfiguracaoProfessor` ausente também
  resolvia para `0`), então nenhum Professor existente tem o
  comportamento de cancelamento alterado silenciosamente pela migration.
- `ConfiguracaoProfessor.PrazoCancelamentoMinutos`/`AlterarPrazoCancelamento`/
  o endpoint `PUT .../configuracao/prazo-cancelamento` (`ConfiguracoesController`)
  **não são removidos nesta Task** — ficam órfãos (nenhum código de
  domínio mais os lê), débito técnico documentado na issue, não resolvido
  aqui para não acoplar migração de leitura + remoção de código no mesmo
  PR.
- Se `AulaService` não usar mais `IConfiguracaoProfessorRepository` em
  nenhum outro método depois desta mudança, remover a dependência do
  construtor (não deixar campo/parâmetro morto) — mas só depois de grep
  confirmar que nenhum outro método da classe ainda usa `_configuracoes`.
