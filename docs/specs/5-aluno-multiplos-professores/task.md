# Task: Aluno pode ser vinculado a vários Professores (relação N:N) (#5)

Card: https://github.com/RafaVargas1/Synclass/issues/5

## Ordem de execução

- [x] Teste unidade (Domain, `Matriculas`): duas `Matricula` plenas do mesmo
      `AlunoUsuarioId`, criadas via `Matricula.CriarVinculada` para dois
      `ProfessorId` diferentes, coexistem como linhas independentes — `Id`
      diferentes, cada uma preserva seu próprio `ProfessorId`, nenhuma
      sobrescreve a outra (RN, propriedade estrutural da tabela)
- [x] Teste unidade (Domain, `Convites/ConviteServiceTests`): dado um Aluno já
      pleno de um Professor A (`Matricula` existente com `AlunoUsuarioId`
      definido), quando um Professor B gera e aceita um convite para o
      mesmo contato, então uma nova `Matricula` é criada vinculando o Aluno
      a B, sem alterar a `Matricula` existente com A (critério de aceite 1)
- [x] Teste unidade (Domain, `Convites/ConviteServiceTests`): dado um Aluno
      **provisório** vinculado a um Professor A (`Matricula` com
      `AlunoUsuarioId` nulo), quando o mesmo Aluno se cadastra pleno via
      convite de um Professor B (sem `matriculaId` de origem), então a
      matrícula provisória de A permanece intacta (`AlunoUsuarioId` ainda
      nulo, `NomeProvisorio`/`IdentificadorProvisorio` preservados) enquanto
      uma nova `Matricula` plena é criada para B (critério de aceite 3)
- [ ] Teste unidade (Domain, `Cobrancas/RegraDeCobrancaServiceTests` ou teste
      cruzado novo em `Matriculas`): dado um Aluno com duas `Matricula`
      (Professor A e Professor B), quando uma `RegraDeCobranca` é definida
      para a matrícula com A, então a matrícula com B continua sem regra
      (ou com sua própria regra prévia, se houver) — a mudança não vaza
      entre matrículas do mesmo Aluno (critério de aceite 4)
- [ ] Revisão de comentário XML: `Matricula.cs`/`IMatriculaRepository.cs` já
      documentam o desenho (issue #3/#2); adicionar referência à issue #5
      onde a ausência de índice único em `AlunoUsuarioId` sozinho é o que
      sustenta N:N, para não perder o rastro da decisão (nenhuma mudança de
      comportamento, só documentação)

Nenhum item de migration, log ou frontend — os Critérios técnicos do card
confirmam que a N:N é uma propriedade emergente do desenho já implementado
nas issues #2/#3/#11 (tabela `Matriculas` sem índice único em
`AlunoUsuarioId` isolado, eventos `AlunoProvisorioCadastrado`/
`MatriculaPromovida`/`ConviteAceito` já existentes). A única tela do Aluno
existente hoje (issue #9, `aluno/[matriculaId]/professores/[professorId]/horarios.tsx`)
já é escopada por `matriculaId` explícito — sem retrofit necessário (ver
`implementation.md`).
