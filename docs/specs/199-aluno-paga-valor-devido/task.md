# Task: Pagamento de valor devido via Mercado Pago (#199)

Card: https://github.com/RafaVargas1/Synclass/issues/199

## Inconsistências encontradas (resolvidas por Claude antes da implementação começar)

1. **`external_reference` do checkout referenciava `pagamentoId` antes dele existir**: o rascunho criava a preferência de checkout (que carrega `external_reference: "{pagamentoId}"`) *antes* de construir a entidade `Pagamento` (cujo `Id` só existe na hora do `new Pagamento(...)`). Resolvido: `PagamentoService.IniciarAsync` gera o `Guid` do pagamento **antes** de chamar `IGeradorDeCheckout.CriarPreferenciaAsync`, passa esse id como `external_reference`, e o construtor de `Pagamento` passa a receber esse `Guid` (não gera mais internamente). Ver `implementation.md#entidade-pagamento` e `implementation.md#fluxo-de-pagamentoserviceiniciarasync`.
2. **`back_urls` usava um deep link `synclass://...` que não existe** — o app ainda não é nativo (épico #194 é o último do roteiro, de propósito). Resolvido: `back_urls` aponta para rotas web reais, base configurável via `MercadoPago:UrlBaseApp` (nova env var). Ver `implementation.md#payload-de-criação-da-preferência`.
3. **`ConsultarPorAlunoAsync` tinha assinatura errada**: o rascunho chamava `ConsultarPorAlunoAsync(alunoUsuarioId, inicio, fimExclusivo, ct)`, mas a assinatura real (`backend/src/Synclass.Domain/Cobrancas/ConsultaCobrancaService.cs:57`) recebe um `PeriodoConsulta`, não datas soltas — mesmo padrão já usado por `ValorDevidoAlunoController.cs:37-38` (`PeriodoConsulta.Criar(inicio, fim)`, que já valida `inicio < fim` via `PeriodoConsultaInvalidoException`). Corrigido em `implementation.md#fluxo-de-pagamentoserviceiniciarasync`.
4. **`ResourceNotFoundException` não existe no repo** (verificado por grep) — o padrão real do repo é uma exceção de domínio específica por caso, capturada no controller e mapeada para `NotFound()` (ver `MarcacoesHorarioController.cs:49-51`, `AlunoNaoVinculadoAoProfessorException` → 404). Trocado por `MatriculaNaoPertenceAoAlunoException` (nova), sempre 404 (não 403 — o padrão do repo não distingue os dois pra não vazar se o recurso existe).
5. **Índice único mencionava "SQL Server"** — o projeto usa PostgreSQL (Npgsql, ver `.env.example`/`docker-compose.yml`), não SQL Server. A decisão final (não usar índice único) continua a mesma; só removida a menção tecnicamente errada.
6. **`422 Unprocessable Entity` sem precedente no repo** — grep não encontrou nenhum uso de 422 em `backend/src/Synclass.Api/`. Trocado por `400` (comportamento padrão do `[ApiController]` para falha de model binding/validation, mesmo já usado em todo o resto da Api).

## Ordem de execução

- [x] Teste unidade (Domain): `Pagamento` nasce `Pendente` com valor congelado e período exatos
- [x] Implementação mínima: entidade `Pagamento`, enum `StatusPagamento` (Pendente/Confirmado/Falhou)
- [x] Teste unidade (Domain): transição Pendente→Confirmado marca `ConfirmadoEm`; Pendente→Falhou marca `FalhouEm`
- [x] Teste unidade (Domain): confirmar/falhar um `Pagamento` já `Confirmado` é no-op (idempotente, sem exceção)
- [x] Implementação mínima: transições de estado + timestamps de transição
- [x] Teste unidade (Domain): `PagamentoService.IniciarAsync` rejeita Matrícula de outro Aluno (`MatriculaNaoPertenceAoAlunoException`, mapeada para 404)
- [x] Teste unidade (Domain): `IniciarAsync` rejeita Professor sem conta conectada (`ProfessorSemContaConectadaException`), sem criar `Pagamento`
- [x] Teste unidade (Domain): `IniciarAsync` rejeita valor zero no período (`SemValorDevidoException` — mapeia pra 400)
- [ ] Teste unidade (Domain): `IniciarAsync` reaproveita `Pagamento` `Pendente` existente da mesma (MatriculaId, período), devolve a mesma `UrlCheckout`, sem chamar `IGeradorDeCheckout`
- [ ] Teste unidade (Domain): `IniciarAsync` cria novo `Pagamento` com `Valor` congelado quando não há pendente
- [ ] Implementação mínima: `PagamentoService.IniciarAsync` completo + `IPagamentoRepository`
- [ ] Teste unidade (Domain): `ValorDevidoService.DescontarPagamentosConfirmadosAsync` remove Matrículas com `Pagamento` `Confirmado` (match exato `Inicio`/`FimExclusivo`)
- [ ] Implementação mínima: passo de desconto no serviço do valor devido (leitura, não altera `ConsultaCobrancaService`)
- [ ] Migration: tabela `Pagamentos` (Fluent API, nome timestamp+PascalCase)
- [ ] Teste integração (Infrastructure): `IGeradorDeCheckoutMercadoPago` serializa payload correto e parseia resposta da API real do MP (contra contrato, mock HTTP)
- [ ] Implementação: `GeradorDeCheckoutMercadoPago` (HttpClient puro, `AddHttpClient` em Program.cs)
- [ ] Teste integração (Api): `POST /alunos/matriculas/{matriculaId:guid}/pagamentos` 201 → `{ pagamentoId, urlCheckout, valor }`
- [ ] Teste integração (Api): 404 Matrícula de outro Aluno (`MatriculaNaoPertenceAoAlunoException`); 400 sem valor devido; 400 Professor sem conta conectada
- [ ] Implementação: controller + DTOs (validação de entrada antes do Domain)
- [ ] Teste (Api): `GET /alunos/valor-devido` desconta `Pagamento` `Confirmado` (teste na camada de serviço, não HTTP puro)
- [ ] Implementação: passo de desconto no endpoint do Aluno (controller/serviço da Aluno)
- [ ] Componente frontend: três telas mínimas `frontend/src/app/aluno/pagamento/{confirmado,falhou,pendente}.tsx` (destino das `back_urls` do checkout) — copie exatamente o desenho de `implementation.md#frontend-telas-de-retorno-do-checkout-novas-mínimas` (mesmo padrão de tela simples de `frontend/src/app/professor/[professorId]/configuracoes.tsx`, sem chamada de API, só mensagem fixa + link de volta pra `/aluno/valor-devido`)
- [ ] Log estruturado: `PagamentoIniciado` (valores, sem dado de cartão) — `PagamentoConfirmado`/`PagamentoFalhou` ficam para #200
- [ ] Suíte de testes completa (Domain + Infrastructure + Api) verde antes do PR

