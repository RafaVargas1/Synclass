# Sistema de estilização

Este documento é o **orientador de aparência** do Synclass — cores, tipografia,
forma, ícones, movimento e voz de conteúdo, para app (React Native/Expo) e para
qualquer superfície pública (ex: tela inicial de cadastro). Ele complementa
[`code-style.md`](./code-style.md) (como o código é escrito),
[`architecture.md`](./architecture.md) (stack e camadas) e
[`ux-heuristics.md`](./ux-heuristics.md) (como a interface deve se
*comportar* — tamanho de alvo de toque, navegação, hierarquia — com base
em pesquisa de usabilidade, não em preferência de estilo); este aqui
define **como tudo deve parecer**.

Os tokens vivem em `frontend/src/theme/palette.js` (fonte única, consumida por
`tailwind.config.js` e por `theme/tokens.ts`) e em `frontend/src/global.css`
(fontes web). Este documento é a justificativa por trás desses valores — ao
alterar um token, atualize também a tabela correspondente aqui.

## Guardrails

Lista fechada de padrões banidos, definida pelo Rafael. Qualquer decisão de UI
nova passa por aqui antes de ir pra tela. Agrupei por tema para facilitar
checagem; o número entre parênteses é o item original da lista.

**Cor e superfície**
- Sem gradientes fortes (1). Sem fundo branco puro `#FFFFFF` (3). Sem
  coloração arco-íris — paleta enxuta, não decorativa (4). Sem sombra/drop
  shadow (5). Sem vidro líquido / glassmorphism translúcido (8). Sem faixa
  colorida na lateral de card/alerta (11). Sem roxo combinado com preto (20).
  Sem orbes radiais de fundo (22). Sem grade de pontos decorativa (23). Sem
  cores neon (29). Sem paleta pastel genérica de dashboard (30).

**Forma**
- Sem raio de borda suave/arredondamento excessivo — nada de pílula em botão
  retangular (19).

**Ícone e ilustração**
- Sem ícones Lucide (2). Sem emoji em UI ou copy (7). Sem ícone de sparkle/✨ (24).
  Sem seta animada (25).

**Composição (padrões de landing page)**
- Sem grid de 3 feature cards (6). Sem depoimento falso/fabricado (12). Sem
  bento grid (13). Sem janela de terminal decorativa (14). Sem lista com
  bullet de checkmark (16). Sem 3 planos de preço (17).

**Voz e conteúdo**
- Sem travessão/em dash na copy (9). Sem fonte Inter, Geist ou Space Grotesk
  (10). Sem a construção retórica "não é X, é Y" (15).

**Interação e estado**
- Sem loading skeleton/shimmer (21). Sem animação de hover (28).

**Fora de escopo agora** (não é regra de estilo — é corte de escopo)
- Sem seção de demo de produto por enquanto (18). Sem página de Termos de Uso
  (26). Sem Política de Privacidade real — usar placeholder (27).

> **Item 18 é ambíguo no texto original** ("no real product demos" dentro de
> uma lista que já começa com "NÃO QUERO"). Interpretei como "não construir
> agora uma seção dedicada de demo" — coerente com 26/27 (conteúdo prematuro
> pra um produto ainda em fundação) e não como "nunca mostrar o produto real".
> Se a intenção era o oposto (proibir demos *falsas*, permitir reais), me avise
> e eu ajusto a seção "Fora de escopo".

## Princípio geral

Cor, forma e ícone só aparecem quando carregam informação real — estado,
hierarquia ou ação. Nenhum elemento é puramente decorativo. Isso é o fio que
conecta metade dos guardrails acima (sem orbe, sem grade de pontos, sem
gradiente, sem sparkle, sem seta animada, sem bento grid): a interface não usa
enfeite pra parecer sofisticada, usa clareza.

## Cor

Fundo e superfícies usam neutros levemente frios, nunca extremos puros
(`#FFFFFF`/`#000000`). Isso também resolve um problema prático: sem drop
shadow disponível, a diferença de luminância entre `background` e
`background-element` É a única forma de comunicar elevação — extremos puros
não deixam espaço pra esse degradê.

| Token | Light | Dark | Uso |
|---|---|---|---|
| `background` | `#F5F6F8` | `#121317` | fundo de tela |
| `background-element` | `#E7E9ED` | `#1D1F24` | card, input, superfície elevada |
| `background-selected` | `#D8DBE1` | `#292C33` | chip/item selecionado, hover-equivalente em toque |
| `border` *(novo)* | `#D3D6DC` | `#34373E` | borda de 1px — substitui sombra como sinal de elevação |
| `text` | `#14161A` | `#F5F6F8` | texto primário |
| `text-secondary` | `#5B616B` | `#A6ACB6` | texto de apoio, legenda |
| `primary` | `#1873BD` | `#4DA3F5` | ação, link, estado ativo |
| `error` | `#DC2626` | `#F87171` | erro/validação |

