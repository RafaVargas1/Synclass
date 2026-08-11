# Task: Professor cadastra Aluno provisório (sem onboarding) (#3)

Card: https://github.com/RafaVargas1/Synclass/issues/3

## Ordem de execução

- [ ] Teste unidade (Domain): cadastro de Aluno provisório com nome e
      identificador válidos cria `Matricula` sem exigir contato/login
      (critério de aceite 1)
- [ ] Teste unidade (Domain): nome vazio/só espaços rejeitado com
      `NomeProvisorioInvalidoException` (edge point, igual à issue #1)
- [ ] Teste unidade (Domain): identificador vazio/só espaços rejeitado com
      `IdentificadorProvisorioInvalidoException`
- [ ] Teste unidade (Domain): identificador já usado por outro Aluno
      provisório do **mesmo** Professor rejeitado com
      `IdentificadorProvisorioDuplicadoException` (critério de aceite 2)
- [ ] Teste unidade (Domain): o mesmo identificador é aceito para **dois
      Professores diferentes** (edge point — unicidade é por
      `(ProfessorId, IdentificadorProvisorio)`, não global)
- [ ] Teste unidade (Domain): promoção de matrícula provisória define
      `AlunoUsuarioId` preservando o `MatriculaId` (e portanto o histórico via
      FK), sem criar uma segunda linha (critério de aceite 4)
- [ ] Teste unidade (Domain): promover uma matrícula já promovida é rejeitado
      (`MatriculaJaPromovidaException`, edge point — a promoção nunca
      sobrescreve um vínculo já existente)
- [ ] Implementação mínima dos itens acima: `Matricula`, `IdentificadorProvisorio`,
      `IMatriculaRepository`, `CadastroAlunoProvisorioService`, exceções
      (`MatriculaRejeitadaException` e subtipos)
- [ ] Migration (`dotnet ef migrations add CriaMatricula`): tabela
      `Matriculas` + índice único parcial em
      `(ProfessorId, IdentificadorProvisorio)` onde não nulo; configuração EF
      Core (`MatriculaConfiguration`) e `MatriculaRepository`
- [ ] Teste de fumaça (Api): `POST /professores/{professorId}/alunos-provisorios`
      com dados válidos retorna 200 com o `matriculaId`
- [ ] Teste de fumaça (Api): nome vazio retorna 400
- [ ] Teste de fumaça (Api): identificador duplicado no mesmo Professor
      retorna 400
- [ ] Implementação mínima: `AlunosProvisoriosController`, DTOs, registro de
      DI, log estruturado `AlunoProvisorioCadastrado` (Information, `TrackId`,
      `ProfessorId`, `MatriculaId`) e `CadastroAlunoProvisorioRejeitado`
      (Warning) — mesma convenção de `ProfessoresController`
- [ ] Teste frontend (`lib/api`): `cadastrarAlunoProvisorio` — sucesso, erro
      de negócio (400) e erro de conexão/timeout
- [ ] Implementação mínima: `frontend/src/lib/api/alunosProvisorios.ts`
- [ ] Teste frontend (organism): `CadastroAlunoProvisorioForm` — captura
      nome/identificador, exibe erro, desabilita botão ao enviar
- [ ] Implementação mínima: `CadastroAlunoProvisorioForm`
- [ ] Teste frontend (tela): fluxo completo de cadastro com confirmação
      inline, contra o contrato já estabilizado do backend
- [ ] Implementação mínima: tela
      `frontend/src/app/professor/[professorId]/alunos/cadastro.tsx` +
      molécula de confirmação

A ordem segue backend até o contrato da Api estabilizar, depois frontend
(ver `fluxo-de-feature.md#fase-3--implementação`).
