
# Desenho técnico — Conectar conta Mercado Pago do Professor (#203)

## Entidades/classes afetadas

### Domain (novo — namespace `Synclass.Domain.Pagamentos`)

- `ConexaoMercadoPago` (entidade nova — não existe precedente de entidade de pagamento no repo; este é o primeiro domínio de integração financeira):
  - `Guid Id`
  - `Guid ProfessorId` (referencia `Usuario.Id` de um usuário com `PapelUsuario.Professor`, ver `backend/src/Synclass.Domain/Usuarios/Usuario.cs` e `PapelUsuario.cs`)
  - `string AccessToken` (criptografado em repouso — ver "Decisão de design" abaixo)
  - `string RefreshToken` (criptografado em repouso — ver "Decisão de design" abaixo)
  - `string CollectorId` (identificador da conta do Professor no Mercado Pago — vindo do `user_id` no payload de troca de `code`)
  - `string? State` (armazenado temporariamente para validar o callback — ver `implementation.md#edge-points`)
  - `DateTime ExpiraEm` (quando `AccessToken` expira — vindo de `expires_in` do Mercado Pago)
  - `DateTime? StateExpiraEm` (janela de validade do `State` pendente — ver `implementation.md#reconexão`; `null` quando não há fluxo OAuth em andamento)
  - `DateTime CriadoEm`, `DateTime AtualizadoEm`
  - Regras de negócio encapsuladas na própria entidade: expiração é por data hora (`Expirado()`), renovação desnecessária se `ExpiraEm` > agora + 5 min (margem de segurança contra race condition na borda).

- `IConexaoMercadoPagoRepository` (interface nova):
  - `Task<ConexaoMercadoPago?> ObterPorProfessorAsync(Guid professorId)`
  - `Task<ConexaoMercadoPago?> ObterPorStateAsync(string state)`
  - `Task AdicionarAsync(ConexaoMercadoPago conexao)`
  - `Task AtualizarAsync(ConexaoMercadoPago conexao)`

- `IClienteOAuthMercadoPago` (interface nova, no Domain — envolve HTTP de saída): ver assinatura definitiva na seção dedicada **"Assinatura do `IClienteOAuthMercadoPago` — contrato"** abaixo (`MontarUrlAutorizacao` é **síncrono**, sem `CancellationToken` — monta uma URL a partir de string, não faz chamada de rede; só `TrocarCodePorTokenAsync`/`RenovarTokenAsync` são assíncronos, porque esses sim chamam a API do Mercado Pago). **Não existe `ObterRedirectUriAsync`** — o `redirectUri` é uma constante fixa (ver "Decisão de design: rota fixa e URL de redirecionamento"), lida via `IConfiguration`/constante no próprio `ConexaoMercadoPagoService`, não obtida de forma assíncrona.

- `ConexaoMercadoPagoService` (service novo, orquestra use cases — siga o padrão de `backend/src/Synclass.Domain/Configuracoes/ConfiguracaoProfessorService.cs`):
  - `Task<string> ConectarAsync(Guid professorId, CancellationToken ct)` — gera `state`, cria/reaproveita registro com `State`/`StateExpiraEm` (ver "Reconexão"), chama `IClienteOAuthMercadoPago.MontarUrlAutorizacao(state, redirectUri)` (síncrono — só monta a string da URL), retorna a URL.
  - `Task ProcessarCallbackAsync(string code, string state, CancellationToken ct)` — **sem `professorId`**: o endpoint de callback é anônimo (ver "Decisão de design: rota fixa e URL de redirecionamento"), não há claim de usuário disponível nesse request. O Professor é resolvido internamente via `IConexaoMercadoPagoRepository.ObterPorStateAsync(state)` — se não encontrar registro com aquele `state` (ou `StateExpiraEm` no passado), lança `StateInvalidoException` antes de qualquer troca de `code`. Só depois de validar o `state` é que troca `code` por token e persiste no mesmo registro encontrado.
  - `Task<string?> ObterCollectorIdAsync(Guid professorId, CancellationToken ct)` — retorna `null` se não conectado/token inválido; renova se expirado.

