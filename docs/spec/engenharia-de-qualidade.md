# Engenharia de qualidade durante o desenvolvimento

Este documento complementa [`code-style.md`](code-style.md) — aquele
define *forma* (tamanho, nomes, tipos). Este define *método*: como
pensar enquanto se escreve código, não só o que checar depois de
escrito. As três práticas abaixo (métricas de qualidade, reflexão de
causa raiz, grafo de contexto) se aplicam **durante** a implementação,
não como um checklist de revisão final — a autorrevisão de
`code-style.md` continua existindo, mas chegar nela já tendo pensado
nisso produz menos achado, não mais trabalho de correção depois.

## Métricas e valores de qualidade — pensar nelas enquanto se escreve

Não são só limites de `code-style.md` (4-20 linhas, 500 linhas/arquivo)
— são sinais pra decidir *como* desenhar uma função antes dela existir,
não só medir depois:

- **Complexidade ciclomática**: conte os pontos de decisão de uma
  função (`if`, `for`, `while`, `case`, `&&`, `||`, `?:`) enquanto
  escreve. Mais de ~5-7 é sinal de que a função está decidindo coisas
  demais — quebre em funções menores com um motivo de mudar cada uma
  (Single Responsibility), não porque bateu no limite de linhas.
- **Acoplamento (fan-in/fan-out)**: antes de importar algo novo,
  pergunte se essa dependência é necessária aqui ou se o dado/serviço já
  deveria chegar via prop/parâmetro (baixo acoplamento = fácil de
  testar isolado, fácil de trocar depois). Uma função que depende de
  muitos módulos diferentes pra fazer uma coisa simples é sinal de
  acoplamento alto.
- **Coesão**: uma função/componente faz uma coisa; um módulo agrupa
  coisas que mudam juntas pelo mesmo motivo. Se ao editar uma função por
  causa da Task, você precisa entender uma segunda responsabilidade não
  relacionada só pra não quebrá-la, a coesão já está baixa — é sinal de
  quebrar, mesmo que não seja o objetivo direto da Task (registre em
  `## Inconsistências encontradas` se o escopo da Task não permitir
  refatorar ali mesmo).
- **Duplicação**: `code-style.md` já proíbe — aqui o ponto é *quando*
  perceber: ao escrever a segunda ocorrência de um bloco (não a
  terceira), já é hora de extrair. Esperar "ficar óbvio" custa mais
  retrabalho do que extrair cedo.
- **Legibilidade sem comentário**: um nome específico + uma função
  pequena substitui um comentário explicando o que o código faz. Se
  sentir vontade de escrever `// filtra os ativos` acima de uma linha,
  o nome da variável/função provavelmente devia ser `ativos`/
  `filtrarAtivos` em vez do comentário existir.

Nenhuma dessas métricas é um número a reportar — são perguntas a se
fazer no momento de desenhar a função, antes do primeiro `write_file`.

## Reflexão de causa raiz (root cause), não só o sintoma

Quando um teste falha de um jeito inesperado, ou uma tentativa óbvia de
correção não resolve de primeira, **pare antes da segunda tentativa** e
pergunte por quê — não repita "tentar de novo com uma variação" até
grudar:

- O erro é realmente no código que você acabou de escrever, ou é um
  contrato que já estava errado antes (tipo incompatível vindo de outra
  camada, estado compartilhado inesperado, ordem de execução assumida
  errado)? Corrigir o sintoma local sem entender isso deixa o problema
  real pra aparecer de novo em outro lugar.
- Esse mesmo problema pode estar em outro lugar do código que você não
  tocou ainda? (ex: mesma race condition em outro componente que usa o
  mesmo hook, mesma suposição errada replicada em outro form). Não
  precisa corrigir tudo na mesma Task — mas vale registrar como achado
  em `## Inconsistências encontradas` se for algo real e fora do escopo
  atual, em vez de silenciosamente deixar passar.
- Documente a causa raiz (não o sintoma) na mensagem de commit quando
  ela não for óbvia pelo diff — "fix: corrige ordem de inicialização
  do hook X" é mais útil que "fix: corrige teste" pra quem revisa depois
  ou faz `git blame` no futuro.

Isso existe porque tentativa-e-erro sem entender a causa consome
iterações do harness sem produzir progresso real — e o teto de
iterações (ADR-0002) existe justamente pra essas Tasks sem virarem loop
infinito. Uma pausa de 30 segundos pra entender por que algo falhou é
mais barata que 5 iterações de tentativas às cegas.

## Grafo de contexto — entender o impacto antes de editar

Antes de editar uma função, componente ou tipo que pode ser usado em
mais de um lugar (exportado de um módulo, prop pública, tipo
compartilhado), mapeie o impacto **antes** de mudar, não descubra um
call site quebrado por vez enquanto o build reclama:

- **Quem usa isso hoje?** `grep -rn "NomeDaFuncao\|NomeDoTipo"` (ou
  `list_dir`/`read_file` nos diretórios óbvios) antes de mudar a
  assinatura/contrato. Se a mudança altera o tipo de retorno ou os
  parâmetros, liste TODOS os call sites afetados de uma vez — corrigir
  todos na mesma passada é mais barato que corrigir um, rodar, achar
  outro quebrado, corrigir, repetir.
- **Do que isso depende?** Antes de assumir que uma função pode ser
  movida/renomeada/tipo alterado, confira se ela por sua vez chama algo
  que pressupõe a assinatura atual (ex: um DTO serializado que o
  frontend também desserializa — mudar o C# sem checar o TypeScript
  correspondente, ou vice-versa).
- **Isso é código gerado ou compartilhado entre camadas?** Migration já
  aplicada, contrato de API entre backend/frontend, tipo usado tanto no
  `Domain` quanto exposto na `Api` — esses têm um raio de impacto maior
  que uma função privada de um único arquivo; trate com mais cautela
  antes de editar (ver também `docs/spec/security-rules.md` e o aviso
  sobre migrations em `AGENTS.md`).

Isso não é burocracia extra — é a diferença entre uma edição
determinística (sei exatamente o que vai quebrar antes de mudar) e uma
edição por tentativa (mudo, rodo teste, descubro o que quebrou, repito).
A segunda consome iterações do harness sem necessidade; a primeira não.
