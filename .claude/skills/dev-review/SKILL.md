---
name: dev-review
description: Revisão de qualidade de código de um Pull Request do projeto Synclass contra as regras específicas de `docs/spec/code-style.md` (tamanho de função/arquivo, nomes específicos, tipos explícitos, injeção de dependência via construtor, wrapper de biblioteca de terceiros, early return, mensagens de exceção, cobertura de teste por função nova) — dispara quando o usuário pede para "revisar o PR", "fazer code review do PR #N", "revisar essa branch antes de abrir PR", "checar se o PR segue o guia de estilo do Synclass", ou "dev-review". Complementa (não substitui) o `/code-review` genérico, que já cobre correção, simplificação, reuso e eficiência — esta skill foca no que é específico do Synclass e sempre termina pedindo confirmação do usuário antes de postar qualquer comentário no GitHub via `gh`.
---

# dev-review

Você vai revisar um Pull Request do projeto Synclass contra as regras de
`docs/spec/code-style.md`, combinando checks mecânicos, um checklist
automatizável e julgamento pontual por leitura de diff — delegando para a
skill `/code-review` tudo que já é responsabilidade dela (correção,
simplificação, reuso, eficiência).

## Passo 1 — Resolver o alvo

Aceite um argumento do usuário (número de PR, nome de branch, ou nenhum
argumento). Resolva nesta ordem, igual o `/code-review` faz:

1. Se um número de PR foi passado (`123` ou `#123`): `gh pr view <n>` e
   `gh pr diff <n>` para obter metadados e diff.
2. Se um nome de branch foi passado: `git diff main...<branch>` (ajuste a
   branch base se o repositório usar outra).
3. Se nada foi passado: use o diff local atual (`git diff` ou, se a branch
   corrente já tiver commits à frente de `main`, `git diff main...HEAD`).

Guarde a lista de arquivos alterados (`git diff --name-only` ou
`gh pr diff <n> --name-only`) — ela é a base de todos os passos seguintes.
Ignore arquivos gerados (`bin/`, `obj/`, `node_modules/`, `.expo/`, lockfiles)
na análise de estilo, mesmo que apareçam no diff.

## Passo 2 — Checks mecânicos primeiro (bloqueantes)

Rode os mesmos comandos da seção "Antes de abrir um PR" do
`CONTRIBUTING.md`. Eles são determinísticos e baratos — se algum falhar, isso
já é um achado bloqueante que entra no topo do relatório, mas continue os
passos seguintes mesmo assim (o relatório final deve mostrar o quadro
completo).

```bash
# Backend
cd backend && dotnet format --verify-no-changes > /tmp/dotnet-format.log 2>&1
echo "format exit: $?"
dotnet test > /tmp/dotnet-test.log 2>&1
echo "test exit: $?"

# Frontend
cd frontend && npm run lint > /tmp/npm-lint.log 2>&1
echo "lint exit: $?"
npm run typecheck > /tmp/npm-typecheck.log 2>&1
echo "typecheck exit: $?"
npm test > /tmp/npm-test.log 2>&1
echo "test exit: $?"
```

Observações:
- Não traga esses logs inteiros para o contexto. Se o exit code for 0,
  descarte o arquivo — o relatório só precisa dizer "passou". Se falhar,
  rode `grep -inE "error|fail" /tmp/<arquivo>.log` primeiro e só abra o log
  completo se o grep não bastar para entender a causa.
- `dotnet format --verify-no-changes` falha (exit não-zero) se houver
  arquivos que o formatador mudaria — é isso que você quer detectar aqui,
  sem aplicar a mudança automaticamente.
- Se o PR só toca backend ou só frontend, ainda assim rode os dois blocos —
  um PR de frontend pode ter quebrado um teste de backend por engano (raro,
  mas barato de checar) e vice-versa. Se um lado não tiver nenhum arquivo
  alterado, pode marcar esse bloco como "N/A" no relatório em vez de rodar.

## Passo 3 — Checklist automatizável do code-style.md

Estes itens não exigem julgamento do modelo — são checáveis por grep/contagem
de linhas sobre os arquivos alterados do diff (não o repo inteiro, exceto
onde indicado):

1. **Tamanho de função (4–20 linhas) e de arquivo (até 500 linhas).**
   Para cada arquivo alterado, conte linhas totais (`wc -l`) e, para C#/TS,
   identifique os limites de cada função tocada no diff (via leitura direta —
   não há uma ferramenta de AST aqui, então use a indentação e chaves como
   guia). Sinalize qualquer função nova/alterada fora da faixa 4–20 linhas e
   qualquer arquivo acima de 500 linhas.
