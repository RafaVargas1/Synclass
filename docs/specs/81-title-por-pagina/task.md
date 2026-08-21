# Task: Cada página define seu próprio title na aba do navegador (#81)

Card: https://github.com/RafaVargas1/Synclass/issues/81

Investigação prévia (critério técnico do card): o Expo Router (v57, ver
`frontend/AGENTS.md`) já resolve o document title automaticamente no web
via `NavigationContainer`/`useDocumentTitle` (interno ao `expo-router`,
`build/fork/useDocumentTitle.js`) — lê `navigation.getCurrentOptions().title`
e escreve em `document.title`, reagindo a `navigation.setOptions(...)`
via listener (`addListener('options', ...)`), independente de
`headerShown:false` no `Stack` raiz. Não precisa de `expo-router/head`.
Mecanismo escolhido: `useNavigation().setOptions({ title })` — chamado uma
única vez dentro do `Topbar`, reaproveitando a prop `titulo` já existente
(sem duplicar texto por rota, critério técnico do card).

## Ordem de execução

- [ ] Teste unidade: `useTituloDaAba(titulo)` — chama `navigation.setOptions({ title: titulo })` (mock de `useNavigation` do `expo-router`)
- [ ] Teste unidade: `useTituloDaAba` — reage a mudança do argumento entre renders, chamando `setOptions` de novo com o novo título
- [ ] Implementação mínima: hook `frontend/src/lib/useTituloDaAba.ts`
- [ ] Teste de componente: `Topbar` — com `titulo`, chama `setOptions({ title: <o mesmo texto do titulo> })`
- [ ] Teste de componente: `Topbar` — sem `titulo` (variante marca), chama `setOptions({ title: 'Synclass' })`
- [ ] Implementação: `Topbar.tsx` usa `useTituloDaAba(titulo ?? 'Synclass')`

### Inconsistências encontradas

_(Nenhuma — mecanismo já confirmado pela investigação acima antes de escrever este task.md.)_