Mudanças em relação ao `palette.js` atual: `background` sai de branco/preto
puro; `primary` no light escurece de `#208AEF` para `#1873BD` — o tom atual
tem contraste apertado quando usado como texto/link sobre fundo claro (ex:
"Já tenho conta — Entrar" em `HomeHero`), e vai continuar servindo bem como
fundo de botão com texto branco. Valide contraste (mínimo AA, 4.5:1 para
texto normal) antes de aplicar — não tenho uma ferramenta de contraste aqui,
então trate os hexadecimais acima como ponto de partida, não valor final.
`border` é um token novo — não existe hoje porque nada precisava de linha
divisória com sombra disponível; agora precisa.

Não adicionar cor de "sucesso" enquanto nenhuma tela precisar dela — hoje
`LoginConfirmado`/`CadastroConfirmado` usam texto normal, e isso está certo.
Quando surgir a necessidade, um verde equivalente em saturação ao `primary`
atual, nunca pastel.

## Tipografia

No nativo (iOS/Android), `theme/tokens.ts` já usa a fonte de sistema
(`system-ui` no iOS = San Francisco, `normal` no Android = Roboto) — nem
Inter, nem Geist, nem Space Grotesk. Não mexer nisso, é a decisão certa:
fonte nativa carrega de graça e parece parte do OS.

O único ponto fora do guardrail é o **fallback web**, em `global.css`:

```css
/* atual — remover Inter da cadeia */
--font-display: Spline Sans, Inter, ui-sans-serif, system-ui, sans-serif, ...;

/* corrigido */
--font-display: 'Spline Sans', ui-sans-serif, system-ui, -apple-system, sans-serif;
```

Spline Sans continua sendo a fonte de exibição no web (título, botão, corpo).
Se o peso carregado de Spline Sans falhar antes de o CSS aplicar, o navegador
cai pra fonte de sistema — nunca pra Inter/Geist/Space Grotesk.

