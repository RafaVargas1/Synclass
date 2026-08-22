# Task: Aluno vê resumo de frequência recente direto no Painel (#167)

Card: https://github.com/RafaVargas1/Synclass/issues/167

Leia `implementation.md` ANTES do primeiro item — decisões fechadas lá
(janela de 30 dias, o que conta como compareceu/faltou, onde entra no
Painel, copy exato com singular/plural, código pronto antes/depois).

## Ordem de execução

- [ ] Teste (frontend, novo `ResumoFrequenciaCard.test.tsx`):
      `presentes=3, ausentes=1` renderiza "3 presenças · 1 falta";
      `presentes=0, ausentes=0` renderiza a mensagem de vazio
      (`MensagemSemDados`); `presentes=0, ausentes=2` SAI do vazio e
      mostra "0 presenças · 2 faltas" (vazio só quando AMBOS são zero);
      `presentes=1, ausentes=0` mostra "1 presença · 0 faltas".
      Ver falhar (componente não existe).
- [ ] Implementação (frontend): criar
      `frontend/src/components/organisms/ResumoFrequenciaCard.tsx`
      conforme `implementation.md` — apresentação pura, esqueleto visual
      de `HistoricoFrequenciaCard.tsx:39`, copy com `rotularPlural`.
- [ ] Implementação (frontend): `frontend/src/lib/api/historicoFrequencia.ts`
      ganha `calcularPeriodoUltimosNDias(hoje, 30)` seguindo o padrão de
      `calcularPeriodoTodos` em `valorDevido.ts:79-82` (precedente de
      cálculo deslocado de data). Sem mudar a assinatura de
      `listarHistoricoFrequenciaDoAluno` (contrato já estável da issue #16).
- [ ] Teste (frontend, `frontend/src/app/painel/index.test.tsx`): com
      `papelAtivo: 'Aluno'` e `listarHistoricoFrequenciaDoAluno` mockado
      resolvendo com 3 `Presente` + 1 `Ausente`, o Painel exibe o resumo
      ("3 presenças · 1 falta"). Ver falhar.
- [ ] Implementação (frontend): `frontend/src/app/painel/index.tsx` —
      quando `papelAtivo === 'Aluno'`, monta `ResumoDeFrequenciaDoAluno`
      entre a saudação e o grid de ações, com o hook co-localizado +
      `calcularResumo` conforme `implementation.md` (trecho antes/depois
      ali). Adicione `useEffect` ao import de `react` se ainda não estiver.
- [ ] Teste (frontend, `painel/index.test.tsx`): com `papelAtivo: 'Aluno'`
      e histórico só `NaoRegistrada`/`Cancelada` (ou `historico: []`),
      exibe a mensagem de vazio (`MensagemSemDados`). Ver falhar.
- [ ] Teste (frontend, `painel/index.test.tsx`): com
      `papelAtivo: 'Professor'`, o resumo NÃO aparece (query por
      `/presença/` é `null`). Ver falhar.
- [ ] Teste (frontend, `painel/index.test.tsx`): na falha de rede
      (`sucesso: false`), a tela não quebra e as ações continuam visíveis
      (resumo degradado para `null`). Ver falhar.
- [ ] Teste (frontend, `painel/index.test.tsx`): `listarHistoricoFrequenciaDoAluno`
      é chamado com o período dos últimos 30 dias (`inicio` = 29 dias antes
      de hoje, `fim` = dia seguinte a hoje, formato `yyyy-MM-dd`). Ver falhar.
- [ ] `npm run lint && npm run typecheck && npm test` (frontend) verde.
- [ ] Refatore se necessário: releia o diff final contra
      `docs/spec/code-style.md` e `docs/spec/ux-heuristics.md#agrupamento-visual-gestalt`
      (o resumo é grupo próprio de informação, sem peso de CTA).

## Fora de escopo

- NÃO criar endpoint novo nem mudar contrato — reusa `GET /alunos/historico-frequencia`
  (issue #16) com `PeriodoConsultaInput` de 30 dias.
- NÃO mexer no backend nem em `FrequenciaService` (quem define os status é
  o backend; o front só conta `Presente`/`Ausente`).
- NÃO mexer na tela `aluno/historico-frequencia.tsx` nem na função
  `useConsultaHistoricoFrequenciaDoAluno` (fica como está — a #167 só
  adiciona o resumo no Painel, não altera a tela existente).
- NÃO introduzir token de cor novo em `tailwind.config.js` (ver
  `docs/spec/design-system.md#cor`).
