# Diretrizes de UX

Este documento é o **orientador de usabilidade e interação** do Synclass —
complementa [`design-system.md`](./design-system.md) (que define aparência:
cor, forma, tipografia) com regras sobre **comportamento**: tamanho de alvo
de toque, navegação, hierarquia de informação, previsibilidade. Onde
`design-system.md` responde "como isso deve parecer", este documento
responde "como isso deve se comportar pra ser fácil de usar".

Cada regra aqui cita a referência de onde vem — não é opinião de estilo, é
achado replicado de ergonomia cognitiva e pesquisa de interação
humano-computador. Onde a literatura dá um número (ex: tamanho mínimo de
alvo de toque), o número vira regra de code review, não sugestão.

## Índice de referências

| Sigla | Referência | O que fundamenta aqui |
|---|---|---|
| Fitts | Fitts, P. M. (1954). *The information capacity of the human motor system...* — formalizado pra UI por Card, English & Burr (1978) | Tamanho e distância de alvo de toque/clique |
| Hick | Hick, W. E. (1952); Hyman, R. (1953) | Tempo de decisão cresce com o número de opções |
| Gestalt | Wertheimer (1923); aplicado a UI em Lidwell, Holden & Butler, *Universal Principles of Design* | Agrupamento visual (proximidade, similaridade, região comum) |
| Miller | Miller, G. A. (1956). *The Magical Number Seven* | Limite de itens simultâneos em memória de trabalho (usado com ressalva — pesquisa mais recente, ex. Cowan 2001, sugere um número mais baixo e mais variável; tratamos como "menos é melhor", não como "7 é o teto mágico") |
| Nielsen | Nielsen, J. (1994). *10 Usability Heuristics for User Interface Design*, Nielsen Norman Group | Heurísticas gerais de usabilidade |
| Norman | Norman, D. (1988/2013). *The Design of Everyday Things* | Affordance e signifier — o que um elemento "diz" que faz |
| Krug | Krug, S. (2000). *Don't Make Me Think* | Reconhecimento em vez de exigir raciocínio pra navegar |
| WCAG | W3C, *Web Content Accessibility Guidelines 2.2* (2023), critérios 2.5.8 (mínimo) e 2.5.5 (avançado) | Tamanho mínimo de alvo de toque, mensurável e testável |
| HIG/Material | Apple *Human Interface Guidelines*; Google *Material Design 3* | Convenção de plataforma pra controles de navegação |

## Alvos de toque

**Regra: todo elemento tocável tem no mínimo 44×44pt (iOS HIG) / 48×48dp
(Material) de área de toque real — não de desenho visual.** WCAG 2.2 SC
2.5.8 exige 24×24px CSS como piso absoluto de acessibilidade (AA); HIG e
Material pedem mais que o dobro disso como padrão de conforto. Um ícone
pode ser desenhado pequeno (a estética art deco do projeto prefere
elementos finos e geométricos) — o que importa é a área que responde ao
toque, não o traço visível.

*Fitts's Law* formaliza por quê: o tempo pra acertar um alvo é proporcional
à distância até ele e inversamente proporcional ao seu tamanho. Alvo
pequeno = mais tempo, mais erro, pior em quem tem tremor de mão, tela
grande ou está andando. Isso não é preferência — é física de movimento
medida.

**Como aplicar**: separe o glifo visual (pode ser 10–14px) da área
`Pressable`/`hitSlop` (sempre ≥44×44 efetivo). Nunca dimensione a área de
toque igual ao desenho.

## Navegação e retorno

**Regra: todo controle de "voltar" fica no mesmo lugar, com o mesmo
comportamento, em toda tela — e nunca leva a um beco sem saída.**
Consistência é a heurística #4 de Nielsen ("consistência e padrões"): o
usuário aprende o controle uma vez e reaplica esse aprendizado em todo
lugar; um "voltar" que muda de posição ou de comportamento entre telas
quebra esse modelo mental e cada tela vira um quebra-cabeça novo.

