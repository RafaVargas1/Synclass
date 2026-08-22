# Implementação: botão Voltar com texto, numa linha própria (#145)

## Estado atual (`frontend/src/components/organisms/Topbar.tsx`, lido de verdade)

```tsx
export function Topbar({ titulo, children, menuNavegacao }: TopbarProps) {
  useTituloDaAba(titulo ?? 'Synclass');
  const telaLarga = useIsTelaLarga();

  return (
    <View className="z-20 border-b border-border bg-background dark:border-dark-border dark:bg-dark-background">
      <View
        className="w-full flex-row items-center justify-between self-center px-four py-three"
        style={{ maxWidth: MaxContentWidth }}
      >
        <View className="flex-row items-center gap-three">
          {!telaLarga && menuNavegacao ? menuNavegacao : null}
          {titulo ? <TituloComVoltar titulo={titulo} /> : <Logotipo />}
        </View>
        {children ? <View className="flex-row items-center gap-three">{children}</View> : null}
      </View>
      {telaLarga && menuNavegacao ? (
        <View className="w-full self-center" style={{ maxWidth: MaxContentWidth }}>
          {menuNavegacao}
        </View>
      ) : null}
    </View>
  );
}

function TituloComVoltar({ titulo }: { titulo: string }) {
  const router = useRouter();
  const voltar = () => (router.canGoBack() ? router.back() : router.replace('/'));

  return (
    <View className="flex-row items-center gap-three">
      <Pressable accessibilityRole="button" accessibilityLabel="Voltar" onPress={voltar} ...>
        <View className="border-l-2 border-t-2 ..." style={{ width: 12, height: 12, transform: [{ rotate: '-45deg' }] }} />
      </Pressable>
      <Text accessibilityRole="header" className="text-lg font-bold ...">{titulo}</Text>
    </View>
  );
}
```

## Mudança: separar o controle de voltar do título, numa linha abaixo

`TituloComVoltar` vira só `Titulo` (o `<Text accessibilityRole="header">`,
sem a seta) — o botão de voltar sai da linha do cabeçalho e vira uma linha
própria, **abaixo** da linha do título/marca, com seta + texto "Voltar":

```tsx
export function Topbar({ titulo, children, menuNavegacao }: TopbarProps) {
  useTituloDaAba(titulo ?? 'Synclass');
  const telaLarga = useIsTelaLarga();

  return (
    <View className="z-20 border-b border-border bg-background dark:border-dark-border dark:bg-dark-background">
      <View
        className="w-full flex-row items-center justify-between self-center px-four py-three"
        style={{ maxWidth: MaxContentWidth }}
      >
        <View className="flex-row items-center gap-three">
          {!telaLarga && menuNavegacao ? menuNavegacao : null}
          {titulo ? <Titulo titulo={titulo} /> : <Logotipo />}
        </View>
        {children ? <View className="flex-row items-center gap-three">{children}</View> : null}
      </View>
      {titulo ? (
        <View className="w-full self-center px-four pb-three" style={{ maxWidth: MaxContentWidth }}>
          <BotaoVoltar />
        </View>
      ) : null}
      {telaLarga && menuNavegacao ? (
        <View className="w-full self-center" style={{ maxWidth: MaxContentWidth }}>
          {menuNavegacao}
        </View>
      ) : null}
    </View>
  );
}

function Titulo({ titulo }: { titulo: string }) {
  return (
    <Text accessibilityRole="header" className="text-lg font-bold text-text dark:text-dark-text">
      {titulo}
    </Text>
  );
}

function BotaoVoltar() {
  const router = useRouter();
  const voltar = () => (router.canGoBack() ? router.back() : router.replace('/'));

  return (
    <Pressable
      accessibilityRole="button"
      accessibilityLabel="Voltar"
      onPress={voltar}
      className="flex-row items-center gap-two self-start"
      style={AlvoDeToqueMinimo}
    >
      <View
        className="border-l-2 border-t-2 border-text dark:border-dark-text"
        style={{ width: 10, height: 10, transform: [{ rotate: '-45deg' }] }}
      />
      <Text className="text-sm font-semibold text-text dark:text-dark-text">Voltar</Text>
    </Pressable>
  );
}
```

Pontos importantes:
- `style={AlvoDeToqueMinimo}` continua no `Pressable` (alvo de toque
  ≥44×44 — não regredir issue #115), mesmo agora tendo texto visível
  (o padding do alvo mínimo aumenta a área de toque além do texto, não é
  redundante).
- `accessibilityLabel="Voltar"` permanece igual — os testes que já usam
  `getByLabelText('Voltar')` continuam funcionando sem mudança de query.
- `Titulo` (sem seta) ainda usa `accessibilityRole="header"` — nenhuma
  mudança de acessibilidade de heading, só o texto "Voltar" que passa a
  existir meio a mais na árvore.

## Testes (`Topbar.test.tsx`, já existe — ajustar)

- Teste `'shows the titulo with a back button instead of the mark'`:
  continua válido (o título ainda aparece), sem mudança.
- Testes `'goes back when there is history...'` e `'replaces with /
  (Home)...'`: continuam válidos — `getByLabelText('Voltar')` ainda
  encontra o `Pressable` (agora com filho `Text` "Voltar" também).
- Teste do alvo de toque (`'gives the back button a touch target of at
  least 44x44...'`): continua válido, mesmo `style` no `Pressable`.
- **Novo teste**: o texto "Voltar" é visível (`screen.getByText('Voltar')`)
  quando `titulo` é passado — antes não existia texto nenhum, só a seta.
- **Novo teste**: quando `titulo` NÃO é passado (tela raiz, mostra
  `Logotipo`), não existe nenhum botão "Voltar" (`queryByLabelText('Voltar')`
  é `null`) — comportamento já implícito hoje, mas sem teste explícito.

## Fora de escopo

- Não mexer em `MenuNavegacao.tsx` (esse é o card #146, sobre a forma do
  painel mobile — arquivo diferente na prática, mas ambos tocam
  `Topbar.tsx`; se as duas Tasks rodarem em paralelo em worktrees
  diferentes, uma delas vai precisar rebasear sobre a outra antes do
  merge — não é um problema de implementação, só sequenciamento de PR).
- Não mudar a lógica de `voltar()` (fallback `/`) — já corrigida na issue
  #141, só reposicionando o JSX.
