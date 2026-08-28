# Task: Conectar conta Mercado Pago do Professor (#203)

Card: https://github.com/RafaVargas1/Synclass/issues/203

## Inconsistências encontradas (resolvidas por Claude, ver histórico do PR/commit)

1. **Assinatura de `IClienteOAuthMercadoPago`**: `implementation.md` tinha duas versões conflitantes. Resolvido para a Versão B (`MontarUrlAutorizacao` **síncrono**, sem `CancellationToken`, sem `ObterRedirectUriAsync`) — `redirectUri` vem de `IConfiguration["MercadoPago:RedirectUri"]` (constante fixa, lida com falha explícita no startup), nunca de forma assíncrona. Use exatamente o bloco de código da seção "Assinatura do `IClienteOAuthMercadoPago` — contrato" como fonte da verdade.
2. **`IClock`**: já existe no repo (`backend/src/Synclass.Domain/Common/IClock.cs`, `backend/src/Synclass.Infrastructure/Common/SystemClock.cs`, já registrado em `Program.cs`) — não recriar, só injetar no `ConexaoMercadoPagoService`.
3. **`ProcessarCallbackAsync` não recebe `professorId`**: o endpoint de callback é `[AllowAnonymous]` (redirect vem do navegador do Professor, sem claim de sessão do Synclass disponível) — não há como o controller obter um `professorId` pra passar ao service. Assinatura corrigida para `ProcessarCallbackAsync(string code, string state, CancellationToken ct)`: o Professor é resolvido internamente via `IConexaoMercadoPagoRepository.ObterPorStateAsync(state)`, que já existia no contrato do repositório. `state` que não bate com nenhum registro (ou com `StateExpiraEm` vencido) lança `StateInvalidoException` antes de qualquer troca de `code`.

## Inconsistências encontradas (resolvidas por Claude — 2ª rodada)

4. **Tela de Configurações do Professor não existia**: resolvido criando `frontend/src/app/professor/[professorId]/configuracoes.tsx` (nova, mínima, só a seção de integração de pagamento) + `frontend/src/lib/api/mercadoPago.ts` (cliente de API) + item novo em `frontend/src/lib/secoesPorPapel.ts#secoesProfessor`. Ver seção "Frontend — tela de Configurações do Professor" do `implementation.md` — tem o código completo de cada arquivo, use exatamente como está escrito, não invente layout diferente.
5. **Log de `ProfessorConectouMercadoPago` movido para o controller**: `ProcessarCallbackAsync` agora **retorna `(Guid ProfessorId, string CollectorId)`** em vez de `void` (Domain continua sem `ILogger`, sem violar camadas). `MercadoPagoController.Callback` recebe esse retorno e loga o evento com `ProfessorId`/`CollectorId`/`TrackId` reais — **substitua** o log atual do controller (que hoje só tem um prefixo de `state`, sem `ProfessorId`/`CollectorId`) por este. Ver seção "Decisão de design: logging de `ProfessorConectouMercadoPago` sem violar camadas" do `implementation.md`.

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
- [x] Teste unidade (Domain): `ConexaoMercadoPagoService.ProcessarCallbackAsync` rejeita `state` inválido (`StateInvalidoException`)
- [x] Implementação mínima do cenário 4: validação de `state` no `ProcessarCallbackAsync`
- [x] Teste unidade (Domain): `ConexaoMercadoPagoService.ObterCollectorIdAsync` retorna `collector_id` para Professor conectado
- [x] Implementação mínima do cenário 5: `ObterCollectorIdAsync`
- [x] Teste unidade (Domain): `ConexaoMercadoPagoService.ObterCollectorIdAsync` retorna `null` para Professor sem conexão
- [x] Implementação mínima do cenário 6: fallback para `null` na `ObterCollectorIdAsync`
- [x] Teste unidade (Domain): `ConexaoMercadoPagoService.ObterCollectorIdAsync` aciona renovação via `RefreshToken` quando `AccessToken` expirado, e retorna `null` se renovação falhar
- [x] Implementação mínima do cenário 7: renovação de token no `ConexaoMercadoPagoService`
- [x] Migration: `CriarConexaoMercadoPago` (tabela `ConexaoMercadoPago`)
- [x] Teste de fumaça (Api): `GET /professores/mercado-pago/conectar` retorna `200` com `{ url }` para Professor autenticado
- [x] Teste de fumaça (Api): `GET /professores/mercado-pago/callback?code=...&state=...` retorna `200` e persiste conexão
- [x] Teste de fumaça (Api): `GET /professores/mercado-pago/conectar` retorna `404` quando `UsuarioNaoEncontradoException` é lançada para Professor inexistente
- [ ] Ajustar `ProcessarCallbackAsync` para retornar `(Guid ProfessorId, string CollectorId)` em vez de `void`; teste unidade (Domain) cobrindo o retorno
- [ ] Log estruturado: `MercadoPagoController.Callback` loga `ProfessorConectouMercadoPago` com `ProfessorId`/`CollectorId`/`TrackId` do retorno de `ProcessarCallbackAsync` — substitui o log atual do controller (ver `implementation.md#decisão-de-design-logging-de-professorconectoumercadopago-sem-violar-camadas`)
- [ ] Cliente de API novo: `frontend/src/lib/api/mercadoPago.ts` (`conectarMercadoPago`) — copie exatamente o código de `implementation.md#frontend-tela-de-configurações-do-professor`
- [ ] Componente frontend: tela nova `frontend/src/app/professor/[professorId]/configuracoes.tsx` com o botão "Conectar conta do Mercado Pago" — copie exatamente o código de `implementation.md#frontend-tela-de-configurações-do-professor` (JSX completo já escrito lá, não redesenhe)
- [ ] Item novo em `frontend/src/lib/secoesPorPapel.ts#secoesProfessor`: `{ label: 'Configurações', href: '/professor/${usuarioId}/configuracoes', icone: 'settings-outline' }`, por último na lista, mais o nome de ícone novo em `NomeIconeSecao`
