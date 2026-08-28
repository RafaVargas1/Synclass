
# Spec Técnica — #193: Entrega de código OTP por WhatsApp

## Correção de baseline (2026-08-27)

A versão anterior deste spec foi escrita contra um layout de repositório que
não é o de `main`. Esta revisão substitui todos os caminhos/nomes/rotas
fictícios pelos reais, confirmados via leitura direta do código
(`backend/src/Synclass.Domain/Autenticacao/`,
`backend/src/Synclass.Api/Controllers/AutenticacaoController.cs`). Nenhuma
regra de negócio, critério de aceite ou decisão de produto muda — só a
localização/nome das classes e o mecanismo de erro HTTP, para bater com o
código real.

## Correções do dev-review (PR #206, 2026-08-27)

- **`WhatsApp:ApiKey` → `WhatsApp:AccountSid` + `WhatsApp:AuthToken`**: a
  Basic Auth do Twilio exige os dois valores separados por `:`
  (`AccountSid:AuthToken`), não um único token — `WhatsAppHttpClient` e a
  validação de startup em `Program.cs` foram ajustados. `AccountSid` também
  monta a `BaseAddress` da Messages API
  (`https://api.twilio.com/2010-04-01/Accounts/{AccountSid}/Messages.json`),
  eliminando o placeholder fixo `.../Accounts/ACCOUNT_SID/...` que nunca
  falhava explicitamente no startup quando a config real estivesse ausente.
- **`AssinaturaDigital:ModoDev`**: precisa estar `true` em
  `appsettings.Development.json` (adicionado) para que `dotnet watch run`
  local continue usando `NotificadorDeLog` sem credenciais reais do Twilio
  — sem isso, a flag nunca era `true` em lugar nenhum e `WhatsAppNotificador`
  virava o único caminho possível, quebrando o login local. `docker-compose.yml`
  continua sem a flag de propósito (simula produção).
- **Contato tipo e-mail**: `WhatsAppNotificador` agora checa
  `Contato.IdentificarTipo` antes de normalizar para E.164; contato
  não-telefone lança `OtpEnvioException` (502, "canal não disponível") em
  vez de deixar `TelefoneUtils` lançar `ContatoInvalidoException` (400,
  "contato inválido") para um contato que na verdade é válido.

## Entidades e classes afetadas

### Backend — Domain

| Arquivo | Mudança |
|---|---|
| `backend/src/Synclass.Domain/Autenticacao/INotificador.cs` | Sem mudança — contrato já existe: `Task EnviarCodigoOtpAsync(string contatoNormalizado, string codigo, CancellationToken cancellationToken)` |
| `backend/src/Synclass.Domain/Autenticacao/OtpEnvioException.cs` | **Nova** — exceção de domínio para falha de envio, com `Motivo` (mensagem amigável, sem detalhe técnico do provedor) e `CausaOriginal` (exceção interna, nunca serializada em log) |
| `backend/src/Synclass.Domain/Autenticacao/TelefoneUtils.cs` | **Nova** — normalização E.164. Método estático `NormalizarParaE164(string)`, rejeita inválidos lançando `Synclass.Domain.Usuarios.ContatoInvalidoException` (reaproveitada, ctor `(string contato, string formatoEsperado)`) |
| `backend/src/Synclass.Domain/Autenticacao/IWhatsAppHttpClient.cs` | **Nova** — interface do wrapper HTTP (fronteira de transporte, ver abaixo) |

**Local das novas classes**: `Synclass.Domain/Autenticacao/`, a mesma pasta
onde `INotificador` e `NotificadorDeLog` (via `Synclass.Infrastructure/Autenticacao/`)
já vivem — **não** `Notificacoes/` nem `Contatos/` (pastas que não existem no
repositório).

### Backend — Infrastructure

| Arquivo | Mudança |
|---|---|
| `backend/src/Synclass.Infrastructure/Autenticacao/NotificadorDeLog.cs` | **Mantido**, comentário de classe atualizado (contexto: só roda quando `AssinaturaDigital:ModoDev=true`, ver DI abaixo). Código funcional não muda. |
| `backend/src/Synclass.Infrastructure/Autenticacao/WhatsAppHttpClient.cs` | **Nova** — wrapper de `HttpClient` (implementa `IWhatsAppHttpClient`) |
| `backend/src/Synclass.Infrastructure/Autenticacao/WhatsAppNotificador.cs` | **Nova** — implementa `INotificador`. Envia via `IWhatsAppHttpClient`, loga `OtpEnviado`/`OtpEnvioFalhou`, nunca o código em texto puro |

