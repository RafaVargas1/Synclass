# Implementation: Preencher melhor o layout em telas largas (web) (#69)

## Decisão de design (a que o card pedia pra confirmar antes de implementar)

O card lista, como critério técnico, um levantamento das 19 telas que usam
`MaxContentWidth` hoje, classificando cada uma como "formulário curto" ou
"painel/lista". Esse levantamento foi feito (ver tabela abaixo), mas o
**escopo de implementação desta Task fica só no Painel** — é a única tela
citada explicitamente nos Critérios de aceite Gherkin do card ("Dado o
Painel em uma tela larga... usa a largura/altura disponível"). Nenhum
critério de aceite pede para as demais telas de lista (`horarios`,
`valor-devido`, `alocacoes`, `chamada`, `minhas-aulas`,
`historico-frequencia`) mudarem de largura — alargar as 9 sem um critério
que peça isso seria expandir escopo por conta própria (ver
`AGENTS.md#antes-de-modificar-código`). Elas ficam classificadas como
candidatas, documentadas aqui, para uma Task futura decidir caso a caso
(cada uma tem um layout de conteúdo diferente — lista vertical simples vs.
tabela vs. calendário — que merece a mesma reflexão de design que o Painel
recebeu, não um valor de largura aplicado em bloco).

### Levantamento das 15 telas que usam `MaxContentWidth` hoje

| Tela | Classificação | Motivo |
|---|---|---|
| `app/painel/index.tsx` | Painel — **muda nesta Task** | único citado no Gherkin; cards de ação já usam `flex-wrap` (`ListaDeAcoes`), plano de fundo vazio em telas largas |
| `app/login/index.tsx` | Formulário curto — mantém 800px | 1-2 campos |
| `app/professor/cadastro.tsx` | Formulário curto — mantém 800px | formulário de cadastro |
| `app/professor/alunos/cadastro.tsx` | Formulário curto — mantém 800px | formulário de cadastro |
| `app/professor/[professorId]/convites/novo.tsx` | Formulário curto — mantém 800px | formulário de convite |
| `app/perfil.tsx` | Formulário curto — mantém 800px | edição de nome, campo único |
| `app/professor/[professorId]/matriculas/[matriculaId]/regra-de-cobranca.tsx` | Formulário curto — mantém 800px | formulário de regra |
| `app/professor/[professorId]/horarios.tsx` | Painel/lista — candidata (fora do escopo desta Task) | lista de horários, mas tem formulário de cadastro embutido no topo — largura maior pode brigar com o form |
| `app/professor/[professorId]/valor-devido.tsx` | Painel/lista — candidata (fora do escopo) | lista de Alunos com valor devido |
| `app/aluno/valor-devido.tsx` | Painel/lista — candidata (fora do escopo) | lista por Professor |
| `app/aluno/professores/[professorId]/horarios.tsx` | Painel/lista — candidata (fora do escopo) | lista de horários vagos |
| `app/professor/[professorId]/alocacoes.tsx` | Painel/lista — candidata (fora do escopo) | lista de alocações |
| `app/professor/[professorId]/horarios/[horarioId]/chamada.tsx` | Painel/lista — candidata (fora do escopo) | lista de presença/chamada |
| `app/aluno/historico-frequencia.tsx` | Painel/lista — candidata (fora do escopo) | lista de frequência, tem filtro de período |
| `app/aluno/professores/[professorId]/minhas-aulas.tsx` | Painel/lista — candidata (fora do escopo) | lista de aulas |

(O card menciona 19 telas; o levantamento atual em `frontend/src/app/**`
encontrou 15 usos de `MaxContentWidth` — a diferença provavelmente conta
sub-rotas ou telas que herdam o valor indiretamente; não foi investigada
mais a fundo por não mudar a decisão acima.)

## Entidades/componentes afetados

- `frontend/src/theme/tokens.ts`: novo `MaxContentWidthPainel = 1120`
  (mantém `MaxContentWidth = 800` como está, sem tocar nas outras 14
  telas).
- `frontend/src/lib/periodoDoDia.ts` (novo arquivo): função pura
  `periodoDoDia(hora: number): 'manha' | 'tarde' | 'noite'` (`0-11` manhã,
  `12-17` tarde, `18-23` noite) + `saudacaoPorPeriodo` (mapa período →
  texto). Sem dependência de fuso do servidor — usa `new Date().getHours()`
  do dispositivo, no componente, não no util (util fica puro/testável sem
  mockar `Date`).
- `frontend/src/app/painel/index.tsx`:
  - `useUsuarioIdLogado` → renomeia para `usePerfilLogado`, buscando
    `buscarPerfil()` sempre que há `token` (tira o gate
    `papelAtivo !== 'Professor'` do efeito) e devolvendo `nome` além de
    `usuarioId`. `acoesProfessor`/`acoesDoPapel` continuam recebendo só
    `usuarioId` (Aluno não usa `usuarioId`, só `nome` agora).
  - Container principal (`style={{ maxWidth: MaxContentWidth }}`) passa a
    usar `MaxContentWidthPainel`.
  - Novo `<Heading>` acima de `<AlternadorDePapel>`:
    `` `${saudacaoPorPeriodo[periodoDoDia(new Date().getHours())]}, ${nome}` ``
    — só renderiza quando `nome` já resolveu (evita "Bom dia, undefined"
    no primeiro render).

## Edge points

- `nome` pode demorar a chegar (mesma latência que `usuarioId` já tinha) —
  a saudação some até resolver, não mostra placeholder; mesmo padrão que
  `acoesProfessor` já usa pra `usuarioId`.
- Erro de rede ao buscar perfil: reaproveita o `erro`/`tentarNovamente` que
  `useUsuarioIdLogado` já expõe (agora dentro de `usePerfilLogado`) — não
  precisa de um segundo estado de erro só pra saudação; a saudação
  simplesmente não aparece nesse caso (o card de erro do Professor já
  cobre o caso de falha de forma visível; o Aluno, que não tinha esse
  card, também não tinha erro tratado antes — fora do escopo desta Task
  mudar isso, é comportamento pré-existente pro Aluno).
- Meia-noite (`getHours() === 0`) cai em `'manha'` — decisão arbitrária
  (não há critério de aceite sobre madrugada), documentada aqui pra não
  virar dúvida de novo.

## Dependência de outras Tasks

Nenhuma — standalone dentro do Épico #66.