`--font-mono` (pilha `ui-monospace` já existente) fica reservado pra dado
que se beneficia de largura fixa — ex: exibir o código de uso único do login
(issue #25). Não é "janela de terminal" (guardrail 14): é um único trecho de
texto monoespaçado dentro do layout normal, sem moldura/chrome de terminal.

`--font-rounded` e `--font-serif` estão definidos em `tokens.ts` mas sem
nenhum uso hoje — não é violação de guardrail, é escopo morto. Não removo
aqui porque isso é decisão de code-style, não de estilização; ver
[`code-style.md`](./code-style.md) se decidirem limpar.

## Forma e elevação

Raio reduzido — nunca pílula em elemento retangular. Círculo só em elemento
que é de fato circular (avatar de inicial, indicador de status).

| Token | Antes | Depois | Uso |
|---|---|---|---|
| `small` | 6px | 4px | chip, badge, input pequeno |
| `medium` | 12px | 8px | botão, input, card |
| `large` | 20px | 14px | modal/bottom sheet — teto do raio, nunca em botão |

Elevação sem sombra é feita só por dois recursos, combináveis:

1. **Contraste de fundo** — `background` → `background-element` →
   `background-selected` já forma uma escada de luminância.
2. **Borda de 1px** — `border` (token novo, tabela de cor acima) delimita
   card/input sem precisar de sombra. Estado de foco usa borda de 1–2px na
   cor `primary`, não glow.

## Ícones

Lucide está fora. Recomendo **Phosphor** (`phosphor-react-native`, MIT,
mantida ativamente) — silhueta visualmente distinta de Lucide, com pesos
variáveis (regular/bold) que dão uma identidade própria sem precisar de ícone
customizado do zero. Alternativa se Phosphor não servir: Tabler Icons.

Regras de uso:
- Peso `regular` por padrão; `bold` só pra ênfase pontual (ex: ícone de erro).
- Tamanho fixo em dois passos: 20px (inline com texto) e 24px (ação
  standalone, como botão de ícone).
- Ícone sempre acompanha um rótulo acessível (`accessibilityLabel`) — nunca é
  o único portador de significado.
- Nada de sparkle (✨) em nenhum contexto — não é um produto de "IA em
  destaque", não precisa do símbolo que virou clichê disso.

## Movimento e interação

Sem animação de hover (guardrail 28) — mas o app ainda precisa de feedback de
toque, senão fica inacessível. A leitura que uso: o que é banido é o efeito
decorativo de hover (lift com transform, glow, ícone saltando, seta
deslizando) — não estado nenhum. `Button.tsx` já faz isso certo hoje
(`active:opacity-80`, sem transição, sem transform): é o padrão a manter.

- Feedback de toque = troca discreta de opacidade ou cor no `onPressIn`/
  `active:`, sem `transition`/easing. Instantâneo, não animado.
- Nenhum elemento se move sozinho (seta pulsando, ícone flutuando, gradiente
  animado) — guardrail 25 cobre isso.
- Estado de foco (teclado/leitor de tela) usa borda `primary`, sem glow.

## Estado de carregamento

Sem skeleton/shimmer (guardrail 21). Dois padrões, conforme o caso:

- **Ação pontual** (submit de formulário, botão): o próprio botão troca o
  label por um spinner simples de uma cor, mantém o tamanho, fica
  `disabled`. Não swap de skeleton — o botão já existe, só muda o conteúdo.
- **Carregamento de tela/lista** (ex: horários do professor): texto simples
  com spinner inline (`Carregando horários...`), sem caixa placeholder
  imitando o formato do conteúdo final.

## Composição — padrões de landing page

Aplica-se à tela inicial (`HomeTemplate`/`HomeHero`) e qualquer superfície
pública futura. Tabela padrão banido → alternativa:

| Banido | Por quê | Alternativa |
|---|---|---|
| Grid de 3 feature cards (6) | Formato genérico de landing gerada por IA | Prosa direta ligada à tela real do produto; se listar capacidades, não force o número 3 |
| Depoimento falso (12) | Prova social fabricada é desonesta | Sem seção de depoimento até existir um depoimento real, com nome/foto/consentimento |
| Bento grid (13) | Mosaico assimétrico decorativo | Layout linear de coluna única ou grid regular, sem célula de tamanho aleatório |
| Janela de terminal (14) | Prop de "tech credibility" que não informa nada | Se for mostrar código/dado, mostrar sem moldura de terminal |
| Bullet com checkmark (16) | Lista genérica de "benefícios" | Frase corrida, específica sobre o que a tela faz |
| 3 planos de preço (17) | Ainda não existe modelo de cobrança | Não construir seção de preço agora — quando existir, não forçar 3 colunas |

## Voz e conteúdo

- Sem travessão (—). Trocar por vírgula, ponto ou parênteses.
- Sem emoji em nenhum texto de UI ou microcopy.
- Sem a estrutura "não é X, é Y". Afirme o que a tela faz, direto.
- Copy específica em vez de vaga — "Professor cadastra horário disponível"
  em vez de "Gerencie sua agenda com facilidade".

## Fora de escopo agora

- **Termos de Uso**: nenhuma página/link ainda.
- **Política de Privacidade**: página existe (loja de apps costuma exigir
  link de privacidade mesmo cedo no ciclo de vida do app), mas com
  placeholder curto e honesto — algo como "Política de privacidade em
  elaboração. Publicaremos antes do lançamento." — nunca lorem ipsum.
- **Seção de demo de produto**: não construir agora (ver nota de ambiguidade
  do item 18 na seção Guardrails).

## Aplicação

Este documento não altera código sozinho. Quando o refactor de estilização
começar, os arquivos que mudam são:

- `frontend/src/theme/palette.js` — cores e raio (tabelas acima)
- `frontend/src/global.css` — cadeia de fallback de `--font-display`
- `frontend/tailwind.config.js` — herda de `palette.js`, provavelmente sem
  mudança direta além de adicionar o token `border`
- Componentes que hoje assumem branco puro implícito ou raio grande — audit
  aponta pra `atoms/Button.tsx`, `atoms/Input.tsx` e os cards em
  `organisms/HorarioCard.tsx` como primeiros candidatos

## Checklist antes de mergear uma tela nova

- [ ] Nenhuma cor fora da paleta definida acima
- [ ] Fundo não é branco/preto puro
- [ ] Nenhuma sombra — elevação por borda/contraste
- [ ] Raio dentro da escala (small/medium/large), nunca pílula em retângulo
- [ ] Ícone é Phosphor (ou o conjunto escolhido), nunca Lucide, nunca emoji
- [ ] Feedback de toque é troca de estado instantânea, sem transform/glow
- [ ] Carregamento não usa skeleton
- [ ] Copy sem travessão, sem "não é X é Y", sem emoji
- [ ] Se for tela pública: sem grid de 3 cards, sem bento, sem depoimento
      fabricado, sem checkmark bullet
