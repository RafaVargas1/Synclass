# Task: Professor cadastra horários disponíveis para aulas (#6)

Card: https://github.com/RafaVargas1/Synclass/issues/6

## Ordem de execução

- [ ] Teste unidade (Domain): `Horario.Criar` com dados válidos cria horário com os campos informados.
- [ ] Implementação mínima: `DiaSemana` (enum), `Horario` (entidade).
- [ ] Teste unidade (Domain): `Horario.Criar` rejeita duração zero/negativa (`DuracaoInvalidaException`).
- [ ] Implementação mínima: `DuracaoAula.Validar`.
- [ ] Teste unidade (Domain): `Horario.Sobrepoe` — mesmo dia + intervalos que se cruzam retorna `true`; borda que só se toca (fim de um == início do outro) retorna `false`; dias diferentes retorna `false`.
- [ ] Implementação mínima: `Horario.Sobrepoe`.
- [ ] Teste unidade (Domain): `HorarioService.CadastrarAsync` — cria e persiste horário válido sem conflito.
- [ ] Teste unidade (Domain): `HorarioService.CadastrarAsync` — rejeita com `HorarioConflitanteException` quando sobrepõe horário existente do mesmo Professor/dia.
- [ ] Implementação mínima: `IHorarioRepository`, `HorarioService.CadastrarAsync`, `FakeHorarioRepository`.
- [ ] Teste unidade (Domain): `HorarioService.ListarAsync` — devolve horários do Professor.
- [ ] Implementação mínima: `HorarioService.ListarAsync`.
- [ ] Teste unidade (Domain): `HorarioService.RemoverAsync` — remove horário existente do Professor dono.
- [ ] Teste unidade (Domain): `HorarioService.RemoverAsync` — rejeita com `HorarioComAlunosAlocadosException` quando existem Alunos alocados (stub, ver `implementation.md#dependência-da-issue-8`).
- [ ] Teste unidade (Domain): `HorarioService.RemoverAsync` — rejeita com `HorarioNaoEncontradoException` quando o horário não existe ou não pertence ao Professor.
- [ ] Implementação mínima: `HorarioService.RemoverAsync`.
- [ ] Migration `CriaHorario`: tabela `Horarios` (`Id`, `ProfessorId` FK → `Usuarios`, `DiaSemana`, `HoraInicio`, `DuracaoMinutos`, `CreatedAt`) + `HorarioConfiguration` + `HorarioRepository` (EF Core).
- [ ] Teste de fumaça (Api): `POST /professores/{professorId}/horarios` — 200 com dados válidos, 400 com duração inválida, 400 com conflito.
- [ ] Teste de fumaça (Api): `GET /professores/{professorId}/horarios` — lista horários cadastrados.
- [ ] Teste de fumaça (Api): `DELETE /professores/{professorId}/horarios/{horarioId}` — 204 quando remove, 404 quando não existe/não pertence ao Professor.
- [ ] Implementação: `HorariosController` (criar/listar/remover) + logs estruturados (`HorarioCriado`, `HorarioRejeitadoPorConflito`, `HorarioRejeitadoPorAlunosAlocados`) + registro DI em `Program.cs`.
- [ ] Teste frontend: `lib/api/horarios.ts` — `criarHorario`/`listarHorarios`/`removerHorario` interpretam sucesso, erro de negócio e erro de conexão.
- [ ] Implementação: `lib/api/horarios.ts`.
- [ ] Teste frontend: `HorarioForm` — valida conflito no cliente antes de submeter (feedback imediato) e emite `onSubmit` com dados válidos.
- [ ] Teste frontend: `HorarioCard` — exibe dia, hora de início e duração; botão remover dispara callback.
- [ ] Implementação: organism `HorarioForm`, organism `HorarioCard`.
- [ ] Teste frontend: tela `professor/[professorId]/horarios` — lista, cria, mostra erro de conflito, remove.
- [ ] Implementação: tela `professor/[professorId]/horarios.tsx`.
- [ ] Checks finais: `dotnet format && dotnet test` / `npm run lint && npm run typecheck && npm test`.
