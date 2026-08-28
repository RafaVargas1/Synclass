
# Desenho técnico — Pagamento de valor devido via Mercado Pago (Checkout Pro)

## Entidades/classes afetadas

### Novas (Domain, namespace `Synclass.Domain.Pagamentos`)

| Tipo | Caminho do arquivo | Descrição |
|---|---|---|
| Enum `StatusPagamento` | `backend/src/Synclass.Domain/Pagamentos/StatusPagamento.cs` | `Pendente`, `Confirmado`, `Falhou` (string via `ToString()` para storage, nunca `int` seguro por nome) |
| Entidade `Pagamento` | `backend/src/Synclass.Domain/Pagamentos/Pagamento.cs` | Ver abaixo |
| Record `ResultadoInicioPagamento` | `backend/src/Synclass.Domain/Pagamentos/ResultadoInicioPagamento.cs` | `PagamentoId`, `UrlCheckout`, `Valor` |
| Interface `IPagamentoRepository` | `backend/src/Synclass.Domain/Pagamentos/IPagamentoRepository.cs` | CRUD + `BuscarPendentePorMatriculaEPeriodoAsync` |
| Interface `IGeradorDeCheckout` | `backend/src/Synclass.Domain/Pagamentos/IGeradorDeCheckout.cs` | Contrato de criação de preferência no MP |
| Classe `PagamentoService` | `backend/src/Synclass.Domain/Pagamentos/PagamentoService.cs` | Lógica de `IniciarAsync` |
| Classe `ValorDevidoService` | `backend/src/Synclass.Domain/Pagamentos/ValorDevidoService.cs` | Passo de desconto de pagamentos confirmados |

### Modificadas (Domain, namespace `Synclass.Domain.Cobrancas`)

- **`ConsultaCobrancaService`** (`backend/src/Synclass.Domain/Cobrancas/ConsultaCobrancaService.cs`) — **NÃO alterar a lógica de cálculo.** O desconto de pagamentos confirmados é feito por `ValorDevidoService`, chamado a partir do nível da `Api` (controller/serviço do Aluno).

### Novas (Infrastructure)

| Tipo | Caminho do arquivo | Descrição |
|---|---|---|
| `GeradorDeCheckoutMercadoPago` | `backend/src/Synclass.Infrastructure/Checkout/GeradorDeCheckoutMercadoPago.cs` | Implementa `IGeradorDeCheckout` via `HttpClient` puro |
| `PagamentoRepository` | `backend/src/Synclass.Infrastructure/Persistence/Repositories/PagamentoRepository.cs` | Implementa `IPagamentoRepository` via EF Core |

### Modificadas (Infrastructure)

- **`Persistence/Configurations/PagamentoConfiguration.cs`** (novo arquivo) — Fluent API para a entidade `Pagamento`.
- **`Persistence/SynclassDbContext.cs`** — adicionar `DbSet<Pagamento>`.

### Modificadas (Api)

- **`Controllers/AlunoController.cs`** (ou serviço correspondente) — endpoint novo `POST /alunos/matriculas/{matriculaId:guid}/pagamentos` + desconto na leitura de `GET /alunos/valor-devido`.
- **`Program.cs`** — registrar `PagamentoService`, `GeradorDeCheckoutMercadoPago` (via `AddHttpClient`).

## Entidade `Pagamento`