- `TrocaCodePorTokenResultado` (record novo, no Domain — resultado da troca de `code`): `AccessToken`, `RefreshToken`, `CollectorId` (do `user_id` do payload), `ExpiraEm`.

- Exceções: `StateInvalidoException` (nova, validação de callback/state expirado). Para "Professor não existe", **reutilizar `Synclass.Domain.Usuarios.UsuarioNaoEncontradoException`** (`backend/src/Synclass.Domain/Usuarios/UsuarioNaoEncontradoException.cs:10`) — **não** o `ProfessorNaoEncontradoException` de `Synclass.Domain.Matriculas` (`backend/src/Synclass.Domain/Matriculas/ProfessorNaoEncontradoException.cs:15`), que herda `MatriculaRejeitadaException` e é semanticamente sobre rejeição de matrícula, não sobre pagamentos.

### Infrastructure (novo)

- `ConexaoMercadoPagoConfiguration` (`backend/src/Synclass.Infrastructure/Persistence/Configurations/ConexaoMercadoPagoConfiguration.cs`) — mapeamento Fluent API via `IEntityTypeConfiguration<ConexaoMercadoPago>`, seguindo `ConfiguracaoProfessorConfiguration.cs` como referência (linha 24 mostra o padrão `HasKey`, `Property(...).IsRequired()`).
- `ConexaoMercadoPagoRepository` (`backend/src/Synclass.Infrastructure/Persistence/ConexaoMercadoPagoRepository.cs`) — implementa `IConexaoMercadoPagoRepository` usando `AppDbContext` (injeção via construtor, `RepositoryBase` — ver `backend/src/Synclass.Infrastructure/Persistence/RepositoryBase.cs` e `ConfiguracaoProfessorRepository.cs` como referência de padrão).
- `ClienteOAuthMercadoPago` (`backend/src/Synclass.Infrastructure/Http/ClienteOAuthMercadoPago.cs`) — implementa `IClienteOAuthMercadoPago` com `HttpClient` puro (primeiro wrapper HTTP do repo, sem precedente — ver `implementation.md#integração-http`).

### Api (novo)

- `MercadoPagoController` (`backend/src/Synclass.Api/Controllers/MercadoPagoController.cs`) — endpoints novos, seguindo o padrão thin de `ConfiguracoesController.cs` (linhas 33-65 mostram o padrão de `[Authorize(Roles = "Professor")]`, `[HttpGet]`, `IActionResult`).

## Endpoints novos

### `GET /professores/mercado-pago/conectar` (auth: `[Authorize(Roles = "Professor")]`)

- **Antes** (não existe — sem precedente):
  - (caminho inteiro é novo)
- **Depois** (desejado):
  - `MercadoPagoController.Conectar()`: chama `ConexaoMercadoPagoService.ConectarAsync(professorIdParaOperacao, ct)`, retorna `Ok(new { url })`.
  - **professorId vem da decisão `implementacion.md#autorização-e-vínculo-com-o-usuário-autenticado`** — ver seção dedicada abaixo (depende do desenho escolhido).

### `GET /professores/mercado-pago/callback` (auth: `[AllowAnonymous]` — ver justificativa abaixo)

- **Antes** (não existe — sem precedente):
  - (caminho inteiro é novo)
