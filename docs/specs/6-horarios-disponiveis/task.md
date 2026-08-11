# Task: Professor cadastra horários disponíveis para aulas (#6)

Card: https://github.com/RafaVargas1/Synclass/issues/6

## Ordem de execução

- [x] Teste unidade (Domain): `Horario.Criar` com dados válidos cria horário com os campos informados.
- [x] Implementação mínima: `DiaSemana` (enum), `Horario` (entidade).
- [x] Teste unidade (Domain): `Horario.Criar` rejeita duração zero/negativa (`DuracaoInvalidaException`).
- [x] Implementação mínima: `DuracaoAula.Validar`.
- [x] Teste unidade (Domain): `Horario.Sobrepoe` — mesmo dia + intervalos que se cruzam retorna `true`; borda que só se toca (fim de um == início do outro) retorna `false`; dias diferentes retorna `false`.
- [x] Implementação mínima: `Horario.Sobrepoe`.
- [x] Teste unidade (Domain): `HorarioService.CadastrarAsync` — cria e persiste horário válido sem conflito.
- [x] Teste unidade (Domain): `HorarioService.CadastrarAsync` — rejeita com `HorarioConflitanteException` quando sobrepõe horário existente do mesmo Professor/dia.
- [x] Implementação mínima: `IHorarioRepository`, `HorarioService.CadastrarAsync`, `FakeHorarioRepository`.
- [x] Teste unidade (Domain): `HorarioService.ListarAsync` — devolve horários do Professor.
- [x] Implementação mínima: `HorarioService.ListarAsync`.
- [x] Teste unidade (Domain): `HorarioService.RemoverAsync` — remove horário existente do Professor dono.
- [x] Teste unidade (Domain): `HorarioService.RemoverAsync` — rejeita com `HorarioComAlunosAlocadosException` quando existem Alunos alocados (stub, ver `implementation.md#dependência-da-issue-8`).
- [x] Teste unidade (Domain): `HorarioService.RemoverAsync` — rejeita com `HorarioNaoEncontradoException` quando o horário não existe ou não pertence ao Professor.
- [x] Implementação mínima: `HorarioService.RemoverAsync`.
- [x] Migration `CriaHorario`: tabela `Horarios` (`Id`, `ProfessorId` FK → `Usuarios`, `DiaSemana`, `HoraInicio`, `DuracaoMinutos`, `CreatedAt`) + `HorarioConfiguration` + `HorarioRepository` (EF Core).
- [x] Teste de fumaça (Api): `POST /professores/{professorId}/horarios` — 200 com dados válidos, 400 com duração inválida, 400 com conflito.
- [x] Teste de fumaça (Api): `GET /professores/{professorId}/horarios` — lista horários cadastrados.
- [x] Teste de fumaça (Api): `DELETE /professores/{professorId}/horarios/{horarioId}` — 204 quando remove, 404 quando não existe/não pertence ao Professor.
- [x] Implementação: `HorariosController` (criar/listar/remover) + logs estruturados (`HorarioCriado`, `HorarioRejeitadoPorConflito`, `HorarioRejeitadoPorAlunosAlocados`) + registro DI em `Program.cs`.
- [x] Teste frontend: `lib/api/horarios.ts` — `criarHorario`/`listarHorarios`/`removerHorario` interpretam sucesso, erro de negócio e erro de conexão.
- [x] Implementação: `lib/api/horarios.ts`.
- [x] Teste frontend: `HorarioForm` — valida conflito no cliente antes de submeter (feedback imediato) e emite `onSubmit` com dados válidos.
- [x] Teste frontend: `HorarioCard` — exibe dia, hora de início e duração; botão remover dispara callback.
- [x] Implementação: organism `HorarioForm`, organism `HorarioCard`.
- [ ] Teste frontend: tela `professor/[professorId]/horarios` — lista, cria, mostra erro de conflito, remove.
- [ ] Implementação: tela `professor/[professorId]/horarios.tsx`.
- [ ] Checks finais: `dotnet format && dotnet test` / `npm run lint && npm run typecheck && npm test`.
