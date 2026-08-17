# Task: Professor define limite de alunos por horário (#17)

Card: https://github.com/RafaVargas1/Synclass/issues/17

## Ordem de execução

- [x] Teste unidade (Domain): `LimiteAlunosHorario.Validar` rejeita 0 e negativo com `LimiteAlunosInvalidoException`
- [x] Implementação mínima: `LimiteAlunosHorario` (validador estático, mesmo padrão de `DuracaoAula`) + `LimiteAlunosInvalidoException`
- [x] Teste unidade (Domain): `Horario.Criar` sem informar limite aplica o default (1)
- [x] Teste unidade (Domain): `Horario.Criar` com limite informado (>1) usa o valor informado
- [x] Teste unidade (Domain): `Horario.Criar` com limite 0/negativo rejeita com `LimiteAlunosInvalidoException`
- [x] Implementação mínima: propriedade `Horario.LimiteAlunos` + parâmetro opcional `limiteAlunos` em `Horario.Criar`
- [x] Teste unidade (Domain): `Horario.AlterarLimiteAlunos` com novo limite >= quantidade alocada aplica o novo valor
- [x] Teste unidade (Domain): `Horario.AlterarLimiteAlunos` com novo limite < quantidade alocada rejeita com `LimiteAlunosMenorQueAlocadosException`
- [x] Teste unidade (Domain): `Horario.AlterarLimiteAlunos` com novo limite 0/negativo rejeita com `LimiteAlunosInvalidoException` (reusa `LimiteAlunosHorario.Validar`)
- [x] Implementação mínima: `Horario.AlterarLimiteAlunos(novoLimite, quantidadeAlunosAlocados)` + `LimiteAlunosMenorQueAlocadosException` (estrutura pronta para a issue #8 — ver implementation.md, não wireada a nenhum endpoint nesta issue)
- [x] Teste unidade (Domain): `HorarioService.CadastrarAsync` propaga `limiteAlunos` opcional para `Horario.Criar` (default e valor informado, em `HorarioServiceTests`)
- [x] Implementação mínima: `HorarioService.CadastrarAsync` aceita `limiteAlunos` opcional
- [x] Migration `AdicionaLimiteAlunosHorario`: coluna `LimiteAlunos` (int, not null, default 1) em `Horarios`, via `HorarioConfiguration`
- [x] Teste de fumaça (Api): `POST /professores/{id}/horarios` sem `limiteAlunos` no corpo devolve `limiteAlunos: 1` na resposta
- [x] Teste de fumaça (Api): `POST /professores/{id}/horarios` com `limiteAlunos: 0` devolve 400
- [x] Implementação mínima: `CriarHorarioRequest.LimiteAlunos` (opcional), `HorarioResponse.LimiteAlunos`, controller repassa o valor para o `HorarioService`
- [x] Log estruturado: evento `LimiteAlunosAlterado` (Information) emitido pelo controller ao criar o horário — ver architecture.md#logs-estruturados-e-track-id e a Decisão documentada em implementation.md sobre `LimiteAnterior=null` na criação
- [x] Componente frontend: campo numérico "Limite de alunos" em `HorarioForm` (default `'1'`, validação inline para valor menor que 1), propagado em `CriarHorarioInput`/`Horario` (`lib/api/horarios.ts`) — inclui atualizar `HorarioForm.test.tsx` para o novo campo
- [x] Componente frontend: `HorarioCard` mostra o limite cadastrado (ex: "Individual" quando 1, "Grupo até N" quando > 1), com teste em `HorarioCard.test.tsx`

## Fora de escopo nesta Task (documentado, não esquecido)

- Endpoint dedicado de atualização de limite (`PUT`/`PATCH`) — decisão documentada em
  `implementation.md#decisão-documentada-sem-endpoint-de-edição-de-horário`.
- Validação de redução contra `AlocacoesHorario` real e o log
  `LimiteRejeitadoPorAlunosAlocados` — dependem da tabela `AlocacoesHorario`,
  que só nasce na issue #8 (ver implementation.md#dependência-da-issue-8).
- Bloqueio de alocação além da capacidade (issues #7, #8, #9).
