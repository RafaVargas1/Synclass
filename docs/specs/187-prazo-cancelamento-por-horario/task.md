# Task: prazo de cancelamento configurável por Horário (#187)

Card: https://github.com/RafaVargas1/Synclass/issues/187

## Ordem de execução

- [x] Teste unidade (Domain, `HorarioTests`): `Horario.Criar` aceita `prazoCancelamentoMinutos` opcional, default 0 quando nulo
- [x] Teste unidade (Domain, `HorarioTests`): `AlterarPrazoCancelamento` atualiza o valor
- [x] Teste unidade (Domain, `HorarioTests`): `AlterarPrazoCancelamento` rejeita valor negativo
- [x] Implementação mínima: `Horario.PrazoCancelamentoMinutos` + `AlterarPrazoCancelamento` + parâmetro em `Criar`
- [x] Teste unidade (Domain, `HorarioServiceTests`): `AlterarPrazoCancelamentoAsync` delega para o método de domínio e salva
- [x] Implementação mínima: `HorarioService.AlterarPrazoCancelamentoAsync`
- [x] Teste unidade (Domain, `AulaServiceTests`): cenário Gherkin 1 — Professor define prazo de um Horário, cancelamento fora do prazo é rejeitado
- [x] Teste unidade (Domain, `AulaServiceTests`): cenário Gherkin 2 — dois Horários do mesmo Professor com prazos diferentes, cada um respeita o próprio prazo
- [x] Teste unidade (Domain, `AulaServiceTests`): cenário Gherkin 3 — Horário sem prazo configurado usa 0 (comportamento atual)
- [x] Implementação: `AulaService.GarantirDentroDoPrazoAsync`/`ListarProximasAsync` leem `horario.PrazoCancelamentoMinutos` em vez de `ConfiguracaoProfessor` — remover a dependência de `IConfiguracaoProfessorRepository` do construtor se nenhum outro método da classe ainda a usar (grep antes de remover)
- [x] Migration: coluna `PrazoCancelamentoMinutos int NOT NULL DEFAULT 0` em `Horario` (via `dotnet ef migrations add`, não escrita à mão)
- [x] Teste de fumaça (Api, `HorariosEndpointTests` ou arquivo novo): `POST .../horarios` aceita `PrazoCancelamentoMinutos` e devolve no `HorarioResponse`
- [x] Teste de fumaça (Api): `PATCH .../horarios/{horarioId}/prazo-cancelamento` altera o valor, 404 se horário não existe/não é do Professor, 400 se negativo
- [x] Implementação: `HorariosController` — `CriarHorarioRequest`/`HorarioResponse` ganham o campo; novo endpoint `AlterarPrazoCancelamento`
- [x] Log estruturado: evento `HorarioPrazoCancelamentoAlterado {TrackId} {ProfessorId} {HorarioId} {PrazoAnterior} {PrazoNovo}` (ver architecture.md#logs-estruturados-e-track-id)
- [x] Componente frontend: `frontend/src/lib/api/horarios.ts` — `Horario`/`CriarHorarioInput` ganham `prazoCancelamentoMinutos`; nova função `alterarPrazoCancelamentoHorario(professorId, horarioId, minutos)` espelhando `alterarTipoMarcacaoHorario`
- [x] Componente frontend: `HorarioForm.tsx` — novo `FormField` "Prazo de cancelamento (minutos)" após o campo "Limite de alunos" (linha ~91-97 do arquivo atual), opcional, incluído em `CriarHorarioInput`
- [x] Componente frontend: `HorarioCard.tsx` — painel de edição existente (`editando`) ganha `FormField` numérico "Prazo de cancelamento (minutos)" abaixo do `ChipSelector` de política, inicializado com `horario.prazoCancelamentoMinutos`; `handleSalvar` chama também a nova prop `onAlterarPrazoCancelamento(horarioId, minutos)`
- [x] Componente frontend: `frontend/src/app/professor/[professorId]/horarios.tsx` — `criarHandleAlterarPrazoCancelamento` (mesmo padrão de `criarHandleAlterarPolitica`), passado para `HorarioCard`
- [x] Rodar gate completo (`dotnet format && dotnet test`, `npm run lint && npm run typecheck && npm test`) antes do PR
