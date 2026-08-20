# Implementação: Aluno se cadastra na plataforma de forma independente (#61)

Espelha o cadastro de Professor (issue #1,
`docs/specs/1-*` — pasta não existe mais no repo atual, o código
correspondente é a referência: `Synclass.Domain.Usuarios.CadastroProfessorService`
antes deste refactor). A diferença é só o `PapelUsuario` atribuído; o card
pede explicitamente avaliar compartilhamento em vez de duplicar.

## Entidades/classes afetadas

- **Domain** (`Synclass.Domain.Usuarios`):
  - `CadastroProfessorService` → renomeado para `CadastroUsuarioService`.
    Método `CadastrarProfessorAsync(nome, contato)` vira
    `CadastrarAsync(PapelUsuario papel, nome, contato)`. Ganha também
    `VerificarContatoAsync(contatoBruto)` (lógica hoje inline em
    `ProfessoresController.VerificarContato`), para não duplicar em
    `AlunosController`.
  - `ResultadoCadastroProfessor` → `ResultadoCadastroUsuario(Usuario, PapelUsuario Papel, bool UsuarioReaproveitado)`.
    Ganha o campo `Papel` porque o log estruturado (evento
    `UsuarioCadastrado`/`PapelAdicionado`) precisa saber qual papel foi
    atribuído nesta chamada — antes vinha de uma constante `PapelUsuario.Professor`
    hardcoded no controller.
  - `CadastroProfessorRejeitadoException` → `CadastroRejeitadoException`
    (nome já era genérico em espírito — só o nome do arquivo/classe
    mencionava Professor). `ContatoInvalidoException`, `NomeInvalidoException`,
    `CadastroConcorrenteException`, `PapelJaAtribuidoException` passam a
    herdar da classe renomeada (sem mudança de comportamento).
  - `Usuario`, `PapelUsuario`, `PapelAtribuido`, `NomeUsuario`, `Contato`:
    sem mudança — já são genéricos por papel.
- **Api** (`Synclass.Api.Controllers`):
  - `ProfessoresController`: passa a injetar `CadastroUsuarioService` em vez
    de `CadastroProfessorService`; chama `CadastrarAsync(PapelUsuario.Professor, ...)`
    e `VerificarContatoAsync(...)`.
  - `AlunosController` (novo): mesmo formato, `PapelUsuario.Aluno`, rotas
    `/alunos/cadastro` (`POST`) e `/alunos/verificar-contato` (`GET`).
  - `CadastroUsuarioLogging` (novo, estático): `LogCadastroSucesso` e
    `LogCadastroRejeitado`, reaproveitados pelos dois controllers — a única
    diferença entre os logs de Professor e Aluno já era o valor de `Papel`,
    que agora vem de `ResultadoCadastroUsuario.Papel`/da exceção, não mais
    hardcoded por controller.
  - DTOs (`CadastroUsuarioRequest`, `CadastroUsuarioResponse`,
    `CadastroUsuarioErrorResponse`, `VerificarContatoResponse`): movidos para
    um arquivo compartilhado (`CadastroUsuarioDtos.cs`), reaproveitados pelos
    dois controllers — o corpo de request/response é idêntico entre
    Professor e Aluno.
  - `Program.cs`: `AddScoped<CadastroProfessorService>()` →
    `AddScoped<CadastroUsuarioService>()`.
- **Frontend**:
  - `CadastroProfessorForm` → `CadastroUsuarioForm`
    (`src/components/organisms/`), ganha prop `papel: 'Professor' | 'Aluno'`
    usada só para o texto do placeholder do nome não muda (campos
    idênticos) — mantém a mesma interface de callbacks.
  - `lib/api/professores.ts` fica como está (endpoint de Professor
    continua existindo). Novo `lib/api/alunos.ts` com
    `cadastrarAluno`/`verificarContatoAluno`, mesmo formato, apontando para
    `/alunos/cadastro` e `/alunos/verificar-contato`.
  - Nova tela `app/aluno/cadastro.tsx`, espelhando
    `app/professor/cadastro.tsx` com o form/lib de Aluno.

## Contrato de API

- `POST /alunos/cadastro`
  - Request: `{ "nome": string, "contato": string }`
  - 200: `{ "usuarioId": guid, "nome": string }`
  - 400: `{ "mensagem": string }` (contato inválido, nome inválido, ou
    contato já cadastrado como Aluno)
- `GET /alunos/verificar-contato?contato=<string>`
  - 200: `{ "identidadeExistente": bool, "nome": string | null }` — nunca
    rejeita (mesmo comportamento do endpoint de Professor).

## Modelo de dados

Nenhuma migration nova — reaproveita `Usuario`/`PapelAtribuido` já
existentes (`PapelUsuario.Aluno` já é um valor válido do enum desde a
issue #4).

## Edge points

- Normalização de contato (espaços/maiúsculas) antes de checar duplicidade:
  já resolvido por `Contato.Normalizar`, reaproveitado sem mudança.
- Reenvio idêntico do mesmo cadastro (duplo clique) cai em
  `PapelJaAtribuidoException`, mesmo comportamento do Professor.
- Tela de cadastro de Aluno não é linkada a partir da Home (`app/index.tsx`)
  nesta Task — o CTA da Home é escopo do redesign em andamento
  (`docs/ux-heuristics`) e não é um critério de aceite desta issue. A tela
  fica acessível por rota direta (`/aluno/cadastro`), mesmo padrão de outras
  telas que ainda não têm entrada de navegação própria.

## Dependência de outras Tasks

Nenhuma — pré-requisito para "Aluno usa o código de convite para entrar na
turma do Professor" (próxima Task do épico #60), que consome a identidade
criada aqui.
