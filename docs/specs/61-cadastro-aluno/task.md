# Task: Aluno se cadastra na plataforma de forma independente (#61)

Card: https://github.com/RafaVargas1/Synclass/issues/61

## Ordem de execução

- [x] Refactor: generaliza `CadastroProfessorService` → `CadastroUsuarioService`
      (`CadastrarAsync(papel, nome, contato)`), mantendo os testes existentes
      de `CadastroProfessorServiceTests` verdes (renomeados para
      `CadastroUsuarioServiceTests`, chamando com `PapelUsuario.Professor`).
- [x] Refactor: renomeia `CadastroProfessorRejeitadoException` →
      `CadastroRejeitadoException` (base genérica, já usada por
      `ContatoInvalidoException`, `NomeInvalidoException`,
      `CadastroConcorrenteException`, `PapelJaAtribuidoException`).
- [x] Teste unidade (Domain): contato inexistente + papel Aluno → cria
      usuário com papel Aluno, sem vínculo com Professor.
- [x] Teste unidade (Domain): usuário já existe como Professor, cadastra
      como Aluno com o mesmo contato → papel Aluno adicionado ao usuário
      existente, sem duplicar identidade (`UsuarioReaproveitado = true`).
- [x] Teste unidade (Domain): contato em formato inválido → rejeita com
      `ContatoInvalidoException` (mensagem clara, mesmo padrão do Professor).
- [x] Extrai `VerificarContatoAsync` para `CadastroUsuarioService` (hoje
      inline em `ProfessoresController`), reaproveitado pelos dois
      controllers — evita duplicar a checagem de identidade existente.
- [x] Teste de fumaça (Api): `POST /alunos/cadastro` — sucesso, contato
      inválido, contato já cadastrado como Aluno (mesmos três casos do
      `ProfessorCadastroEndpointTests`).
- [x] Teste de fumaça (Api): `POST /alunos/cadastro` com contato já
      cadastrado como Professor → papel Aluno adicionado ao mesmo usuário.
- [x] Teste de fumaça (Api): `GET /alunos/verificar-contato`.
- [x] `AlunosController` (`/alunos/cadastro`, `/alunos/verificar-contato`),
      registra `PapelUsuario.Aluno` — espelha `ProfessoresController` reaproveitando
      `CadastroUsuarioService`.
- [x] Log estruturado: evento `UsuarioCadastrado`/`PapelAdicionado` com
      `Papel=Aluno` (mesmo padrão do cadastro de Professor, já genérico via
      `CadastroUsuarioLogging`).
- [x] `Program.cs`: registra `CadastroUsuarioService` no lugar de
      `CadastroProfessorService`.
- [x] Frontend: generaliza `CadastroProfessorForm` → `CadastroUsuarioForm`
      (campos idênticos entre papéis, sem duplicação); `CadastroConfirmado`
      ganha a prop `papel` para o texto de confirmação, único ponto onde o
      papel afeta a UI diretamente.
- [x] Frontend: `lib/api/alunos.ts` (`cadastrarAluno`,
      `verificarContatoAluno`), mesmo formato de `lib/api/professores.ts`.
- [x] Frontend: tela `app/aluno/cadastro.tsx`, espelhando
      `app/professor/cadastro.tsx`, com teste de tela cobrindo os mesmos
      cinco casos do `cadastro.test.tsx` do Professor.
