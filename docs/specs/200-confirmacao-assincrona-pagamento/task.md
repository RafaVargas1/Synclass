# Task: Endpoint de webhook de confirmação de pagamento (#200)

Card: https://github.com/RafaVargas1/Synclass/issues/200

## Inconsistências encontradas (resolvidas por Claude antes da implementação começar)

1. **Ordem invertida no fluxo do webhook**: o rascunho descrevia "buscar `Pagamento` por `Id` (= `external_reference`)" *antes* do passo que efetivamente busca o `external_reference` na API do Mercado Pago — impossível na prática (não dá pra buscar por um valor que ainda não se tem). Corrigido: a ordem real é (1) verificar assinatura, (2) `GET /v1/payments/{data.id}` pra obter `status` + `external_reference`, (3) só então buscar o `Pagamento` local por esse `external_reference` (= `Pagamento.Id`). Ver `implementation.md#fluxo-completo-do-endpoint-sequência`.
2. **Guard `??=` no `EventoId` travava a idempotência dos eventos seguintes**: o rascunho só setava `EventoId` na primeira vez (nula-coalescente) e nunca mais atualizava. Isso quebra o requisito de "reentrega do MESMO evento não duplica log" pra qualquer evento *depois* do primeiro — ex: o evento de estorno (`data.id` diferente do de confirmação) processa normalmente na primeira vez, mas se o Mercado Pago reentregar esse MESMO evento de estorno depois, `Pagamento.EventoId` ainda guardaria o `data.id` da confirmação (nunca atualizado), o check de idempotência não bateria, e o estorno seria reprocessado/logado de novo. Corrigido: `EventoId` é **sempre sobrescrito** com o `data.id` do evento atual após processar com sucesso (não `??=`) — ele guarda sempre o último evento processado, e é isso que protege contra a reentrega desse evento específico.
3. **`Devido` misturado com `Pendente` na busca de pendente**: o rascunho pedia pra `BuscarPendentePorMatriculaEPeriodoAsync` incluir o novo estado (`Confirmado` que foi estornado) como se fosse "pendente reaproveitável" — errado: reaproveitar o `Pagamento` estornado devolveria ao Aluno a `UrlCheckout` de um checkout **já usado/estornado**, quebrado. Renomeado o estado novo pra `Estornado` (mais claro que "Devido" — que confundia estado de pagamento com o conceito de "valor devido"), e a query de pendente **não muda** — continua só `Pendente`. O requisito do card ("valor volta a aparecer como devido") já é satisfeito de graça: `ValorDevidoService.DescontarPagamentosConfirmadosAsync` (#199) só desconta pagamentos com `Status == Confirmado` — assim que o status vira `Estornado`, o valor deixa de ser descontado e volta a aparecer sozinho, sem mudar nenhuma outra query.

## Ordem de execução

- [ ] Teste unidade (Domain): `WebhookMercadoPagoService.VerificarAssinaturaAsync` aceita assinatura HMAC-SHA256 válida conforme formato `x-signature` do Mercado Pago (ver `implementation.md#verificação-de-assinatura` — dívida técnica documentada, formato do manifest a confirmar)
- [ ] Implementação mínima do cenário 1: `VerificarAssinaturaAsync` em `WebhookMercadoPagoService`, com `TODO(webhook-signature)` no código apontando a incerteza do formato exato do manifest
- [ ] Teste unidade (Domain): payload adulterado (mudança no `data.id`) tem assinatura rejeitada
- [ ] Teste unidade (Domain): sem `x-signature` ou com formato malformado (ex: só `ts=`, sem `v1=`) lança `AssinaturaInvalidaException`
- [ ] Implementação: `IPagamentoRepository.ObterPorIdAsync(Guid id, CancellationToken ct)` e `AtualizarAsync(Pagamento pagamento, CancellationToken ct)` — não existem hoje, adicionar na interface e na implementação EF (`PagamentoRepository`)
- [ ] Implementação: `GeradorDeCheckoutMercadoPago.ObterPagamentoAsync(string paymentId, CancellationToken ct)` — reaproveita o mesmo `HttpClient`/Bearer já configurado (não cria cliente HTTP novo), devolve `PagamentoMercadoPagoDto` (`Id`, `Status`, `ExternalReference`)
- [ ] Migration: coluna `EventoId` (string?, nullable) em `Pagamentos`
- [ ] Teste unidade (Domain): `Pagamento.Estornar(IClock)` só age quando `Status == Confirmado` (vira `Estornado`); nos demais estados é no-op
- [ ] Implementação: `Estornar(IClock)` na entidade `Pagamento` + valor `Estornado` no enum `StatusPagamento`
- [ ] Teste unidade (Domain): `WebhookMercadoPagoService.ProcessarEventoAsync` — evento `approved` chama `Confirmar` e seta `EventoId = data.id`
- [ ] Teste unidade (Domain): evento `refunded`/`rejected` num `Pagamento` `Confirmado` chama `Estornar`; no-op se não estava `Confirmado`
- [ ] Teste unidade (Domain): `Pagamento` inexistente pro `external_reference` recebido — não lança, só sinaliza "não encontrado" pro controller responder 200 e logar aviso
- [ ] Teste unidade (Domain): mesmo evento (`data.id` igual) processado duas vezes — segunda vez não chama `AtualizarAsync` de novo nem loga `WebhookPagamentoRecebido` de novo (idempotência via `EventoId`, comparado ANTES de aplicar qualquer transição)
- [ ] Teste unidade (Domain): dois eventos DIFERENTES pro mesmo `Pagamento` (ex: `approved` depois `refunded`) processam os dois normalmente — `EventoId` reflete sempre o último evento processado, não trava no primeiro
- [ ] Implementação: `WebhookMercadoPagoService.ProcessarEventoAsync` completo, seguindo a ordem corrigida em `implementation.md#fluxo-completo-do-endpoint-sequência`
- [ ] Implementação: `PagamentosWebhookController` (ou método novo em `PagamentosController`) — `[AllowAnonymous]` com comentário explícito de que é intencional, rota fixa `POST /webhooks/mercado-pago` (já é a `notification_url` em produção desde #199, não pode mudar), lê `data.id` da query string, headers `x-signature`/`x-request-id`
- [ ] Teste de integração (Api): `POST /webhooks/mercado-pago` com assinatura válida e evento `approved` → 200, `Pagamento` correspondente fica `Confirmado`
- [ ] Teste de integração (Api): assinatura inválida/ausente → 400, `Pagamento` não muda
- [ ] Teste de integração (Api): `data.id` ausente na query → 400
- [ ] `.env.example` (raiz): adicionar `MERCADOPAGO_WEBHOOK_SECRET` (vazio no exemplo) — lido via `IConfiguration["MercadoPago:WebhookSecret"]`, falha explícita no startup
- [ ] Log estruturado: `WebhookPagamentoRecebido`, `WebhookPagamentoRejeitado`, `PagamentoEstornado`, `PagamentoNaoEncontrado` (aviso) — ver `implementation.md#logs-estruturados`
- [ ] Suíte de testes completa (Domain + Infrastructure + Api) verde antes do PR
