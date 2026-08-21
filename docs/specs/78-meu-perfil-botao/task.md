# Task: 'Meu perfil' vira um botão, não um link de texto (#78)

Card: https://github.com/RafaVargas1/Synclass/issues/78

## Ordem de execução

- [x] Teste de componente: `painel/index.tsx` — "Meu perfil" é exposto com `accessibilityRole="button"` (não mais como link sublinhado) e navega para `/perfil` ao ser pressionado
- [x] Implementação: troca `<Link href="/perfil" className="...underline...">Meu perfil</Link>` por `<Link href="/perfil" asChild><Button label="Meu perfil" /></Link>` em `frontend/src/app/painel/index.tsx`
- [x] Ajustar teste existente de `painel/index.test.tsx` que fizer asserção sobre o `Link`/texto sublinhado de "Meu perfil", se houver

### Inconsistências encontradas

A RN do card afirma que "as demais ações do Painel usam o átomo `Button`",
mas isso não é verdade no código atual: as ações da `ListaDeAcoes`
(`ItemDeAcao`, painel/index.tsx) são `Link` estilizados como card
(`border-border bg-background-element`), não o átomo `Button`
(`border-text bg-primary`, usado hoje só no botão "Tentar novamente" de
erro). Resolvida sem bloquear: os **Critérios de aceite** e **Critérios
técnicos** do card são explícitos e não-ambíguos — pedem literalmente
"mesmo átomo `Button`" e `Link asChild` com `Button` — então a instrução
direta prevalece sobre a imprecisão da RN. Seguindo como especificado.
