# Implementação: menu mobile vira painel lateral de altura cheia (#146)

## Estado atual (`frontend/src/components/organisms/MenuNavegacao.tsx`, lido de verdade)

```tsx
{exibirSeccoes ? (
  <View
    testID="dropdown-menu-navegacao"
    className={
      telaLarga
        ? 'flex-row flex-wrap items-center gap-three border-t border-border py-two dark:border-dark-border'
        : 'absolute left-0 top-full z-20 min-w-[220px] gap-two rounded-medium border border-border bg-background p-three shadow-md dark:border-dark-border dark:bg-dark-background'
    }
  >
    ...
  </View>
) : null}
```

No mobile (`!telaLarga`), o painel é `position: absolute`, ancorado
`top-full` do próprio botão hambúrguer (que fica dentro do cabeçalho,
altura ~44px) — então o painel só cai logo abaixo do botão, com altura
`auto` (do tamanho do conteúdo), largura mínima 220px. Resultado: uma
caixinha pequena flutuando no canto superior esquerdo, desproporcional ao
tamanho da tela (achado do usuário via screenshot real, issue #146) — não
parece um "menu lateral", parece um popover solto.

O backdrop (`Pressable` logo acima desse bloco no mesmo arquivo) já usa
`position: 'fixed'` (guardado por `Platform.OS === 'web'`) pra cobrir o
viewport inteiro, escapando do container pequeno do cabeçalho — mesma
técnica que o painel precisa adotar pra ocupar a altura cheia da tela.

## Mudança

Trocar o posicionamento do painel mobile de `absolute` (ancorado no botão)
para `fixed` (ancorado no viewport, como o backdrop), ocupando a altura
inteira da tela, largura fixa maior (280px, cabe confortavelmente rótulos
de duas linhas como "Alocar Aluno em horário" sem quebrar demais), e sem
borda arredondada (painel lateral não é um card flutuante, é uma extensão
da borda da tela):

```tsx
{exibirSeccoes ? (
  <View
    testID="dropdown-menu-navegacao"
    className={
      telaLarga
        ? 'flex-row flex-wrap items-center gap-three border-t border-border py-two dark:border-dark-border'
        : 'z-20 w-[280px] gap-two border-r border-border bg-background p-four shadow-lg dark:border-dark-border dark:bg-dark-background'
    }
    style={telaLarga ? undefined : { position: 'fixed' as 'absolute', top: 0, left: 0, bottom: 0 }}
  >
    ...
  </View>
) : null}
```

Notas:
- Mesmo padrão já usado no backdrop deste arquivo pro `position: 'fixed'`
  (cast `as 'absolute'` pra satisfazer o tipo de `ViewStyle` do React
  Native, que não lista `'fixed'` — só existe de fato no output web).
  Não precisa de guard `Platform.OS === 'web'` aqui como no backdrop,
  porque esse `style` só é aplicado quando `!telaLarga` E o componente
  inteiro já assume comportamento de overlay mobile-only nesse branch;
  native (iOS/Android) não usa React Native Web, então a string
  `'fixed'` nunca chega no motor de estilo nativo de verdade — mas
  **confirme isso rodando a suíte de testes em modo nativo/jsdom padrão
  antes de finalizar** (não deve quebrar, RNTL não valida o valor do
  enum `position`).
- `w-[280px]` fixo (não `min-w`) — o painel deve ter largura previsível
  de "faixa lateral", não crescer com o conteúdo mais longo.
- Removido `rounded-medium` (não faz sentido borda arredondada numa faixa
  que encosta nas bordas superior/inferior da tela) e `min-w-[220px]`.
- `border-r` no lugar de `border` (borda só do lado que separa o painel
  do conteúdo, já que agora ele encosta nas bordas da tela em cima/baixo/
  esquerda).

## Testes (`MenuNavegacao.test.tsx`, já existe — ajustar/estender)

- Teste existente "em modo mobile aberto, todos os 6 itens são filhos
  diretos do dropdown com fundo opaco" (issue #129) continua válido sem
  mudança de query — só a classe/style do container mudou, não a
  estrutura de filhos.
- **Novo teste**: no modo mobile aberto, o container do dropdown
  (`getByTestId('dropdown-menu-navegacao')`) tem `style` contendo
  `position: 'fixed'` e `top: 0`/`left: 0`/`bottom: 0` — confirma que o
  painel escapa do container do cabeçalho (RNTL não mede layout real,
  mas confirma que o style pretendido está lá, mesmo padrão já usado
  pelos testes de classe/estrutura desta suíte).
- **Novo teste**: a classe do container no modo mobile NÃO contém
  `rounded-medium` nem `min-w-[220px]` (confirma que a forma antiga de
  popover foi removida, não apenas escondida atrás da nova).

## Fora de escopo

- Não mexer no modo desktop (`telaLarga`) — só o painel mobile muda de
  forma.
- Não mexer no backdrop (`Pressable` acima deste bloco) — já correto.
- Não mexer em `Topbar.tsx` (issue #145, arquivo separado — se rodar em
  paralelo, uma das duas Tasks vai precisar rebasear sobre a outra antes
  do merge, não é um problema de implementação).
