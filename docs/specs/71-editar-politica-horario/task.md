# Task: Professor edita a política de marcação de um Horário já cadastrado (#71)

Card: https://github.com/RafaVargas1/Synclass/issues/71

## Ordem de execução

- [ ] Teste unidade (Domain, `HorarioTests.cs`): `AlterarTipoMarcacao` troca a política de um Horário existente para cada um dos 3 valores válidos (`Livre`/`Fixo`/`Hibrido`).
- [ ] Teste unidade (Domain, `HorarioTests.cs`): `AlterarTipoMarcacao` rejeita valor fora do enum com `TipoMarcacaoInvalidoException` (mesma exceção já usada em `Criar`).
- [ ] Implementação mínima: `Horario.AlterarTipoMarcacao(TipoMarcacao novo)` em `backend/src/Synclass.Domain/Horarios/Horario.cs`, reaproveitando o `private static void ValidarTipoMarcacao` já existente.
- [ ] Teste unidade (Domain, `HorarioServiceTests.cs`): `AlterarPoliticaAsync` persiste a nova política quando o horário pertence ao Professor.
- [ ] Teste unidade (Domain, `HorarioServiceTests.cs`): `AlterarPoliticaAsync` lança `HorarioNaoEncontradoException` quando o horário não existe ou não é do Professor.
- [ ] Implementação mínima: `HorarioService.AlterarPoliticaAsync(Guid professorId, Guid horarioId, TipoMarcacao novo, CancellationToken cancellationToken)` em `backend/src/Synclass.Domain/Horarios/HorarioService.cs`, reaproveitando `BuscarDoProfessorAsync` (já `internal`) e `SalvarAsync`.
- [ ] Teste de fumaça (Api, `HorarioEndpointTests.cs`): `PATCH /professores/{professorId}/horarios/{horarioId}` retorna 200 com o `HorarioResponse` atualizado quando a política muda.
- [ ] Teste de fumaça (Api, `HorarioEndpointTests.cs`): o mesmo endpoint retorna 404 para horário de outro Professor/inexistente, e 400 para `tipoMarcacao` fora do enum.
- [ ] Implementação mínima: endpoint `PATCH` em `backend/src/Synclass.Api/Controllers/HorariosController.cs` (`AlterarPoliticaHorarioRequest(int TipoMarcacao)`), tratando `HorarioNaoEncontradoException` → 404 e `TipoMarcacaoInvalidoException` → 400.
- [ ] Log estruturado: evento `HorarioTipoMarcacaoAlterado` (Information, `TrackId`, `ProfessorId`, `HorarioId`, `TipoMarcacaoAnterior`, `TipoMarcacaoNovo`) no mesmo controller, mesmo padrão de `LogLimiteAlunosAlterado` já existente.
- [ ] Teste de componente (frontend, `HorarioCard.test.tsx`): ação "Editar política" alterna o card para modo de edição com `ChipSelector`, chama `onAlterarPolitica(horarioId, novoTipo)` ao salvar, e volta ao modo normal ao cancelar sem chamar a prop.
- [ ] Implementação mínima: `HorarioCard.tsx` ganha o modo de edição inline descrito acima (reaproveita `ChipSelector`, mesmas opções de `HorarioForm.tsx`) e a prop nova `onAlterarPolitica`.
- [ ] Implementação mínima: `lib/api/horarios.ts` ganha `alterarTipoMarcacaoHorario(professorId, horarioId, tipoMarcacao)` (mesmo padrão de resultado tipado `{ sucesso: true, horario } | { sucesso: false, mensagem }` dos demais métodos do arquivo).
- [ ] Teste de componente (frontend, teste da tela `professor/[professorId]/horarios.test.tsx` se existir, ou criar seguindo o padrão de `criarHandleSubmit`/`criarHandleRemover`): `handleAlterarPolitica` atualiza o horário certo na lista local em caso de sucesso e propaga a mensagem de erro em caso de falha.
- [ ] Implementação mínima: `professor/[professorId]/horarios.tsx` ganha `handleAlterarPolitica` (mesmo padrão de `criarHandleSubmit`/`criarHandleRemover`) e passa `onAlterarPolitica` para `HorarioCard`.
- [ ] Docs: atualizar `docs/spec/business-rules.md#horários-e-política-de-marcação` — a frase "Horários mantêm-se imutáveis após criação (remover e recriar, sem edição)" ganha a exceção explícita desta Task (só `TipoMarcacao` é editável; duração, dia, hora e `LimiteAlunos` continuam imutáveis).
