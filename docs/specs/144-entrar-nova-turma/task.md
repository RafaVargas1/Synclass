# Task: Aluno autenticado entra em nova turma por código (#144)

Card: https://github.com/RafaVargas1/Synclass/issues/144

Leia `implementation.md` ANTES do primeiro item — as duas ambiguidades
que a issue original deixava em aberto já foram investigadas e resolvidas
(o backend já suporta múltiplas matrículas por Aluno, e o aceite não
precisa de mudança de contrato, só o frontend deixa de pedir nome/contato
de novo). Não re-investigue essas duas perguntas, implemente o desenho já
pronto.

## Ordem de execução

- [ ] Teste (Api, `UsuariosControllerTests.cs`): `GET /usuarios/me`
      devolve `Contato` na resposta. Ver falhar.
- [ ] Implementação mínima: `UsuarioPerfilResponse` ganha o campo
      `Contato`; os dois call sites (`Me`, `AtualizarNome`) passam
      `usuario.Contato` (código exato em `implementation.md`).
- [ ] Teste (`frontend/src/lib/api/usuarios.test.ts`): `buscarPerfil`
      devolve `contato` na resposta de sucesso.
- [ ] Implementação mínima: `frontend/src/lib/api/usuarios.ts` —
      `BuscarPerfilResultado` ganha `contato`.
- [ ] Teste (`frontend/src/lib/usePerfilLogado.test.ts`): estende pra
      cobrir `contato` resolvido junto de `nome`.
- [ ] Implementação mínima: `frontend/src/lib/usePerfilLogado.ts` —
      expõe `contato`, mesmo padrão de `nome`.
- [ ] Teste (`frontend/src/lib/secoesPorPapel.test.ts`): `secoesAluno()`
      inclui a seção nova "Entrar em nova turma" → `/aluno/entrar-em-turma`,
      primeira da lista.
- [ ] Implementação mínima: `frontend/src/lib/secoesPorPapel.ts` —
      adiciona a seção (código exato em `implementation.md`).
- [ ] Teste (`EntrarEmNovaTurmaForm.test.tsx`, criar): render do campo de
      código (5 dígitos), `onChangeCodigo`/`onSubmit` chamados, `enviando`
      desabilita o botão.
- [ ] Implementação mínima: cria
      `frontend/src/components/organisms/EntrarEmNovaTurmaForm.tsx`
      (código exato em `implementation.md` — siga `VerificarCodigoForm.tsx`
      como padrão de estilo pro campo mascarado, sem botão de reenviar).
- [ ] Teste (`frontend/src/app/aluno/entrar-em-turma.test.tsx`, criar):
      submit chama `aceitarConvitePorCodigo({ codigo, nome, contato })`
      com nome/contato vindos do perfil mockado, sem nenhum campo de
      nome/contato na tela; sucesso mostra "Turma adicionada!"; erro da
      Api aparece inline.
- [ ] Implementação mínima: cria
      `frontend/src/app/aluno/entrar-em-turma.tsx` (código exato em
      `implementation.md`).
- [ ] `npm run lint && npm run typecheck && npm test` (frontend) e
      `dotnet format --verify-no-changes && dotnet test` (backend) —
      suíte completa de cada lado, verde.
- [ ] Verificação manual do fluxo N:N (se o ambiente permitir rodar a Api
      localmente): Professor A gera convite por código; Aluno já
      cadastrado com Professor B usa `/aluno/entrar-em-turma` com esse
      código; confirme que o Aluno passa a ter matrícula com os dois
      Professores (`GET /professores/{professorA}/alunos-provisorios` ou
      equivalente lista o Aluno). Se não for possível verificar no
      ambiente, registre isso em "## Inconsistências encontradas" em vez
      de pular em silêncio.
- [ ] Refatore se necessário: releia o diff final contra
      `docs/spec/code-style.md`.

## Fora de escopo

Ver seção "Fora de escopo" de `implementation.md`.
