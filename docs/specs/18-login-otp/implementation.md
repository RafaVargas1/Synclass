# Implementação: login por código de uso único (#18)

## Entidades/classes afetadas

**Domain** — novo módulo `Synclass.Domain/Autenticacao/` (separado de
`Usuarios/`: login é uma responsabilidade própria — sessão/OTP — que
consome `Usuario`, mas não é sobre identidade em si):

- `CodigoOtp` (entidade): `Id`, `UsuarioId`, `CodigoHash`, `ExpiraEm`,
  `UsadoEm` (nullable), `CreatedAt`. Método `Corresponde(codigoBruto)`
  (compara hash), `Expirado(IClock)`, `Invalidar(IClock)` — este último é
  usado tanto para "consumido com sucesso" quanto para "substituído por um
  código novo" (mesmo campo `UsadoEm`, ver Edge points).
- `HashDeCodigoOtp` (estático): SHA-256 do código, hex lowercase. Sem sal —
  código de 6 dígitos já é de uso único e expira em 10min, o mesmo raciocínio
  de custo/benefício que `MascaradorDeContato` aplica a mascaramento, não a
  proteção de senha de longo prazo.
- `LoginService`: orquestra `SolicitarCodigoAsync` e `ConfirmarCodigoAsync`,
  espelhando a forma de `CadastroProfessorService` (injeta
  `IUsuarioRepository`, `IClock` + os novos abaixo).
- `ContatoSemIdentidadePlenaException` / `CodigoOtpInvalidoException`,
  ambas herdando de `LoginRejeitadoException` (abstract) — mesmo padrão de
  `CadastroProfessorRejeitadoException`, para a Api tratar qualquer rejeição
  de forma uniforme.
- Interfaces novas no Domain (implementadas no Infrastructure):
  `ICodigoOtpRepository`, `IGeradorDeCodigoOtp`, `INotificador`,
  `IGeradorDeTokenSessao`.

**Infrastructure** (`Synclass.Infrastructure/Autenticacao/` +
`Persistence/Configurations/CodigoOtpConfiguration.cs`):

- `CodigoOtpRepository : ICodigoOtpRepository` (EF Core).
- `GeradorDeCodigoOtp : IGeradorDeCodigoOtp` — `RandomNumberGenerator`,
  string de 6 dígitos com zero à esquerda.
- `NotificadorDeLog : INotificador` — loga o código via `ILogger` em vez de
  integrar WhatsApp/SMS/e-mail real, conforme autorizado explicitamente
  pelos Critérios técnicos da issue #18 ("implementação inicial pode só
  logar o código em ambiente de desenvolvimento"). Evento de log próprio
  (`OtpEnviadoParaDesenvolvimento`), deliberadamente separado do evento de
  negócio `CodigoOtpSolicitado` da Api (que nunca inclui o código).
- `GeradorDeTokenSessaoJwt : IGeradorDeTokenSessao` — wrapper fino sobre
  `System.IdentityModel.Tokens.Jwt` (`JwtSecurityTokenHandler`), conforme
  `code-style.md#dependências` (envolver biblioteca de terceiros). Claims:
  `sub` (UsuarioId), `name`, um claim `role` por papel do usuário. Chave
  simétrica HMAC-SHA256 de `Jwt:SigningKey` (configuração), expiração de
  `Jwt:ExpiracaoDias` (30) dias.

**Api** (`Controllers/AutenticacaoController.cs`):

- `POST /auth/codigo` → `LoginService.SolicitarCodigoAsync`.
- `POST /auth/confirmacao` → `LoginService.ConfirmarCodigoAsync`.
- Segue o mesmo padrão de `ProfessoresController` (TrackId do
  `Response.Headers`, log de sucesso/rejeição, `MascaradorDeContato` para
  nunca logar o contato em texto pleno).
- **Fora de escopo desta Task**: middleware de validação de JWT
  (`AddAuthentication().AddJwtBearer`) para proteger endpoints futuros —
  nenhum critério de aceite desta issue exige um endpoint protegido ainda;
  fica para a primeira Task que de fato precisar de `[Authorize]`, evitando
  código não exercitado por teste nenhum aqui.

**Frontend**:

- `src/lib/api/auth.ts`: `solicitarCodigo(contato)` /
  `confirmarCodigo(contato, codigo)`, mesmo padrão de
  `src/lib/api/professores.ts` (nunca lança, sempre devolve resultado
  tipado, timeout de 15s).
- `src/lib/auth/sessao.ts`: wrapper fino sobre `expo-secure-store`
  (`salvarToken`/`lerToken`/`limparToken`), conforme
  `code-style.md#dependências`.
- `src/components/organisms/SolicitarCodigoForm.tsx` e
  `VerificarCodigoForm.tsx`: mesmo padrão de `CadastroProfessorForm` (não
  conhecem a Api, só emitem callbacks).
