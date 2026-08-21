# Implementação: Repensar o separador do Topbar (#68)

## Componentes afetados

- `frontend/src/components/organisms/Topbar.tsx` — remove o import e o
  render de `<ZigzagDivider />`. O `View` externo já tem
  `className="border-b border-border ... dark:border-dark-border"`, que
  passa a ser a única linha divisória sob o cabeçalho.
- `frontend/src/components/atoms/ZigzagDivider.tsx` — removido por completo.
  Não há outro uso no repo (`grep -rn "ZigzagDivider" frontend/src` só
  retorna o próprio arquivo e o import em `Topbar.tsx`).

## Por que não há solução nova a desenhar

O critério técnico do card pedia avaliar substituir o zigue-zague por uma
`View` simples com borda — mas essa solução **já existe** no próprio
`Topbar.tsx`: o `border-b border-border` no container externo, mesmo padrão
de "elevação por borda" de `docs/spec/design-system.md#forma-e-elevação`.
O `ZigzagDivider` era puramente decorativo, renderizado *depois* do
conteúdo mas *dentro* do `View` com a borda — ou seja, a borda de 1px já
aparecia visualmente abaixo do zigue-zague, redundante. Bastava remover o
elemento decorativo.

Isso também resolve o critério de consistência nativo/web sem esforço
extra: `border-b`/`border-border` são classes NativeWind padrão, suportadas
igual em iOS/Android/web — ao contrário do `backgroundImage` que só
funcionava no React Native Web.

## Testes

`Topbar.test.tsx` não testa o `ZigzagDivider` diretamente (não há
`getByTestId`/asserção sobre ele nos testes atuais) — a suíte existente
deve continuar passando sem alteração. Não há teste dedicado a
`ZigzagDivider.tsx` para remover.
