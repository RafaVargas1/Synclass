# Task: Clarear a paleta de fundo, mantendo o guardrail de extremos puros (#67)

Card: https://github.com/RafaVargas1/Synclass/issues/67

## Ordem de execução

- [x] Teste unidade (frontend, `frontend/src/theme/palette.test.ts`, novo arquivo):
      cenário "Dado o novo `Colors.light.background`, quando comparado a
      `backgroundElement`/`backgroundSelected`, então a razão de contraste
      (WCAG) entre os três permanece >= 1.10 par a par" — ver função de
      contraste em `implementation.md`. Rode o teste primeiro e veja-o
      falhar contra o valor atual do token (que ainda não existe até o
      próximo item, então o teste deve falhar por o arquivo de teste
      importar um util que ainda não existe — ver próximo item).
- [x] Implementação mínima: crie `frontend/src/theme/contraste.ts` (util
      puro, sem dependência de terceiro — `luminanciaRelativa`/
      `razaoDeContraste`, fórmula WCAG 2.x) e o teste acima passando a
      importar dele. Rode o teste — ele deve falhar ainda contra o token
      atual (`#F5F6F8`), confirmando que o teste testa o valor certo antes
      de mudar o token.
- [x] Teste unidade: adicione ao mesmo arquivo o cenário "Dado texto
      `text`/`text-secondary` sobre o novo `background`, quando o contraste
      é calculado, então é >= 4.5:1" — ainda falhando contra `#F5F6F8`
      atual (ou passando, se já atender — o objetivo é travar a regressão
      para o valor novo, não provar que o antigo falha).
- [ ] Implementação: edite `Colors.light.background` em
      `frontend/src/theme/palette.js` de `'#F5F6F8'` para `'#F9FAFB'` (ver
      `implementation.md` para o racional do valor). Não toque em nenhum
      outro token (`backgroundElement`, `backgroundSelected`, `border`,
      `text`, `textSecondary`, `primary`, `error`, modo `dark` inteiro).
      Rode o teste — os dois cenários acima devem passar agora.
- [ ] Atualizar a tabela de cor em `docs/spec/design-system.md#cor`: troque
      o valor de `background`/Light de `#F5F6F8` para `#F9FAFB` e adicione
      uma frase à nota "Mudanças em relação ao `palette.js` atual"
      explicando que o novo valor foi validado por teste automatizado
      (ratio contra `backgroundElement`/`backgroundSelected` e contraste de
      texto).
- [ ] Validação visual (captura de tela): rode o app web (`npm run web` ou
      equivalente já documentado no README do frontend) e capture uma tela
      com cards empilhados (Painel do Professor ou do Aluno) no modo claro,
      confirmando visualmente que a escada `background` → `background-
      element` → `background-selected` continua perceptível. Anexe a
      captura como comentário no PR (não precisa virar arquivo commitado).
