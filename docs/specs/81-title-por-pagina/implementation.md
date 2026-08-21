# Implementação: title por página na aba do navegador (#81)

## Componentes afetados

- **Novo hook** `frontend/src/lib/useTituloDaAba.ts`: `useTituloDaAba(titulo: string)`
  chama `useNavigation().setOptions({ title: titulo })` num `useLayoutEffect`
  (antes da pintura, evita flash do título anterior). Reexecuta quando
  `titulo` muda (dependência do efeito), cobrindo a navegação entre rotas
  sem reload — o próprio `expo-router` reage ao evento `options` do
  navigator.
- **Modificado** `frontend/src/components/organisms/Topbar.tsx`: chama
  `useTituloDaAba(titulo ?? 'Synclass')` — ponto único de integração,
  cobre todas as ~19 rotas que já renderizam `Topbar`/`TopbarAutenticada`
  sem precisar tocar cada arquivo de rota individualmente.

## Por que centralizar no Topbar, não em cada rota

O critério técnico do card pede "não duplicar" o texto do título — como
toda tela autenticada e a maioria das anônimas já passam `titulo` (ou
omitem, pra variante marca) ao montar `Topbar`, colocar o hook ali dá
cobertura automática de 100% das rotas sem exportar `<Stack.Screen
options={{title}}/>` em cada arquivo de `app/`.

## Por que `useNavigation().setOptions` e não `document.title =` direto

`expo-router` já embute `useDocumentTitle` (biblioteca interna,
`NavigationContainer`) que escreve em `document.title` reagindo a
`navigation.setOptions`. Escrever `document.title` manualmente no Topbar
duplicaria a responsabilidade e correria risco de condição de corrida com
o listener interno (o interno reage por evento, o manual reagiria por
efeito de render — sem garantia de ordem). Usar o `setOptions` já
oferecido pelo React Navigation é a integração suportada, não uma
API interna do expo-router.

## Testes

`useTituloDaAba.test.ts`: mocka `useNavigation` de `expo-router`,
confirma a chamada de `setOptions` com o título certo e a reexecução ao
trocar o argumento. `Topbar.test.tsx`: adiciona duas asserções (com e sem
`titulo`) sobre o mock de `setOptions`, sem tocar nos testes existentes.