- **Depois** (desejado):
  - **Rota fixa para o Mercado Pago redirecionar** — ver decisão em `implementation.md#rota-fixa-e-url-de-redirecionamento`. Não pode exigir o token de autenticação do Synclass, pois o redirect vem do navegador do Professor para dentro do fluxo OAuth.
  - `MercadoPagoController.Callback([FromQuery] string code, [FromQuery] string state, CancellationToken ct)`: chama `ConexaoMercadoPagoService.ProcessarCallbackAsync(code, state, ct)` (sem `professorId` — o service resolve o Professor pelo `state`, ver assinatura acima), retorna `Ok` (página simples "conta conectada" em HTML mínimo — sem precedente de renderização de HTML no repo; decisão: retornar `Content("<html>...")` com `text/html` — template literal em C# 12, ver `implementation.md#resposta-do-callback`).
  - Em caso de `StateInvalidoException`: `BadRequest` com mensagem clara.
  - **Este endpoint não leva `[Authorize]`** — é `[AllowAnonymous]` explícito (com comentário no código dizendo por quê, para não ser confundido com endpoint desprotegido por descuido), consistente com "Decisão de design: rota fixa e URL de redirecionamento".

## Modelo de dados

Nova tabela `ConexaoMercadoPago` (migration `20260824001345_CriarConexaoMercadoPago` — nome seguindo padrão de `20260824001318_CriaCodigoEntradaTurma.cs`):

| Coluna | Tipo | Notas |
|---|---|---|
| `Id` | `Guid` | PK |
| `ProfessorId` | `Guid` | FK → `Usuario.Id`, `IsRequired` (o Professor É um Usuário, ver achado 3) |
| `AccessTokenCipherText` | `text` | criptografado — ver `implementation.md#decisão-de-design-criptografia` |
| `RefreshTokenCipherText` | `text` | criptografado — ver `implementation.md#decisão-de-design-criptografia` |
| `CollectorId` | `varchar(32)` | do `user_id` do payload de troca, `IsRequired` |
| `State` | `varchar(64)` | nullable (limpo após uso no callback) |
| `ExpiraEm` | `timestamptz` | UTC |
| `CriadoEm` | `timestamptz` | UTC, `HasDefaultValueSql("now()")` — ver `ConfiguracaoProfessorConfiguration.cs:41` |
| `AtualizadoEm` | `timestamptz` | UTC |

- Relação: N:1 com `Usuario` (Professor). **Não é relação de posse como `Matricula`** — é o Professor (Usuário) que possui a conexão, mas a relação pode ser modelada como FK simples (sem navegação necessário — ver `implementation.md#por-que-sem-navegação`).
- `State` e tokens: sem mapeamento de valor (campos simples no entity, configurados via Fluent API).

## Assinatura do `IClienteOAuthMercadoPago` — contrato

```csharp
public interface IClienteOAuthMercadoPago
{
    string MontarUrlAutorizacao(string state, string redirectUri);
    Task<TrocaCodePorTokenResultado> TrocarCodePorTokenAsync(string code, string redirectUri, CancellationToken ct);
    Task<TrocaCodePorTokenResultado> RenovarTokenAsync(string refreshToken, CancellationToken ct);
}
```

## Padrão de serviço a seguir

- `ConexaoMercadoPagoService` deve seguir o padrão de `ConfiguracaoProfessorService.cs`:
  - Construtor recebe `IConexaoMercadoPagoRepository`, `IUsuarioRepository` (para checar Professor), `IClienteOAuthMercadoPago`, `IClock` (novo — envolve `DateTime.UtcNow`, ver `code-style.md#dependências`).
  - Métodos principais (`ConectarAsync`, `ProcessarCallbackAsync`, `ObterCollectorIdAsync`) com retorno antecipado (`early return`) para errors — ver `code-style.md#estilo-de-código`.
  - Não lança exceção para "sem conexão" — `ObterCollectorIdAsync` retorna `null` (decisão de contrato, ver `implementation.md#contrato-com-a-task-199`).

## Integração HTTP (primeira do repo — sem precedente)

- **Não existe nenhum `HttpClient` wrapper hoje** — grep confirmou (achado 2).
- `ClienteOAuthMercadoPago` em `backend/src/Synclass.Infrastructure/Http/` — primeira pasta `Http` do projeto, **decisão nova: não há precedente**.
- Registro em `Program.cs` via `AddHttpClient` (que **não existe hoje** em `Program.cs` — precisa ser adicionado):
  ```csharp
  builder.Services.AddHttpClient<IClienteOAuthMercadoPago, ClienteOAuthMercadoPago>(client =>
  {
      client.BaseAddress = new Uri("https://api.mercadopago.com");
      client.Timeout = TimeSpan.FromSeconds(30);
  });
  ```
- **Endpoint de autorização** (navegador): `https://auth.mercadopago.com.br/authorization?client_id=...&response_type=code&platform_id=mp&redirect_uri=...&state=...` — construído como string, não via `HttpClient`.
- **Endpoint de token** (posterior, HTTP): `POST /oauth/token` com `grant_type=authorization_code`, `client_id`, `client_secret`, `code`, `redirect_uri`; renovação com `grant_type=refresh_token`. Payload em `application/x-www-form-urlencoded` (padrão OAuth 2.0 do Mercado Pago).
- **Tratamento de erro**: se a resposta não for `2xx` (incluindo 401 — token revogado), a implementação lança `MercadoPagoApiException` (exceção nova no Infrastructure, ver `implementation.md#exceções-novas-em-camada-de-infraestrutura`).
- **Segredos**: `MERCADOPAGO__CLIENT_ID` e `MERCADOPAGO__CLIENT_SECRET` lidos via `IConfiguration["MercadoPago:ClientId"]` / `IConfiguration["MercadoPago:ClientSecret"]` no construtor de `ClienteOAuthMercadoPago` (padrão de leitura de `Program.cs`), com falha explícita no startup se ausentes (mesmo padrão do `Jwt:SigningKey` em `Program.cs`). Adicionar ambos a `.env.example` na raiz (chaves vazias) e repassar no `docker-compose.yml`.

## Reconexão

- `ConectarAsync` é **idempotente por Professor**: busca `ObterPorProfessorAsync` primeiro.
  - Sem registro: cria um novo (`AdicionarAsync`) com `State`/`StateExpiraEm` novos, sem `AccessToken`/`RefreshToken`/`CollectorId` ainda (preenchidos só no callback).
  - Com registro existente (conectado ou com fluxo abandonado): **reaproveita o mesmo registro**, sobrescrevendo `State`/`StateExpiraEm` (`AtualizarAsync`) — não cria segundo registro, não exige "desconectar" antes. Tokens antigos (se havia conexão ativa) permanecem válidos até o novo callback confirmar a troca; se o Professor abandonar o novo fluxo, a conexão antiga continua funcionando normalmente (só o `State` foi sobrescrito, o que invalida qualquer callback pendente do fluxo anterior — comportamento aceito, é o mesmo Professor reiniciando o próprio fluxo).
- Não existe endpoint de "desconectar" nesta Task (fora de escopo do card) — trocar de conta MP é só chamar `/conectar` de novo e completar o novo fluxo.

## Edge points

- `State` temporário: a URL de autorização carrega um `state` aleatório (32 bytes criptográficos, base64url — mesmo padrão do token de convite, ver `backend/src/Synclass.Domain/Convites/Convite.cs`). Ele é armazenado no registro de conexão (coluna `State`) com `StateExpiraEm = agora + 10 minutos` para o callback validar (mesmo Professor, mesmo fluxo, dentro da janela). Após o callback bem-sucedido, `State`/`StateExpiraEm` são limpos (nullable) — não é para reuso. `ProcessarCallbackAsync` rejeita com `StateInvalidoException` tanto `state` que não bate quanto `state` correto porém com `StateExpiraEm` no passado (fluxo abandonado há mais de 10 minutos) — sem isso, um link de autorização vazado/reenviado ficaria válido indefinidamente.
- Renovação de token: **margem de segurança de 5 minutos** antes da expiração real (`ExpiraEm < DateTime.UtcNow.AddMinutes(5)` → renova). Evita corrida de borda (token vencendo entre a checagem e o uso).
- `user_id` do payload de troca vira `CollectorId` (identificador público da conta Mercado Pago do Professor) — usado por #199 no `collector_id` da preferência de checkout.
- Dois Profissionais com mesmo `CollectorId`? **Tratar como válido** — mesma conta pode ser conectada por dois usuários distintos se ambos têm acesso a ela; não fazer merge.

## Dependência de outras Tasks

- **Bloqueia #199** — sem `ConexaoMercadoPago` não há em nome de quem criar checkout. #199 consumirá `ConexaoMercadoPagoService.ObterCollectorIdAsync`.
- **Não depende de nenhuma** — primeira peça do épico #198.

## Decisão de design: criptografia de tokens em repouso

- `AccessToken` e `RefreshToken` são **credenciais de terceiros** — não são chaves nossas, mas abrem o cofre do Professor. Criptografar em repouso seguindo o padrão já existente no repo? **Não existe** precedente de criptografia de dados em repouso no Synclass (dados sensíveis hoje: senha, códigos OTP — todos só transitam, nunca persistidos). **Decisão nova: usar `IDataProtector` do ASP.NET Core** (já disponível, sem dependência nova) — registrar `builder.Services.AddDataProtection()` em `Program.cs` (não existe hoje — conferir), injetar `IDataProtectionProvider` na `ConexaoMercadoPagoConfiguration` ou num ValueConverter de EF Core 8 (`HasConversion` com `EncryptingConverter` — ver `backend/src/Synclass.Infrastructure/Persistence/Configurations/` para onde adicionar). Não criptografar `CollectorId`, `State`, nem timestamps.

## Decisão de design: rota fixa e URL de redirecionamento

- **Rota do callback é fixa**: para o Mercado Pago redirecionar, a `redirect_uri` é sempre `https://api.synclass.com.br/professores/mercado-pago/callback` (âmbito público — ver `implementation.md#endpoints-novos`). Lida via `IConfiguration["MercadoPago:RedirectUri"]` (mesma leitura direta com falha explícita no startup dos demais segredos do Mercado Pago, ver "Integração HTTP" abaixo) — **nunca** obtida de forma assíncrona/dinâmica do `IClienteOAuthMercadoPago` (não existe `ObterRedirectUriAsync`).
- **Conflito com auth**: essa rota é `[AllowAnonymous]` porque o redirect vem do navegador do Professor, que pode não ter token de sessão do Synclass no momento. Segurança: `state` (valor aleatório) é a prova de que quem chamou o callback é o mesmo Professor que iniciou o fluxo.
- **Fluxo**: `ConectarAsync` gera `state`, chama `IClienteOAuthMercadoPago.MontarUrlAutorizacao(state, redirectUri)` → retorna URL para o Professor clicar. Mercado Pago redireciona para `redirect_uri?code=...&state=...` → `ProcessarCallbackAsync` valida `state` contra o `State` persistido (registro existe e `State` bate).

## Decisão de design: autorização e vínculo com o usuário autenticado

- **Problema**: `ConfiguracoesController.cs` (backend/src/Synclass.Api/Controllers/ConfiguracoesController.cs:33-65) usa `professorId` do parâmetro de rota, sem comparar com `User.GetUsuarioId()`. Isso permite um Professor consultar/alterar a configuração de outro.
- **Decisão**: para #203, **usar `User.GetUsuarioId()` como única fonte do `professorId`** (ignorar qualquer `professorId` vindo da rota, porque a rota não tem parâmetro). O endpoint `GET /professores/mercado-pago/conectar` **não recebe `professorId` na rota** — o service recebe `User.GetUsuarioId()` (ClaimTypes.NameIdentifier, ver `backend/src/Synclass.Api/ClaimsPrincipalExtensions.cs`). Isso elimina a ambiguidade rota-vs-claim de forma definitiva para esta feature.
- **Nota**: documentar o débito no `ConfiguracoesController` (uso de rota vinda do parâmetro sem comparar com claim) como débito conhecido em `docs/developers.md` ou diretamente no código (comentário `// TODO(#204)`) — não corrigir nesta Task por escopo.

## Decisão de design: por que sem navegação em `ConexaoMercadoPagoConfiguration`

- A relação N:1 com `Usuario` será mapeada apenas como FK (`HasOne<Usuario>().WithMany().HasForeignKey(c => c.ProfessorId)`), **sem propriedade de navegação** `public Usuario Professor { get; set; }` na entidade `ConexaoMercadoPago`. Motivo: não existe nenhum consumer que precise navegar de `ConexaoMercadoPago` para `Usuario` ou vice-versa — o service sempre parte do `professorId` e busca a conexão. Navegação adicionaria acoplamento desnecessário (ver `code-style.md#estrutura`: módulos pequenos e focados). Se um futuro use case precisar, adiciona-se depois — YAGNI.

## Decisão de design: contrato com a Task #199

- `ObterCollectorIdAsync` **retorna `string?`**, não lança exceção para "sem conexão":
  - `null` sem conexão registrada;
  - `null` se token expirado e renovação falhou (refresh também 401) — a Task #199 deve tratar `null` como "Professor não conectado" e exibir a mensagem clara do critério de aceite 5.
- A Task #199 irá chamar `ObterCollectorIdAsync(professorId)` — **contrato fechado antes de #199 começar** (ver `fluxo-de-feature.md#fase-3--implementação` sobre paralelização com contrato estável).

## Exceções novas em camada de infraestrutura

- `MercadoPagoApiException` (Infrastructure, `backend/src/Synclass.Infrastructure/Http/MercadoPagoApiException.cs`): lançada por `ClienteOAuthMercadoPago` quando o Mercado Pago responde com erro (status `4xx`/`5xx` exceto `401` na renovação — ver fluxo em `implementation.md#renovação-de-token`). O `MercadoPagoController` captura essa exceção e responde com `502 Bad Gateway` (swallow do erro cru, ver `implementation.md#erro-cru-do-mercado-pago`).
- `StateInvalidoException` (Domain, `ConexaoMercadoPago.ProcessarCallbackAsync`): lançada quando `state` não confere.

## Exceção por Professor não encontrado

- Já resolvido acima (ver "Entidades/classes afetadas — Domain"): `ConexaoMercadoPagoService` lança `Synclass.Domain.Usuarios.UsuarioNaoEncontradoException` quando `professorId` não existe em `IUsuarioRepository`. `MercadoPagoController` a captura e responde `404`.

## Erro cru do Mercado Pago não vaza

- `ClienteOAuthMercadoPago` NUNCA loga o corpo da resposta de erro do Mercado Pago se contiver `access_token`/token no payload de erro (defensivo; o Mercado Pago não devolve token em erro, mas não confiar):
  ```csharp
  catch (HttpRequestException ex)
  {
      _logger.LogError(ex, "Falha na chamada ao Mercado Pago para {Endpoint}. Status: {StatusCode}", endpoint, statusCode);
      throw new MercadoPagoApiException($"Falha na chamada ao Mercado Pago: {endpoint}", ex);
  }
  ```
- `MercadoPagoController` na captura de `MercadoPagoApiException`:
  ```csharp
  catch (MercadoPagoApiException)
  {
      return StatusCode(StatusCodes.Status502BadGateway, new { mensagem = "Falha ao comunicar com o Mercado Pago. Tente novamente." });
  }
  ```

## Renovação de token

- `ObterCollectorIdAsync`:
  1. Busca `ConexaoMercadoPago` por `professorId`; se `null`, retorna `null`.
  2. Se `Expirado()` (com margem de 5 min), chama `IClienteOAuthMercadoPago.RenovarTokenAsync(refreshToken)`.
  3. Se renovação OK: atualiza `AccessToken`, `RefreshToken`, `ExpiraEm` no registro, persiste (`AtualizarAsync`), retorna `CollectorId`.
  4. Se renovação falha (401 — token revogado): **apaga a conexão** (`RemoverAsync` — novo método no repositório), loga `ProfessorDesconectouMercadoPago` (porque a conexão morreu), retorna `null` — o Professor verá "não conectado".
- **Por que apagar em vez de manter inválida**: token revogado é irrecuperável; manter registro só acumula lixo e confunde o status ("conectado" sem token válido). Novas tentativas de conectar passam por OAuth limpo.

## Resposta do callback

- Após `ProcessarCallbackAsync` com sucesso, a resposta é uma página HTML mínima (`<!doctype html><html><body><p>Conta do Mercado Pago conectada com sucesso! Você já pode fechar esta aba e voltar ao Synclass.</p></body></html>`) — **sem precedente no repo** (nenhum controller retorna HTML hoje; todos retornam JSON). Decisão: usar `Content()`, template C# 12 (raw string literal), para o Professor ver confirmação no navegador após o redirect. 
- Em caso de `StateInvalidoException`: `BadRequest` com mensagem clara em JSON.
- **Fechar a experiência**: o front conecta a conta enquanto o Professor está autenticado (o `state` liga o fluxo ao Professor). A página de sucesso não depende de sessão — é só a confirmação final; a UI do app já mostra "conta conectada" na próxima consulta.

## Padrões de teste

- `ConexaoMercadoPagoServiceTests.cs` (Domain), seguindo `LoginComGoogleServiceTests.cs`:
  - Nomenclatura `Metodo_Cenario_ResultadoEsperado`.
  - Factory privado `CriarServico(...)` que monta `ConexaoMercadoPagoService` com `FakeConexaoMercadoPagoRepository` e `FakeClienteOAuthMercadoPago` (fakes manuais, SEM Moq — ver `backend/tests/Synclass.Domain.Tests/Fakes/`).
  - Factory privado `CriarConexao(...)` para instanciar `ConexaoMercadoPago` com valores padrão.
  - Cobrir: geração de URL (com `state` e `redirect_uri` corretos); persistência no callback; `StateInvalidoException`; retorno `null` sem conexão; renovação com sucesso e com falha (apaga e retorna `null`); registro de evento `ProfessorConectouMercadoPago`.
- `MercadoPagoControllerTests.cs` (Api) é **opcional** nesta Task (não existe padrão de teste de controller no repo — conferir `backend/tests/`); se criar, seguir o estilo de teste de controller de `ConfiguracoesController` se houver, senão criar com xUnit + `WebApplicationFactory` — **sem precedente, decisão nova**; se for complexo demais, cobrir no teste de fumaça manual/script de smoke (ver `implementation.md#testes-de-fumaça`).

## Testes de fumaça

- Rodar a suíte completa do backend (`dotnet test` a partir de `backend/`) antes do PR.
- Teste manual do fluxo OAuth real **fora do escopo de CI** (requer conta Mercado Pago real) — registrar em `docs/qa/203-conectar-mercado-pago.md` o passo a passo manual.

## Log estruturado

- `ProfessorConectouMercadoPago` (nível informação), com `ProfessorId`, `CollectorId`, `TrackId` — **nunca** `AccessToken`/`RefreshToken` (mesma política de `CodigoOtpSolicitado`, ver `architecture.md#logs-estruturados-e-track-id`).
- `ProfessorDesconectouMercadoPago` (nível informação) no fluxo de revogação (item 4 da renovação).
- `ConexaoMercadoPagoFalhouRenovacao` (nível aviso) quando a renovação falha, sem o payload de erro cru.
- Em `ClienteOAuthMercadoPago`: log de debug com endpoint chamado e status, sem corpo de resposta quando contém dados sensíveis.

## Registro de dependência: `IClock`

- **Correção**: `IClock`/`SystemClock` **já existem no repo** (`backend/src/Synclass.Domain/Common/IClock.cs`, `backend/src/Synclass.Infrastructure/Common/SystemClock.cs`) e já estão registrados em `Program.cs` — **não recrie**. `ConexaoMercadoPagoService` recebe `IClock` no construtor (não `DateTime.UtcNow` direto), do mesmo jeito que qualquer outro Service já existente que precisa de tempo controlável em teste — siga esse padrão já estabelecido, sem criar arquivo novo.