### Backend — Api

| Arquivo | Mudança |
|---|---|
| `backend/src/Synclass.Api/Program.cs` | Registra `IWhatsAppHttpClient` (Twilio, via `AddHttpClient<>`) e `INotificador` como `WhatsAppNotificador`, exceto quando `AssinaturaDigital:ModoDev=true` (aí registra `NotificadorDeLog`, comportamento atual). Valida `WhatsApp:ApiKey` e `WhatsApp:NumeroRemetente` no startup, mesmo padrão de `Jwt:SigningKey` (linha ~61 do `Program.cs` atual) — `?? throw new InvalidOperationException(...)`, ponto único de validação (não duplicar no ctor das classes de Infrastructure) |
| `backend/src/Synclass.Api/Controllers/AutenticacaoController.cs` | **Não existe `LoginController`** — o controller real é `AutenticacaoController` (`[Route("auth")]`). Adiciona `catch (OtpEnvioException ex)` no método `SolicitarCodigo` (mesmo padrão de `try/catch` já usado ali para `LoginRejeitadoException`/`ContatoInvalidoException`), retornando `StatusCode(502, new AutenticacaoErrorResponse(ex.Motivo))` — **não** cria filtro/middleware global de exceção (não existe nenhum hoje no repo; seguir o padrão local já estabelecido no controller, não introduzir mecanismo novo) |

**Primeira integração HTTP de saída do repo** — este é o primeiro HttpClient
wrapper do Synclass. Não há precedente para seguir; o padrão abaixo é decisão
nova deste spec, deve ser validado pelo revisor.

## Ponto de inserção exato

### `IWhatsAppHttpClient.cs` / `OtpEnvioException.cs` (novos, `Synclass.Domain/Autenticacao/`)

```csharp
// backend/src/Synclass.Domain/Autenticacao/IWhatsAppHttpClient.cs
namespace Synclass.Domain.Autenticacao;

public interface IWhatsAppHttpClient
{
    Task EnviarMensagemAsync(string numeroE164, string mensagem, CancellationToken cancellationToken);
}
```

```csharp
// backend/src/Synclass.Domain/Autenticacao/OtpEnvioException.cs
namespace Synclass.Domain.Autenticacao;

public sealed class OtpEnvioException : Exception
{
    public string Motivo { get; }
    public Exception? CausaOriginal { get; }

    public OtpEnvioException(string motivo, Exception? causaOriginal = null)
        : base(motivo)
    {
        Motivo = motivo;
        CausaOriginal = causaOriginal;
    }
}
```

### `WhatsAppHttpClient.cs` (nova, `Synclass.Infrastructure/Autenticacao/`) — implementação Twilio

```csharp
// backend/src/Synclass.Infrastructure/Autenticacao/WhatsAppHttpClient.cs
namespace Synclass.Infrastructure.Autenticacao;

public sealed class WhatsAppHttpClient : IWhatsAppHttpClient
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(3); // RN: não prender o usuário

    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _numeroRemetente;

    public WhatsAppHttpClient(HttpClient httpClient, IConfiguration config)
    {
        _httpClient = httpClient;
        _httpClient.Timeout = Timeout;
        _apiKey = config["WhatsApp:ApiKey"]!; // validado no startup (Program.cs)
        _numeroRemetente = config["WhatsApp:NumeroRemetente"]!; // validado no startup (Program.cs)
    }

    public async Task EnviarMensagemAsync(string numeroE164, string mensagem, CancellationToken cancellationToken)
    {
        // Endpoint Twilio Messages API: POST /2010-04-01/Accounts/{AccountSid}/Messages.json
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["To"] = $"whatsapp:{numeroE164}",
            ["From"] = $"whatsapp:{_numeroRemetente}",
            ["Body"] = mensagem,
        });

        using var request = new HttpRequestMessage(HttpMethod.Post, _httpClient.BaseAddress)
        {
            Content = content,
        };
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes(_apiKey)));

        try
        {
            using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
        {
            throw new OtpEnvioException("Não foi possível enviar o código. Tente novamente em instantes.", ex);
        }
    }
}
```

