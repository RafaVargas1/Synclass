# Task: Nova versão 2D da marca, estilo 'gráfico de presença', sem dourado (#79)

Card: https://github.com/RafaVargas1/Synclass/issues/79

Decisão de design confirmada pelo mantenedor (critério técnico do card
pedia essa confirmação antes de codar): 5 barras, alturas bem irregulares
(sem progressão simétrica), proporção base `[12, 24, 16, 28, 18]`
(em px, escala 1).

## Ordem de execução

- [ ] Teste unidade: `Marca` (novo átomo, substitui `Crown`) — renderiza 5 barras com as alturas base `[12, 24, 16, 28, 18]` quando `escala` não é informado (default 1)
- [ ] Teste unidade: `Marca` — aplica o multiplicador de `escala` a cada altura (ex: `escala=0.5` → `[6, 12, 8, 14, 9]`)
- [ ] Teste unidade: `Marca` — `invertido` aplica `scaleY: -1` (mesmo comportamento do `Crown` atual), para o par de barras espelhado verticalmente na moldura da Home
- [ ] Teste unidade: `Marca` — usa só os tokens `bg-primary`/`dark:bg-dark-primary` (nenhuma cor dourada/bronze/petróleo hardcoded)
- [ ] Implementação: cria `frontend/src/components/atoms/Marca.tsx`, substituindo `Crown.tsx` (mesmo formato de átomo: `View` com barras `flex-row items-end`, prop `escala?: number` no lugar de `degraus: number[]`, mantém `invertido?: boolean`)
- [ ] Implementação: remove `frontend/src/components/atoms/Crown.tsx`
- [ ] Implementação: atualiza `Topbar.tsx` — troca `<Crown degraus={[10, 16, 20]} />` por `<Marca escala={0.7} />` (mantém a marca proporcionalmente menor no cabeçalho, ~20px de altura máxima, igual ao tamanho anterior)
- [ ] Implementação: atualiza `HomeHero.tsx` — troca as duas ocorrências de `<Crown degraus={[8, 16, 24]} />`/`<Crown degraus={[8, 16, 24]} invertido />` por `<Marca />`/`<Marca invertido />` (escala 1, altura máxima 28px)
- [ ] Docs: nenhuma atualização de `design-system.md` necessária — o átomo mantém o mesmo papel documentado (marca ao lado do wordmark), só troca o desenho geométrico

### Inconsistências encontradas

_(Nenhuma — decisão de design já confirmada pelo mantenedor antes desta spec, ver acima.)_
