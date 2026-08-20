# Task: Alocação/marcação de horário passa a decidir pela política do horário, não mais do Professor (#74)

Card: https://github.com/RafaVargas1/Synclass/issues/74

## Ordem de execução

- [ ] Teste unidade (Domain): `AlocacaoHorarioService.AlocarAsync` — horário `Livre`, Professor tenta atribuir Aluno, rejeita com a exceção existente (adaptada a horário).
- [ ] Teste unidade (Domain): `AlocacaoHorarioService.AlocarAsync` — horário `Fixo`/`Hibrido`, Professor atribui Aluno, aceita (comportamento já coberto, migrar setup do teste de `ModeloAgendamento` do Professor para `TipoMarcacao` do horário).
- [ ] Implementação mínima: `GarantirModeloPermiteAlocacaoAsync` vira checagem síncrona sobre `Horario.TipoMarcacao` (o `horario` já é buscado em `AlocarAsync` antes desta chamada — não precisa mais de `IConfiguracaoProfessorRepository`).
- [ ] Teste unidade (Domain): `AlocacaoHorarioService.MarcarAsync` — horário `Fixo`, Aluno tenta se marcar livremente, rejeita.
- [ ] Teste unidade (Domain): `AlocacaoHorarioService.MarcarAsync` — horário `Hibrido` **já com atribuição do Professor**, Aluno se marca livremente no mesmo horário, aceita (comportamento novo — o Híbrido por Professor de hoje bloqueava esse caso, o Híbrido por horário não bloqueia mais).
- [ ] Teste unidade (Domain): `AlocacaoHorarioService.MarcarAsync` — horário `Livre`, Aluno se marca livremente, aceita.
- [ ] Implementação mínima: `GarantirModeloPermiteMarcacaoAsync` vira checagem síncrona sobre `Horario.TipoMarcacao`, sem o parâmetro `horarioPossuiAtribuicaoFixa` (deixa de ser relevante).
- [ ] Teste unidade (Domain): `AlocacaoHorarioService.ListarVagosAsync` — só retorna horários `Livre`/`Hibrido` com vaga, não retorna `Fixo`.
- [ ] Implementação: `ListarVagosAsync`/`ParaHorarioVagoSeElegivelAsync` decidem por `horario.TipoMarcacao`, removendo a dependência de `ConfiguracaoProfessor` nesta classe.
- [ ] Remoção: campo `_configuracoes`/`IConfiguracaoProfessorRepository` do construtor de `AlocacaoHorarioService` (nada mais na classe usa).
- [ ] Ajuste dos testes de fumaça (Api) em `AlocacaoHorarioEndpointTests`/`MarcacaoHorarioEndpointTests` que hoje configuram `ModeloAgendamento` do Professor para controlar o cenário — passam a cadastrar o `Horario` já com o `TipoMarcacao` desejado.
- [ ] Checks finais: `dotnet format && dotnet test`.
