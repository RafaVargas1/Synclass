
# Spec Técnica — #193: Entrega de código OTP por WhatsApp

## Entidades e classes afetadas

### Backend — Domain

| Arquivo | Mudança |
|---|---|
| `backend/src/Synclass.Domain/Notificacoes/INotificador.cs` | Sem mudança estrutural — contrato já existe (`EnviarCodigoAsync`), usado por `NotificadorDeLog` |
| `backend/src/Synclass.Domain/Notificacoes/OtpEnvioException.cs` | **Nova** — exceção de domínio para falha de envio, com `Motivo` (mensagem amigável, sem detalhe técnico do provedor) e `CausaOriginal` (exceção interna, nunca serializada em log) |
| `backend/src/Synclass.Domain/Notificacoes/TelefoneUtils.cs` | **Nova** — normalização E.164 (`+[DDI][DDD][número]`, sem espaços/hífens). Método estático `NormalizarParaE164(string)`, rejeita inválidos com `ContatoInvalidoException` (mesma exceção de `Contato.Normalizar`) |

### Backend — Infrastructure

| Arquivo | Mudança |
|---|---|
| `backend/src/Synclass.Infrastructure/Autenticacao/NotificadorDeLog.cs` | **Mantido**, mas condicionado: só é registrado quando `AssinaturaDigital:ModoDev=true` (ver padrão abaixo). Loga `OtpEnviadoParaDesenvolvimento` **apenas** nesse contexto |
| `backend/src/Synclass.Infrastructure/Notificacoes/WhatsAppHttpClient.cs` | **Nova** — wrapper de `HttpClient`. Interface `IWhatsAppHttpClient` (em Domain, `backend/src/Synclass.Domain/Notificacoes/IWhatsAppHttpClient.cs`), implementação na Infrastructure |
| `backend/src/Synclass.Infrastructure/Notificacoes/WhatsAppNotificador.cs` | **Nova** — implementa `INotificador`. Envia via `IWhatsAppHttpClient`, loga `OtpEnviado`/`OtpEnvioFalhou`, nunca o código em texto puro |

### Backend — Api

| Arquivo | Mudança |
|---|---|
| `backend/src/Synclass.Api/Program.cs` | **Resolve DI**: registra `IWhatsAppHttpClient` (Twilio) e `INotificador` como `WhatsAppNotificador`; valida `WhatsApp:ApiKey` e `WhatsApp:NumeroRemetente` no startup (padrão `Jwt:SigningKey`); registra `NotificadorDeLog` **apenas** se `AssinaturaDigital:ModoDev=true` (usa `IConfiguration`, não `#if DEBUG` — configuração em `.env`, não compilação) |
| `backend/src/Synclass.Api/Controllers/LoginController.cs` | Sem mudança de código — `INotificador` já é injetado e usado; só muda a **implementação concreta** resolvida pelo DI |

**Primeira integração HTTP de saída do repo** — este é o primeiro HttpClient wrapper do Synclass. Não há precedente para seguir; o padrão abaixo é decisão nova deste spec, deve ser validado pelo revisor.

## Ponto de inserção exato

### `WhatsAppHttpClient.cs` (nova) — contrato

