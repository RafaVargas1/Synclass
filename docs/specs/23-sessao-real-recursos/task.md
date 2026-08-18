# Task: Ligar professorId de sessão real no cadastro de Aluno provisório quando #18 mergear (#23)

Card: https://github.com/RafaVargas1/Synclass/issues/23

Débito técnico documentado desde os PRs #22 (#3), #30 (#8) e #32 (#9) — a
sessão real (issue #4: JWT + `[Authorize]` no backend, `SessaoProvider`/
`useSessao()` no frontend) já está plugada nos dois lados; esta Task troca
os `professorId`/`matriculaId` de parâmetro de rota confiável para leitura
de sessão + checagem de posse.

## Ordem de execução

- [x] Teste de fumaça (Api): extensão `ClaimsPrincipalExtensions.GetUsuarioId()`
      lê `ClaimTypes.NameIdentifier` do JWT e devolve o `Guid` do `Usuario`
      autenticado
- [x] `AlunosProvisoriosController`: rota `professores/{professorId}/alunos-provisorios`
      → `professores/alunos-provisorios`; `Cadastrar`/`Listar` usam
      `User.GetUsuarioId()` em vez do parâmetro de rota — teste de fumaça
      confirma que dois Professores autenticados só veem/cadastram os
      próprios Alunos provisórios
- [x] `AlocacoesHorarioController`: rota `professores/{professorId}/horarios/{horarioId}/alocacoes`
      → `professores/horarios/{horarioId}/alocacoes`; `Alocar`/`Listar`/
      `Desalocar` usam `User.GetUsuarioId()` — teste de fumaça confirma que
      um Professor não consegue alocar/desalocar/listar em um `horarioId`
      de outro Professor (403 via `HorarioNaoEncontradoException`, mesmo
      comportamento hoje devolvido para `horarioId` inexistente)
- [x] `AlocacaoHorarioService.ResolverMatriculaDoAlunoAsync(professorId, alunoUsuarioId, ct)`:
      novo método usando `IMatriculaRepository.BuscarVinculoAsync` — lança
      `AlunoNaoVinculadoAoProfessorException` quando não há vínculo
- [x] `MarcacoesHorarioController`: `ListarVagos`/`Marcar` param `matriculaId`
      (query/body) removido; resolvido via `ResolverMatriculaDoAlunoAsync`
      com `User.GetUsuarioId()` — teste de fumaça confirma que um Aluno não
      consegue marcar/listar horários vagos de um Professor ao qual não
      está vinculado (404)
- [x] `lib/api/alunosProvisorios.ts`: `cadastrarAlunoProvisorio`/
      `listarAlunosProvisorios` removem `professorId` do input, chamam
      caminho fixo
- [x] `frontend/src/app/professor/[professorId]/alunos/cadastro.tsx` →
      `frontend/src/app/professor/alunos/cadastro.tsx` (remove segmento
      dinâmico, não lê mais `professorId`)
- [x] `lib/api/alocacoes.ts`: `alocarAluno`/`listarAlocacoes`/`desalocarAluno`
      removem `professorId` do input, chamam caminho fixo
      (`professor/[professorId]/alocacoes.tsx` mantém o segmento — ainda
      chama `HorariosController`/`ConfiguracoesController`, fora de escopo
      desta Task — só para de repassar `professorId` às chamadas em escopo)
- [x] `lib/api/marcacoes.ts`: `listarHorariosVagos`/`marcarHorario` removem
      `matriculaId` do input
- [x] `frontend/src/app/aluno/[matriculaId]/professores/[professorId]/horarios.tsx`
      → `frontend/src/app/aluno/professores/[professorId]/horarios.tsx`
      (remove só o segmento `[matriculaId]`; `[professorId]` continua
      identificando o Professor sendo navegado, não é a identidade do
      chamador)
