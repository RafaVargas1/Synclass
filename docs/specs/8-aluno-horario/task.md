# Task: Professor coloca Aluno em um horário específico (#8)

Card: https://github.com/RafaVargas1/Synclass/issues/8

## Ordem de execução

- [x] `IMatriculaRepository`: `BuscarPorIdAsync` + `ListarPorProfessorAsync` (interface, `MatriculaRepository`, `FakeMatriculaRepository`) — pré-requisito dos testes de `AlocacaoHorarioService` abaixo
- [x] `HorarioService.BuscarDoProfessorAsync` vira `internal` (era `private`) — pré-requisito para `AlocacaoHorarioService` reaproveitar sem duplicar
- [x] Teste unidade (Domain): `AlocacaoHorarioService.AlocarAsync` em modelo Fixo com vaga cria a alocação (AC1)
- [x] Teste unidade (Domain): `AlocarAsync` em modelo Híbrido com vaga cria a alocação (AC1)
- [x] Implementação mínima: `AlocacaoHorario`, `IAlocacaoHorarioRepository`, `AlocacaoHorarioService.AlocarAsync` (caminho feliz), `FakeAlocacaoHorarioRepository`
- [x] Teste unidade (Domain): `AlocarAsync` em modelo Vago rejeita com `ModeloNaoPermiteAlocacaoException` (AC2)
- [x] Implementação mínima: checagem de modelo + `ModeloNaoPermiteAlocacaoException`
- [ ] Teste unidade (Domain): `AlocarAsync` em horário no limite (`LimiteAlunos` já atingido) rejeita com `HorarioLotadoException` (AC3)
- [ ] Implementação mínima: checagem de vaga via `ContarPorHorarioAsync` + `HorarioLotadoException`
- [ ] Teste unidade (Domain): `AlocarAsync` com Matrícula inexistente rejeita com `MatriculaNaoVinculadaAoProfessorException` (AC5)
- [ ] Teste unidade (Domain): `AlocarAsync` com Matrícula de outro Professor rejeita com `MatriculaNaoVinculadaAoProfessorException` (AC5)
- [ ] Implementação mínima: checagem de vínculo via `IMatriculaRepository.BuscarPorIdAsync` + `MatriculaNaoVinculadaAoProfessorException`
- [ ] Teste unidade (Domain): `AlocarAsync` com horário inexistente/de outro Professor rejeita com `HorarioNaoEncontradoException`
- [ ] Teste unidade (Domain): `AlocarAsync` com o mesmo Aluno já alocado no horário rejeita com `AlocacaoJaExisteException`
- [ ] Implementação mínima: checagem de duplicidade + `AlocacaoJaExisteException`
- [ ] Teste unidade (Domain): `DesalocarAsync` remove a alocação e libera a vaga (AC4 — nova `AlocarAsync` após `DesalocarAsync` no mesmo horário funciona)
- [ ] Teste unidade (Domain): `DesalocarAsync` de um horário não afeta outra alocação do mesmo Aluno em outro horário (RN — alocações independentes)
- [ ] Teste unidade (Domain): `DesalocarAsync` de alocação inexistente rejeita com `AlocacaoNaoEncontradaException`
- [ ] Teste unidade (Domain): `DesalocarAsync` com horário inexistente/de outro Professor rejeita com `HorarioNaoEncontradoException`
- [ ] Implementação mínima: `AlocacaoHorarioService.DesalocarAsync` + `AlocacaoNaoEncontradaException`
- [ ] Teste unidade (Domain): `ListarPorHorarioAsync` devolve as alocações do horário (usado pelo `GET`)
- [ ] Implementação mínima: `AlocacaoHorarioService.ListarPorHorarioAsync`
- [ ] Migration `CriaAlocacaoHorario`: tabela `AlocacoesHorario` (`Id`, `HorarioId` FK, `MatriculaId` FK, `CreatedAt`), índice único (`HorarioId`, `MatriculaId`) — via `AlocacaoHorarioConfiguration`, `AlocacaoHorarioRepository`, `DbSet` em `SynclassDbContext`
- [ ] Liga o stub: `HorarioRepository.PossuiAlunosAlocadosAsync` consulta `AlocacoesHorario` de verdade (issue #6 dependia disto — ver implementation.md)
- [ ] Teste de fumaça (Api): `POST /professores/{id}/horarios/{horarioId}/alocacoes` com dados válidos devolve 200 com a alocação
- [ ] Teste de fumaça (Api): `POST .../alocacoes` em modelo Vago devolve 400
- [ ] Teste de fumaça (Api): `POST .../alocacoes` com horário lotado devolve 400
- [ ] Teste de fumaça (Api): `POST .../alocacoes` com Matrícula não vinculada devolve 400
- [ ] Teste de fumaça (Api): `POST .../alocacoes` com horário inexistente devolve 404
- [ ] Implementação mínima: `AlocacoesHorarioController` (`POST`), registro de DI em `Program.cs`
- [ ] Teste de fumaça (Api): `GET .../alocacoes` lista as alocações do horário
- [ ] Teste de fumaça (Api): `DELETE .../alocacoes/{matriculaId}` devolve 204 e remove a alocação
- [ ] Teste de fumaça (Api): `DELETE .../alocacoes/{matriculaId}` inexistente devolve 404
- [ ] Implementação mínima: `AlocacoesHorarioController` (`GET`, `DELETE`)
- [ ] Teste de fumaça (Api): `GET /professores/{id}/alunos-provisorios` lista as Matrículas do Professor
- [ ] Implementação mínima: `AlunosProvisoriosController.Listar` + `IMatriculaRepository.ListarPorProfessorAsync` (EF Core)
- [ ] Log estruturado: evento `AlunoAlocadoEmHorario` (Information) no `POST` de sucesso
- [ ] Log estruturado: evento `AlocacaoRejeitada` (Warning) em qualquer `AlocacaoRejeitadaException`
- [ ] Log estruturado: evento `AlocacaoDesfeita` (Information) no `DELETE` de sucesso
- [ ] Componente frontend: `lib/api/alocacoes.ts` (`alocarAluno`, `listarAlocacoes`, `desalocarAluno`) + `listarAlunosProvisorios` em `lib/api/alunosProvisorios.ts`
- [ ] Componente frontend: organism `HorarioAlocacaoCard` (vagas ocupadas/total, lista de Alunos alocados com remover, seletor de Aluno disponível + botão Alocar), com teste
- [ ] Componente frontend: tela `app/professor/[professorId]/alocacoes.tsx` (gate de modelo Vago com mensagem, grade de `HorarioAlocacaoCard` para Fixo/Híbrido), com teste