2. **Nomes específicos.** Para cada identificador novo (função, classe,
   variável de escopo não-trivial) introduzido no diff, rode
   `grep -rn "<identificador>" --include="*.cs" --include="*.ts" --include="*.tsx"`
   a partir da raiz do projeto. Se retornar 5 ou mais ocorrências e o nome for
   genérico (candidatos óbvios: `data`, `handler`, `Manager`, `Helper`,
   `info`, `item`), sinalize como nome genérico — cite o grep usado.
3. **Nenhum `any`/`dynamic`/`object` genérico introduzido.** Rode
   `grep -n '\bany\b'` nos arquivos `.ts`/`.tsx` alterados e
   `grep -n '\bdynamic\b\|\bobject\b'` nos arquivos `.cs` alterados,
   filtrando apenas linhas adicionadas no diff (prefixo `+`). Tipos já
   existentes antes do PR não contam como achado desta skill (isso seria
   débito técnico pré-existente, não uma regressão deste PR) — confirme
   olhando o diff, não o arquivo final isolado.
4. **Toda função nova tem teste correspondente no mesmo PR.** Para cada
   função nova ou com lógica alterada no diff, confirme que o mesmo PR também
   adiciona/altera um arquivo de teste:
   - Backend: arquivo em `backend/tests/Synclass.Api.Tests/` (ou outro
     projeto de teste que venha a existir em `backend/tests/`).
   - Frontend: o padrão observado no projeto é teste co-localizado
     `NomeDoArquivo.test.tsx` ao lado do componente/módulo (ex:
     `src/components/atoms/Button.test.tsx`), não uma pasta `__tests__`
     separada — confirme o padrão atual com
     `find frontend/src -name "*.test.ts*"` antes de cobrar, pois isso pode
     mudar. Se não encontrar teste para uma função nova relevante, sinalize.

## Passo 3.5 — Verificação visual obrigatória (PRs que tocam frontend)

Todo PR cuja lista de arquivos (Passo 1) inclui algo em `frontend/src/app/**`
ou `frontend/src/components/**` passa por este passo — não é opcional, não
depende de pedido do usuário (diferente de `qa-review`/Playwright, que
continua sob pedido explícito para o resto). Motivo: os passos 1–3 e 5 desta
skill leem código e diff — nenhum deles renderiza a tela. Um menu que
sobrepõe o logo, um botão do tamanho errado, ou um rótulo sem explicação
passam batido por análise textual mesmo com código "limpo" (foi exatamente
assim que PRs #77/#107/#111 chegaram a `main` com o menu lateral quebrado e
um CTA de "Meu perfil" do tamanho de ação primária — nenhum achado de
`dev-review` cobria isso).

1. Suba o app (`npx expo start --web`) se ainda não estiver rodando.
2. Para cada rota nova/alterada no diff, tire screenshot via Playwright em
   **dois viewports**: mobile (390×844) e desktop (1440×900) — os dois, não
   um só, já que responsividade quebrada num dos dois é o tipo de achado
   mais comum. Injete uma sessão fake via `localStorage` (`synclass.sessao.token`/
   `synclass.sessao.papeis`) quando a rota exigir autenticação, igual ao
   padrão já usado nesta sessão.
3. Compare cada screenshot contra `docs/spec/ux-heuristics.md`: hierarquia
   de CTA, alvo de toque, sobreposição/corte de elementos, rótulo sem
   explicação de jargão, espaço vazio sem propósito, ícone/controle que não
   segue convenção de plataforma (ex: hamburger deve parecer hamburger, não
   texto "Abrir menu").
4. Qualquer defeito visual concreto encontrado aqui é **bloqueante**, mesmo
   que os passos 2–5 não tenham achado nada — entra na tabela do Passo 6 na
   mesma prioridade dos checks mecânicos falhados. Anexe o screenshot (ou o
   caminho do arquivo) como evidência na linha da tabela.
5. Se o PR não tiver nenhuma rota nova/alterada (só componente interno sem
   tela própria, ex: um hook), ainda assim renderize a tela mãe mais próxima
   que o consome — não pule este passo alegando "não é uma tela".

## Passo 4 — Delegar correção/simplificação/reuso/eficiência para /code-review

