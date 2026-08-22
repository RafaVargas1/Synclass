# Task: unificar filtro de período com ChipSelector (#140)

Card: https://github.com/RafaVargas1/Synclass/issues/140

Leia `implementation.md` ANTES do primeiro item — código pronto pra colar.

## Ordem de execução

- [ ] Teste (`valor-devido.test.tsx`): ajuste os testes que dependem da
      estrutura antiga de `FiltroDePeriodo`/`ChipDeModo` pra verificar
      via `ChipSelector` (ex: `getByRole('button', { name: 'Este mês' })`
      já deve funcionar igual, `ChipSelector` usa `Pressable` com role
      button). Rode e veja o que quebra antes de mudar o componente.
- [ ] Implementação mínima: aplique a mudança de `implementation.md`
      (remove `FiltroDePeriodo`/`ChipDeModo`, usa `ChipSelector`).
- [ ] `npm test -- valor-devido` e `npm test -- ChipSelector` — confirme
      os dois passando.
- [ ] Refatore se necessário: releia o diff final contra
      `docs/spec/code-style.md`.

## Fora de escopo

Ver seção "Fora de escopo" de `implementation.md`.
