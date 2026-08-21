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

- [x] Teste de componente: `SeletorDeHora` — mostra o valor atual formatado (`HH:mm`) ou um placeholder quando `undefined`
- [x] Teste de componente: `SeletorDeHora` — abre o painel ao tocar, lista horas 00-23 e minutos 00-59
- [x] Teste de componente: `SeletorDeHora` — selecionar hora e minuto chama `onSelecionar` com `HH:mm` e fecha o painel
- [x] Implementação mínima: `frontend/src/components/molecules/SeletorDeHora.tsx` (props: `label`, `valor: string | undefined` em `HH:mm`, `onSelecionar: (hora: string) => void`)
- [x] Teste de componente: cada botão de hora/minuto no painel tem `minWidth`/`minHeight` de 44 efetivos (mesmo padrão da issue #115, aplicar aqui desde já já que é componente novo)
- [x] Teste de componente: `HorarioForm` — usa `SeletorDeHora` em vez do `FormField` de texto livre pra "Hora de início"
- [x] Implementação: `frontend/src/components/organisms/HorarioForm.tsx` — troca o `FormField` de "Hora de início" por `SeletorDeHora`; remove a validação de formato `HH:mm` via regex (`validar`, `MensagemFormatoHoraInvalido`) já que o seletor não permite formato inválido por construção; mantém o restante da validação (duração, limite de alunos, conflito)
- [x] Teste de componente: `HorarioForm` — submissão continua enviando `horaInicio` como `HH:mm:00` pra Api (contrato inalterado)

### Inconsistências encontradas

O task não especificava o que fazer quando o usuário submete o formulário
sem tocar no seletor de hora — com o campo trocado do `FormField` de texto
(que iniciava `''`) para `SeletorDeHora` (que inicia `undefined`), enviar
sem escolher geraria `undefined:00` na Api, um bug claro. Como horário sem
hora de início não existe no domínio, decidi manter a obrigatoriedade do
campo ao remover só a checagem de *formato*: adicionei uma validação mínima
(`horaInicio === undefined` → mensagem "Escolha a hora de início."), sem
regex. Não é ambiguidade que bloqueia a Task — os critérios de aceite dados
(remover regex de formato, manter demais validações e o contrato `HH:mm:00`)
continuam atendidos —, mas registro aqui a decisão para transparência.
