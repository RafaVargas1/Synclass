# Task: Tela de cadastro de horário ganha o seletor de política, sem o gate de modelo do Professor (#76)

Card: https://github.com/RafaVargas1/Synclass/issues/76

## Ordem de execução

- [x] Implementação mínima: `TipoMarcacao` (enum `Livre=0/Fixo=1/Hibrido=2`) + campo `tipoMarcacao` em `Horario`/`CriarHorarioInput` (`frontend/src/lib/api/horarios.ts`), espelhando `Synclass.Domain.Horarios.TipoMarcacao` do backend.
- [x] Teste (`HorarioForm.test.tsx`): seleciona uma política via `ChipSelector` e `onSubmit` recebe `tipoMarcacao` no input.
- [x] Teste (`HorarioForm.test.tsx`): tenta enviar sem escolher política — não chama `onSubmit`, mostra mensagem de campo obrigatório.
- [x] Implementação: `HorarioForm.tsx` ganha `ChipSelector` de política, estado inicial sem seleção (`undefined`, diferente do padrão de `diaSemana` que já nasce com default), valida obrigatoriedade em `validar`.
- [x] Teste (`HorarioCard.test.tsx`): mostra o rótulo da política do horário (Livre/Fixo/Híbrido).
- [x] Implementação: `HorarioCard.tsx` exibe o rótulo de `horario.tipoMarcacao`.
- [ ] Teste (`horarios.test.tsx`): tela carrega direto no formulário de horários (sem `ModeloAgendamentoForm`/gate), mesmo sem `ConfiguracaoProfessor` prévia.
- [ ] Implementação: `professor/[professorId]/horarios.tsx` remove `useCarregamentoConfiguracao`/`GateModeloAgendamento`/`useDefinirModelo`/`TelaErroConfiguracao` e as chamadas a `obterConfiguracao`/`definirModeloAgendamento` — `HorariosProfessorScreen` renderiza `Topbar` + `HorariosConteudo` direto.
- [ ] Ajuste de testes remanescentes: remover do `horarios.test.tsx` os casos que só existiam para cobrir o gate (mockavam `obterConfiguracao`); ajustar chamadas a `criarHorario`/asserts de `CriarHorarioInput` que agora incluem `tipoMarcacao`.
- [ ] Checks finais: `npm run lint && npm run typecheck && npm test` (escopo do frontend tocado).
