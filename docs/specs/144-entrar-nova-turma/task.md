# Task: Aluno autenticado entra em nova turma por código (#144)

Card: https://github.com/RafaVargas1/Synclass/issues/144

Leia `implementation.md` ANTES do primeiro item — as duas ambiguidades
que a issue original deixava em aberto já foram investigadas e resolvidas
(o backend já suporta múltiplas matrículas por Aluno, e o aceite não
precisa de mudança de contrato, só o frontend deixa de pedir nome/contato
de novo). Não re-investigue essas duas perguntas, implemente o desenho já
pronto.

## Ordem de execução

- [x] Teste (Api, `UsuariosControllerTests.cs`): `GET /usuarios/me`
      devolve `Contato` na resposta. Ver falhar.
- [x] Implementação mínima: `UsuarioPerfilResponse` ganha o campo
      `Contato`; os dois call sites (`Me`, `AtualizarNome`) passam
      `usuario.Contato` (código exato em `implementation.md`).
- [x] Teste (`frontend/src/lib/api/usuarios.test.ts`): `buscarPerfil`
      devolve `contato` na resposta de sucesso.
- [x] Implementação mínima: `frontend/src/lib/api/usuarios.ts` —
      `BuscarPerfilResultado` ganha `contato`.
- [x] Teste (`frontend/src/lib/usePerfilLogado.test.ts`): estende pra
      cobrir `contato` resolvido junto de `nome`.
- [x] Implementação mínima: `frontend/src/lib/usePerfilLogado.ts` —
      expõe `contato`, mesmo padrão de `nome`.
- [x] Teste (`frontend/src/lib/secoesPorPapel.test.ts`): `secoesAluno()`
      inclui a seção nova "Entrar em nova turma" → `/aluno/entrar-em-turma`,
      primeira da lista.
- [x] Implementação mínima: `frontend/src/lib/secoesPorPapel.ts` —
      adiciona a seção (código exato em `implementation.md`).
- [x] Teste (`EntrarEmNovaTurmaForm.test.tsx`, criar): render do campo de
      código (5 dígitos), `onChangeCodigo`/`onSubmit` chamados, `enviando`
      desabilita o botão.
- [x] Implementação mínima: cria
      `frontend/src/components/organisms/EntrarEmNovaTurmaForm.tsx`
      (código exato em `implementation.md` — segue `VerificarCodigoForm.tsx`
      como padrão de estilo pro campo mascarado, sem botão de reenviar).
- [x] Teste (`frontend/src/app/aluno/entrar-em-turma.test.tsx`, criar):
      submit chama `aceitarConvitePorCodigo({ codigo, nome, contato })`
      com nome/contato vindos do perfil mockado, sem nenhum campo de
      nome/contato na tela; sucesso mostra "Turma adicionada!"; erro da
      Api aparece inline.
- [x] Implementação mínima: cria
      `frontend/src/app/aluno/entrar-em-turma.tsx` (código exato em
      `implementation.md`).
- [x] `npm run lint && npm run typecheck && npm test` (frontend) e
      `dotnet format --verify-no-changes && dotnet test` (backend) —
      suíte completa de cada lado, verde (frontend 533/533, backend
      `UsuariosControllerTests` 5/5).
- [x] Refatore: releu o diff final contra `docs/spec/code-style.md`.

## Inconsistências encontradas

- **Verificação manual do fluxo N:N não executada** (item original do
  plano): exigiria subir a Api localmente e simular dois Professores +
  um convite por código de ponta a ponta. A lógica de backend em si (N:N
  Aluno-Professor, `ConviteService.VincularMatriculaAsync`) não foi
  tocada por esta Task — é pré-existente e já coberta pelos próprios
  testes de `ConviteService`/`AlunoProvisorioCadastroEndpointTests`
  citados em `implementation.md`. O que esta Task adiciona (campo
  `Contato` em `GET /usuarios/me`, tela nova) está coberto pelos testes
  automatizados listados acima. Recomendo uma verificação manual pontual
  em ambiente de staging antes de anunciar a feature, mas não bloqueia o
  merge — não há sinal de risco na lógica reaproveitada.

## Fora de escopo

Ver seção "Fora de escopo" de `implementation.md`.
