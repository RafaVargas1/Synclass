# Task: Todo Aluno recebe um identificador único e human-readable (#70)

Card: https://github.com/RafaVargas1/Synclass/issues/70

## Ordem de execução

- [ ] Teste unidade (Domain): `GeradorDeIdentificadorAluno.Gerar()` produz
  formato `ALU-XXXX` (4 caracteres), só com o alfabeto sem caracteres
  ambíguos (sem `0`, `O`, `1`, `I`, `L`).
- [ ] Implementação: `IGeradorDeIdentificadorAluno` (Domain,
  `Synclass.Domain/Alunos/`) + `GeradorDeIdentificadorAluno`
  (Infrastructure, `Synclass.Infrastructure/Alunos/`, via
  `RandomNumberGenerator`, mesmo padrão de `GeradorDeCodigoConvite`).
- [ ] Teste unidade (Domain): `IdentificadorAlunoService.GerarUnicoAsync`
  — cenário feliz (sem colisão), cenário de 1 colisão (retry até achar
  livre, usando `FakeIdentificadorAlunoUnicidadeChecker`), cenário de
  colisões repetidas até o teto de tentativas (lança
  `LimiteDeTentativasDeIdentificadorAlunoExcedidoException`).
- [ ] Implementação: `IIdentificadorAlunoUnicidadeChecker` (Domain) +
  `IdentificadorAlunoService` (Domain, orquestra geração + retry — mesmo
  desenho de `ConviteService.GerarCodigoUnicoAsync`).
- [ ] Teste unidade (Domain): `Usuario.Cadastrar(..., identificadorAluno)`
  grava o identificador quando `papel == Aluno`; não grava (permanece
  `null`) quando `papel == Professor`.
- [ ] Teste unidade (Domain): `Usuario.AdicionarPapel(Aluno, ...,
  identificadorAluno)` grava o identificador na primeira vez que o papel
  Aluno é anexado; chamar de novo com o papel já presente continua
  lançando `PapelJaAtribuidoException` (comportamento existente
  preservado) sem tocar no identificador já gravado.
- [ ] Implementação: `Usuario.Cadastrar`/`Usuario.AdicionarPapel` passam a
  receber `string? identificadorAluno` (obrigatório quando papel Aluno,
  ignorado quando Professor) e gravar em `Usuario.IdentificadorAluno`
  (propriedade nova, sem setter público — só gravada nesses dois pontos).
- [ ] Teste unidade (Domain): `Matricula.CriarProvisoria(...,
  identificadorAluno)` grava o identificador; imutável depois (sem método
  de alteração).
- [ ] Implementação: `Matricula.CriarProvisoria` recebe
  `identificadorAluno` e grava em `Matricula.IdentificadorAluno`
  (propriedade nova, independente de `IdentificadorProvisorio` já
  existente).
- [ ] Migration: nova (não editar migrations já aplicadas) adicionando
  `IdentificadorAluno` (string, nullable) em `Usuarios` e em
  `Matriculas`, cada uma com índice único parcial (`WHERE
  "IdentificadorAluno" IS NOT NULL`), mesmo padrão de
  `MatriculaConfiguration.IdentificadorProvisorio`.
- [ ] Teste unidade (Domain): `CadastroUsuarioService.CadastrarAsync`
  chama `IdentificadorAlunoService.GerarUnicoAsync` e propaga o
  identificador ao criar/atualizar o `Usuario` quando o papel é Aluno; não
  chama quando o papel é Professor.
- [ ] Implementação: injeta `IdentificadorAlunoService` em
  `CadastroUsuarioService` (issue #61).
- [ ] Teste unidade (Domain): `ConviteService.AceitarPorCodigoAsync`
  (issue #63) e `ConviteService.AceitarAsync` propagam o identificador ao
  criar/reaproveitar o `Usuario` via `ObterOuCriarUsuarioAsync`, só quando
  o papel Aluno é de fato anexado (não quando já existia).
- [ ] Implementação: injeta `IdentificadorAlunoService` em
  `ConviteService`.
- [ ] Teste unidade (Domain): `CadastroAlunoProvisorioService.CadastrarAsync`
  gera e grava o identificador na `Matricula` provisória.
- [ ] Implementação: injeta `IdentificadorAlunoService` em
  `CadastroAlunoProvisorioService`.
- [ ] Registro DI: `Program.cs` registra
  `IGeradorDeIdentificadorAluno`/`GeradorDeIdentificadorAluno`,
  `IIdentificadorAlunoUnicidadeChecker`/`IdentificadorAlunoUnicidadeChecker`
  e `IdentificadorAlunoService`.
- [ ] Teste de fumaça (Api): endpoints que já expõem dados do Aluno criado
  (`UsuariosController`, `AlunosController`, `AlunosProvisoriosController`)
  continuam funcionando com o novo campo presente na entidade (sem
  quebrar contrato existente — não é obrigatório expor o campo na
  resposta HTTP neste card, ver `implementation.md`).
- [ ] Suíte completa (`dotnet format && dotnet test`) verde antes do PR.

## Fora de escopo (não implementar)

- Exibir `IdentificadorAluno` nas telas do frontend — não pedido pelos
  Critérios de aceite (que falam de "qualquer tela" em abstrato, sem
  apontar uma tela específica alterada neste card) nem pelos Critérios
  técnicos (só citam #61/#63 como consumidores do gerador no
  backend). Fica registrado como próximo passo natural (issue futura),
  não implementado aqui.
- Reconciliar `Matricula.IdentificadorAluno` (provisório) com o
  `Usuario.IdentificadorAluno` criado na promoção via convite (issue #2)
  — ver `## Inconsistências encontradas` abaixo.

## Inconsistências encontradas

- **Dois identificadores possíveis para a mesma pessoa física na
  promoção de Matrícula provisória (issue #2, fora do escopo direto de
  #61/#63 mas já implementada em `main`).** Quando uma `Matricula`
  provisória (com `IdentificadorAluno` próprio, gerado por este card) é
  promovida via `ConviteService` (`Matricula.Promover`, aceite de convite
  direcionado), o `Usuario` recém-criado ou reaproveitado nessa promoção
  recebe seu **próprio** `IdentificadorAluno` (gerado por
  `ObterOuCriarUsuarioAsync`), sem relação com o da Matrícula que está
  sendo promovida. Resultado: a mesma pessoa passa a ter dois
  identificadores válidos e distintos gravados no banco (um na Matrícula
  promovida, outro no Usuario). **Decisão** (Claude, não delegada à
  DeepSeek): não bloquear #70 por isso — a RN de #70 nomeia só #61/#63
  como consumidores a atualizar, e ajustar o fluxo de promoção da issue
  #2 seria expandir escopo. Registrado aqui e recomendado abrir uma issue
  de acompanhamento própria (ex: "Matrícula promovida reaproveita o
  IdentificadorAluno provisório em vez de gerar um novo") antes de este
  edge case incomodar um Professor de verdade.