```csharp
// backend/src/Synclass.Domain/Notificacoes/IWhatsAppHttpClient.cs
public interface IWhatsAppHttpClient
{
    Task EnviarMensagemAsync(string numeroE164, string mensagem, CancellationToken ct);
}

// backend/src/Synclass.Domain/Notificacoes/OtpEnvioException.cs
public class OtpEnvioException : Exception
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

### `WhatsAppHttpClient.cs` (nova) — implementação Twilio

```csharp
// backend/src/Synclass.Infrastructure/Notificacoes/WhatsAppHttpClient.cs
public sealed class WhatsAppHttpClient : IWhatsAppHttpClient
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _numeroRemetente;
    private readonly ILogger<WhatsAppHttpClient> _logger;
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(3); // RN: não prender o usuário

    public WhatsAppHttpClient(HttpClient httpClient, IConfiguration config, ILogger<WhatsAppHttpClient> logger)
    {
        _httpClient = httpClient;
        _apiKey = config["WhatsApp:ApiKey"] ?? throw new InvalidOperationException("WhatsApp:ApiKey é obrigatória. Configure em .env.");
        _numeroRemetente = config["WhatsApp:NumeroRemetente"] ?? throw new InvalidOperationException("WhatsApp:NumeroRemetente é obrigatório. Configure em .env.");
        _logger = logger;
    }

    public async Task EnviarMensagemAsync(string numeroE164, string mensagem, CancellationToken ct)
    {
        // Endpoint Twilio Messages API
        // https://api.twilio.com/2010-04-01/Accounts/{AccountSid}/Messages.json
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["To"] = $"whatsapp:{numeroE164}",
            ["From"] = $"whatsapp:{_numeroRemetente}",
            ["Body"] = mensagem
        });

        using var request = new HttpRequestMessage(HttpMethod.Post, _httpClient.BaseAddress!.ToString())
        {
            Content = content
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes(_apiKey)));

        try
        {
            using var response = await _httpClient.SendAsync(request, ct).ConfigureAwait(false);
            response.EnsureSuccessStatusCode(); // lança HttpRequestException em 4xx/5xx
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
        {
            throw new OtpEnvioException("Não foi possível enviar o código. Tente novamente em instantes.", ex);
        }
    }
}
```

### `WhatsAppNotificador.cs` (nova)

```csharp
// backend/src/Synclass.Infrastructure/Notificacoes/WhatsAppNotificador.cs
public sealed class WhatsAppNotificador : INotificador
{
    private readonly IWhatsAppHttpClient _httpClient;
    private readonly ILogger<WhatsAppNotificador> _logger;

    public WhatsAppNotificador(IWhatsAppHttpClient httpClient, ILogger<WhatsAppNotificador> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task EnviarCodigoAsync(string codigo, string contato, CancellationToken ct)
    {
        var numeroE164 = TelefoneUtils.NormalizarParaE164(contato);
        var mensagem = $"Seu código de acesso ao Synclass é: {codigo}. Ele expira em 10 minutos.";

        try
        {
            await _httpClient.EnviarMensagemAsync(numeroE164, mensagem, ct);
            _logger.LogInformation("Evento OtpEnviado: Destino={DestinoMascarado}, DuracaoMs={DuracaoMs}, TrackId={TrackId}",
                TelefoneUtils.Mascarar(numeroE164), /* duração */ 0, /* TrackId do contexto */ string.Empty);
            // TrackId vem do ILogger<T> já instrumentado — ver architecture.md#logs-estruturados-e-track-id
        }
        catch (OtpEnvioException ex)
        {
            _logger.LogError("Evento OtpEnvioFalhou: Destino={DestinoMascarado}, Motivo={Motivo}, TrackId={TrackId}",
                TelefoneUtils.Mascarar(numeroE164), ex.Motivo, string.Empty);
            throw; // preserva stack trace original
        }
    }
}
```

**Nota**: `TrackId={TrackId}` — o `ILogger<T>` já é instrumentado no `Program.cs` com o track id do request (ver `architecture.md`); estes placeholders são preenchidos pelo enricher, não pelo caller. Mantenha no log o placeholder; não invente um novo mecanismo de track id.

### `TelefoneUtils.cs` (nova)

```csharp
// backend/src/Synclass.Domain/Notificacoes/TelefoneUtils.cs
public static class TelefoneUtils
{
    public static string NormalizarParaE164(string contato)
    {
        // Remove espaços, hífens, parênteses
        var limpo = new string(contato.Where(char.IsDigit).ToArray());

        // Se não tem DDI (+55 por ser Brasil), adiciona — edge point do card
        if (limpo.StartsWith("55") && limpo.Length == 12) // 55 + DDD + 8 dígitos
            return $"+{limpo}";
        if (limpo.StartsWith("0")) // 0 + DDD + número
            limpo = limpo[1..];
        if (limpo.Length == 10 || limpo.Length == 11) // DDD + 8 ou 9 dígitos
            return $"+55{limpo}";
        if (limpo.Length == 12 && limpo.StartsWith("55"))
            return $"+{limpo}";

        throw new ContatoInvalidoException("Número de WhatsApp deve estar em formato E.164 (ex: +5511999999999).");
    }

