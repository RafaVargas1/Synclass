# Task: seletor de hora vira campo HH:mm mascarado (#138)

Card: https://github.com/RafaVargas1/Synclass/issues/138

Leia `implementation.md` (ao lado deste arquivo) ANTES do primeiro item —
ele já resolve a decisão de design (campo de texto mascarado, não input
nativo), traz o código de `mascararHora`/`SeletorDeHora` prontos, e explica
o ajuste necessário em `HorarioForm.tsx`. Implemente exatamente isso, não
uma variação.

## Ordem de execução

- [x] Teste (`frontend/src/lib/mascararHora.test.ts`, novo arquivo): casos
      listados em `implementation.md` (mascaramento progressivo, clamp de
      hora/minuto, backspace sobre `:`, `horaEstaCompleta`). Ver primeiro
      falhar (o arquivo `mascararHora.ts` ainda não existe).
- [x] Implementação mínima: crie `frontend/src/lib/mascararHora.ts` com o
      código de `implementation.md` (`mascararHora`, `formatarHoraParcial`,
      `horaEstaCompleta`).
- [ ] Teste (`frontend/src/components/molecules/SeletorDeHora.test.tsx`):
      **substitua o arquivo inteiro** pelos casos descritos em
      `implementation.md` (o componente muda de forma, os testes antigos
      testam uma UI que deixa de existir — não tente manter os testes
      antigos passando).
- [ ] Implementação mínima: reescreva
      `frontend/src/components/molecules/SeletorDeHora.tsx` com o código de
      `implementation.md`.
- [ ] Ajuste em `frontend/src/components/organisms/HorarioForm.tsx`
      (função `validar`): troque a checagem `horaInicio === undefined` por
      `!horaEstaCompleta(horaInicio ?? '')`, importando `horaEstaCompleta`
      de `@/lib/mascararHora` — ver "Edge point" em `implementation.md`.
      Rode `HorarioForm.test.tsx` depois — se algum teste quebrar por causa
      dessa mudança de contrato, ajuste o teste (não a regra), consultando
      o teste atual pra entender o que ele espera.
- [ ] `grep -rn "SeletorDeHora" frontend/src` — confirme que `HorarioForm`
      é o único consumidor e que não sobrou nenhum outro lugar referenciando
      a UI antiga (ex: nenhum outro teste importa `SeletorDeHora` esperando
      o painel de 2 listas).
- [ ] Refatore se necessário: releia o diff final contra
      `docs/spec/code-style.md` e `docs/spec/engenharia-de-qualidade.md`
      antes de marcar a Task concluída.

## Fora de escopo

Ver seção "Fora de escopo" de `implementation.md`.

## Inconsistências encontradas

### Backspace sobre o `:` ("remove o dígito anterior a ele")

O texto de `implementation.md` descreve o backspace sobre o `:` como
"remove o dígito anterior a ele", mas o código fornecido (a fonte da
decisão) se comporta como `mascararContato`: ao apagar um caractere de
máscara, remove o **último** dígito, não o dígito imediatamente anterior
ao caractere apagado. Exemplo com o código verbatim:
`mascararHora('1030', '10:30')` → `'10:3'` (remove o último dígito, o `0`
de minuto). Segui o código fornecido (design source of truth), não a
literalidade da frase; o teste reflete o comportamento real do código.
