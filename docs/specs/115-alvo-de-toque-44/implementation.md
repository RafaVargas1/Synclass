# Implementação: Alvo de toque ≥44×44pt (#115)

## Componentes afetados

Todos client-side, sem mudança de contrato de API. Lista completa no
`task.md`. Padrão único aplicado em cada um: `style={{ minWidth: 44,
minHeight: 44 }}` + `items-center justify-center` no `Pressable`,
mantendo o padding/traço visual atual (`px-two py-one`, texto pequeno)
dentro dessa área maior — mesmo raciocínio já documentado em
`docs/spec/ux-heuristics.md#alvos-de-toque` e aplicado uma vez em
`Topbar.tsx`.

## Por que não criar um wrapper `PressableComAlvoMinimo`

Cada `Pressable` afetado já tem um `className` específico (cor de fundo,
borda, raio) que não compensa a abstração de um wrapper genérico só para
acrescentar duas linhas de `style`. Aplicar o padrão inline, componente a
componente, é mais direto de revisar e não introduz uma indireção nova
sem necessidade (`docs/spec/code-style.md`).

## Testes

Cada `Pressable` alterado ganha uma asserção `toHaveStyle({ minWidth: 44,
minHeight: 44 })`, mesmo padrão de `Topbar.test.tsx` ("gives the back
button a touch target of at least 44x44 (Fitts/HIG)").
