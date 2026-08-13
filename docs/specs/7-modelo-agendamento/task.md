# Task: Professor escolhe o modelo de agendamento do seu sistema (#7)

Card: https://github.com/RafaVargas1/Synclass/issues/7

## Ordem de execução

- [x] Teste unidade (Domain): `ConfiguracaoProfessor.Criar` cria configuração com o modelo informado.
- [x] Teste unidade (Domain): `ConfiguracaoProfessor.AlterarModelo` troca o modelo e atualiza `UpdatedAt`, sem versionar o anterior.
- [x] Teste unidade (Domain): `ConfiguracaoProfessor.PermiteMarcacaoLivre` — Vago sempre `true`; Fixo sempre `false`; Híbrido é `true` só quando o horário não tem atribuição fixa.
- [x] Implementação mínima: `ModeloAgendamento` (enum), `ConfiguracaoProfessor` (entidade).
- [x] Teste unidade (Domain): `HorarioService.CadastrarAsync` rejeita com `ModeloAgendamentoNaoDefinidoException` quando o Professor não tem configuração.
- [x] Teste unidade (Domain): `HorarioService.CadastrarAsync` segue normalmente quando a configuração já existe (qualquer modelo).
- [x] Implementação mínima: `IConfiguracaoProfessorRepository`, `FakeConfiguracaoProfessorRepository`, ajuste em `HorarioService` (nova dependência).
- [x] Teste unidade (Domain): `ConfiguracaoProfessorService.DefinirModeloAsync` cria configuração quando não existe.
- [x] Teste unidade (Domain): `ConfiguracaoProfessorService.DefinirModeloAsync` altera o modelo quando já existe, preservando `ProfessorId`/`CreatedAt`.
- [x] Implementação mínima: `ConfiguracaoProfessorService`.
- [x] Migration `CriaConfiguracaoProfessor`: tabela `ConfiguracoesProfessor` (`Id`, `ProfessorId` FK único → `Usuarios`, `ModeloAgendamento`, `CreatedAt`, `UpdatedAt`) + `ConfiguracaoProfessorConfiguration` + `ConfiguracaoProfessorRepository` (EF Core).
- [x] Teste de fumaça (Api): `PUT /professores/{professorId}/configuracao/modelo-agendamento` — 200 cria na 1ª chamada, 200 altera numa chamada seguinte.
- [x] Teste de fumaça (Api): `GET /professores/{professorId}/configuracao` — 200 quando existe, 404 quando não definida.
- [x] Teste de fumaça (Api): `POST /professores/{professorId}/horarios` agora retorna 400 quando o Professor não definiu modelo.
- [x] Implementação: `ConfiguracoesController` (definir/consultar) + log estruturado `ModeloAgendamentoDefinido` + registro DI em `Program.cs`.
- [ ] Teste frontend: `lib/api/configuracao.ts` — `obterConfiguracao`/`definirModeloAgendamento` interpretam sucesso, "não definida" (404) e erro de conexão.
- [ ] Implementação: `lib/api/configuracao.ts`.
- [ ] Teste frontend: `ModeloAgendamentoForm` — seleciona um dos 3 modelos e emite `onSubmit` com o valor escolhido.
- [ ] Implementação: organism `ModeloAgendamentoForm`.
- [ ] Teste frontend: tela `professor/[professorId]/horarios` — quando a configuração ainda não existe (404), mostra `ModeloAgendamentoForm` no lugar da lista/form de horários; ao definir o modelo, libera a tela normal sem precisar recarregar.
- [ ] Implementação: gate em `professor/[professorId]/horarios.tsx`.
- [ ] Checks finais: `dotnet format && dotnet test` / `npm run lint && npm run typecheck && npm test`.
