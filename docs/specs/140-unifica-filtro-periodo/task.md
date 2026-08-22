# Task: unificar filtro de período com ChipSelector (#140)

Card: https://github.com/RafaVargas1/Synclass/issues/140

## Concluída

Implementada e mergeada via PR #148 — todos os itens abaixo já foram
feitos, checkboxes atualizados retroativamente (spec ficou órfã sem
marcação, causando falso positivo de Task pendente no orquestrador
mecânico, ADR-0003).

Leia `implementation.md` ANTES do primeiro item — código pronto pra colar.

## Ordem de execução

- [x] Teste (`valor-devido.test.tsx`): ajuste os testes que dependem da
      estrutura antiga de `FiltroDePeriodo`/`ChipDeModo` pra verificar
      via `ChipSelector` (ex: `getByRole('button', { name: 'Este mês' })`
      já deve funcionar igual, `ChipSelector` usa `Pressable` com role
      button). Rode e veja o que quebra antes de mudar o componente.
- [x] Implementação mínima: aplique a mudança de `implementation.md`
      (remove `FiltroDePeriodo`/`ChipDeModo`, usa `ChipSelector`).
- [x] `npm test -- valor-devido` e `npm test -- ChipSelector` — confirme
      os dois passando.
- [x] Refatore se necessário: releia o diff final contra
      `docs/spec/code-style.md`.

## Fora de escopo

Ver seção "Fora de escopo" de `implementation.md`.