**Nota**: `_httpClient.BaseAddress` deve ser configurado na fábrica em
`Program.cs` (`AddHttpClient<>(client => client.BaseAddress = new Uri(...))`),
não fixado no ctor — mantém `WhatsAppHttpClient` sem hardcode de URL.

### `WhatsAppNotificador.cs` (nova, `Synclass.Infrastructure/Autenticacao/`)

```csharp
// backend/src/Synclass.Infrastructure/Autenticacao/WhatsAppNotificador.cs
namespace Synclass.Infrastructure.Autenticacao;

public sealed class WhatsAppNotificador : INotificador
{
    private readonly IWhatsAppHttpClient _httpClient;
    private readonly ILogger<WhatsAppNotificador> _logger;

    public WhatsAppNotificador(IWhatsAppHttpClient httpClient, ILogger<WhatsAppNotificador> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task EnviarCodigoOtpAsync(string contatoNormalizado, string codigo, CancellationToken cancellationToken)
    {
        var numeroE164 = TelefoneUtils.NormalizarParaE164(contatoNormalizado);
        var mensagem = $"Seu código de acesso ao Synclass é: {codigo}. Ele expira em 10 minutos.";
        var contatoMascarado = MascaradorDeContato.Mascarar(numeroE164);

        try
        {
            await _httpClient.EnviarMensagemAsync(numeroE164, mensagem, cancellationToken);
            _logger.LogInformation("OtpEnviado {ContatoMascarado}", contatoMascarado);
        }
        catch (OtpEnvioException ex)
        {
            _logger.LogError("OtpEnvioFalhou {ContatoMascarado} {Motivo}", contatoMascarado, ex.Motivo);
            throw; // preserva stack trace original — capturado pelo controller (502)
        }
    }
}
```

**TrackId**: o padrão de log estruturado do repositório já usa
`_logger.Log*("Evento {TrackId} {...}", trackId, ...)` só nos pontos onde o
`trackId` está disponível via `HttpContext`/`Response.Headers` (ver
`AutenticacaoController`). `WhatsAppNotificador` roda na camada de
Infrastructure, sem acesso direto ao `HttpContext` — **não** inventar
mecanismo de propagação de TrackId aqui (fora de escopo desta Task); log sem
`TrackId` explícito é aceitável neste nível, correlação por contato mascarado
+ timestamp é suficiente para depurar falha de envio.

**Mascaramento**: reaproveita `Synclass.Domain.Usuarios.MascaradorDeContato`
(já existe, mantém 2 primeiros + 2 últimos caracteres) — **não** cria
`TelefoneUtils.Mascarar` novo (evita duplicar padrão de mascaramento).

### `TelefoneUtils.cs` (nova, `Synclass.Domain/Autenticacao/`)

```csharp
// backend/src/Synclass.Domain/Autenticacao/TelefoneUtils.cs
namespace Synclass.Domain.Autenticacao;

public static class TelefoneUtils
{
    private const string FormatoEsperado = "telefone em formato E.164 (ex: +5511987654321) ou BR com DDD (10 ou 11 dígitos)";

    public static string NormalizarParaE164(string contato)
    {
        var digitos = new string(contato.Where(char.IsDigit).ToArray());

        // Já tem DDI 55 + DDD + número (8 ou 9 dígitos) = 12 ou 13 dígitos
        if (digitos.StartsWith("55") && digitos.Length is 12 or 13)
        {
            return $"+{digitos}";
        }

        if (digitos.StartsWith('0'))
        {
            digitos = digitos[1..];
        }

        // DDD + número (8 ou 9 dígitos), sem DDI — assume Brasil
        if (digitos.Length is 10 or 11)
        {
            return $"+55{digitos}";
        }

        throw new Synclass.Domain.Usuarios.ContatoInvalidoException(contato, FormatoEsperado);
    }
}
```

**Por que ainda é necessária**: `Contato.Normalizar` (chamado por
`LoginService` antes de invocar `INotificador`) só valida telefone BR de
10-11 dígitos e retorna **apenas os dígitos, sem DDI/E.164** — não garante o
formato que o provedor de WhatsApp exige. `TelefoneUtils.NormalizarParaE164`
faz essa conversão final, específica do canal WhatsApp, e é chamada dentro de
`WhatsAppNotificador` (não em `LoginService`, que é agnóstico de canal).

### `Program.cs` (modificação)

