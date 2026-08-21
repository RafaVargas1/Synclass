# Implementação: Rate limiting nos endpoints anônimos de convite (#89)

## Entidades/classes afetadas

- `backend/src/Synclass.Api/Program.cs` (Api): registro de
  `AddRateLimiter`/`UseRateLimiter` e leitura obrigatória da configuração de
  limite, mesmo padrão fail-fast já usado para `Jwt:ExpiracaoDias` e
  `Convites:DiasValidade` (`LerExpiracaoDiasObrigatoria`,
  `LerDiasValidadeConviteObrigatoria`) — adicionar uma função equivalente
  (ex: `LerConfiguracaoRateLimitConvitesObrigatoria`) em vez de aceitar
  `GetValue<int>` silenciosamente devolvendo 0.
- `backend/src/Synclass.Api/Controllers/ConvitesController.cs` (Api): novo
  atributo `[EnableRateLimiting("ConvitesAnonimos")]` em `Aceitar` (token) e
  `AceitarPorCodigo` (código) — os dois `[AllowAnonymous]` da issue. `Gerar`
  (autenticado, `[Authorize(Roles = "Professor")]`) fica fora do escopo.
- `backend/src/Synclass.Api/appsettings.json`: nova seção `RateLimiting`.
- Nenhuma mudança em `Domain`/`Infrastructure`/migration — é
  infraestrutura pura de `Api` (middleware nativo do ASP.NET Core, incluso
  no SDK `Microsoft.NET.Sdk.Web` já usado pelo projeto, sem pacote NuGet
  novo).

## Contrato de API

Sem mudança nos contratos de sucesso já existentes (200 de `Aceitar`/
`AceitarPorCodigo`, 400 de rejeição de negócio). Novo caso possível:

- `429 Too Many Requests`, corpo `ConviteErrorResponse` (mesmo tipo já
  usado pelas rejeições 400) com mensagem genérica ("Muitas tentativas.
  Tente novamente em instantes.") — não diferenciar o motivo por endpoint,
  para não abrir um oráculo novo sobre o próprio rate limit.
- Header de resposta `Retry-After` (segundos restantes da janela atual).

## Modelo de dados

Nenhum. O estado do limiter (contagem por partição/janela) vive em memória
do processo ASP.NET Core (mecanismo nativo do
`Microsoft.AspNetCore.RateLimiting`) — não há tabela/coluna nova.

## Decisão de abordagem (issue deixa em aberto — decisão técnica registrada aqui)

A issue lista `Microsoft.AspNetCore.RateLimiting`, limite por IP e CAPTCHA
como opções e deixa a escolha em aberto para "discussão de produto/
segurança". Como esta é uma Task técnica sem produto no loop agora, a
decisão abaixo segue bom senso de infraestrutura, não uma escolha de
produto:

- **Biblioteca**: `Microsoft.AspNetCore.RateLimiting` (nativo do ASP.NET
  Core 7+, já disponível no SDK atual do projeto) — descarta CAPTCHA
  (exigiria integração de terceiro e mudança de UX no fluxo de aceite,
  fora do escopo desta Task de infraestrutura de backend).
- **Algoritmo**: fixed window (`RateLimiterOptionsExtensions.AddFixedWindowLimiter`)
  — mais simples de configurar e testar que sliding window/token bucket;
  suficiente para o objetivo real (frear enumeração automatizada, não
  eliminar completamente força bruta).
- **Partição**: por IP do cliente
  (`RateLimitPartition.GetFixedWindowLimiter` com chave =
  `httpContext.Connection.RemoteIpAddress?.ToString() ?? "desconhecido"`).
- **Limite**: 5 requisições por 60 segundos por IP, aplicado igualmente aos
  dois endpoints de aceite (`token` e `codigo`). Raciocínio: o keyspace do
  código curto é 100k valores (issue #62); a 5 tentativas/minuto por IP,
  enumerar o espaço inteiro por um único IP levaria dias — inviabiliza
  automação simples sem impedir um Aluno legítimo de errar o código
  algumas vezes seguidas. Número não vem de nenhum requisito de produto
  explícito — se o mantenedor quiser afinar depois, é mudança de
  configuração (`appsettings.json`), não de código.
- **`QueueLimit = 0`**: rejeita imediatamente com 429 em vez de enfileirar
  — não faz sentido enfileirar uma tentativa de aceite de convite.
- **Config obrigatória**: `RateLimiting:ConvitesAnonimos:PermissoesPorJanela`
  (int, default de exemplo em `appsettings.json`: `5`) e
  `RateLimiting:ConvitesAnonimos:JanelaEmSegundos` (int, default `60`) —
  lidas com o mesmo padrão fail-fast já estabelecido (lança
  `InvalidOperationException` explícita se ausente/inválida no startup, não
  silenciosamente 0).

## Edge points

- **Atrás de proxy/load balancer sem `X-Forwarded-For` configurado**: todos
  os clientes apareceriam com o mesmo IP (o do proxy) e o limite ficaria
  efetivamente compartilhado entre usuários legítimos. Não há proxy
  configurado hoje em dev/CI — fora do escopo resolver isso agora; se
  identificado como problema real em produção, é uma Task nova
  (`ForwardedHeadersMiddleware`), não parte desta.
- **Estado em memória, por instância do processo**: reinicia a cada
  deploy/restart e não é compartilhado entre múltiplas instâncias (sem
  Redis/store distribuído). Aceitável dado que o projeto não roda múltiplas
  instâncias em produção hoje — citar como próximo passo natural se isso
  mudar.
- **Testes de fumaça e estado compartilhado do limiter**: o contador de uma
  partição/IP persiste durante toda a vida de uma instância de
  `WebApplicationFactory`. Use uma classe de teste dedicada (ex:
  `ConvitesRateLimitEndpointTests`), com uma instância de
  `WebApplicationFactory` isolada por `[Fact]` (não compartilhada via
  `IClassFixture` entre os testes desta classe) — do contrário, um teste
  anterior que já bateu o limite faz o próximo teste falhar por motivo
  errado (contagem vazada, não o comportamento sendo testado).
- **`Gerar` (`POST /professores/{professorId}/convites`) fica fora do
  escopo**: já exige `[Authorize(Roles = "Professor")]`; o oráculo de força
  bruta descrito na issue é específico dos dois endpoints anônimos de
  aceite (`ConviteInvalidoException` vs. `ConviteContatoDivergenteException`
  como sinal distinguível).
- **Log de rejeição não inclui IP nem payload da requisição** — só
  `TrackId` e a rota — consistente com `security-rules.md` (payload bruto
  de requisição não deveria ir para log estruturado).

## Dependência de outras Tasks

Nenhuma.