- `src/app/login/index.tsx` (solicita contato) e
  `src/app/login/verificar.tsx` (confirma código, salva sessão, mostra
  confirmação — não há área logada para navegar ainda, mesma decisão já
  registrada em `CadastroConfirmado`).
- `HomeHero`/`HomeTemplate` ganham um link secundário "Entrar" → `/login`,
  ao lado do já existente fluxo de cadastro.

## Contrato de API

### `POST /auth/codigo`

Request: `{ "contato": string }`

- 200 `{ "enviado": true }` — código gerado e "enviado" (logado em dev).
- 400 `{ "mensagem": string }` — contato em formato inválido
  (`ContatoInvalidoException`, reaproveitada de `Usuarios`) ou sem
  identidade plena associada (`ContatoSemIdentidadePlenaException`) — mesma
  mensagem genérica nos dois casos de "sem identidade", sem indicar se o
  contato é de um Aluno provisório ou nunca foi cadastrado.

### `POST /auth/confirmacao`

Request: `{ "contato": string, "codigo": string }`

- 200 `{ "token": string, "usuarioId": guid, "nome": string, "papeis": string[] }`.
- 400 `{ "mensagem": string }` — código incorreto/expirado/já usado
  (`CodigoOtpInvalidoException`) ou contato sem identidade plena
  (`ContatoSemIdentidadePlenaException`).

## Modelo de dados

Nova tabela `CodigosOtp`:

| Coluna | Tipo | Observação |
|---|---|---|
| `Id` | uuid | PK, gerado na aplicação (`ValueGeneratedNever`, mesmo padrão de `Usuario`/`PapelAtribuido`) |
| `UsuarioId` | uuid | FK → `Usuarios.Id`, `OnDelete: Cascade` |
| `CodigoHash` | varchar(64) | SHA-256 em hex |
| `ExpiraEm` | timestamptz | `CreatedAt` + 10min |
| `UsadoEm` | timestamptz, nullable | preenchido ao confirmar com sucesso OU ao ser invalidado por um novo pedido |
| `CreatedAt` | timestamptz | |

Índice em `UsuarioId` (não único — histórico de códigos por usuário fica na
tabela; a "validade" é decidida em memória por `UsadoEm`/`ExpiraEm`, não por
índice único, pois um usuário pode ter vários códigos expirados/usados ao
longo do tempo).

Sem tabela de sessão: o token de sessão é o JWT em si (stateless), conforme
a RN do card.

## Edge points

- `CodigoOtp.Invalidar` reaproveita a única coluna `UsadoEm` tanto para "uso
  bem-sucedido" quanto para "substituído por um pedido de código novo" — a
  migration não tem uma coluna própria de "invalidado" (o card não pediu
  uma), e nenhum critério de aceite depende de diferenciar os dois casos
  depois do fato; só o estado "não é mais válido" importa para a próxima
  tentativa de confirmação.
- `LoginService` busca "o código não-usado mais recente" do usuário
  (`ICodigoOtpRepository.BuscarMaisRecenteNaoUsadoAsync`) em vez de manter
  um ponteiro/flag "código ativo" — como só um pedido por vez invalida o
  anterior, o mais recente não-usado é sempre, por construção, o único
  candidato válido.
- Contato sem identidade plena cobre dois casos hoje indistinguíveis pelo
  repositório atual: usuário inexistente, ou usuário existente com zero
  papéis (`usuario.Papeis.Count == 0`) — este segundo caso não ocorre ainda
  no código atual (`Usuario.Cadastrar` sempre atribui um papel), mas deixa o
  `LoginService` corretamente pronto para o desenho futuro de "Aluno
  provisório" (issue #3, RN da issue #18 explícita sobre isso) sem precisar
  saber como aquela issue efetivamente modela o provisório.
- Ambiente de execução deste pipeline: três worktrees rodam em paralelo no
  mesmo host: `backend/scripts/watch.sh` sobe `docker compose` com portas
  fixas (5432/8080/8081, ver `docker-compose.yml`), que colidiriam entre
  worktrees simultâneas. Guardrail usado aqui foi `dotnet test` (EF Core
  InMemory, mesmo padrão de `ProfessorCadastroEndpointTests`) em vez do
  `watch.sh` ao vivo — decisão conservadora para não derrubar/colidir com as
  outras duas issues em andamento (#3, #6); não afeta a corretude do código,
  só a forma de observar o log localmente durante o desenvolvimento.
- `Jwt:SigningKey` value committed in `appsettings.json`/`.Development.json`
  (mesmo padrão já usado para `ConnectionStrings:Default` neste projeto —
  sem gestão de segredo real ainda, ver `architecture.md#fora-de-escopo-nesta-etapa`);
  precisa migrar para secret manager antes de qualquer deploy real.

## Dependência de outras Tasks

Nenhuma — issue #18 não depende de #3 nem #6 (rodam em paralelo). A issue #4
(papéis acumuláveis) é bloqueada por esta e pelas outras duas por tocar as
mesmas tabelas (`Usuarios`/`PapeisUsuario`).