    public static string Mascarar(string numeroE164)
    {
        // +5511999999999 → +55******9999
        var ultimos4 = numeroE164[^4..];
        var ddi = numeroE164.StartsWith("+") ? numeroE164[..3] : string.Empty;
        return $"{ddi}******{ultimos4}";
    }
}
```

**Flag**: `ContatoInvalidoException` já existe em `backend/src/Synclass.Domain/Contatos/` — reutilize, não crie exceção nova se a semântica for a mesma.

### `Program.cs` (modificação)

```csharp
// Após o bloco de validação de Jwt:SigningKey
var webhookKey = builder.Configuration["WhatsApp:ApiKey"]
    ?? throw new InvalidOperationException("WhatsApp:ApiKey é obrigatória. Configure em .env (ver .env.example).");
_ = webhookKey; // usado pela factory do WhatsAppHttpClient

// Registro condicional — só em dev loga o código (comportamento atual)
if (builder.Configuration.GetValue<bool>("AssinaturaDigital:ModoDev"))
    builder.Services.AddSingleton<INotificador, NotificadorDeLog>();
else
    builder.Services.AddSingleton<INotificador, WhatsAppNotificador>();

// Factory para o HttpClient (timeout curto — RN do card)
builder.Services.AddHttpClient<IWhatsAppHttpClient, WhatsAppHttpClient>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(3);
    // BaseAddress configurado na factory ou via IConfiguration no ctor
});
```

**Nota**: a validação de `WhatsApp:NumeroRemetente` acontece no ctor do `WhatsAppNotificador` (via `IConfiguration`) — mantém o padrão de falha explícita no startup, mas no ponto de uso, não em `Program.cs` (evita duplicar validação em dois lugares).

### `NotificadorDeLog.cs` (modificação)

```csharp
// backend/src/Synclass.Infrastructure/Autenticacao/NotificadorDeLog.cs
// Contexto: implementação de DESENVOLVIMENTO — loga o código DE PROPÓSITO (ver issue #18).
// NUNCA deve ser registrado em produção: só via AssinaturaDigital:ModoDev=true em Program.cs.
public sealed class NotificadorDeLog : INotificador
{
    // ... código atual mantido (loga OtpEnviadoParaDesenvolvimento com o código)
}
```

**Mudança**: adicionar comentário de classe atualizado (não remover o existente — ver `code-style.md#comentários`); o código em si não muda, o DI de `Program.cs` é que decide onde ele roda.

## Padrão de estilo a seguir

- **Sem precedente de HttpClient no repo** — este é o primeiro. O padrão proposto (`IWhatsAppHttpClient` separando transporte de negócio, `Timeout` de 3s, `OtpEnvioException` de domínio) é decisão nova deste spec. Não há arquivo anterior para copiar.
- **Configuração**: seguir o padrão de `Jwt:SigningKey` em `Program.cs` (validação com `?? throw`, `.env.example` com chave vazia na raiz).
- **Contato/E.164**: conferir se `Contato.Normalizar` já normaliza para E.164 (edge point do card). Se sim, `TelefoneUtils` pode ser desnecessário ou apenas um adapter — **não duplique lógica existente**; o spec assume que `Contato.Normalizar` NÃO garante E.164 (retorna contato normalizado, mas não valida DDI), então `TelefoneUtils` é necessário — mas confira no código real no primeiro commit.
- **Logs estruturados**: seguir `code-style.md#logging` — JSON estruturado com `TrackId`, nunca código OTP em texto puro (ver `security-requirements.md#dados-sensíveis-e-pii-em-log`).
- **Mascaramento**: `TelefoneUtils.Mascarar` — mesmo padrão de mascaramento já usado para e-mail em `Contato.Mascarar` (se existir; senão, é o novo padrão).

## Contrato de API

**Não há mudança de contrato** — `POST /api/login/solicitar-codigo` já existe e já retorna `ContatoInvalidoException` (400) quando o contato é inválido. O que muda:

- **Novo caso de erro**: `POST /api/login/solicitar-codigo` → se `OtpEnvioException` for lançada do service, o `ExceptionFilter` global retorna `502 Bad Gateway` com `{ "message": "Não foi possível enviar o código. Tente novamente em instantes." }` — **sem** detalhe do provedor (ex: "Twilio retornou 401") no corpo da resposta.
- **Nenhuma mudança no DTO de entrada/saída** (`SolicitarCodigoRequest`/`SolicitarCodigoResponse`).