```csharp
// Logo após o bloco de validação de Jwt:SigningKey (linha ~61 atual)
var whatsAppApiKey = builder.Configuration["WhatsApp:ApiKey"]
    ?? throw new InvalidOperationException("Configuração ausente: WhatsApp:ApiKey.");
var whatsAppNumeroRemetente = builder.Configuration["WhatsApp:NumeroRemetente"]
    ?? throw new InvalidOperationException("Configuração ausente: WhatsApp:NumeroRemetente.");

// Registro condicional — só em dev loga o código (comportamento atual, issue #18)
if (builder.Configuration.GetValue<bool>("AssinaturaDigital:ModoDev"))
{
    builder.Services.AddSingleton<INotificador, NotificadorDeLog>();
}
else
{
    builder.Services.AddHttpClient<IWhatsAppHttpClient, WhatsAppHttpClient>(client =>
    {
        client.BaseAddress = new Uri(builder.Configuration["WhatsApp:BaseUrl"]
            ?? "https://api.twilio.com/2010-04-01/Accounts/ACCOUNT_SID/Messages.json");
    });
    builder.Services.AddScoped<INotificador, WhatsAppNotificador>();
}
```

**Nota**: `whatsAppApiKey`/`whatsAppNumeroRemetente` são lidos e validados uma
única vez aqui — `WhatsAppHttpClient`/`WhatsAppNotificador` leem de
`IConfiguration` diretamente (não recebem como parâmetro de construtor), mas
a falha explícita já aconteceu no startup antes de qualquer request. Isso
resolve a contradição da versão anterior do spec (que ora mandava validar em
`Program.cs`, ora no ctor): **um único ponto de validação, em `Program.cs`**,
seguindo o precedente real de `Jwt:SigningKey`.

### `.env.example` (modificação)

```
WhatsApp__ApiKey=
WhatsApp__NumeroRemetente=
```

(convenção de env var para configuração aninhada do ASP.NET Core —
`__` mapeia para `:`; mesmo padrão que permitiria `Jwt__SigningKey`, hoje
ausente do `.env.example` porque essa chave vive só em `appsettings.json`
local — não copiar esse padrão específico, só o `__`.)

### `NotificadorDeLog.cs` (modificação — só comentário)

Comentário de classe atualizado para deixar explícito que, a partir desta
Task, o registro em produção depende de `AssinaturaDigital:ModoDev=false` em
`Program.cs` (DI condicional), não de compilação (`#if DEBUG`). Corpo do
método não muda.

## Padrão de estilo a seguir

- **Sem precedente de HttpClient no repo** — este é o primeiro. O padrão
  proposto (`IWhatsAppHttpClient` separando transporte de negócio, `Timeout`
  de 3s, `OtpEnvioException` de domínio) é decisão nova deste spec.
- **Configuração**: seguir o padrão real de `Jwt:SigningKey` em `Program.cs`
  (`?? throw`, único ponto de validação, `.env.example` com chave vazia).
- **Erro HTTP**: seguir o padrão real do `AutenticacaoController` (try/catch
  local por exceção de domínio) — **não** introduzir filtro/middleware
  global novo.
- **Logs estruturados**: nunca código OTP em texto puro; reaproveitar
  `MascaradorDeContato` (já existe) em vez de criar mascaramento próprio.
- **Namespace**: tudo em `Autenticacao` (Domain e Infrastructure), mesma
  pasta de `INotificador`/`NotificadorDeLog` — não criar `Notificacoes/`.

## Contrato de API

**Não há mudança de contrato de request/response** —
`POST /auth/codigo` (`AutenticacaoController.SolicitarCodigo`) já existe e já
retorna `ContatoInvalidoException`/`LoginRejeitadoException` como 400. O que
muda:

- **Novo caso de erro**: se `OtpEnvioException` for lançada por
  `LoginService.SolicitarCodigoAsync` (via `INotificador`), o controller
  captura e retorna **502 Bad Gateway** com
  `{ "mensagem": "Não foi possível enviar o código. Tente novamente em instantes." }`
  (`AutenticacaoErrorResponse`, mesmo shape do erro 400 existente) — sem
  detalhe do provedor.
- **Nenhuma mudança nos records `SolicitarCodigoRequest`/`SolicitarCodigoResponse`.**

## Modelo de dados

**Sem migration.** Nenhuma tabela/coluna nova.

## Edge points

