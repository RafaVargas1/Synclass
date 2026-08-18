# Implementação: Usuário edita seu nome no perfil (#27)

## Entidades/classes afetadas

- `Synclass.Domain.Usuarios.Usuario` (modificado): novo método
  `AtualizarNome(string nomeBruto)`, valida via `NomeUsuario.Validar` (mesma
  regra do cadastro) e sobrescreve `Nome`. Segue o mesmo padrão de
  `AdicionarPapel` — mutação encapsulada na entidade, não um setter público.
- `Synclass.Domain.Usuarios.IUsuarioRepository` (modificado): novo método
  `Task<Usuario?> BuscarPorIdAsync(Guid id, CancellationToken)`.
  `Synclass.Infrastructure.Persistence.UsuarioRepository` implementa via
  `_dbContext.Usuarios.FirstOrDefaultAsync(u => u.Id == id, ct)` — sem
  `Include(Papeis)` (não é necessário para atualizar nome, evita join
  desnecessário).
- `Synclass.Domain.Usuarios.UsuarioNaoEncontradoException` (novo): lançada
  quando o `usuarioId` do token não corresponde a nenhum `Usuario`
  persistido — defensivo (não deveria acontecer com um JWT válido emitido
  pelo próprio sistema), mapeado para 404 pela Api.
- `Synclass.Domain.Usuarios.AtualizacaoNomeUsuarioService` (novo): orquestra
  `NomeUsuario.Validar` → `BuscarPorIdAsync` → `Usuario.AtualizarNome` →
  `SalvarAsync`, mesmo formato de `CadastroProfessorService`.
- `Synclass.Api.Controllers.UsuariosController` (novo): `[Authorize]` sem
  restrição de `Roles` (Professor e Aluno editam nome igualmente — o nome
  pertence ao `Usuario`, não ao papel, RN da issue #27).
- `Synclass.Api.Controllers.ProfessoresController` (modificado): nova ação
  `GET /professores/verificar-contato` — pública (roda antes do login
  existir), reaproveita `IUsuarioRepository.BuscarPorContatoAsync` direto
  (sem novo serviço de Domain: é só uma leitura, sem regra de negócio a
  proteger).

## Contrato de API

- `GET /usuarios/me` — `[Authorize]`. Sem parâmetros (usuário vem do token).
  - 200: `{ usuarioId: Guid, nome: string }`
- `PUT /usuarios/me/nome` — `[Authorize]`. Body: `{ nome: string }`.
  - 200: `{ nome: string }`
  - 400: `{ mensagem: string }` (nome vazio/inválido, `NomeInvalidoException`)
  - 404: `{ mensagem: string }` (`UsuarioNaoEncontradoException`, defensivo)
- `GET /professores/verificar-contato?contato=<string>` — sem autenticação
  (mesmo estágio do fluxo do `POST /professores/cadastro`, antes de existir
  sessão).
  - 200: `{ identidadeExistente: bool, nome: string | null }` — `nome` só
    preenchido quando `identidadeExistente` é `true`. Contato inválido não
    rejeita (200 com `identidadeExistente: false`) — o formulário de
    cadastro já valida o contato no submit final; este endpoint só informa
    se deve travar o campo Nome.

## Modelo de dados

Nenhuma migration nova — `Usuarios.Nome` já existe (issue #1/#4). Só
update de uma coluna já mapeada.

## Edge points

- `Usuario.AtualizarNome` faz `NomeUsuario.Validar` internamente (diferente
  de `Usuario.Cadastrar`, que recebe o nome já validado pelo chamador) — a
  entidade se torna a única responsável por proteger o próprio invariante em
  todo ponto de mutação de `Nome`, não só na criação. `CadastroProfessorService`
  continua validando antes de chamar `Usuario.Cadastrar` (não muda, para não
  tocar em código estável fora do escopo desta issue) — a assimetria é
  aceitável porque ambos os pontos de entrada continuam protegidos.
- `GET /professores/verificar-contato` não normaliza erro de contato
  inválido como falha: um contato ainda incompleto enquanto o usuário digita
  (ex: e-mail sem `@`) deve devolver "não existe" silenciosamente, não um
  400 que a tela precisaria engolir a cada tecla.
- Log `NomeAtualizado` (Information, `TrackId`, `UsuarioId`) só no endpoint
  de update — `GET /usuarios/me` e `GET /professores/verificar-contato` são
  leitura, sem evento de negócio a registrar.
- Frontend: `verificarContatoProfessor` roda no `onBlur` do campo Contato de
  `CadastroProfessorForm`; falha de rede nesse endpoint não deve travar o
  cadastro — em caso de erro, mantém o campo Nome editável (fail-open: pior
  caso é o RN original sendo aplicado no fim, o cadastro ainda ignora o nome
  reenviado no backend independente do que a tela mostrou).

## Dependências

Nenhuma Task irmã — depende apenas de infraestrutura já mergeada (sessão
real via JWT, issue #23/PR #36, `ClaimsPrincipalExtensions.GetUsuarioId()`).
