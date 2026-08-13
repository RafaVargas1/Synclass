# Task: Professor define limite de alunos por horário (#17)

Card: https://github.com/RafaVargas1/Synclass/issues/17

## Ordem de execução

- [ ] Teste unidade (Domain): `LimiteAlunosHorario.Validar` rejeita 0 e negativo com `LimiteAlunosInvalidoException`
- [ ] Implementação mínima: `LimiteAlunosHorario` (validador estático, mesmo padrão de `DuracaoAula`) + `LimiteAlunosInvalidoException`
- [ ] Teste unidade (Domain): `Horario.Criar` sem informar limite aplica o default (1)
- [ ] Teste unidade (Domain): `Horario.Criar` com limite informado (>1) usa o valor informado
- [ ] Teste unidade (Domain): `Horario.Criar` com limite 0/negativo rejeita com `LimiteAlunosInvalidoException`
- [ ] Implementação mínima: propriedade `Horario.LimiteAlunos` + parâmetro opcional `limiteAlunos` em `Horario.Criar`
- [ ] Teste unidade (Domain): `Horario.AlterarLimiteAlunos` com novo limite >= quantidade alocada aplica o novo valor
- [ ] Teste unidade (Domain): `Horario.AlterarLimiteAlunos` com novo limite < quantidade alocada rejeita com `LimiteAlunosMenorQueAlocadosException`
- [ ] Teste unidade (Domain): `Horario.AlterarLimiteAlunos` com novo limite 0/negativo rejeita com `LimiteAlunosInvalidoException` (reusa `LimiteAlunosHorario.Validar`)
- [ ] Implementação mínima: `Horario.AlterarLimiteAlunos(novoLimite, quantidadeAlunosAlocados)` + `LimiteAlunosMenorQueAlocadosException` (estrutura pronta para a issue #8 — ver implementation.md, não wireada a nenhum endpoint nesta issue)
- [ ] Teste unidade (Domain): `HorarioService.CadastrarAsync` propaga `limiteAlunos` opcional para `Horario.Criar` (default e valor informado, em `HorarioServiceTests`)
- [ ] Implementação mínima: `HorarioService.CadastrarAsync` aceita `limiteAlunos` opcional
- [ ] Migration `AdicionaLimiteAlunosHorario`: coluna `LimiteAlunos` (int, not null, default 1) em `Horarios`, via `HorarioConfiguration`
- [ ] Teste de fumaça (Api): `POST /professores/{id}/horarios` sem `limiteAlunos` no corpo devolve `limiteAlunos: 1` na resposta
- [ ] Teste de fumaça (Api): `POST /professores/{id}/horarios` com `limiteAlunos: 0` devolve 400
- [ ] Implementação mínima: `CriarHorarioRequest.LimiteAlunos` (opcional), `HorarioResponse.LimiteAlunos`, controller repassa o valor para o `HorarioService`
- [ ] Log estruturado: evento `LimiteAlunosAlterado` (Information) emitido pelo controller ao criar o horário — ver architecture.md#logs-estruturados-e-track-id e a Decisão documentada em implementation.md sobre `LimiteAnterior=null` na criação
- [ ] Componente frontend: campo numérico "Limite de alunos" em `HorarioForm` (default `'1'`, validação inline para valor menor que 1), propagado em `CriarHorarioInput`/`Horario` (`lib/api/horarios.ts`) — inclui atualizar `HorarioForm.test.tsx` para o novo campo
- [ ] Componente frontend: `HorarioCard` mostra o limite cadastrado (ex: "Individual" quando 1, "Grupo até N" quando > 1), com teste em `HorarioCard.test.tsx`

## Fora de escopo nesta Task (documentado, não esquecido)

- Endpoint dedicado de atualização de limite (`PUT`/`PATCH`) — decisão documentada em
  `implementation.md#decisão-documentada-sem-endpoint-de-edição-de-horário`.
- Validação de redução contra `AlocacoesHorario` real e o log
  `LimiteRejeitadoPorAlunosAlocados` — dependem da tabela `AlocacoesHorario`,
  que só nasce na issue #8 (ver implementation.md#dependência-da-issue-8).
- Bloqueio de alocação além da capacidade (issues #7, #8, #9).
