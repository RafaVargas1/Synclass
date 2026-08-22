# Task: tela "Meus Alunos" (#160)

Card: https://github.com/RafaVargas1/Synclass/issues/160

Leia `implementation.md` ANTES do primeiro item — código pronto pros 4
arquivos (3 novos + 1 editado).

## Ordem de execução

- [x] Teste (`frontend/src/lib/agruparAlocacoesPorAluno.test.ts`, novo):
      casos listados em `implementation.md`. Ver falhar (arquivo ainda
      não existe).
- [x] Implementação mínima: `frontend/src/lib/agruparAlocacoesPorAluno.ts`
      (código pronto em `implementation.md`).
- [x] Teste (`AlunoVinculadoCard.test.tsx`, novo): casos listados em
      `implementation.md`.
- [x] Implementação mínima: `frontend/src/components/organisms/AlunoVinculadoCard.tsx`.
- [x] Teste (`alunos.test.tsx`, novo): os 4 cenários listados em
      `implementation.md` (loading, erro+retry, lista vazia, lista com
      alunos com/sem horário).
- [x] Implementação mínima: `frontend/src/app/professor/[professorId]/alunos.tsx`
      (código pronto em `implementation.md` — extraia pra hook próprio se
      passar dos limites de tamanho de `docs/spec/code-style.md`).
- [x] Teste (`secoesPorPapel.test.ts`): `secoesProfessor(usuarioId)`
      inclui "Meus Alunos" com o href certo.
- [x] Implementação mínima: adicione a seção em
      `frontend/src/lib/secoesPorPapel.ts` (`implementation.md`).
- [x] `npm run lint && npm run typecheck && npm test` verde.
- [x] Refatore se necessário: releia o diff final contra
      `docs/spec/code-style.md` e `docs/spec/engenharia-de-qualidade.md`.

## Fora de escopo

Ver seção "Fora de escopo" de `implementation.md`.

## Inconsistências encontradas
