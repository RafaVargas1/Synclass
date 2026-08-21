# Implementação: Nova versão 2D da marca (#79)

## Componentes afetados

- **Novo**: `frontend/src/components/atoms/Marca.tsx` — substitui
  `Crown.tsx`. Mesma estrutura de átomo (`View` com barras verticais,
  `flex-row items-end`), mas com alturas fixas assimétricas em vez de um
  padrão espelhado configurável por `degraus`.
- **Removido**: `frontend/src/components/atoms/Crown.tsx`.
- **Modificado**: `Topbar.tsx`, `HomeHero.tsx` — trocam `Crown` por `Marca`.

## Por que `escala` no lugar de `degraus`

`Crown` recebia `degraus` (metade do padrão, espelhado pelo componente) —
fazia sentido porque o desenho era paramétrico e sempre simétrico. A nova
marca é um padrão fixo e assimétrico (identidade visual única, não uma
forma geométrica configurável) — receber alturas arbitrárias por caller
quebraria a proposta de "símbolo reconhecível". Por isso a única
variação exposta é `escala` (multiplicador numérico), preservando as
proporções entre as 5 barras em qualquer tamanho de uso.

`escala` no `Topbar` (0.7) deriva de `20px / 28px` — mantém a altura
máxima da marca no cabeçalho igual à do `Crown` anterior (`degraus`
terminava em 20). `HomeHero` usa a escala padrão (1 → altura máxima
28px), única mudança visual de tamanho aceita pelo card (marca um pouco
maior na Home, mesma posição/papel).

## Sem dourado

`Marca` usa só `bg-primary dark:bg-dark-primary`, mesmo token que `Crown`
já usava — não introduz cor nova, só o desenho muda.
