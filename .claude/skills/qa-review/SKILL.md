---
name: qa-review
description: Simulação de QA/UX com Playwright para um Pull Request do projeto Synclass, executando cada critério de aceite Gherkin (Dado/Quando/Então) da issue que o PR fecha como um teste real no navegador, mais checks de UX que não estão nos critérios (acessibilidade, responsividade mobile/desktop, estados de loading/erro) — dispara quando o usuário pede para "testar o PR", "fazer QA do PR #N", "validar os critérios de aceite do PR", "rodar os cenários Gherkin da issue no navegador", ou "qa-review". Se o PR não referenciar uma issue com critérios de aceite testáveis (violando `docs/backlog/padrao-de-issue.md`), a skill para e reporta isso como o achado principal em vez de adivinhar o que testar. Sempre pede confirmação do usuário antes de postar qualquer comentário no GitHub via `gh`.
---

# qa-review

Você vai validar um Pull Request do projeto Synclass rodando, num navegador
real via Playwright, um teste para cada critério de aceite Gherkin da issue
que o PR fecha — e complementar isso com checks de UX que os critérios de
aceite normalmente não cobrem.

## Passo 1 — Resolver a issue-oráculo

1. `gh pr view <n> --json body,title,number` para ler o corpo do PR.
2. Procure no corpo uma referência `Closes #N` (ou `Fixes #N`/`Resolves #N`)
   apontando para a issue que o PR fecha, conforme exigido pelo
   `CONTRIBUTING.md`.
3. Se encontrar, rode `gh issue view N` e extraia a seção **Critérios de
   aceite** (cabeçalho exato definido em
   `docs/backlog/padrao-de-issue.md`), que deve estar em linhas no formato
   `Dado ... quando ... então ...` (Gherkin, em texto corrido — confirme o
   formato lendo o exemplo aplicado no próprio documento antes de assumir).

**Se não houver issue linkada, ou a issue não tiver uma seção de Critérios de
aceite em formato Gherkin testável**: pare aqui. Não tente adivinhar cenários
a partir do diff ou da descrição do PR. O achado da skill, nesse caso, é
exatamente esse: o PR (ou a issue) está violando o padrão de issue do
projeto (que exige que cada critério mapeie 1:1 para um teste). Vá direto
para o Passo 8 já com esse único achado e ofereça postar o comentário na
issue (não no PR) sinalizando a lacuna.

## Passo 2 — Parsear os cenários

Transforme cada bloco `Dado/Quando/Então` da issue em um cenário discreto e
numerado (Cenário 1, Cenário 2, ...), preservando o texto original de cada
critério — ele vai para a coluna "Critério Gherkin" do relatório final sem
paráfrase, para que o rastreio 1:1 exigido pelo padrão de issue fique
verificável por qualquer pessoa que compare o relatório com a issue.

## Passo 3 — Subir a stack de forma reprodutível

```bash
# a partir da raiz do repo
docker compose up -d db api
```

Aguarde ativamente o backend responder antes de prosseguir — nunca use
`sleep` fixo:

```bash
until curl -sf http://localhost:${API_PORT:-8080}/health >/dev/null; do sleep 1; done
```

Depois, suba o frontend web a partir de `frontend/`:

```bash
cd frontend && npm run web
```

(`npm run web` executa `expo start --web`; confirme no `package.json` do
frontend que o script ainda se chama `web` antes de rodar, pois pode mudar.)
Rode em background e aguarde ativamente a porta responder (por padrão o Expo
web sobe em `http://localhost:8081` — confirme a porta real no log de start,
ela pode variar se 8081 estiver ocupada) antes de iniciar qualquer teste
Playwright:

```bash
until curl -sf http://localhost:8081 >/dev/null; do sleep 1; done
```

