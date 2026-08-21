# Padrões de teste

Este documento substitui e expande a antiga seção "Testes" de
[`code-style.md`](code-style.md#testes) — se procurando aqui a partir de
lá, este é o destino atual.

## Princípios (F.I.R.S.T)

Testes devem ser rápidos (*fast*), independentes (*independent*),
repetíveis (*repeatable*), que se autovalidam (*self-validating*) e
oportunos (*timely* — escritos junto com o código, não depois).

## TDD estrito, na ordem do `task.md`

Para toda Task com `docs/specs/<n>-<slug>/task.md` (ver
[`especificacao-tecnica.md`](especificacao-tecnica.md)): para cada item da
checklist, escreva o teste primeiro, veja-o falhar, implemente o mínimo
para passar, então refatore. Não escreva código de produção sem um teste
vermelho guiando. Marque o item como concluído a cada commit.

## Testes escopados durante o loop, suíte completa antes do PR

Rodar a suíte inteira a cada ciclo de TDD desperdiça tempo (e, quando a
implementação é feita via `scripts/deepseek-agent.mjs`, desperdiça
chamadas de API). A regra:

- **Durante a implementação** (loop de TDD, incluindo o harness da
  DeepSeek): rode só o teste do arquivo/classe diretamente tocado ou que
  toque a parte modificada —
  `dotnet test --filter <Classe ou Namespace>` (backend),
  `npx jest <caminho ou nome do arquivo>` (frontend).
- **Antes de abrir o PR**: o gate completo de
  [`CONTRIBUTING.md#antes-de-abrir-um-pr`](../../CONTRIBUTING.md#antes-de-abrir-um-pr)
  roda por inteiro, sem exceção — `dotnet format && dotnet test` /
  `npm run lint && npm run typecheck && npm test`. Esse é o gate de CI, não
  o loop de TDD, e não é dispensado por nenhum nível de rigor (ver
  [`fluxo-de-feature.md#níveis-de-rigor`](fluxo-de-feature.md#níveis-de-rigor)).

## Cobertura por camada

- **Toda função nova ganha um teste.** Correção de bug ganha teste de
  regressão (nomeie referenciando a issue/bug, ex:
  `RejeitaContatoComEspacos_Issue42`).
- **Backend**: teste de unidade no `Domain` para toda regra de negócio
  (entidades, services) — sem I/O real. Teste de fumaça na `Api`
  (`WebApplicationFactory`, `tests/Synclass.Api.Tests`) para o contrato
  HTTP de cada endpoint novo (happy path + rejeições documentadas nos
  critérios de aceite).
- **Frontend**: teste de componente (`@testing-library/react-native`)
  para todo componente com lógica/estado — comportamento observável
  (texto renderizado, callback chamado), não detalhe de implementação
  (classe CSS, estrutura interna do DOM).
- **Sem cobertura mínima configurada** hoje (nem `coverageThreshold` no
  Jest, nem `coverlet` no backend) — débito conhecido, registrado em
  [`security-rules.md`](security-rules.md#débitos-conhecidos). Até existir
  um número-alvo decidido, a régua é "toda função nova tem teste", não uma
  porcentagem.

## Mocks e fakes

Mocke I/O externo (API, banco de dados, sistema de arquivos) com classes
fake nomeadas, não com stubs inline anônimos — mesmo padrão de
`IClock`/`INotificador` já usado no projeto (ver
[`code-style.md#dependências`](code-style.md#dependências)).

## E2E/Playwright

`qa-review` (`.claude/skills/qa-review/SKILL.md`) cobre isso hoje, mas
**não roda por padrão** no pipeline automático de `/feature-flow` (ver
[`fluxo-de-feature.md`](fluxo-de-feature.md#fase-4--revisão)) — só
`dev-review`. `qa-review` continua disponível sob pedido direto do
usuário ("testar o PR", "fazer QA do PR #N").
