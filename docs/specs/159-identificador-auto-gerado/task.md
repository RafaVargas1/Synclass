# Task: identificador de Aluno provisório gerado pelo sistema (#159)

Card: https://github.com/RafaVargas1/Synclass/issues/159

## Concluída

Implementada e mergeada via PR #163 (`dc70f88`, `Closes #159`) — todos os
itens abaixo já foram feitos, checkboxes atualizados retroativamente
(spec ficou órfã sem marcação, causando falso positivo de Task pendente
no orquestrador mecânico, ADR-0003).

Leia `implementation.md` ANTES do primeiro item — código pronto (antes/
depois) pros dois lados (backend C# + frontend TS/TSX).

## Ordem de execução

- [x] Teste (Domain, `CadastroAlunoProvisorioServiceTests` ou equivalente):
      `CadastrarAsync(professorId, nome, ct)` (sem `identificador`) produz
      uma `Matricula` com `IdentificadorProvisorio == IdentificadorAluno`,
      ambos iguais ao valor gerado por `IdentificadorAlunoService`. Ver
      falhar (a assinatura atual ainda exige `identificador`).
- [x] Implementação mínima (backend): aplique a mudança de
      `CadastroAlunoProvisorioService.cs` em `implementation.md` —
      assinatura sem `identificador`, remove
      `GarantirIdentificadorDisponivelAsync`.
- [x] Teste de fumaça (Api): `POST /professores/alunos-provisorios` com
      corpo `{ "nome": "..." }` (sem `identificador`) devolve 200 com um
      `Identificador` no formato gerado (`ALU-` + sufixo).
- [x] Implementação mínima (backend): `AlunosProvisoriosController.cs` —
      `CadastroAlunoProvisorioRequest` sem `Identificador`, `Cadastrar` sem
      repassar identificador pro Service.
- [x] `dotnet format && dotnet test` (backend) verde antes de seguir pro
      frontend — não deixe o backend quebrado enquanto mexe no frontend.
- [x] Teste (frontend, `CadastroAlunoProvisorioForm.test.tsx`): sem campo
      "Identificador" no formulário; `onSubmit` funciona só com nome. Ver
      falhar.
- [x] Implementação mínima (frontend): `CadastroAlunoProvisorioForm.tsx`
      sem o campo/props de identificador (`implementation.md`).
- [x] Teste (frontend, criar `AlunoProvisorioConfirmado.test.tsx` se não
      existir): a tela de confirmação mostra o identificador recebido via
      prop.
- [x] Implementação mínima (frontend): `AlunoProvisorioConfirmado.tsx`
      ganha a prop `identificador` e mostra na tela (`implementation.md`).
- [x] Implementação mínima (frontend): `frontend/src/app/professor/alunos/cadastro.tsx`
      e `frontend/src/lib/api/alunosProvisorios.ts` ajustados conforme
      `implementation.md` (input sem `identificador`, estado de
      confirmação carrega nome + identificador devolvidos).
- [x] Ajuste `frontend/src/app/professor/alunos/cadastro.test.tsx`
      conforme a seção "Testes a ajustar/criar" de `implementation.md`.
- [x] `npm run lint && npm run typecheck && npm test` (frontend) verde.
- [x] `grep -rn "identificador" frontend/src/components/organisms/CadastroAlunoProvisorioForm.tsx`
      — confirme que não sobrou nenhuma referência ao campo removido.
- [x] Refatore se necessário: releia o diff final contra
      `docs/spec/code-style.md` e `docs/spec/engenharia-de-qualidade.md`.

## Fora de escopo

Ver seção "Fora de escopo" de `implementation.md`.
