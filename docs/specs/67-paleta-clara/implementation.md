# Implementação: Clarear a paleta de fundo (#67)

## Entidades/arquivos afetados

- `frontend/src/theme/palette.js` — único token de valor mudando:
  `Colors.light.background`.
- `frontend/src/theme/contraste.ts` — **novo** util puro (sem lib externa)
  com `luminanciaRelativa(hex: string): number` e
  `razaoDeContraste(hexA: string, hexB: string): number`, fórmula WCAG 2.x
  (`(L1 + 0.05) / (L2 + 0.05)`, com L1 >= L2). Usado só pelo teste — não
  precisa ser exportado para uso em runtime de produto, mas fica em
  `theme/` por ser um utilitário de token, não de teste.
- `frontend/src/theme/palette.test.ts` — **novo** arquivo de teste, cobre
  os dois critérios de aceite do card (ver `task.md`).
- `docs/spec/design-system.md#cor` — tabela de cor, só a linha
  `background`/Light.
- `frontend/src/theme/tokens.ts` — não muda (já reexporta `palette.js`
  dinamicamente, não tem valor hardcoded).
- `tailwind.config.js` — não muda (já consome `palette.js` via
  `require()`, propaga sozinho).

Nenhuma camada de backend, migration, ou API é afetada — é um token de
apresentação puro.

## Valor escolhido e racional (não re-derive, já verificado nesta spec)

Novo valor: **`#F9FAFB`** (era `#F5F6F8`). Verificado com a fórmula WCAG
(luminância relativa + razão de contraste):

| Comparação | Razão atual (`#F5F6F8`) | Razão nova (`#F9FAFB`) |
|---|---|---|
| `background` vs `text` (`#14161A`) | 16.75:1 | 17.33:1 |
| `background` vs `text-secondary` (`#5B616B`) | 5.77:1 | 5.97:1 |
| `background` vs `background-element` (`#E7E9ED`) | 1.124:1 | 1.163:1 |
| `background` vs `background-selected` (`#D8DBE1`) | 1.283:1 | 1.327:1 |

Conclusões que o `task.md` assume como já resolvidas (não re-abrir):

- AA (4.5:1) para texto normal continua atendido com folga em ambos os
  tokens de texto — `text` e `text-secondary` sobre o novo `background`.
- A razão de contraste `background` → `background-element` →
  `background-selected` **aumenta** (não diminui) ao clarear o fundo,
  porque os dois tokens de superfície ficam parados e o fundo se afasta
  deles — a escada de elevação fica mais visível, não menos. É o oposto
  do risco que a RN do card aponta como possível ("clarear pode zerar a
  escada"); com este valor específico, não zera.
- `#F9FAFB` mantém distância de `#FFFFFF` (delta ainda perceptível,
  `rgb(249,250,251)` vs `rgb(255,255,255)`) — não é um extremo puro, só
  mais claro que o atual.
- Nenhum outro token muda de valor (ver critério técnico do card) —
  `backgroundElement`, `backgroundSelected`, `border`, `text`,
  `textSecondary`, `primary`, `error`, e o modo `dark` inteiro permanecem
  idênticos.

O teste (`palette.test.ts`) trava essas duas primeiras conclusões como
regressão automatizada — não depende de validação manual futura para
não quebrar de novo.

## Contrato/API

Não aplicável — não há contrato de API nesta Task.

## Modelo de dados

Não aplicável — não há mudança de schema/migration.

## Edge points

- O teste de contraste deve comparar contra o par mínimo do modo claro
  (`text`/`text-secondary`), não o modo escuro — modo escuro não muda
  nesta Task (RN do card é explícita sobre isso).
- Se o teste `razaoDeContraste` for reutilizado depois para validar outros
  tokens (ex: uma Task futura de paleta escura), trate isso como
  refatoração de uma Task futura — não expanda escopo aqui.

## Dependência de outras Tasks

Nenhuma. É a primeira Task da cadeia do Épico #66 e não depende de #69,
#77, #78, #79, #81, #68, #80 (que vêm depois na sequência combinada).