**Limitação conhecida a documentar no relatório, não a resolver agora**: o
projeto ainda não tem seed nem reset automático de dados de teste no banco.
Se algum cenário depender de estado limpo (ex: "dado que não existe usuário
com aquele contato"), avise no relatório que o resultado pode ser
inconsistente entre execuções e sugira rodar
`docker compose down -v && docker compose up -d db api` antes de repetir os
testes, para reduzir flakiness. Não implemente um mecanismo de seed/reset
como parte desta skill.

## Passo 4 — Traduzir cada cenário em um teste Playwright

Escreva os testes em um arquivo avulso e descartável, não commitado:

```
frontend/e2e/.qa-review/<slug-do-pr>.spec.ts
```

(ex: `frontend/e2e/.qa-review/pr-42-cadastro-professor.spec.ts`). Antes de
escrever, confirme que `@playwright/test` está instalado
(`frontend/package.json` → `devDependencies`) e que o browser Chromium do
Playwright existe em `~/.cache/ms-playwright/`; se algum dos dois faltar,
rode `npm install -D @playwright/test && npx playwright install chromium` a
partir de `frontend/` antes de continuar. Não use `--with-deps` — esse flag
tenta instalar pacotes de sistema via `sudo` e falha sem senha interativa
neste ambiente; o Chromium sozinho (sem `--with-deps`) já roda headless sem
depender disso.

Use locators por role (`getByRole`, `getByLabel`, `getByText`), alinhado ao
que `@testing-library/react-native` já usa no restante do projeto — evite
seletores CSS/XPath frágeis. Cada cenário Gherkin vira um `test(...)`
próprio, nomeado com o texto do critério, para que o relatório final possa
citar diretamente qual teste corresponde a qual critério.

Rode com:

```bash
cd frontend && npx playwright test e2e/.qa-review/<slug-do-pr>.spec.ts
```

Ao final da skill (Passo 7), apague o arquivo de spec gerado
(`rm frontend/e2e/.qa-review/<slug-do-pr>.spec.ts`) ou, se preferir manter o
histórico de execução, garanta que `frontend/e2e/.qa-review/` esteja listado
no `.gitignore` do frontend antes de deixar arquivos lá parados — hoje essa
pasta não existe e não está no `.gitignore`, então confira antes de assumir
que já está coberta.

## Passo 5 — Para cada cenário, capturar evidência

Em cada teste, independentemente de passar ou falhar:

- Screenshot (`page.screenshot({ path: ... })`) salvo em
  `frontend/e2e/.qa-review/screenshots/<slug-do-pr>/<cenario>.png`.
- Console errors: `page.on('console', msg => { if (msg.type() === 'error') ... })`.
- Erros de página não tratados: `page.on('pageerror', ...)`.
- Requests que falharam: `page.on('requestfailed', ...)`.

Anexe esses dados ao relatório do cenário correspondente, não só ao final do
arquivo.

## Passo 6 — Checks de UX além do funcional puro

Isso é o que justifica esta skill existir além de "rodar os critérios da
issue":

1. **Accessibility snapshot**: rode `page.accessibility.snapshot()` nos
   fluxos principais testados e sinalize elementos sem role/label
   acessível.
2. **Responsividade**: repita os cenários-chave (não necessariamente todos)
   em pelo menos 2 viewports — mobile (ex: 390x844) e desktop (ex:
   1280x800) — já que o mesmo código serve web e mobile via Expo Router.
3. **Loading/erro sob rede ruim**: escolha uma requisição-chave do fluxo
   testado e intercepte com `page.route()` para simular lentidão (delay
   artificial) ou erro (abortar/retornar 500). Confirme que a UI mostra um
   estado de loading ou de erro visível, em vez de tela branca ou travar.

## Passo 7 — Sempre derrubar a stack no final

Trate subida e execução dos testes como um bloco try/finally: mesmo se um
teste falhar ou o comando Playwright retornar erro, rode ao final:

```bash
docker compose down
```

Se você iniciou `npm run web` em background, encerre esse processo também
antes de finalizar. Não deixe a stack (nem o Expo dev server) rodando depois
que a skill termina, mesmo em caso de exceção durante os testes.

## Passo 8 — Compor o relatório

Monte uma tabela markdown com uma linha por critério Gherkin, na ordem em
que aparecem na issue:

| Critério Gherkin | Status | Evidência |
|---|---|---|
| Dado ..., quando ..., então ... | Passou / Falhou / Não implementado | `frontend/e2e/.qa-review/screenshots/.../cenario-1.png` |

Adicione, depois da tabela, uma seção separada para os achados de UX do
Passo 6 (acessibilidade, responsividade, loading/erro) — eles não mapeiam
1:1 para um critério da issue, então não force uma linha da tabela para
cada um.

## Passo 9 — Antes de postar, pedir confirmação

Nunca rode `gh pr comment` ou `gh issue comment` direto. Mostre ao usuário o
texto final exato do relatório que seria postado e peça confirmação
explícita:

- Se o achado é sobre os critérios de um PR válido: proponha postar em
  `gh pr comment <n>`.
- Se o achado é a ausência de issue linkada ou de critérios testáveis
  (Passo 1): proponha postar em `gh issue comment <N>` (ou como comentário
  no PR pedindo a correção da issue, se não houver issue alguma para
  comentar).

Só rode o comando `gh` depois da confirmação — a menos que o usuário já
tenha pedido postagem automática nesta mesma invocação da skill, caso em
que você ainda deve mostrar o texto antes de rodar o comando `gh`.
