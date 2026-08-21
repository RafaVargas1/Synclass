# Task: Seletor de hora guiado no cadastro de horário (#117)

Card: https://github.com/RafaVargas1/Synclass/issues/117

Decisão de design (resolvida antes da implementação, já que o card não
tinha um componente equivalente pronto pra reaproveitar): `SeletorDeHora`
segue o mesmo padrão de abrir/fechar de `SeletorDeData` (`Pressable` que
mostra o valor atual, abre um painel, fecha ao confirmar), mas o painel
interno tem **duas listas roláveis horizontais** (`ScrollView
horizontal`) de números — Hora (00-23) e Minuto (00-59) — em vez de
`ChipSelector` com `flex-wrap` (60 chips quebrando linha ficaria
ilegível). Mantém o range completo 00:00-23:59 (não restringe a
incrementos de 15/30 minutos — o card explicitamente pede manter o range
atual, e restringir minutos mudaria o comportamento aceito hoje).

## Ordem de execução

- [ ] Teste de componente: `SeletorDeHora` — mostra o valor atual formatado (`HH:mm`) ou um placeholder quando `undefined`
- [ ] Teste de componente: `SeletorDeHora` — abre o painel ao tocar, lista horas 00-23 e minutos 00-59
- [ ] Teste de componente: `SeletorDeHora` — selecionar hora e minuto chama `onSelecionar` com `HH:mm` e fecha o painel
- [ ] Implementação mínima: `frontend/src/components/molecules/SeletorDeHora.tsx` (props: `label`, `valor: string | undefined` em `HH:mm`, `onSelecionar: (hora: string) => void`)
- [ ] Teste de componente: cada botão de hora/minuto no painel tem `minWidth`/`minHeight` de 44 efetivos (mesmo padrão da issue #115, aplicar aqui desde já já que é componente novo)
- [ ] Teste de componente: `HorarioForm` — usa `SeletorDeHora` em vez do `FormField` de texto livre pra "Hora de início"
- [ ] Implementação: `frontend/src/components/organisms/HorarioForm.tsx` — troca o `FormField` de "Hora de início" por `SeletorDeHora`; remove a validação de formato `HH:mm` via regex (`validar`, `MensagemFormatoHoraInvalido`) já que o seletor não permite formato inválido por construção; mantém o restante da validação (duração, limite de alunos, conflito)
- [ ] Teste de componente: `HorarioForm` — submissão continua enviando `horaInicio` como `HH:mm:00` pra Api (contrato inalterado)

### Inconsistências encontradas

_(Nenhuma até o momento — decisão de design do painel de seleção já
resolvida acima antes de começar. Se surgir ambiguidade nova durante a
implementação, registrar aqui em vez de decidir sozinho.)_