"Beco sem saída" viola a heurística #3 de Nielsen ("controle e liberdade
do usuário") — toda ação tem que ter uma saída clara (desfazer, sair,
voltar). Um botão de voltar que não faz nada quando não há histórico de
navegação (ex: usuário chegou direto por link) é pior que não ter botão
nenhum, porque parece quebrado.

**Como aplicar** (`Topbar`, `frontend/src/components/organisms/Topbar.tsx`):
sempre no canto superior esquerdo (convenção HIG/Material — usuário
ocidental lê e escaneia esquerda→direita, então "voltar" fica no ponto de
entrada natural do olhar), com fallback explícito quando não há pra onde
voltar (`canGoBack() ? back() : replace('/painel')`) em vez de desabilitar
o botão ou deixá-lo inerte.

## Reconhecimento em vez de recordação

**Regra: prefira ícone + rótulo visível a ícone sozinho, quando o ícone não
é 100% universal.** Krug resume isso como "não me faça pensar" — o
usuário não deveria ter que decifrar o que um símbolo significa; ele
deveria reconhecer de cara. Nielsen formaliza como heurística #6
("reconhecimento em vez de recordação"): informação e opções visíveis
custam menos carga cognitiva que informação que o usuário precisa lembrar
ou inferir.

Ícones amplamente reconhecidos (lupa = buscar, seta-esquerda = voltar) se
sustentam sozinhos porque o aprendizado já veio de fora do produto — o
usuário já viu esse símbolo em dezenas de outros apps (Norman chama isso
de *signifier* cultural já estabelecido). Um ícone desenhado do zero pro
projeto (nosso caso: chevron geométrico próprio, não um glifo de
biblioteca padrão) ainda não tem esse capital de reconhecimento — por
isso reforça com `accessibilityLabel` (leitor de tela) e, quando o layout
permitir, contraste/peso visual suficiente pra não depender só da forma
abstrata pra comunicar "isso volta".

## Agrupamento visual (Gestalt)

**Regra: elementos relacionados ficam visualmente próximos e elementos
não relacionados ficam visualmente separados — a separação em si já
comunica a relação, sem precisar de texto extra.** Proximidade e região
comum (Gestalt) fazem o olho agrupar automaticamente o que está perto ou
dentro do mesmo contorno, antes mesmo de ler qualquer rótulo. No app isso
já aparece nas bordas retas e nos divisores do redesign art deco — a
moldura substitui sombra, mas continua cumprindo o mesmo papel
perceptivo: dizer "isto aqui é um grupo".

**Como aplicar**: ação primária de uma tela nunca disputa peso visual com
ação secundária (ex: "Cadastrar" cheio de cor vs. "Já tenho conta" como
link de texto) — a hierarquia visual precisa ser lida antes de qualquer
leitura de texto.

## Número de opções simultâneas

**Regra: menos opções visíveis ao mesmo tempo é sempre mais rápido de
decidir, não só mais bonito.** Hick's Law: o tempo de decisão cresce
(aproximadamente logaritmicamente) com o número de opções apresentadas de
uma vez. Miller (1956) é citado com frequência como "7±2 itens", mas
pesquisa posterior (Cowan, 2001) reduz essa estimativa e mostra que ela
varia por pessoa e por contexto — a lição prática não é "nunca passe de 7
itens", é "cada opção a mais tem custo real de tempo de decisão, então
cada uma precisa se justificar".

**Como aplicar**: filtros/segmentos (ex: `FiltroDePeriodo` da tela de
valor devido) ficam com 2–4 opções nomeadas, não uma lista longa; ações do
painel agrupadas por papel (Professor vê só ações de Professor), não uma
lista única com todas as ações de todos os papéis misturadas.

---

## Feedback do sistema (erros, avisos, confirmações)

