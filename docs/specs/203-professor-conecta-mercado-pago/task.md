# Task: Conectar conta Mercado Pago do Professor (#203)

Card: https://github.com/RafaVargas1/Synclass/issues/203

## Inconsistências encontradas (resolvidas por Claude, ver histórico do PR/commit)

1. **Assinatura de `IClienteOAuthMercadoPago`**: `implementation.md` tinha duas versões conflitantes. Resolvido para a Versão B (`MontarUrlAutorizacao` **síncrono**, sem `CancellationToken`, sem `ObterRedirectUriAsync`) — `redirectUri` vem de `IConfiguration["MercadoPago:RedirectUri"]` (constante fixa, lida com falha explícita no startup), nunca de forma assíncrona. Use exatamente o bloco de código da seção "Assinatura do `IClienteOAuthMercadoPago` — contrato" como fonte da verdade.
2. **`IClock`**: já existe no repo (`backend/src/Synclass.Domain/Common/IClock.cs`, `backend/src/Synclass.Infrastructure/Common/SystemClock.cs`, já registrado em `Program.cs`) — não recriar, só injetar no `ConexaoMercadoPagoService`.
3. **`ProcessarCallbackAsync` não recebe `professorId`**: o endpoint de callback é `[AllowAnonymous]` (redirect vem do navegador do Professor, sem claim de sessão do Synclass disponível) — não há como o controller obter um `professorId` pra passar ao service. Assinatura corrigida para `ProcessarCallbackAsync(string code, string state, CancellationToken ct)`: o Professor é resolvido internamente via `IConexaoMercadoPagoRepository.ObterPorStateAsync(state)`, que já existia no contrato do repositório. `state` que não bate com nenhum registro (ou com `StateExpiraEm` vencido) lança `StateInvalidoException` antes de qualquer troca de `code`.

## Inconsistências encontradas (bloqueiam o término da Task — decisão necessária, fora do escopo já decidido)

4. **Não existe tela de Configurações do Professor no frontend**: o item "Componente frontend: botão 'Conectar conta do Mercado Pago' na tela de Configurações do Professor, dentro do slot de integrações" pressupõe uma tela que **não existe** no repo. Verificado por `grep`/`find` em `frontend/src/`:
   - Não há nenhuma rota/tela de configurações na área do Professor (`frontend/src/app/professor/[professorId]/` só tem `chamada`, `horarios`, `alunos`, `alocacoes`, `matriculas`, `valor-devido`).
   - O menu de navegação do Professor (`frontend/src/lib/secoesPorPapel.ts` → `secoesProfessor`) não tem item "Configurações" — não há como o usuário chegar à tela.
   - Nenhuma tela/componente referencia um "slot de integrações".
   - A referência de estilo citada (`docs/spec/design-system.md#botao-secundario`, "botão 'Salvar'") não existe: o `design-system.md` atual não tem seção/ancoragem `botao-secundario` nem menciona "Salvar".
   - O `implementation.md` é todo backend — não especifica a rota do frontend, o cliente de API de conexão, nem como o botão consome `GET /professores/mercado-pago/conectar`.
   - Implementar exigiria inventar uma tela de Configurações inteira (rota, layout, hierarquia visual, posicionamento no menu) que não foi especificada — exatamente o tipo de decisão de UI que não pode ser tomada por conta própria (ver instruções do harness: menu de navegação, hierarquia, texto). **Bloqueado até definir a tela de destino.**

5. **Log estruturado `ProfessorConectouMercadoPago` divergente do `implementation.md#log-estruturado`**: o `implementation.md` especifica o evento com `ProfessorId`, `CollectorId`, `TrackId` (e ainda os eventos `ProfessorDesconectouMercadoPago`/`ConexaoMercadoPagoFalhouRenovacao` no fluxo de renovação do item 4). O controller atual (`backend/src/Synclass.Api/Controllers/MercadoPagoController.cs`, callback `[AllowAnonymous]`) só emite `ProfessorConectouMercadoPago {TrackId} {Fluxo}` com um prefixo do `state` — sem `ProfessorId`/`CollectorId`. Não há como cumprir a spec sem tomar uma decisão de design: o callback é anônimo (não há claim de sessão) e o `ConexaoMercadoPagoService` (Domain) **não injeta `ILogger`** (camada Domain sem dependência externa, ver `docs/spec/architecture.md#backend-camadas`) — os únicos pontos com `ProfessorId`/`CollectorId` (o `ProcessarCallbackAsync`) não conseguem logar no padrão do repo (logs de negócio sempre no controller, ver `CodigoOtpSolicitado`). Emitir conforme a spec pede ou (a) alterar o contrato fechado de `ProcessarCallbackAsync` (achado 3 acima) para devolver a conexão ao controller, ou (b) injetar `ILogger` no Domain (quebra da separação de camadas) — decisões fora do escopo já decidido. **Bloqueado.**

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
- [ ] Log estruturado: evento `ProfessorConectouMercadoPago` (ver architecture.md#logs-estruturados-e-track-id) — **bloqueado pela inconsistência 5**
- [ ] Componente frontend: botão "Conectar conta do Mercado Pago" na tela de Configurações do Professor, dentro do slot de integrações — **não** um CTA cheio isolado no corpo da tela; mesmo peso visual dos demais itens de configuração. Rótulo exato: "Conectar conta do Mercado Pago". Ao clicar, redireciona para `URL de autorização` retornada pela API (sem precedente no repo: não existe tela de integração de pagamento — padrão a seguir: botão de ação secundária da tela de configuração, mesmo estilo do botão "Salvar" em `docs/spec/design-system.md#botao-secundario`). — **bloqueado pela inconsistência 4**