**`Id` é passado pelo chamador, não gerado no construtor**: `PagamentoService.IniciarAsync` precisa do `Id` do pagamento *antes* de chamar `IGeradorDeCheckout.CriarPreferenciaAsync` (o `Id` vira o `external_reference` do payload de checkout — é assim que o webhook de #200 vai casar a notificação do Mercado Pago com o `Pagamento` certo). Se o construtor gerasse o `Id` internamente, não haveria como conhecê-lo antes de criar a preferência — ordem quebrada. Por isso `IniciarAsync` chama `Guid.NewGuid()` primeiro, usa esse valor no payload de checkout, e só then constrói o `Pagamento` com esse mesmo `Id` (ver `implementation.md#fluxo-de-pagamentoserviceiniciarasync`).

```csharp
public sealed class Pagamento
{
    public Guid Id { get; private set; }
    public Guid MatriculaId { get; private set; }
    public Guid AlunoUsuarioId { get; private set; }
    public Guid ProfessorId { get; private set; }
    public decimal Valor { get; private set; } // congelado na criação
    public DateOnly PeriodoInicio { get; private set; }
    public DateOnly PeriodoFimExclusivo { get; private set; }
    public StatusPagamento Status { get; private set; }
    public string? ReferenciaExterna { get; private set; } // id da preferência no MP
    public string UrlCheckout { get; private set; } // salva para reaproveitar pendente
    public DateTime CriadoEm { get; private set; }
    public DateTime? ConfirmadoEm { get; private set; }
    public DateTime? FalhouEm { get; private set; }

    private Pagamento() { } // EF Core

    public Pagamento(Guid id, Guid matriculaId, Guid alunoUsuarioId, Guid professorId, decimal valor, DateOnly inicio, DateOnly fimExclusivo, string urlCheckout, string referenciaExterna)
    {
        if (valor <= 0) throw new ArgumentException($"Valor deve ser maior que zero: {valor}.", nameof(valor));
        Id = id;
        MatriculaId = matriculaId;
        AlunoUsuarioId = alunoUsuarioId;
        ProfessorId = professorId;
        Valor = valor;
        PeriodoInicio = inicio;
        PeriodoFimExclusivo = fimExclusivo;
        Status = StatusPagamento.Pendente;
        UrlCheckout = urlCheckout;
        ReferenciaExterna = referenciaExterna;
        CriadoEm = DateTime.UtcNow;
    }

    public void Confirmar()
    {
        if (Status is StatusPagamento.Confirmado) return; // idempotente
        Status = StatusPagamento.Confirmado;
        ConfirmadoEm = DateTime.UtcNow;
    }

    public void Falhar()
    {
        if (Status is StatusPagamento.Falhou or StatusPagamento.Confirmado) return; // idempotente
        Status = StatusPagamento.Falhou;
        FalhouEm = DateTime.UtcNow;
    }
}
```

> Nota: `ReferenciaExterna` e `UrlCheckout` são gravados na criação. `Confirmar()`/`Falhar()` são idempotentes — chamadas duplicadas (ex: webhook reentrante de #200) não mudam estado já terminal.

## Contrato de API

### Novos endpoints

**`POST /alunos/matriculas/{matriculaId:guid}/pagamentos`** (roles: `Aluno`)

- **Request body** (mesmo shape de período de `GET /alunos/valor-devido`):
  ```json
  { "inicio": "2024-01-01", "fimExclusivo": "2024-02-01" }
  ```
- **201 Created**:
  ```json
  { "pagamentoId": "guid", "urlCheckout": "https://checkout.mercadopago.com/...", "valor": 120.00 }
  ```
- **400 Bad Request**, dois `tipo` distintos (mesma mensagem já pensada pro usuário, não reutilize o mesmo `tipo` pras duas causas — são erros diferentes):
  - `{ "tipo": "sem-valor-devido", "mensagem": "Não há valor devido para esta matrícula no período selecionado." }`
  - `{ "tipo": "professor-sem-conta-conectada", "mensagem": "Professor ainda não conectou uma conta para receber pagamentos." }`
  - Falha de model binding/validação (DTO inválido): resposta padrão do `[ApiController]` (400, sem precedente de 422 no repo — grep confirmou).
- **404 NotFound**: Matrícula não pertence ao Aluno do token (`MatriculaNaoPertenceAoAlunoException`, nova) — mesmo padrão de `MarcacoesHorarioController.cs:49-51` (`AlunoNaoVinculadoAoProfessorException` → `NotFound()`), não usa 403 (o repo não distingue "não é seu" de "não existe" pra não vazar existência do recurso).

### Endpoint modificado

**`GET /alunos/valor-devido`** — mantém contrato de resposta existente (lista de `ValorDevidoPorMatricula`). Após obter a lista, `ValorDevidoService.DescontarPagamentosConfirmadosAsync` remove entradas com pagamento `Confirmado` para o mesmo `(MatriculaId, Inicio, FimExclusivo)`.

## Fluxo de `PagamentoService.IniciarAsync`

```
IniciarAsync(Guid matriculaId, Guid alunoUsuarioId, DateOnly inicio, DateOnly fim, CancellationToken ct)
  ├─ 1. periodo = PeriodoConsulta.Criar(inicio, fim) — valida inicio < fim (PeriodoConsultaInvalidoException se não)
  ├─ 2. Buscar Matricula por id (via IMatriculaRepository — se sumiu, MatriculaNaoPertenceAoAlunoException)
  ├─ 3. Validar que Matricula.AlunoUsuarioId == alunoUsuarioId (senão: MatriculaNaoPertenceAoAlunoException — mesma exceção do passo 2, controller mapeia as duas pra 404 igual)
  ├─ 4. Resolver collector_id do Professor: ConexaoMercadoPagoService.ObterCollectorIdAsync(Matricula.ProfessorId, ct)
  │     └─ null → ProfessorSemContaConectadaException("Professor ainda não conectou uma conta para receber pagamentos")
  ├─ 5. Recalcular valor devido: ConsultaCobrancaService.ConsultarPorAlunoAsync(alunoUsuarioId, periodo, ct)
  │     └─ matricula sem valor > 0 na lista devolvida → SemValorDevidoException("Não há valor devido...")
  ├─ 6. Buscar pendente: IPagamentoRepository.BuscarPendentePorMatriculaEPeriodoAsync(matriculaId, periodo.Inicio, periodo.FimExclusivo, ct)
  │     └─ achou → retorna ResultadoInicioPagamento(PagamentoId, UrlCheckout existente, Valor) — NÃO chama IGeradorDeCheckout de novo
  ├─ 7. pagamentoId = Guid.NewGuid() — gerado ANTES da preferência, porque vira o external_reference do payload (ver "Entidade Pagamento" acima)
  ├─ 8. Criar preferência: IGeradorDeCheckout.CriarPreferenciaAsync(professorId: Matricula.ProfessorId, collectorId, valor, descricao, externalReference: pagamentoId.ToString(), ct)
  ├─ 9. Persistir novo Pagamento(pagamentoId, ...) — valor congelado + urlCheckout + referenciaExterna vindos do retorno do passo 8
  ├─ 10. Log estruturado "PagamentoIniciado" (valores, sem dado de cartão)
  └─ 11. Retorna ResultadoInicioPagamento
```

## `IGeradorDeCheckout`

```csharp
public interface IGeradorDeCheckout
{
    Task<ResultadoCheckout> CriarPreferenciaAsync(
        Guid professorId,
        string collectorId,
        decimal valor,
        string descricao,
        string externalReference,
        CancellationToken ct);
}

public sealed record ResultadoCheckout(string UrlCheckout, string ReferenciaExterna);
```

- **Implementação**: `GeradorDeCheckoutMercadoPago` — `HttpClient` apontando para `https://api.mercadopago.com`, `POST /checkout/preferences`.
- **Autenticação**: `Authorization: Bearer {AccessToken da aplicação MP}` (lido de `IConfiguration["MercadoPago:AccessToken"]` — mesmo token da aplicação que criou o OAuth em #203, **não** o token OAuth do Professor).
- **Payload** (campos confirmados na doc oficial do Mercado Pago Checkout Pro/Marketplace — `collector_id` identifica o vendedor recebedor; consulte a doc atual do MP se o formato mudar, não invente campo novo sem checar):
  ```json
  {
    "items": [{ "title": "Aula particular — {periodo}", "quantity": 1, "unit_price": {valor}, "currency_id": "BRL" }],
    "back_urls": { "success": "{MercadoPago:UrlBaseApp}/aluno/pagamento/confirmado", "failure": "{MercadoPago:UrlBaseApp}/aluno/pagamento/falhou", "pending": "{MercadoPago:UrlBaseApp}/aluno/pagamento/pendente" },
    "auto_return": "approved",
    "external_reference": "{pagamentoId}",
    "notification_url": "{MercadoPago:ApiBaseUrlPublica}/webhooks/mercado-pago",
    "payer": { "email": "{email do Aluno}" },
    "collector_id": "{collectorId do Professor}"
  }
  ```
  - **`back_urls` usa uma URL web real, não deep link**: o app ainda não é nativo (o épico #194 — app nativo — é deliberadamente o último do roteiro). `MercadoPago:UrlBaseApp` é uma env var nova (`.env.example`, vazia no exemplo) apontando para onde o frontend web roda; as rotas `/aluno/pagamento/{confirmado,falhou,pendente}` **não existem ainda no frontend** — criar como telas mínimas (só uma mensagem de status + link de volta pro app) como parte desta Task, seguindo o mesmo padrão de tela simples já usado no repo (`Paragraph` + `TopbarAutenticada`).
  - **`notification_url` é a rota que #200 vai implementar** — ainda não existe. `MercadoPago:ApiBaseUrlPublica` é env var nova (URL pública da Api, ex: `https://api.synclass.com.br` em produção, `.env.example` vazia) + o caminho fixo `/webhooks/mercado-pago`. **#200 precisa implementar o endpoint exatamente nesse caminho** — deixe isso registrado na "Dependência de outras Tasks" abaixo, para não haver mismatch de rota quando #200 for implementada.
  - Sem campo `"marketplace"` no payload — não é um campo real do Checkout Pro/Marketplace (removido do rascunho original, que citava um valor `"NONE"` inventado sem confirmação); `collector_id` sozinho já direciona o pagamento à conta do Professor.
- **Tratamento de resposta**: sucesso → parse de `init_point` (ou `sandbox_init_point` em ambiente de teste) e `id` da preferência; erro → log estruturado + exception `FalhaAoCriarCheckoutException` (novo).

## Configuração e segredos

- **`appsettings` / `.env.example`** (raiz): adicionar `MERCADOPAGO_ACCESS_TOKEN`, `MERCADOPAGO_URL_BASE_APP`, `MERCADOPAGO_API_BASE_URL_PUBLICA` (todas vazias no exemplo). Reutilizar `MERCADOPAGO_CLIENT_ID`/`MERCADOPAGO_CLIENT_SECRET` já existentes (de #203) para o OAuth de conexão — o `AccessToken` daqui é diferente, é o token da própria aplicação Mercado Pago (não do Professor), usado pra criar a preferência de checkout.
- **`Program.cs`**: registrar com `AddHttpClient` (mesmo padrão de `ClienteOAuthMercadoPago.cs:17`):
  ```csharp
  services.AddHttpClient<IGeradorDeCheckout, GeradorDeCheckoutMercadoPago>((sp, httpClient) =>
  {
      httpClient.BaseAddress = new Uri(sp.GetRequiredService<IConfiguration>()["MercadoPago:ApiBaseUrl"] ?? "https://api.mercadopago.com");
      httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", sp.GetRequiredService<IConfiguration>()["MercadoPago:AccessToken"]);
  });
  ```

## Desconto de pagamentos confirmados no `GET /alunos/valor-devido`

- `ValorDevidoService.DescontarPagamentosConfirmadosAsync(List<ValorDevidoPorMatricula> valores, Guid alunoUsuarioId, DateOnly inicio, DateOnly fimExclusivo, CancellationToken ct)`
- Para cada item da lista, buscar `Pagamento` `Confirmado` com `(MatriculaId, PeriodoInicio == inicio, PeriodoFimExclusivo == fimExclusivo)` — se existir, remover da lista.
- **Não** altera `ConsultaCobrancaService` (blst radius contido — Professor usa o mesmo serviço, sem desconto nesta Task).

## `IPagamentoRepository`

```csharp
public interface IPagamentoRepository
{
    Task<Pagamento?> BuscarPendentePorMatriculaEPeriodoAsync(Guid matriculaId, DateOnly inicio, DateOnly fimExclusivo, CancellationToken ct);
    Task<Pagamento?> BuscarConfirmadoPorMatriculaEPeriodoAsync(Guid matriculaId, DateOnly inicio, DateOnly fimExclusivo, CancellationToken ct);
    Task AdicionarAsync(Pagamento pagamento, CancellationToken ct);
    Task<List<Pagamento>> ListarPorMatriculaAsync(Guid matriculaId, CancellationToken ct); // para #200
}
```

## Migration

- `dotnet ef migrations add CriaPagamento --project src/Synclass.Infrastructure --startup-project src/Synclass.Api --output-dir Persistence/Migrations`
- Tabela `Pagamentos` com Fluent API (`PagamentoConfiguration.cs`):
  - Chave primária `Id`
  - Sem índice único em `(MatriculaId, PeriodoInicio, PeriodoFimExclusivo)` — ver decisão abaixo (mesmo texto, só a menção errada a "SQL Server" removida: o projeto usa PostgreSQL/Npgsql).
  - Colunas: `Valor` (decimal), `Status` (string via `HasConversion<string>()`), `UrlCheckout` (string, tamanho 500+ para URL longa do MP), timestamps

> **Decisão de design**: **não** usar índice único em `(MatriculaId, PeriodoInicio, PeriodoFimExclusivo)` — um período pode ter tentativas falhas + um pagamento pendente novo (Aluno tenta de novo após falha). A unicidade do pendente é garantida pela busca de `BuscarPendentePorMatriculaEPeriodoAsync` na lógica de domínio, não por constraint de banco.

## Testes

### Domain (`backend/tests/Synclass.Domain.Tests/Pagamentos/`)

- `PagamentoTests.cs` — estado inicial, transições, idempotência de `Confirmar()`/`Falhar()`.
- `PagamentoServiceTests.cs` — com fakes manuais de `IPagamentoRepository` e `IGeradorDeCheckout` (padrão: `backend/tests/Synclass.Domain.Tests/Fakes/`):
  - Rejeita Matrícula de outro Aluno
  - Rejeita Professor sem conta conectada (fake de `ConexaoMercadoPagoService` retornando null — injetar via interface ou fazer `virtual`)
  - Rejeita valor zero (fake de `ConsultaCobrancaService` sem a matricula na lista)
  - Reaproveita pendente (retorna mesma `UrlCheckout`, `IGeradorDeCheckout` **não** é chamado)
  - Cria novo pagamento com valor congelado
- `ValorDevidoServiceTests.cs` — desconto remove matricula certa, mantém outras, respeita match exato de período.

### Infrastructure

- `GeradorDeCheckoutMercadoPagoTests.cs` — usar `HttpMessageHandler` fake (padrão de `ClienteOAuthMercadoPagoTests` se existir, senão criar fake novo): verifica URL, headers, payload JSON, parse da resposta (incluindo `init_point` null → exception).

### Api

- `PagamentosControllerTests.cs` — integração com `WebApplicationFactory`: 201, 400 sem valor devido, 400 sem conta conectada, 404 matricula de outro Aluno.
- `AlunoControllerTests.cs` (ajustar existente) — `GET /alunos/valor-devido` desconta pagamento confirmado.

## Dependências de outras Tasks

- **#203 (mergeada)**: `ConexaoMercadoPagoService.ObterCollectorIdAsync(Guid professorId, CancellationToken ct)` — usado no passo 4 de `IniciarAsync`.
- **#200 (futura)**: efetua `Confirmar()`/`Falhar()` no `Pagamento` via webhook, num endpoint `[AllowAnonymous]` que precisa existir exatamente em `{MercadoPago:ApiBaseUrlPublica}/webhooks/mercado-pago` — é a `notification_url` já enviada no payload de checkout por esta Task (#199). #199 é implementável e mergeável antes, deixando pagamentos `Pendente` — desconto no valor devido só passa a operar quando #200 avançar o status.
- **[ADR-0005](decisions/ADR-0005-checkout-marketplace-mercado-pago-connect.md)**: modo marketplace obrigatório — `collector_id` do Professor, nunca conta fixa do Synclass.

## Frontend — telas de retorno do checkout (novas, mínimas)

Depois que o Aluno paga (ou desiste/falha) no Checkout Pro hospedado pelo Mercado Pago, o navegador redireciona pra uma das `back_urls` do payload — precisam existir como rotas reais no frontend, mesmo que só confirmem visualmente (o status real do pagamento só muda de fato via webhook de #200, essas telas não confirmam nada por si).

- `frontend/src/app/aluno/pagamento/confirmado.tsx`, `falhou.tsx`, `pendente.tsx` — três telas mínimas, mesmo padrão de `SafeAreaView` + `TopbarAutenticada` + `Paragraph` já usado em outras telas simples do repo (ex: `frontend/src/app/professor/[professorId]/configuracoes.tsx`, de #203). Cada uma só mostra uma mensagem fixa (“Pagamento em processamento — pode levar alguns instantes para confirmar.” pra `pendente`, mensagens equivalentes pras outras duas) e um link/botão de volta pra `/aluno/valor-devido`. Sem chamada de API nessas telas — o valor devido só reflete o pagamento confirmado na próxima consulta normal a `GET /alunos/valor-devido` (depois que #200 processar o webhook).

## Logs estruturados

- **Evento novo**: `PagamentoIniciado` — campos: `PagamentoId`, `MatriculaId`, `ProfessorId`, `AlunoUsuarioId`, `Valor`, `PeriodoInicio`, `PeriodoFimExclusivo`, `ReferenciaExterna` — **nunca** payload bruto do MP (pode conter dado sensível de payer), nunca dado de cartão (Checkout Pro é redirecionamento hospedado — Synclass nunca vê cartão).
- Adequar ao padrão de `architecture.md#logs-estruturados-e-track-id` (JSON + `TrackId`).

## Padrão de estilo a seguir

- **HttpClient puro**: `backend/src/Synclass.Infrastructure/Http/ClienteOAuthMercadoPago.cs` — mesmo padrão de registro `AddHttpClient`, injeção de `IConfiguration`, tratamento de erro e log.
- **Entidade rica com estados**: `backend/src/Synclass.Domain/Convites/Convite.cs` — métodos de transição encapsulados, setters privados, `private` ctor para EF.
- **Config de segredo**: leitura via `IConfiguration[...]`, falha explícita no startup se obrigatório — padrão de `Program.cs` atual.
- **Fakes de teste**: manuais, sem Moq — ver `backend/tests/Synclass.Domain.Tests/Fakes/`.

## Edge points (implementação, não comportamento visível)

- Valor congelado na criação — nunca recalcular ao confirmar (ver RN).
- `UrlCheckout` gravada no `Pagamento` para reaproveitar pendente sem nova chamada ao MP.
- Idempotência de `Confirmar()`/`Falhar()` — webhook de #200 pode chegar duplicado.
- Sem professor separado — `ProfessorId` em `Pagamento` referencia `Usuario.Id`.
- `GET /alunos/valor-devido` não muda forma de resposta — só remove matricula paga do array.

