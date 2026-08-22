# Implementação: unificar filtro de período com ChipSelector (#140)

## Estado atual

`frontend/src/app/professor/[professorId]/valor-devido.tsx` define
`FiltroDePeriodo` + `ChipDeModo` (borda quadrada preta/branca sólida) —
reimplementação própria do mesmo padrão de `ChipSelector`
(`frontend/src/components/molecules/ChipSelector.tsx`, já usado em
`HorarioForm`/`HorarioCard`, chip arredondado com `bg-primary` quando
selecionado).

## Mudança

Delete `FiltroDePeriodo` e `ChipDeModo` de `valor-devido.tsx`. No lugar da
chamada `<FiltroDePeriodo modo={estado.modo} onMudarModo={estado.setModo} />`,
use `ChipSelector` diretamente:

```tsx
import { ChipSelector, type ChipSelectorOption } from '@/components/molecules/ChipSelector';

const OpcoesDeModo: readonly ChipSelectorOption<Modo>[] = [
  { valor: 'todos', rotulo: 'Todos' },
  { valor: 'mes', rotulo: 'Este mês' },
  { valor: 'personalizado', rotulo: 'Personalizado' },
];

// dentro de ValorDevidoScreen, no lugar de <FiltroDePeriodo ... />:
<ChipSelector
  label="Período"
  opcoes={OpcoesDeModo}
  valor={estado.modo}
  onChange={estado.setModo}
/>
```

`ChipSelector` já exige um `label` (prop obrigatória) — a tela não tinha
um rótulo visível pro filtro antes (`FiltroDePeriodo` não tinha label
próprio, só o `accessibilityRole="tablist"`); "Período" é o rótulo mais
direto, mas confirme que não colide visualmente com outro texto próximo
antes de finalizar (leia o JSX ao redor em `ValorDevidoScreen`).

## Testes

- `valor-devido.test.tsx` (já existe): troque qualquer assert que dependia
  da estrutura antiga (`accessibilityRole="tablist"`, texto/estrutura de
  `ChipDeModo`) pelos testes já existentes de `ChipSelector`
  (`ChipSelector.test.tsx`) — não duplique cobertura, só confirme que a
  tela passa as `opcoes`/`valor`/`onChange` certas pro `ChipSelector` e
  que trocar o chip ainda muda `estado.modo` (comportamento, não estilo).

## Fora de escopo

- Não mexer em `PeriodoPersonalizado`/`SeletorDeData` — só o filtro de
  modo (Todos/Este mês/Personalizado).
- Não mexer em `ChipSelector.tsx` — reaproveitar como está.