**Regra: toda ação assíncrona (rede, autenticação, salvamento) que pode
falhar ou ter um desfecho não óbvio precisa de feedback visível — nunca
fica muda entre o toque e o resultado** (Nielsen #1, visibilidade do
status do sistema). Quando esse feedback é um erro, ele precisa ajudar a
pessoa a entender o que houve e como seguir, não só apontar que algo deu
errado (Nielsen #9, ajudar a reconhecer/diagnosticar/recuperar de erros).

**Como aplicar**: use `Notificacao`
(`frontend/src/components/organisms/Notificacao.tsx`) via
`useNotificacoes()` (`frontend/src/lib/notificacoes/contexto-notificacoes.tsx`)
como a forma padrão de mostrar erro/aviso/informação/sucesso em qualquer
tela nova ou tocada por uma Task — **antes de inventar um padrão novo ou
reaproveitar só `ErrorMessage` para um caso que não é erro de campo de
formulário**, prefira este componente:

- `ErrorMessage` continua certo para erro de validação **inline, junto do
  campo** (ex: "Informe um valor maior que zero." abaixo de um
  `FormField`) — o erro precisa aparecer perto de onde a pessoa vai
  corrigi-lo, não flutuando longe do campo.
- `Notificacao`/`useNotificacoes` é a forma certa para qualquer feedback
  que não é sobre um campo específico: falha de rede, sucesso de uma
  operação assíncrona, aviso sobre algo que vai acontecer, confirmação de
  uma ação concluída — `notificar({ tipo: 'erro' | 'aviso' | 'informacao'
  | 'sucesso', mensagem, duracaoMs? })`.

**Por que um componente único, não um `Alert`/toast improvisado por
tela**: cor sozinha nunca é o único sinal (WCAG) — `Notificacao` sempre
mostra um rótulo textual ("Erro"/"Aviso"/...) junto da cor, então basta
usar o `tipo` certo pra herdar isso automaticamente. `duracaoMs`
configurável por chamada, mas com default por severidade: erro **não**
soma sozinho por padrão (precisa de dispensa manual — um erro que some
antes de ser lido derrota o propósito de mostrá-lo), os demais tipos
somem em 5s. Sempre com botão de fechar visível, independente da
duração — controle do usuário sobre quando dispensar (Nielsen #3; WCAG
2.2.1, conteúdo com prazo precisa poder ser dispensado/estendido pela
pessoa, não só por timer). Só uma notificação ativa por vez (Hick's
Law) — mostrar uma nova substitui a anterior, em vez de empilhar.

---

## Aplicação: revisão do botão de voltar (`Topbar`)

Auditoria de `frontend/src/components/organisms/Topbar.tsx` contra as
regras acima, feita ao escrever este documento:

| Critério | Estado antes | Regra violada | Correção |
|---|---|---|---|
| Área de toque | Glifo 10×10px + `hitSlop={8}` = ~26×26px efetivo | Fitts / WCAG 2.5.8 (24×24 é o piso, não o alvo) / HIG-Material (44×44 / 48×48) | `Pressable` ganhou `minWidth`/`minHeight` de 44 explícitos, com o glifo continuando pequeno e centralizado dentro dela |
| Peso visual do ícone | Traço 2px, sem preenchimento | Norman (signifier fraco pra um ícone não-universal) | Traço mantido fino (linguagem art deco), mas dentro de uma área de toque clara — o objetivo era tamanho de alvo, não engrossar o traço |
| Posição | Canto superior esquerdo, fixo em toda tela | Nenhuma — já seguia Nielsen #4/HIG | Mantido |
| Beco sem saída | `canGoBack() ? back() : replace('/painel')` | Nenhuma — já seguia Nielsen #3 | Mantido |
| Rótulo acessível | `accessibilityLabel="Voltar"` presente | Nenhuma — já atendia leitor de tela | Mantido |

A correção de área de toque foi aplicada em
`frontend/src/components/organisms/Topbar.tsx` no mesmo commit que este
documento.
