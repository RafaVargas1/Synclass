# Task: identificador de Aluno provisório gerado pelo sistema (#159)

Card: https://github.com/RafaVargas1/Synclass/issues/159

Leia `implementation.md` ANTES do primeiro item — código pronto (antes/
depois) pros dois lados (backend C# + frontend TS/TSX).

## Ordem de execução

- [ ] Teste (Domain, `CadastroAlunoProvisorioServiceTests` ou equivalente):
      `CadastrarAsync(professorId, nome, ct)` (sem `identificador`) produz
      uma `Matricula` com `IdentificadorProvisorio == IdentificadorAluno`,
      ambos iguais ao valor gerado por `IdentificadorAlunoService`. Ver
      falhar (a assinatura atual ainda exige `identificador`).
- [ ] Implementação mínima (backend): aplique a mudança de
      `CadastroAlunoProvisorioService.cs` em `implementation.md` —
      assinatura sem `identificador`, remove
      `GarantirIdentificadorDisponivelAsync`.
- [ ] Teste de fumaça (Api): `POST /professores/alunos-provisorios` com
      corpo `{ "nome": "..." }` (sem `identificador`) devolve 200 com um
      `Identificador` no formato gerado (`ALU-` + sufixo).
- [ ] Implementação mínima (backend): `AlunosProvisoriosController.cs` —
      `CadastroAlunoProvisorioRequest` sem `Identificador`, `Cadastrar` sem
      repassar identificador pro Service.
- [ ] `dotnet format && dotnet test` (backend) verde antes de seguir pro
      frontend — não deixe o backend quebrado enquanto mexe no frontend.
- [ ] Teste (frontend, `CadastroAlunoProvisorioForm.test.tsx`): sem campo
      "Identificador" no formulário; `onSubmit` funciona só com nome. Ver
      falhar.
- [ ] Implementação mínima (frontend): `CadastroAlunoProvisorioForm.tsx`
      sem o campo/props de identificador (`implementation.md`).
- [ ] Teste (frontend, criar `AlunoProvisorioConfirmado.test.tsx` se não
      existir): a tela de confirmação mostra o identificador recebido via
      prop.
- [ ] Implementação mínima (frontend): `AlunoProvisorioConfirmado.tsx`
      ganha a prop `identificador` e mostra na tela (`implementation.md`).
- [ ] Implementação mínima (frontend): `frontend/src/app/professor/alunos/cadastro.tsx`
      e `frontend/src/lib/api/alunosProvisorios.ts` ajustados conforme
      `implementation.md` (input sem `identificador`, estado de
      confirmação carrega nome + identificador devolvidos).
- [ ] Ajuste `frontend/src/app/professor/alunos/cadastro.test.tsx`
      conforme a seção "Testes a ajustar/criar" de `implementation.md`.
- [ ] `npm run lint && npm run typecheck && npm test` (frontend) verde.
- [ ] `grep -rn "identificador" frontend/src/components/organisms/CadastroAlunoProvisorioForm.tsx`
      — confirme que não sobrou nenhuma referência ao campo removido.
- [ ] Refatore se necessário: releia o diff final contra
      `docs/spec/code-style.md` e `docs/spec/engenharia-de-qualidade.md`.

## Fora de escopo

Ver seção "Fora de escopo" de `implementation.md`.