Não reimplemente essa análise. Invoke a skill `code-review` (nível `medium`)
sobre o mesmo alvo resolvido no Passo 1, via Skill tool. Incorpore os
achados retornados por ela como uma seção própria do relatório final —
não os reescreva, apenas resuma/organize junto com o resto.

## Passo 5 — Julgamento por LLM no que sobra

Leia o diff (não o repo inteiro) e avalie manualmente, à luz de
`docs/spec/code-style.md`, apenas o que os passos anteriores não cobrem
mecanicamente:

- **Early return / máximo 2 níveis de indentação**: procure `if` aninhado em
  `if` em funções alteradas; sugira inversão de condição + retorno
  antecipado quando aplicável.
- **Injeção de dependência via construtor**: no backend, procure uso de
  singletons estáticos, `new` direto de uma dependência concreta dentro de
  um método (em vez de receber via construtor), ou imports diretos de
  instância global. No frontend, o equivalente é importar um módulo
  singleton com estado/efeito colateral em vez de recebê-lo como prop/hook
  injetável.
- **Wrapper fino de biblioteca de terceiros**: se o diff introduz uso direto
  de um SDK/biblioteca externa para I/O (envio de mensagem, hora atual,
  chamada HTTP a serviço externo), confirme se existe uma interface própria
  do projeto por trás (padrão citado no guia: `IWhatsAppNotifier`, `IClock`).
  Se o código chama a biblioteca diretamente sem esse wrapper, sinalize.
- **Mensagens de exceção**: toda exceção lançada no diff deve incluir o
  valor problemático e o formato esperado (ex:
  `throw new ArgumentException($"Duração inválida: {duracao}. Esperado um valor entre 15 e 240 minutos.")`).
  Sinalize exceções genéricas sem esse contexto.
- **Duplicação de código não capturada pelo /code-review**: releia o diff
  procurando blocos quase-idênticos que o passo anterior pode ter deixado
  passar por serem pequenos demais para o critério de "reuso" dele, mas que
  já violam a regra explícita "sem duplicação" do guia.

## Passo 6 — Compor o relatório

Monte uma única tabela markdown, achados mais graves primeiro (bloqueante >
checklist mecânico violado > julgamento por LLM > nota informativa):

| Regra | Status | Arquivo:linha | Observação |
|---|---|---|---|
| ... | Falhou / Passou / Atenção | `caminho/arquivo.ts:42` | ... |

Inclua no topo um resumo de 2-3 linhas com o veredito geral (aprovar,
aprovar com ressalvas, pedir mudanças) e, logo abaixo da tabela, a seção
delegada do `/code-review` (Passo 4).

**Se você está rodando como subagente** (delegado por outra instância do
Claude, não diretamente pelo usuário): a tabela é o entregável — não repita
o mesmo achado de novo em prosa solta nem no texto do comentário proposto
do Passo 7 além do necessário para o `gh pr comment` funcionar como texto
autônomo. Itens "Passou" não precisam de explicação além da célula
"Observação"; gaste texto extra só nos achados que pedem ação.

## Passo 7 — Antes de postar, pedir confirmação

Nunca rode `gh pr comment`, `gh pr review --comment` (ou similar) direto.
Mostre ao usuário o texto final exato do comentário que seria postado e
peça confirmação explícita. Só rode o comando `gh` depois que o usuário
confirmar — a menos que o usuário já tenha pedido postagem automática nesta
mesma invocação da skill (ex: "revisa o PR 12 e já posta o comentário"), caso
em que você pode postar sem esperar uma segunda confirmação, mas ainda assim
deve mostrar o texto antes de rodar o comando `gh`.

**Sempre termine o comentário postado com um marcador HTML** (invisível na
renderização do GitHub, lido por `scripts/pipeline-orchestrator.sh` —
ADR-0003, `docs/spec/decisions/ADR-0003-orquestrador-mecanico-sem-claude.md`
— pra decidir se um PR pode ser mergeado sem gastar uma sessão Claude nova):

```
<!-- dev-review:status=aprovado -->
```

se o veredito do Passo 6 foi "aprovar" (nenhum achado bloqueante, gate de
CI passou), ou

```
<!-- dev-review:status=mudancas-solicitadas -->
```

se foi "aprovar com ressalvas" ou "pedir mudanças" — qualquer coisa que não
seja aprovação direta conta como `mudancas-solicitadas`, o orquestrador
mecânico só mergeia no primeiro caso. Use sempre um dos dois valores
exatos; o script lê o comentário mais recente do PR que contenha
`dev-review:status=` — não invente um terceiro valor nem omita o marcador.
