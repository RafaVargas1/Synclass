# Task: tela "Meus Alunos" (#160)

Card: https://github.com/RafaVargas1/Synclass/issues/160

Leia `implementation.md` ANTES do primeiro item — código pronto pros 4
arquivos (3 novos + 1 editado).

## Ordem de execução

- [ ] Teste (`frontend/src/lib/agruparAlocacoesPorAluno.test.ts`, novo):
      casos listados em `implementation.md`. Ver falhar (arquivo ainda
      não existe).
- [ ] Implementação mínima: `frontend/src/lib/agruparAlocacoesPorAluno.ts`
      (código pronto em `implementation.md`).
- [ ] Teste (`AlunoVinculadoCard.test.tsx`, novo): casos listados em
      `implementation.md`.
- [ ] Implementação mínima: `frontend/src/components/organisms/AlunoVinculadoCard.tsx`.
- [ ] Teste (`alunos.test.tsx`, novo): os 4 cenários listados em
      `implementation.md` (loading, erro+retry, lista vazia, lista com
      alunos com/sem horário).
- [ ] Implementação mínima: `frontend/src/app/professor/[professorId]/alunos.tsx`
      (código pronto em `implementation.md` — extraia pra hook próprio se
      passar dos limites de tamanho de `docs/spec/code-style.md`).
- [ ] Teste (`secoesPorPapel.test.ts`): `secoesProfessor(usuarioId)`
      inclui "Meus Alunos" com o href certo.
- [ ] Implementação mínima: adicione a seção em
      `frontend/src/lib/secoesPorPapel.ts` (`implementation.md`).
- [ ] `npm run lint && npm run typecheck && npm test` verde.
- [ ] Refatore se necessário: releia o diff final contra
      `docs/spec/code-style.md` e `docs/spec/engenharia-de-qualidade.md`.

## Fora de escopo

Ver seção "Fora de escopo" de `implementation.md`.