## Modelo de dados

**Sem migration.** Nenhuma tabela/coluna nova. Dependência de provedor externo (Twilio) é configuração em `.env`, não entidade de banco.

## Edge points

- **E.164 obrigatório**: `TelefoneUtils.NormalizarParaE164` rejeita números sem DDI válido (`ContatoInvalidoException`) — edge point do card, testado.
- **Timeout de 3s**: se o provedor não responder em 3s, `TaskCanceledException` é capturada e vira `OtpEnvioException` com mensagem amigável — não deixa o usuário esperando. Decisão: síncrono com timeout curto (aceito no card: "síncrono com timeout curto vs. fila assíncrona é decisão de implementação").
- **Rate limit**: card pede para considerar, não implementar. Decisão desta spec: **não implementar no escopo** — `OtpEnvioException` já cobre o fluxo, e rate limit de reenvio por contato vira Task separada se o produto pedir. O edge point fica documentado aqui.
- **Provedor escolhido**: Twilio é o padrão desta implementação. `IWhatsAppHttpClient` é a fronteira — trocar para WhatsApp Business Cloud API exige nova implementação da interface e fábrica no DI, sem tocar `INotificador`/domain.
- **`NotificadorDeLog` jamais em produção**: DI condicional em `Program.cs` (não compilação). Teste de fumaça deve confirmar que a chave `AssinaturaDigital:ModoDev=false` registra `WhatsAppNotificador`.
- **`OtpEnviadoParaDesenvolvimento`**: continua existindo no `NotificadorDeLog`, mas só logado quando `ModoDev=true` — eventos `OtpEnviado`/`OtpEnvioFalhou` são os novos eventos estruturais para o fluxo real.

## Dependência de outras Tasks

- **Sem dependência de #203**: #203 (Professor conecta Mercado Pago) está sendo implementada em paralelo, em worktree separada, e cria seu próprio wrapper HTTP (`ClienteOAuthMercadoPago`, domínio de Pagamentos) — decisão deliberada: **não compartilhar abstração HTTP entre #193 e #203**, cada uma cria seu wrapper fino independente (`IWhatsAppHttpClient` aqui, `IClienteOAuthMercadoPago` lá). Coordenar uma abstração comum agora exigiria travar uma das duas Tasks esperando a outra definir o contrato primeiro, o que anula o ganho de rodar as duas em paralelo — se um wrapper HTTP genérico fizer sentido depois que as duas existirem, é uma Task de refatoração futura, fora do escopo daqui. Ambas tocam `Program.cs` (registro de DI) e serão rebaseadas uma contra a outra no merge, não contra código compartilhado.
- **Sem outras dependências**: geração do OTP (`GeradorDeCodigoOtp`, `ICodigoOtpRepository`) já existe e é imutável; fluxo de login (`LoginController`) já injeta `INotificador`.

## Testes

- `backend/tests/Synclass.Domain.Tests/Notificacoes/WhatsAppNotificadorTests.cs` — fakes manuais em `Fakes/` (sem Moq):
  - `FakeWhatsAppHttpClientSucesso` (implements `IWhatsAppHttpClient`, retorna `Task.CompletedTask`)
  - `FakeWhatsAppHttpClientFalha` (lança `OtpEnvioException("Não foi possível enviar o código.")`)
  - Verificar: sucesso → sem exceção, log `OtpEnviado` sem código; falha → `OtpEnvioException` com `Motivo` amigável, log `OtpEnvioFalhou` sem código
- `backend/tests/Synclass.Domain.Tests/Notificacoes/TelefoneUtilsTests.cs` — cenários: `+5511999999999` (já E.164), `+55 11 99999-9999` (com espaços/hífen), `11999999999` (sem DDI, adiciona +55), `5511999999999` (já com DDI), `123` (inválido → `ContatoInvalidoException`)
- Testes de fumaça via `LoginControllerTests` (se existirem): mock de `INotificador` falhando → 502 com mensagem amigável, sem detalhe do provedor

**Nota**: conferir se `NotificadorDeLog` já é testado — se sim, garantir que os testes ajustados para o registro condicional (`ModoDev=true` vs `false`) continuem passando sem quebrar a suíte existente.
