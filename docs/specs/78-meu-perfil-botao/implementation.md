# Implementação: 'Meu perfil' vira um botão (#78)

## Componente afetado

- `frontend/src/app/painel/index.tsx` (linhas ~164-166): troca o `Link`
  de texto sublinhado por `<Link href="/perfil" asChild><Button
  label="Meu perfil" /></Link>` — padrão declarativo do `expo-router`
  (`asChild` repassa a navegação para o filho via `onPress` injetado,
  em vez de duplicar lógica com `router.push` manual). `Button`
  (`atoms/Button.tsx`) já define `accessibilityRole="button"`
  internamente, então a navegação continua exposta a leitor de tela como
  ação de botão, não como link.

## Por que `Link asChild` e não `onPress={() => router.push(...)}`

Mantém a navegação declarativa (URL como fonte de verdade, sem lógica de
navegação imperativa espalhada pelos componentes) — mesmo racional já
usado em `ItemDeAcao` (`Link href={acao.href}`) para as demais ações do
Painel, só que agora compondo com `Button` em vez de um `Link`
estilizado manualmente.

## Testes

Revisar `painel/index.test.tsx` por qualquer asserção que dependa do
`Link` renderizar como texto sublinhado (className, role de link) —
ajustar para `getByRole('button', { name: 'Meu perfil' })` e checar
navegação para `/perfil` do mesmo jeito que os testes de `ItemDeAcao`
já verificam `href`.
