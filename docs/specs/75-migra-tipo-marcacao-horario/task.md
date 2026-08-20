# Task: Migrar horários já existentes para a nova política individual (#75)

Card: https://github.com/RafaVargas1/Synclass/issues/75

## Ordem de execução

- [ ] Teste de integração (Infrastructure, banco real via `WebApplicationFactory`/contexto EF): horário de Professor com `ModeloAgendamento.Vago` → migration aplica `TipoMarcacao.Livre`.
- [ ] Teste de integração: horário de Professor com `ModeloAgendamento.Fixo` → migration aplica `TipoMarcacao.Fixo`.
- [ ] Teste de integração: horário de Professor com `ModeloAgendamento.Hibrido` **com** alocação de `OrigemAlocacao.Professor` já registrada nesse horário → migration aplica `TipoMarcacao.Fixo`.
- [ ] Teste de integração: horário de Professor com `ModeloAgendamento.Hibrido` **sem** nenhuma alocação → migration aplica `TipoMarcacao.Livre`.
- [ ] Teste de integração: horário "órfão" (sem `ConfiguracaoProfessor` correspondente — cenário defensivo, inalcançável no fluxo normal, ver `implementation.md#edge-points`) → migration aplica o default único `TipoMarcacao.Livre`.
- [ ] Implementação: migration `MigraTipoMarcacaoHorarioExistente` (`migrationBuilder.Sql`, idempotente) derivando `Horarios.TipoMarcacao` a partir de `ConfiguracoesProfessor.ModeloAgendamento` + `AlocacoesHorario.OrigemAlocacao`, conforme a RN do card.
- [ ] Checks finais: `dotnet format && dotnet test`.
