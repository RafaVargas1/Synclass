
# Spec técnica — Task #200: Endpoint de webhook de confirmação de pagamento

## Contexto

Esta spec é a **fonte de verdade técnica** para a implementação. O card #200 define a regra de negócio de alto nível — ler este documento é OBRIGATÓRIO antes de codar. A ordem de execução no `task.md` é a ordem dos commits.

## Entidades/classes afetadas

### Backend

| Arquivo | Camada | Ação |
|---|---|---|
| `backend/src/Synclass.Domain/Pagamentos/Pagamento.cs` | Domain | **Modificar**: adicionar método `Estornar(IClock)` — muda de `Confirmado` para `Estornado` |
| `backend/src/Synclass.Domain/Pagamentos/Pagamento.cs` | Domain | **Modificar**: adicionar propriedade `EventoId` (string?) — identificador do evento do webhook que confirmou o pagamento |
| `backend/src/Synclass.Infrastructure/Persistence/...PagamentoRepository.cs` (nome exato depende do layout EF) | Infrastructure | **Modificar**: implementar `ObterPorIdAsync` e `AtualizarAsync` |
| `backend/src/Synclass.Domain/Pagamentos/IPagamentoRepository.cs` | Domain | **Modificar**: adicionar assinaturas `Task<Pagamento?> ObterPorIdAsync(Guid id, CancellationToken ct)` e `Task AtualizarAsync(Pagamento pagamento, CancellationToken ct)` |
| `backend/src/Synclass.Domain/Pagamentos/WebhookMercadoPagoService.cs` | Domain | **Criar**: serviço de domínio — verificação de assinatura, callback de busca do pagamento, idempotência, estorno |
| `backend/src/Synclass.Domain/Pagamentos/WebhookPayloadMercadoPago.cs` | Domain | **Criar**: DTO do payload do webhook (`data.id` / `id`) — este DTO NÃO contém dados do pagamento, só o ID para buscar |
| `backend/src/Synclass.Api/Controllers/PagamentosWebhookController.cs` | Api | **Criar**: controller `[AllowAnonymous]` com verificação própria — a rota DEVE ser exatamente `POST /webhooks/mercado-pago` (a `notification_url` já enviada pela #199, não pode mudar) |
| `backend/src/Synclass.Infrastructure/Checkout/WebhookMercadoPagoClient.cs` | Infrastructure | **Criar**: se a busca do pagamento for encapsulada — PODE ser substituída por um método no `GeradorDeCheckoutMercadoPago` existente; **não crie camada de infra separada se o `GeradorDeCheckoutMercadoPago` já tiver o HttpClient configurado - reutilize o MESMO `HttpClient` via `IHttpClientFactory` com nome do Mercado Pago** |

### Frontend

Nenhum arquivo frontend é tocado nesta Task — o endpoint é consumido exclusivamente pelo Mercado Pago.

## Entidade `Pagamento` — estados e transições

Extrato da classe atual (NÃO altere estes métodos):

```
Confirmar(IClock clock)  // idempotente: se já Confirmado/Pago, não faz nada
Falhar(IClock clock)     // idempotente: idem
```

Alterações:
- **`EventoId`** (string?): o `data.id` do webhook do Mercado Pago do ÚLTIMO evento processado com sucesso pra este `Pagamento` (não só o primeiro). Guardado para idempotência — a checagem é: se o `Pagamento` correspondente já tem `EventoId == data.id` do evento atual, NÃO processa de novo (já tratado). **É sobrescrito a cada evento novo processado** (não `??=`/nula-coalescente) — se travasse no primeiro valor, um evento *seguinte* diferente do primeiro (ex: `refunded` depois de `approved`) processaria certo na primeira vez, mas uma reentrega desse MESMO evento de estorno não seria detectada como duplicata (o `EventoId` ainda apontaria pro evento de confirmação, nunca atualizado) — reprocessaria/logaria de novo. `EventoId` precisa sempre refletir o último evento tratado, não o primeiro.
- **`Estornar(IClock clock)`**: muda de `Confirmado` para `Estornado` (nome do estado — ver abaixo). **Decisão: só age quando `Status == Confirmado`; nos demais estados (`Pendente`, `Falhou`, já `Estornado`), é no-op** — a regra de negócio é "o valor volta a aparecer como devido", que só faz sentido se ele tinha sido de fato confirmado antes.
- **Estado novo `Estornado`** (não `Devido` — esse nome confundiria o *estado do pagamento* com o *conceito de "valor devido"* que já existe em `ConsultaCobrancaService`/`ValorDevidoPorMatricula`, coisas diferentes). **Nenhuma query muda por causa desse estado novo**: `BuscarPendentePorMatriculaEPeriodoAsync` continua buscando só `Pendente` — um `Pagamento` `Estornado` NÃO deve ser reaproveitado pelo fluxo de "pendente existente" de `PagamentoService.IniciarAsync` (#199), porque isso devolveria ao Aluno a `UrlCheckout` de um checkout já usado/estornado. Um novo pagamento depois de um estorno é um `Pagamento` novo, com checkout novo — o requisito do card ("valor volta a aparecer como devido") já é satisfeito sozinho: `ValorDevidoService.DescontarPagamentosConfirmadosAsync` (#199) só desconta pagamentos `Confirmado`; assim que o status sai de `Confirmado`, o valor natural do `ConsultaCobrancaService` volta a aparecer, sem nenhuma mudança de query adicional.

## Fluxo completo do endpoint (sequência)

Cenário feliz (depois de implementado):

1. `PagamentosWebhookController.ReceberWebhookAsync` recebe `POST /webhooks/mercado-pago`.

   **Antes (nada — código novo):**

2. Extrai `data.id` (payload) + headers `x-signature` e `x-request-id`.

   **Antes (nada — código novo):**

3. `WebhookMercadoPagoService.VerificarAssinaturaAsync` valida HMAC-SHA256 (ver abaixo). **Atenção: `VerificarAssinaturaAsync` deve receber o payload BRUTO como string e os headers como argumentos — assinatura é calculada sobre o payload EXATO recebido, não sobre JSON re-serializado.** Defina a assinatura do método assim:

   ```csharp
   Task<bool> VerificarAssinaturaAsync(string payloadJson, string xSignatureHeader, string xRequestIdHeader, CancellationToken ct)
   ```

   **Depois (dentro do método):**

   ```csharp
   // TODO(webhook-signature): confirmar formato exato do manifest na doc oficial do Mercado Pago.
   // Implementado com base em documentação pública parcial; se a doc oficial divergir, ajustar AQUI e em implementation.md.
   // Formato documentado atualmente (VRI): manifest = $"id:{dataId};request-id:{requestId};ts:{timestamp};"
   // onde dataId extraído do JSON, requestId do header, timestamp do x-signature.
   ```

   O serviço extrai `ts`, `v1` do header (formato `ts=<timestamp>,v1=<hash>`), monta o manifest `id:{data.id};request-id:{xRequestId};ts:{ts};`, calcula HMAC-SHA256 com a chave `MercadoPago:WebhookSecret`, compara em tempo constante (comparador de string slow-equals — `CryptographicOperations.FixedTimeEquals` para o array de bytes do HMAC, ver código de referência em `backend/src/Synclass.Infrastructure/Checkout/` se aplicável).

4. Se inválida (ou ausente/malformada): `AssinaturaInvalidaException` → controller mapeia para HTTP 400 (ver contrato) + log `WebhookPagamentoRejeitado`. **Interromper o fluxo — NÃO buscar pagamento.**

5. **Fetch do Mercado Pago API** (ANTES de buscar o `Pagamento` local — é aqui que se descobre o `external_reference`, não há como buscar por ele antes disso): `GET https://api.mercadopago.com/v1/payments/{paymentId}` — o `paymentId` é o `data.id`. Este retorno traz `status` e `external_reference` (= nosso `Pagamento.Id`). **Use o MESMO `HttpClient`/Bearer token já configurado para o GeradorDeCheckoutMercadoPago** — adicione um método `ObterPagamentoAsync(string paymentId, CancellationToken ct)` na classe existente (ou novo cliente dedicado, MAS reutilizando a mesma configuração de `AddHttpClient`, `MercadoPago:AccessToken`).

   **Antes** (`backend/src/Synclass.Infrastructure/Checkout/GeradorDeCheckoutMercadoPago.cs` — construtor real, confira no arquivo antes de editar): construtor recebe `HttpClient httpClient, IConfiguration configuration, ILogger<GeradorDeCheckoutMercadoPago> logger`; o `HttpClient` já tem `BaseAddress`/`Authorization: Bearer` configurados via `AddHttpClient` em `Program.cs:236` (o token de acesso NÃO é lido de novo dentro da classe, já vem pronto no `HttpClient` injetado — confirme isso lendo o construtor real antes de escrever `ObterPagamentoAsync`, para reaproveitar exatamente o mesmo campo/padrão de autenticação, não reinventar).

   **Depois (adicionar método na mesma classe):**

   ```csharp
   public async Task<PagamentoMercadoPagoDto?> ObterPagamentoAsync(string paymentId, CancellationToken ct)
   {
       using var request = new HttpRequestMessage(HttpMethod.Get, $"/v1/payments/{Uri.EscapeDataString(paymentId)}");
       using var resposta = await _httpClient.SendAsync(request, ct);
       if (!resposta.IsSuccessStatusCode)
       {
           return null; // 404/401 etc — sem pagamento utilizável, controller trata como "não encontrado"
       }
       var corpo = await resposta.Content.ReadAsStringAsync(ct);
       return JsonSerializer.Deserialize<PagamentoMercadoPagoDto>(corpo);
   }
   ```
   Sem `request.Headers.Authorization` explícito aqui — o `HttpClient` injetado já carrega o Bearer token (mesmo padrão do resto da classe, ver `EndpointCheckoutPreferences`/`CriarPreferenciaAsync`).

   `PagamentoMercadoPagoDto` (novo, em `Synclass.Domain/Pagamentos/`): propriedades `Id` (string), `Status` (string, valores `approved`/`rejected`/`pending`/`refunded`), `ExternalReference` (string? é o nosso `Pagamento.Id`).

6. `WebhookMercadoPagoService.ProcessarEventoAsync` busca `Pagamento` local por `Id` (= `dto.ExternalReference`, obtido no passo 5 — só agora esse valor existe) via `IPagamentoRepository.ObterPorIdAsync`.
   - Se `Pagamento == null` (webhook para pagamento que não existe/foi apagado): **ignora sem erro** — log de aviso `PagamentoNaoEncontrado` (sem payload), controller responde 200 OK (não é erro do MP, não faz sentido pedir reenvio de algo que nunca vai resolver).
   - **SE `Pagamento.EventoId == dto.Id` (mesmo evento já processado antes)**: retorna sem ação — nem chama `Confirmar`/`Estornar`, nem `AtualizarAsync`, nem loga de novo. Essa checagem acontece **antes** de aplicar qualquer transição, exatamente pra garantir que o log e a persistência só aconteçam na primeira vez que esse evento específico é visto.
   - **Status `approved`**: `Pagamento.Confirmar(clock)` (idempotente na entidade também) + `Pagamento.EventoId = dto.Id` (sobrescreve sempre — ver "Entidade `Pagamento`" acima).
   - **Status `refunded` ou `rejected`** (rejeitado DEPOIS de pago = estorno): se `Pagamento.Status == Confirmado`, chama `Pagamento.Estornar(clock)` + `Pagamento.EventoId = dto.Id`; se não estava `Confirmado`, no-op (mas ainda assim atualiza `EventoId`, pra reentrega desse mesmo evento não reprocessar).
   - **Outros status** (`pending`, `in_process`, etc.): no-op total, inclusive sem atualizar `EventoId` — não houve efeito nenhum a proteger contra reentrega; o Pagamento permanece `Pendente`, o MP reenviará se o status mudar de verdade depois.

7. `AtualizarAsync(pagamento)` persiste as mudanças (só chamado quando houve transição real ou atualização de `EventoId`, ver passo 6).

8. Log `WebhookPagamentoRecebido` (para `approved`) ou `PagamentoEstornado` (para estorno efetivo) — só na primeira vez que o evento é visto (guard do passo 6, comparando `EventoId` **antes** de aplicar qualquer transição).

## Modelo de dados

- Tabela `Pagamentos`, coluna `EventoId` (string?, nula — pagamentos pendentes ainda não têm evento associado). Migration nova: `AlterarPagamentosAdicionarEventoId` (ou similar padrão das migrations existentes).
- Nenhuma tabela nova.
- **`EventoId` é a chave de idempotência**, não o próprio status. A justificativa (`implementation.md` seção de modelo de dados): dois eventos distintos (`approved` e `refunded`) referenciam o MESMO `Pagamento.Id` — checar só status não basta; como o README aponta ("dois eventos diferentes podem se referir ao mesmo pagamento (confirmação e depois estorno)"), precisamos do identificador específico do evento.

## Contrato de API

### Endpoint: `POST /webhooks/mercado-pago`

- **Rota fixa**: `{MercadoPago:ApiBaseUrlPublica}/webhooks/mercado-pago` — JÁ EM PRODUÇÃO como `notification_url` (enviada pela #199) — **NÃO ALTERAR**.
- **`[AllowAnonymous]` + verificação própria** — é um endpoint público AUTENTICADO via HMAC, não aberto. **Incluir comentário no controller explicando que o `[AllowAnonymous]` é INTENCIONAL** — a proteção é a assinatura HMAC verificada no serviço de domínio, NÃO autenticação de usuário. Sem esse comentário, um futuro refactor pode "corrigir" para `[Authorize]` e quebrar o webhook silenciosamente.
- **Entrada**: o Mercado Pago envia como:
  - Formato atual (IPN v2): query params — `?type=payment&data.id={paymentId}`. O ENDPOINT recebe `?type=payment&data.id={id}` — a controller deve LER o `data.id` da query string. O corretor retorna `200` sem body para confirmar o recebimento (o MP não exige body). Payload JSON também pode vir, mas não confie nele — a fonte de verdade do ID é a query string.
  - Formato legado (`?topic=payment&id={id}`): **NÃO SUPORTAR** (decisão da #200: somente o formato atual é garantido pela nossa aplicação).
- **Saída**:
  - **200 OK**: assinatura válida, evento aceito (mesmo que no-op — MP agradece o ACK). **Sem body.**
  - **400 Bad Request**: assinatura inválida/ausente/malformada → `AssinaturaInvalidaException`, mapeada para 400. NÃO retornar 401/403 (evita confusão com auth de usuário — é assinatura, não sessão, mas 400 genérico é aceitável por simplicidade de handler). Não importa o detalhe do erro, o MP não precisa fraturar por ele — se 400 em vez de 200, o MP reintentará (não é desejável, mas não é crítico).
  - **500 Internal Server Error**: exceções inesperadas (falha de infra, JSON inesperado). Empacotar como genérico — o MP tratará como falha transitória e reintentará.
- **Headers**: `x-signature` (obrigatório), `x-request-id` (obrigatório para o manifest HMAC).
- **CORS**: não aplicável — chamadas vêm do Mercado Pago (servidor para servidor), não de browser.

## Padrão de estilo a seguir

### Verificação de assinatura (dívida técnica documentada)

**NÃO INVENTE O FORMATO DO MANIFEST SE NÃO TIVER CERTEZA.** O que temos é:
- Header `x-signature: ts=<timestamp>,v1=<hmac-sha256-hex>` onde o hash é HMAC-SHA256 (chave `MercadoPago:WebhookSecret`).
- O manifest assinado inclui `id`, `request-id` e `ts` — MAS A ORDEM E O FORMATO EXATO (separadores, aspas?) NÃO FORAM CONFIRMADOS na documentação oficial desta sessão.
- **Decisão**: implemente seguindo o formato `id:{data.id};request-id:{xRequestId};ts:{ts};` (formato de manifest que aparece na documentação pública do Mercado Pago v1) e adicione `TODO(webhook-signature)` no código. O teste da unidade será escrito contra ESSE formato — se a doc oficial divergir, o teste vai falhar e a spec atualizada. **A ação de implementação é: escrever o teste + implementação usando o formato descrito, com flag in-code de que precisa confirmação formal.**

### Mensagens de exceção (ver doc `code-style.md`)

- `AssinaturaInvalidaException`: `throw new InvalidOperationException("Assinatura de webhook inválida ou ausente (x-signature ausente ou malformatada).")` — NÃO incluir detalhes do payload para não logar dado sensível. Preferir `InvalidOperationException` a exceção customizada (XUnit/C# moderno não exige exceção dedicada para caso de validação; se a maioria das exceções do repo é customizada, siga o padrão — verificar em `backend/src/Synclass.Domain/` o que é mais comum).

### Idempotência

- **Não duplicar lógica de idempotência entre serviço e entidade.** A entidade já tem idempotência no `Confirmar`/`Falhar`/`Estornar` (não muda de estado se já terminal). O serviço adiciona a camada `EventoId`, que é uma guarda **anterior e mais forte**: se o `Pagamento` já tem `EventoId == data.id` do evento atual, o serviço retorna **antes** de chamar qualquer método do domínio, antes de `AtualizarAsync`, antes de logar — nenhum dos três acontece de novo pra um evento já visto.
- **Teste de idempotência:** o mesmo evento (mesmo `data.id`) processado duas vezes deve resultar em: `AtualizarAsync` chamado só na **primeira** vez, log `WebhookPagamentoRecebido` postado só na **primeira** vez, `EventoId` igual nas duas (não muda na segunda porque nem chega a reprocessar).

### Padrão de HttpCliente

`ObterPagamentoAsync` é um método novo na MESMA classe `GeradorDeCheckoutMercadoPago` (não uma classe nova) — o registro em `Program.cs` já existe (`AddHttpClient<IGeradorDeCheckout, GeradorDeCheckoutMercadoPago>`, tipado, não nomeado — confirmado em `Program.cs:236`), então não precisa de nenhum registro novo, só adicionar o método na classe existente (e, se fizer sentido pro contrato, expor `ObterPagamentoAsync` na própria `IGeradorDeCheckout` ou numa interface nova só se `IGeradorDeCheckout` não for o lugar certo — decisão de nomenclatura na hora de implementar, mas a classe concreta é a mesma).

### Logs estruturados (ver `architecture.md#logs-estruturados-e-track-id`)

Eventos novos:
- `WebhookPagamentoRecebido` com `TrackId`, `dataId` (`data.id` do webhook), `PagamentoId` (nosso `Id`), `StatusPagamento` (`approved`/`refunded`).
- `WebhookPagamentoRejeitado` com `TrackId`, `dataId`, motivo (nunca payload bruto).
- `PagamentoEstornado` com `TrackId`, `PagamentoId`, `Valor`.
- `PagamentoNaoEncontrado` (warning) com `TrackId`, `dataId` — para webhook órfão.
- Nunca logar `x-signature` completo (segredo!) — apenas prefixo de hash se correlacionar (padrão de `security-requirements.md`).

## Edge points não cobertos por critério de aceite Gherkin

1. **Webhook para pagamento inexistente** (Mercado Pago notifica mas nosso `Pagamento` não existe): retorna 200 OK para interromper retries desnecessários; loga warning `PagamentoNaoEncontrado`. Alternativa 404 forçaria retries. **(Decisão: 200 OK é o correto. É edge point, a regra não manda explicitamente, mas 404 geraria retries que não resolveriam nada.)**
2. **`data.id` ausente ou query string sem `data.id`**: não é assinatura inválida (assinatura pode ser válida se o header vier), mas o evento é inutilizável → retornar 400 (não 200 — queremos que o MP não reenvie um evento que nunca vai funcionar). Considerar `AssinaturaInvalidaException` para SIMPLIFICAção — decisão de implementação: **retornar 400 com log `WebhookPagamentoRejeitado` (motivo: `data.id` ausente)** — reutiliza o mesmo caminho de erro.
3. **JSON do webhook malformado**: o corpo pode vir em branco (query string é a fonte do ID). Se o payload JSON não parsear (mas `data.id` veio na query), usa `data.id` da query; o corpo JSON é ignorado. Se NEM query NEM corpo têm `data.id`, → caso (2).
4. **Falha na chamada de volta ao Mercado Pago** (rede/500): não confirmar nem estornar nada — deixar `Pagamento` como está; retornar 500 para o MP (que reenviará) e logar `WebhookPagamentoRejeitado` com motivo `GET v1/payments falhou`. NUNCA falhar a confirmação por transient network e manter como `Pendente` eterno — o retry do MP resolve.
5. **Concorrência**: dois MESMOS eventos chegam simultaneamente (processos paralelos) — se o `GET v1/payments` for feito duas vezes e os dois chegarem ao `Confirmar` antes de qualquer um persistir, a idempotência da ENTIDADE (`Confirmar` no-op se já `Confirmado`) evita duplicar o efeito de negócio; o pior caso é `AtualizarAsync` sendo chamado duas vezes com o mesmo resultado final (`EventoId` = o mesmo `data.id` nos dois) — sem efeito colateral incorreto, só uma escrita redundante. Não precisa de lock otimista adicional nesta Task; se a concorrência real se mostrar um problema (dois eventos DIFERENTES processados fora de ordem), é um edge point pra revisar depois com dado de produção, não uma decisão a inventar aqui sem evidência.
6. **Modo marketplace (ADR-0005)**: o checkout foi criado em nome da conta conectada do Professor (#203). O `external_reference` dos pagamentos da conta conectada aponta para o MESMO `Pagamento.Id` do banco. O acesso ao token é o mesmo `MercadoPago:AccessToken`? **NÃO — em modo marketplace, o token é o `ACCESS_TOKEN` do Professor (conta conectada), não o token mestre da aplicação.** O fluxo do webhook busca `v1/payments/{id}` — este endpoint exige o token DONO do pagamento. **Decisão: para a Task #200, usar o `MercadoPago:AccessToken` mestre para o GET — se falhar (401), logar `WebhookPagamentoRejeitado` com motivo `NaoAutorizadoMarketplace` e retornar 500 (o MP reenviará).** A integração completa do marketplace (usar o token do Professor por `collector_id`) é escopo da Task #203/#204. **Documentar explicitamente aqui para não "resolver" errado dentro desta Task.**

## Dependência de outras Tasks

- **#199** (entidade `Pagamento`, `PagamentoService.IniciarAsync`, `PagamentosController`, `GeradorDeCheckoutMercadoPago`): JÁ MERGEADA — esta Task CONSTRÓI sobre ela. Assumir a existência descrita no Contexto.
- **#203** (conta Mercado Pago conectada, ADR-0005): afeta o roteamento em modo marketplace (edge point 6) — mas esta Task implementa o padrão genérico com o token mestre, a integração com a conta conectada é posterior.
- **#198** (épico): pai.
