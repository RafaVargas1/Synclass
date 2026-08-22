# Implementação: menu vira um painel lateral de verdade também no desktop (#161)

## Decisão de design (resolvida, não é pra DeepSeek escolher)

Hoje, em viewport larga, `Topbar.tsx` renderiza `menuNavegacao` como uma
FAIXA HORIZONTAL de largura total, numa segunda linha do cabeçalho — não
um painel lateral (issue #146 já resolveu isso pro mobile, com um painel
vertical de altura cheia; esta Task faz o equivalente pro desktop, mas
como uma coluna PERSISTENTE, não um overlay que abre/fecha, já que em
telas largas há espaço de sobra pra deixá-la sempre visível — convenção
mais comum de apps com navegação por seções, ver
`docs/spec/ux-heuristics.md#navegação-e-retorno`).

**Onde a coluna lateral é montada**: não em cada tela (evita tocar os
~15 arquivos de rota que usam `Topbar`/`TopbarAutenticada`) — no layout
raiz (`frontend/src/app/_layout.tsx`), que já envolve toda a árvore de
rotas com `SessaoProvider`. Um componente novo lá dentro (com acesso a
`useSessao()`) decide se mostra a coluna (viewport larga E sessão ativa)
e monta `MenuNavegacao` uma única vez, ao lado do `<Stack>` de rotas — as
telas continuam desenhando seu próprio `Topbar`/corpo normalmente dentro
do espaço restante, sem saber que a coluna existe.

**Consequência**: `Topbar.tsx` para de renderizar `menuNavegacao` como
faixa horizontal em viewport larga (isso agora é responsabilidade da
coluna lateral em `_layout.tsx`) — mas continua recebendo/renderizando o
botão de abrir o menu mobile normalmente (viewport estreita não muda
nada). `TopbarAutenticada.tsx` para de passar `menuNavegacao` pro
`Topbar` quando a viewport é larga (a coluna lateral já cobre esse caso).

## `frontend/src/app/_layout.tsx` — estado atual

```tsx
import '@/global.css';

import { DarkTheme, DefaultTheme, Stack, ThemeProvider } from 'expo-router';
import { StatusBar } from 'expo-status-bar';
import { useColorScheme } from 'react-native';

import { SessaoProvider } from '@/lib/auth/contexto-sessao';

export default function RootLayout() {
  const colorScheme = useColorScheme();

  return (
    <ThemeProvider value={colorScheme === 'dark' ? DarkTheme : DefaultTheme}>
      <SessaoProvider>
        <Stack screenOptions={{ headerShown: false }} />
        <StatusBar style="auto" />
      </SessaoProvider>
    </ThemeProvider>
  );
}
```

### Mudança

```tsx
import '@/global.css';

import { DarkTheme, DefaultTheme, Stack, ThemeProvider } from 'expo-router';
import { StatusBar } from 'expo-status-bar';
import { useColorScheme, View } from 'react-native';

import { MenuNavegacao } from '@/components/organisms/MenuNavegacao';
import { SessaoProvider, useSessao } from '@/lib/auth/contexto-sessao';
import { useIsTelaLarga } from '@/lib/useIsTelaLarga';

export default function RootLayout() {
  const colorScheme = useColorScheme();

  return (
    <ThemeProvider value={colorScheme === 'dark' ? DarkTheme : DefaultTheme}>
      <SessaoProvider>
        <AppShell />
        <StatusBar style="auto" />
      </SessaoProvider>
    </ThemeProvider>
  );
}

/**
 * Coluna lateral persistente do menu de navegação em viewport larga
 * (issue #161) — só quando há sessão ativa (`token`); sem sessão (Home,
 * login, cadastro) o app inteiro continua sem coluna nenhuma, mesmo em
 * tela larga. `useSessao()` funciona aqui porque este componente já está
 * dentro de `SessaoProvider`.
 */
function AppShell() {
  const { token, papeis, papelAtivo, definirPapelAtivo } = useSessao();
  const telaLarga = useIsTelaLarga();
  const mostrarColunaLateral = telaLarga && Boolean(token);

  return (
    <View className="flex-1 flex-row">
      {mostrarColunaLateral ? (
        <View
          testID="coluna-lateral-menu"
          className="border-r border-border bg-background dark:border-dark-border dark:bg-dark-background"
          style={{ width: 260 }}
        >
          <MenuNavegacao papeis={papeis} papelAtivo={papelAtivo} onSelecionarPapel={definirPapelAtivo} />
        </View>
      ) : null}
      <View className="flex-1">
        <Stack screenOptions={{ headerShown: false }} />
      </View>
    </View>
  );
}
```

## `frontend/src/components/organisms/MenuNavegacao.tsx` — ajuste

O `telaLarga` branch de `exibirSeccoes`/o bloco `border-t` (a antiga faixa
horizontal) fica sem uso quando montado a partir da coluna lateral — mas
`MenuNavegacao` continua sendo o MESMO componente, só que agora sempre
recebe `telaLarga = true` vindo de dentro de uma coluna estreita e alta
(260px), não uma faixa larga e baixa. Ajuste o branch `telaLarga` do
container de seções pra empilhar verticalmente (coluna), não
horizontalmente:

