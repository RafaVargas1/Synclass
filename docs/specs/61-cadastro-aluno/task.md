# Task: Aluno se cadastra na plataforma de forma independente (#61)

Card: https://github.com/RafaVargas1/Synclass/issues/61

## Ordem de execução

- [ ] Refactor: generaliza `CadastroProfessorService` → `CadastroUsuarioService`
      (`CadastrarAsync(papel, nome, contato)`), mantendo os testes existentes
      de `CadastroProfessorServiceTests` verdes (renomeados para
      `CadastroUsuarioServiceTests`, chamando com `PapelUsuario.Professor`).
- [ ] Refactor: renomeia `CadastroProfessorRejeitadoException` →
      `CadastroRejeitadoException` (base genérica, já usada por
      `ContatoInvalidoException`, `NomeInvalidoException`,
      `CadastroConcorrenteException`, `PapelJaAtribuidoException`).
- [ ] Teste unidade (Domain): contato inexistente + papel Aluno → cria
      usuário com papel Aluno, sem vínculo com Professor.
- [ ] Teste unidade (Domain): usuário já existe como Professor, cadastra
      como Aluno com o mesmo contato → papel Aluno adicionado ao usuário
      existente, sem duplicar identidade (`UsuarioReaproveitado = true`).
- [ ] Teste unidade (Domain): contato em formato inválido → rejeita com
      `ContatoInvalidoException` (mensagem clara, mesmo padrão do Professor).
- [ ] Extrai `VerificarContatoAsync` para `CadastroUsuarioService` (hoje
      inline em `ProfessoresController`), reaproveitado pelos dois
      controllers — evita duplicar a checagem de identidade existente.
- [ ] Teste de fumaça (Api): `POST /alunos/cadastro` — sucesso, contato
      inválido, contato já cadastrado como Aluno (mesmos três casos do
      `ProfessorCadastroEndpointTests`).
- [ ] Teste de fumaça (Api): `POST /alunos/cadastro` com contato já
      cadastrado como Professor → papel Aluno adicionado ao mesmo usuário.
- [ ] Teste de fumaça (Api): `GET /alunos/verificar-contato`.
- [ ] `AlunosController` (`/alunos/cadastro`, `/alunos/verificar-contato`),
      registra `PapelUsuario.Aluno` — espelha `ProfessoresController` reaproveitando
      `CadastroUsuarioService`.
- [ ] Log estruturado: evento `UsuarioCadastrado`/`PapelAdicionado` com
      `Papel=Aluno` (mesmo padrão do cadastro de Professor, já genérico via
      `CadastroUsuarioLogging`).
- [ ] `Program.cs`: registra `CadastroUsuarioService` no lugar de
      `CadastroProfessorService`.
- [ ] Frontend: generaliza `CadastroProfessorForm` → `CadastroUsuarioForm`
      (parametrizado por papel só no texto do formulário — os campos são os
      mesmos), com teste de componente cobrindo o novo parâmetro.
- [ ] Frontend: `lib/api/alunos.ts` (`cadastrarAluno`,
      `verificarContatoAluno`), mesmo formato de `lib/api/professores.ts`.
- [ ] Frontend: tela `app/aluno/cadastro.tsx`, espelhando
      `app/professor/cadastro.tsx`, com teste de tela cobrindo os mesmos
      cinco casos do `cadastro.test.tsx` do Professor.