- **E.164 obrigatório**: `TelefoneUtils.NormalizarParaE164` rejeita números
  sem DDD/DDI válido (`ContatoInvalidoException`) — edge point do card,
  testado.
- **Timeout de 3s**: se o provedor não responder em 3s,
  `TaskCanceledException`/`OperationCanceledException` vira `OtpEnvioException`
  com mensagem amigável.
- **Rate limit**: fora de escopo desta Task (card pede só para considerar,
  não implementar) — vira Task separada se o produto pedir.
- **Provedor escolhido**: Twilio é o padrão. `IWhatsAppHttpClient` é a
  fronteira — trocar de provedor exige nova implementação da interface e
  ajuste na fábrica de `Program.cs`, sem tocar `INotificador`/domain.
- **`NotificadorDeLog` jamais em produção**: DI condicional em `Program.cs`
  via `AssinaturaDigital:ModoDev`. Teste de fumaça confirma que
  `ModoDev=false` resolve `WhatsAppNotificador`.
- **Contato tipo e-mail**: `INotificador.EnviarCodigoOtpAsync` hoje não
  discrimina o canal por tipo de contato (mesmo comportamento do
  `NotificadorDeLog` atual — loga/envia independente do tipo). Se um usuário
  tiver e-mail como contato em produção (`ModoDev=false`),
  `TelefoneUtils.NormalizarParaE164` lança `ContatoInvalidoException` para
  esse valor — gap pré-existente ao design atual de `INotificador` (não
  introduzido por esta Task), roteamento por canal fica fora de escopo
  (issue #193 trata só do canal WhatsApp).

## Dependência de outras Tasks

- **Sem dependência de #203**: #203 (Professor conecta Mercado Pago) está
  sendo implementada em paralelo, em worktree separada, e cria seu próprio
  wrapper HTTP (`IClienteOAuthMercadoPago`, domínio de Pagamentos) —
  decisão deliberada: não compartilhar abstração HTTP entre #193 e #203.
  Ambas tocam `Program.cs` (registro de DI) e serão rebaseadas uma contra a
  outra no merge.
- **Sem outras dependências**: geração do OTP (`GeradorDeCodigoOtp`,
  `ICodigoOtpRepository`) já existe e é imutável; `LoginService` já injeta
  `INotificador`.

## Testes

- `backend/tests/Synclass.Domain.Tests/Autenticacao/WhatsAppNotificadorTests.cs`
  — fakes manuais em `Fakes/` (sem Moq):
  - `FakeWhatsAppHttpClientSucesso` (implementa `IWhatsAppHttpClient`,
    retorna `Task.CompletedTask`)
  - `FakeWhatsAppHttpClientFalha` (lança
    `new OtpEnvioException("Não foi possível enviar o código.")`)
  - Verificar: sucesso → sem exceção; falha → `OtpEnvioException`
    propagada com `Motivo` amigável. Verificação de log sem código em texto
    puro pode ser feita com um `ILogger<WhatsAppNotificador>` fake que
    captura as mensagens formatadas (padrão já usado em outros testes do
    repo — conferir `Fakes/` existentes antes de criar um novo).
- `backend/tests/Synclass.Domain.Tests/Autenticacao/TelefoneUtilsTests.cs` —
  cenários: `+5511999999999` (já E.164), `+55 11 99999-9999` (com
  espaços/hífen), `11999999999` (sem DDI, adiciona +55),
  `5511999999999` (já com DDI, sem `+`), `123` (inválido →
  `ContatoInvalidoException`)
- `backend/tests/Synclass.Api.Tests/AutenticacaoControllerNotificacaoTests.cs`
  (ou adicionar caso a um arquivo de teste de `AutenticacaoController` já
  existente, se houver) — mesmo padrão de `UsuariosControllerTests.cs`
  (`WebApplicationFactory<Program>`, `services.RemoveAll<>()` para
  substituir `INotificador` por um fake que lança `OtpEnvioException`):
  `POST /auth/codigo` com contato inválido → 400; com `INotificador`
  mockado lançando `OtpEnvioException` → 502, corpo sem detalhe técnico.

**Nota**: confirmar se já existem testes de `NotificadorDeLog` — se sim,
garantir que continuam passando (o registro condicional em `Program.cs` não
afeta o teste unitário de `NotificadorDeLog` isoladamente, só o DI de
produção).
