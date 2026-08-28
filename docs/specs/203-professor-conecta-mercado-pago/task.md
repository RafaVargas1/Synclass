# Task: Conectar conta Mercado Pago do Professor (#203)

Card: https://github.com/RafaVargas1/Synclass/issues/203

## Inconsistências encontradas (resolvidas por Claude, ver histórico do PR/commit)

1. **Assinatura de `IClienteOAuthMercadoPago`**: `implementation.md` tinha duas versões conflitantes. Resolvido para a Versão B (`MontarUrlAutorizacao` **síncrono**, sem `CancellationToken`, sem `ObterRedirectUriAsync`) — `redirectUri` vem de `IConfiguration["MercadoPago:RedirectUri"]` (constante fixa, lida com falha explícita no startup), nunca de forma assíncrona. Use exatamente o bloco de código da seção "Assinatura do `IClienteOAuthMercadoPago` — contrato" como fonte da verdade.
2. **`IClock`**: já existe no repo (`backend/src/Synclass.Domain/Common/IClock.cs`, `backend/src/Synclass.Infrastructure/Common/SystemClock.cs`, já registrado em `Program.cs`) — não recriar, só injetar no `ConexaoMercadoPagoService`.
3. **`ProcessarCallbackAsync` não recebe `professorId`**: o endpoint de callback é `[AllowAnonymous]` (redirect vem do navegador do Professor, sem claim de sessão do Synclass disponível) — não há como o controller obter um `professorId` pra passar ao service. Assinatura corrigida para `ProcessarCallbackAsync(string code, string state, CancellationToken ct)`: o Professor é resolvido internamente via `IConexaoMercadoPagoRepository.ObterPorStateAsync(state)`, que já existia no contrato do repositório. `state` que não bate com nenhum registro (ou com `StateExpiraEm` vencido) lança `StateInvalidoException` antes de qualquer troca de `code`.

## Ordem de execução

- [x] Teste unidade (Domain): `ConexaoMercadoPagoService.ConectarAsync` retorna URL de autorização quando Professor não possui conexão ativa
- [x] Implementação mínima do cenário 1: `ConexaoMercadoPagoService` + `IConexaoMercadoPagoRepository`
- [x] Teste unidade (Domain): `ConexaoMercadoPagoService.ConectarAsync` gera URL de autorização com `state` e `redirect_uri` corretos
- [x] Implementação mínima do cenário 2: geração da URL no Service
- [x] Teste unidade (Domain): `ConexaoMercadoPagoService.ConectarAsync` chamado por um Professor que já tem conexão ativa **reaproveita o registro existente** (atualiza `State`/`StateExpiraEm`, não cria segundo registro nem exige desconectar antes) — ver `implementation.md#reconexão`
- [x] Implementação mínima do cenário 2b: `ConectarAsync` faz upsert sobre `ObterPorProfessorAsync` em vez de sempre `AdicionarAsync`
- [x] Teste unidade (Domain): `ConexaoMercadoPagoService.ProcessarCallbackAsync` rejeita `state` expirado (mais de 10 minutos desde `ConectarAsync`) com `StateInvalidoException`
- [x] Teste unidade (Domain): `ConexaoMercadoPagoService.ProcessarCallbackAsync` persiste conexão quando `code` é trocado com sucesso
- [x] Implementação mínima do cenário 3: `ProcessarCallbackAsync(string code, string state, CancellationToken ct)` — resolve o registro via `ObterPorStateAsync(state)`, valida, chama `IClienteOAuthMercadoPago`, persiste via repositório
- [ ] Teste unidade (Domain): `ConexaoMercadoPagoService.ProcessarCallbackAsync` rejeita `state` inválido (`StateInvalidoException`)
- [ ] Implementação mínima do cenário 4: validação de `state` no `ProcessarCallbackAsync`
- [ ] Teste unidade (Domain): `ConexaoMercadoPagoService.ObterCollectorIdAsync` retorna `collector_id` para Professor conectado
- [ ] Implementação mínima do cenário 5: `ObterCollectorIdAsync`
- [ ] Teste unidade (Domain): `ConexaoMercadoPagoService.ObterCollectorIdAsync` retorna `null` para Professor sem conexão
- [ ] Implementação mínima do cenário 6: fallback para `null` na `ObterCollectorIdAsync`
- [ ] Teste unidade (Domain): `ConexaoMercadoPagoService.ObterCollectorIdAsync` aciona renovação via `RefreshToken` quando `AccessToken` expirado, e retorna `null` se renovação falhar
- [ ] Implementação mínima do cenário 7: renovação de token no `ConexaoMercadoPagoService`
- [ ] Migration: `CriarConexaoMercadoPago` (tabela `ConexaoMercadoPago`)
- [ ] Teste de fumaça (Api): `GET /professores/mercado-pago/conectar` retorna `200` com `{ url }` para Professor autenticado
- [ ] Teste de fumaça (Api): `GET /professores/mercado-pago/callback?code=...&state=...` retorna `200` e persiste conexão
- [ ] Teste de fumaça (Api): `GET /professores/mercado-pago/conectar` retorna `404` quando `UsuarioNaoEncontradoException` é lançada para Professor inexistente
- [ ] Log estruturado: evento `ProfessorConectouMercadoPago` (ver architecture.md#logs-estruturados-e-track-id)
- [ ] Componente frontend: botão "Conectar conta do Mercado Pago" na tela de Configurações do Professor, dentro do slot de integrações — **não** um CTA cheio isolado no corpo da tela; mesmo peso visual dos demais itens de configuração. Rótulo exato: "Conectar conta do Mercado Pago". Ao clicar, redireciona para `URL de autorização` retornada pela API (sem precedente no repo: não existe tela de integração de pagamento — padrão a seguir: botão de ação secundária da tela de configuração, mesmo estilo do botão "Salvar" em `docs/spec/design-system.md#botao-secundario`).
