# Padrão de criação de issues (cards)

Este documento define como toda issue de funcionalidade (`feature`) ou
correção (`fix`) deste projeto deve ser escrita e organizada. Vale para
issues novas e para revisão das existentes.

## Princípio: UX antes de implementação

Uma issue não descreve uma tabela, um endpoint ou um componente — descreve o
que um Professor ou Aluno consegue fazer e por que isso importa para ele. A
implementação nasce a partir dessa descrição, não o contrário. Se um card só
faz sentido do ponto de vista técnico ("criar índice X", "extrair serviço
Y"), ele é um débito técnico, não uma feature, e deve deixar isso explícito
no título.

## Estrutura do card

Todo card tem estas seções, nesta ordem, sempre com este título exato (em
negrito, como cabeçalho `##` no corpo da issue):

1. **Título direto** — resume a funcionalidade em poucas palavras, verbo +
   resultado (ex: "Professor registra frequência da aula"). Vira o título da
   issue no GitHub, não uma seção do corpo.
2. **História de usuário** — `Como <Professor|Aluno>, quero <ação>, para
   <valor>`. Uma frase. Se não cabe em uma frase, o card provavelmente cobre
   mais de uma funcionalidade e deve ser quebrado.
3. **Regra de Negócio (RN)** — o coração do card. Texto corrido (não lista de
   bullets soltos), escrito enxergando o problema por duas lentes ao mesmo
   tempo: quem entende o negócio (o que é justo, o que o Professor espera, o
   que pode dar confusão para o Aluno) e quem vai codar (que invariante o
   domínio precisa proteger, que caso de borda existe, onde isso mora nas
   camadas do backend). É essa seção que orienta a implementação — o código
   deve satisfazer a RN, não só os critérios de aceite.
4. **Critérios de aceite** — lista testável, no formato
   `Dado/Quando/Então` (Gherkin). Cada critério deve ser verificável sem
   ambiguidade e mapeia 1:1 para um teste (unidade no `Domain` ou teste de
   fumaça na `Api`, conforme o caso — ver
   [`docs/spec/architecture.md`](../spec/architecture.md#tdd--sdd)). É o que
   torna a issue testável "de nascença": o teste que vai validar a
   implementação já está desenhado aqui, antes do código existir.
5. **Critérios técnicos** — direcionado a quem implementa, não ao dono do
   produto: lista objetiva (bullets, ao contrário da RN em texto corrido)
   com (a) testes a serem criados e em qual camada (unidade no `Domain`,
   teste de fumaça na `Api`, teste de componente no frontend — ver
   [`code-style.md`](../spec/code-style.md#testes)), (b) *edge points*
   identificados durante a reflexão que não viraram um critério de aceite
   Gherkin por serem detalhe de implementação e não comportamento visível,
   (c) colunas/tabelas que entram em migration nova, se houver, e (d)
   eventos que devem aparecer nos logs estruturados (ver
   [`architecture.md`](../spec/architecture.md#logs-estruturados-e-track-id))
   para que o fluxo seja rastreável por `TrackId`. Nasce do loop de reflexão
   descrito em [`fluxo-de-feature.md`](../spec/fluxo-de-feature.md#fase-2--loop-de-reflexão-e-perguntas-até-3-rodadas),
   não da Regra de Negócio isoladamente.
6. **Contexto ou protótipo** — links para telas/wireframes (quando existirem
   no Figma ou similar), para o item correspondente em
   [`requisitos-funcionais.md`](requisitos-funcionais.md) e para qualquer
   decisão de arquitetura relevante. Sem isso, um link `Ref:` mínimo para o
   item do backlog é o piso aceitável.

Nada além dessas 6 seções. Se uma frase não muda como alguém lê ou implementa
o card, ela não entra.

## Tipo e prioridade

- **Tipo**: label `feature` ou `fix`. (GitHub Issue Types nativo não está
  disponível em repositórios pessoais, só em organizações — por isso o tipo
  continua sendo label, mas só essa, sem sub-taxonomia de área.)
- **Prioridade**: label `priority:P0`..`priority:P3` (P0 = bloqueia o
  próximo incremento entregável, P3 = desejável, sem prazo). É o que ordena
  os cards dentro de cada coluna do board — ordenação subjetiva ("mais
  importante primeiro", sem um valor explícito) não escala e não é
  auditável.
- As antigas labels `funcional` e `area:*` saem de circulação: `funcional`
  só descrevia "isso ainda não foi implementado", que é redundante com a
  própria coluna do board (issue aberta = não implementado); `area:*` é
  reconstruível a qualquer momento a partir do título/RN e não precisa virar
  metadado permanente.

## Board

Colunas, nesta ordem: **Backlog → Em Desenvolvimento → Em Teste →
Concluído**. Dentro de cada coluna, os cards ficam ordenados por prioridade
(`priority:P0` no topo). Um card só avança de coluna quando o critério da
coluna seguinte é satisfeito (ex: só vai para "Em Teste" quando a
implementação existe e os critérios de aceite viraram testes reais rodando).

## Processo de escrita (loop de 3 iterações)

Isso é um **método de rascunho**, não conteúdo que aparece no card. O card
final mostra só o resultado — texto fluido, sem numerar iterações nem deixar
rastro do processo.

1. **Escreve a RN** (seção 3 acima) a partir da história de usuário.
2. **Confere em código**: pensa em como isso se implementaria (que entidade
   de domínio muda, que invariante o código precisa proteger, que caso de
   borda o Professor/Aluno real vai encontrar) e reflete se a RN escrita no
   passo 1 cobre isso. Ajusta a RN e já rascunha os critérios de aceite que
   nascem dessa reflexão.
3. Repete os passos 1 e 2 mais duas vezes (três rodadas no total),
   refinando: corta o que ficou redundante entre rodadas, adiciona o que a
   rodada anterior deixou implícito, testa se a história de usuário, a RN e
   os critérios de aceite ainda contam a mesma história sem se contradizer.

Três rodadas é o teto pensado para cards complexos (ex: regra de cobrança
flexível). Um CRUD simples pode convergir na 2ª rodada — se a 2ª já não muda
nada em relação à 1ª, não force a 3ª só para cumprir o número.

## Divisão de esforço por modelo/agente

Ao criar issues em lote (ex: destrinchar um documento de requisitos
funcionais inteiro), nem toda etapa do processo acima exige o mesmo nível de
raciocínio — e isso deve guiar qual modelo/agente executa cada parte:

- **Passo 1 (rascunho da RN a partir da história de usuário)** é o candidato
  natural para um modelo mais leve/barato (ex: Haiku) rodando como subagente
  em background, desde que receba como referência este documento e pelo
  menos um card já finalizado como exemplo (few-shot). É trabalho de seguir
  um padrão já estabelecido, não de descobrir o padrão.
- **Passo 2 (confere em código / reflexão) e a rodada final de polimento**
  devem ficar com o modelo principal da sessão. É aqui que aparecem as
  ligações não óbvias entre cards (ex: perceber que "frequência contratada"
  e "frequência registrada" são conceitos diferentes, ou que cadastro de
  Professor precisa checar identidade duplicada por causa do item de papéis
  acumuláveis) — o tipo de raciocínio que um modelo mais leve tende a não
  capturar sozinho.
- **Atribuição de labels e movimentação no board** (criar/aplicar label,
  mover card de coluna, ordenar por prioridade) não passa por raciocínio de
  modelo nenhum — são chamadas diretas de CLI/API (`gh issue edit`, `gh
  project item-edit`). Não há o que "otimizar" nessa parte além de
  executá-la direto, sem delegar a um agente.

Na prática: para lotes grandes, um agente leve rascunha o passo 1 de vários
cards em paralelo; o modelo principal revisa cada rascunho com a lente do
passo 2, ajusta o que precisar, e só então publica no GitHub.

## Exemplo aplicado

Card real (issue #1), reescrito com este padrão:

---

**Título:** Professor se cadastra na plataforma

**História de usuário:** Como um Professor que ainda não usa o Synclass,
quero me cadastrar informando meus dados básicos, para começar a organizar
meus horários, alunos e cobranças em um único lugar.

**Regra de Negócio (RN):** O cadastro de Professor é o ponto de entrada de
um usuário "pleno" no sistema — diferente do Aluno provisório (item 3 do
backlog), que existe sem nunca ter passado por um fluxo de cadastro. Como um
mesmo usuário pode acumular os papéis de Professor e Aluno (item 4), o
cadastro cria ou reaproveita uma identidade de usuário única por e-mail/telefone
e associa a ela o papel de Professor — nunca cria um registro duplicado se a
pessoa já existir como Aluno. Campos obrigatórios: nome e um contato
(e-mail ou telefone) usado depois para autenticação; nenhum dado de
agenda ou cobrança é exigido neste passo, pois eles nascem em fluxos
próprios (cadastro de horários, regra de cobrança).

**Critérios de aceite:**
- Dado que não existe usuário com aquele contato, quando o cadastro é
  concluído com nome e contato válidos, então um usuário com o papel
  Professor é criado.
- Dado um usuário já existente como Aluno, quando ele se cadastra como
  Professor com o mesmo contato, então o papel Professor é adicionado ao
  usuário existente, sem duplicar identidade.
- Dado um contato em formato inválido ou já usado por outro papel
  incompatível (regra a confirmar quando autenticação for desenhada), quando
  o cadastro é tentado, então o sistema rejeita com uma mensagem clara do
  motivo.

**Contexto ou protótipo:** Ref:
[`requisitos-funcionais.md`, item 1](requisitos-funcionais.md#contas-e-convites).
Depende do desenho de identidade de usuário único (ver "Notas de modelagem
para etapas futuras" no mesmo documento).

---

Este exemplo é anterior à adição da seção "Critérios técnicos" (item 5) e por
isso não a inclui. Um card novo, seguindo o padrão atual, teria entre
"Critérios de aceite" e "Contexto ou protótipo" uma lista como: testes de
unidade no `Domain` para os três cenários acima, edge point "contato com
espaços/maiúsculas inconsistentes deve normalizar antes de comparar
duplicidade", nenhuma coluna nova de migration além das já previstas para
`Usuario`/`PapelUsuario`, e log estruturado do evento `UsuarioCadastrado` com
o papel atribuído.

---

Repare que a RN cita explicitamente outro item do backlog (papéis
acumuláveis) porque a 2ª rodada do loop ("confere em código") expôs que
implementar cadastro sem pensar nisso levaria a usuários duplicados — é
exatamente esse tipo de ligação que o loop existe para capturar.