```tsx
// dentro do bloco de className condicional (View com testID="dropdown-menu-navegacao"):
// ANTES (telaLarga: true → faixa horizontal)
telaLarga
  ? 'flex-row flex-wrap items-center gap-three border-t border-border py-two dark:border-dark-border'
  : '...' // (branch mobile, sem mudança)

// DEPOIS (telaLarga: true → coluna vertical, sem borda de topo — a borda
// direita já vem do container em _layout.tsx)
telaLarga
  ? 'gap-one p-three'
  : '...' // (branch mobile, sem mudança)
```

`ItemDeSecao`'s prop `largoTotal` (hoje `!telaLarga`) passa a ser sempre
`true` nesse contexto (itens empilhados em coluna sempre ocupam a largura
toda, tanto no painel mobile quanto na coluna desktop) — troque
`largoTotal={!telaLarga}` para `largoTotal` (sempre `true`) no `.map` de
`todasSecoes`, já que não existe mais nenhum caso `telaLarga` com layout
horizontal.

`AlternadorDePapel` (Professor/Aluno) continua no topo da coluna, sem
mudança de posição.

## `frontend/src/components/organisms/Topbar.tsx` — ajuste

Remove o bloco que renderizava a faixa horizontal de `menuNavegacao` em
viewport larga (issue #145 tinha colocado isso ANTES do Voltar — esse
bloco inteiro deixa de existir, a coluna lateral em `_layout.tsx`
substitui):

```tsx
// REMOVER este bloco inteiro do corpo de Topbar:
{telaLarga ? (
  <>
    {menuNavegacao ? (
      <View className="w-full self-center" style={{ maxWidth: MaxContentWidth }}>
        {menuNavegacao}
      </View>
    ) : null}
    {titulo ? (
      <View className="w-full self-center border-t border-border px-four py-three dark:border-dark-border" style={{ maxWidth: MaxContentWidth }}>
        <BotaoVoltar />
      </View>
    ) : null}
  </>
) : (
  titulo ? (
    <View className="w-full self-center px-four pb-three" style={{ maxWidth: MaxContentWidth }}>
      <BotaoVoltar />
    </View>
  ) : null
)}
```

Substitua por (Voltar sempre na própria linha abaixo do cabeçalho,
independente de `telaLarga` — a distinção que restava era só sobre
onde o menu entrava, que não existe mais aqui):

```tsx
{titulo ? (
  <View className="w-full self-center px-four pb-three" style={{ maxWidth: MaxContentWidth }}>
    <BotaoVoltar />
  </View>
) : null}
```

O parâmetro `menuNavegacao` de `TopbarProps` continua existindo (ainda é
usado no mobile, dentro do `flex-row` do cabeçalho, sem mudança nesse
trecho) — só o branch de viewport larga é removido.

## `frontend/src/components/organisms/TopbarAutenticada.tsx` — sem mudança de código

Continua montando `<MenuNavegacao .../>` e passando pro slot
`menuNavegacao` do `Topbar` — em viewport larga, `Topbar` agora
simplesmente NÃO usa esse valor (o branch foi removido), então o
componente é montado duas vezes (uma vez aqui, sem efeito visual em
desktop; uma vez na coluna lateral de `_layout.tsx`, essa sim visível).
Isso é aceitável (o componente é barato, sem chamadas de rede próprias —
`usePerfilLogado`/`useSessao` já são compartilhados via contexto/cache),
mas se o dev-review considerar isso um problema real de duplicação,
registre como achado não-bloqueante em vez de tentar resolver dentro
desta Task (mudaria a API de `TopbarAutenticada`, mais arriscado).

## Testes

- `AppShell` (`_layout.test.tsx`, criar se não existir — verifique
  primeiro se já existe algum teste de `_layout.tsx`, senão crie do
  zero): com `useSessao` mockado retornando `token` definido e
  `useIsTelaLarga` mockado `true`, a coluna lateral (`testID`
  `coluna-lateral-menu`) aparece; sem `token`, não aparece mesmo com
  `telaLarga: true`; com `telaLarga: false`, não aparece mesmo com
  `token` definido.
- `MenuNavegacao.test.tsx`: ajuste os testes que hoje verificam a classe
  `border-t`/`flex-row` no modo `telaLarga` pra esperar a nova classe
  (coluna vertical); ajuste qualquer teste que verificasse
  `largoTotal={!telaLarga}` — agora é sempre `true`.
- `Topbar.test.tsx`: remova/ajuste o teste "em tela larga, o menu aparece
  antes do Voltar" (issue #145 revisitada) — esse comportamento não
  existe mais aqui, a asserção não faz mais sentido (o menu não renderiza
  mais dentro do `Topbar` em viewport larga). Documente no teste (ou
  remova o teste inteiro com um comentário explicando por que) que esse
  comportamento migrou pra `_layout.tsx`/`AppShell`.

## Fora de escopo

- Não mexer no painel mobile (overlay `position: fixed`, issue #146) —
  continua igual, só a variante desktop muda.
- Não adicionar opção de colapsar/expandir a coluna lateral manualmente —
  fixa em 260px sempre visível quando aplicável, sem esse controle extra
  (poderia ser um card futuro se o usuário pedir).
- Não mudar `MaxContentWidth`/centralização do conteúdo de cada tela —
  cada tela continua se auto-centralizando no espaço que sobra ao lado da
  coluna, sem ajuste extra necessário.
